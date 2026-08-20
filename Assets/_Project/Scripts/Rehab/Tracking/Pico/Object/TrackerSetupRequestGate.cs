namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    /// <summary>
    /// Prevents overlapping requests to the PICO system Motion Tracker setup UI.
    /// A request ends on SDK completion, immediate failure, stop, or app resume.
    /// </summary>
    internal sealed class TrackerSetupRequestGate
    {
        public bool IsInFlight { get; private set; }

        public bool TryBegin()
        {
            if (IsInFlight) return false;
            IsInFlight = true;
            return true;
        }

        public void Complete()
        {
            IsInFlight = false;
        }
    }
}
