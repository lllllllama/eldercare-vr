using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [Serializable]
    public struct PicoObjectTrackerPose
    {
        public string trackerId;
        public bool connected;
        public bool poseValid;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public double timestamp;
    }

    [Serializable]
    public struct WristTrackerInfo
    {
        public string trackerId;
        public bool bound;
        public bool connected;
        public bool poseValid;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public double lastUpdateAgeSeconds;
        public int stableFrameCount;
    }

    /// <summary>Read-only snapshot used by setup/debug UI while binding is in progress.</summary>
    [Serializable]
    public struct WristBindingSampleDiagnostics
    {
        public string trackerId;
        public bool poseValid;
        public Vector3 position;
        public int validSamples;
        public float travelMeters;
    }

    public enum WristTrackerSetupState
    {
        Unsupported,
        NoTracker,
        OneTrackerOnly,
        BindingRequired,
        BindingLeft,
        BindingRight,
        VerifyingLeft,
        CalibrationRequired,
        Calibrating,
        Stabilizing,
        Ready,
        PoseLost,
        ApiError
    }
}
