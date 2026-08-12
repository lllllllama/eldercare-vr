using System;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [Serializable]
    public sealed class PicoObjectTrackingDiagnostics
    {
        public int discoveryResult;
        public int poseResult;
        public int connectedTrackerCount;
        public int successfulPoseCount;
        public int failedPoseCount;
        public int refreshCount;
        public int providerSwitchCount;
        public string lastError = string.Empty;
    }
}
