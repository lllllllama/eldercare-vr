using System;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [Serializable]
    public sealed class PicoObjectTrackingDiagnostics
    {
        public int setupRequestResult;
        public int poseResult;
        public int connectedTrackerCount;
        public int successfulPoseCount;
        public int failedPoseCount;
        public int setupRequestCount;
        public bool setupRequestInFlight;
        public int providerSwitchCount;
        public string lastError = string.Empty;
    }
}
