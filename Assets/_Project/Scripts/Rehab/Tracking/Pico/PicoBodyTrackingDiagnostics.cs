using System;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    [Serializable]
    public sealed class PicoBodyTrackingDiagnostics
    {
        public int supportResult;
        public int startResult;
        public int stateResult;
        public int dataResult;
        public int stopResult;
        public int calibrationResult;
        public int validJointCount;
        public int successfulSampleCount;
        public int failedSampleCount;
        public RehabTrackingState trackingState = RehabTrackingState.Unavailable;
        public string lastError = string.Empty;
    }
}
