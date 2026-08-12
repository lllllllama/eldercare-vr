using UnityEngine;

namespace PicoElderCare.Rehab.Tracking
{
    public sealed class ControllerPoseProvider : RehabPoseProviderBase
    {
        private const string StoppedMessage = "Controller tracking is stopped.";
        private const string WaitingMessage = "Waiting for headset or controller transforms.";
        private const string LimitedMessage = "Controller tracking is available with missing points.";
        private const string ValidMessage = "Headset and controller tracking is available.";

        [SerializeField] private HandPoseTracker handPoseTracker;

        private bool _isRunning;
        private RehabTrackingState _trackingState = RehabTrackingState.Unavailable;
        private string _statusMessage = StoppedMessage;

        public HandPoseTracker HandPoseTracker
        {
            get { return handPoseTracker; }
            set { handPoseTracker = value; }
        }

        public override bool IsSupported
        {
            get { return true; }
        }

        public override bool IsRunning
        {
            get { return _isRunning; }
        }

        public override RehabTrackingState TrackingState
        {
            get { return _trackingState; }
        }

        public override string StatusMessage
        {
            get { return _statusMessage; }
        }

        public override void StartTracking()
        {
            if (handPoseTracker == null)
            {
                handPoseTracker = FindObjectOfType<HandPoseTracker>(true);
            }

            if (handPoseTracker != null)
            {
                handPoseTracker.ResolveReferences();
            }

            _isRunning = true;
            UpdateTrackingStateFromReferences();
        }

        public override void StopTracking()
        {
            _isRunning = false;
            SetState(RehabTrackingState.Unavailable, StoppedMessage);
        }

        public override bool TryGetSample(RehabBodySample target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            target.timestamp = Time.realtimeSinceStartupAsDouble;

            if (!_isRunning)
            {
                target.trackingState = RehabTrackingState.Unavailable;
                return false;
            }

            if (handPoseTracker == null)
            {
                SetState(RehabTrackingState.WaitingForDevice, WaitingMessage);
                target.trackingState = _trackingState;
                return false;
            }

            var headTransform = handPoseTracker.hmdTransform;
            var leftControllerTransform = handPoseTracker.leftControllerTransform;
            var rightControllerTransform = handPoseTracker.rightControllerTransform;

            if (headTransform != null)
            {
                target.SetJoint(
                    RehabJoint.Head,
                    CreateTrackedPose(
                        headTransform.position,
                        headTransform.rotation,
                        RehabTrackingSource.HmdDirect));
            }

            if (leftControllerTransform != null)
            {
                var leftPose = CreateTrackedPose(
                    leftControllerTransform.position,
                    leftControllerTransform.rotation,
                    RehabTrackingSource.ControllerDirect);
                target.SetJoint(RehabJoint.LeftWrist, leftPose);
            }

            if (rightControllerTransform != null)
            {
                var rightPose = CreateTrackedPose(
                    rightControllerTransform.position,
                    rightControllerTransform.rotation,
                    RehabTrackingSource.ControllerDirect);
                target.SetJoint(RehabJoint.RightWrist, rightPose);
            }

            UpdateTrackingStateFromSample(
                headTransform != null,
                leftControllerTransform != null,
                rightControllerTransform != null,
                target.validJointCount);
            target.trackingState = _trackingState;
            return target.IsTrackingUsable;
        }

        private static RehabJointPose CreateTrackedPose(
            Vector3 position,
            Quaternion rotation,
            RehabTrackingSource source)
        {
            return new RehabJointPose
            {
                valid = true,
                confidence = 1f,
                position = position,
                rotation = rotation,
                velocity = Vector3.zero,
                acceleration = Vector3.zero,
                angularVelocity = Vector3.zero,
                source = source
            };
        }

        private void UpdateTrackingStateFromReferences()
        {
            if (handPoseTracker == null)
            {
                SetState(RehabTrackingState.WaitingForDevice, WaitingMessage);
                return;
            }

            var availablePointCount = 0;
            if (handPoseTracker.hmdTransform != null) availablePointCount++;
            if (handPoseTracker.leftControllerTransform != null) availablePointCount++;
            if (handPoseTracker.rightControllerTransform != null) availablePointCount++;

            if (availablePointCount == 3)
            {
                SetState(RehabTrackingState.Valid, ValidMessage);
            }
            else if (availablePointCount > 0)
            {
                SetState(RehabTrackingState.Limited, LimitedMessage);
            }
            else
            {
                SetState(RehabTrackingState.WaitingForDevice, WaitingMessage);
            }
        }

        private void UpdateTrackingStateFromSample(
            bool hasHead,
            bool hasLeftController,
            bool hasRightController,
            int validJointCount)
        {
            if (hasHead && hasLeftController && hasRightController)
            {
                SetState(RehabTrackingState.Valid, ValidMessage);
            }
            else if (validJointCount > 0)
            {
                SetState(RehabTrackingState.Limited, LimitedMessage);
            }
            else
            {
                SetState(RehabTrackingState.WaitingForDevice, WaitingMessage);
            }
        }

        private void SetState(RehabTrackingState state, string message)
        {
            _trackingState = state;
            _statusMessage = message;
        }
    }
}
