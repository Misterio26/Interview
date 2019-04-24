using Lime;

namespace Robot.Layer1.Common.ExtendedDisplay
{
	public readonly struct DisplayPreset
	{
		public readonly string Name;
		public readonly Size SizeLandscape;
		public readonly Rectangle SafeAreaLandscapeFactors;
		public readonly Rectangle SafeAreaPortraitFactors;
		public readonly string OverlayLandscape;
		public readonly string OverlayPortrait;

		public DisplayPreset(string name, in Size sizeLandscape)
			: this(name, in sizeLandscape, null, null, in Rectangle.Empty, in Rectangle.Empty)
		{
		}

		public DisplayPreset(
			string name,
			in Size sizeLandscape,
			string overlayLandscape,
			string overlayPortrait,
			in Rectangle safeAreaLandscapeFactors,
			in Rectangle safeAreaPortraitFactors)
		{
			Name = name;
			SizeLandscape = sizeLandscape;
			OverlayLandscape = overlayLandscape;
			OverlayPortrait = overlayPortrait;
			SafeAreaLandscapeFactors = safeAreaLandscapeFactors;
			SafeAreaPortraitFactors = safeAreaPortraitFactors;
		}
	}
}
