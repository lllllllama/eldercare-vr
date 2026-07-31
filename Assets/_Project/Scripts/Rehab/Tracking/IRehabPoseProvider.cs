namespace PicoElderCare.Rehab.Tracking
{
    public interface IRehabPoseProvider
    {
        bool IsSupported { get; }
        bool IsRunning { get; }
        RehabTrackingState TrackingState { get; }
        string StatusMessage { get; }

        void StartTracking();
        void StopTracking();
        bool TryGetSample(RehabBodySample target);
    }
}
