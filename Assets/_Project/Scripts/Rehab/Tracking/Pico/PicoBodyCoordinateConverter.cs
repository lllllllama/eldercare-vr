using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public enum PicoBodyTrackingOutputSpace
    {
        XrOriginLocal,
        World
    }

    public static class PicoBodyCoordinateConverter
    {
        public static bool TryConvert(
            PicoBodyJointData source,
            PicoBodyTrackingOutputSpace outputSpace,
            Transform xrOrigin,
            RehabTrackingState trackingState,
            out RehabJointPose pose)
        {
            pose = default(RehabJointPose);
            if (!source.valid)
            {
                return false;
            }

            if (outputSpace == PicoBodyTrackingOutputSpace.World && xrOrigin == null)
            {
                return false;
            }

            var position = source.position;
            var rotation = source.rotation;
            var velocity = source.velocity;
            var acceleration = source.acceleration;
            var angularVelocity = source.angularVelocity;
            if (outputSpace == PicoBodyTrackingOutputSpace.World)
            {
                position = xrOrigin.TransformPoint(position);
                rotation = xrOrigin.rotation * rotation;
                velocity = xrOrigin.TransformVector(velocity);
                acceleration = xrOrigin.TransformVector(acceleration);
                angularVelocity = xrOrigin.TransformDirection(angularVelocity);
            }

            pose.valid = true;
            // PICO 3.4.0 does not expose a reliable per-joint probability. This value is an
            // engineering quality marker derived from the frame state, not an SDK confidence.
            pose.confidence = trackingState == RehabTrackingState.Valid ? 1f : 0.5f;
            pose.position = position;
            pose.rotation = rotation;
            pose.velocity = velocity;
            pose.acceleration = acceleration;
            pose.angularVelocity = angularVelocity;
            return true;
        }
    }
}
