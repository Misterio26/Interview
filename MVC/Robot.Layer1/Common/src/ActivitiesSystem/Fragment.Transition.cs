using Lime;
using Robot.Core.Common.Utils;

namespace Robot.Layer1.Common.ActivitiesSystem
{
	public abstract partial class Fragment
	{
		public class Transition
		{
			private readonly ActivityManager.ITransitionSource transitionSource;
			private readonly Node initialSourceSelf;
			private readonly Node initialRoot;
			private bool used;

			public Fragment Fragment { get; }

			internal Transition(ActivityManager.ITransitionSource transitionSource, Fragment initialFragment)
			{
				this.transitionSource = transitionSource;
				Fragment = initialFragment;

				initialSourceSelf = transitionSource.Self;
				initialRoot = transitionSource.Self.TopmostParent;
				if (initialFragment != FindClosestFragment()) {
					throw new System.Exception("Inconsistent state initialFragment != closestFragment");
				}
			}

			public void Update()
			{
				if (AreAllConditionsMet()) {
					return;
				}

				Free();
			}

			public bool IsFinished()
			{
				return used;
			}

			internal bool AreAllConditionsMet()
			{
				return
					transitionSource.IsActive() &&
					initialSourceSelf == transitionSource.Self &&
					initialRoot == transitionSource.Self.TopmostParent &&
					Fragment.stateHelper.Value != StatesLibrary.TransitionalVisibilityWidgetState.Detached &&
					Fragment == FindClosestFragment();
			}

			private void Free()
			{
				if (used) {
					return;
				}
				used = true;

				transitionSource.OnComplete();
				Fragment.EndTransition(this);
			}

			private Fragment FindClosestFragment()
			{
				Fragment closestFragment = null;
				Node lookingNode = transitionSource.Self;
				while (lookingNode != null) {
					if (lookingNode is Fragment fragment) {
						closestFragment = fragment;
						break;
					}
					lookingNode = lookingNode.Parent;
				}

				return closestFragment;
			}
		}
	}
}
