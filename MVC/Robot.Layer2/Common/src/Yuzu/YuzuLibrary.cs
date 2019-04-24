using Yuzu;
using Yuzu.Json;

namespace Robot.Layer2.Common.Yuzu
{
	public static class YuzuLibrary
	{
		private static Lime.Yuzu forCommon;
		private static Lime.Yuzu forGameProgress;
		private static Lime.Yuzu forSettings;

		public static Lime.Yuzu ForCommon => forCommon ?? (forCommon = CreateForCommon());
		public static Lime.Yuzu ForGameProgress => forGameProgress ?? (forGameProgress = CreateForGameProgress());
		public static Lime.Yuzu ForSettings => forSettings ?? (forSettings = CreateForSettings());

		public static Lime.Yuzu CreateForCommon()
		{
			return CreateDefault();
		}

		public static Lime.Yuzu CreateForGameProgress()
		{
			return CreateDefault();
		}

		public static Lime.Yuzu CreateForSettings()
		{
			return CreateDefault();
		}

		private static Lime.Yuzu CreateDefault()
		{
			return new Lime.Yuzu(
				new CommonOptions {
					TagMode = TagMode.Aliases,
					AllowEmptyTypes = true,
					CheckForEmptyCollections = true,
					AllowUnknownFields = true,
				},
				new JsonSerializeOptions {
					ArrayLengthPrefix = false,
					Indent = "\t",
					FieldSeparator = "\n",
					SaveRootClass = true,
					Unordered = true,
					MaxOnelineFields = 8,
					BOM = true,
				}
			);
		}
	}
}
