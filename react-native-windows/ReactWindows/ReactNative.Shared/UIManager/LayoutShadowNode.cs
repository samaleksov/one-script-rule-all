﻿using Facebook.CSSLayout;
using ReactNative.Reflection;
using ReactNative.UIManager.Annotations;

namespace ReactNative.UIManager
{
    /// <summary>
    /// Shadow node subclass that supplies setters for base view layout
    /// properties such as width, height, flex properties, borders, etc.
    /// </summary>
    public class LayoutShadowNode : ReactShadowNode
    {
        private const float Undefined = CSSConstants.Undefined;

        /// <summary>
        /// Set the width of the shadow node.
        /// </summary>
        /// <param name="width">The width.</param>
        [ReactProp(ViewProps.Width, DefaultSingle = Undefined)]
        public void SetWidth(float width)
        {
            Width = width;
        }

        /// <summary>
        /// Sets the minimum width of the shadow node.
        /// </summary>
        /// <param name="minWidth">The minimum width.</param>
        [ReactProp(ViewProps.MinWidth, DefaultSingle = Undefined)]
        public void SetMinWidth(float minWidth)
        {
            MinWidth = minWidth;
        }

        /// <summary>
        /// Sets the maximum width of the shadow node.
        /// </summary>
        /// <param name="maxWidth">The maximum width.</param>
        [ReactProp(ViewProps.MaxWidth, DefaultSingle = Undefined)]
        public void SetMaxWidth(float maxWidth)
        {
            MaxWidth = maxWidth;
        }

        /// <summary>
        /// Set the heigth of the shadow node.
        /// </summary>
        /// <param name="height">The height.</param>
        [ReactProp(ViewProps.Height, DefaultSingle = Undefined)]
        public void SetHeight(float height)
        {
            Height = height;
        }

        /// <summary>
        /// Sets the minimum height of the shadow node.
        /// </summary>
        /// <param name="minHeight">The minimum height.</param>
        [ReactProp(ViewProps.MinHeight, DefaultSingle = Undefined)]
        public void SetMinHeight(float minHeight)
        {
            MinHeight = minHeight;
        }

        /// <summary>
        /// Sets the maximum height of the shadow node.
        /// </summary>
        /// <param name="maxHeight">The maximum height.</param>
        [ReactProp(ViewProps.MaxHeight, DefaultSingle = Undefined)]
        public void SetMaxHeight(float maxHeight)
        {
            MaxHeight = maxHeight;
        }

        /// <summary>
        /// Sets the left position of the shadow node.
        /// </summary>
        /// <param name="left">The left position.</param>
        [ReactProp(ViewProps.Left, DefaultSingle = Undefined)]
        public void SetLeft(float left)
        {
            PositionLeft = left;
        }

        /// <summary>
        /// Sets the top position of the shadow node.
        /// </summary>
        /// <param name="top">The top position.</param>
        [ReactProp(ViewProps.Top, DefaultSingle = Undefined)]
        public void SetTop(float top)
        {
            PositionTop = top;
        }

        /// <summary>
        /// Sets the bottom position of the shadow node.
        /// </summary>
        /// <param name="bottom">The bottom position.</param>
        [ReactProp(ViewProps.Bottom, DefaultSingle = Undefined)]
        public void SetBottom(float bottom)
        {
            PositionBottom = bottom;
        }

        /// <summary>
        /// Sets the right position of the shadow node.
        /// </summary>
        /// <param name="right">The right position.</param>
        [ReactProp(ViewProps.Right, DefaultSingle = Undefined)]
        public void SetRight(float right)
        {
            PositionRight = right;
        }

        /// <summary>
        /// Sets the flex of the shadow node.
        /// </summary>
        /// <param name="flex">The flex value.</param>
        [ReactProp(ViewProps.Flex, DefaultSingle = 0f)]
        public void SetFlex(float flex)
        {
            Flex = flex;
        }

        /// <summary>
        /// Sets the flex direction of the shadow node.
        /// </summary>
        /// <param name="flexDirection">The flex direction.</param>
        [ReactProp(ViewProps.FlexDirection)]
        public void SetFlexDirection(string flexDirection)
        {
            FlexDirection = EnumHelpers.ParseNullable<CSSFlexDirection>(flexDirection) ?? CSSFlexDirection.Column;
        }

        /// <summary>
        /// Sets the wrap property on the shadow node.
        /// </summary>
        /// <param name="flexWrap">The wrap.</param>
        [ReactProp(ViewProps.FlexWrap)]
        public void SetFlexWrap(string flexWrap)
        {
            Wrap = EnumHelpers.ParseNullable<CSSWrap>(flexWrap) ?? CSSWrap.NoWrap;
        }

        /// <summary>
        /// Sets the self alignment of the shadow node.
        /// </summary>
        /// <param name="alignSelf">The align self property.</param>
        [ReactProp(ViewProps.AlignSelf)]
        public void SetAlignSelf(string alignSelf)
        {
            AlignSelf = EnumHelpers.ParseNullable<CSSAlign>(alignSelf) ?? CSSAlign.Auto;
        }

        /// <summary>
        /// Sets the item alignment for the shadow node.
        /// </summary>
        /// <param name="alignItems">The item alignment.</param>
        [ReactProp(ViewProps.AlignItems)]
        public void SetAlignItems(string alignItems)
        {
            AlignItems = EnumHelpers.ParseNullable<CSSAlign>(alignItems) ?? CSSAlign.Stretch;
        }

        /// <summary>
        /// Sets the content justification.
        /// </summary>
        /// <param name="justifyContent">The content justification.</param>
        [ReactProp(ViewProps.JustifyContent)]
        public void SetJustifyContent(string justifyContent)
        {
            JustifyContent = EnumHelpers.ParseNullable<CSSJustify>(justifyContent) ?? CSSJustify.FlexStart;
        }

        /// <summary>
        /// Sets the margins of the shadow node.
        /// </summary>
        /// <param name="index">The spacing type index.</param>
        /// <param name="margin">The margin value.</param>
        [ReactPropGroup(
            ViewProps.Margin,
            ViewProps.MarginVertical,
            ViewProps.MarginHorizontal,
            ViewProps.MarginLeft,
            ViewProps.MarginRight,
            ViewProps.MarginTop,
            ViewProps.MarginBottom,
            DefaultSingle = Undefined)]
        public void SetMargins(int index, float margin)
        {
            SetMargin(ViewProps.PaddingMarginSpacingTypes[index], margin);
        }

        /// <summary>
        /// Sets the paddings of the shadow node.
        /// </summary>
        /// <param name="index">The spacing type index.</param>
        /// <param name="padding">The padding value.</param>
        [ReactPropGroup(
            ViewProps.Padding,
            ViewProps.PaddingVertical,
            ViewProps.PaddingHorizontal,
            ViewProps.PaddingLeft,
            ViewProps.PaddingRight,
            ViewProps.PaddingTop,
            ViewProps.PaddingBottom,
            DefaultSingle = Undefined)]
        public void SetPaddings(int index, float padding)
        {
            SetPaddingCore(ViewProps.PaddingMarginSpacingTypes[index], padding);
        }

        /// <summary>
        /// Sets the border width properties for the shadow node.
        /// </summary>
        /// <param name="index">The border spacing type index.</param>
        /// <param name="borderWidth">The border width.</param>
        [ReactPropGroup(
            ViewProps.BorderWidth,
            ViewProps.BorderLeftWidth,
            ViewProps.BorderRightWidth,
            ViewProps.BorderTopWidth,
            ViewProps.BorderBottomWidth,
            DefaultSingle = Undefined)]
        public void SetBorderWidth(int index, float borderWidth)
        {
            SetBorder(ViewProps.BorderSpacingTypes[index], borderWidth);
        }

        /// <summary>
        /// Sets the position of the shadow node.
        /// </summary>
        /// <param name="position">The position.</param>
        [ReactProp(ViewProps.Position)]
        public void SetPosition(string position)
        {
            PositionType = EnumHelpers.ParseNullable<CSSPositionType>(position) ?? CSSPositionType.Relative;
        }

        /// <summary>
        /// Sets if the view should send an event on layout.
        /// </summary>
        /// <param name="shouldNotifyOnLayout">
        /// The flag signaling if the view should sent an event on layout.
        /// </param>
        [ReactProp("onLayout")]
        public void SetShouldNotifyOnLayout(bool shouldNotifyOnLayout)
        {
            ShouldNotifyOnLayout = shouldNotifyOnLayout;
        }

        /// <summary>
        /// Sets the padding of the shadow node.
        /// </summary>
        /// <param name="spacingType">The spacing type.</param>
        /// <param name="padding">The padding value.</param>
        /// <remarks>
        /// Override this virtual method if the view has custom padding behavior.
        /// </remarks>
        protected virtual void SetPaddingCore(CSSSpacingType spacingType, float padding)
        {
            SetPadding(spacingType, padding);
        }
    }
}
