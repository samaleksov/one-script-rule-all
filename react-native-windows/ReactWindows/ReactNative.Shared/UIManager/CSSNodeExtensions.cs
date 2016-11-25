﻿using Facebook.CSSLayout;

namespace ReactNative.UIManager
{
    static class CSSNodeExtensions
    {
        public static float GetLeftBorderWidth(this CSSNode node)
        {
            var width = node.GetBorder(CSSSpacingType.Left);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            width = node.GetBorder(CSSSpacingType.Horizontal);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            width = node.GetBorder(CSSSpacingType.All);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            return 0.0f;
        }

        public static float GetPaddingValue(this CSSNode node, CSSSpacingType spacingType)
        {
            var padding = node.GetPadding(spacingType);
            if (!CSSConstants.IsUndefined(padding))
            {
                return padding;
            }

            if (spacingType == CSSSpacingType.Left || spacingType == CSSSpacingType.Right)
            {
                padding = node.GetPadding(CSSSpacingType.Horizontal);
            }

            if (!CSSConstants.IsUndefined(padding))
            {
                return padding;
            }

            if (spacingType == CSSSpacingType.Top || spacingType == CSSSpacingType.Bottom)
            {
                padding = node.GetPadding(CSSSpacingType.Vertical);
            }

            if (!CSSConstants.IsUndefined(padding))
            {
                return padding;
            }

            return node.GetPadding(CSSSpacingType.All);
        }

        public static float GetPaddingSpace(this CSSNode node, CSSSpacingType spacingType)
        {
            var padding = node.GetPaddingValue(spacingType);
            return CSSConstants.IsUndefined(padding)
                ? 0.0f
                : padding;
        }

        public static float GetTopBorderWidth(this CSSNode node)
        {
            var width = node.GetBorder(CSSSpacingType.Top);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            width = node.GetBorder(CSSSpacingType.Vertical);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            width = node.GetBorder(CSSSpacingType.All);
            if (!CSSConstants.IsUndefined(width))
            {
                return width;
            }

            return 0.0f;
        }
    }
}
