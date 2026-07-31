namespace PicoElderCare.Rehab.Tracking
{
    public static class BodySampleToLegacyAdapter
    {
        public static bool TryConvert(RehabBodySample bodySample, out RehabPoseSample legacySample)
        {
            legacySample = default(RehabPoseSample);
            if (bodySample == null)
            {
                return false;
            }

            RehabJointPose head;
            RehabJointPose leftWrist;
            RehabJointPose rightWrist;
            if (!bodySample.TryGetJoint(RehabJoint.Head, out head) ||
                !bodySample.TryGetJoint(RehabJoint.LeftWrist, out leftWrist) ||
                !bodySample.TryGetJoint(RehabJoint.RightWrist, out rightWrist))
            {
                return false;
            }

            legacySample.hasHead = true;
            legacySample.headPosition = head.position;
            legacySample.headRotation = head.rotation;
            legacySample.hasLeftHand = true;
            legacySample.leftHandPosition = leftWrist.position;
            legacySample.leftHandRotation = leftWrist.rotation;
            legacySample.hasRightHand = true;
            legacySample.rightHandPosition = rightWrist.position;
            legacySample.rightHandRotation = rightWrist.rotation;
            return true;
        }
    }
}
