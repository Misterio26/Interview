using System;
using Lime;

namespace Robot.Layer1.Common.GameSystem
{
	/// <summary>
	/// Part of refactored class ResolutionChanger.
	/// Only sets root size according to window size when it is changed.
	/// </summary>
	public class GameWindowAndRootCoordinator
	{
		private readonly GameWindow window;
		private readonly GameRoot root;
		private readonly Size rootSizePreferedLandscape;

		private bool active;

		private bool needFitRootSizeToWindow;

		private float rootScale = 1;

		public float RootScale => rootScale;

		internal GameWindowAndRootCoordinator(GameWindow window, GameRoot root, Size rootSizePreferedLandscape)
		{
			this.window = window;
			this.root = root;
			this.rootSizePreferedLandscape = rootSizePreferedLandscape;
		}

		internal void Activate()
		{
			if (active) {
				throw new InvalidOperationException("GameWindowAndRootCoordinator already activated");
			}
			active = true;
			window.Subscribe(OnResolutionChanged);
		}

		public bool NeedFitRootSizeToWindow
		{
			get { return needFitRootSizeToWindow; }
			set
			{
				if (needFitRootSizeToWindow == value) return;
				needFitRootSizeToWindow = value;
				ApplyState();
			}
		}

		private void OnResolutionChanged()
		{
			ApplyState();
		}

		private void ApplyState()
		{
			var mainWindowResolution = window.GetResolution();

			if (NeedFitRootSizeToWindow) {
				root.Size = new Vector2(mainWindowResolution.Width, mainWindowResolution.Height);
			} else {
				if (mainWindowResolution.Width > 0 && mainWindowResolution.Height > 0) {
					int rootWidth;
					int rootHeight;
					if (window.IsOrientationPortrait()) {
						rootWidth = rootSizePreferedLandscape.Height;
						rootHeight = (int) (rootSizePreferedLandscape.Height *
							((float) mainWindowResolution.Height / mainWindowResolution.Width));
						rootScale = (float) mainWindowResolution.Width / rootSizePreferedLandscape.Height;
					} else {
						rootWidth = (int) (rootSizePreferedLandscape.Height *
							((float) mainWindowResolution.Width / mainWindowResolution.Height));
						rootHeight = rootSizePreferedLandscape.Height;
						rootScale = (float) mainWindowResolution.Height / rootSizePreferedLandscape.Height;
					}

					root.Size = new Vector2(rootWidth, rootHeight);
				}
			}
		}

	}
}
