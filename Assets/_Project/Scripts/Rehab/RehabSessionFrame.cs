using System;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    /// <summary>
    /// User-relative frame captured from the stable natural pose prepared for a session.
    /// Facing is frozen at capture time so looking toward UI later cannot rotate movement axes.
    /// </summary>
    [Serializable]
    public struct RehabSessionFrame
    {
        public bool IsValid;
        public float NeutralHeadHeight;
        public float NeutralLeftWristHeight;
        public float NeutralRightWristHeight;
        public float ComfortableOverheadHeight;
        public Vector3 InitialFacingDirection;
        public Vector3 Origin;

        public static RehabSessionFrame Capture(
            RehabPoseSample sample,
            float overheadAboveHeadMeters,
            float minimumWristRiseMeters)
        {
            if (!sample.IsValid) return default(RehabSessionFrame);

            var facing = Vector3.ProjectOnPlane(sample.headRotation * Vector3.forward, Vector3.up);
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();

            return new RehabSessionFrame
            {
                IsValid = true,
                NeutralHeadHeight = sample.headPosition.y,
                NeutralLeftWristHeight = sample.leftHandPosition.y,
                NeutralRightWristHeight = sample.rightHandPosition.y,
                ComfortableOverheadHeight = Mathf.Max(
                    sample.headPosition.y + Mathf.Max(0f, overheadAboveHeadMeters),
                    Mathf.Max(sample.leftHandPosition.y, sample.rightHandPosition.y) +
                    Mathf.Max(0.1f, minimumWristRiseMeters)),
                InitialFacingDirection = facing,
                Origin = new Vector3(sample.headPosition.x, 0f, sample.headPosition.z)
            };
        }

        public Vector3 ToSessionLocal(Vector3 worldPosition)
        {
            if (!IsValid) return worldPosition;
            var yaw = Mathf.Atan2(InitialFacingDirection.x, InitialFacingDirection.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, -yaw, 0f) * (worldPosition - Origin);
        }
    }
}
