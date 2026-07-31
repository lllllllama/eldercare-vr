using System;
using Unity.XR.PXR;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public struct PicoBodyTrackingApiState
    {
        public bool isTracking;
        public BodyTrackingStatusCode statusCode;
        public BodyTrackingMessage message;
    }

    public interface IPicoBodyTrackingApi : IDisposable
    {
        string LastError { get; }

        int GetBodyTrackingSupported(out bool supported);
        int StartBodyTracking();
        int StopBodyTracking();
        int GetBodyTrackingState(out PicoBodyTrackingApiState state);
        int GetBodyTrackingData(PicoBodyTrackingFrame target);
        int StartMotionTrackerCalibApp();
    }
}
