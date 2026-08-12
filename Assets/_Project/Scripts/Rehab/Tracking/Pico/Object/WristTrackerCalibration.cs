using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    public sealed class WristTrackerCalibration
    {
        private readonly IPicoObjectTrackingApi _api;
        private readonly WristTrackerBindingManager _binding;
        private readonly IWristTrackerCalibrationStore _store;
        private readonly WristTrackerCalibrationProfile _profile;

        private bool _calibrating;
        private float _elapsed;
        private int _validFrames;

        public float minimumCalibrationSeconds = 0.75f;
        public int minimumValidFrames = 15;

        public WristTrackerCalibrationProfile Profile { get { return _profile; } }
        public bool IsCalibrationReady { get { return _profile.IsReady; } }
        public bool IsCalibrating { get { return _calibrating; } }
        public string StatusMessage { get; private set; }

        public WristTrackerCalibration(
            IPicoObjectTrackingApi api,
            WristTrackerBindingManager binding,
            IWristTrackerCalibrationStore store = null,
            WristTrackerCalibrationProfile profile = null)
        {
            _api = api ?? throw new ArgumentNullException("api");
            _binding = binding ?? throw new ArgumentNullException("binding");
            _store = store ?? new PlayerPrefsWristTrackerCalibrationStore();
            _profile = profile ?? new WristTrackerCalibrationProfile();
            _store.Load(_profile);
            StatusMessage = _profile.IsReady ? "腕部校准已就绪。" : "需要进行腕部校准。";
        }

        public bool BeginCalibration()
        {
            if (!_binding.IsBindingReady)
            {
                StatusMessage = "请先完成左右腕匹配。";
                return false;
            }

            _calibrating = true;
            _elapsed = 0f;
            _validFrames = 0;
            StatusMessage = "请自然抬起双腕并朝前，正在记录安装方向。";
            return true;
        }

        public void CancelCalibration()
        {
            _calibrating = false;
            StatusMessage = _profile.IsReady ? "腕部校准已就绪。" : "腕部校准已取消。";
        }

        /// <summary>
        /// Explicitly selects zero mount offset. This is a deliberate user
        /// choice, not an implicit Connected == Ready shortcut.
        /// </summary>
        public void UseIdentityCalibration()
        {
            _profile.leftPositionOffset = Vector3.zero;
            _profile.leftRotationOffset = Quaternion.identity;
            _profile.rightPositionOffset = Vector3.zero;
            _profile.rightRotationOffset = Quaternion.identity;
            _profile.leftReady = true;
            _profile.rightReady = true;
            _profile.identityCalibrationExplicitlyAccepted = true;
            _store.Save(_profile);
            _calibrating = false;
            StatusMessage = "已使用腕带中心作为腕部位置。";
        }

        public void ClearCalibration()
        {
            _profile.leftPositionOffset = Vector3.zero;
            _profile.leftRotationOffset = Quaternion.identity;
            _profile.rightPositionOffset = Vector3.zero;
            _profile.rightRotationOffset = Quaternion.identity;
            _profile.leftReady = false;
            _profile.rightReady = false;
            _profile.identityCalibrationExplicitlyAccepted = false;
            _store.Clear();
            _calibrating = false;
            StatusMessage = "需要进行腕部校准。";
        }

        public void Tick(float deltaTime)
        {
            if (!_calibrating) return;

            PicoObjectTrackerPose left;
            PicoObjectTrackerPose right;
            if (!_api.TryGetTrackerPose(_binding.Profile.leftTrackerId, out left) ||
                !_api.TryGetTrackerPose(_binding.Profile.rightTrackerId, out right) ||
                !left.poseValid || !right.poseValid)
            {
                _validFrames = 0;
                StatusMessage = "校准时传感器信号丢失，请保持双腕可见。";
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            _validFrames++;
            if (_elapsed < minimumCalibrationSeconds || _validFrames < minimumValidFrames)
            {
                return;
            }

            // A single neutral pose cannot geometrically infer the physical
            // wrist centre. Version one records an explicit zero position
            // offset and corrects mount roll/pitch to the neutral world-up
            // frame. Position remains the directly observed tracker position.
            _profile.leftPositionOffset = Vector3.zero;
            _profile.rightPositionOffset = Vector3.zero;
            _profile.leftRotationOffset = ComputeNeutralRotationOffset(left.rotation);
            _profile.rightRotationOffset = ComputeNeutralRotationOffset(right.rotation);
            _profile.leftReady = true;
            _profile.rightReady = true;
            _profile.identityCalibrationExplicitlyAccepted = false;
            _store.Save(_profile);
            _calibrating = false;
            StatusMessage = "腕部安装方向校准完成。";
        }

        public bool TryApplyLeft(PicoObjectTrackerPose trackerPose, out PicoObjectTrackerPose wristPose)
        {
            return TryApply(
                trackerPose,
                _profile.leftPositionOffset,
                _profile.leftRotationOffset,
                _profile.leftReady || _profile.identityCalibrationExplicitlyAccepted,
                out wristPose);
        }

        public bool TryApplyRight(PicoObjectTrackerPose trackerPose, out PicoObjectTrackerPose wristPose)
        {
            return TryApply(
                trackerPose,
                _profile.rightPositionOffset,
                _profile.rightRotationOffset,
                _profile.rightReady || _profile.identityCalibrationExplicitlyAccepted,
                out wristPose);
        }

        private static bool TryApply(
            PicoObjectTrackerPose trackerPose,
            Vector3 localPositionOffset,
            Quaternion localRotationOffset,
            bool ready,
            out PicoObjectTrackerPose wristPose)
        {
            wristPose = trackerPose;
            if (!ready || !trackerPose.connected || !trackerPose.poseValid)
            {
                return false;
            }

            wristPose.position = trackerPose.position + trackerPose.rotation * localPositionOffset;
            wristPose.rotation = trackerPose.rotation * localRotationOffset;
            return true;
        }

        private static Quaternion ComputeNeutralRotationOffset(Quaternion trackerRotation)
        {
            var horizontalForward = Vector3.ProjectOnPlane(trackerRotation * Vector3.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude < 0.0001f)
            {
                horizontalForward = Vector3.forward;
            }

            var neutralRotation = Quaternion.LookRotation(horizontalForward.normalized, Vector3.up);
            return Quaternion.Inverse(trackerRotation) * neutralRotation;
        }
    }
}
