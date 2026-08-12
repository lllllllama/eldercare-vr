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
        bool RefreshTrackers();
        bool TryGetTrackerId(int index, out string trackerId);
        bool TryGetTrackerPose(string trackerId, out PicoObjectTrackerPose pose);
    }
}
