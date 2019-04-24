using System;
using Lime;

namespace Robot.Core.Common.Extensions
{
	public static partial class WidgetExtensions
	{
		public class AlignmentOptions
		{
			public enum ScaleOption
			{
				Ignore,
				Apply,
				ResetToOne
			}

			public enum RotationOption
			{
				Keep,
				ResetToZero
			}

			public enum AnchorsOption
			{
				ResetByAlignment
			}


			public HAlignment HorizontalAlignment { get; }
			public VAlignment VerticalAlignment { get; }

			public ScaleOption Scale { get; }
			public RotationOption Rotation { get; }
			public Anchors Anchors { get; }

			public AlignmentOptions(
				HAlignment hAlignment,
				VAlignment vAlignment,
				ScaleOption scaleOption,
				RotationOption rotationOption,
				Anchors anchors)
			{
				HorizontalAlignment = hAlignment;
				VerticalAlignment = vAlignment;
				Scale = scaleOption;
				Rotation = rotationOption;
				Anchors = anchors;
			}

			public AlignmentOptions(
				HAlignment hAlignment,
				VAlignment vAlignment,
				ScaleOption scaleOption,
				RotationOption rotationOption,
				AnchorsOption anchorsOption) : this(
					hAlignment,
					vAlignment,
					scaleOption,
					rotationOption,
					(anchorsOption == AnchorsOption.ResetByAlignment)
						? GetAnchorsByAlignment(hAlignment, vAlignment)
						: Anchors.None)
			{
			}

			private static Anchors GetAnchorsByAlignment(HAlignment hAlignment, VAlignment vAlignment)
			{
				Anchors anchors = Anchors.None;
				switch (hAlignment) {
					case HAlignment.Left: anchors |= Anchors.Left; break;
					case HAlignment.Right: anchors |= Anchors.Right; break;
					case HAlignment.Center: anchors |= Anchors.CenterH; break;
					case HAlignment.Justify: anchors |= Anchors.LeftRight; break;
					default: throw new NotImplementedException();
				}
				switch (vAlignment) {
					case VAlignment.Top: anchors |= Anchors.Top; break;
					case VAlignment.Bottom: anchors |= Anchors.Bottom; break;
					case VAlignment.Center: anchors |= Anchors.CenterV; break;
					case VAlignment.Justify: anchors |= Anchors.TopBottom; break;
					default: throw new NotImplementedException();
				}
				return anchors;
			}
		}
	}
}
