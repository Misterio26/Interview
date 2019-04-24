using System;
using System.Collections.Generic;
using Lime;
using Robot.Core.Common.Extensions;

namespace Robot.Layer1.Common.ActivitiesSystem
{
	public abstract class Activity<E> : Activity
	{
		public E Extras;
	}

	public abstract class Activity
	{

		public enum AddMode
		{
			Normal,
			Unique
		}

		/// <summary>
		/// Важный момент, что Root появляется только в Start, тогда есть гарантия, что Fragment не будет
		/// добавлен в сразу же созданный Activity, потому что если это разрешить, то придется откладывать
		/// this.ExpandToContainer(); и OnAppeared(); во Fragment, потому что если добавить Fragment к Root
		/// от активити, который еще не был Start, то эти действия оказываются невалидными
		/// </summary>
		public Frame Root { get; private set; }

		private bool started;

		internal readonly List<Fragment> Fragments = new List<Fragment>();

		public bool IsOrientationPortrait => ActivityManager.Instance.Window.IsOrientationPortrait();

		public void AddFragment(Fragment fragment, AddMode mode, Fragment afterFragment = null)
		{
			if (!started) {
				return;
			}

			if (mode == AddMode.Unique) {
				foreach (var fragmentEl in Fragments) {
					if (fragmentEl.GetType() == fragment.GetType()) return;
				}
			}

			fragment.SetActivity(this, afterFragment);
		}

		public Fragment GetFrontFragment(Func<Fragment, bool> condition = null)
		{
			return FindFrontFragment(condition);
		}

		internal void Start(Activity previousActivity)
		{
			if (started) {
				throw new InvalidOperationException("Activity already started");
			}
			started = true;

			Root = new Frame();
			Root.Id = "ActivityRoot";
			ActivityManager.Instance.Root.AddNode(Root);
			Root.ExpandToContainer();

			ActivityManager.Instance.Window.Subscribe(OnWindowChanged);

			OnStart(previousActivity);
		}

		internal void BeforeStartOnStop(Activity nextActivity)
		{
			OnStopping(nextActivity);
		}

		internal void Stop(Activity nextActivity)
		{
			started = false;

			ActivityManager.Instance.Window.Unsubscribe(OnWindowChanged);

			foreach (var fragment in Fragments.ToArray()) {
				fragment.RemoveFromActivityImmediately();
			}
			Root.Unlink();
			Root = null;

			OnStop(nextActivity);
		}

		internal void RefillScopeRestrictions()
		{
			foreach (var fragment in Fragments) {
				if (fragment.IsInputIntercepted) {
					fragment.Input.DerestrictScope();
					fragment.Input.RestrictScope();
				}
			}
		}

		private Fragment GetFrontInterceptor()
		{
			return FindFrontFragment(fragment => fragment.IsInputIntercepted);
		}

		private Fragment FindFrontFragment(Func<Fragment, bool> condition)
		{
			int maxLayer = -1;
			for (int i = Fragments.Count - 1; i >= 0; i--) {
				var fragment = Fragments[i];
				if ((condition == null || condition.Invoke(fragment)) && maxLayer < fragment.Layer) {
					maxLayer = fragment.Layer;
				}
			}

			for (int i = Fragments.Count - 1; i >= 0; i--) {
				var fragment = Fragments[i];
				if ((condition == null || condition.Invoke(fragment)) && maxLayer == fragment.Layer) {
					return fragment;
				}
			}

			return null;
		}

		public IEnumerator<object> WillBecomeStopped(Activity nextActivity)
		{
			yield return OnWillBecomeStopped(nextActivity);
		}

		private void OnWindowChanged()
		{
			foreach (var fragment in Fragments.ToArray()) {
				fragment.OnActivityStateChanged();
			}
		}

		protected abstract void OnStart(Activity previousActivity);

		protected abstract void OnStop(Activity nextActivity);

		protected virtual void OnStopping(Activity nextActivity)
		{
		}

		internal void NotifyFragmentsChanged(Fragment changedFragment, bool isAttached)
		{
			OnFragmentsChanged();
		}

		protected virtual void OnFragmentsChanged()
		{
		}

		protected virtual IEnumerator<object> OnWillBecomeStopped(Activity nextActivity)
		{
			yield break;
		}

		protected internal virtual void OnBackButtonPressed(BackButtonEventArgs eventArgs)
		{
			var lastInputInterceptor = GetFrontInterceptor();
			if (lastInputInterceptor != null) {
				lastInputInterceptor.OnBackButtonPressed(eventArgs);
			} else if (Fragments.Count > 0) {
				var tmpFragments = new List<Fragment>(Fragments);
				tmpFragments.Sort((l, r) => {
					if (l.Layer == r.Layer) {
						return Fragments.IndexOf(l) - Fragments.IndexOf(r);
					}
					return l.Layer - r.Layer;
				});

				for (int i = tmpFragments.Count - 1; i >= 0; i--) {
					var fragment = tmpFragments[i];
					fragment.OnBackButtonPressed(eventArgs);
					if (eventArgs.Handled) {
						break;
					}
				}
			}
		}

	}
}
