using System;
using System.Collections.Generic;

namespace Robot.Core.Common.Utils
{
	/// <summary>
	/// Нужен для правильно ведения FillState, когда данные разрознены. Роли:
	/// 1. Запускать FillState на каждое изменения свойств или Отложенно (dirty), а запускать FillState в Update, только если были изменения.
	/// 2. Индикация можно ли применить состояние во вью с анимированием или надо сразу.
	/// 3. Включение/выключение, только когда добавлен в игре или на сцену, или сработал OnBuilt.
	/// 4. Упрощенный интерфейс установки полей (SetField), чтобы сразу пометить как dirty.
	/// </summary>
	public class FillStateHelper
	{
		private class Transaction : IDisposable
		{
			private readonly FillStateHelper stateHelper;

			public Transaction(FillStateHelper stateHelper)
			{
				this.stateHelper = stateHelper;
			}

			public void Dispose()
			{
				stateHelper.OnTransactionDisposed(this);
			}
		}

		public delegate void OnFillState(bool animated);

		public delegate void OnCleanState();

		private readonly bool isAutoConsumeChanges;
		private OnFillState onFillState;
		private OnCleanState onCleanState;

		private bool isFillStateActive;
		private object suppressDeepCallsActiveReason;

		private bool isStarted;
		private bool isStartVirgin;
		private bool wasChanges;

		private readonly HashSet<Transaction> transactions = new HashSet<Transaction>();

		public FillStateHelper(
			bool isAutoStart, bool isAutoConsumeChanges,
			OnFillState onFillState, OnCleanState onCleanState
		)
		{
			this.isAutoConsumeChanges = isAutoConsumeChanges;
			this.onFillState = onFillState;
			this.onCleanState = onCleanState;

			if (isAutoStart) {
				Start();
			} else {
				wasChanges = true;
			}
		}

		public void ForceModelChanges()
		{
			if (!isStarted) {
				return;
			}

			ModelChanged();
			ConsumeModelChangesIfNeeded();
		}

		public void ModelChangedForSubscription()
		{
			ModelChanged();
		}

		public void ModelChanged(object suppressDeepCallsReason = null)
		{
			if (suppressDeepCallsActiveReason != null && suppressDeepCallsActiveReason == suppressDeepCallsReason) {
				return;
			}

			wasChanges = true;

			suppressDeepCallsActiveReason = suppressDeepCallsReason;

			if (isAutoConsumeChanges) {
				ConsumeModelChangesIfNeeded();
			}
		}

		public void ConsumeModelChangesIfNeeded()
		{
			if (!isStarted) {
				return;
			}

			if (!wasChanges) {
				return;
			}

			if (transactions.Count > 0) {
				return;
			}

			if (isFillStateActive) {
				return;
			}
			isFillStateActive = true;

			bool wasVirgin = isStartVirgin;
			isStartVirgin = false;

			try {
				do {
					wasChanges = false;
					onFillState?.Invoke(!wasVirgin);
					isStartVirgin = false;
					wasVirgin = false;
					if (!isStarted) {
						onCleanState?.Invoke();
					}
				} while (wasChanges && isStarted && transactions.Count <= 0);
			} finally {
				isFillStateActive = false;
				suppressDeepCallsActiveReason = null;
			}
		}

		public void Start()
		{
			if (isStarted) {
				ForceModelChanges();
				return;
			}

			isStarted = true;
			isStartVirgin = true;
			ConsumeModelChangesIfNeeded();
		}

		public void Stop()
		{
			if (isStarted) {
				isStarted = false;
				if (!isFillStateActive) {
					onCleanState?.Invoke();
				}
			} else {
				isStarted = false;
			}
		}

		public void SetField<T>(ref T field, in T newValue)
		{
			if (typeof(T).IsSubclassOf(typeof(IEquatable<T>))) {
				if (newValue.Equals(field)) {
					return;
				}
			} else {
				if (ReferenceEquals(field, newValue)) {
					return;
				}
			}

			field = newValue;
			ModelChanged();
		}

		public IDisposable BeginSetFieldTransaction()
		{
			var transaction = new Transaction(this);
			transactions.Add(transaction);
			return transaction;
		}

		private void OnTransactionDisposed(Transaction transaction)
		{
			if (transactions.Remove(transaction) && transactions.Count == 0) {
				ConsumeModelChangesIfNeeded();
			}
		}

		public FillStateHelper Clone(OnFillState onFillState, OnCleanState onCleanState)
		{
			var clone = (FillStateHelper) MemberwiseClone();
			clone.onFillState = onFillState;
			clone.onCleanState = onCleanState;
			clone.isFillStateActive = false;
			clone.suppressDeepCallsActiveReason = null;
			clone.transactions.Clear();
			return clone;
		}
	}
}
