﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if WINDOWS_UWP
using Windows.UI.Xaml.Media;
#else
using System.Windows.Media;
#endif

namespace ReactNative.UIManager
{
    /// <summary>
    /// This class acts as a buffer for command executed on
    /// <see cref="NativeViewHierarchyManager"/>. It exposes similar methods as
    /// mentioned classes but instead of executing commands immediately, it 
    /// enqueues those operations in a queue that is then flushed from 
    /// <see cref="UIManagerModule"/> once a JavaScript batch of UI operations
    /// is finished.
    /// </summary>
    public class UIViewOperationQueue
    {
        private const long TicksPerFrame = 166666;
        private const long MaxNonBatchedTicksPerFrame = 83333;

        private static Stopwatch s_stopwatch = Stopwatch.StartNew();

        private readonly object _gate = new object();
        private readonly object _nonBatchedGate = new object();
        private readonly double[] _measureBuffer = new double[4];

        private readonly NativeViewHierarchyManager _nativeViewHierarchyManager;
        private readonly ReactContext _reactContext;

        private readonly IList<Action> _nonBatchedOperations = new List<Action>();

        private IList<Action> _operations = new List<Action>();
        private IList<Action> _batches = new List<Action>();

        private long _lastRenderingTicks = -1;

        /// <summary>
        /// Instantiates the <see cref="UIViewOperationQueue"/>.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="nativeViewHierarchyManager">
        /// The native view hierarchy manager.
        /// </param>
        public UIViewOperationQueue(ReactContext reactContext, NativeViewHierarchyManager nativeViewHierarchyManager)
        {
            _nativeViewHierarchyManager = nativeViewHierarchyManager;
            _reactContext = reactContext;
        }

        /// <summary>
        /// The native view hierarchy manager.
        /// </summary>
        internal NativeViewHierarchyManager NativeViewHierarchyManager
        {
            get
            {
                return _nativeViewHierarchyManager;
            }
        } 

        /// <summary>
        /// Checks if the operation queue is empty.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the queue is empty, <b>false</b> otherwise.
        /// </returns>
        public bool IsEmpty()
        {
            lock (_gate)
            {
                return _operations.Count == 0;
            }
        }

        /// <summary>
        /// Adds a root view to the hierarchy.
        /// </summary>
        /// <param name="tag">The root view tag.</param>
        /// <param name="rootView">The root view.</param>
        /// <param name="themedRootContext">The React context.</param>
        public void AddRootView(
            int tag,
            SizeMonitoringCanvas rootView,
            ThemedReactContext themedRootContext)
        {
            DispatcherHelpers.AssertOnDispatcher();
            _nativeViewHierarchyManager.AddRootView(tag, rootView, themedRootContext);
        }

        /// <summary>
        /// Enqueues an operation to remove the root view.
        /// </summary>
        /// <param name="rootViewTag">The root view tag.</param>
        public void EnqueueRemoveRootView(int rootViewTag)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.RemoveRootView(rootViewTag));
        }

        /// <summary>
        /// Enqueues an operation to set the JavaScript responder.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="initialTag">The initial tag.</param>
        /// <param name="blockNativeResponder">
        /// Signal to block the native responder.
        /// </param>
        public void EnqueueSetJavaScriptResponder(
            int tag,
            int initialTag,
            bool blockNativeResponder)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.SetJavaScriptResponder(
                tag,
                initialTag,
                blockNativeResponder));
        }

        /// <summary>
        /// Enqueues an operation to clear the JavaScript responder.
        /// </summary>
        public void EnqueueClearJavaScriptResponder()
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.ClearJavaScriptResponder());
        }

        /// <summary>
        /// Enqueues an operation to dispatch a command.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="commandArgs">The command arguments.</param>
        public void EnqueueDispatchCommand(int tag, int commandId, JArray commandArgs)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.DispatchCommand(tag, commandId, commandArgs));
        }

        /// <summary>
        /// Enqueues an operation to update the extra data for a view.
        /// </summary>
        /// <param name="reactTag">The view tag.</param>
        /// <param name="extraData">The extra data.</param>
        public void EnqueueUpdateExtraData(int reactTag, object extraData)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.UpdateViewExtraData(reactTag, extraData));
        }

        /// <summary>
        /// Enqueues an operation to show a popup menu.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="items">The menu items.</param>
        /// <param name="error">Called on error.</param>
        /// <param name="success">Called on success.</param>
        public void EnqueueShowPopupMenu(int tag, string[] items, ICallback error, ICallback success)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.ShowPopupMenu(tag, items, success));
        }

        /// <summary>
        /// Enqueues an operation to create a view.
        /// </summary>
        /// <param name="themedContext">The React context.</param>
        /// <param name="viewReactTag">The view React tag.</param>
        /// <param name="viewClassName">The view class name.</param>
        /// <param name="initialProps">The initial properties.</param>
        public void EnqueueCreateView(
            ThemedReactContext themedContext,
            int viewReactTag,
            string viewClassName,
            ReactStylesDiffMap initialProps)
        {
            lock (_nonBatchedGate)
            {
                _nonBatchedOperations.Add(() => _nativeViewHierarchyManager.CreateView(
                   themedContext,
                   viewReactTag,
                   viewClassName,
                   initialProps));
            }
        }

        /// <summary>
        /// Clears the animation layout updates.
        /// </summary>
        public void ClearAnimationLayout()
        {
            _nativeViewHierarchyManager.ClearLayoutAnimation();
        }

        /// <summary>
        /// Enqueue a configure layout animation operation.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="success">The success callback.</param>
        /// <param name="error">The error callback.</param>
        public void EnqueueConfigureLayoutAnimation(JObject config, ICallback success, ICallback error)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.ConfigureLayoutAnimation(config, success, error));
        }

        /// <summary>
        /// Enqueues an operation to update the properties of a view.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="className">The class name.</param>
        /// <param name="props">The properties.</param>
        public void EnqueueUpdateProperties(int tag, string className, ReactStylesDiffMap props)
        {
            EnqueueOperation(() =>
                _nativeViewHierarchyManager.UpdateProperties(tag, props));
        }

        /// <summary>
        /// Enqueues an operation to update the layout of a view.
        /// </summary>
        /// <param name="parentTag">The parent tag.</param>
        /// <param name="tag">The view tag.</param>
        /// <param name="dimensions">The dimensions.</param>
        public void EnqueueUpdateLayout(
            int parentTag,
            int tag,
            Dimensions dimensions)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.UpdateLayout(
                parentTag,
                tag,
                dimensions));
        }

        /// <summary>
        /// Enqueues an operation to manage the children of a view.
        /// </summary>
        /// <param name="tag">The view to manage.</param>
        /// <param name="indexesToRemove">The indices to remove.</param>
        /// <param name="viewsToAdd">The views to add.</param>
        /// <param name="tagsToDelete">The tags to delete.</param>
        public void EnqueueManageChildren(
            int tag,
            int[] indexesToRemove,
            ViewAtIndex[] viewsToAdd,
            int[] tagsToDelete)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.ManageChildren(
                tag,
                indexesToRemove,
                viewsToAdd,
                tagsToDelete));
        }

        /// <summary>
        /// Enqueues an operation to set the children of a view.
        /// </summary>
        /// <param name="reactTag">The view to manage.</param>
        /// <param name="childrenTags">The children tags.</param>
        public void EnqueueSetChildren(int reactTag, int[] childrenTags)
        {
            EnqueueOperation(() => _nativeViewHierarchyManager.SetChildren(
                reactTag,
                childrenTags));
        }

        /// <summary>
        /// Enqueues an operation to measure the view.
        /// </summary>
        /// <param name="reactTag">The tag of the view to measure.</param>
        /// <param name="callback">The measurement result callback.</param>
        public void EnqueueMeasure(int reactTag, ICallback callback)
        {
            EnqueueOperation(() =>
            {
                try
                {
                    _nativeViewHierarchyManager.Measure(reactTag, _measureBuffer);
                }
                catch
                {
                    callback.Invoke();
                    return;
                }

                var x = _measureBuffer[0];
                var y = _measureBuffer[1];
                var width = _measureBuffer[2];
                var height = _measureBuffer[3];
                callback.Invoke(0, 0, width, height, x, y);
            });
        }

        /// <summary>
        /// Enqueues an operation to measure the view relative to the window.
        /// </summary>
        /// <param name="reactTag">The tag of the view to measure.</param>
        /// <param name="callback">The measurement result callback.</param>
        public void EnqueueMeasureInWindow(int reactTag, ICallback callback)
        {
            EnqueueOperation(() =>
            {
                try
                {
                    _nativeViewHierarchyManager.MeasureInWindow(reactTag, _measureBuffer);
                }
                catch
                {
                    callback.Invoke();
                    return;
                }

                var x = _measureBuffer[0];
                var y = _measureBuffer[1];
                var width = _measureBuffer[2];
                var height = _measureBuffer[3];
                callback.Invoke(0, 0, width, height, x, y);
            });
        }

        /// <summary>
        /// Enqueues an operation to find a touch target.
        /// </summary>
        /// <param name="reactTag">The parent view to search from.</param>
        /// <param name="targetX">The x-coordinate of the touch event.</param>
        /// <param name="targetY">The y-coordinate of the touch event.</param>
        /// <param name="callback">The callback.</param>
        public void EnqueueFindTargetForTouch(
            int reactTag,
            double targetX,
            double targetY,
            ICallback callback)
        {
            EnqueueOperation(() =>
            {
                try
                {
                    _nativeViewHierarchyManager.Measure(reactTag, _measureBuffer);
                }
                catch
                {
                    // TODO: catch specific exception?
                    callback.Invoke();
                    return;
                }

                var containerX = (double)_measureBuffer[0];
                var containerY = (double)_measureBuffer[1];
                var touchTargetReactTag = _nativeViewHierarchyManager.FindTargetForTouch(reactTag, targetX, targetY);

                try
                {
                    _nativeViewHierarchyManager.Measure(touchTargetReactTag, _measureBuffer);
                }
                catch
                {
                    // TODO: catch specific exception?
                    callback.Invoke();
                    return;
                }

                var x = _measureBuffer[0] - containerX;
                var y = _measureBuffer[1] - containerY;
                var width = _measureBuffer[2];
                var height = _measureBuffer[3];
                callback.Invoke(touchTargetReactTag, x, y, width, height);
            });
        }

        /// <summary>
        /// Called when the host receives the suspend event.
        /// </summary>
        public void OnSuspend()
        {
            CompositionTarget.Rendering -= OnRenderingSafe;
        }

        /// <summary>
        /// Called when the host receives the resume event.
        /// </summary>
        public void OnResume()
        {
            CompositionTarget.Rendering += OnRenderingSafe;
        }

        /// <summary>
        /// Called when the host is shutting down.
        /// </summary>
        public void OnShutdown()
        {
        }

        /// <summary>
        /// Dispatches the view updates.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        internal void DispatchViewUpdates(int batchId)
        {
            var operations = _operations.Count == 0 ? null : _operations;
            if (operations != null)
            {
                _operations = new List<Action>();
            }

            var nonBatchedOperations = default(Action[]);
            lock (_nonBatchedGate)
            {
                if (_nonBatchedOperations.Count > 0)
                {
                    nonBatchedOperations = _nonBatchedOperations.ToArray();
                    _nonBatchedOperations.Clear();
                }
                else
                {
                    nonBatchedOperations = null;
                }
            }

            lock (_gate)
            {
                _batches.Add(() =>
                {
                    using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "DispatchUI")
                        .With("BatchId", batchId)
                        .Start())
                    {
                        if (nonBatchedOperations != null)
                        {
                            foreach (var operation in nonBatchedOperations)
                            {
                                operation();
                            }
                        }

                        if (operations != null)
                        {
                            foreach (var operation in operations)
                            {
                                operation();
                            }
                        }

                        _nativeViewHierarchyManager.ClearLayoutAnimation();
                    }
                });
            }
        }

        private void EnqueueOperation(Action action)
        {
            lock (_gate)
            {
                _operations.Add(action);
            }
        }

        private void OnRenderingSafe(object sender, object e)
        {
            try
            {
                OnRendering(sender, e);
            }
            catch (Exception ex)
            {
                _reactContext.HandleException(ex);
            }
        }

        private void OnRendering(object sender, object e)
        {
            var renderingArgs = e as RenderingEventArgs;
            if (renderingArgs != null)
            {
                using (Tracer.Trace(Tracer.TRACE_TAG_REACT_BRIDGE, "dispatchNonBatchedUIOperations").Start())
                {
                    DispatchPendingNonBatchedOperations(renderingArgs.RenderingTime);
                }
            }

            lock (_gate)
            {
                try
                {
                    foreach (var batch in _batches)
                    {
                        batch();
                    }
                }
                finally
                {
                    _batches.Clear();
                }
            }
        }

        private void DispatchPendingNonBatchedOperations(TimeSpan renderingTime)
        {
            if (_lastRenderingTicks < 0)
            {
                _lastRenderingTicks = renderingTime.Ticks;
            }

            var ticksSinceLastFrame = renderingTime.Ticks - _lastRenderingTicks;
            _lastRenderingTicks = renderingTime.Ticks;
            var lastFrameTicksOverage = Math.Max(0, ticksSinceLastFrame - TicksPerFrame);
            var allowedTicks = MaxNonBatchedTicksPerFrame - lastFrameTicksOverage;

            var frameStartTicks = s_stopwatch.ElapsedTicks;
            while (true)
            {
                // Use up to `MaxNonBatchedTicksPerFrame` minus the delay in the last frame
                var elapsedTicks = s_stopwatch.ElapsedTicks - frameStartTicks;
                if (elapsedTicks > allowedTicks)
                {
                    break;
                }

                var nextOperation = default(Action);
                lock (_nonBatchedGate)
                {
                    if (_nonBatchedOperations.Count == 0)
                    {
                        break;
                    }

                    nextOperation = _nonBatchedOperations[0];
                    _nonBatchedOperations.RemoveAt(0);
                }

                nextOperation();
            }
        }
    }
}
