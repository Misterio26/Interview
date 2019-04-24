using System;
using Lime;
using Robot.Layer1.Common.GameSystem;
using Robot.Layer4.Common;

namespace Robot.Win
{
	public static class Application
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Lime.Application.Initialize(new ApplicationOptions {
				DecodeAudioInSeparateThread = false,
				RenderingBackend = RenderingBackend.ES20
			});

			Core.Common.Native.NativeEnvironment.Initialize(new Robot.Core.Win.NativeEnvironment());
			Layer1.Common.Native.NativeEnvironment.Initialize(new Robot.Layer1.Win.NativeEnvironment());
			Layer2.Common.Native.NativeEnvironment.Initialize(new Robot.Layer2.Win.NativeEnvironment());
			Layer4.Common.Native.NativeEnvironment.Initialize(new Robot.Layer4.Win.NativeEnvironment());
			Game.Activate(new GameRobot());
			Lime.Application.Run();
		}
	}
}
