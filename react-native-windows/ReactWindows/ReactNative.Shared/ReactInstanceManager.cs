﻿using ReactNative.Bridge;
using ReactNative.Bridge.Queue;
using ReactNative.Chakra.Executor;
using ReactNative.Common;
using ReactNative.DevSupport;
using ReactNative.Modules.Core;
using ReactNative.Touch;
using ReactNative.Tracing;
using ReactNative.UIManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.FormattableString;

namespace ReactNative
{
    /// <summary>
    /// This interface manages instances of <see cref="IReactInstance" />.
    /// It exposes a way to configure React instances using
    /// <see cref="IReactPackage"/> and keeps track of the lifecycle of that
    /// instance. It also sets up a connection between the instance and the
    /// developer support functionality of the framework.
    ///
    /// An instance of this manager is required to start the JavaScript
    /// application in <see cref="ReactRootView"/>
    /// (<see cref="ReactRootView.StartReactApplication(IReactInstanceManager, string)"/>).
    ///
    /// The lifecycle of the instance of <see cref="IReactInstanceManager"/>
    /// should be bound to the application that owns the
    /// <see cref="ReactRootView"/> that is used to render the React
    /// application using this instance manager. It is required to pass
    /// lifecycle events to the instance manager (i.e., <see cref="OnSuspend"/>,
    /// <see cref="IAsyncDisposable.DisposeAsync"/>, and <see cref="OnResume(Action)"/>).
    /// </summary>
    public class ReactInstanceManager : IReactInstanceManager
    {
        private readonly List<ReactRootView> _attachedRootViews = new List<ReactRootView>();

        private readonly string _jsBundleFile;
        private readonly string _jsMainModuleName;
        private readonly IReadOnlyList<IReactPackage> _packages;
        private readonly IDevSupportManager _devSupportManager;
        private readonly bool _useDeveloperSupport;
        private readonly UIImplementationProvider _uiImplementationProvider;
        private readonly Action<Exception> _nativeModuleCallExceptionHandler;

        private LifecycleState _lifecycleState;
        private bool _hasStartedCreatingInitialContext;
        private Task _contextInitializationTask;
        private Func<IJavaScriptExecutor> _pendingJsExecutorFactory;
        private JavaScriptBundleLoader _pendingJsBundleLoader;
        private string _sourceUrl;
        private ReactContext _currentReactContext;
        private Action _defaultBackButtonHandler;

        /// <summary>
        /// Event triggered when a React context has been initialized.
        /// </summary>
        public event EventHandler<ReactContextInitializedEventArgs> ReactContextInitialized;

        private ReactInstanceManager(
            string jsBundleFile,
            string jsMainModuleName,
            IReadOnlyList<IReactPackage> packages,
            bool useDeveloperSupport,
            LifecycleState initialLifecycleState,
            UIImplementationProvider uiImplementationProvider,
            Action<Exception> nativeModuleCallExceptionHandler)
        {
            if (packages == null)
                throw new ArgumentNullException(nameof(packages));
            if (uiImplementationProvider == null)
                throw new ArgumentNullException(nameof(uiImplementationProvider));

            _jsBundleFile = jsBundleFile;
            _jsMainModuleName = jsMainModuleName;
            _packages = packages;

            _useDeveloperSupport = useDeveloperSupport;
            _devSupportManager = _useDeveloperSupport
                ? (IDevSupportManager)new DevSupportManager(
                    new ReactInstanceDevCommandsHandler(this),
                    _jsBundleFile,
                    _jsMainModuleName)
                : new DisabledDevSupportManager();

            _lifecycleState = initialLifecycleState;
            _uiImplementationProvider = uiImplementationProvider;
            _nativeModuleCallExceptionHandler = nativeModuleCallExceptionHandler;
        }

        /// <summary>
        /// The developer support manager for the instance.
        /// </summary>
        public IDevSupportManager DevSupportManager
        {
            get
            {
                return _devSupportManager;
            }
        }

        /// <summary>
        /// Signals whether <see cref="CreateReactContextInBackground"/> has
        /// been called. Will return <code>false</code> after 
        /// <see cref="IAsyncDisposable.DisposeAsync"/>  until a new initial
        /// context has been created.
        /// </summary>
        public bool HasStartedCreatingInitialContext
        {
            get
            {
                return _hasStartedCreatingInitialContext;
            }
        }

        /// <summary>
        /// The URL where the last bundle was loaded from.
        /// </summary>
        public string SourceUrl
        {
            get
            {
                return _sourceUrl;
            }
        }

        /// <summary>
        /// Gets the current React context instance.
        /// </summary>
        public ReactContext CurrentReactContext
        {
            get
            {
                return _currentReactContext;
            }
        }

        /// <summary>
        /// Trigger the React context initialization asynchronously in a
        /// background task. This enables applications to pre-load the
        /// application JavaScript, and execute global core code before the
        /// <see cref="ReactRootView"/> is available and measure. This should
        /// only be called the first time the application is set up, which is
        /// enforced to keep developers from accidentally creating their
        /// applications multiple times.
        /// </summary>
        public async void CreateReactContextInBackground()
        {
            await CreateReactContextInBackgroundAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Trigger the React context initialization asynchronously in a
        /// background task. This enables applications to pre-load the
        /// application JavaScript, and execute global core code before the
        /// <see cref="ReactRootView"/> is available and measure. This should
        /// only be called the first time the application is set up, which is
        /// enforced to keep developers from accidentally creating their
        /// applications multiple times.
        /// </summary>
        /// <returns>A task to await the result.</returns>
        internal async Task CreateReactContextInBackgroundAsync()
        {
            if (_hasStartedCreatingInitialContext)
            {
                throw new InvalidOperationException(
                    "React context creation should only be called when creating the React " +
                    "application for the first time. When reloading JavaScript, e.g., from " +
                    "a new file, explicitly, use the re-create method.");
            }

            _hasStartedCreatingInitialContext = true;
            await RecreateReactContextInBackgroundInnerAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Recreate the React application and context. This should be called
        /// if configuration has changed or the developer has requested the
        /// application to be reloaded.
        /// </summary>
        public async void RecreateReactContextInBackground()
        {
            await RecreateReactContextInBackgroundAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Recreate the React application and context. This should be called
        /// if configuration has changed or the developer has requested the
        /// application to be reloaded.
        /// </summary>
        /// <returns>A task to await the result.</returns>
        internal async Task RecreateReactContextInBackgroundAsync()
        {
            if (!_hasStartedCreatingInitialContext)
            {
                throw new InvalidOperationException(
                    "React context re-creation should only be called after the initial " +
                    "create context background call.");
            }

            await RecreateReactContextInBackgroundInnerAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Method that gives JavaScript the opportunity to consume the back
        /// button event. If JavaScript does not consume the event, the
        /// default back press action will be invoked at the end of the
        /// roundtrip to JavaScript.
        /// </summary>
        public void OnBackPressed()
        {
            DispatcherHelpers.AssertOnDispatcher();
            var reactContext = _currentReactContext;
            if (reactContext == null)
            {
                Tracer.Write(ReactConstants.Tag, "Instance detached from instance manager.");
                InvokeDefaultOnBackPressed();
            }
            else
            {
                reactContext.GetNativeModule<DeviceEventManagerModule>().EmitHardwareBackPressed();
            }
        }

        /// <summary>
        /// Called when the application is suspended.
        /// </summary>
        public void OnSuspend()
        {
            DispatcherHelpers.AssertOnDispatcher();

            _lifecycleState = LifecycleState.BeforeResume;
            _defaultBackButtonHandler = null;

            if (_useDeveloperSupport)
            {
                _devSupportManager.IsEnabled = false;
            }

            var currentReactContext = _currentReactContext;
            if (currentReactContext != null)
            {
                _currentReactContext.OnSuspend();
            }
        }

        /// <summary>
        /// Used when the application resumes to reset the back button handling
        /// in JavaScript.
        /// </summary>
        /// <param name="onBackPressed">
        /// The action to take when back is pressed.
        /// </param>
        public void OnResume(Action onBackPressed)
        {
            if (onBackPressed == null)
                throw new ArgumentNullException(nameof(onBackPressed));

            DispatcherHelpers.AssertOnDispatcher();

            _lifecycleState = LifecycleState.Resumed;

            _defaultBackButtonHandler = onBackPressed;

            if (_useDeveloperSupport)
            {
                _devSupportManager.IsEnabled = true;
            }

            var currentReactContext = _currentReactContext;
            if (currentReactContext != null)
            {
                currentReactContext.OnResume();
            }
        }

        /// <summary>
        /// Destroy the <see cref="IReactInstanceManager"/>.
        /// </summary>
        public async Task DisposeAsync()
        {
            DispatcherHelpers.AssertOnDispatcher();

            // TODO: memory pressure hooks
            if (_useDeveloperSupport)
            {
                _devSupportManager.IsEnabled = false;
            }

            var currentReactContext = _currentReactContext;
            if (currentReactContext != null)
            {
                await currentReactContext.DisposeAsync().ConfigureAwait(false);
                _currentReactContext = null;
                _hasStartedCreatingInitialContext = false;
            }
        }

        /// <summary>
        /// Attach given <paramref name="rootView"/> to a React instance
        /// manager and start the JavaScript application using the JavaScript
        /// module provided by the <see cref="ReactRootView.JavaScriptModuleName"/>. If
        /// the React context is currently being (re-)created, or if the react
        /// context has not been created yet, the JavaScript application
        /// associated with the provided root view will be started
        /// asynchronously. This view will then be tracked by this manager and
        /// in case of React instance restart, it will be re-attached.
        /// </summary>
        /// <param name="rootView">The root view.</param>
        public void AttachMeasuredRootView(ReactRootView rootView)
        {
            if (rootView == null)
                throw new ArgumentNullException(nameof(rootView));

            DispatcherHelpers.AssertOnDispatcher();

            _attachedRootViews.Add(rootView);

            // If the React context is being created in the background, the
            // JavaScript application will be started automatically when
            // creation completes, as root view is part of the attached root
            // view list.
            var currentReactContext = _currentReactContext;
            if (_contextInitializationTask == null && currentReactContext != null)
            {
                AttachMeasuredRootViewToInstance(rootView, currentReactContext.ReactInstance);
            }
        }

        /// <summary>
        /// Detach given <paramref name="rootView"/> from the current react
        /// instance. This method is idempotent and can be called multiple
        /// times on the same <see cref="ReactRootView"/> instance.
        /// </summary>
        /// <param name="rootView">The root view.</param>
        public void DetachRootView(ReactRootView rootView)
        {
            if (rootView == null)
                throw new ArgumentNullException(nameof(rootView));

            DispatcherHelpers.AssertOnDispatcher();

            if (_attachedRootViews.Remove(rootView))
            {
                var currentReactContext = _currentReactContext;
                if (currentReactContext != null && currentReactContext.HasActiveReactInstance)
                {
                    DetachViewFromInstance(rootView, currentReactContext.ReactInstance);
                }
            }
        }

        /// <summary>
        /// Uses the configured <see cref="IReactPackage"/> instances to create
        /// all <see cref="IViewManager"/> instances.
        /// </summary>
        /// <param name="reactContext">
        /// The application context.
        /// </param>
        /// <returns>The list of view managers.</returns>
        public IReadOnlyList<IViewManager> CreateAllViewManagers(ReactContext reactContext)
        {
            if (reactContext == null)
                throw new ArgumentNullException(nameof(reactContext));

            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "createAllViewManagers").Start())
            {
                var allViewManagers = new List<IViewManager>();
                foreach (var package in _packages)
                {
                    allViewManagers.AddRange(
                        package.CreateViewManagers(reactContext));
                }

                return allViewManagers;
            }
        }

        private async Task RecreateReactContextInBackgroundInnerAsync()
        {
            DispatcherHelpers.AssertOnDispatcher();

            if (_useDeveloperSupport && _jsBundleFile == null && _jsMainModuleName != null)
            {
                if (await _devSupportManager.HasUpToDateBundleInCacheAsync())
                {
                    OnJavaScriptBundleLoadedFromServer();
                }
                else
                {
                    _devSupportManager.HandleReloadJavaScript();
                }
            }
            else
            {
                RecreateReactContextInBackgroundFromBundleFile();
            }
        }

        private void RecreateReactContextInBackgroundFromBundleFile()
        {
            RecreateReactContextInBackground(
                () => new ChakraJavaScriptExecutor(),
                JavaScriptBundleLoader.CreateFileLoader(_jsBundleFile));
        }

        private void InvokeDefaultOnBackPressed()
        {
            DispatcherHelpers.AssertOnDispatcher();
            _defaultBackButtonHandler?.Invoke();
        }

        private void OnReloadWithJavaScriptDebugger(Func<IJavaScriptExecutor> javaScriptExecutorFactory)
        {
            RecreateReactContextInBackground(
                javaScriptExecutorFactory,
                JavaScriptBundleLoader.CreateRemoteDebuggerLoader(
                    _devSupportManager.JavaScriptBundleUrlForRemoteDebugging,
                    _devSupportManager.SourceUrl));
        }

        private void OnJavaScriptBundleLoadedFromServer()
        {
            RecreateReactContextInBackground(
                () => new ChakraJavaScriptExecutor(),
                JavaScriptBundleLoader.CreateCachedBundleFromNetworkLoader(
                    _devSupportManager.SourceUrl,
                    _devSupportManager.DownloadedJavaScriptBundleFile));
        }

        private void RecreateReactContextInBackground(
            Func<IJavaScriptExecutor> jsExecutorFactory,
            JavaScriptBundleLoader jsBundleLoader)
        {
            if (_contextInitializationTask == null)
            {
                _contextInitializationTask = InitializeReactContextAsync(jsExecutorFactory, jsBundleLoader);
            }
            else
            {
                _pendingJsExecutorFactory = jsExecutorFactory;
                _pendingJsBundleLoader = jsBundleLoader;
            }
        }

        private async Task InitializeReactContextAsync(
            Func<IJavaScriptExecutor> jsExecutorFactory,
            JavaScriptBundleLoader jsBundleLoader)
        {
            var currentReactContext = _currentReactContext;
            if (currentReactContext != null)
            {
                await TearDownReactContextAsync(currentReactContext);
                _currentReactContext = null;
            }

            try
            {
                var reactContext = await CreateReactContextAsync(jsExecutorFactory, jsBundleLoader);
                SetupReactContext(reactContext);
            }
            catch (Exception ex)
            {
                _devSupportManager.HandleException(ex);
            }
            finally
            {
                _contextInitializationTask = null;
            }

            if (_pendingJsExecutorFactory != null)
            {
                var pendingJsExecutorFactory = _pendingJsExecutorFactory;
                var pendingJsBundleLoader = _pendingJsBundleLoader;

                _pendingJsExecutorFactory = null;
                _pendingJsBundleLoader = null;

                RecreateReactContextInBackground(
                    pendingJsExecutorFactory,
                    pendingJsBundleLoader);
            }
        }

        private void SetupReactContext(ReactContext reactContext)
        {
            DispatcherHelpers.AssertOnDispatcher();
            if (_currentReactContext != null)
            {
                throw new InvalidOperationException(
                    "React context has already been setup and has not been destroyed.");
            }

            _currentReactContext = reactContext;
            var reactInstance = reactContext.ReactInstance;
            _devSupportManager.OnNewReactContextCreated(reactContext);
            // TODO: set up memory pressure hooks
            MoveReactContextToCurrentLifecycleState(reactContext);

            foreach (var rootView in _attachedRootViews)
            {
                AttachMeasuredRootViewToInstance(rootView, reactInstance);
            }

            OnReactContextInitialized(reactContext);
        }

        private void AttachMeasuredRootViewToInstance(
            ReactRootView rootView,
            IReactInstance reactInstance)
        {
            DispatcherHelpers.AssertOnDispatcher();

            // Reset view content as it's going to be populated by the
            // application content from JavaScript
            rootView.TouchHandler?.Dispose();
            rootView.Children.Clear();
            rootView.Tag = null;

            var uiManagerModule = reactInstance.GetNativeModule<UIManagerModule>();
            var rootTag = uiManagerModule.AddMeasuredRootView(rootView);
            rootView.TouchHandler = new TouchHandler(rootView);

            var jsAppModuleName = rootView.JavaScriptModuleName;
            var appParameters = new Dictionary<string, object>
            {
                { "rootTag", rootTag },
                { "initalProps", null /* TODO: add launch options to root view */ }
            };

            reactInstance.GetJavaScriptModule<AppRegistry>().runApplication(jsAppModuleName, appParameters);
        }

        private void DetachViewFromInstance(ReactRootView rootView, IReactInstance reactInstance)
        {
            DispatcherHelpers.AssertOnDispatcher();
            reactInstance.GetJavaScriptModule<AppRegistry>().unmountApplicationComponentAtRootTag(rootView.GetTag());
        }

        private async Task TearDownReactContextAsync(ReactContext reactContext)
        {
            DispatcherHelpers.AssertOnDispatcher();

            if (_lifecycleState == LifecycleState.Resumed)
            {
                reactContext.OnSuspend();
            }

            foreach (var rootView in _attachedRootViews)
            {
                DetachViewFromInstance(rootView, reactContext.ReactInstance);
            }

            await reactContext.DisposeAsync();
            _devSupportManager.OnReactContextDestroyed(reactContext);
            // TODO: add memory pressure hooks
        }

        private async Task<ReactContext> CreateReactContextAsync(
            Func<IJavaScriptExecutor> jsExecutorFactory,
            JavaScriptBundleLoader jsBundleLoader)
        {
            Tracer.Write(ReactConstants.Tag, "Creating React context.");

            _sourceUrl = jsBundleLoader.SourceUrl;

            var nativeRegistryBuilder = new NativeModuleRegistry.Builder();
            var jsModulesBuilder = new JavaScriptModuleRegistry.Builder();

            var reactContext = new ReactContext();
            if (_useDeveloperSupport)
            {
                reactContext.NativeModuleCallExceptionHandler = _devSupportManager.HandleException;
            }

            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "createAndProcessCoreModulesPackage").Start())
            {
                var coreModulesPackage =
                    new CoreModulesPackage(this, InvokeDefaultOnBackPressed, _uiImplementationProvider);

                ProcessPackage(coreModulesPackage, reactContext, nativeRegistryBuilder, jsModulesBuilder);
            }

            foreach (var reactPackage in _packages)
            {
                using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "createAndProcessCustomReactPackage").Start())
                {
                    ProcessPackage(reactPackage, reactContext, nativeRegistryBuilder, jsModulesBuilder);
                }
            }

            var nativeModuleRegistry = default(NativeModuleRegistry);
            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "buildNativeModuleRegistry").Start())
            {
                nativeModuleRegistry = nativeRegistryBuilder.Build();
            }

            var exceptionHandler = _nativeModuleCallExceptionHandler ?? _devSupportManager.HandleException;
            var reactInstanceBuilder = new ReactInstance.Builder
            {
                QueueConfigurationSpec = ReactQueueConfigurationSpec.Default,
                JavaScriptExecutorFactory = jsExecutorFactory,
                Registry = nativeModuleRegistry,
                JavaScriptModuleRegistry = jsModulesBuilder.Build(),
                BundleLoader = jsBundleLoader,
                NativeModuleCallExceptionHandler = exceptionHandler,
            };

            var reactInstance = default(ReactInstance);
            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "createReactInstance").Start())
            {
                reactInstance = reactInstanceBuilder.Build();
            }

            // TODO: add bridge idle debug listener

            reactContext.InitializeWithInstance(reactInstance);

            reactInstance.Initialize();

            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "RunJavaScriptBundle").Start())
            {
                await reactInstance.InitializeBridgeAsync().ConfigureAwait(false);
            }

            return reactContext;
        }

        private void ProcessPackage(
            IReactPackage reactPackage,
            ReactContext reactContext,
            NativeModuleRegistry.Builder nativeRegistryBuilder,
            JavaScriptModuleRegistry.Builder jsModulesBuilder)
        {
            foreach (var nativeModule in reactPackage.CreateNativeModules(reactContext))
            {
                nativeRegistryBuilder.Add(nativeModule);
            }

            foreach (var type in reactPackage.CreateJavaScriptModulesConfig())
            {
                jsModulesBuilder.Add(type);
            }
        }

        private void MoveReactContextToCurrentLifecycleState(ReactContext reactContext)
        {
            if (_lifecycleState == LifecycleState.Resumed)
            {
                reactContext.OnResume();
            }
        }

        private void OnReactContextInitialized(ReactContext reactContext)
        {
            ReactContextInitialized?
                .Invoke(this, new ReactContextInitializedEventArgs(reactContext));
        }

        private void ToggleElementInspector()
        {
            _currentReactContext?
                .GetJavaScriptModule<RCTDeviceEventEmitter>()
                .emit("toggleElementInspector", null);
        }
        
        /// <summary>
        /// A Builder responsible for creating a React Instance Manager.
        /// </summary>
        public sealed class Builder
        {
            private List<IReactPackage> _packages = new List<IReactPackage>();

            private bool _useDeveloperSupport;
            private string _jsBundleFile;
            private string _jsMainModuleName;
            private LifecycleState? _initialLifecycleState;
            private UIImplementationProvider _uiImplementationProvider;
            private Action<Exception> _nativeModuleCallExceptionHandler;

            /// <summary>
            /// A provider of <see cref="UIImplementation" />.
            /// </summary>
            public UIImplementationProvider UIImplementationProvider
            {
                set
                {
                    _uiImplementationProvider = value;
                }
            }

            /// <summary>
            /// Path to the JavaScript bundle file to be loaded from the file
            /// system.
            /// </summary>
            public string JavaScriptBundleFile
            {
                set
                {
                    _jsBundleFile = value;
                }
            }

            /// <summary>
            /// Path to the applications main module on the packager server.
            /// </summary>
            public string JavaScriptMainModuleName
            {
                set
                {
                    _jsMainModuleName = value;
                }
            }

            /// <summary>
            /// The mutable list of React packages.
            /// </summary>
            public List<IReactPackage> Packages
            {
                get
                {
                    return _packages;
                }
            }

            /// <summary>
            /// Signals whether the application should enable developer support.
            /// </summary>
            public bool UseDeveloperSupport
            {
                set
                {
                    _useDeveloperSupport = value;
                }
            }

            /// <summary>
            /// The initial lifecycle state of the host.
            /// </summary>
            public LifecycleState InitialLifecycleState
            {
                set
                {
                    _initialLifecycleState = value;
                }
            }

            /// <summary>
            /// The exception handler for all native module calls.
            /// </summary>
            public Action<Exception> NativeModuleCallExceptionHandler
            {
                set
                {
                    _nativeModuleCallExceptionHandler = value;
                }
            }

            /// <summary>
            /// Instantiates a new <see cref="ReactInstanceManager"/>.
            /// </summary>
            /// <returns>A React instance manager.</returns>
            public ReactInstanceManager Build()
            {
                AssertNotNull(_initialLifecycleState, nameof(InitialLifecycleState));

                if (!_useDeveloperSupport && _jsBundleFile == null)
                {
                    throw new InvalidOperationException("JavaScript bundle file has to be provided when dev support is disabled.");
                }

                if (_jsBundleFile == null && _jsMainModuleName == null)
                {
                    throw new InvalidOperationException("Either the main module name or the JavaScript bundle file must be provided.");
                }

                if (_uiImplementationProvider == null)
                {
                    _uiImplementationProvider = new UIImplementationProvider();
                }

                return new ReactInstanceManager(
                    _jsBundleFile,
                    _jsMainModuleName,
                    _packages,
                    _useDeveloperSupport,
                    _initialLifecycleState.Value,
                    _uiImplementationProvider,
                    _nativeModuleCallExceptionHandler);
            }

            private void AssertNotNull(object value, string name)
            {
                if (value == null)
                    throw new InvalidOperationException(Invariant($"'{name}' has not been set."));
            }
        }

        class ReactInstanceDevCommandsHandler : IReactInstanceDevCommandsHandler
        {
            private readonly ReactInstanceManager _parent;

            public ReactInstanceDevCommandsHandler(ReactInstanceManager parent)
            {
                _parent = parent;
            }

            public void OnBundleFileReloadRequest()
            {
                _parent.RecreateReactContextInBackground();
            }

            public void OnJavaScriptBundleLoadedFromServer()
            {
                _parent.OnJavaScriptBundleLoadedFromServer();
            }

            public void OnReloadWithJavaScriptDebugger(Func<IJavaScriptExecutor> javaScriptExecutorFactory)
            {
                _parent.OnReloadWithJavaScriptDebugger(javaScriptExecutorFactory);
            }

            public void ToggleElementInspector()
            {
                _parent.ToggleElementInspector();
            }
            
        }
    }
}
