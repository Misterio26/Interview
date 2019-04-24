using System;
using System.Collections.Generic;
using Lime;
using Robot.Layer1.Common.GameSystem;

namespace Robot.Layer1.Common.ActivitiesSystem
{
	public partial class ActivityManager
	{
		public enum State
		{
			None,
			ToNext,
			Idle,
		}

		public static ActivityManager Instance { get; private set; }

		internal readonly GameRoot Root;
		internal readonly GameWindow Window;

		private Activity currentActivity;
		private Activity nextActivity;

		private readonly TaskList taskList = new TaskList();

		private ActivityManager(GameRoot root, GameWindow window)
		{
			Root = root;
			Window = window;
		}

		private void Initialize()
		{
			taskList.Add(StartStateProcessor());
		}

		public void OnFrameUpdating(float delta)
		{
			taskList.Update(delta);
		}

		private IEnumerator<object> StartStateProcessor()
		{
			while (true) {
				if (nextActivity != null) {
					if (currentActivity != null) {
						yield return currentActivity.WillBecomeStopped(nextActivity);
					}

					var savedActivity = currentActivity;

					savedActivity?.BeforeStartOnStop(currentActivity);

					currentActivity = nextActivity;
					nextActivity = null;
					currentActivity.Start(savedActivity);

					savedActivity?.Stop(currentActivity);
				} else {
					yield return null;
				}
			}
			// ReSharper disable once IteratorNeverReturns
		}

		public void ShowActivity<T>() where T : Activity, new()
		{
			nextActivity = new T();
		}

		public void ShowActivity<T, E>(E extras = null) where T : Activity<E>, new() where E : class
		{
			var activity = new T();
			nextActivity = activity;
			activity.Extras = extras;
		}

		public Fragment.Transition BeginTransition(ITransitionSource source)
		{
			Fragment closestFragment = null;
			Node lookingNode = source.Self;
			while (lookingNode != null) {
				if (lookingNode is Fragment fragment) {
					closestFragment = fragment;
					break;
				}
				lookingNode = lookingNode.Parent;
			}

			if (source.Self.TopmostParent != Root || closestFragment == null) {
				return null;
			}

			return closestFragment.BeginTransition(source);
		}

		// public T GetActivity<T>() where T : Activity {
		// 	return currentActivity as T;
		// }

		public Activity GetActivity() {
			return currentActivity;
		}

		public State GetState()
		{
			return nextActivity != null
				? State.ToNext
				: currentActivity == null
					? State.None
					: State.Idle;
		}

		public void OnBackButtonPressed(BackButtonEventArgs eventArgs)
		{
			currentActivity?.OnBackButtonPressed(eventArgs);
		}


		public static void Activate(GameRoot gameRoot, GameWindow window)
		{
			if (Instance != null) {
				throw new InvalidOperationException($"{nameof(ActivityManager)} already activated");
			}
			Instance = new ActivityManager(gameRoot, window);
			Instance.Initialize();
		}
	}
}
