using Unity.XR.PXR;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public static class PicoBodyJointMapper
    {
        public static bool TryMap(BodyTrackerRole role, out RehabJoint joint)
        {
            switch (role)
            {
                case BodyTrackerRole.Pelvis: joint = RehabJoint.Hips; return true;
                case BodyTrackerRole.LEFT_HIP: joint = RehabJoint.LeftHip; return true;
                case BodyTrackerRole.RIGHT_HIP: joint = RehabJoint.RightHip; return true;
                case BodyTrackerRole.SPINE1: joint = RehabJoint.SpineLower; return true;
                case BodyTrackerRole.LEFT_KNEE: joint = RehabJoint.LeftKnee; return true;
                case BodyTrackerRole.RIGHT_KNEE: joint = RehabJoint.RightKnee; return true;
                case BodyTrackerRole.SPINE2: joint = RehabJoint.SpineUpper; return true;
                case BodyTrackerRole.LEFT_ANKLE: joint = RehabJoint.LeftAnkle; return true;
                case BodyTrackerRole.RIGHT_ANKLE: joint = RehabJoint.RightAnkle; return true;
                case BodyTrackerRole.SPINE3: joint = RehabJoint.Chest; return true;
                case BodyTrackerRole.LEFT_FOOT: joint = RehabJoint.LeftFoot; return true;
                case BodyTrackerRole.RIGHT_FOOT: joint = RehabJoint.RightFoot; return true;
                case BodyTrackerRole.NECK: joint = RehabJoint.Neck; return true;
                case BodyTrackerRole.LEFT_COLLAR: joint = RehabJoint.LeftShoulder; return true;
                case BodyTrackerRole.RIGHT_COLLAR: joint = RehabJoint.RightShoulder; return true;
                case BodyTrackerRole.HEAD: joint = RehabJoint.Head; return true;
                case BodyTrackerRole.LEFT_SHOULDER: joint = RehabJoint.LeftUpperArm; return true;
                case BodyTrackerRole.RIGHT_SHOULDER: joint = RehabJoint.RightUpperArm; return true;
                case BodyTrackerRole.LEFT_ELBOW: joint = RehabJoint.LeftElbow; return true;
                case BodyTrackerRole.RIGHT_ELBOW: joint = RehabJoint.RightElbow; return true;
                case BodyTrackerRole.LEFT_WRIST: joint = RehabJoint.LeftWrist; return true;
                case BodyTrackerRole.RIGHT_WRIST: joint = RehabJoint.RightWrist; return true;
                case BodyTrackerRole.LEFT_HAND: joint = RehabJoint.LeftHand; return true;
                case BodyTrackerRole.RIGHT_HAND: joint = RehabJoint.RightHand; return true;
                default:
                    joint = RehabJoint.Count;
                    return false;
            }
        }
    }
}
