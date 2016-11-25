﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.Touch;
using ReactNative.Tracing;
using ReactNative.UIManager.LayoutAnimation;
using System;
using System.Collections.Generic;
using System.Linq;
#if WINDOWS_UWP
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
#endif
using static System.FormattableString;

namespace ReactNative.UIManager
{
    /// <summary>
    /// Delegate of <see cref="UIManagerModule"/> that owns the native view
    /// hierarchy and mapping between native view names used in JavaScript and
    /// corresponding instances of <see cref="IViewManager"/>. The 
    /// <see cref="UIManagerModule"/> communicates with this class by it's
    /// public interface methods:
    /// - <see cref="UpdateProperties(int, ReactStylesDiffMap)"/>
    /// - <see cref="UpdateLayout(int, int, Dimensions)"/>
    /// - <see cref="CreateView(ThemedReactContext, int, string, ReactStylesDiffMap)"/>
    /// - <see cref="ManageChildren(int, int[], ViewAtIndex[], int[])"/>
    /// executing all the scheduled operations at the end of the JavaScript batch.
    /// </summary>
    /// <remarks>
    /// All native view management methods listed above must be called from the
    /// dispatcher thread.
    /// 
    /// The <see cref="ReactContext"/> instance that is passed to views that
    /// this manager creates differs from the one that we pass to the
    /// constructor. Instead we wrap the provided instance of 
    /// <see cref="ReactContext"/> in an instance of <see cref="ThemedReactContext"/>
    /// that additionally provides a correct theme based on the root view for
    /// a view tree that we attach newly created views to. Therefore this view
    /// manager will create a copy of <see cref="ThemedReactContext"/> that
    /// wraps the instance of <see cref="ReactContext"/> for each root view
    /// added to the manager (see
    /// <see cref="AddRootView(int, SizeMonitoringCanvas, ThemedReactContext)"/>).
    /// 
    /// TODO: 
    /// 1) AnimationRegistry
    /// 2) ShowPopupMenu
    /// </remarks>
    public class NativeViewHierarchyManager
    {
        private readonly IDictionary<int, IViewManager> _tagsToViewManagers;
        private readonly IDictionary<int, DependencyObject> _tagsToViews;
        private readonly IDictionary<int, bool> _rootTags;
        private readonly ViewManagerRegistry _viewManagers;
        private readonly JavaScriptResponderHandler _jsResponderHandler;
        private readonly RootViewManager _rootViewManager;
        private readonly LayoutAnimationController _layoutAnimator;

        /// <summary>
        /// Instantiates the <see cref="NativeViewHierarchyManager"/>.
        /// </summary>
        /// <param name="viewManagers">The view manager registry.</param>
        public NativeViewHierarchyManager(ViewManagerRegistry viewManagers)
        {
            _viewManagers = viewManagers;
            _layoutAnimator = new LayoutAnimationController();
            _tagsToViews = new Dictionary<int, DependencyObject>();
            _tagsToViewManagers = new Dictionary<int, IViewManager>();
            _rootTags = new Dictionary<int, bool>();
            _jsResponderHandler = new JavaScriptResponderHandler();
            _rootViewManager = new RootViewManager();
        }

        /// <summary>
        /// Signals if layout animation is enabled.
        /// </summary>
        public bool LayoutAnimationEnabled
        {
            private get;
            set;
        }
        
        /// <summary>
        /// Updates the properties of the view with the given tag.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="props">The properties.</param>
        public void UpdateProperties(int tag, ReactStylesDiffMap props)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var viewManager = ResolveViewManager(tag);
            var viewToUpdate = ResolveView(tag);
            viewManager.UpdateProperties(viewToUpdate, props);
        }

        /// <summary>
        /// Updates the extra data for the view with the given tag.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="extraData">The extra data.</param>
        public void UpdateViewExtraData(int tag, object extraData)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var viewManager = ResolveViewManager(tag);
            var viewToUpdate = ResolveView(tag);
            viewManager.UpdateExtraData(viewToUpdate, extraData);
        }

        /// <summary>
        /// Updates the layout of a view.
        /// </summary>
        /// <param name="parentTag">The parent view tag.</param>
        /// <param name="tag">The view tag.</param>
        /// <param name="dimensions">The dimensions.</param>
        public void UpdateLayout(int parentTag, int tag, Dimensions dimensions)
        {
            DispatcherHelpers.AssertOnDispatcher();
            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_VIEW, "NativeViewHierarcyManager.UpdateLayout")
                .With("parentTag", parentTag)
                .With("tag", tag)
                .Start())
            {
                var viewToUpdate = ResolveView(tag);
                var viewManager = ResolveViewManager(tag);

                var parentViewManager = default(IViewManager);
                var parentViewParentManager = default(IViewParentManager);
                if (!_tagsToViewManagers.TryGetValue(parentTag, out parentViewManager) ||
                    (parentViewParentManager = parentViewManager as IViewParentManager) == null)
                {
                    throw new InvalidOperationException(
                        Invariant($"Trying to use view with tag '{tag}' as a parent, but its manager doesn't extend ViewParentManager."));
                }

                if (!parentViewParentManager.NeedsCustomLayoutForChildren)
                {
                    UpdateLayout(viewToUpdate, viewManager, dimensions);
                }
            }
        }

        /// <summary>
        /// Creates a view with the given tag and class name.
        /// </summary>
        /// <param name="themedContext">The context.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="className">The class name.</param>
        /// <param name="initialProperties">The properties.</param>
        public void CreateView(ThemedReactContext themedContext, int tag, string className, ReactStylesDiffMap initialProperties)
        {
            DispatcherHelpers.AssertOnDispatcher();
            using (Tracer.Trace(Tracer.TRACE_TAG_REACT_VIEW, "NativeViewHierarcyManager.CreateView")
                .With("tag", tag)
                .With("className", className)
                .Start())
            {
                var viewManager = _viewManagers.Get(className);
                var view = viewManager.CreateView(themedContext, _jsResponderHandler);
                _tagsToViews.Add(tag, view);
                _tagsToViewManagers.Add(tag, viewManager);

                // Uses an extension method and `Tag` property on 
                // DependencyObject to store the tag of the view.
                view.SetTag(tag);
                view.SetReactContext(themedContext);

                if (initialProperties != null)
                {
                    viewManager.UpdateProperties(view, initialProperties);
                }
            }
        }

        /// <summary>
        /// Sets up the Layout Animation Manager.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void ConfigureLayoutAnimation(JObject config, ICallback success, ICallback error)
        {
            _layoutAnimator.InitializeFromConfig(config);
        }

        /// <summary>
        /// Clears out the <see cref="LayoutAnimationController"/>.
        /// </summary>
        public void ClearLayoutAnimation()
        {
            _layoutAnimator.Reset();
        }

        /// <summary>
        /// Manages the children of a React view.
        /// </summary>
        /// <param name="tag">The tag of the view to manager.</param>
        /// <param name="indexesToRemove">Child indices to remove.</param>
        /// <param name="viewsToAdd">Views to add.</param>
        /// <param name="tagsToDelete">Tags to delete.</param>
        public void ManageChildren(int tag, int[] indexesToRemove, ViewAtIndex[] viewsToAdd, int[] tagsToDelete)
        {
            var viewManager = default(IViewManager);
            if (!_tagsToViewManagers.TryGetValue(tag, out viewManager))
            {
                throw new InvalidOperationException(
                    Invariant($"Trying to manage children with tag '{tag}' which doesn't exist."));
            }

            var viewParentManager = (IViewParentManager)viewManager;
            var viewToManage = _tagsToViews[tag];

            var lastIndexToRemove = viewParentManager.GetChildCount(viewToManage);
            if (indexesToRemove != null)
            {
                for (var i = indexesToRemove.Length - 1; i >= 0; --i)
                {
                    var indexToRemove = indexesToRemove[i];
                    if (indexToRemove < 0)
                    {
                        throw new InvalidOperationException(
                            Invariant($"Trying to remove a negative index '{indexToRemove}' on view tag '{tag}'."));
                    }

                    if (indexToRemove >= viewParentManager.GetChildCount(viewToManage))
                    {
                        throw new InvalidOperationException(
                            Invariant($"Trying to remove a view index '{indexToRemove}' greater than the child could for view tag '{tag}'."));
                    }

                    if (indexToRemove >= lastIndexToRemove)
                    {
                        throw new InvalidOperationException(
                            Invariant($"Trying to remove an out of order index '{indexToRemove}' (last index was '{lastIndexToRemove}') for view tag '{tag}'."));
                    }

                    var viewToRemove = viewParentManager.GetChildAt(viewToManage, indexToRemove) as FrameworkElement;
                    if (viewToRemove != null &&
                        _layoutAnimator.ShouldAnimateLayout(viewToRemove) &&
                        tagsToDelete.Contains(viewToRemove.GetTag()))
                    {
                        // The view will be removed and dropped by the 'delete'
                        // layout animation instead, so do nothing.
                    }
                    else
                    {
                        viewParentManager.RemoveChildAt(viewToManage, indexToRemove);
                    }

                    lastIndexToRemove = indexToRemove;
                }
            }

            if (viewsToAdd != null)
            {
                for (var i = 0; i < viewsToAdd.Length; ++i)
                {
                    var viewAtIndex = viewsToAdd[i];
                    var viewToAdd = default(DependencyObject);
                    if (!_tagsToViews.TryGetValue(viewAtIndex.Tag, out viewToAdd))
                    {
                        throw new InvalidOperationException(
                            Invariant($"Trying to add unknown view tag '{viewAtIndex.Tag}'."));
                    }

                    viewParentManager.AddView(viewToManage, viewToAdd, viewAtIndex.Index);
                }
            }

            if (tagsToDelete != null)
            {
                for (var i = 0; i < tagsToDelete.Length; ++i)
                {
                    var tagToDelete = tagsToDelete[i];
                    var viewToDestroy = default(DependencyObject);
                    if (!_tagsToViews.TryGetValue(tagToDelete, out viewToDestroy))
                    {
                        throw new InvalidOperationException(
                            Invariant($"Trying to destroy unknown view tag '{tagToDelete}'."));
                    }

                    var elementToDestroy = viewToDestroy as FrameworkElement;
                    if (elementToDestroy != null &&
                        _layoutAnimator.ShouldAnimateLayout(elementToDestroy))
                    {
                        _layoutAnimator.DeleteView(elementToDestroy, () =>
                        {
                            if (viewParentManager.TryRemoveView(viewToManage, viewToDestroy))
                            {
                                DropView(viewToDestroy);
                            }
                        });
                    }
                    else
                    {
                        DropView(viewToDestroy);
                    }
                }
            }
        }

        /// <summary>
        /// Simplified version of <see cref="UIManagerModule.manageChildren(int, int[], int[], int[], int[], int[])"/>
        /// that only deals with adding children views.
        /// </summary>
        /// <param name="tag">The view tag to manage.</param>
        /// <param name="childrenTags">The children tags.</param>
        public void SetChildren(int tag, int[] childrenTags)
        {
            var viewToManage = _tagsToViews[tag];
            var viewManager = (IViewParentManager)ResolveViewManager(tag);

            for (var i = 0; i < childrenTags.Length; ++i)
            {
                var viewToAdd = _tagsToViews[childrenTags[i]];
                if (viewToAdd == null)
                {
                    throw new InvalidOperationException(
                        Invariant($"Trying to add unknown view tag: {childrenTags[i]}."));
                }

                viewManager.AddView(viewToManage, viewToAdd, i);
            }
        }

        /// <summary>
        /// Remove the root view with the given tag.
        /// </summary>
        /// <param name="rootViewTag">The root view tag.</param>
        public void RemoveRootView(int rootViewTag)
        {
            DispatcherHelpers.AssertOnDispatcher();
            if (!_rootTags.ContainsKey(rootViewTag))
            {
                throw new InvalidOperationException(
                    Invariant($"View with tag '{rootViewTag}' is not registered as a root view."));
            }

            var rootView = _tagsToViews[rootViewTag];
            DropView(rootView);
            _rootTags.Remove(rootViewTag);
        }

        /// <summary>
        /// Measures a view and sets the output buffer to (x, y, width, height).
        /// Measurements are relative to the RootView.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="outputBuffer">The output buffer.</param>
        public void Measure(int tag, double[] outputBuffer)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(tag, out view))
            {
                throw new ArgumentOutOfRangeException(nameof(tag));
            }

            var viewManager = default(IViewManager);
            if (!_tagsToViewManagers.TryGetValue(tag, out viewManager))
            {
                throw new InvalidOperationException(
                    Invariant($"Could not find view manager for tag '{tag}."));
            }

            var rootView = RootViewHelper.GetRootView(view);
            if (rootView == null)
            {
                throw new InvalidOperationException(
                    Invariant($"Native view '{tag}' is no longer on screen."));
            }

            // TODO: better way to get relative position?
            var uiElement = view.As<UIElement>();
            var rootTransform = uiElement.TransformToVisual(rootView);
#if WINDOWS_UWP
            var positionInRoot = rootTransform.TransformPoint(new Point(0, 0));
#else
            var positionInRoot = rootTransform.Transform(new Point(0, 0));
#endif

            var dimensions = viewManager.GetDimensions(uiElement);
            outputBuffer[0] = positionInRoot.X;
            outputBuffer[1] = positionInRoot.Y;
            outputBuffer[2] = dimensions.Width;
            outputBuffer[3] = dimensions.Height;
        }

        /// <summary>
        /// Measures a view and sets the output buffer to (x, y, width, height).
        /// Measurements are relative to the window.
        /// </summary>
        /// <param name="tag">The view tag.</param>
        /// <param name="outputBuffer">The output buffer.</param>
        public void MeasureInWindow(int tag, double[] outputBuffer)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(tag, out view))
            {
                throw new ArgumentOutOfRangeException(nameof(tag));
            }

            var viewManager = default(IViewManager);
            if (!_tagsToViewManagers.TryGetValue(tag, out viewManager))
            {
                throw new InvalidOperationException(
                    Invariant($"Could not find view manager for tag '{tag}."));
            }

            var uiElement = view.As<UIElement>();
#if WINDOWS_UWP
            var windowTransform = uiElement.TransformToVisual(Window.Current.Content);
            var positionInWindow = windowTransform.TransformPoint(new Point(0, 0));
#else
            var windowTransform = uiElement.TransformToVisual(Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive));
            var positionInWindow = windowTransform.Transform(new Point(0, 0));
#endif

            var dimensions = viewManager.GetDimensions(uiElement);
            outputBuffer[0] = positionInWindow.X;
            outputBuffer[1] = positionInWindow.Y;
            outputBuffer[2] = dimensions.Width;
            outputBuffer[3] = dimensions.Height;
        }

        /// <summary>
        /// Adds a root view with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="view">The root view.</param>
        /// <param name="themedContext">The themed context.</param>
        public void AddRootView(int tag, SizeMonitoringCanvas view, ThemedReactContext themedContext)
        {
            AddRootViewParent(tag, view, themedContext);
        }

        /// <summary>
        /// Find the view target for touch coordinates.
        /// </summary>
        /// <param name="reactTag">The view tag.</param>
        /// <param name="touchX">The x-coordinate of the touch event.</param>
        /// <param name="touchY">The y-coordinate of the touch event.</param>
        /// <returns>The view target.</returns>
        public int FindTargetForTouch(int reactTag, double touchX, double touchY)
        {
            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(reactTag, out view))
            {
                throw new InvalidOperationException(
                    Invariant($"Could not find view with tag '{reactTag}'."));
            }

            var uiElement = view.As<UIElement>();
#if WINDOWS_UWP
            var target = VisualTreeHelper.FindElementsInHostCoordinates(new Point(touchX, touchY), uiElement)
#else
            var sources = new List<DependencyObject>();
            // ToDo: Consider a pooled structure to improve performance in touch heavy applications
            VisualTreeHelper.HitTest(
                uiElement,
                null,
                hit =>
                {
                    sources.Add(hit.VisualHit);
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(new Point(touchX, touchY)));
            var target = sources
#endif
                .OfType<FrameworkElement>()
                .Where(e => e.HasTag())
                .FirstOrDefault();

            if (target == null)
            {
                throw new InvalidOperationException(
                    Invariant($"Could not find React view at coordinates '{touchX},{touchY}'."));
            }

            return target.GetTag();
        }

        /// <summary>
        /// Sets the JavaScript responder handler for a view.
        /// </summary>
        /// <param name="reactTag">The view tag.</param>
        /// <param name="initialReactTag">The initial tag.</param>
        /// <param name="blockNativeResponder">
        /// Flag to block the native responder.
        /// </param>
        public void SetJavaScriptResponder(int reactTag, int initialReactTag, bool blockNativeResponder)
        {
            if (!blockNativeResponder)
            {
                _jsResponderHandler.SetJavaScriptResponder(initialReactTag, null);
                return;
            }

            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(reactTag, out view))
            {
                throw new InvalidOperationException(
                    Invariant($"Could not find view with tag '{reactTag}'."));
            }

            // TODO: (#306) Finish JS responder implementation. 
        }

        /// <summary>
        /// Clears the JavaScript responder.
        /// </summary>
        public void ClearJavaScriptResponder()
        {
            _jsResponderHandler.ClearJavaScriptResponder();
        }

        /// <summary>
        /// Dispatches a command to a view.
        /// </summary>
        /// <param name="reactTag">The view tag.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="args">The command arguments.</param>
        public void DispatchCommand(int reactTag, int commandId, JArray args)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(reactTag, out view))
            {
                throw new InvalidOperationException(
                    Invariant($"Trying to send command to a non-existent view with tag '{reactTag}."));
            }

            var viewManager = ResolveViewManager(reactTag);
            viewManager.ReceiveCommand(view, commandId, args);
        }

        /// <summary>
        /// Shows a popup menu.
        /// </summary>
        /// <param name="tag">
        /// The tag of the anchor view (the popup menu is
        /// displayed next to this view); this needs to be the tag of a native
        /// view (shadow views cannot be anchors).
        /// </param>
        /// <param name="items">The menu items as an array of strings.</param>
        /// <param name="success">
        /// A callback used with the position of the selected item as the first
        /// argument, or no arguments if the menu is dismissed.
        /// </param>
        public void ShowPopupMenu(int tag, string[] items, ICallback success)
        {
#if WINDOWS_UWP
            DispatcherHelpers.AssertOnDispatcher();
            var view = ResolveView(tag);

            var menu = new PopupMenu();
            for (var i = 0; i < items.Length; ++i)
            {
                menu.Commands.Add(new UICommand(
                    items[i],
                    cmd =>
                    {
                        success.Invoke(cmd.Id);
                    },
                    i));
            }
#endif

            // TODO: figure out where to popup the menu
            // TODO: add continuation that calls the callback with empty args
            throw new NotImplementedException();
        }

        private DependencyObject ResolveView(int tag)
        {
            var view = default(DependencyObject);
            if (!_tagsToViews.TryGetValue(tag, out view))
            {
                throw new InvalidOperationException(
                    Invariant($"Trying to resolve view with tag '{tag}' which doesn't exist."));
            }

            return view;
        }

        private IViewManager ResolveViewManager(int tag)
        {
            var viewManager = default(IViewManager);
            if (!_tagsToViewManagers.TryGetValue(tag, out viewManager))
            {
                throw new InvalidOperationException(
                    Invariant($"ViewManager for tag '{tag}' could not be found."));
            }

            return viewManager;
        }

        private void AddRootViewParent(int tag, FrameworkElement view, ThemedReactContext themedContext)
        {
            DispatcherHelpers.AssertOnDispatcher();
            _tagsToViews.Add(tag, view);
            _tagsToViewManagers.Add(tag, _rootViewManager);
            _rootTags.Add(tag, true);
            view.SetTag(tag);
            view.SetReactContext(themedContext);
        }

        private void DropView(DependencyObject view)
        {
            DispatcherHelpers.AssertOnDispatcher();
            var tag = view.GetTag();
            if (!_rootTags.ContainsKey(tag))
            {
                // For non-root views, we notify the view manager with `OnDropViewInstance`
                var mgr = ResolveViewManager(tag);
                mgr.OnDropViewInstance(view.GetReactContext(), view);
            }

            var viewManager = default(IViewManager);
            if (_tagsToViewManagers.TryGetValue(tag, out viewManager))
            {
                var viewParentManager = viewManager as IViewParentManager;
                if (viewParentManager != null)
                {
                    for (var i = viewParentManager.GetChildCount(view) - 1; i >= 0; --i)
                    {
                        var child = viewParentManager.GetChildAt(view, i);
                        var managedChild = default(DependencyObject);
                        if (_tagsToViews.TryGetValue(child.GetTag(), out managedChild))
                        {
                            DropView(managedChild);
                        }
                    }

                    viewParentManager.RemoveAllChildren(view);
                }
            }

            _tagsToViews.Remove(tag);
            _tagsToViewManagers.Remove(tag);
        }

        private void UpdateLayout(DependencyObject viewToUpdate, IViewManager viewManager, Dimensions dimensions)
        {
            var frameworkElement = viewToUpdate as FrameworkElement;
            if (frameworkElement != null && _layoutAnimator.ShouldAnimateLayout(frameworkElement))
            {
                _layoutAnimator.ApplyLayoutUpdate(frameworkElement, dimensions);
            }
            else
            {;
                viewManager.SetDimensions(viewToUpdate, dimensions);
            }
        }
    }
}
