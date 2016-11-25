﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.Collections;
using ReactNative.Modules.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
#else
using PCLStorage;
using System.Linq;
using System.Net.Http;
using HttpMultipartFormDataContent = System.Net.Http.MultipartFormDataContent;
using HttpStreamContent = System.Net.Http.StreamContent;
using HttpStringContent = System.Net.Http.StringContent;
using HttpBaseProtocolFilter = System.Net.Http.WebRequestHandler;
#endif

namespace ReactNative.Modules.Network
{
    /// <summary>
    /// Implements the XMLHttpRequest JavaScript interface.
    /// </summary>
    public class NetworkingModule : ReactContextNativeModuleBase, ILifecycleEventListener
    {
        private const int MaxChunkSizeBetweenFlushes = 8 * 1024; // 8kb
        private readonly IHttpClient _client;
        private readonly TaskCancellationManager<int> _tasks;

        private bool _shuttingDown;

        /// <summary>
        /// Instantiates the <see cref="NetworkingModule"/>.
        /// </summary>
        /// <param name="reactContext">The context.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient disposed by module.")]
        internal NetworkingModule(ReactContext reactContext)
            : this(CreateDefaultHttpClient(), reactContext)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="NetworkingModule"/>.
        /// </summary>
        /// <param name="client">The HTTP client.</param>
        /// <param name="reactContext">The context.</param>
        internal NetworkingModule(IHttpClient client, ReactContext reactContext)
            : base(reactContext)
        {
            _client = client;
            _tasks = new TaskCancellationManager<int>();
        }

        /// <summary>
        /// The name of the native module.
        /// </summary>
        public override string Name
        {
            get
            {
                return "RCTNetworking";
            }
        }

        private RCTDeviceEventEmitter EventEmitter
        {
            get
            {
                return Context.GetJavaScriptModule<RCTDeviceEventEmitter>();
            }
        }

        /// <summary>
        /// Send an HTTP request on the networking module.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="requestId">The request ID.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="data">The request data.</param>
        /// <param name="responseType">The response type (either "text" or "base64").</param>
        /// <param name="useIncrementalUpdates">
        /// <code>true</code> if incremental updates are allowed.
        /// </param>
        /// <param name="timeout">The timeout.</param>
        [ReactMethod]
        public void sendRequest(
            string method,
            Uri url,
            int requestId,
            string[][] headers,
            JObject data,
            string responseType,
            bool useIncrementalUpdates,
            int timeout)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            if (responseType == null)
                throw new ArgumentNullException(nameof(responseType));
            if (responseType != "text" && responseType != "base64")
                throw new ArgumentOutOfRangeException(nameof(responseType));

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            var headerData = default(HttpContentHeaderData);
            if (headers != null)
            {
                headerData = HttpContentHelpers.ExtractHeaders(headers);
                ApplyHeaders(request, headers);
            }

            if (data != null)
            {
                var body = data.Value<string>("string");
                var uri = default(string);
                var formData = default(JArray);
                if (body != null)
                {
                    if (headerData.ContentType == null)
                    {
                        OnRequestError(requestId, "Payload is set but no 'content-type' header specified.", false);
                        return;
                    }

                    request.Content = HttpContentHelpers.CreateFromBody(headerData, body);
                }
                else if ((uri = data.Value<string>("uri")) != null)
                {
                    if (headerData.ContentType == null)
                    {
                        OnRequestError(requestId, "Payload is set but no 'content-type' header specified.", false);
                        return;
                    }

                    _tasks.Add(requestId, token => ProcessRequestFromUriAsync(
                        requestId,
                        new Uri(uri),
                        useIncrementalUpdates,
                        timeout,
                        request,
                        responseType,
                        token));

                    return;
                }
                else if ((formData = (JArray)data.GetValue("formData", StringComparison.Ordinal)) != null)
                {
                    if (headerData.ContentType == null)
                    {
                        headerData.ContentType = "multipart/form-data";
                    }

                    var formDataContent = new HttpMultipartFormDataContent();
                    foreach (var content in formData)
                    {
                        var fieldName = content.Value<string>("fieldName");

                        var stringContent = content.Value<string>("string");
                        if (stringContent != null)
                        {
                            formDataContent.Add(new HttpStringContent(stringContent), fieldName);
                        }
                    }

                    request.Content = formDataContent;
                }
            }

            _tasks.Add(requestId, async token =>
            {
                using (request)
                {
                    try
                    {
                        await ProcessRequestAsync(
                            requestId,
                            useIncrementalUpdates,
                            timeout,
                            request,
                            responseType,
                            token);
                    }
                    finally
                    {
                        var content = request.Content;
                        if (content != null)
                        {
                            content.Dispose();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Abort an HTTP request with the given request ID.
        /// </summary>
        /// <param name="requestId">The request ID.</param>
        [ReactMethod]
        public void abortRequest(int requestId)
        {
            _tasks.Cancel(requestId);
        }

        /// <summary>
        /// Called before a <see cref="IReactInstance"/> is disposed.
        /// </summary>
        public override void OnReactInstanceDispose()
        {
            _shuttingDown = true;
        }

        /// <summary>
        /// Called when the host receives the suspend event.
        /// </summary>
        public void OnSuspend()
        {
        }

        /// <summary>
        /// Called when the host receives the resume event.
        /// </summary>
        public void OnResume()
        {
        }

        /// <summary>
        /// Called when the host is disposed.
        /// </summary>
        public void OnDestroy()
        {
            _client.Dispose();
        }

        private async Task ProcessRequestFromUriAsync(
            int requestId,
            Uri uri,
            bool useIncrementalUpdates,
            int timeout,
            HttpRequestMessage request,
            string responseType,
            CancellationToken token)
        {
#if WINDOWS_UWP
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            var inputStream = await storageFile.OpenReadAsync().AsTask().ConfigureAwait(false);
#else
            var storageFile = await FileSystem.Current.GetFileFromPathAsync(uri.ToString()).ConfigureAwait(false);
            var input = await storageFile.ReadAllTextAsync().ConfigureAwait(false);
            var byteArray = Encoding.UTF8.GetBytes(input);
            var inputStream = new MemoryStream(byteArray);
#endif
            request.Content = new HttpStreamContent(inputStream);
            await ProcessRequestAsync(
                requestId,
                useIncrementalUpdates,
                timeout,
                request,
                responseType,
                token).ConfigureAwait(false);
        }

        private async Task ProcessRequestAsync(
            int requestId,
            bool useIncrementalUpdates,
            int timeout,
            HttpRequestMessage request,
            string responseType,
            CancellationToken token)
        {
            var timeoutSource = timeout > 0
                ? new CancellationTokenSource(timeout)
                : new CancellationTokenSource();

            using (timeoutSource)
            {
                try
                {
                    using (token.Register(timeoutSource.Cancel))
                    using (var response = await _client.SendRequestAsync(request, timeoutSource.Token).ConfigureAwait(false))
                    {
                        OnResponseReceived(requestId, response);

                        if (useIncrementalUpdates && responseType == "text")
                        {
#if WINDOWS_UWP
                            var length = response.Content.Headers.ContentLength;
                            var inputStream = await response.Content.ReadAsInputStreamAsync().AsTask().ConfigureAwait(false);
                            var stream = inputStream.AsStreamForRead();
#else
                            var length = (ulong?)response.Content.Headers.ContentLength;
                            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                            using (stream)
                            {
                                await ProcessResponseIncrementalAsync(requestId, stream, length, timeoutSource.Token).ConfigureAwait(false);
                                OnRequestSuccess(requestId);
                            }
#if WINDOWS_UWP
                            inputStream.Dispose();
#endif
                        }
                        else
                        {
                            if (response.Content != null)
                            {
                                if (responseType == "text")
                                {
#if WINDOWS_UWP
                                    var responseBody = await response.Content.ReadAsStringAsync().AsTask().ConfigureAwait(false);
#else
                                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                                    if (responseBody != null)
                                    {
                                        OnDataReceived(requestId, responseBody);
                                    }
                                }
                                else
                                {
                                    Debug.Assert(responseType == "base64");
                                    using (var memoryStream = new MemoryStream())
                                    {
#if WINDOWS_UWP
                                        using (var outputStream = memoryStream.AsOutputStream())
                                        {
                                            await response.Content.WriteToStreamAsync(outputStream).AsTask().ConfigureAwait(false);
                                        }
#else
                                        using (var outputStream = memoryStream)
                                        {
                                            await response.Content.CopyToAsync(outputStream).ConfigureAwait(false);
                                        }
#endif

                                        OnDataReceived(requestId, Convert.ToBase64String(memoryStream.ToArray()));
                                    }
                                }
                            }

                            OnRequestSuccess(requestId);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                when (ex.CancellationToken == timeoutSource.Token)
                {
                    // Cancellation was due to timeout
                    if (!token.IsCancellationRequested)
                    {
                        OnRequestError(requestId, ex.Message, true);
                    }
                }
                catch (Exception ex)
                {
                    if (_shuttingDown)
                    {
                        return;
                    }

                    OnRequestError(requestId, ex.Message, false);
                }
            }
        }

        private async Task ProcessResponseIncrementalAsync(int requestId, Stream stream, ulong? length, CancellationToken token)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, MaxChunkSizeBetweenFlushes, true))
            {
                var buffer = new char[MaxChunkSizeBetweenFlushes];
                var read = default(int);
                var progress = 0;
                var total = length.HasValue ? (long)length : -1;
                while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    progress += read;
                    OnIncrementalDataReceived(requestId, new string(buffer, 0, read), progress, total);
                }
            }
        }

        private void OnResponseReceived(int requestId, HttpResponseMessage response)
        {
            var headerData = new JObject();
#if WINDOWS_UWP
            var responseHeaders = response.Headers;
#else
            var responseHeaders = response.Headers.Select(pair => 
                new KeyValuePair<string, string>(pair.Key, string.Join(", ", pair.Value)));
#endif
            TranslateHeaders(headerData, responseHeaders);

            if (response.Content != null)
            {
#if WINDOWS_UWP
                var responseContentHeaders = response.Content.Headers;
#else
                var responseContentHeaders = response.Content.Headers.Select(pair => 
                    new KeyValuePair<string, string>(pair.Key, string.Join(", ", pair.Value)));
#endif
                TranslateHeaders(headerData, responseContentHeaders);
            }

            var args = new JArray
            {
                requestId,
                (int)response.StatusCode,
                headerData,
                response.RequestMessage.RequestUri.AbsolutePath,
            };

            EventEmitter.emit("didReceiveNetworkResponse", args);
        }

        private void OnIncrementalDataReceived(int requestId, string data, long progress, long total)
        {
            var args = new JArray
            {
                requestId,
                data,
                progress,
                total
            };

            EventEmitter.emit("didReceiveNetworkIncrementalData", args);
        }

        private void OnDataReceived(int requestId, string responseBody)
        {
            EventEmitter.emit("didReceiveNetworkData", new JArray
            {
                requestId,
                responseBody,
            });
        }

        private void OnRequestError(int requestId, string message, bool timeout)
        {
            EventEmitter.emit("didCompleteNetworkResponse", new JArray
            {
                requestId,
                message,
                timeout
            });
        }

        private void OnRequestSuccess(int requestId)
        {
            EventEmitter.emit("didCompleteNetworkResponse", new JArray
            {
                requestId,
                null,
            });
        }

        private static void ApplyHeaders(HttpRequestMessage request, string[][] headers)
        {
            foreach (var header in headers)
            {
                var key = header[0];
                switch (key.ToLowerInvariant())
                {
                    case "content-encoding":
                    case "content-length":
                    case "content-type":
                        break;
                    default:
                        request.Headers.Add(key, header[1]);
                        break;
                }
            }
        }

        private static void TranslateHeaders(JObject headerData, IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                if (headerData.ContainsKey(header.Key))
                {
                    var existing = headerData[header.Key].Value<string>();
                    headerData[header.Key] = existing + ", " + header.Value;
                }
                else
                {
                    headerData.Add(header.Key, header.Value);
                }
            }
        }

        private static IHttpClient CreateDefaultHttpClient()
        {
            return new DefaultHttpClient(
                new HttpClient(
                    new HttpBaseProtocolFilter
                    {
                        AllowAutoRedirect = false,
                    }));
        }
    }
}
