namespace PicoElderCare.Rehab.Tracking
{
    public enum RehabTrackingSource
    {
        Unknown,
        HmdDirect,
        ControllerDirect,
        ObjectTrackerDirect,
        BodyTrackingEstimated,
        HandTrackingObserved
    }

    public enum RehabTrackingPreference
    {
        Auto,
        ControllersOnly,
        WristTrackersOnly
    }

    public enum RehabTrackingMode
    {
        Unavailable,
        Controllers,
        WristTrackers
    }

    public struct RehabProviderChange
    {
        public RehabPoseProviderBase oldProvider;
        public RehabPoseProviderBase newProvider;
        public RehabTrackingMode oldMode;
        public RehabTrackingMode newMode;

        public RehabProviderChange(
            RehabPoseProviderBase oldProvider,
            RehabPoseProviderBase newProvider,
            RehabTrackingMode oldMode,
            RehabTrackingMode newMode)
        {
            this.oldProvider = oldProvider;
            this.newProvider = newProvider;
            this.oldMode = oldMode;
            this.newMode = newMode;
        }
    }
}
