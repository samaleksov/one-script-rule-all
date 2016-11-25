﻿using ReactNative.Bridge;
using ReactNative.DevSupport;
using ReactNative.Modules.Core;
using ReactNative.Modules.DevSupport;
using ReactNative.Tracing;
using ReactNative.UIManager;
using ReactNative.UIManager.Events;
using System;
using System.Collections.Generic;
#if !WINDOWS_UWP
using System.Windows;
#endif

namespace ReactNative
{
    /// <summary>
    /// Package defining core framework modules (e.g., <see cref="UIManagerModule"/>). 
    /// It should be used for modules that require special integration with
    /// other framework parts (e.g., with the list of packages to load view
    /// managers from).
    /// </summary>
    class CoreModulesPackage : IReactPackage
    {
        private readonly IReactInstanceManager _reactInstanceManager;
        private readonly Action _hardwareBackButtonHandler;
        private readonly UIImplementationProvider _uiImplementationProvider;

        public CoreModulesPackage(
            IReactInstanceManager reactInstanceManager,
            Action hardwareBackButtonHandler,
            UIImplementationProvider uiImplementationProvider)
        {
            _reactInstanceManager = reactInstanceManager;
            _hardwareBackButtonHandler = hardwareBackButtonHandler;
            _uiImplementationProvider = uiImplementationProvider;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller manages scope of returned list of disposables.")]
        public IReadOnlyList<INativeModule> CreateNativeModules(ReactContext reactContext)
        {
            var uiManagerModule = default(INativeModule);
            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "createUIManagerModule").Start())
            {
                var viewManagerList = _reactInstanceManager.CreateAllViewManagers(reactContext);
#if WINDOWS_UWP
                uiManagerModule = new UIManagerModule(
                    reactContext, 
                    viewManagerList,
                    _uiImplementationProvider.Create(
                        reactContext, 
                        viewManagerList));
#else
                uiManagerModule = new UIManagerModule(
                    reactContext,
                    viewManagerList,
                    _uiImplementationProvider.Create(
                        reactContext,
                        viewManagerList), 
                    Application.Current.MainWindow);
#endif
            }

            return new List<INativeModule>
            {
                //new AnimationsDebugModule(
                //    reactContext,
                //    _reactInstanceManager.DevSupportManager.DevSettings),
                //new SystemInfoModule(),
                new DeviceEventManagerModule(reactContext, _hardwareBackButtonHandler),
                new ExceptionsManagerModule(_reactInstanceManager.DevSupportManager),
                new Timing(reactContext),
                new SourceCodeModule(
                    _reactInstanceManager.SourceUrl,
                    _reactInstanceManager.DevSupportManager.SourceMapUrl),
                uiManagerModule,
                //new DebugComponentOwnershipModule(reactContext),
            };
        }

        public IReadOnlyList<Type> CreateJavaScriptModulesConfig()
        {
            return new List<Type>
            {
                typeof(RCTDeviceEventEmitter),
                typeof(JSTimersExecution),
                typeof(RCTEventEmitter),
                typeof(RCTNativeAppEventEmitter),
                typeof(AppRegistry),
                // TODO: some tracing module
                typeof(HMRClient),
                //typeof(RCTDebugComponentOwnership),
            };
        }

        public IReadOnlyList<IViewManager> CreateViewManagers(
            ReactContext reactContext)
        {
            return new List<IViewManager>(0);
        }
    }
}
