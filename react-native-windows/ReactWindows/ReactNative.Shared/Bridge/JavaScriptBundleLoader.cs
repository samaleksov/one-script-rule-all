﻿using PCLStorage;
using System;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage;
#else
using System.IO;
using System.Reflection;
#endif
using static System.FormattableString;

namespace ReactNative.Bridge
{
    /// <summary>
    /// A class that stores JavaScript bundle information and allows the
    /// <see cref="IReactInstance"/> to load a correct bundle through the
    /// <see cref="IReactBridge"/>.
    /// </summary>
    public abstract class JavaScriptBundleLoader
    {
        /// <summary>
        /// The source URL of the bundle.
        /// </summary>
        public abstract string SourceUrl { get; }

        /// <summary>
        /// Initializes the JavaScript bundle loader, typically making an
        /// asynchronous call to cache the bundle in memory.
        /// </summary>
        /// <returns>A task to await initialization.</returns>
        public abstract Task InitializeAsync();

        /// <summary>
        /// Loads the bundle into a JavaScript executor.
        /// </summary>
        /// <param name="executor">The JavaScript executor.</param>
        public abstract void LoadScript(IReactBridge executor);

        /// <summary>
        /// This loader will read the file from the project directory.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The JavaScript bundle loader.</returns>
        public static JavaScriptBundleLoader CreateFileLoader(string fileName)
        {
            return new FileJavaScriptBundleLoader(fileName);
        }

        /// <summary>
        /// This loader will use the cached bundle from the
        /// <see cref="DevSupport.IDevSupportManager"/>.
        /// </summary>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="cachedFileLocation">The cached bundle.</param>
        /// <returns>The JavaScript bundle loader.</returns>
        public static JavaScriptBundleLoader CreateCachedBundleFromNetworkLoader(string sourceUrl, string cachedFileLocation)
        {
            return new CachedJavaScriptBundleLoader(sourceUrl, cachedFileLocation);
        }

        /// <summary>
        /// This loader will trigger a remote debugger to load JavaScript from
        /// the given <paramref name="proxySourceUrl"/>.
        /// </summary>
        /// <param name="proxySourceUrl">
        /// The URL to load the JavaScript bundle from.
        /// </param>
        /// <param name="realSourceUrl">
        /// The URL to report as the source URL, e.g., for asset loading.
        /// </param>
        /// <returns>The JavaScript bundle loader.</returns>
        public static JavaScriptBundleLoader CreateRemoteDebuggerLoader(string proxySourceUrl, string realSourceUrl)
        {
            return new RemoteDebuggerJavaScriptBundleLoader(proxySourceUrl, realSourceUrl);
        }

        class FileJavaScriptBundleLoader : JavaScriptBundleLoader
        {
            private string _script;

            public FileJavaScriptBundleLoader(string fileName)
            {
                SourceUrl = fileName;
            }

            public override string SourceUrl
            {
                get;
            }

#if WINDOWS_UWP
            public override async Task InitializeAsync()
            {
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(SourceUrl)).AsTask().ConfigureAwait(false);
                _script = storageFile.Path;
            }
#else
            public override Task InitializeAsync()
            {
                var assembly = Assembly.GetAssembly(typeof(JavaScriptBundleLoader));
                var assemblyName = assembly.GetName();
                var pathToAssembly = Path.GetDirectoryName(assemblyName.CodeBase);
                var pathToAssemblyResource = Path.Combine(pathToAssembly, SourceUrl.Replace("ms-appx:///", String.Empty));
                var u = new Uri(pathToAssemblyResource);
                _script = u.LocalPath;
                return Task.CompletedTask;
            }
#endif

            public override void LoadScript(IReactBridge bridge)
            {
                if (bridge == null)
                    throw new ArgumentNullException(nameof(bridge));

                if (_script == null)
                {
                    throw new InvalidOperationException("Bundle loader has not yet been initialized.");
                }

                bridge.RunScript(_script, SourceUrl);
            }
        }

        class CachedJavaScriptBundleLoader : JavaScriptBundleLoader
        {
            private readonly string _cachedFileLocation;
            private string _script;

            public CachedJavaScriptBundleLoader(string sourceUrl, string cachedFileLocation)
            {
                SourceUrl = sourceUrl;
                _cachedFileLocation = cachedFileLocation;
            }

            public override string SourceUrl { get; }

            public override async Task InitializeAsync()
            {
                var localFolder = FileSystem.Current.LocalStorage;
                var storageFile = await localFolder.GetFileAsync(_cachedFileLocation).ConfigureAwait(false);
                _script = storageFile.Path;
            }

            public override void LoadScript(IReactBridge executor)
            {
                if (executor == null)
                    throw new ArgumentNullException(nameof(executor));

                executor.RunScript(_script, SourceUrl);
            }
        }

        class RemoteDebuggerJavaScriptBundleLoader : JavaScriptBundleLoader
        {
            private readonly string _proxySourceUrl;

            public RemoteDebuggerJavaScriptBundleLoader(string proxySourceUrl, string realSourceUrl)
            {
                _proxySourceUrl = proxySourceUrl;
                SourceUrl = realSourceUrl;
            }

            public override string SourceUrl
            {
                get;
            }

            public override Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            public override void LoadScript(IReactBridge executor)
            {
                if (executor == null)
                    throw new ArgumentNullException(nameof(executor));

                executor.RunScript(_proxySourceUrl, SourceUrl);
            }
        }
    }
}
