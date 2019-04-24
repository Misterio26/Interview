using System;
using System.Collections.Generic;

namespace Robot.Core.Common.Utils
{
	public static class CollectionExtensions
	{
		public static int BinarySearch<TElement, TSearch>(this IList<TElement> list, in TSearch search)
			where TElement : IComparable<TSearch>
		{
			if (list == null) {
				throw new ArgumentNullException(nameof(list));
			}
			for (int left = 0, right = list.Count - 1; left <= right;) {
				int middle = (int) Math.Floor((left + right) / 2.0);
				var checkValue = list[middle];
				switch (Math.Sign(checkValue.CompareTo(search))) {
					case -1: left = middle + 1; break;
					case +1: right = middle - 1; break;
					default: {
						return middle;
					}
				}
			}
			return -1;
		}
	}
}
