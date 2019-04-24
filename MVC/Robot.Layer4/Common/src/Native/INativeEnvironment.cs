using Lime;

namespace Robot.Layer4.Common.Native
{
	public interface INativeEnvironment
	{
		bool IsDisplaySimulationAllowed { get; }

		bool IsTopSafeAreaRequired { get; }

		AssetBundle CreateMainAssetBundle();

		void ShowInspectorFor(Node node);
	}
}
