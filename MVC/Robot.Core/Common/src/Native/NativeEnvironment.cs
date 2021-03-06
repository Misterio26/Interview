using System;

namespace Robot.Core.Common.Native
{
	public static class NativeEnvironment
	{
		public static INativeEnvironment Instance { get; private set; }

		public static void Initialize(INativeEnvironment instance)
		{
			if (Instance != null) {
				throw new InvalidOperationException("NativeEnvironment already initialized");
			}
			Instance = instance;
		}
	}
}
