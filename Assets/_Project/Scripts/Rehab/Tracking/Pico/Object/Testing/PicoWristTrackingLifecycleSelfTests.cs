using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking.Testing
{
    public static class PicoWristTrackingLifecycleSelfTests
    {
        public static void RunAll()
        {
            Runtime_StartupAndUpdateNeverRequestTrackerSetup();
            Runtime_ResumeClearsInFlightWithoutRequestingSetup();
        }

        private static void Runtime_StartupAndUpdateNeverRequestTrackerSetup()
        {
            var runtime = WristTrackingRuntime.EnsureInstance();
            try
            {
                var api = new FakePicoObjectTrackingApi();
                runtime.ReplaceApiForTesting(api);

                AssertEqual(1, api.StartCount, "Runtime should start the tracking API once.");
                AssertEqual(0, api.Diagnostics.setupRequestCount, "Runtime startup must not request tracker setup.");

                for (var i = 0; i < 20; i++) runtime.TickRuntime(0.75f);
                AssertEqual(0, api.Diagnostics.setupRequestCount, "Runtime ticks must never request tracker setup.");
            }
            finally
            {
                if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime.gameObject);
            }
        }

        private static void Runtime_ResumeClearsInFlightWithoutRequestingSetup()
        {
            var runtime = WristTrackingRuntime.EnsureInstance();
            try
            {
                var api = new FakePicoObjectTrackingApi();
                runtime.ReplaceApiForTesting(api);
                if (!api.RequestTrackerSetup())
                    throw new InvalidOperationException("Explicit setup request should enter the in-flight state.");

                runtime.ReconcileAfterApplicationResume();
                AssertEqual(1, api.Diagnostics.setupRequestCount, "Application resume must not issue another setup request.");
                AssertEqual(false, api.Diagnostics.setupRequestInFlight, "Application resume should release a setup request whose system UI was cancelled.");

                if (!api.RequestTrackerSetup())
                    throw new InvalidOperationException("A new explicit user request should be allowed after resume reconciliation.");
                AssertEqual(2, api.Diagnostics.setupRequestCount, "Only the later explicit user request should reach setup again.");
            }
            finally
            {
                if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime.gameObject);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual);
        }
    }
}
