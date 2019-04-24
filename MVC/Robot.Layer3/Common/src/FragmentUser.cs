namespace Robot.Layer3.Common
{
	public partial class FragmentUser
	{
		private readonly int userId;

		public FragmentUser()
		{
			// Do not remove.
		}

		public FragmentUser(int userId)
		{
			this.userId = userId;
		}

		protected override int OnGetLayer()
		{
			return 0;
		}
	}
}
