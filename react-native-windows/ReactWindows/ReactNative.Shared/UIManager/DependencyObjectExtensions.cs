﻿using System;
using System.Runtime.CompilerServices;
#if WINDOWS_UWP
using Windows.UI.Xaml;
#else
using System.Windows;
#endif
using static System.FormattableString;

namespace ReactNative.UIManager
{
    /// <summary>
    /// Extension methods for <see cref="DependencyObject"/>.
    /// </summary>
    public static class DependencyObjectExtensions
    {
        private static readonly ConditionalWeakTable<DependencyObject, DependencyObjectData> s_properties =
            new ConditionalWeakTable<DependencyObject, DependencyObjectData>();
        private static readonly IReactCompoundView s_defaultCompoundView = new ReactDefaultCompoundView();

        /// <summary>
        /// Sets the pointer events for the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="pointerEvents">The pointer events.</param>
        public static void SetPointerEvents(this DependencyObject view, PointerEvents pointerEvents)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            s_properties.GetOrCreateValue(view).PointerEvents = pointerEvents;
        }

        /// <summary>
        /// Gets the pointer events for the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>The pointer events.</returns>
        public static PointerEvents GetPointerEvents(this DependencyObject view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var elementData = default(DependencyObjectData);
            if (!s_properties.TryGetValue(view, out elementData) || !elementData.PointerEvents.HasValue)
            {
                return PointerEvents.Auto;
            }

            return elementData.PointerEvents.Value;
        }

        /// <summary>
        /// Associates an implementation of IReactCompoundView with the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="compoundView">The implementation of IReactCompoundView.</param>
        public static void SetReactCompoundView(this DependencyObject view, IReactCompoundView compoundView)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            s_properties.GetOrCreateValue(view).CompoundView = compoundView;
        }

        /// <summary>
        /// Gets the implementation of IReactCompoundView associated with the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>
        /// The implementation of IReactCompoundView associated with the view. Defaults to
        /// an instance of ReactDefaultCompoundView when no other implementation has been
        /// provided.
        /// </returns>
        public static IReactCompoundView GetReactCompoundView(this DependencyObject view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var elementData = default(DependencyObjectData);
            if (s_properties.TryGetValue(view, out elementData))
            {
                var compoundView = elementData.CompoundView;
                if (compoundView != null)
                {
                    return compoundView;
                }
            }

            return s_defaultCompoundView;
        }

        internal static void SetTag(this DependencyObject view, int tag)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            s_properties.GetOrCreateValue(view).Tag = tag;
        }

        /// <summary>
        /// Get the React tag for the view instance. 
        /// </summary>
        /// <param name="view">The view instance.</param>
        /// <returns>The React tag.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if tag is not available for the view.
        /// </exception>
        public static int GetTag(this DependencyObject view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var elementData = default(DependencyObjectData);
            if (!s_properties.TryGetValue(view, out elementData) || !elementData.Tag.HasValue)
            {
                throw new InvalidOperationException("Could not get tag for view.");
            }

            return elementData.Tag.Value;
        }

        /// <summary>
        /// Checks if a React tag is available for the view instance.
        /// </summary>
        /// <param name="view">The view instance.</param>
        /// <returns>
        /// <code>true</code> if the view has a tag, <code>false</code> otherwise.
        /// </returns>
        public static bool HasTag(this DependencyObject view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var elementData = default(DependencyObjectData);
            return s_properties.TryGetValue(view, out elementData) && elementData.Tag.HasValue;
        }

        internal static void SetReactContext(this DependencyObject view, ThemedReactContext context)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            s_properties.GetOrCreateValue(view).Context = context;
        }

        /// <summary>
        /// Gets the <see cref="ThemedReactContext"/> associated with the view
        /// instance.
        /// </summary>
        /// <param name="view">The view instance.</param>
        /// <returns>The context.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if context is not available for the view.
        /// </exception>
        public static ThemedReactContext GetReactContext(this DependencyObject view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var elementData = default(DependencyObjectData);
            if (!s_properties.TryGetValue(view, out elementData) || !elementData.Tag.HasValue)
            {
                throw new InvalidOperationException("Could not get tag for view.");
            }

            return elementData.Context;
        }

        internal static T As<T>(this DependencyObject view)
            where T : DependencyObject
        {
            var convertedView = view as T;
            if (convertedView == null)
            {
                throw new ArgumentOutOfRangeException(Invariant($"Child of type '{view.GetType()}' is not assignable to '{typeof(T)}'."));
            }
            return convertedView;
        }

        class DependencyObjectData
        {
            public ThemedReactContext Context { get; set; }

            public PointerEvents? PointerEvents { get; set; }

            public int? Tag { get; set; }

            public IReactCompoundView CompoundView { get; set; }
        }
    }
}
