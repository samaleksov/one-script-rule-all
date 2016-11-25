﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ReactNative.DevSupport
{
    class WebSocketJavaScriptExecutor : IJavaScriptExecutor
    {
        private const int ConnectTimeoutMilliseconds = 5000;
        private const int ConnectRetryCount = 3;

        private readonly MessageWebSocket _webSocket;
        private readonly JObject _injectedObjects;
        private readonly IDictionary<int, TaskCompletionSource<JToken>> _callbacks;

        private bool _connected;
        private DataWriter _messageWriter;
        private int _requestId;
        private bool _isDisposed;

        public WebSocketJavaScriptExecutor()
        {
            _webSocket = new MessageWebSocket();
            _webSocket.Control.MessageType = SocketMessageType.Utf8;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Closed += OnClosed;

            _injectedObjects = new JObject();
            _callbacks = new Dictionary<int, TaskCompletionSource<JToken>>();
        }

        public async Task ConnectAsync(string webSocketServerUrl, CancellationToken token)
        {
            var uri = default(Uri);
            if (!Uri.TryCreate(webSocketServerUrl, UriKind.Absolute, out uri))
            {
                throw new ArgumentOutOfRangeException(nameof(webSocketServerUrl), "Expected valid URI argument.");
            }

            var retryCount = ConnectRetryCount;
            while (true)
            {
                var timeoutSource = new CancellationTokenSource(ConnectTimeoutMilliseconds);
                using (token.Register(timeoutSource.Cancel))
                {
                    try
                    {
                        await ConnectCoreAsync(uri, timeoutSource.Token).ConfigureAwait(false);
                        return;
                    }
                    catch (OperationCanceledException ex)
                    when (ex.CancellationToken == timeoutSource.Token)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    catch
                    {
                        if (--retryCount <= 0)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public JToken CallFunctionReturnFlushedQueue(string moduleName, string methodName, JArray arguments)
        {
            return Call("callFunctionReturnFlushedQueue", new JArray
            {
                moduleName,
                methodName,
                arguments,
            });
        }

        public JToken InvokeCallbackAndReturnFlushedQueue(int callbackId, JArray arguments)
        {
            return Call("invokeCallbackAndReturnFlushedQueue", new JArray
            {
                callbackId,
                arguments,
            });
        }

        public JToken FlushedQueue()
        {
            return Call("flushedQueue", new JArray());
        }

        public void RunScript(string script, string sourceUrl)
        {
            var requestId = Interlocked.Increment(ref _requestId);
            var callback = new TaskCompletionSource<JToken>();
            _callbacks.Add(requestId, callback);

            try
            {
                var request = new JObject
                {
                    { "id", requestId },
                    { "method", "executeApplicationScript" },
                    { "url", script },
                    { "inject", _injectedObjects },
                };

                SendMessageAsync(requestId, request.ToString(Formatting.None)).Wait();
                callback.Task.Wait();
            }
            catch (AggregateException ex)
            when (ex.InnerExceptions.Count == 1)
            {
                throw ex.InnerException;
            }
            finally
            {
                _callbacks.Remove(requestId);
            }
        }

        public void SetGlobalVariable(string propertyName, JToken value)
        {
            _injectedObjects.Add(propertyName, value.ToString(Formatting.None));
        }

        public void Dispose()
        {
            _isDisposed = true;
            _messageWriter.Dispose();
            _webSocket.Dispose();
        }

        private JToken Call(string methodName, JArray arguments)
        {
            var requestId = Interlocked.Increment(ref _requestId);
            var callback = new TaskCompletionSource<JToken>();
            _callbacks.Add(requestId, callback);

            try
            {
                var request = new JObject
                {
                    { "id", requestId },
                    { "method", methodName },
                    { "arguments", arguments },
                };

                SendMessageAsync(requestId, request.ToString(Formatting.None)).Wait();
                return callback.Task.Result;
            }
            catch (AggregateException ex)
            when (ex.InnerExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                /* Should not */
                throw;
            }
            finally
            {
                _callbacks.Remove(requestId);
            }
        }

        private async Task ConnectCoreAsync(Uri uri, CancellationToken token)
        {
            var asyncAction = default(IAsyncAction);
            using (token.Register(() => asyncAction?.Cancel()))
            {
                if (!_connected)
                {
                    asyncAction = _webSocket.ConnectAsync(uri);
                    await asyncAction;
                    _messageWriter = new DataWriter(_webSocket.OutputStream);
                    _connected = true;
                }
            }

            await PrepareJavaScriptRuntimeAsync(token);
        }

        private async Task<JToken> PrepareJavaScriptRuntimeAsync(CancellationToken token)
        {
            var cancellationSource = new TaskCompletionSource<bool>();
            using (token.Register(() => cancellationSource.SetResult(false)))
            {
                var requestId = Interlocked.Increment(ref _requestId);
                var callback = new TaskCompletionSource<JToken>();
                _callbacks.Add(requestId, callback);

                try
                {
                    var request = new JObject
                    {
                        { "id", requestId },
                        { "method", "prepareJSRuntime" },
                    };

                    await SendMessageAsync(requestId, request.ToString(Formatting.None));
                    await Task.WhenAny(callback.Task, cancellationSource.Task);
                    token.ThrowIfCancellationRequested();
                    return await callback.Task;
                }
                finally
                {
                    _callbacks.Remove(requestId);
                }
            }
        }

        private async Task SendMessageAsync(int requestId, string message)
        {
            if (!_isDisposed)
            {
                _messageWriter.WriteString(message);
                await _messageWriter.StoreAsync();
                // TODO: check result of `StoreAsync()`
            }
            else
            {
                var callback = default(TaskCompletionSource<JToken>);
                if (_callbacks.TryGetValue(requestId, out callback))
                {
                    callback.TrySetResult(JValue.CreateNull());
                }
            }
        }

        private void OnMessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            using (var reader = args.GetDataReader())
            {
                reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                var response = reader.ReadString(reader.UnconsumedBufferLength);

                var json = JObject.Parse(response);
                if (json.ContainsKey("replyID"))
                {
                    var replyId = json.Value<int>("replyID");
                    var callback = default(TaskCompletionSource<JToken>);
                    if (_callbacks.TryGetValue(replyId, out callback))
                    {
                        var result = default(JToken);
                        if (json != null && json.TryGetValue("result", out result))
                        {
                            if (result.Type == JTokenType.String)
                            {
                                callback.TrySetResult(JToken.Parse(result.Value<string>()));
                            }
                            else
                            {
                                callback.TrySetResult(result);
                            }
                        }
                        else
                        {
                            callback.TrySetResult(null);
                        }
                    }
                }
            }
        }

        private void OnClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            Dispose();
        }
    }
}
