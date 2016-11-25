﻿using ReactNative.Bridge;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using DataTransfer = Windows.ApplicationModel.DataTransfer;

namespace ReactNative.Modules.Clipboard
{
    /// <summary>
    /// A module that allows JS to get/set clipboard contents.
    /// </summary>
    class ClipboardModule : NativeModuleBase
    {
        private readonly IClipboardInstance _clipboard;

        public ClipboardModule() : this(new ClipboardInstance())
        {

        }

        public ClipboardModule(IClipboardInstance clipboard)
        {
            _clipboard = clipboard;
        }

        /// <summary>
        /// The name of the native module.
        /// </summary>
        public override string Name
        {
            get
            {
                return "Clipboard";
            }
        }

        /// <summary>
        /// Get the clipboard content through a promise.
        /// </summary>
        /// <param name="promise">The promise.</param>
        [ReactMethod]
        public void getString(IPromise promise)
        {
            if (promise == null)
            {
                throw new ArgumentNullException(nameof(promise));
            }

            RunOnDispatcher(async () =>
            {
                try
                {
                    var clip = _clipboard.GetContent();
                    if (clip == null)
                    {
                        promise.Resolve("");
                    }
                    else if (clip.Contains(DataTransfer.StandardDataFormats.Text))
                    {
                        var text = await clip.GetTextAsync().AsTask().ConfigureAwait(false);
                        promise.Resolve(text);
                    }
                    else
                    {
                        promise.Resolve("");
                    }
                }
                catch (Exception ex)
                {
                    promise.Reject(ex);
                }
            });
        }

        /// <summary>
        /// Add text to the clipboard or clear the clipboard.
        /// </summary>
        /// <param name="text">The text. If null clear clipboard.</param>
        [ReactMethod]
        public void setString(string text)
        {
            RunOnDispatcher(() =>
            {
                if (text == null)
                {
                    _clipboard.Clear();
                }
                else
                {
                    var package = new DataTransfer.DataPackage();
                    package.SetData(DataTransfer.StandardDataFormats.Text, text);
                    _clipboard.SetContent(package);
                }
            });
        }

        /// <summary>
        /// Run action on UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        private static async void RunOnDispatcher(DispatchedHandler action)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, action).AsTask().ConfigureAwait(false);
        }
    }
}
