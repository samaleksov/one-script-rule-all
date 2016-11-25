﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
#if WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace ReactNative.UIManager.LayoutAnimation
{
    /// <summary>
    /// Class responsible for animation layout changes, if a valid animation
    /// configuration has been supplied. If animation is not available, the
    /// layout change is applied immediately instead of animating.
    /// </summary>
    /// <remarks>
    /// TODO: Invoke success callback at the end of the animation.
    /// </remarks>
    public class LayoutAnimationController
    {
        private readonly ConditionalWeakTable<FrameworkElement, SerialDisposable> _activeAnimations =
            new ConditionalWeakTable<FrameworkElement, SerialDisposable>();

        private readonly LayoutAnimation _layoutCreateAnimation = new LayoutCreateAnimation();
        private readonly LayoutAnimation _layoutUpdateAnimation = new LayoutUpdateAnimation();
        private readonly LayoutAnimation _layoutDeleteAnimation = new LayoutDeleteAnimation();

        private bool _shouldAnimateLayout;

        /// <summary>
        /// Initializes the layout animation.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public void InitializeFromConfig(JObject config)
        {
#if !LAYOUT_ANIMATION_DISABLED
            if (config == null)
            {
                Reset();
                return;
            }

            _shouldAnimateLayout = false;
            var globalDuration = config.Value<int>("duration");
            var createData = (JObject)config.GetValue("create", StringComparison.Ordinal);
            if (createData != null)
            {
                _layoutCreateAnimation.InitializeFromConfig(createData, globalDuration);
                _shouldAnimateLayout = true;
            }

            var updateData = (JObject)config.GetValue("update", StringComparison.Ordinal);
            if (updateData != null)
            {
                _layoutUpdateAnimation.InitializeFromConfig(updateData, globalDuration);
                _shouldAnimateLayout = true;
            }

            var deleteData = (JObject)config.GetValue("delete", StringComparison.Ordinal);
            if (deleteData != null)
            {
                _layoutDeleteAnimation.InitializeFromConfig(deleteData, globalDuration);
                _shouldAnimateLayout = true;
            }
#else
            return;
#endif
        }

        /// <summary>
        /// Determines if <see cref="FrameworkElement"/> should apply layout animation.
        /// </summary>
        /// <param name="view">The view to animate.</param>
        /// <returns>
        /// <code>true</code> if the layout operation should be animated, 
        /// otherwise <code>false</code>.
        /// </returns>
        public bool ShouldAnimateLayout(FrameworkElement view)
        {
            return _shouldAnimateLayout && view.Parent != null;
        }

        /// <summary>
        /// Applies a layout animation for the given view on the transition to
        /// the given coordinates and dimensions.
        /// </summary>
        /// <param name="view">The native view to animate.</param>
        /// <param name="dimensions">The new view dimensions to animate to.</param>
        public void ApplyLayoutUpdate(FrameworkElement view, Dimensions dimensions)
        {
            DispatcherHelpers.AssertOnDispatcher();

            var layoutAnimation = view.ActualWidth == 0 || view.ActualHeight == 0
                ? _layoutCreateAnimation
                : _layoutUpdateAnimation;

            var animation = layoutAnimation.CreateAnimation(view, dimensions);
            if (animation == null)
            {
                Canvas.SetLeft(view, dimensions.X);
                Canvas.SetTop(view, dimensions.Y);
                view.Width = dimensions.Width;
                view.Height = dimensions.Height;
            }
            else
            {
                StartAnimation(view, animation);
            }
        }

        /// <summary>
        /// Animate a view deletion using the layout animation configuration
        /// supplied during initialization.
        /// </summary>
        /// <param name="view">The view to animation.</param>
        /// <param name="finally">
        /// Called once the animation is finished, should be used to completely
        /// remove the view.
        /// </param>
        public void DeleteView(FrameworkElement view, Action @finally)
        {
            DispatcherHelpers.AssertOnDispatcher();

            var layoutAnimation = _layoutDeleteAnimation;

            var animation = layoutAnimation.CreateAnimation(
                view, new Dimensions
                {
                    X = Canvas.GetLeft(view),
                    Y = Canvas.GetTop(view),
                    Width = view.Width,
                    Height = view.Height,
                });

            if (animation != null)
            {
                view.IsHitTestVisible = false;
                StartAnimation(view, animation.Finally(@finally));
            }
            else
            {
                @finally();
            }
        }

        /// <summary>
        /// Reset the animation manager.
        /// </summary>
        public void Reset()
        {
            _layoutCreateAnimation.Reset();
            _layoutUpdateAnimation.Reset();
            _shouldAnimateLayout = false;
        }

        private void StartAnimation(FrameworkElement view, IObservable<Unit> animation)
        {
            // Get the serial disposable for the view
            var serialDisposable = _activeAnimations.GetOrCreateValue(view);

            // Dispose any existing animations
            serialDisposable.Disposable = Disposable.Empty;

            // Start the next animation
            serialDisposable.Disposable = animation.Subscribe();
        }
    }
}