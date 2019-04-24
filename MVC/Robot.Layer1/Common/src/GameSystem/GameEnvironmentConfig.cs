using Lime;

namespace Robot.Layer1.Common.GameSystem
{
	public struct GameEnvironmentConfig
	{
		public readonly string VendorName;
		public readonly string ApplicationVersion;
		public readonly string ApplicationName;

		public readonly Size PreferedLansacapeRootSize;

		public GameEnvironmentConfig(string vendorName, string applicationVersion, string applicationName,
			Size preferedLansacapeRootSize)
		{
			VendorName = vendorName;
			ApplicationVersion = applicationVersion;
			ApplicationName = applicationName;
			PreferedLansacapeRootSize = preferedLansacapeRootSize;
		}
	}
}
