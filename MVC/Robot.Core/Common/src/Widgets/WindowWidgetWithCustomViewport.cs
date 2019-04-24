using Lime;

namespace Robot.Core.Common.Widgets
{
	public class WindowWidgetWithCustomViewport : WindowWidget
	{
		public WindowRect? CustomViewport { get; set; }

		public WindowWidgetWithCustomViewport(IWindow window) : base(window)
		{
		}

		protected override void Render(RenderObjectList renderObjects)
		{
			RendererWrapper.Current.Viewport = new Viewport(CustomViewport ?? GetViewport());
			renderObjects.Render();
		}
	}
}
