using Lime;

namespace Robot.Core.Common.Extensions
{
	public static class WindowInputExtensions
	{
		public static bool WasTripleTouch(this WindowInput windowInput)
		{
			return
				windowInput.IsTripleTouch() &&
				(windowInput.WasTouchBegan(0) ||
				windowInput.WasTouchBegan(1) ||
				windowInput.WasTouchBegan(2));
		}

		public static bool IsTripleTouch(this WindowInput windowInput)
		{
			return
				windowInput.IsTouching(0) &&
				windowInput.IsTouching(1) &&
				windowInput.IsTouching(2);
		}
	}
}
