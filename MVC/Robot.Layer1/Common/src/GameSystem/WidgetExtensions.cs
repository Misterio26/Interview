using Lime;

namespace Robot.Layer1.Common.GameSystem
{
	public static class WidgetExtensions
	{
		public static bool IsInGame(this Widget widget)
		{
			return widget.TopmostParent == Game.Root;
		}
	}
}
