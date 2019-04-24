using Lime;
using Robot.Layer4.Common.Native;

namespace Robot.Layer4.Win
{
	public class NativeEnvironment : INativeEnvironment
	{
		public bool IsDisplaySimulationAllowed => true;

		public bool IsTopSafeAreaRequired => false;

		public AssetBundle CreateMainAssetBundle()
		{
			return new PackedAssetBundle("Data.Win");
		}

		public void ShowInspectorFor(Node node)
		{
		}
	}
}
