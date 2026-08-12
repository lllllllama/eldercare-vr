using UnityEngine;
using UnityEngine.XR;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    public sealed class PicoWristObjectTrackingProvider : RehabPoseProviderBase
    {
        [SerializeField] private Transform hmdTransform;
        [SerializeField] private Transform xrOrigin;
        [SerializeField, Range(1, 60)] private int requiredStableFrames = 20;

        private IPicoObjectTrackingApi _api;
        private WristTrackerBindingManager _binding;
        private WristTrackerCalibration _calibration;
        private bool _isRunning;
        private int _stableFrameCount;
        private RehabTrackingState _trackingState = RehabTrackingState.Unavailable;
        private WristTrackerSetupState _setupState = WristTrackerSetupState.NoTracker;
        private string _statusMessage = "腕部追踪尚未启动。";
        private PicoObjectTrackerPose _leftPose;
        private PicoObjectTrackerPose _rightPose;

        public Transform HmdTransform { get { return hmdTransform; } set { hmdTransform = value; } }
        public Transform XrOrigin { get { return xrOrigin; } set { xrOrigin = value; } }
        public int RequiredStableFrames { get { return requiredStableFrames; } set { requiredStableFrames = Mathf.Max(1, value); } }
        public int StableFrameCount { get { return _stableFrameCount; } }
        public WristTrackerSetupState SetupState { get { return _setupState; } }
        public PicoObjectTrackerPose LeftTrackerPose { get { return _leftPose; } }
        public PicoObjectTrackerPose RightTrackerPose { get { return _rightPose; } }
        public bool WristTrackingReady
        {
            get
            {
                return _isRunning &&
                       _trackingState == RehabTrackingState.Valid &&
                       _setupState == WristTrackerSetupState.Ready &&
                       _stableFrameCount >= Mathf.Max(1, requiredStableFrames);
            }
        }
        public bool HeadPoseValid { get { return IsHeadPoseAvailable(); } }

        public override bool IsSupported { get { return _api != null && _api.IsSupported; } }
        public override bool IsRunning { get { return _isRunning; } }
        public override RehabTrackingState TrackingState { get { return _trackingState; } }
        public override string StatusMessage { get { return _statusMessage; } }

        public void Configure(
            IPicoObjectTrackingApi api,
            WristTrackerBindingManager binding,
            WristTrackerCalibration calibration,
            Transform head,
            Transform origin)
        {
            _api = api;
            _binding = binding;
            _calibration = calibration;
            hmdTransform = head;
            xrOrigin = origin;
            ResetStability();
        }

        public override void StartTracking()
        {
            if (_isRunning) return;
            if (_api == null || _binding == null || _calibration == null)
            {
                SetState(RehabTrackingState.Error, WristTrackerSetupState.ApiError, "腕部追踪服务未配置。", true);
                return;
            }

            if (!_api.IsRunning && !_api.StartTracking())
            {
                SetState(
                    _api.IsSupported ? RehabTrackingState.Error : RehabTrackingState.Unsupported,
                    _api.IsSupported ? WristTrackerSetupState.ApiError : WristTrackerSetupState.Unsupported,
                    string.IsNullOrEmpty(_api.LastError) ? "无法启动腕部追踪。" : _api.LastError,
                    true);
                return;
            }

            _isRunning = true;
            SetState(RehabTrackingState.Starting, WristTrackerSetupState.Stabilizing, "正在准备腕部追踪。", true);
        }

        public override void StopTracking()
        {
            _isRunning = false;
            ResetStability();
            SetState(RehabTrackingState.Unavailable, WristTrackerSetupState.NoTracker, "腕部追踪已停止。", false);
        }

        public override bool TryGetSample(RehabBodySample target)
        {
            if (target == null) return false;
            target.Clear();
            target.timestamp = Time.realtimeSinceStartupAsDouble;

            if (!_isRunning || _api == null)
            {
                target.trackingState = _trackingState;
                return false;
            }

            if (!_api.IsSupported)
            {
                SetState(RehabTrackingState.Unsupported, WristTrackerSetupState.Unsupported, "当前设备不支持腕部传感器。", true);
                target.trackingState = _trackingState;
                return false;
            }

            if (_api.ConnectedTrackerCount == 0)
            {
                SetState(RehabTrackingState.WaitingForDevice, WristTrackerSetupState.NoTracker, "未检测到腕部传感器。", true);
                target.trackingState = _trackingState;
                return false;
            }

            if (_api.ConnectedTrackerCount == 1)
            {
                SetState(RehabTrackingState.WaitingForDevice, WristTrackerSetupState.OneTrackerOnly, "仅检测到一个腕部传感器。", true);
                target.trackingState = _trackingState;
                return false;
            }

            if (!_binding.IsBindingReady)
            {
                SetState(RehabTrackingState.WaitingForCalibration, WristTrackerSetupState.BindingRequired, "需要完成左右腕匹配。", true);
                target.trackingState = _trackingState;
                return false;
            }

            if (!_calibration.IsCalibrationReady)
            {
                SetState(RehabTrackingState.WaitingForCalibration, WristTrackerSetupState.CalibrationRequired, "需要进行腕部校准。", true);
                target.trackingState = _trackingState;
                return false;
            }

            PicoObjectTrackerPose rawLeft;
            PicoObjectTrackerPose rawRight;
            if (!_api.TryGetTrackerPose(_binding.Profile.leftTrackerId, out rawLeft) ||
                !_api.TryGetTrackerPose(_binding.Profile.rightTrackerId, out rawRight) ||
                !_calibration.TryApplyLeft(rawLeft, out _leftPose) ||
                !_calibration.TryApplyRight(rawRight, out _rightPose))
            {
                SetState(RehabTrackingState.Lost, WristTrackerSetupState.PoseLost, "腕部传感器信号暂时丢失。", true);
                target.trackingState = _trackingState;
                return false;
            }

            if (!IsHeadPoseAvailable())
            {
                SetState(RehabTrackingState.Lost, WristTrackerSetupState.PoseLost, "头显追踪暂时不可用。", true);
                target.trackingState = _trackingState;
                return false;
            }

            _stableFrameCount++;
            if (_stableFrameCount < Mathf.Max(1, requiredStableFrames))
            {
                SetState(RehabTrackingState.Starting, WristTrackerSetupState.Stabilizing, "腕部传感器正在稳定。", false);
                target.trackingState = _trackingState;
                return false;
            }

            target.SetJoint(
                RehabJoint.Head,
                CreateJointPose(
                    hmdTransform.position,
                    hmdTransform.rotation,
                    Vector3.zero,
                    Vector3.zero,
                    RehabTrackingSource.HmdDirect));
            SetWristJoint(target, RehabJoint.LeftWrist, _leftPose);
            SetWristJoint(target, RehabJoint.RightWrist, _rightPose);
            SetState(RehabTrackingState.Valid, WristTrackerSetupState.Ready, "腕部传感器已就绪。", false);
            target.trackingState = RehabTrackingState.Valid;
            return true;
        }

        private void SetWristJoint(RehabBodySample target, RehabJoint joint, PicoObjectTrackerPose pose)
        {
            var worldPosition = xrOrigin != null ? xrOrigin.TransformPoint(pose.position) : pose.position;
            var worldRotation = xrOrigin != null ? xrOrigin.rotation * pose.rotation : pose.rotation;
            var worldVelocity = xrOrigin != null ? xrOrigin.TransformDirection(pose.velocity) : pose.velocity;
            var worldAngularVelocity = xrOrigin != null ? xrOrigin.TransformDirection(pose.angularVelocity) : pose.angularVelocity;
            target.SetJoint(
                joint,
                CreateJointPose(
                    worldPosition,
                    worldRotation,
                    worldVelocity,
                    worldAngularVelocity,
                    RehabTrackingSource.ObjectTrackerDirect));
        }

        private static RehabJointPose CreateJointPose(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            Vector3 angularVelocity,
            RehabTrackingSource source)
        {
            return new RehabJointPose
            {
                valid = true,
                confidence = 1f,
                position = position,
                rotation = rotation,
                velocity = velocity,
                acceleration = Vector3.zero,
                angularVelocity = angularVelocity,
                source = source
            };
        }

        private void SetState(
            RehabTrackingState state,
            WristTrackerSetupState setupState,
            string message,
            bool resetStability)
        {
            if (resetStability) ResetStability();
            _trackingState = state;
            _setupState = setupState;
            _statusMessage = message;
        }

        private void ResetStability()
        {
            _stableFrameCount = 0;
            _leftPose = default(PicoObjectTrackerPose);
            _rightPose = default(PicoObjectTrackerPose);
        }

        private bool IsHeadPoseAvailable()
        {
            if (hmdTransform == null) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!headDevice.isValid) return false;
            bool isTracked;
            return !headDevice.TryGetFeatureValue(CommonUsages.isTracked, out isTracked) || isTracked;
#else
            return true;
#endif
        }
    }
}
