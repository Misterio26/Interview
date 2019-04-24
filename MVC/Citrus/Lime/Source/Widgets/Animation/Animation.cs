using System;
using System.Collections.Generic;
using System.Threading;
using Yuzu;

namespace Lime
{
	public class Animation : ICloneable
	{
		private bool isRunning;
		private bool animatorsArePropagated;
		internal Animation Next;
		internal double TimeInternal;
		internal double? NextMarkerOrTriggerTime;

		[Obsolete("Use AssuredStopped instead")]
		public event Action Stopped;

		private event Action BackfieldAssuredStopped;
		public event Action AssuredStopped
		{
			add
			{
				if (!IsRunning) {
					value();
				}
				BackfieldAssuredStopped += value;
			}
			remove => BackfieldAssuredStopped -= value;
		}

		public AnimationEngine AnimationEngine = DefaultAnimationEngine.Instance;
		public string RunningMarkerId { get; set; }

		[YuzuMember]
		[TangerineIgnore]
		public MarkerList Markers { get; private set; }

		[YuzuMember]
		public string Id { get; set; }

		[YuzuMember]
		[TangerineIgnore]
		public bool IsLegacy { get; set; }

		[YuzuMember]
		public string ContentsPath { get; set; }

		public double Time
		{
			get { return TimeInternal; }
			set
			{
				TimeInternal = value;
				NextMarkerOrTriggerTime = null;
				RunningMarkerId = null;
				ApplyAnimators(invokeTriggers: false);
			}
		}

		public int Frame
		{
			get { return AnimationUtils.SecondsToFrames(Time); }
			set { Time = AnimationUtils.FramesToSeconds(value); }
		}

		public Node Owner { get; internal set; }

		public bool IsRunning
		{
			get { return isRunning; }
			set
			{
				if (isRunning != value) {
					bool wasRunning = isRunning;
					isRunning = value;
					if (wasRunning) {
						RaiseStopped();
					}
					if (isRunning) {
						Load();
					}
					Owner?.RefreshRunningAnimationCount();
				}
			}
		}

		public Animation()
		{
			Markers = new MarkerList(this);
		}

		public void Advance(float delta)
		{
			if (IsRunning) {
				AnimationEngine.AdvanceAnimation(this, delta);
			}
		}

		public void FindAnimators(List<IAnimator> animators)
		{
			if (Owner != null) {
				foreach (var node in Owner.Nodes) {
					FindAnimators(node, animators);
				}
			}
		}

		private void FindAnimators(Node node, List<IAnimator> animators)
		{
			foreach (var animator in node.Animators) {
				if (animator.AnimationId == Id) {
					animators.Add(animator);
				}
			}
			if (IsLegacy) {
				return;
			}
			foreach (var animation in node.Animations) {
				if (animation.Id == Id) {
					return;
				}
			}
			foreach (var child in node.Nodes) {
				FindAnimators(child, animators);
			}
		}

		public void Run(string markerId = null)
		{
			if (!TryRun(markerId)) {
				throw new Lime.Exception("Unknown marker '{0}'", markerId);
			}
		}

		public bool TryRun(string markerId = null, double animationTimeCorrection = 0)
		{
			bool wasRunning = IsRunning;
			if (AnimationEngine.TryRunAnimation(this, markerId, animationTimeCorrection)) {
				Stopped = null;
				if (wasRunning) {
					RaiseStopped();
				}
				return true;
			}
			return false;
		}

		public void ApplyAnimators(bool invokeTriggers)
		{
			Load();
			AnimationEngine.ApplyAnimators(this, invokeTriggers);
		}

		public void RewindToNextMarker()
		{
			if (!isRunning) {
				return;
			}

			int frameIndex = AnimationUtils.SecondsToFrames(TimeInternal);

			int? nextMarkerFrame = null;
			foreach (var marker in Markers) {
				if (marker.Frame > frameIndex) {
					nextMarkerFrame = marker.Frame;
					break;
				}
			}

			if (nextMarkerFrame == null) {
				return;
			}

			float step = (float) AnimationUtils.SecondsPerFrame - 0.00001f;
			float acc = (float) (nextMarkerFrame.Value - TimeInternal);
			while (acc >= 0) {
				AdvancePropagated(Math.Min(acc, step));
				acc -= step;
			}

			void AdvancePropagated(float delta)
			{
				Advance(delta);

				foreach (var node in Owner.Descendants) {
					foreach (var animation in node.Animations) {
						animation.Advance(delta);
					}
				}
			}
		}

		internal void RaiseStopped()
		{
			Stopped?.Invoke();

			BackfieldAssuredStopped?.Invoke();
			BackfieldAssuredStopped = null;
		}

		public Animation Clone()
		{
			var clone = (Animation)MemberwiseClone();
			clone.Owner = null;
			clone.Next = null;
			clone.Markers = MarkerList.DeepClone(Markers, clone);
			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public void Load()
		{
			if (animatorsArePropagated || string.IsNullOrEmpty(ContentsPath) || Owner == null) {
				return;
			}
			var d = AnimationData.Load(ContentsPath);
			foreach (var animator in d.Animators) {
				var clone = animator.Clone();
				var (host, index) = AnimationUtils.GetPropertyHost(Owner, clone.TargetPropertyPath);
				if (host == null) {
					continue;
				}
				clone.TargetPropertyPath = clone.TargetPropertyPath.Substring(index);
				host.Animators.Add(clone);
			}
			animatorsArePropagated = true;
		}

		public AnimationData GetData()
		{
			var d = new AnimationData();
			var animators = new List<IAnimator>();
			FindAnimators(animators);
			foreach (var animator in animators) {
				var node = (Node)animator.Owner;
				var propertyPath = $"{node.Id}/{animator.TargetPropertyPath}";
				while (node.Parent != Owner) {
					node = node.Parent;
					propertyPath = $"{node.Id}/{propertyPath}";
				}
				var clone = animator.Clone();
				clone.TargetPropertyPath = propertyPath;
				d.Animators.Add(clone);
			}
			return d;
		}

		public static string FixAntPath(string path)
		{
			path = path.Replace("|", "_");
			return path;
		}

		public class AnimationData
		{
			public delegate bool LoadingDelegate(string path, ref AnimationData instance);
			public delegate void LoadedDelegate(string path, AnimationData instance);
			public static ThreadLocal<LoadingDelegate> Loading;
			public static ThreadLocal<LoadedDelegate> Loaded;

			[YuzuMember]
			public List<IAnimator> Animators { get; private set; } = new List<IAnimator>();

			public static AnimationData Load(string path)
			{
				AnimationData instance = null;
				path = FixAntPath(path);
				path += ".ant";
				if (Loading?.Value?.Invoke(path, ref instance) ?? false) {
					Loaded?.Value?.Invoke(path, instance);
					return instance;
				}
				instance = Serialization.ReadObject<AnimationData>(path);
				Loaded?.Value?.Invoke(path, instance);
				return instance;
			}
		}
	}
}
