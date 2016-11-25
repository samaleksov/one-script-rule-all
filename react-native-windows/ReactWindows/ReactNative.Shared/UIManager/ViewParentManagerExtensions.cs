﻿#if WINDOWS_UWP
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace ReactNative.UIManager
{
    /// <summary>
    /// Extension methods for <see cref="IViewParentManager"/>. 
    /// </summary>
    public static class ViewParentManagerExtensions
    {
        /// <summary>
        /// Tries to remove a view from a <see cref="IViewParentManager"/>. 
        /// </summary>
        /// <param name="viewManager">The view manager.</param>
        /// <param name="parent">The parent view.</param>
        /// <param name="view">The view to remove.</param>
        /// <returns>
        /// <code>true</code> if a view is removed, <code>false</code> otherwise.
        /// </returns>
        public static bool TryRemoveView(this IViewParentManager viewManager, DependencyObject parent, DependencyObject view)
        {
            for (var i = 0; i < viewManager.GetChildCount(parent); ++i)
            {
                if (viewManager.GetChildAt(parent, i) == view)
                {
                    viewManager.RemoveChildAt(parent, i);
                    return true;
                }
            }

            return false;
        }
    }
}
