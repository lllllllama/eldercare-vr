using UnityEngine;

namespace PicoElderCare.Rehab.Tracking
{
    public abstract class RehabPoseProviderBase : MonoBehaviour, IRehabPoseProvider
    {
        public abstract bool IsSupported { get; }
        public abstract bool IsRunning { get; }
        public abstract RehabTrackingState TrackingState { get; }
        public abstract string StatusMessage { get; }

        public abstract void StartTracking();
        public abstract void StopTracking();
        public abstract bool TryGetSample(RehabBodySample target);
    }
}
