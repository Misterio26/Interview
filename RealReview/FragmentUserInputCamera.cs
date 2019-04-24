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

		private readonly Widget inputOwner;
		private readonly Camera camera;
		private readonly UserInputCameraSettings settings;
		private readonly DragGesture dragGesture;
		private readonly PinchGesture pinchGesture;
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
			Camera camera, in UserInputCameraSettings cameraSettings, Widget inputOwner)
		{
			this.inputOwner = inputOwner;
			settings = cameraSettings;
			this.camera = camera;
			isEnabledDesired = true;

			inputStateHelper = new StateHelper<InputFragmentState>(
				InputFragmentState.Detached, ProcessStateInner, OnStateChanged);

			dragGesture = new KineticDragGesture(new DeceleratingKineticMotionStrategy(0.97f, 1.002f));
			pinchGesture = new PinchGesture(exclusive: true);
		}

		protected override int OnGetLayer()
		{
			return RenderLayers.AbstractControllers;
		}

		protected override void OnAppeared()
		{
			base.OnAppeared();
			inputOwner.Gestures.Add(dragGesture);
			inputOwner.Gestures.Add(pinchGesture);
			inputStateHelper.ProcessState();
		}

		protected override void OnDisappeared()
		{
			inputOwner.Gestures.Remove(dragGesture);
			inputOwner.Gestures.Remove(pinchGesture);
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

			bool hasActiveDrag = dragGesture.IsActive || pinchGesture.IsActive;
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
			dragGesture.Changed += OnDragged;
			pinchGesture.Changed += OnPinched;
			MouseWheelProcessor.Attach(inputOwner, OnMouseWheel);
		}

		private void UnsubscribeFromGestures()
		{
			dragGesture.Changed -= OnDragged;
			pinchGesture.Changed -= OnPinched;
			MouseWheelProcessor.Detach(inputOwner);
		}

		private void OnPinched()
		{
			if (!IsInfluenceAllowed()) {
				return;
			}
			camera.Position -= pinchGesture.LastDragDistance / camera.Zoom;
			float zoom = ClampZoom(camera.Zoom * pinchGesture.LastPinchScale);
			ZoomOrigin(pinchGesture.MousePosition, zoom);
		}

		private void OnDragged()
		{
			if (!IsInfluenceAllowed()) {
				return;
			}
			camera.Position -= dragGesture.LastDragDistance / camera.Zoom;
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
