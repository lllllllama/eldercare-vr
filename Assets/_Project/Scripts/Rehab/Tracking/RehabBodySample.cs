using System;

namespace PicoElderCare.Rehab.Tracking
{
    public sealed class RehabBodySample
    {
        public double timestamp;
        public RehabTrackingState trackingState;
        public RehabJointPose[] joints { get; private set; }
        public bool isSeatedMode;
        public int validJointCount { get; private set; }

        public bool IsTrackingUsable
        {
            get
            {
                return validJointCount > 0 && trackingState == RehabTrackingState.Valid;
            }
        }

        public RehabBodySample()
        {
            joints = new RehabJointPose[(int)RehabJoint.Count];
            Clear();
        }

        public bool TryGetJoint(RehabJoint joint, out RehabJointPose pose)
        {
            var index = (int)joint;
            if (!IsJointIndexValid(index))
            {
                pose = default(RehabJointPose);
                return false;
            }

            pose = joints[index];
            return pose.valid;
        }

        public bool HasRequiredJoints(RehabJoint[] requiredJoints)
        {
            if (requiredJoints == null)
            {
                return false;
            }

            for (var i = 0; i < requiredJoints.Length; i++)
            {
                RehabJointPose pose;
                if (!TryGetJoint(requiredJoints[i], out pose))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetJoint(RehabJoint joint, RehabJointPose pose)
        {
            var index = (int)joint;
            if (!IsJointIndexValid(index))
            {
                throw new ArgumentOutOfRangeException("joint", joint, "Joint must identify a body joint, not Count.");
            }

            var wasValid = joints[index].valid;
            joints[index] = pose;

            if (!wasValid && pose.valid)
            {
                validJointCount++;
            }
            else if (wasValid && !pose.valid)
            {
                validJointCount--;
            }
        }

        public void Clear()
        {
            timestamp = 0d;
            trackingState = RehabTrackingState.Unavailable;
            isSeatedMode = false;
            validJointCount = 0;

            for (var i = 0; i < joints.Length; i++)
            {
                joints[i] = default(RehabJointPose);
            }
        }

        private bool IsJointIndexValid(int index)
        {
            return index >= 0 && index < joints.Length;
        }
    }
}
