using System;
using System.IO;
using Lime;
using Robot.Layer1.Common.ActivitiesSystem;
using Robot.Layer1.Common.Native;

namespace Robot.Layer1.Common.GameSystem
{

	public interface IGameExtension
	{
		GameEnvironmentConfig OnGetGameEnvironment();
		void OnGameStart();
		void OnInitializing();
		AssetBundle OnCreateAssetBundle();
		void OnLoadFonts();
		float OnFrameUpdating(float delta);
		void OnFrameUpdated(float delta);
		void OnRootRendered();
		void OnRootAfterEndFrame();
		void OnBackButtonPressed(BackButtonEventArgs eventArgs);
	}

	public class Game
	{
		public static Game Instance { get; private set; }
		public static GameWindow Window => Instance.window;
		public static GameRoot Root => Instance.root;
		public static GameWindowAndRootCoordinator Coordinator => Instance.coordinator;
		public static WindowInput Input => Window.DirectWindow.Input;

		private readonly object uiSync = new object();

		private IGameExtension gameExtension;

		private GameWindow window;
		private GameRoot root;
		private GameWindowAndRootCoordinator coordinator;

		public int FrameIndex { get; private set; }

		public static void Activate(IGameExtension gameExtension)
		{
			if (Instance != null) {
				throw new InvalidOperationException("Game already activated");
			}
			Instance = new Game();
			Instance.Initialize(gameExtension);

			Instance.gameExtension.OnGameStart();
		}

		private void Initialize(IGameExtension gameExtension)
		{
			GameEnvironment.Activate(gameExtension.OnGetGameEnvironment());
			this.gameExtension = gameExtension;

			var options = new WindowOptions {
				Title = GameEnvironment.ApplicationName,
				UseTimer = false,
				AsyncRendering = true
			};

			window = new GameWindow(new Window(options));

			root = new GameRoot(window.DirectWindow) {
				Layer = RenderChain.LayerCount - 1
			};

			window.Activate();
			coordinator = new GameWindowAndRootCoordinator(window, root, GameEnvironment.PreferedLandsacapeRootSize);
			coordinator.Activate();

			window.DirectWindow.Updating += OnUpdateFrame;
			window.DirectWindow.Rendering += OnRenderFrame;

			AssetBundle.Current = gameExtension.OnCreateAssetBundle();

			gameExtension.OnInitializing();

			gameExtension.OnLoadFonts();
			LoadDictionary();
		}

		private void LoadDictionary()
		{
			string fileName = GameEnvironment.DictionaryFileName;

			// ReSharper disable once RedundantLogicalConditionalExpressionOperand
			if (NativeEnvironment.Instance.IsLocalLocalizationDictionaryAllowed && File.Exists(fileName)) {
				Localization.Dictionary.Clear();
				using (var stream = new FileStream(fileName, FileMode.Open)) {
					Localization.Dictionary.ReadFromStream(new LocalizationDictionaryTextSerializer(), stream);
				}

				return;
			}

			if (!AssetBundle.Current.FileExists(fileName)) {
				return;
			}

			Localization.Dictionary.Clear();
			using (var stream = AssetBundle.Current.OpenFile(fileName)) {
				Localization.Dictionary.ReadFromStream(new LocalizationDictionaryTextSerializer(), stream);
			}
		}

		private void OnUpdateFrame(float delta)
		{
			lock (uiSync) {
				FrameIndex++;
				delta = gameExtension.OnFrameUpdating(delta);
				root.Update(delta);
				gameExtension.OnFrameUpdated(delta);
				root.PrepareToRender();
			}
		}

		private void OnRenderFrame()
		{
			lock (uiSync) {
				RendererWrapper.Current.BeginFrame();
				SetupViewportAndProjectionMatrix();
				root.RenderAll();
				gameExtension.OnRootRendered();
				RendererWrapper.Current.EndFrame();
				gameExtension.OnRootAfterEndFrame();
			}
		}

		private void SetupViewportAndProjectionMatrix()
		{
			Vector2 windowSize;
			Vector2 windowPos;

			var customViewport = window.GetLetterboxingViewport();

			if (customViewport == null) {
				windowSize = window.DirectWindow.ClientSize;
				windowPos = Vector2.Zero;
			} else {
				float pixelScale = window.DirectWindow.PixelScale;
				windowSize = new Vector2(
					customViewport.Value.Width / pixelScale,
					customViewport.Value.Height / pixelScale
				);
				windowPos = new Vector2(customViewport.Value.X / pixelScale, customViewport.Value.Y / pixelScale);
			}

			var scaling = Matrix32.Scaling(root.Width / windowSize.X, root.Height / windowSize.Y);
			window.DirectWindow.Input.MousePositionTransform = Matrix32.Translation(-windowPos) * scaling;


			RendererWrapper.Current.SetOrthogonalProjection(0, 0, root.Width, root.Height);
			root.CustomViewport = customViewport;
		}

		public void OnBackButtonPressed(BackButtonEventArgs eventArgs)
		{
			lock (uiSync) {
				gameExtension.OnBackButtonPressed(eventArgs);
			}
		}
	}
}
