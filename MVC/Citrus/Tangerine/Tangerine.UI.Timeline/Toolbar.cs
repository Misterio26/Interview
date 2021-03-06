using System;
using System.Collections.Generic;
using System.Linq;
using Lime;
using Tangerine.Core;
using Tangerine.UI;

namespace Tangerine.UI.Timeline
{
	public class Toolbar
	{
		public Widget RootWidget { get; private set; }

		public Toolbar()
		{
			RootWidget = new Widget {
				Padding = new Thickness(2, 0),
				MinMaxHeight = Metrics.ToolbarHeight,
				MinWidth = TimelineMetrics.ToolbarMinWidth,
				Presenter = new WidgetFlatFillPresenter(ColorTheme.Current.Toolbar.Background),
				Layout = new HBoxLayout { DefaultCell = new DefaultLayoutCell(Alignment.Center) },
				Nodes = {
					CreateAnimationModeButton(),
					CreateAutoKeyframesButton(),
					CreateNewFolderButton(),
					CreateCurveEditorButton(),
					CreateTimelineCursorLockButton(),
					CreateAnimationStretchButton(),
					CreateSlowMotionButton(),
					CreateFrameProgressionButton(),
					CreateAnimationSelector(),
					CreateAnimationIndicator(),
					new Widget(),
					CreateExitButton(),
					CreateLockAnimationButton(),
					CreateEyeButton(),
					CreateLockButton(),
				}
			};
		}

		TimelineUserPreferences UserPreferences => TimelineUserPreferences.Instance;
		CoreUserPreferences CoreUserPreferences => Core.CoreUserPreferences.Instance;

		Widget CreateAnimationSelector()
		{
			var ddl = new ThemedDropDownList { MinWidth = 100 };
			ddl.Changed += arg => {
				if (arg.ChangedByUser) {
					Document.Current.History.DoTransaction(() => {
						var animation = (Animation)arg.Value;
						Core.Operations.SetProperty.Perform(Document.Current, nameof(Document.SelectedAnimation), animation);
					});
				}
			};
			ddl.AddChangeWatcher(() => CalcAnimationIdsHash(), _ => RefreshSelector(ddl));
			ddl.AddChangeWatcher(() => Document.Current.SelectedAnimation, _ => RefreshSelectedAnimation(ddl));
			return ddl;
		}

		List<Animation> tmpAnimations = new List<Animation>();

		void RefreshSelector(CommonDropDownList ddl)
		{
			try {
				Document.Current.GetAnimations(tmpAnimations);
				ddl.Items.Clear();
				foreach (var a in tmpAnimations) {
					var item = a.IsLegacy
						? new CommonDropDownList.Item(a.Id ?? "Primary", null)
						: new CommonDropDownList.Item(a.Id, a);
					ddl.Items.Add(item);
				}
				RefreshSelectedAnimation(ddl);
			} finally {
				tmpAnimations.Clear();
			}
		}

		void RefreshSelectedAnimation(CommonDropDownList ddl)
		{
			var item = ddl.Items.FirstOrDefault(i => i.Value == Document.Current.SelectedAnimation);
			if (item != null) {
				ddl.Index = ddl.Items.IndexOf(item);
			}
		}

		int CalcAnimationIdsHash()
		{
			unchecked {
				try {
					Document.Current.GetAnimations(tmpAnimations);
					int result = 17;
					foreach (var a in tmpAnimations) {
						var id = a.Id;
						result = (result * 31) + (id != null ? id.GetHashCode() : 0);
					}
					return result;
				} finally {
					tmpAnimations.Clear();
				}
			}
		}

		ToolbarButton CreateAnimationModeButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.AnimationMode")) { Tip = "Animation mode" };
			button.AddChangeWatcher(() => CoreUserPreferences.AnimationMode, i => button.Checked = i);
			button.Clicked += () => CoreUserPreferences.AnimationMode = !CoreUserPreferences.AnimationMode;
			button.Components.Add(new DocumentationComponent("AnimationMode"));
			return button;
		}

		ToolbarButton CreateCurveEditorButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Curve")) { Tip = "Edit curves" };
			button.AddChangeWatcher(() => UserPreferences.EditCurves, i => button.Checked = i);
			button.Clicked += () => UserPreferences.EditCurves = !UserPreferences.EditCurves;
			button.Components.Add(new DocumentationComponent("EditCurves"));
			return button;
		}

		Widget CreateAnimationIndicator()
		{
			var t = new ThemedSimpleText { Padding = new Thickness(4, 0), MinWidth = 100 };
			Action f = () => {
				var distance = Timeline.Instance.Ruler.MeasuredFrameDistance;
				t.Text = (distance == 0) ?
					$"Col : {Document.Current.AnimationFrame}" :
					$"Col : {Document.Current.AnimationFrame} {Timeline.Instance.Ruler.MeasuredFrameDistance:+#;-#;0}";
			};
			t.AddChangeWatcher(() => Document.Current.AnimationFrame, _ => f());
			t.AddChangeWatcher(() => Timeline.Instance.Ruler.MeasuredFrameDistance, _ => f());
			return t;
		}

		ToolbarButton CreateAutoKeyframesButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Key")) { Tip = "Automatic keyframes" };
			button.AddChangeWatcher(() => CoreUserPreferences.AutoKeyframes, i => button.Checked = i);
			button.Clicked += () => CoreUserPreferences.AutoKeyframes = !CoreUserPreferences.AutoKeyframes;
			button.Components.Add(new DocumentationComponent("AutomaticKeyframes"));
			return button;
		}

		ToolbarButton CreateNewFolderButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Tools.NewFolder")) { Tip = "Create folder" };
			button.AddTransactionClickHandler(() => {
				var newFolder = new Folder { Id = "Folder" };
				Core.Operations.InsertFolderItem.Perform(newFolder);
				Core.Operations.ClearRowSelection.Perform();
				Core.Operations.SelectRow.Perform(Document.Current.GetRowForObject(newFolder));
			});
			button.Components.Add(new DocumentationComponent("CreateFolder"));
			return button;
		}

		ToolbarButton CreateExitButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.ExitContainer")) { Tip = "Exit current container (backspace)" };
			button.AddTransactionClickHandler(Core.Operations.LeaveNode.Perform);
			button.Updating += _ => button.Enabled = Core.Operations.LeaveNode.IsAllowed();
			button.Components.Add(new DocumentationComponent("ExitContainer"));
			return button;
		}

		ToolbarButton CreateEyeButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Eye")) { Tip = "Show widgets" };
			button.AddTransactionClickHandler(() => {
				var nodes = !RootWidget.Input.IsKeyPressed(Key.Shift) ? Document.Current.Container.Nodes.ToList() : Document.Current.SelectedNodes().ToList();
				var visibility = NodeVisibility.Hidden;
				if (nodes.All(i => i.EditorState().Visibility == NodeVisibility.Hidden)) {
					visibility = NodeVisibility.Shown;
				} else if (nodes.All(i => i.EditorState().Visibility == NodeVisibility.Shown)) {
					visibility = NodeVisibility.Default;
				}
				foreach (var node in nodes) {
					Core.Operations.SetProperty.Perform(node.EditorState(), nameof(NodeEditorState.Visibility), visibility);
				}
			});
			button.Components.Add(new DocumentationComponent("ShowWidgets"));
			return button;
		}

		ToolbarButton CreateLockButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Lock")) { Tip = "Lock widgets" };
			button.AddTransactionClickHandler(() => {
				var nodes = !RootWidget.Input.IsKeyPressed(Key.Shift) ? Document.Current.Container.Nodes.ToList() : Document.Current.SelectedNodes().ToList();
				var locked = nodes.All(i => !i.EditorState().Locked);
				foreach (var node in nodes) {
					Core.Operations.SetProperty.Perform(node.EditorState(), nameof(NodeEditorState.Locked), locked);
				}
			});
			button.Components.Add(new DocumentationComponent("LockWidgets"));
			return button;
		}

		ToolbarButton CreateLockAnimationButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.AnimationEnabled")) { Tip = "Lock animation" };
			button.AddTransactionClickHandler(() => {
				var nodes = !RootWidget.Input.IsKeyPressed(Key.Shift) ? Document.Current.Container.Nodes.ToList() : Document.Current.SelectedNodes().ToList();
				var enable = !nodes.All(IsAnimationEnabled);
				foreach (var node in nodes) {
					foreach (var animator in node.Animators) {
						Core.Operations.SetProperty.Perform(animator, nameof(IAnimator.Enabled), enable);
					}
				}
			});
			button.Components.Add(new DocumentationComponent("LockAnimation"));
			return button;
		}

		ToolbarButton CreateAnimationStretchButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.AnimationStretch")) { Tip = "Animation stretch mode" };
			button.AddChangeWatcher(() => UserPreferences.AnimationStretchMode, i => button.Checked = i);
			button.Clicked += () => UserPreferences.AnimationStretchMode = !UserPreferences.AnimationStretchMode;
			button.Components.Add(new DocumentationComponent("AnimationStretch.md"));
			return button;
		}

		private ToolbarButton CreateSlowMotionButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.SlowMotionMode")) { Tip = "Slow motion mode (~)" };
			button.AddChangeWatcher(() => UserPreferences.SlowMotionMode, i => button.Checked = i);
			button.Clicked += () => UserPreferences.SlowMotionMode = !UserPreferences.SlowMotionMode;
			button.Components.Add(new DocumentationComponent("SlowMotionMode.md"));
			return button;
		}

		private ToolbarButton CreateFrameProgressionButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.FrameProgression")) { Tip = "Frame progression mode" };
			button.AddChangeWatcher(() => CoreUserPreferences.ShowFrameProgression, i => button.Checked = i);
			button.Clicked += () => CoreUserPreferences.ShowFrameProgression = !CoreUserPreferences.ShowFrameProgression;
			return button;
		}

		private ToolbarButton CreateTimelineCursorLockButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.TimelineCursorLock")) { Tip = "Lock timeline cursor" };
			button.AddChangeWatcher(() => CoreUserPreferences.LockTimelineCursor, i => button.Checked = i);
			button.Clicked += () => CoreUserPreferences.LockTimelineCursor = !CoreUserPreferences.LockTimelineCursor;
			return button;
		}

		static bool IsAnimationEnabled(IAnimationHost animationHost)
		{
			foreach (var a in animationHost.Animators) {
				if (!a.Enabled)
					return false;
			}
			return true;
		}
	}
}

