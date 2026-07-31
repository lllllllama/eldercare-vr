using Unity.XR.PXR;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public struct PicoBodyJointData
    {
        public bool valid;
        public BodyTrackerRole role;
        public long timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 angularVelocity;
    }

    public sealed class PicoBodyTrackingFrame
    {
        public PicoBodyJointData[] joints { get; private set; }
        public int validJointCount { get; private set; }

        public PicoBodyTrackingFrame()
        {
            joints = new PicoBodyJointData[(int)BodyTrackerRole.ROLE_NUM];
        }

        public void SetJoint(PicoBodyJointData joint)
        {
            var index = (int)joint.role;
            if (joint.role == BodyTrackerRole.NONE_ROLE || index < 0 || index >= joints.Length)
            {
                return;
            }

            var wasValid = joints[index].valid;
            joints[index] = joint;
            if (!wasValid && joint.valid)
            {
                validJointCount++;
            }
            else if (wasValid && !joint.valid)
            {
                validJointCount--;
            }
        }

        public void CopyFrom(PicoBodyTrackingFrame source)
        {
            Clear();
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < joints.Length; i++)
            {
                joints[i] = source.joints[i];
            }

            validJointCount = source.validJointCount;
        }

        public void Clear()
        {
            validJointCount = 0;
            for (var i = 0; i < joints.Length; i++)
            {
                joints[i] = default(PicoBodyJointData);
            }
        }
    }
}
