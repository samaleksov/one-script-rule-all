﻿using Newtonsoft.Json.Linq;
using System;

namespace ReactNative.Bridge
{
    /// <summary>
    /// Interface to the JavaScript execution environment and means of
    /// transport for messages between JavaScript and the native environment.
    /// </summary>
    public interface IReactBridge : IDisposable
    {
        /// <summary>
        /// Calls a JavaScript function.
        /// </summary>
        /// <param name="moduleName">The module ID.</param>
        /// <param name="method">The method ID.</param>
        /// <param name="arguments">The arguments.</param>
        void CallFunction(string moduleName, string method, JArray arguments);

        /// <summary>
        /// Invokes a JavaScript callback.
        /// </summary>
        /// <param name="callbackId">The callback identifier.</param>
        /// <param name="arguments">The arguments.</param>
        void InvokeCallback(int callbackId, JArray arguments);

        /// <summary>
        /// Sets a global JavaScript variable.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonEncodedArgument">The JSON-encoded value.</param>
        void SetGlobalVariable(string propertyName, string jsonEncodedArgument);

        /// <summary>
        /// Evaluates JavaScript.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="sourceUrl">The source URL.</param>
        void RunScript(string script, string sourceUrl);
    }
}
