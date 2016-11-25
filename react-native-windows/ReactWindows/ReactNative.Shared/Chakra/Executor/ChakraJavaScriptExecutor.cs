﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLStorage;
using ReactNative.Bridge;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.FormattableString;

namespace ReactNative.Chakra.Executor
{
    /// <summary>
    /// JavaScript runtime wrapper.
    /// </summary>
    public sealed class ChakraJavaScriptExecutor : IJavaScriptExecutor
    {
        private const string JsonName = "JSON";
        private const string FBBatchedBridgeVariableName = "__fbBatchedBridge";

        private readonly JavaScriptRuntime _runtime;

        private JavaScriptSourceContext _context;

        private JavaScriptNativeFunction _consoleInfo;
        private JavaScriptNativeFunction _consoleLog;
        private JavaScriptNativeFunction _consoleWarn;
        private JavaScriptNativeFunction _consoleError;

        private JavaScriptValue _globalObject;

        private JavaScriptValue _callFunctionAndReturnFlushedQueueFunction;
        private JavaScriptValue _invokeCallbackAndReturnFlushedQueueFunction;
        private JavaScriptValue _flushedQueueFunction;

#if !NATIVE_JSON_MARSHALING
        private JavaScriptValue _parseFunction;
        private JavaScriptValue _stringifyFunction;
#endif

        /// <summary>
        /// Instantiates the <see cref="ChakraJavaScriptExecutor"/>.
        /// </summary>
        public ChakraJavaScriptExecutor()
        {
            _runtime = JavaScriptRuntime.Create();
            _context = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);
            InitializeChakra();
        }

        /// <summary>
        /// Call the JavaScript method from the given module.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The flushed queue of native operations.</returns>
        public JToken CallFunctionReturnFlushedQueue(string moduleName, string methodName, JArray arguments)
        {
            if (moduleName == null)
                throw new ArgumentNullException(nameof(moduleName));
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var moduleNameValue = JavaScriptValue.FromString(moduleName);
            moduleNameValue.AddRef();
            var methodNameValue = JavaScriptValue.FromString(methodName);
            methodNameValue.AddRef();
            var argumentsValue = ConvertJson(arguments);
            argumentsValue.AddRef();

            var callArguments = new JavaScriptValue[4];
            callArguments[0] = EnsureGlobalObject();
            callArguments[1] = moduleNameValue;
            callArguments[2] = methodNameValue;
            callArguments[3] = argumentsValue;
            var method = EnsureCallFunction();
            var flushedQueue = ConvertJson(method.CallFunction(callArguments));

            argumentsValue.Release();
            methodNameValue.Release();
            moduleNameValue.Release();

            return flushedQueue;
        }

        /// <summary>
        /// Flush the queue.
        /// </summary>
        /// <returns>The flushed queue of native operations.</returns>
        public JToken FlushedQueue()
        {
            var method = EnsureFlushedQueueFunction();
            var callArguments = new JavaScriptValue[1];
            callArguments[0] = EnsureGlobalObject();
            return ConvertJson(method.CallFunction(callArguments));
        }

        /// <summary>
        /// Invoke the JavaScript callback.
        /// </summary>
        /// <param name="callbackId">The callback identifier.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The flushed queue of native operations.</returns>
        public JToken InvokeCallbackAndReturnFlushedQueue(int callbackId, JArray arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var callbackIdValue = JavaScriptValue.FromInt32(callbackId);
            callbackIdValue.AddRef();
            var argumentsValue = ConvertJson(arguments);
            argumentsValue.AddRef();

            var callArguments = new JavaScriptValue[3];
            callArguments[0] = EnsureGlobalObject();
            callArguments[1] = callbackIdValue;
            callArguments[2] = argumentsValue;
            var method = EnsureInvokeFunction();
            var flushedQueue = ConvertJson(method.CallFunction(callArguments));

            argumentsValue.Release();
            callbackIdValue.Release();

            return flushedQueue;
        }

        /// <summary>
        /// Runs the given script.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="sourceUrl">The source URL.</param>
        public void RunScript(string script, string sourceUrl)
        {
            if (script == null)
                throw new ArgumentNullException(nameof(script));
            if (sourceUrl == null)
                throw new ArgumentNullException(nameof(sourceUrl));

            string source = LoadScriptAsync(script).Result;

            try
            {
                _context = JavaScriptSourceContext.Increment(_context);
                JavaScriptContext.RunScript(source, _context, sourceUrl);
            }
            catch (JavaScriptScriptException ex)
            {
                var jsonError = JavaScriptValueToJTokenConverter.Convert(ex.Error);
                var message = jsonError.Value<string>("message");
                var stackTrace = jsonError.Value<string>("stack");
                throw new Modules.Core.JavaScriptException(message ?? ex.Message, stackTrace, ex);
            }
        }

        private static async Task<string> LoadScriptAsync(string fileName)
        {
            try
            {
                var storageFile = await FileSystem.Current.GetFileFromPathAsync(fileName).ConfigureAwait(false);
                return await storageFile.ReadAllTextAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var exceptionMessage = Invariant($"File read exception for asset '{fileName}'.");
                throw new InvalidOperationException(exceptionMessage, ex);
            }
        }

        /// <summary>
        /// Sets a global variable in the JavaScript runtime.
        /// </summary>
        /// <param name="propertyName">The global variable name.</param>
        /// <param name="value">The value.</param>
        public void SetGlobalVariable(string propertyName, JToken value)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var javaScriptValue = ConvertJson(value);
            var propertyId = JavaScriptPropertyId.FromString(propertyName);
            EnsureGlobalObject().SetProperty(propertyId, javaScriptValue, true);
        }

        /// <summary>
        /// Gets a global variable from the JavaScript runtime.
        /// </summary>
        /// <param name="propertyName">The global variable name.</param>
        /// <returns>The value.</returns>
        public JToken GetGlobalVariable(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var propertyId = JavaScriptPropertyId.FromString(propertyName);
            return ConvertJson(EnsureGlobalObject().GetProperty(propertyId));
        }

        /// <summary>
        /// Disposes the <see cref="ChakraJavaScriptExecutor"/> instance.
        /// </summary>
        public void Dispose()
        {
            JavaScriptContext.Current = JavaScriptContext.Invalid;
            _runtime.Dispose();
        }

        private void InitializeChakra()
        {
            JavaScriptContext.Current = _runtime.CreateContext();

            var consolePropertyId = default(JavaScriptPropertyId);
            Native.ThrowIfError(
                Native.JsGetPropertyIdFromName("console", out consolePropertyId));

            var consoleObject = JavaScriptValue.CreateObject();
            EnsureGlobalObject().SetProperty(consolePropertyId, consoleObject, true);

            _consoleInfo = ConsoleInfo;
            _consoleLog = ConsoleLog;
            _consoleWarn = ConsoleWarn;
            _consoleError = ConsoleError;

            DefineHostCallback(consoleObject, "info", _consoleInfo);
            DefineHostCallback(consoleObject, "log", _consoleLog);
            DefineHostCallback(consoleObject, "warn", _consoleWarn);
            DefineHostCallback(consoleObject, "error", _consoleError);

            Debug.WriteLine("Chakra initialization successful.");
        }

        #region JSON Marshaling

#if NATIVE_JSON_MARSHALING
        private JavaScriptValue ConvertJson(JToken token)
        {
            return JTokenToJavaScriptValueConverter.Convert(token);
        }

        private JToken ConvertJson(JavaScriptValue value)
        {
            return JavaScriptValueToJTokenConverter.Convert(value);
        }
#else
        private JavaScriptValue ConvertJson(JToken token)
        {
            var jsonString = token.ToString(Formatting.None);
            var jsonStringValue = JavaScriptValue.FromString(jsonString);
            jsonStringValue.AddRef();
            var parseFunction = EnsureParseFunction();
            var jsonValue = parseFunction.CallFunction(_globalObject, jsonStringValue);
            jsonStringValue.Release();
            return jsonValue;
        }

        private JToken ConvertJson(JavaScriptValue value)
        {
            var stringifyFunction = EnsureStringifyFunction();
            var jsonStringValue = stringifyFunction.CallFunction(_globalObject, value);
            jsonStringValue.AddRef();
            var jsonString = jsonStringValue.ToString();
            jsonStringValue.Release();
            return JToken.Parse(jsonString);
        }
#endif

        #endregion

        #region Console Callbacks

        private static void DefineHostCallback(
            JavaScriptValue obj,
            string callbackName,
            JavaScriptNativeFunction callback)
        {
            var propertyId = JavaScriptPropertyId.FromString(callbackName);
            var function = JavaScriptValue.CreateFunction(callback);
            obj.SetProperty(propertyId, function, true);
        }

        private JavaScriptValue ConsoleInfo(
            JavaScriptValue callee,
            bool isConstructCall,
            JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            return ConsoleCallback("Info", callee, isConstructCall, arguments, argumentCount, callbackData);
        }

        private JavaScriptValue ConsoleLog(
            JavaScriptValue callee,
            bool isConstructCall,
            JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            return ConsoleCallback("Log", callee, isConstructCall, arguments, argumentCount, callbackData);
        }

        private JavaScriptValue ConsoleWarn(
            JavaScriptValue callee,
            bool isConstructCall,
            JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            return ConsoleCallback("Warn", callee, isConstructCall, arguments, argumentCount, callbackData);
        }

        private JavaScriptValue ConsoleError(
            JavaScriptValue callee,
            bool isConstructCall,
            JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            return ConsoleCallback("Error", callee, isConstructCall, arguments, argumentCount, callbackData);
        }

        private JavaScriptValue ConsoleCallback(
            string kind,
            JavaScriptValue callee,
            bool isConstructCall,
            JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            try
            {
                Debug.Write(Invariant($"[JS {kind}]"));

                // First argument is this-context, ignore...
                for (var i = 1; i < argumentCount; ++i)
                {
                    Debug.Write(Stringify(arguments[i]) + " ");
                }

                Debug.WriteLine("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(Invariant($"Error in ChakraExecutor.ConsoleCallback: {ex.Message}"));
            }

            return JavaScriptValue.Undefined;
        }

        private string Stringify(JavaScriptValue value)
        {
            switch (value.ValueType)
            {
                case JavaScriptValueType.Undefined:
                case JavaScriptValueType.Null:
                case JavaScriptValueType.Number:
                case JavaScriptValueType.String:
                case JavaScriptValueType.Boolean:
                case JavaScriptValueType.Object:
                case JavaScriptValueType.Array:
                case JavaScriptValueType.TypedArray:
                    return ConvertJson(value).ToString(Formatting.None);
                case JavaScriptValueType.Function:
                case JavaScriptValueType.Error:
                case JavaScriptValueType.Symbol:
                case JavaScriptValueType.ArrayBuffer:
                    return value.ConvertToString().ToString();
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion

        #region Global Helpers

        private JavaScriptValue EnsureGlobalObject()
        {
            if (!_globalObject.IsValid)
            {
                _globalObject = JavaScriptValue.GlobalObject;
            }

            return _globalObject;
        }

        private JavaScriptValue EnsureParseFunction()
        {
            if (!_parseFunction.IsValid)
            {
                var globalObject = EnsureGlobalObject();
                var jsonObject = globalObject.GetProperty(JavaScriptPropertyId.FromString(JsonName));
                _parseFunction = jsonObject.GetProperty(JavaScriptPropertyId.FromString("parse"));
            }

            return _parseFunction;
        }

        private JavaScriptValue EnsureBatchedBridge()
        {
            var globalObject = EnsureGlobalObject();
            var propertyId = JavaScriptPropertyId.FromString(FBBatchedBridgeVariableName);
            var fbBatchedBridge = globalObject.GetProperty(propertyId);
            if (fbBatchedBridge.ValueType != JavaScriptValueType.Object)
            {
                throw new InvalidOperationException(
                    Invariant($"Could not resolve '{FBBatchedBridgeVariableName}' object.  Check the JavaScript bundle to ensure it is generated correctly."));
            }

            return fbBatchedBridge;
        }

        private JavaScriptValue EnsureStringifyFunction()
        {
            if (!_stringifyFunction.IsValid)
            {
                var globalObject = EnsureGlobalObject();
                var jsonObject = globalObject.GetProperty(JavaScriptPropertyId.FromString(JsonName));
                _stringifyFunction = jsonObject.GetProperty(JavaScriptPropertyId.FromString("stringify"));
            }

            return _stringifyFunction;
        }

        private JavaScriptValue EnsureCallFunction()
        {
            if (!_callFunctionAndReturnFlushedQueueFunction.IsValid)
            {
                var fbBatchedBridge = EnsureBatchedBridge();
                var functionPropertyId = JavaScriptPropertyId.FromString("callFunctionReturnFlushedQueue");
                _callFunctionAndReturnFlushedQueueFunction = fbBatchedBridge.GetProperty(functionPropertyId);
            }

            return _callFunctionAndReturnFlushedQueueFunction;
        }

        private JavaScriptValue EnsureInvokeFunction()
        {
            if (!_invokeCallbackAndReturnFlushedQueueFunction.IsValid)
            {
                var fbBatchedBridge = EnsureBatchedBridge();
                var functionPropertyId = JavaScriptPropertyId.FromString("invokeCallbackAndReturnFlushedQueue");
                _invokeCallbackAndReturnFlushedQueueFunction = fbBatchedBridge.GetProperty(functionPropertyId);
            }

            return _invokeCallbackAndReturnFlushedQueueFunction;
        }

        private JavaScriptValue EnsureFlushedQueueFunction()
        {
            if (!_flushedQueueFunction.IsValid)
            {
                var fbBatchedBridge = EnsureBatchedBridge();
                var functionPropertyId = JavaScriptPropertyId.FromString("flushedQueue");
                _flushedQueueFunction = fbBatchedBridge.GetProperty(functionPropertyId);
            }

            return _flushedQueueFunction;
        }

        #endregion
    }
}
