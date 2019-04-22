using System.Collections.Generic;
using Lime;
using Lime.KineticMotionStrategy;
using Robot.Core.Common.Camera;
using Robot.Core.Common.Utils;
using Robot.Layer1.Common.ActivitiesSystem;
using Robot.Layer2.Common;

namespace Robot.Layer3.Common.World.UserInput
{
	public class FragmentUserInputCamera : FragmentInput
	{
		private const float WheelZoomFactor = 1.1f;

		private readonly List<Widget> inputOwners;
		private readonly Camera camera;
		private readonly UserInputCameraSettings settings;
		private readonly List<DragGesture> dragGestures;
		private readonly List<PinchGesture> pinchGestures;
		private readonly StateHelper<InputFragmentState> inputStateHelper;
		private bool isEnabledDesired;

		public override InputFragmentState InputState => inputStateHelper.Value;

		public bool IsEnabledDesired
		{
			get => isEnabledDesired;
			set
			{
				isEnabledDesired = value;
				inputStateHelper.ProcessState();
			}
		}

		public FragmentUserInputCamera(
			Camera camera, in UserInputCameraSettings cameraSettings, List<Widget> inputOwners)
		{
			this.inputOwners = inputOwners;
			settings = cameraSettings;
			this.camera = camera;
			isEnabledDesired = true;

			inputStateHelper = new StateHelper<InputFragmentState>(
				InputFragmentState.Detached, ProcessStateInner, OnStateChanged);

			dragGestures = new List<DragGesture>();
			pinchGestures = new List<PinchGesture>();
		}

		protected override int OnGetLayer()
		{
			return RenderLayers.AbstractControllers;
		}

		protected override void OnAppeared()
		{
			base.OnAppeared();

			foreach (var inputOwner in inputOwners) {

				var dragGesture = new KineticDragGesture(new DeceleratingKineticMotionStrategy(0.97f, 1.002f));
				var pinchGesture = new PinchGesture(exclusive: true);

				inputOwner.Gestures.Add(dragGesture);
				dragGestures.Add(dragGesture);
				inputOwner.Gestures.Add(pinchGesture);
				pinchGestures.Add(pinchGesture);
			}

			inputStateHelper.ProcessState();
		}

		protected override void OnDisappeared()
		{
			for (int i = 0; i < inputOwners.Count; i++) {
				var inputOwner = inputOwners[i];
				inputOwner.Gestures.Remove(dragGestures[i]);
				inputOwner.Gestures.Remove(pinchGestures[i]);
			}

			base.OnDisappeared();
			inputStateHelper.ProcessState();
		}

		public override void Update(float delta)
		{
			base.Update(delta);
			inputStateHelper.ProcessState();
		}

		private void ProcessStateInner()
		{
			if (!IsAttached) {
				inputStateHelper.Value = InputFragmentState.Detached;
				return;
			}

			bool hasActiveDrag = false;

			for (int i = 0; i < inputOwners.Count; i++) {
				hasActiveDrag = hasActiveDrag || dragGestures[i].IsActive || pinchGestures[i].IsActive;
			}

			switch (inputStateHelper.Value) {
				case InputFragmentState.Idle when hasActiveDrag:
					inputStateHelper.Value = InputFragmentState.Working;
					break;
				case InputFragmentState.Idle when !IsEnabledDesired:
					inputStateHelper.Value = InputFragmentState.Pending;
					break;
				case InputFragmentState.Working when !hasActiveDrag:
					inputStateHelper.Value = InputFragmentState.Idle;
					break;
				case InputFragmentState.Pending when IsEnabledDesired:
					inputStateHelper.Value = InputFragmentState.Idle;
					break;
				case InputFragmentState.Detached when IsAttached:
					inputStateHelper.Value = InputFragmentState.Idle;
					break;
			}
		}

		private void OnStateChanged(InputFragmentState newState, InputFragmentState oldState)
		{
			if (
				(oldState == InputFragmentState.Pending || oldState == InputFragmentState.Detached) &&
				newState != InputFragmentState.Pending &&
				newState != InputFragmentState.Detached
			) {
				SubscribeOnGestures();
			} else if (
				(newState == InputFragmentState.Pending || newState == InputFragmentState.Detached) &&
				oldState != InputFragmentState.Pending &&
				oldState != InputFragmentState.Detached
			) {
				UnsubscribeFromGestures();
			}
		}

		private void SubscribeOnGestures()
		{
			for (int i = 0; i < inputOwners.Count; i++) {
				var dragGesture = dragGestures[i];
				var pinchGesture = pinchGestures[i];
				dragGesture.Changed += OnDragged;
				pinchGesture.Changed += OnPinched;
				if (pinchGesture.IsActive || dragGesture.IsActive) {
					MouseWheelProcessor.Attach(inputOwners[i], OnMouseWheel);
				}
			}
		}

		private void UnsubscribeFromGestures()
		{
			for (int i = 0; i < inputOwners.Count; i++) {
				var dragGesture = dragGestures[i];
				var pinchGesture = pinchGestures[i];
				dragGesture.Changed -= OnDragged;
				pinchGesture.Changed -= OnPinched;
				if (pinchGesture.IsActive || dragGesture.IsActive) {
					MouseWheelProcessor.Detach(inputOwners[i]);
				}
			}
		}

		private void OnPinched()
		{
			if (!IsInfluenceAllowed()) {
				return;
			}

			for (int i = 0; i < inputOwners.Count; i++) {
				var pinchGesture = pinchGestures[i];
				if (pinchGesture.IsActive) {
					camera.Position -= pinchGesture.LastDragDistance / camera.Zoom;
					float zoom = ClampZoom(camera.Zoom * pinchGesture.LastPinchScale);
					ZoomOrigin(pinchGesture.MousePosition, zoom);
				}
			}
		}

		private void OnDragged()
		{
			if (!IsInfluenceAllowed()) {
				return;
			}

			for (int i = 0; i < inputOwners.Count; i++) {
				var dragGesture = dragGestures[i];
				if (dragGesture.IsActive) {
					camera.Position -= dragGesture.LastDragDistance / camera.Zoom;
				}
			}
		}

		private void OnMouseWheel(Vector2 position, float wheelDelta)
		{
			if (!IsInfluenceAllowed()) {
				return;
			}
			float zoom = (wheelDelta > 0.0f)
				? ClampZoom(camera.Zoom * WheelZoomFactor)
				: ClampZoom(camera.Zoom / WheelZoomFactor);

			ZoomOrigin(position, zoom);
		}

		private void ZoomOrigin(in Vector2 origin, float zoom)
		{
			float zoomDelta = zoom / camera.Zoom;
			var originInViewport = origin * camera.CalcGlobalToLocalTransform();
			var offset = originInViewport * (1.0f - 1.0f / zoomDelta);
			camera.Position += offset;
			camera.Zoom = zoom;
		}

		private float ClampZoom(float desiredZoom)
		{
			return desiredZoom.Clamp(settings.MinZoom, settings.MaxZoom);
		}

		private bool IsInfluenceAllowed()
		{
			return inputStateHelper.Value == InputFragmentState.Idle ||
				inputStateHelper.Value == InputFragmentState.Working;
		}
	}
}
