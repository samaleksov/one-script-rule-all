﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge.Queue;
using ReactNative.Common;
using ReactNative.Tracing;
using System;
using static System.FormattableString;

namespace ReactNative.Bridge
{
    /// <summary>
    /// Class to the JavaScript execution environment and means of transport
    /// for messages between JavaScript and the native environment.
    /// </summary>
    public class ReactBridge : IReactBridge
    {
        private readonly IJavaScriptExecutor _jsExecutor;
        private readonly IReactCallback _reactCallback;
        private readonly IMessageQueueThread _nativeModulesQueueThread;

        /// <summary>
        /// Instantiates the <see cref="IReactBridge"/>.
        /// </summary>
        /// <param name="executor">The JavaScript executor.</param>
        /// <param name="reactCallback">The native callback handler.</param>
        /// <param name="nativeModulesQueueThread">
        /// The native modules queue thread.
        /// </param>
        public ReactBridge(
            IJavaScriptExecutor executor,
            IReactCallback reactCallback,
            IMessageQueueThread nativeModulesQueueThread)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));
            if (reactCallback == null)
                throw new ArgumentNullException(nameof(reactCallback));
            if (nativeModulesQueueThread == null)
                throw new ArgumentNullException(nameof(nativeModulesQueueThread));

            _jsExecutor = executor;
            _reactCallback = reactCallback;
            _nativeModulesQueueThread = nativeModulesQueueThread;
        }

        /// <summary>
        /// Calls a JavaScript function.
        /// </summary>
        /// <param name="moduleName">The module ID.</param>
        /// <param name="method">The method ID.</param>
        /// <param name="arguments">The arguments.</param>
        public void CallFunction(string moduleName, string method, JArray arguments)
        {
            var response = _jsExecutor.CallFunctionReturnFlushedQueue(moduleName, method, arguments);
            ProcessResponse(response);
        }

        /// <summary>
        /// Invokes a JavaScript callback.
        /// </summary>
        /// <param name="callbackId">The callback ID.</param>
        /// <param name="arguments">The arguments.</param>
        public void InvokeCallback(int callbackId, JArray arguments)
        {
            var response = _jsExecutor.InvokeCallbackAndReturnFlushedQueue(callbackId, arguments);
            ProcessResponse(response);
        }

        /// <summary>
        /// Sets a global JavaScript variable.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonEncodedArgument">The JSON-encoded value.</param>
        public void SetGlobalVariable(string propertyName, string jsonEncodedArgument)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            _jsExecutor.SetGlobalVariable(propertyName, JToken.Parse(jsonEncodedArgument));
        }

        /// <summary>
        /// Evaluates JavaScript.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="sourceUrl">The source URL.</param>
        public void RunScript(string script, string sourceUrl)
        {
            if (script == null)
                throw new ArgumentNullException(nameof(script));
            if (sourceUrl == null)
                throw new ArgumentNullException(nameof(sourceUrl));

            _jsExecutor.RunScript(script, sourceUrl);
            var response = _jsExecutor.FlushedQueue();
            ProcessResponse(response);
        }

        /// <summary>
        /// Disposes the bridge.
        /// </summary>
        public void Dispose()
        {
            _jsExecutor.Dispose();
        }

        private void ProcessResponse(JToken response)
        {
            if (response == null || response.Type == JTokenType.Null || response.Type == JTokenType.Undefined)
            {
                return;
            }

            var messages = response as JArray;
            if (messages == null)
            {
                throw new InvalidOperationException(
                    "Did not get valid calls back from JavaScript. Message type: " + response.Type);
            }

            if (messages.Count < 3)
            {
                throw new InvalidOperationException(
                    "Did not get valid calls back from JavaScript. Message count: " + messages.Count);
            }

            var moduleIds = messages[0] as JArray;
            var methodIds = messages[1] as JArray;
            var paramsArray = messages[2] as JArray;
            if (moduleIds == null || methodIds == null || paramsArray == null ||
                moduleIds.Count != methodIds.Count || moduleIds.Count != paramsArray.Count)
            {
                throw new InvalidOperationException(
                    "Did not get valid calls back from JavaScript. JSON: " + response);
            }

            _nativeModulesQueueThread.RunOnQueue(() =>
            {
                for (var i = 0; i < moduleIds.Count; ++i)
                {
                    var moduleId = moduleIds[i].Value<int>();
                    var methodId = methodIds[i].Value<int>();
                    var args = (JArray)paramsArray[i];

                    _reactCallback.Invoke(moduleId, methodId, args);
                };

                _reactCallback.OnBatchComplete();
            });
        }
    }
}
