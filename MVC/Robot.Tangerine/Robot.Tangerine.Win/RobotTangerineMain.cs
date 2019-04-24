using System;
using Lime;
using Tangerine;

namespace Robot.Tangerine.Win
{
	internal static class RobotTangerineMain
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			string [] resources = thisExe.GetManifestResourceNames();
			Lime.Application.Initialize(new ApplicationOptions {
				RenderingBackend = RenderingBackend.OpenGL
			});
			TangerineApp.Initialize(args);
			Lime.Application.Run();
		}
	}
}
