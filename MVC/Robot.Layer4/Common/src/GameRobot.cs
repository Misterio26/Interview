using Lime;
using Robot.Layer1.Common.ActivitiesSystem;
using Robot.Layer1.Common.ExtendedDisplay;
using Robot.Layer1.Common.GameSystem;
using Robot.Layer3.Common;
using Robot.Layer4.Common.Native;

namespace Robot.Layer4.Common
{
	public class GameRobot : IGameExtension
	{
		GameEnvironmentConfig IGameExtension.OnGetGameEnvironment()
		{
			return new GameEnvironmentConfig(
				"Game Forest",
				"1.0",
				"Review",
				new Size(1024, 768)
			);
		}

		void IGameExtension.OnGameStart()
		{
			ActivityManager.Instance.ShowActivity<ActivityMain>();
		}

		void IGameExtension.OnInitializing()
		{
			Game.Window.FixedSize = false;
			Game.Window.SetIsLetterboxing(true);

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (NativeEnvironment.Instance.IsDisplaySimulationAllowed) {
				// ReSharper disable once InconsistentNaming
				// ReSharper disable once PossibleLossOfFraction
				const float IPhoneXsMaxWindowWidth = 2668 / 3;
				// ReSharper disable once InconsistentNaming
				// ReSharper disable once PossibleLossOfFraction
				const float IPhoneXsMaxWindowHeight = 1242 / 3;

				DisplaySimulatePresets.Activate(
					Game.Window, Game.Root,
					new[] {
						new DisplayPreset("iPad", new Size(1024, 768)),
						new DisplayPreset("Wide Screen", new Size(1366, 768)),
						new DisplayPreset("iPhone 4", new Size(960, 640)),
						new DisplayPreset("iPhone 5", new Size(1136, 640)),
						new DisplayPreset("iPhone 6, 7, 8", new Size(1334, 750)),
						new DisplayPreset("iPhone 6, 7, 8 Plus", new Size(1920, 1080)),
						new DisplayPreset("Google Nexus 9 portrait", new Size(976, 768)),
						new DisplayPreset("Google Nexus 9 landscape", new Size(1024, 720)),
						new DisplayPreset("Galaxy S8", new Size(2960, 1440)),
						new DisplayPreset("Galaxy S8 /2", new Size(1480, 720)),
						new DisplayPreset(
							"IPhone XS Max /2",
							new Size(
								(int) (IPhoneXsMaxWindowWidth * 1.5f),
								(int) (IPhoneXsMaxWindowHeight * 1.5f)
							),
							"Group_Dev/Assets_DisplayOverlays/IPhoneXsMaxLandscape",
							"Group_Dev/Assets_DisplayOverlays/IPhoneXsMaxPortrait",
							new Rectangle {
								Left = 44 / IPhoneXsMaxWindowWidth,
								Right = 44 / IPhoneXsMaxWindowWidth,
								Bottom = 21 / IPhoneXsMaxWindowHeight
							},
							new Rectangle {
								Top = 44 / IPhoneXsMaxWindowWidth,
								Bottom = 34 / IPhoneXsMaxWindowWidth
							}
						),
						new DisplayPreset("Xperia Z4 Tablet", new Size(2560, 1600)),
						new DisplayPreset("LG G6", new Size(2880, 1440)),
					}
				);
			}

			ActivityManager.Activate(Game.Root, Game.Window);
		}

		AssetBundle IGameExtension.OnCreateAssetBundle()
		{
			return NativeEnvironment.Instance.CreateMainAssetBundle();
		}

		void IGameExtension.OnLoadFonts()
		{
			FontPool.Instance.AddFont("regular", new DynamicFont("Dynamic/Roboto-Regular.ttf"));
			FontPool.Instance.AddFont("bold", new DynamicFont("Dynamic/Roboto-Bold.ttf"));
			FontPool.Instance.AddFont("italic", new DynamicFont("Dynamic/Roboto-Italic.ttf"));
			FontPool.Instance.AddFont("bolditalic", new DynamicFont("Dynamic/Roboto-BoldItalic.ttf"));
		}

		float IGameExtension.OnFrameUpdating(float delta)
		{
			ActivityManager.Instance.OnFrameUpdating(delta);
			return delta;
		}

		public void OnFrameUpdated(float delta)
		{
		}

		void IGameExtension.OnRootRendered()
		{
			DisplaySimulatePresets.Instance?.OnFrameRendered();
		}

		public void OnRootAfterEndFrame()
		{
		}

		void IGameExtension.OnBackButtonPressed(BackButtonEventArgs eventArgs)
		{
			ActivityManager.Instance.OnBackButtonPressed(eventArgs);
		}
	}
}
