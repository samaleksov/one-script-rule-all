﻿using System;
using System.Reactive;
#if WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
#else
using System.Windows;
using System.Windows.Media.Animation;
#endif

namespace ReactNative.UIManager.LayoutAnimation
{
    /// <summary>
    /// Class responsible for default layout animation.
    /// </summary>
    abstract class BaseLayoutAnimation : LayoutAnimation
    {
        /// <summary>
        /// Signals if the animation should be performed in reverse.
        /// </summary>
        protected abstract bool IsReverse { get; }

        /// <summary>
        /// Signals if the animation configuration is valid.
        /// </summary>
        protected override bool IsValid
        {
            get
            {
                return Duration > TimeSpan.Zero && AnimatedProperty != null;
            }
        }

        /// <summary>
        /// Create a <see cref="Storyboard"/> to be used to animate the view, 
        /// based on the animation configuration supplied at initialization
        /// time and the new view position and size.
        /// </summary>
        /// <param name="view">The view to create the animation for.</param>
        /// <param name="dimensions">The view dimensions.</param>
        protected override IObservable<Unit> CreateAnimationCore(FrameworkElement view, Dimensions dimensions)
        {
            var fromValue = IsReverse ? 1.0 : 0.0;
            var toValue = IsReverse ? 0.0 : 1.0;

            var animatedProperty = AnimatedProperty;
            if (animatedProperty.HasValue)
            {
                var storyboard = new Storyboard();
                var @finally = default(Action);
                switch (animatedProperty.Value)
                {
                    case AnimatedPropertyType.Opacity:
                        view.Opacity = fromValue;
                        storyboard.Children.Add(CreateOpacityAnimation(view, fromValue, toValue));
                        @finally = () => view.Opacity = toValue;
                        break;
                    case AnimatedPropertyType.ScaleXY:
                        // TODO: implement this layout animation option
                        throw new NotImplementedException();
                    default:
                        throw new InvalidOperationException(
                            "Missing animation for property: " + animatedProperty.Value);
                }

                return new StoryboardObservable(storyboard, @finally);
            }

            throw new InvalidOperationException(
                "Missing animated property from the animation configuration.");
        }

        private Timeline CreateOpacityAnimation(FrameworkElement view, double from, double to)
        {
            var timeline = new DoubleAnimation
            {
                From = from,
                To = to,
                EasingFunction = Interpolator,
                Duration = Duration,
                BeginTime = Delay,
            };

            Storyboard.SetTarget(timeline, view);
#if WINDOWS_UWP
            Storyboard.SetTargetProperty(timeline, "Opacity");
#else
            Storyboard.SetTargetProperty(timeline, new PropertyPath("Opacity"));
#endif

            return timeline;
        }
    }
}
