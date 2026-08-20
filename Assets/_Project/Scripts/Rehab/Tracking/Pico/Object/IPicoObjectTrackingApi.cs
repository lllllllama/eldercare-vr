using System;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    public interface IPicoObjectTrackingApi : IDisposable
    {
        bool IsSupported { get; }
        bool IsRunning { get; }
        int ConnectedTrackerCount { get; }
        string LastError { get; }
        PicoObjectTrackingDiagnostics Diagnostics { get; }

        bool StartTracking();
        void StopTracking();
        /// <summary>Explicit user request that may open the PICO system Motion Tracker setup UI.</summary>
        bool RequestTrackerSetup();
        /// <summary>Releases request state after returning from system UI without starting another request.</summary>
        void ReconcileAfterApplicationResume();
        bool TryGetTrackerId(int index, out string trackerId);
        bool TryGetTrackerPose(string trackerId, out PicoObjectTrackerPose pose);
    }
}
