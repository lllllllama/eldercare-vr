using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking
{
    [Serializable]
    public struct RehabJointPose
    {
        public bool valid;
        [Range(0f, 1f)] public float confidence;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 angularVelocity;
        public RehabTrackingSource source;
    }
}
