using System.Runtime.CompilerServices;

namespace Robot.Core.Common.Utils
{
	public class UniqueObjectIdsHelper
	{
		public static readonly UniqueObjectIdsHelper Instance = new UniqueObjectIdsHelper();

		private readonly ConditionalWeakTable<object, LongWrapper> map;

		private long lastId;

		private UniqueObjectIdsHelper()
		{
			map = new ConditionalWeakTable<object, LongWrapper>();
		}

		public long GetIdFor(object forObject)
		{
			lock (map) {
				if (!map.TryGetValue(forObject, out var res)) {
					res = new LongWrapper(++lastId);
					map.Add(forObject, res);
				}
				return res.Value;
			}
		}

		private class LongWrapper
		{
			public readonly long Value;

			public LongWrapper(long value)
			{
				Value = value;
			}
		}
	}
}
