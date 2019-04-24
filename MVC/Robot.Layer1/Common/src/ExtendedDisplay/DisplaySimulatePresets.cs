using System;
using Lime;
using Robot.Core.Common;
using Robot.Layer1.Common.GameSystem;

namespace Robot.Layer1.Common.ExtendedDisplay
{
	/// <summary>
	/// Part of refactored class ResolutionChanger. <para/>
	/// 1. Supports ability to switch between default resolutions <para/>
	/// 2. Sets title according resolution <para/>
	/// 3. Can draw overlay <para/>
	/// Needed only on Win or Mac. <para/>
	/// </summary>
	public class DisplaySimulatePresets
	{

		public class Storage
		{
			public readonly DisplayPreset Preset;
			public readonly bool IsPortrait;

			public Storage(DisplayPreset preset, bool isPortrait)
			{
				Preset = preset;
				IsPortrait = isPortrait;
			}
		}

		public static DisplaySimulatePresets Instance { get; private set; }

		private int presetIndex;
		private bool isOrientationPortrait;

		private bool overlayVisible = true;
		private string overlayFrameLandscapePath;
		private ITexture overlayFrameLandscape;
		private string overlayFramePortraitPath;
		private ITexture overlayFramePortrait;
		private bool overlayRotated;

		private readonly GameWindow window;
		private readonly GameRoot root;
		private readonly DisplayPreset[] presets;


		private DisplaySimulatePresets(GameWindow window, GameRoot root, DisplayPreset[] presets)
		{
			this.window = window;
			this.root = root;
			this.presets = presets;
		}

		public static bool IsActive => Instance != null;

		public static void Activate(GameWindow window, GameRoot root, DisplayPreset[] presets)
		{
			if (Instance != null) {
				throw new InvalidOperationException("DisplaySimulatePresets already activated");
			}
			Instance = new DisplaySimulatePresets(window, root, presets);
			Instance.Initialize();
		}

		private void Initialize()
		{
			window.Subscribe(() => FixTitle(false));
			Apply();
		}

		private void FixTitle(bool forceCurrent)
		{
			var effectiveWindowResolution = window.GetResolution();
			if (window.IsOrientationPortrait()) {
				effectiveWindowResolution = new Size(effectiveWindowResolution.Height, effectiveWindowResolution.Width);
			}

			int findIndex = forceCurrent
				? presetIndex
				: Array.FindIndex(presets, preset => preset.SizeLandscape == effectiveWindowResolution);

			window.DirectWindow.Title = findIndex < 0
				? $"Custom ({effectiveWindowResolution})"
				: $"{presets[findIndex].Name} ({presets[findIndex].SizeLandscape})";
		}

		public void Apply()
		{
			var preset = GetCurrentPreset();
			window.SetSimulateIsOrientationPortrait(isOrientationPortrait);

			if (isOrientationPortrait) {
				var resolution = new Size(preset.SizeLandscape.Height, preset.SizeLandscape.Width);
				window.SetSimulateSafeAreaFactors(preset.SafeAreaPortraitFactors);
				window.SetResolution(resolution, center: true);
				window.SetLetterboxingSize(resolution);
			} else {
				window.SetSimulateSafeAreaFactors(preset.SafeAreaLandscapeFactors);
				window.SetResolution(preset.SizeLandscape, center: true);
				window.SetLetterboxingSize(preset.SizeLandscape);
			}

			FixTitle(forceCurrent: true);
		}

		public void SwitchToNextPreset(bool isReversed)
		{
			int step = isReversed ? -1 : 1;
			presetIndex = (presetIndex + step).Wrap(0, presets.Length - 1);
			Log.Instance.Info("Current resolution: " + presets[presetIndex].Name);
			Apply();
		}

		public void SwitchToPreset(DisplayPreset preset)
		{
			int index = Array.IndexOf(presets, preset);
			if (index == -1) {
				Log.Instance.Info($"Resolution preset \"{preset.Name}\" not found.");
			} else {
				presetIndex = index;
				Log.Instance.Info($"Current resolution: {presets[presetIndex].Name}");
				Apply();
			}
		}

		public void ToggleOrientation()
		{
			isOrientationPortrait = !isOrientationPortrait;
			Apply();
		}

		public DisplayPreset GetCurrentPreset()
		{
			return presets[presetIndex];
		}

		public Storage Store()
		{
			return new Storage(GetCurrentPreset(), isOrientationPortrait);
		}

		public void Restore(Storage storage)
		{
			isOrientationPortrait = storage.IsPortrait;
			SwitchToPreset(storage.Preset);
		}

		public bool OverlayRotated => overlayRotated;

		public void RotateOverlay()
		{
			overlayRotated = !overlayRotated;
		}

		public void ToggleOvelayVisibility()
		{
			overlayVisible = !overlayVisible;
		}

		public void OnFrameRendered()
		{
			if (!overlayVisible) {
				return;
			}
			var currentPreset = GetCurrentPreset();

			if (currentPreset.OverlayLandscape == null || currentPreset.OverlayPortrait == null) {
				return;
			}

			ITexture overlayFrame;
			if (overlayFramePortrait == null || overlayFramePortraitPath != currentPreset.OverlayPortrait) {
				overlayFramePortraitPath = currentPreset.OverlayPortrait;
				overlayFramePortrait = new SerializableTexture(currentPreset.OverlayPortrait);
			}
			if (overlayFrameLandscape == null || overlayFrameLandscapePath != currentPreset.OverlayLandscape) {
				overlayFrameLandscapePath = currentPreset.OverlayLandscape;
				overlayFrameLandscape = new SerializableTexture(currentPreset.OverlayLandscape);
			}

			var scale = Vector2.One;
			var translation = Vector2.One;

			if (window.IsOrientationPortrait()) {
				overlayFrame = overlayFramePortrait;
				if (overlayRotated) {
					scale = new Vector2(1, -1);
					translation = new Vector2(0, root.Size.Y);
				}
			} else {
				overlayFrame = overlayFrameLandscape;
				if (overlayRotated) {
					scale *= new Vector2(-1, 1);
					translation += new Vector2(root.Size.X, 0);
				}
			}

			Renderer.Transform1 = Matrix32.TransformationRough(Vector2.Zero, scale, 0, translation);
			Renderer.Blending = Blending.Alpha;
			Renderer.Shader = ShaderId.Diffuse;
			Renderer.DrawSprite(overlayFrame, Color4.White, Vector2.Zero, root.Size, Vector2.Zero, Vector2.One);
		}
	}
}
