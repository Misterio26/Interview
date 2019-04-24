using System;
using System.IO;
using Lime;
using Environment = Lime.Environment;

namespace Robot.Layer1.Common.GameSystem
{
	public static class GameEnvironment
	{
		private static GameEnvironmentConfig? environmentConfig;

		public static string VendorName => environmentConfig?.VendorName;
		public static string ApplicationVersion => environmentConfig?.ApplicationVersion;
		public static string ApplicationName => environmentConfig?.ApplicationName;

		// ReSharper disable once PossibleInvalidOperationException
		public static Size PreferedLandsacapeRootSize => environmentConfig.Value.PreferedLansacapeRootSize;

		public const string ApplicationSettingsFileName = "AppSettings.dat";
		public const string ApplicationGameProgressFileName = "GameProgress.dat";
		public const string DictionaryFileName = "Dictionary.txt";

		public static void Activate(GameEnvironmentConfig environmentConfig)
		{
			if (GameEnvironment.environmentConfig != null) {
				throw new InvalidOperationException("GameEnvironment already activated");
			}
			GameEnvironment.environmentConfig = environmentConfig;
		}

		public static string GetDataCustomFilePath(string fileName)
		{
			return Path.Combine(GetDataDirectory(), fileName);
		}

		public static string GetDataSettingsFilePath()
		{
			return GetDataCustomFilePath(ApplicationSettingsFileName);
		}

		public static string GetDataProgressFilePath()
		{
			return GetDataCustomFilePath(ApplicationGameProgressFileName);
		}

		public static string GetDataDirectory()
		{
			return Environment.GetDataDirectory(VendorName, ApplicationName, ApplicationVersion);
		}
	}
}
