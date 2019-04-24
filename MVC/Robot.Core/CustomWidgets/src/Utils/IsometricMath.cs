using Lime;

namespace Robot.Core.Common.Utils
{
	public static class IsometricMath
	{
		private static readonly Matrix32 classicWorldToScreen = Matrix32.Scaling(1.0f, 0.5f);
		private static readonly Matrix32 classicScreenToWorld = classicWorldToScreen.CalcInversed();

		public static ref readonly Matrix32 DefaultWorldToScreenProjection => ref classicWorldToScreen;
		public static ref readonly Matrix32 DefaultScreenToWorldProjection => ref classicScreenToWorld;

		public static float GetRotationForDirection(
			in Vector2 begin, in Vector2 end, Matrix32? correction = null)
		{
			return GetRotationForDirection(end - begin, correction);
		}

		private static float GetRotationForDirection(Vector2 direction, Matrix32? correction = null)
		{
			if (correction.HasValue) {
				direction *= correction.Value;
			}
			return 45.0f - Mathf.Atan2(direction.Y, direction.X) * Mathf.RadToDeg;
		}
	}
}
