using System;
using Lime;

namespace Robot.Layer1.Common.GameSystem
{
	/// <summary>
	/// Part of refactored class ResolutionChanger. <para/>
	/// Extends standard window functionality with: <para/>
	/// 1. Simulates orientation change for Win and Mac <para/>
	/// 2. Provides letterboxing functionality <para/>
	/// 3. Preservates aspect ratio for sizes which outs from display <para/>
	/// 4. Implements Subscription pattern <para/>
	/// </summary>
	public class GameWindow
	{
		private event Action OnChanged;

		public readonly IWindow DirectWindow;

		private bool active;
		private bool? simulateOrientationIsPortrait;
		private Rectangle? simulateSafeAreaFactors;
		private bool isLetterboxing;
		private Size? letterboxingSize;

		/// <summary>
		/// Workaround to set correct ClientSize after full screen
		/// </summary>
		private Size? sizeAfterFullScreen;
		private bool? centerAfterFullScreen;


		internal GameWindow(IWindow window)
		{
			DirectWindow = window;
		}

		internal void Activate()
		{
			if (active) {
				throw new InvalidOperationException($"{nameof(GameWindow)} already activated");
			}
			active = true;

			DirectWindow.Resized += rotated => {
				OnChanged?.Invoke();

				// workaround @see sizeAfterFullScreen
				bool needCenter = centerAfterFullScreen == true;
				sizeAfterFullScreen = null;
				centerAfterFullScreen = null;
				if (needCenter) {
					DirectWindow.Updating += WindowCenterOnUpdating;
				}
			};

			DirectWindow.SafeAreaInsetsChanged += (safeAreaInsets) => OnChanged?.Invoke();
		}

		private void WindowCenterOnUpdating(float delta)
		{
			DirectWindow.Center();
			DirectWindow.Updating -= WindowCenterOnUpdating;
		}

		public bool FixedSize
		{
			get => DirectWindow.FixedSize;
			set => DirectWindow.FixedSize = value;
		}

		public WindowState State
		{
			get => DirectWindow.State;
			set
			{
				// workaround @see sizeAfterFullScreen
				var wasSizeAfterFullScreen = sizeAfterFullScreen;
				var wasCenterAfterFullScreen = centerAfterFullScreen;
				sizeAfterFullScreen = null;
				centerAfterFullScreen = null;

				DirectWindow.State = value;

				// workaround @see sizeAfterFullScreen
				if (DirectWindow.State == WindowState.Normal && wasSizeAfterFullScreen != null) {
					SetResolution(wasSizeAfterFullScreen.Value, wasCenterAfterFullScreen == true);
				}
			}
		}

		public void SetIsLetterboxing(bool isLetterboxing)
		{
			if (this.isLetterboxing == isLetterboxing) return;
			this.isLetterboxing = isLetterboxing;
			OnChanged?.Invoke();
		}

		public void SetLetterboxingSize(Size? letterboxingSize)
		{
			if (this.letterboxingSize == letterboxingSize) return;
			this.letterboxingSize = letterboxingSize;
			OnChanged?.Invoke();
		}

		public void SetResolution(Size resolution, bool center)
		{
			var size = new Vector2(resolution.Width, resolution.Height);
			if (DirectWindow.ClientSize == size) return;
			DirectWindow.ClientSize = size;

			if (DirectWindow.ClientSize != size) {
				var fixedSize = FitSize(resolution, (Size) DirectWindow.ClientSize);
				DirectWindow.ClientSize = new Vector2(fixedSize.Width, fixedSize.Height);
			}

			if (center) DirectWindow.Center();

			if (DirectWindow.State != WindowState.Normal) {
				sizeAfterFullScreen = resolution;
				centerAfterFullScreen = center;
			}
		}

		public Size GetResolution()
		{
			var clientSize = (Size) DirectWindow.ClientSize;
			if (!isLetterboxing || letterboxingSize == null || letterboxingSize == clientSize) {
				return clientSize;
			}

			return FitSize(letterboxingSize.Value, clientSize);
		}

		public WindowRect? GetLetterboxingViewport()
		{
			var clientSize = (Size) DirectWindow.ClientSize;
			if (!isLetterboxing || letterboxingSize == null || letterboxingSize == clientSize) {
				return null;
			}

			var boxedSize = FitSize(letterboxingSize.Value, clientSize);

			var scaledSize = new Size(
				(int) (boxedSize.Width * DirectWindow.PixelScale),
				(int) (boxedSize.Height * DirectWindow.PixelScale)
			);
			int width = (clientSize.Width - boxedSize.Width) / 2;
			int height = (clientSize.Height - boxedSize.Height) / 2;

			return new WindowRect {
				X = width,
				Y = height,
				Width = scaledSize.Width,
				Height = scaledSize.Height
			};
		}

		public bool IsOrientationPortrait()
		{
			return simulateOrientationIsPortrait ?? (
				Application.CurrentDeviceOrientation == DeviceOrientation.Portrait ||
				Application.CurrentDeviceOrientation == DeviceOrientation.PortraitUpsideDown
			);
		}

		public void SetSimulateIsOrientationPortrait(bool? simulateOrientationIsPortrait)
		{
			if (this.simulateOrientationIsPortrait == simulateOrientationIsPortrait) return;
			this.simulateOrientationIsPortrait = simulateOrientationIsPortrait;
			OnChanged?.Invoke();
		}

		public Rectangle GetSafeAreaInsets()
		{
			if (!simulateSafeAreaFactors.HasValue) {
				return DirectWindow.SafeAreaInsets;
			}

			var resolution = GetResolution();
			var factors = simulateSafeAreaFactors.Value;

			return new Rectangle(
				factors.Left * resolution.Width,
				factors.Top * resolution.Height,
				factors.Right * resolution.Width,
				factors.Bottom * resolution.Height
			);
		}

		public void SetSimulateSafeAreaFactors(Rectangle? insets)
		{
			if (simulateSafeAreaFactors != insets) {
				simulateSafeAreaFactors = insets;
				OnChanged?.Invoke();
			}
		}

		private static Size FitSize(Size preferedSize, Size overalSize)
		{
			if (preferedSize.Width == 0 || preferedSize.Height == 0) {
				return Size.Zero;
			}

			float scaleWidth = (float) overalSize.Width / preferedSize.Width;
			float scaleHeight = (float) overalSize.Height / preferedSize.Height;

			float scaleUse = Math.Min(scaleWidth, scaleHeight);

			if (scaleUse >= 1) {
				return preferedSize;
			}

			return new Size((int) (preferedSize.Width * scaleUse), (int) (preferedSize.Height * scaleUse));
		}

		public void Subscribe(Action onChanged)
		{
			OnChanged += onChanged;
			onChanged();
		}

		public void Unsubscribe(Action onChanged)
		{
			OnChanged -= onChanged;
		}
	}
}
