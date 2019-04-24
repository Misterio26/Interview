using Lime;
using Robot.Layer1.Common.ActivitiesSystem;

namespace Robot.Layer3.Common
{
	public partial class FragmentMain
	{
		private Button user1024Button;

		protected override void OnBuilt()
		{
			base.OnBuilt();

			user1024Button = Find<Button>("ButtonUser1024");
			user1024Button.Clicked += () => {
				OnUserClicked(1024);
			};
		}

		protected override void OnAppeared()
		{
			base.OnAppeared();
		}

		protected override void OnDisappeared()
		{
			base.OnDisappeared();
		}

		private void OnUserClicked(int userId)
		{
			var fragmentUser = new FragmentUser(userId);
			CreateFromAssetBundle("Assets_Fragments/Scenes/FragmentUser", fragmentUser);
			Context.AddFragment(fragmentUser, Activity.AddMode.Normal);
		}

		protected override int OnGetLayer()
		{
			return 0;
		}
	}
}
