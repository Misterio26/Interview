using System;
using System.Collections.Generic;
using Lime;
using Tangerine.Core;

namespace Tangerine.UI
{
	public static class UIProcessors
	{
		public static IEnumerator<object> PickColorProcessor(Widget widget, Action<Color4> setter)
		{
			var input = CommonWindow.Current.Input;
			var drag = new DragGesture();
			widget.Gestures.Add(drag);
			yield return null;
			while (true) {
				if (widget.GloballyEnabled && drag.WasRecognized()) {
					using (Document.Current.History.BeginTransaction()) {
						input.ConsumeKey(Key.Mouse0);
						WidgetContext.Current.Root.Input.ConsumeKey(Key.Mouse0);
						while (!drag.WasEnded()) {
							Utils.ChangeCursorIfDefault(Cursors.Pipette);
							setter(ColorPicker.PickAtCursor());
							yield return null;
						}
						Utils.ChangeCursorIfDefault(MouseCursor.Default);
						Document.Current.History.CommitTransaction();
					}
				}
				yield return null;
			}
		}
	}
}
