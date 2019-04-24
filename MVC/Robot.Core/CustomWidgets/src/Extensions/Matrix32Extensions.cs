using System;
using Lime;

namespace Robot.Core.Common.Extensions
{
	public static class Matrix32Extensions
	{
		public static bool EqualsApproximate(this float value, float other)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value == other) {
				return true;
			}

			if (Math.Abs(other) <= 0.00001 && Math.Abs(value) <= 0.00001) {
				return true;
			}

			return Math.Abs((double) value / other - 1) <= 0.000001;
		}

		public static bool EqualsApproximate(this double value, double other)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value == other) {
				return true;
			}

			if (Math.Abs(other) <= 0.000000001 && Math.Abs(value) <= 0.000000001) {
				return true;
			}

			return Math.Abs(value / other - 1) <= 0.00000000001;
		}

		public static bool EqualsApproximate(this Matrix32 matrix32, Matrix32 other)
		{
			return
				matrix32.Equals(other) ||
				matrix32.TX.EqualsApproximate(other.TX) &&
				matrix32.TY.EqualsApproximate(other.TY) &&
				matrix32.UX.EqualsApproximate(other.UX) &&
				matrix32.UY.EqualsApproximate(other.UY) &&
				matrix32.VX.EqualsApproximate(other.VX) &&
				matrix32.VY.EqualsApproximate(other.VY);
		}

		public static bool EqualsApproximate(this Vector2 value, Vector2 other)
		{
			return
				value.Equals(other) ||
				EqualsApproximate(value.X, other.X) &&
				EqualsApproximate(value.Y, other.Y);
		}
	}
}
