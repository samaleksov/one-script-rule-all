﻿#if WINDOWS_UWP
using Windows.UI;
#else
using System.Windows.Media;
#endif

namespace ReactNative.UIManager
{
    static class ColorHelpers
    {
        public const uint Transparent = 0x00FFFFFF;

        public static Color Parse(uint value)
        {
            var color = value;
            var b = (byte)color;
            color >>= 8;
            var g = (byte)color;
            color >>= 8;
            var r = (byte)color;
            color >>= 8;
            var a = (byte)color;
            return Color.FromArgb(a, r, g, b);
        }
    }
}
