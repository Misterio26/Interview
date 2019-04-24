using System;
using System.Diagnostics;

namespace Robot.Core.Common.Extensions
{
	public static class StringExtensions
	{
		public static T ParseEnum<T>(this string value, bool ignoreCase = false) where T : struct
		{
			Trace.Assert(typeof(T).IsEnum, $"{nameof(T)} must be enum type");
			return (T) Enum.Parse(typeof(T), value, ignoreCase);
		}

		public static bool TryParseEnum<T>(this string value, out T result) where T : struct
		{
			return Enum.TryParse(value, out result);
		}

		public static bool TryParseEnum<T>(this string value, bool ignoreCase, out T result) where T : struct
		{
			return Enum.TryParse(value, ignoreCase, out result);
		}
	}
}
