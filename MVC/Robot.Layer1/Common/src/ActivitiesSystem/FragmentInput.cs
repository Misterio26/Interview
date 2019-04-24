namespace Robot.Layer1.Common.ActivitiesSystem
{
	public enum InputFragmentState
	{
		Detached,
		Idle,
		Ready,
		Working,
		Pending
	}

	public abstract partial class FragmentInput
	{
		public abstract InputFragmentState InputState { get; }
	}
}
