using System;
using System.Collections.Generic;
using Lime;
using Robot.Core.Common.Extensions;
using Robot.Core.Common.Utils;

namespace Robot.Layer1.Common.ActivitiesSystem
{
	public abstract partial class Fragment
	{
		private const string FragmentTag = "Fragment";

		public enum PresentationMode
		{
			Normal,
			Exclusive,
		}

		private static readonly object transitionReason = new object();

		public event Action<Fragment> StateChanged;

		protected Activity Context => ownerActivity;
		public bool IsAttached => stateHelper.Value != StatesLibrary.TransitionalVisibilityWidgetState.Detached;
		public bool IsClosed =>
			stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Hided ||
			stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Detached;
		public bool IsActive =>
			stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Showing ||
			stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Showed;
		public StatesLibrary.TransitionalVisibilityWidgetState VisibilityState => stateHelper.Value;

		public PresentationMode Mode
		{
			get { return mode; }
			set => interceptionFillStateHelper.SetField(ref mode, value);
		}

		internal bool IsInputIntercepted => inputIntercepted;

		private Activity ownerActivity;

		private Activity targetOwnerActivity;
		private Fragment targetAfterFragment;
		private bool isImmediate;

		private bool inputIntercepted;
		private bool localized;

		private object activeShowAnimationContext;
		private object activeCloseAnimationContext;

		private StateHelper<StatesLibrary.TransitionalVisibilityWidgetState> stateHelper;

		private PresentationMode mode = PresentationMode.Normal;

		private Frame glassWallFrame;
		private readonly HashSet<object> disableReasons = new HashSet<object>();

		private readonly List<Transition> activeTransitions = new List<Transition>();

		private FillStateHelper interceptionFillStateHelper;

		public Fragment()
		{
			Initialize();
		}

		public Fragment(Vector2 position) : base(position)
		{
			Initialize();
		}

		private void Initialize()
		{
			Tag = FragmentTag;
			stateHelper = new StateHelper<StatesLibrary.TransitionalVisibilityWidgetState>(
				StatesLibrary.TransitionalVisibilityWidgetState.Detached,
				ProcessStateInner, OnStateChangedInner
			);
			interceptionFillStateHelper = new FillStateHelper(
				false, true, OnInterceptionFillState, OnInterceptionCleanup
			);
			Size = new Vector2(1024, 768);
		}

		internal void SetActivity(Activity activity, Fragment afterFragment)
		{
			targetOwnerActivity = activity;
			targetAfterFragment = afterFragment;
			stateHelper.ProcessState();
		}

		public void RemoveFromActivity()
		{
			targetOwnerActivity = null;
			targetAfterFragment = null;
			stateHelper.ProcessState();
		}

		public void RemoveFromActivityImmediately()
		{
			targetOwnerActivity = null;
			isImmediate = true;
			stateHelper.ProcessState();
		}

		public void Enable(object reason)
		{
			if (disableReasons.Remove(reason) && disableReasons.Count == 0) {
				glassWallFrame?.Unlink();
			}
		}

		public void Disable(object reason)
		{
			if (disableReasons.Add(reason) && disableReasons.Count == 1) {
				if (glassWallFrame == null) {
					glassWallFrame = new Frame {
						HitTestTarget = true,
						HitTestMethod = HitTestMethod.BoundingRect
					};
				}
				PushNode(glassWallFrame);
				glassWallFrame.ExpandToContainer();
			}
		}

		internal Transition BeginTransition(ActivityManager.ITransitionSource source)
		{
			var transition = new Transition(source, this);

			if (!transition.AreAllConditionsMet()) {
				return null;
			}

			if (!activeTransitions.Contains(transition)) {
				activeTransitions.Add(transition);
				if (activeTransitions.Count == 1) {
					Disable(transitionReason);
				}
			}

			transition.Update();

			return transition;
		}

		private void EndTransition(Transition transition)
		{
			if (activeTransitions.Remove(transition) && activeTransitions.Count == 0) {
				Enable(transitionReason);
			}
		}

		private void UpdateTransitions()
		{
			foreach (var transition in activeTransitions.ToArray()) {
				transition.Update();
			}
		}

		protected virtual IEnumerator<object> OnStartShowAnimation()
		{
			string animation = OnGetShowAnimationName();
			if (animation != null && TryRunAnimation(animation)) {
				yield return this;
			}
		}

		protected virtual IEnumerator<object> OnStartCloseAnimation()
		{
			string animation = OnGetHideAnimationName();
			if (animation != null && TryRunAnimation(animation)) {
				yield return this;
			}
		}

		protected virtual void OnForceStopTransitionAnimation()
		{
			DefaultAnimation.IsRunning = false;
		}

		protected internal virtual void OnActivityStateChanged()
		{
			this.ExpandToContainer();
			ApplyOrientation();

			void ApplyOrientation()
			{
				string animationName = ownerActivity.IsOrientationPortrait ? "~Portrait" : "~Landscape";
				string preferredAnimationNameCustom = OnGetCustomRotationAnimation();
				foreach (var node in Descendants) {
					if (preferredAnimationNameCustom == null || !node.TryRunAnimation(preferredAnimationNameCustom)) {
						node.TryRunAnimation(animationName);
					}
				}
			}
		}

		public override void Update(float delta)
		{
			UpdateTransitions();

			base.Update(delta);
		}

		protected virtual string OnGetShowAnimationName()
		{
			return "Show";
		}

		protected virtual string OnGetHideAnimationName()
		{
			return "Hide";
		}

		protected virtual string OnGetCustomRotationAnimation()
		{
			return null;
		}

		protected internal virtual void OnBackButtonPressed(BackButtonEventArgs eventArgs)
		{
		}

		protected virtual void OnAppeared()
		{
		}

		protected virtual void OnDisappeared()
		{
		}

		protected virtual void OnDidAppear()
		{
		}

		protected virtual void OnWillDisappear()
		{
		}

		protected abstract int OnGetLayer();

		private void OnStateChangedInner(
			StatesLibrary.TransitionalVisibilityWidgetState newValue,
			StatesLibrary.TransitionalVisibilityWidgetState oldValue
		)
		{
			NotifyFragmentChanged();

			switch (oldValue) {
				case StatesLibrary.TransitionalVisibilityWidgetState.Detached:
					OnAppeared();
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Showed:
					OnWillDisappear();
					break;
			}

			switch (newValue) {
				case StatesLibrary.TransitionalVisibilityWidgetState.Detached:
					UpdateTransitions();
					OnDisappeared();
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Showed:
					OnDidAppear();
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Hiding:
					BeginTransition(new ActivityManager.DelegatableTransitionSource(
						this, () => stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Hiding, null
					));
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Showing:
					BeginTransition(new ActivityManager.DelegatableTransitionSource(
						this, () => stateHelper.Value == StatesLibrary.TransitionalVisibilityWidgetState.Showing, null
					));
					break;
			}

			StateChanged?.Invoke(this);
		}

		private void ProcessStateInner()
		{
			switch (stateHelper.Value) {
				case StatesLibrary.TransitionalVisibilityWidgetState.Detached:
					if (targetOwnerActivity != null) {
						isImmediate = false;

						Attach(targetOwnerActivity, targetAfterFragment);
						targetAfterFragment = null;

						var showAnimationContext = new object();
						activeShowAnimationContext = showAnimationContext;
						Tasks.Add(StartShowAnimation(() => {
							if (showAnimationContext == activeShowAnimationContext) {
								activeShowAnimationContext = null;
								stateHelper.ProcessState();
							}
						}));
						// Workaround to avoid blinking if initial state is not fist frame of show animation.
						Update(0);

						stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Showing;
					}
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Showing:

					if (!ProcessCommon()) {
						if (activeShowAnimationContext == null) {
							stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Showed;
						}
					}

					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Showed:
					if (!ProcessCommon()) {
						if (targetOwnerActivity == null && !isImmediate) {
							var closeAnimationContext = new object();
							activeCloseAnimationContext = closeAnimationContext;
							Tasks.Add(StartCloseAnimation(() => {
								if (closeAnimationContext == activeCloseAnimationContext) {
									activeCloseAnimationContext = null;
									stateHelper.ProcessState();
								}
							}));
							// Workaround to avoid blinking if initial state is not fist frame of show animation.
							Update(0);

							stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Hiding;
						}
					}
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Hiding:
					if (!ProcessCommon()) {
						if (activeCloseAnimationContext == null) {
							stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Hided;
						}
					}
					break;
				case StatesLibrary.TransitionalVisibilityWidgetState.Hided:
					if (!ProcessCommon()) {
						if (targetOwnerActivity == null && !isImmediate) {
							Detach();
							stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Detached;
						}
					}
					break;
			}

			bool ProcessCommon()
			{
				if (targetOwnerActivity == null) {
					if (isImmediate) {
						Detach();
						stateHelper.Value = StatesLibrary.TransitionalVisibilityWidgetState.Detached;
						return true;
					}
				} else if (targetOwnerActivity != ownerActivity) {
					throw new InvalidOperationException(
						$"Fragment {this} already attached to another activity {ownerActivity}"
					);
				}

				return false;
			}
		}

		private void Attach(Activity activity, Fragment afterFragment)
		{
			ownerActivity = activity;
			int afterIndex;
			if (afterFragment != null && (afterIndex = ownerActivity.Fragments.IndexOf(afterFragment)) >= 0) {
				ownerActivity.Fragments.Insert(afterIndex + 1, this);
			} else {
				ownerActivity.Fragments.Add(this);
			}

			Layer = OnGetLayer();
			if (afterFragment != null && (afterIndex = ownerActivity.Root.Nodes.IndexOf(afterFragment)) >= 0) {
				ownerActivity.Root.Nodes.Insert(afterIndex, this);
			} else {
				ownerActivity.Root.PushNode(this);
			}

			if (!localized) {
				localized = true;

				string animationName = string.IsNullOrEmpty(AssetBundle.CurrentLanguage)
					? "~EN"
					: ("~" + AssetBundle.CurrentLanguage);
				foreach (var node in Descendants) {
					if (!node.TryRunAnimation(animationName)) {
						node.TryRunAnimation("~other");
					}
				}
			}

			OnActivityStateChanged();
			ownerActivity.NotifyFragmentsChanged(this, true);
			interceptionFillStateHelper.Start();
		}

		private void Detach()
		{
			var saveOwnerActivity = ownerActivity;

			if (saveOwnerActivity == null) {
				return;
			}

			saveOwnerActivity.Fragments.Remove(this);
			Unlink();
			ownerActivity = null;

			OnForceStopTransitionAnimation();
			foreach (var fragment in saveOwnerActivity.Fragments) {
				if (fragment == this) {
					continue;
				}
				fragment.OnFragmentsChanged();
			}
			saveOwnerActivity.NotifyFragmentsChanged(this, false);
			interceptionFillStateHelper.Stop();
		}

		private void NotifyFragmentChanged()
		{
			if (ownerActivity == null) {
				return;
			}
			foreach (var fragment in ownerActivity.Fragments) {
				if (fragment == this) {
					continue;
				}
				fragment.OnFragmentsChanged();
			}
		}

		protected virtual void OnFragmentsChanged()
		{
		}

		private IEnumerator<object> StartShowAnimation(Action onDone)
		{
			yield return OnStartShowAnimation();
			onDone();
		}

		private IEnumerator<object> StartCloseAnimation(Action onDone)
		{
			yield return OnStartCloseAnimation();
			onDone();
		}

		private void OnInterceptionCleanup()
		{
			if (inputIntercepted) {
				inputIntercepted = false;
				Input.DerestrictScope();
			}
		}

		private void OnInterceptionFillState(bool animated)
		{
			switch (mode) {
				case PresentationMode.Normal:
					if (inputIntercepted) {
						inputIntercepted = false;
						Input.DerestrictScope();
					}
					break;
				case PresentationMode.Exclusive:
					if (!inputIntercepted) {
						inputIntercepted = true;
						ownerActivity?.RefillScopeRestrictions();
					}
					break;
			}
		}
	}
}
