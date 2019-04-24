using Lime;
using Robot.Layer1.Common.ActivitiesSystem;

namespace Robot.Layer3.Common
{
	public class ActivityMain : Activity
	{
		protected override void OnStart(Activity previousActivity)
		{
			AddFragment((Fragment) Node.CreateFromAssetBundle("Assets_Fragments/Scenes/FragmentMain"), AddMode.Normal);
		}

		protected override void OnStop(Activity nextActivity)
		{

		}
	}
}
