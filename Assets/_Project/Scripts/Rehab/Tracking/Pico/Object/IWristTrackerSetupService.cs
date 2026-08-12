namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    public interface IWristTrackerSetupService
    {
        WristTrackerSetupState State { get; }
        string StatusMessage { get; }
        RehabTrackingPreference Preference { get; set; }
        RehabTrackingMode CurrentTrackingMode { get; }
        WristTrackerInfo LeftTracker { get; }
        WristTrackerInfo RightTracker { get; }
        int ConnectedTrackerCount { get; }
        bool HasValidBinding { get; }
        bool IsCalibrationReady { get; }
        bool IsWristTrackingReady { get; }
        bool IsHmdPoseValid { get; }
        bool DiagnosticsActive { get; }
        bool AdvancedDiagnosticsVisible { get; set; }
        int ProviderSwitchCount { get; }
        string LastError { get; }
        PicoWristObjectTrackingProvider Provider { get; }
        WristTrackerBindingManager Binding { get; }
        WristTrackerCalibration Calibration { get; }

        bool TryGetConnectedTrackerId(int index, out string trackerId);
        bool TryGetConnectedTrackerPose(int index, out PicoObjectTrackerPose pose);
        void RefreshTrackers();
        void BeginBinding();
        void CancelBinding();
        void ClearBinding();
        void BeginQuickVerification();
        void BeginCalibration();
        void CancelCalibration();
        void UseIdentityCalibration();
        void StartDiagnostics();
        void StopDiagnostics();
    }
}
