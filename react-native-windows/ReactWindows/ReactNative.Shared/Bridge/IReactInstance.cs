﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge.Queue;
using System.Collections.Generic;

namespace ReactNative.Bridge
{
    /// <summary>
    /// An abstraction for the asynchronous JavaScript bridge. This provides an
    /// environment allowing the invocation of JavaScript methods and lets a
    /// set of native APIs be invokable from JavaScript as well.
    /// </summary>
    public interface IReactInstance : IAsyncCancelable
    {
        /// <summary>
        /// Enumerates the available native modules.
        /// </summary>
        IEnumerable<INativeModule> NativeModules { get; }

        /// <summary>
        /// The React queue configuration.
        /// </summary>
        IReactQueueConfiguration QueueConfiguration { get; }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Invokes a JavaScript function.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        /// <param name="method">The method ID.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="tracingName">The tracing name.</param>
        void InvokeFunction(string moduleName, string method, JArray arguments, string tracingName);

        /// <summary>
        /// Invokes a JavaScript callback.
        /// </summary>
        /// <param name="callbackId">The callback ID.</param>
        /// <param name="arguments">The arguments.</param>
        void InvokeCallback(int callbackId, JArray arguments);

        /// <summary>
        /// Gets a JavaScript module instance.
        /// </summary>
        /// <typeparam name="T">Type of JavaScript module.</typeparam>
        /// <returns>The JavaScript module instance.</returns>
        T GetJavaScriptModule<T>() where T : IJavaScriptModule;

        /// <summary>
        /// Gets a native module instance.
        /// </summary>
        /// <typeparam name="T">Type of native module.</typeparam>
        /// <returns>The native module instance.</returns>
        T GetNativeModule<T>() where T : INativeModule;
    }
}
