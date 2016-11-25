﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.Common;
using ReactNative.Modules.Core;
using ReactNative.Modules.DevSupport;
using ReactNative.Tracing;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage;
#else
using PCLStorage;
using System.Reflection;
#endif

namespace ReactNative.DevSupport
{
    class DevSupportManager : IDevSupportManager, IDisposable
    {
        private const int NativeErrorCookie = -1;
        private const string JSBundleFileName = "ReactNativeDevBundle.js";

#if WINDOWS_UWP
        private readonly ShakeAccelerometer _accelerometer = ShakeAccelerometer.Instance;
        private bool _isShakeDetectorRegistered;
#endif

        private readonly SerialDisposable _pollingDisposable = new SerialDisposable();

        private readonly IReactInstanceDevCommandsHandler _reactInstanceCommandsHandler;
        private readonly string _jsBundleFile;
        private readonly string _jsAppBundleName;
        private readonly DevInternalSettings _devSettings;
        private readonly DevServerHelper _devServerHelper;

        private bool _isDevSupportEnabled = true;

        private ReactContext _currentContext;
        private RedBoxDialog _redBoxDialog;
        private Action _dismissRedBoxDialog;
        private bool _redBoxDialogOpen;
        private DevOptionDialog _devOptionsDialog;
        private Action _dismissDevOptionsDialog;
        private bool _devOptionsDialogOpen;

        public DevSupportManager(
            IReactInstanceDevCommandsHandler reactInstanceCommandsHandler,
            string jsBundleFile,
            string jsAppBundleName)
        {
            _reactInstanceCommandsHandler = reactInstanceCommandsHandler;
            _jsBundleFile = jsBundleFile;
            _jsAppBundleName = jsAppBundleName;
            _devSettings = new DevInternalSettings(this);
            _devServerHelper = new DevServerHelper(_devSettings);
            ReloadSettings();
        }

        public IDeveloperSettings DevSettings
        {
            get
            {
                return _devSettings;
            }
        }

        public string DownloadedJavaScriptBundleFile
        {
            get
            {
                return JSBundleFileName;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _isDevSupportEnabled;
            }
            set
            {
                if (value != _isDevSupportEnabled)
                {
                    _isDevSupportEnabled = value;
                    ReloadSettings();
                }
            }
        }

        public bool IsRemoteDebuggingEnabled
        {
            get;
            set;
        }

        public string SourceMapUrl
        {
            get
            {
                if (_jsAppBundleName == null)
                {
                    return "";
                }

                return _devServerHelper.GetSourceMapUrl(_jsAppBundleName);
            }
        }

        public string SourceUrl
        {
            get
            {
                if (_jsAppBundleName == null)
                {
                    return "";
                }

                return _devServerHelper.GetSourceUrl(_jsAppBundleName);
            }
        }

        public string JavaScriptBundleUrlForRemoteDebugging
        {
            get
            {
                return _devServerHelper.GetJavaScriptBundleUrlForRemoteDebugging(_jsAppBundleName);
            }
        }

        public void HandleException(Exception exception)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif

            if (IsEnabled)
            {
                ShowNewNativeError(exception.Message, exception);
            }
            else
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        public async Task<bool> HasUpToDateBundleInCacheAsync()
        {
            if (_isDevSupportEnabled)
            {
#if WINDOWS_UWP
                var lastUpdateTime = Windows.ApplicationModel.Package.Current.InstalledDate;
                var localFolder = ApplicationData.Current.LocalFolder;
                var bundleItem = await localFolder.TryGetItemAsync(JSBundleFileName);
                if (bundleItem != null)
                {
                    var bundleProperties = await bundleItem.GetBasicPropertiesAsync();
                    return bundleProperties.DateModified > lastUpdateTime;
                }
#else
                var lastUpdateTime = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
                var localFolder = FileSystem.Current.LocalStorage;
                if (await localFolder.CheckExistsAsync(JSBundleFileName) == ExistenceCheckResult.FileExists)
                {
                    return File.GetLastWriteTime(JSBundleFileName) > lastUpdateTime;
                }
#endif
            }

            return false;
        }

        public void ShowNewNativeError(string message, Exception exception)
        {
            var javaScriptException = exception as JavaScriptException;
            if (javaScriptException != null && javaScriptException.JavaScriptStackTrace != null)
            {
                var stackTrace = StackTraceHelper.ConvertChakraStackTrace(javaScriptException.JavaScriptStackTrace);
                ShowNewError(exception.Message, stackTrace, NativeErrorCookie);
            }
            else
            {
                Tracer.Error(ReactConstants.Tag, "Exception in native call from JavaScript.", exception);
                ShowNewError(message, StackTraceHelper.ConvertNativeStackTrace(exception), NativeErrorCookie);
            }
        }

        public void ShowNewJavaScriptError(string title, JArray details, int errorCookie)
        {
            ShowNewError(title, StackTraceHelper.ConvertJavaScriptStackTrace(details), errorCookie);
        }

        public void UpdateJavaScriptError(string message, JArray details, int errorCookie)
        {
            DispatcherHelpers.RunOnDispatcher(() =>
            {
                if (_redBoxDialog == null
                    || !_redBoxDialogOpen
                    || errorCookie != _redBoxDialog.ErrorCookie)
                {
                    return;
                }

                _redBoxDialog.Message = message;
                _redBoxDialog.StackTrace = StackTraceHelper.ConvertJavaScriptStackTrace(details);
            });
        }

        public void HideRedboxDialog()
        {
            var dismissRedBoxDialog = _dismissRedBoxDialog;
            if (_redBoxDialogOpen && dismissRedBoxDialog != null)
            {
                dismissRedBoxDialog();
            }
        }

        public void ShowDevOptionsDialog()
        {
            if (_devOptionsDialog != null || !IsEnabled)
            {
                return;
            }

            DispatcherHelpers.RunOnDispatcher(() =>
            {
                var options = new[]
                {
                    new DevOptionHandler(
                        "Reload JavaScript",
                        HandleReloadJavaScript),
                    new DevOptionHandler(
                        IsRemoteDebuggingEnabled
                            ? "Stop JS Remote Debugging"
                            : "Start JS Remote Debugging",
                        () =>
                        {
                            IsRemoteDebuggingEnabled = !IsRemoteDebuggingEnabled;
                            HandleReloadJavaScript();
                        }),
                    new DevOptionHandler(
                        _devSettings.IsHotModuleReplacementEnabled
                            ? "Disable Hot Reloading"
                            : "Enable Hot Reloading",
                        () =>
                        {
                            _devSettings.IsHotModuleReplacementEnabled = !_devSettings.IsHotModuleReplacementEnabled;
                            HandleReloadJavaScript();
                        }),
                    new DevOptionHandler(
                        _devSettings.IsReloadOnJavaScriptChangeEnabled
                            ? "Disable Live Reload"
                            : "Enable Live Reload",
                        () =>
                            _devSettings.IsReloadOnJavaScriptChangeEnabled =
                                !_devSettings.IsReloadOnJavaScriptChangeEnabled),
                    new DevOptionHandler(
                        _devSettings.IsElementInspectorEnabled
                            ? "Hide Inspector"
                            : "Show Inspector",
                        () =>
                        {
                            _devSettings.IsElementInspectorEnabled = !_devSettings.IsElementInspectorEnabled;
                            _reactInstanceCommandsHandler.ToggleElementInspector();
                        }),
                    new DevOptionHandler("Packager Settings",
                        () =>
                        {
                            var dialog = new PackagerSettingsDialog(this._devSettings);

                            // The freakin thing can only run one dialog at a time
                            Task.Factory.StartNew(async () => {
                                await Task.Delay(2000);
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                {
                                    await dialog.ShowAsync();
                                });
                            });
                        }),
                };

                _devOptionsDialogOpen = true;
                _devOptionsDialog = new DevOptionDialog();
                _devOptionsDialog.Closed += (_, __) =>
                {
                    _devOptionsDialogOpen = false;
                    _dismissDevOptionsDialog = null;
                    _devOptionsDialog = null;
                };

                foreach (var option in options)
                {
                    _devOptionsDialog.Add(option.Name, option.OnSelect);
                }

                if (_redBoxDialog != null)
                {
                    _dismissRedBoxDialog();
                }

#if WINDOWS_UWP
                var asyncInfo = _devOptionsDialog.ShowAsync();
                _dismissDevOptionsDialog = asyncInfo.Cancel;

                foreach (var option in options)
                {
                    option.HideDialog = _dismissDevOptionsDialog;
                }
#else
                var asyncInfo = _devOptionsDialog.ShowDialog();

                foreach (var option in options)
                {
                    option.HideDialog = _devOptionsDialog.Hide;
                }
#endif
            });
        }

        private void HideDevOptionsDialog()
        {
            var dismissDevOptionsDialog = _dismissDevOptionsDialog;
            if (_devOptionsDialogOpen && dismissDevOptionsDialog != null)
            {
                dismissDevOptionsDialog();
            }
        }

        public void OnNewReactContextCreated(ReactContext context)
        {
            ResetCurrentContext(context);
        }

        public void OnReactContextDestroyed(ReactContext context)
        {
            if (context == _currentContext)
            {
                ResetCurrentContext(null);
            }
        }

        public Task<bool> IsPackagerRunningAsync()
        {
            return _devServerHelper.IsPackagerRunningAsync();
        }

        public async void HandleReloadJavaScript()
        {
            DispatcherHelpers.AssertOnDispatcher();

            HideRedboxDialog();
            HideDevOptionsDialog();

            var message = !IsRemoteDebuggingEnabled
                ? "Fetching JavaScript bundle."
                : "Connecting to remote debugger.";

            var progressDialog = new ProgressDialog("Please wait...", message);
#if WINDOWS_UWP
            var dialogOperation = progressDialog.ShowAsync();
            Action cancel = dialogOperation.Cancel;
#else
            progressDialog.ShowDialog();
            Action cancel = progressDialog.Hide;
#endif
            if (IsRemoteDebuggingEnabled)
            {
                await ReloadJavaScriptInProxyMode(cancel, progressDialog.Token).ConfigureAwait(false);
            }
            else if (_jsBundleFile == null)
            {
                await ReloadJavaScriptFromServerAsync(cancel, progressDialog.Token).ConfigureAwait(false);
            }
            else
            {
                await ReloadJavaScriptFromFileAsync(progressDialog.Token);
                cancel();
            }
        }

        public void ReloadSettings()
        {
            if (_isDevSupportEnabled)
            {
#if WINDOWS_UWP
                RegisterDevOptionsMenuTriggers();
#endif
                if (_devSettings.IsReloadOnJavaScriptChangeEnabled)
                {
                    _pollingDisposable.Disposable =
                        _devServerHelper.StartPollingOnChangeEndpoint(HandleReloadJavaScript);
                }
                else
                {
                    // Disposes any existing poller
                    _pollingDisposable.Disposable = Disposable.Empty;
                }
            }
            else
            {
#if WINDOWS_UWP
                UnregisterDevOptionsMenuTriggers();
#endif

                if (_redBoxDialog != null)
                {
                    _dismissRedBoxDialog();
                }

                _pollingDisposable.Disposable = Disposable.Empty;
            }
        }

        public void Dispose()
        {
            _pollingDisposable.Dispose();
            _devServerHelper.Dispose();
        }

        private void ResetCurrentContext(ReactContext context)
        {
            if (_currentContext == context)
            {
                return;
            }

            _currentContext = context;

            if (_devSettings.IsHotModuleReplacementEnabled && context != null)
            {
                var uri = new Uri(SourceUrl);
                var path = uri.LocalPath.Substring(1); // strip initial slash in path
                var host = uri.Host;
                var port = uri.Port;
                context.GetJavaScriptModule<HMRClient>().enable("windows", path, host, port);
            }
        }

        private void ShowNewError(string message, IStackFrame[] stack, int errorCookie)
        {
            DispatcherHelpers.RunOnDispatcher(() =>
            {
                if (_redBoxDialog == null)
                {
                    _redBoxDialog = new RedBoxDialog(HandleReloadJavaScript);
                }

                if (_redBoxDialogOpen)
                {
                    return;
                }

                _redBoxDialogOpen = true;
                _redBoxDialog.ErrorCookie = errorCookie;
                _redBoxDialog.Message = message;
                _redBoxDialog.StackTrace = stack;
                _redBoxDialog.Closed += (_, __) =>
                {
                    _redBoxDialogOpen = false;
                    _dismissRedBoxDialog = null;
                    _redBoxDialog = null;
                };

#if WINDOWS_UWP
                var asyncInfo = _redBoxDialog.ShowAsync();
                _dismissRedBoxDialog = asyncInfo.Cancel;
#else
                var asyncInfo = _redBoxDialog.ShowDialog();
                _dismissRedBoxDialog = _redBoxDialog.Hide;
#endif
            });
        }

        private async Task ReloadJavaScriptInProxyMode(Action dismissProgress, CancellationToken token)
        {
            try
            {
                await _devServerHelper.LaunchDevToolsAsync(token).ConfigureAwait(true);
                var factory = new Func<IJavaScriptExecutor>(() =>
                {
                    var executor = new WebSocketJavaScriptExecutor();
                    executor.ConnectAsync(_devServerHelper.WebsocketProxyUrl, token).Wait();
                    return executor;
                });

                _reactInstanceCommandsHandler.OnReloadWithJavaScriptDebugger(factory);
                dismissProgress();
            }
            catch (DebugServerException ex)
            {
                dismissProgress();
                ShowNewNativeError(ex.Message, ex);
            }
            catch (Exception ex)
            {
                dismissProgress();
                ShowNewNativeError(
                    "Unable to download JS bundle. Did you forget to " +
                    "start the development server or connect your device?",
                    ex);
            }
        }

        private async Task ReloadJavaScriptFromServerAsync(Action dismissProgress, CancellationToken token)
        {
            var moved = false;
#if WINDOWS_UWP
            var temporaryFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(JSBundleFileName, CreationCollisionOption.GenerateUniqueName);
            try
            {
                using (var stream = await temporaryFile.OpenStreamForWriteAsync())
                {
                    await _devServerHelper.DownloadBundleFromUrlAsync(_jsAppBundleName, stream, token);
                }

                await temporaryFile.MoveAsync(ApplicationData.Current.LocalFolder, JSBundleFileName, NameCollisionOption.ReplaceExisting);
                moved = true;

                dismissProgress();
                _reactInstanceCommandsHandler.OnJavaScriptBundleLoadedFromServer();
            }
            catch (DebugServerException ex)
            {
                dismissProgress();
                ShowNewNativeError(ex.Message, ex);
            }
            catch (Exception ex)
            {
                dismissProgress();
                ShowNewNativeError(
                    "Unable to download JS bundle. Did you forget to " +
                    "start the development server or connect your device?",
                    ex);
            }
            finally
            {
                if (!moved)
                {
                    await temporaryFile.DeleteAsync();
                }
            }
#else
            var localStorage = FileSystem.Current.LocalStorage;
            var temporaryFolder = await localStorage.CreateFolderAsync("temp", CreationCollisionOption.GenerateUniqueName);
            var temporaryFile = await temporaryFolder.CreateFileAsync(JSBundleFileName, CreationCollisionOption.GenerateUniqueName);
            try
            {
                using (var stream = new MemoryStream())
                {
                    await _devServerHelper.DownloadBundleFromUrlAsync(_jsAppBundleName, stream, token);
                    await temporaryFile.WriteAllTextAsync(stream.ToString());
                }
                string newPath = PortablePath.Combine(localStorage.ToString(), JSBundleFileName);
                await temporaryFile.MoveAsync(newPath, NameCollisionOption.ReplaceExisting);
                moved = true;

                dismissProgress();
                _reactInstanceCommandsHandler.OnJavaScriptBundleLoadedFromServer();
            }
            catch (DebugServerException ex)
            {
                dismissProgress();
                ShowNewNativeError(ex.Message, ex);
            }
            catch (Exception ex)
            {
                dismissProgress();
                ShowNewNativeError(
                    "Unable to download JS bundle. Did you forget to " +
                    "start the development server or connect your device?",
                    ex);
            }
            finally
            {
                if (!moved)
                {
                    await temporaryFile.DeleteAsync();
                }
            }
#endif
        }

        private Task ReloadJavaScriptFromFileAsync(CancellationToken token)
        {
            _reactInstanceCommandsHandler.OnBundleFileReloadRequest();
            return Task.CompletedTask;
        }

#if WINDOWS_UWP
        private void RegisterDevOptionsMenuTriggers()
        {
            if (!_isShakeDetectorRegistered && _accelerometer != null)
            {
                _isShakeDetectorRegistered = true;
                _accelerometer.Shaken += OnAccelerometerShake;
            }
        }

        private void UnregisterDevOptionsMenuTriggers()
        {
            if (_isShakeDetectorRegistered && _accelerometer != null)
            {
                _accelerometer.Shaken -= OnAccelerometerShake;
                _isShakeDetectorRegistered = false;
            }
        }

        private void OnAccelerometerShake(object sender, EventArgs args)
        {
            ShowDevOptionsDialog();
        }
#endif

        class DevOptionHandler
        {
            private readonly Action _onSelect;

            public DevOptionHandler(string name, Action onSelect)
            {
                Name = name;
                _onSelect = onSelect;
            }

            public string Name { get; }

            public Action HideDialog { get; set; }

            public void OnSelect()
            {
                HideDialog?.Invoke();

                _onSelect();
            }
        }
    }
}
