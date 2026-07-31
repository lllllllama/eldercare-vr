namespace PicoElderCare.Rehab.Tracking
{
    public enum RehabTrackingState
    {
        Unavailable,
        Unsupported,
        Starting,
        WaitingForDevice,
        WaitingForCalibration,
        Valid,
        Limited,
        Lost,
        Error
    }
}
