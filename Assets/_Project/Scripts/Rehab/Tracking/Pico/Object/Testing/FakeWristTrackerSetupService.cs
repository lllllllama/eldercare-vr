namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking.Testing
{
    public sealed class FakeWristTrackerSetupService : IWristTrackerSetupService
    {
        public int SetupRequestCount { get; private set; }
        public bool SetupRequestSucceeds = true;

        public WristTrackerSetupState State { get { return WristTrackerSetupState.NoTracker; } }
        public string StatusMessage { get { return "Test"; } }
        public RehabTrackingPreference Preference { get; set; }
        public RehabTrackingMode CurrentTrackingMode { get { return RehabTrackingMode.Controllers; } }
        public WristTrackerInfo LeftTracker { get { return default(WristTrackerInfo); } }
        public WristTrackerInfo RightTracker { get { return default(WristTrackerInfo); } }
        public int ConnectedTrackerCount { get { return 0; } }
        public bool HasValidBinding { get { return false; } }
        public bool IsCalibrationReady { get { return false; } }
        public bool IsWristTrackingReady { get { return false; } }
        public bool IsHmdPoseValid { get { return true; } }
        public bool DiagnosticsActive { get; private set; }
        public bool AdvancedDiagnosticsVisible { get; set; }
        public int ProviderSwitchCount { get { return 0; } }
        public string LastError { get { return string.Empty; } }
        public PicoWristObjectTrackingProvider Provider { get { return null; } }
        public WristTrackerBindingManager Binding { get { return null; } }
        public WristTrackerCalibration Calibration { get { return null; } }

        public bool TryGetConnectedTrackerId(int index, out string trackerId)
        {
            trackerId = string.Empty;
            return false;
        }

        public bool TryGetConnectedTrackerPose(int index, out PicoObjectTrackerPose pose)
        {
            pose = default(PicoObjectTrackerPose);
            return false;
        }

        public bool RequestTrackerSetup()
        {
            SetupRequestCount++;
            return SetupRequestSucceeds;
        }

        public void BeginBinding() { }
        public void CancelBinding() { }
        public void ClearBinding() { }
        public void BeginQuickVerification() { }
        public void BeginCalibration() { }
        public void CancelCalibration() { }
        public void UseIdentityCalibration() { }
        public void StartDiagnostics() { DiagnosticsActive = true; }
        public void StopDiagnostics() { DiagnosticsActive = false; }
    }
}
