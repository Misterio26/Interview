using System;
using Lime;

namespace Robot.Layer1.Common.ActivitiesSystem
{
	public partial class ActivityManager
	{
		public interface ITransitionSource
		{
			Widget Self { get; }
			bool IsActive();
			void OnComplete();
		}

		public class DelegatableTransitionSource : ITransitionSource
		{

			private readonly Func<bool> isActive;
			private readonly Action onComplete;

			public Widget Self { get; }

			public DelegatableTransitionSource(Widget self, Func<bool> isActive, Action onComplete)
			{
				Self = self;
				this.isActive = isActive;
				this.onComplete = onComplete;
			}

			bool ITransitionSource.IsActive()
			{
				return isActive.Invoke();
			}

			void ITransitionSource.OnComplete()
			{
				onComplete?.Invoke();
			}
		}
	}
}
