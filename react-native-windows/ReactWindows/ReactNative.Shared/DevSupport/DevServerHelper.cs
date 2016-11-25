﻿using ReactNative.Bridge;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.UI.Core;
#endif
using static System.FormattableString;

namespace ReactNative.DevSupport
{
    /// <summary>
    /// Helper class for debug server running in the host machine.
    /// </summary> 
    sealed class DevServerHelper : IDisposable
    {
        private const string DeviceLocalhost = "localhost:8081";
        private const string BundleUrlFormat = "http://{0}/{1}.bundle?platform=windows&dev={2}&hot={3}";
        private const string SourceMapUrlFormat = "http://{0}/{1}.map?platform=windows&dev={2}&hot={3}";
        private const string LaunchDevToolsCommandUrlFormat = "http://{0}/launch-js-devtools";
        private const string OnChangeEndpointUrlFormat = "http://{0}/onchange";
        private const string WebsocketProxyUrlFormat = "ws://{0}/debugger-proxy?role=client";
        private const string PackagerStatusUrlFormat = "http://{0}/status";
        private const string PackagerOkStatus = "packager-status:running";
        private const int LongPollFailureDelayMs = 5000;

        private readonly DevInternalSettings _settings;
        private readonly HttpClient _client;

        /// <summary>
        /// Instantiates the <see cref="DevServerHelper"/>.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public DevServerHelper(DevInternalSettings settings)
        {
            _settings = settings;
            _client = new HttpClient();
        }

        /// <summary>
        /// The JavaScript debugging proxy URL.
        /// </summary>
        public string WebSocketProxyUrl
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, WebSocketProxyUrl, DebugServerHost);
            }
        }

        /// <summary>
        /// The host to use when connecting to the bundle server.
        /// </summary>
        private string DebugServerHost
        {
            get
            {
                return _settings.DebugServerHost ?? DeviceLocalhost;
            }
        }

        /// <summary>
        /// Signals whether to enable dev mode when requesting JavaScript bundles.
        /// </summary>
        private bool IsJavaScriptDevModeEnabled
        {
            get
            {
                return _settings.IsJavaScriptDevModeEnabled;
            }
        }

        /// <summary>
        /// Signals whether hot module replacement is enabled.
        /// </summary>
        private bool IsHotModuleReplacementEnabled
        {
            get
            {
                return _settings.IsHotModuleReplacementEnabled;
            }
        }

        /// <summary>
        /// The Websocket proxy URL.
        /// </summary>
        public string WebsocketProxyUrl
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, WebsocketProxyUrlFormat, DebugServerHost);
            }
        }

        private string JavaScriptProxyHost
        {
            get
            {
                return DeviceLocalhost;
            }
        }

        /// <summary>
        /// Download the latest bundle into a local stream.
        /// </summary>
        /// <param name="jsModulePath">The module path.</param>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="token">A token to cancel the request.</param>
        /// <returns>A task to await completion.</returns>
        public async Task DownloadBundleFromUrlAsync(string jsModulePath, Stream outputStream, CancellationToken token)
        {
            var bundleUrl = GetSourceUrl(jsModulePath);
            using (var response = await _client.GetAsync(bundleUrl, token).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var exception = DebugServerException.Parse(body);
                    if (exception == null)
                    {
                        var nl = Environment.NewLine;
                        exception = new DebugServerException(
                            "The development server returned response error code: " +
                            Invariant($"{response.StatusCode}{nl}{nl}URL: {bundleUrl}{nl}{nl}Body:{nl}{body}"));
                    }

                    throw exception;
                }

                await response.Content.CopyToAsync(outputStream).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks if the packager is running.
        /// </summary>
        /// <returns>A task to await the packager status.</returns>
        public async Task<bool> IsPackagerRunningAsync()
        {
            var statusUrl = CreatePackagerStatusUrl(DebugServerHost);
            try
            {
                using (var response = await _client.GetAsync(statusUrl).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return body == PackagerOkStatus;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Start the bundle change polling service.
        /// </summary>
        /// <param name="onServerContentChanged">
        /// Callback for when the bundle content changes.
        /// </param>
        /// <returns>A disposable to use to stop polling.</returns>
        public IDisposable StartPollingOnChangeEndpoint(Action onServerContentChanged)
        {
            var disposable = new CancellationDisposable();

            var task = Task.Run(async () =>
            {
                var onChangePollingClient = new HttpClient();
                onChangePollingClient.DefaultRequestHeaders.Connection.Add("keep-alive");
                while (!disposable.IsDisposed)
                {
                    var onChangeUrl = CreateOnChangeEndpointUrl(DebugServerHost);
                    try
                    {
                        using (var response = await onChangePollingClient.GetAsync(onChangeUrl, disposable.Token).ConfigureAwait(false))
                        {
                            if (response.StatusCode == HttpStatusCode.ResetContent)
                            {
#if WINDOWS_UWP
                                DispatcherHelpers.RunOnDispatcher(new DispatchedHandler(onServerContentChanged));
#else
                                DispatcherHelpers.RunOnDispatcher(onServerContentChanged);
#endif
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    when (disposable.IsDisposed)
                    {
                    }
                    catch
                    {
                        await Task.Delay(LongPollFailureDelayMs).ConfigureAwait(false);
                    }
                }
            });

            return disposable;
        }

        /// <summary>
        /// Launch the developer tools.
        /// </summary>
        public async Task LaunchDevToolsAsync(CancellationToken token)
        {
            var url = CreateLaunchDevToolsCommandUrl();
            await _client.GetAsync(url, token).ConfigureAwait(false);
            // Ignore response, nothing to report here.
        }

        /// <summary>
        /// Get the source map URL for the JavaScript bundle.
        /// </summary>
        /// <param name="mainModuleName">The main module name.</param>
        /// <returns>The source map URL.</returns>
        public string GetSourceMapUrl(string mainModuleName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                SourceMapUrlFormat,
                DebugServerHost,
                mainModuleName,
                IsJavaScriptDevModeEnabled ? "true" : "false",
                IsHotModuleReplacementEnabled ? "true" : "false");
        }

        /// <summary>
        /// Get the source URL for the JavaScript bundle.
        /// </summary>
        /// <param name="mainModuleName">The main module name.</param>
        /// <returns>The source URL.</returns>
        public string GetSourceUrl(string mainModuleName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                BundleUrlFormat,
                DebugServerHost,
                mainModuleName,
                IsJavaScriptDevModeEnabled ? "true" : "false",
                IsHotModuleReplacementEnabled ? "true" : "false");
        }

        /// <summary>
        /// Gets the bundle URL for remote debugging.
        /// </summary>
        /// <param name="mainModuleName">The main module name.</param>
        /// <returns>The bundle URL.</returns>
        public string GetJavaScriptBundleUrlForRemoteDebugging(string mainModuleName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                BundleUrlFormat,
                JavaScriptProxyHost,
                mainModuleName,
                IsJavaScriptDevModeEnabled ? "true" : "false",
                IsHotModuleReplacementEnabled ? "true" : "false");
        }

        /// <summary>
        /// Disposes the <see cref="DevServerHelper"/>. 
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
        }

        private string CreatePackagerStatusUrl(string host)
        {
            return string.Format(CultureInfo.InvariantCulture, PackagerStatusUrlFormat, host);
        }

        private string CreateOnChangeEndpointUrl(string host)
        {
            return string.Format(CultureInfo.InvariantCulture, OnChangeEndpointUrlFormat, host);
        }

        private string CreateLaunchDevToolsCommandUrl()
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                LaunchDevToolsCommandUrlFormat, 
                DebugServerHost);
        }
    }
}
