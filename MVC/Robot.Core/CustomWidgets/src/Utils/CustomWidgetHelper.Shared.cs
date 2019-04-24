using System;
using Lime;

namespace Robot.Core.Common.Utils
{
	public interface ICustomWidgetHelperOwner
	{
		bool IsLightweightUpdateForbidden { get; }

		void OnUpdating(float delta);
		void OnUpdated(float delta);
	}

	public partial class CustomWidgetHelper<T> where T:Node, ICustomWidgetHelperOwner
	{
		private readonly T owner;
		private readonly bool isWithTanOnBuilt;

		public CustomWidgetHelper(T owner, bool isWithTanOnBuilt)
		{
			this.owner = owner;
			this.isWithTanOnBuilt = isWithTanOnBuilt;
		}

		public void Update(float delta, Action<float> baseUpdate) => UpdatePartial(delta, baseUpdate);

		partial void UpdatePartial(float delta, Action<float> baseUpdate);

		public CustomWidgetHelper<T> Clone(T clonedOwner)
		{
			return new CustomWidgetHelper<T>(clonedOwner, isWithTanOnBuilt);
		}
	}
}
