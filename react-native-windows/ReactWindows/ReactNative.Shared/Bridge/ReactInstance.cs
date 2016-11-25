using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactNative.Bridge.Queue;
using ReactNative.Common;
using ReactNative.Tracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using static System.FormattableString;

namespace ReactNative.Bridge
{
    /// <summary>
    /// A higher level API on top of the <see cref="IJavaScriptExecutor" /> and module registries. This provides an
    /// environment allowing the invocation of JavaScript methods.
    /// </summary>
    class ReactInstance : IReactInstance, IAsyncDisposable
    {
        private readonly NativeModuleRegistry _registry;
        private readonly JavaScriptModuleRegistry _jsRegistry;
        private readonly Func<IJavaScriptExecutor> _jsExecutorFactory;
        private readonly JavaScriptBundleLoader _bundleLoader;
        private readonly Action<Exception> _nativeModuleCallExceptionHandler;

        private IReactBridge _bridge;

        private bool _initialized;

        private ReactInstance(
            ReactQueueConfigurationSpec reactQueueConfigurationSpec,
            Func<IJavaScriptExecutor> jsExecutorFactory,
            NativeModuleRegistry registry,
            JavaScriptModuleRegistry jsModuleRegistry,
            JavaScriptBundleLoader bundleLoader,
            Action<Exception> nativeModuleCallExceptionHandler)
        {
            _registry = registry;
            _jsExecutorFactory = jsExecutorFactory;
            _nativeModuleCallExceptionHandler = nativeModuleCallExceptionHandler;
            _jsRegistry = jsModuleRegistry;
            _bundleLoader = bundleLoader;

            QueueConfiguration = ReactQueueConfiguration.Create(
                reactQueueConfigurationSpec,
                HandleException);
        }

        public bool IsDisposed
        {
            get;
            private set;
        }

        public IEnumerable<INativeModule> NativeModules
        {
            get
            {
                return _registry.Modules;
            }
        }

        public IReactQueueConfiguration QueueConfiguration
        {
            get;
        } 

        public T GetJavaScriptModule<T>() where T : IJavaScriptModule
        {
            return _jsRegistry.GetJavaScriptModule<T>(this);
        }

        public T GetNativeModule<T>() where T : INativeModule
        {
            return _registry.GetModule<T>();
        }

        public void Initialize()
        {
            DispatcherHelpers.AssertOnDispatcher();
            if (_initialized)
            {
                throw new InvalidOperationException("This React instance has already been initialized.");
            }

            _initialized = true;
            _registry.NotifyReactInstanceInitialize();
        }

        public async Task InitializeBridgeAsync()
        {
            await _bundleLoader.InitializeAsync().ConfigureAwait(false);

            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "initializeBridge").Start())
            {
                _bridge = await QueueConfiguration.JavaScriptQueueThread.CallOnQueue(() =>
                {
                    QueueConfiguration.JavaScriptQueueThread.AssertOnThread();

                    var jsExecutor = _jsExecutorFactory();

                    var bridge = default(ReactBridge);
                    using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "ReactBridgeCtor").Start())
                    {
                        bridge = new ReactBridge(
                            jsExecutor,
                            new NativeModulesReactCallback(this),
                            QueueConfiguration.NativeModulesQueueThread);
                    }

                    return bridge;
                }).ConfigureAwait(false);

                await QueueConfiguration.JavaScriptQueueThread.CallOnQueue(() =>
                {
                    using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "setBatchedBridgeConfig").Start())
                    {
                        _bridge.SetGlobalVariable("__fbBatchedBridgeConfig", BuildModulesConfig());
                    }

                    _bundleLoader.LoadScript(_bridge);

                    return default(object);
                }).ConfigureAwait(false);
            }
        }

        public void InvokeCallback(int callbackId, JArray arguments)
        {
            if (IsDisposed)
            {
                Tracer.Write(ReactConstants.Tag, "Invoking JS callback after bridge has been destroyed.");
                return;
            }

            QueueConfiguration.JavaScriptQueueThread.RunOnQueue(() =>
            {
                QueueConfiguration.JavaScriptQueueThread.AssertOnThread();
                if (IsDisposed)
                {
                    return;
                }

                using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "<callback>").Start())
                {
                    _bridge.InvokeCallback(callbackId, arguments);
                }
            });
        }

        public /* TODO: internal? */ void InvokeFunction(string module, string method, JArray arguments, string tracingName)
        {
            QueueConfiguration.JavaScriptQueueThread.RunOnQueue(() =>
            {
                QueueConfiguration.JavaScriptQueueThread.AssertOnThread();

                if (IsDisposed)
                {
                    return;
                }

                using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, tracingName).Start())
                {
                    if (_bridge == null)
                    {
                        throw new InvalidOperationException("Bridge has not been initialized.");
                    }

                    _bridge.CallFunction(module, method, arguments);
                }
            });
        }

        public async Task DisposeAsync()
        {
            DispatcherHelpers.AssertOnDispatcher();

            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            _registry.NotifyReactInstanceDispose();

            await QueueConfiguration.JavaScriptQueueThread.CallOnQueue(() =>
            {
                using (_bridge) { }
                return true;
            }).ConfigureAwait(false);

            await Task.Run(new Action(QueueConfiguration.Dispose)).ConfigureAwait(false);
        }

        private string BuildModulesConfig()
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            try
            {
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("remoteModuleConfig");
                    _registry.WriteModuleDescriptions(writer);
                    writer.WriteEndObject();
                }

                return stringWriter.ToString();
            }
            finally
            {
                if (stringWriter != null)
                {
                    stringWriter.Dispose();
                }
            }
        }

        private void HandleException(Exception ex)
        {
            _nativeModuleCallExceptionHandler(ex);
            QueueConfiguration.DispatcherQueueThread.RunOnQueue(async () => 
                await DisposeAsync().ConfigureAwait(false));
        }

        public sealed class Builder
        {
            private ReactQueueConfigurationSpec _reactQueueConfigurationSpec;
            private NativeModuleRegistry _registry;
            private JavaScriptModuleRegistry _jsModuleRegistry;
            private Func<IJavaScriptExecutor> _jsExecutorFactory;
            private JavaScriptBundleLoader _bundleLoader;
            private Action<Exception> _nativeModuleCallExceptionHandler;

            public ReactQueueConfigurationSpec QueueConfigurationSpec
            {
                set
                {
                    _reactQueueConfigurationSpec = value;
                }
            }

            public NativeModuleRegistry Registry
            {
                set
                {
                    _registry = value;
                }
            }

            public JavaScriptModuleRegistry JavaScriptModuleRegistry
            {
                set
                {
                    _jsModuleRegistry = value;
                }
            }

            public Func<IJavaScriptExecutor> JavaScriptExecutorFactory
            {
                set
                {
                    _jsExecutorFactory = value;
                }
            }

            public JavaScriptBundleLoader BundleLoader
            {
                set
                {
                    _bundleLoader = value;
                }
            }

            public Action<Exception> NativeModuleCallExceptionHandler
            {
                set
                {
                    _nativeModuleCallExceptionHandler = value;
                }
            }

            public ReactInstance Build()
            {
                AssertNotNull(_reactQueueConfigurationSpec, nameof(QueueConfigurationSpec));
                AssertNotNull(_jsExecutorFactory, nameof(JavaScriptExecutorFactory));
                AssertNotNull(_registry, nameof(Registry));
                AssertNotNull(_bundleLoader, nameof(BundleLoader));
                AssertNotNull(_nativeModuleCallExceptionHandler, nameof(NativeModuleCallExceptionHandler));
                 
                return new ReactInstance(
                    _reactQueueConfigurationSpec,
                    _jsExecutorFactory,
                    _registry,
                    _jsModuleRegistry,
                    _bundleLoader,
                    _nativeModuleCallExceptionHandler);
            }

            private void AssertNotNull(object value, string name)
            {
                if (value == null)
                    throw new InvalidOperationException(Invariant($"'{name}' has not been set."));
            }
        }

        class NativeModulesReactCallback : IReactCallback
        {
            private readonly ReactInstance _parent;

            public NativeModulesReactCallback(ReactInstance parent)
            {
                _parent = parent;
            }

            public void Invoke(int moduleId, int methodId, JArray parameters)
            {
                _parent.QueueConfiguration.NativeModulesQueueThread.AssertOnThread();

                if (_parent.IsDisposed)
                {
                    return;
                }

                _parent._registry.Invoke(_parent, moduleId, methodId, parameters);
            }

            public void OnBatchComplete()
            {
                _parent.QueueConfiguration.NativeModulesQueueThread.AssertOnThread();

                // The bridge may have been destroyed due to an exception
                // during the batch. In that case native modules could be in a
                // bad state so we don't want to call anything on them.
                if (!_parent.IsDisposed)
                {
                    using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "OnBatchComplete").Start())
                    {
                        _parent._registry.OnBatchComplete();
                    }
                }
            }
        }
    }
}