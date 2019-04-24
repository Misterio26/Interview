using System;
using System.Collections.Generic;
using System.Linq;
using Lime;

namespace Robot.Core.Common.Utils
{
	public static class AnimatorPropertyGetter
	{
		public interface IPropertyValueGetter<T>
		{
			IEnumerable<T> Values { get; }
			T GetValue(double time);
		}

		private class AnimatorValueGetter<T> : IPropertyValueGetter<T>
		{
			private readonly Animator<T> animator;
			public IEnumerable<T> Values => animator.ReadonlyKeys.Select(keyframe => keyframe.Value);

			public AnimatorValueGetter(Animator<T> source) => animator = source;
			public T GetValue(double time) => animator.CalcValue(time);
		}

		private class FixedValueGetter<T> : IPropertyValueGetter<T>
		{
			private readonly T value;
			public IEnumerable<T> Values => Enumerable.Repeat(value, 1);

			public FixedValueGetter(T fixedValue) => value = fixedValue;
			public T GetValue(double time) => value;
		}

		public static IPropertyValueGetter<T> Create<T>(
			Node owner, string propertyName, string animationId = null)
		{
			if (TryCreate<T>(owner, propertyName, animationId, out var getter)) {
				return getter;
			}

			throw new InvalidOperationException(
				$"Property \"{propertyName}\" not found. Animation: \"{animationId}\"");
		}

		public static bool TryCreate<T>(
			Node owner, string propertyName, out IPropertyValueGetter<T> getter)
		{
			return TryCreate(owner, propertyName, animationId: null, out getter);
		}

		public static bool TryCreate<T>(
			Node owner, string propertyName, string animationId, out IPropertyValueGetter<T> getter)
		{
			if (owner.Animators.TryFind<T>(propertyName, out var animator, animationId)) {
				getter = new AnimatorValueGetter<T>(animator);
				return true;
			}

			var propertyInfo = owner.GetType().GetProperty(propertyName, returnType: typeof(T));
			if (propertyInfo != null) {
				getter = new FixedValueGetter<T>((T) propertyInfo.GetValue(owner));
				return true;
			}

			getter = null;
			return false;
		}
	}
}
