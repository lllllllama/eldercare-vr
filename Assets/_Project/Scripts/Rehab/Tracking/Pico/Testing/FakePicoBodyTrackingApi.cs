#if UNITY_EDITOR
namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class FakePicoBodyTrackingApi : IPicoBodyTrackingApi
    {
        public bool supported = true;
        public int supportResult;
        public int startResult;
        public int stopResult;
        public int stateResult;
        public int dataResult;
        public int calibrationResult;
        public string errorMessage = string.Empty;
        public PicoBodyTrackingApiState trackingState;
        public PicoBodyTrackingFrame bodyData = new PicoBodyTrackingFrame();

        public int supportCallCount;
        public int startCallCount;
        public int stopCallCount;
        public int stateCallCount;
        public int dataCallCount;
        public int calibrationCallCount;

        public string LastError
        {
            get { return errorMessage; }
        }

        public int GetBodyTrackingSupported(out bool isSupported)
        {
            supportCallCount++;
            isSupported = supported;
            return supportResult;
        }

        public int StartBodyTracking()
        {
            startCallCount++;
            return startResult;
        }

        public int StopBodyTracking()
        {
            stopCallCount++;
            return stopResult;
        }

        public int GetBodyTrackingState(out PicoBodyTrackingApiState state)
        {
            stateCallCount++;
            state = trackingState;
            return stateResult;
        }

        public int GetBodyTrackingData(PicoBodyTrackingFrame target)
        {
            dataCallCount++;
            if (dataResult == 0 && target != null)
            {
                target.CopyFrom(bodyData);
            }
            else if (target != null)
            {
                target.Clear();
            }

            return dataResult;
        }

        public int StartMotionTrackerCalibApp()
        {
            calibrationCallCount++;
            return calibrationResult;
        }

        public void Dispose()
        {
        }
    }
}
#endif
