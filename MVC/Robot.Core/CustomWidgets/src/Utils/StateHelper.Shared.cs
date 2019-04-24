using System;
using System.Collections.Generic;

namespace Robot.Core.Common.Utils
{
	public class StateHelper<T> where T : Enum
	{
		public delegate void OnStateChangedDelegate(T newValue, T oldValue);

		private const int CountToLock = 4;

		private readonly Stack<T> processStateStack = new Stack<T>();
		private int processStateStackCounter = 4;

		private readonly Action onProcessState;
		private readonly OnStateChangedDelegate onStateChanged;
		private readonly OnStateChangedDelegate onFinalStateChanged;
		private T value;

		private event Action OnStateChangedExt;
		private bool wasProcessStateChange;

		private bool isInOnStateChanged;

		public T Value
		{
			get => value;
			set
			{
				// TODO проверять, что было две установки стейт за один ProcessState

				if (processStateStack.Count == 0) {
					throw new Exception("Can not change state outside of ProcessState method");
				}

				if (this.value.Equals(value)) {
					return;
				}

				var oldValue = this.value;
				this.value = value;
				wasProcessStateChange = true;
				isInOnStateChanged = true;
				onStateChanged?.Invoke(value, oldValue);
				isInOnStateChanged = false;
				ProcessStateInner(true);
			}
		}

		public StateHelper(
			T value, Action onProcessState,
			OnStateChangedDelegate onStateChanged = null,
			OnStateChangedDelegate onFinalStateChanged = null
		)
		{
			this.value = value;
			this.onProcessState = onProcessState;
			this.onStateChanged = onStateChanged;
			this.onFinalStateChanged = onFinalStateChanged;
		}

		public void ProcessState()
		{
			ProcessStateInner(false);
		}

		private void ProcessStateInner(bool analyzeValue)
		{
			if (processStateStackCounter <= 0 || isInOnStateChanged) {
				return;
			}

			var savedValue = value;

			bool hasValue = analyzeValue && processStateStack.Contains(savedValue);

			processStateStack.Push(savedValue);

			if (hasValue) {
				processStateStackCounter--;
				if (processStateStackCounter <= 0) {
					return;
				}
			}

			if (processStateStack.Count > 1) {
				return;
			}

			int onProcessBeginStackCount = 0;
			try {
				while (processStateStack.Count > onProcessBeginStackCount) {
					onProcessBeginStackCount = processStateStack.Count;
					onProcessState?.Invoke();
				}
			} finally {
				processStateStack.Clear();
				processStateStackCounter = CountToLock;

				if (wasProcessStateChange) {
					wasProcessStateChange = false;
					OnStateChangedExt?.Invoke();
				}
				if (!value.Equals(savedValue)) {
					onFinalStateChanged?.Invoke(value, savedValue);
				}
			}
		}

		public bool WasValueInProcessStateChain(T value)
		{
			return processStateStack.Contains(value);
		}

		/// <summary>
		/// Use it very carefully.
		/// </summary>
		public void ForceStateReset(T state)
		{
			value = state;
		}

		public void Subscribe(Action onChanged)
		{
			OnStateChangedExt += onChanged;
			onChanged();
		}

		public void Unsubscribe(Action onChanged)
		{
			OnStateChangedExt -= onChanged;
		}

		public StateHelper<T> Clone(Action onProcessState,
			OnStateChangedDelegate onStateChanged = null,
			OnStateChangedDelegate onFinalStateChanged = null)
		{
			return new StateHelper<T>(value, onProcessState, onStateChanged, onFinalStateChanged);
		}
	}
}
