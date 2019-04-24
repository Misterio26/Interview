using System;
using Lime;
using Robot.Core.Common;

namespace Robot.Core.Common.Extensions
{
	public static partial class WidgetExtensions
	{
		public static void ExpandToContainerWithAnchors(this Widget widget, Widget targetContainer)
		{
			widget.Anchors = Anchors.None;
			widget.Size = targetContainer.Size;
			widget.Anchors = Anchors.LeftRightTopBottom;
		}

		public static void ExpandToContainer(this Widget widget)
		{
			if (widget.ParentWidget != null) {
				AlignToContainer(
					widget, HAlignment.Justify, VAlignment.Justify, resetAnchorsByAlignment: true);
			}
		}

		public static void AlignToContainer(
			this Widget widget, HAlignment hAlignment, VAlignment vAlignment, bool resetAnchorsByAlignment)
		{
			if (resetAnchorsByAlignment) {
				AlignToContainer(widget, new AlignmentOptions(
					hAlignment,
					vAlignment,
					AlignmentOptions.ScaleOption.Ignore,
					AlignmentOptions.RotationOption.ResetToZero,
					AlignmentOptions.AnchorsOption.ResetByAlignment));
			} else {
				AlignToContainer(widget, hAlignment, vAlignment, widget.Anchors);
			}
		}

		public static void AlignToContainer(
			this Widget widget, HAlignment hAlignment, VAlignment vAlignment, Anchors anchors)
		{
			AlignToContainer(widget, new AlignmentOptions(
				hAlignment,
				vAlignment,
				AlignmentOptions.ScaleOption.Ignore,
				AlignmentOptions.RotationOption.Keep,
				anchors));
		}

		public static void AlignToContainer(this Widget widget, AlignmentOptions options)
		{
			if (widget.ParentWidget == null) {
				return;
			}

			if (options.Rotation == AlignmentOptions.RotationOption.ResetToZero) {
				widget.Rotation = 0.0f;
			}

			var scale = Vector2.One;
			switch (options.Scale) {
				case AlignmentOptions.ScaleOption.Apply: scale = widget.Scale; break;
				case AlignmentOptions.ScaleOption.ResetToOne: widget.Scale = Vector2.One; break;
			}

			AlignToParentHorizontally(widget, options.HorizontalAlignment, scale.X);
			AlignToParentVertically(widget, options.VerticalAlignment, scale.Y);
			widget.Anchors = options.Anchors;
		}

		private static void AlignToParentHorizontally(
			Widget widget, HAlignment hAlignment, float scaleX)
		{
			switch (hAlignment) {
				case HAlignment.Left:
					widget.X = widget.Pivot.X * widget.Width * scaleX;
					break;
				case HAlignment.Right:
					widget.X = widget.ParentWidget.Width -
						((1.0f - widget.Pivot.X) * widget.Width * scaleX);
					break;
				case HAlignment.Center: {
					AlignToParentCenter(widget, scaleX);
					break;
				}
				case HAlignment.Justify: {
					widget.Width = widget.ParentWidget.Width / scaleX;
					AlignToParentCenter(widget, scaleX);
					break;
				}
				default:
					throw new NotImplementedException();
			}

			void AlignToParentCenter(Widget widgetToAlign, float scale)
			{
				widgetToAlign.X = widgetToAlign.ParentWidget.Width / 2.0f -
					(0.5f - widgetToAlign.Pivot.X) * widgetToAlign.Width * scale;
			}
		}

		private static void AlignToParentVertically(
			Widget widget, VAlignment vAlignment, float scaleY)
		{
			switch (vAlignment) {
				case VAlignment.Top:
					widget.Y = widget.Pivot.Y * widget.Height * scaleY;
					break;
				case VAlignment.Bottom:
					widget.Y = widget.ParentWidget.Height -
						((1.0f - widget.Pivot.Y) * widget.Height * scaleY);
					break;
				case VAlignment.Center:
					AlignToParentCenter(widget, scaleY);
					break;
				case VAlignment.Justify:
					widget.Height = widget.ParentWidget.Height / scaleY;
					AlignToParentCenter(widget, scaleY);
					break;
				default:
					throw new NotImplementedException();
			}

			void AlignToParentCenter(Widget widgetToAlign, float scale)
			{
				widgetToAlign.Y = widgetToAlign.ParentWidget.Height / 2.0f -
					(0.5f - widgetToAlign.Pivot.Y) * widgetToAlign.Height * scale;
			}
		}
	}
}
