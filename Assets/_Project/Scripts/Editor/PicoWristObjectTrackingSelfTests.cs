using System;
using PicoElderCare.Rehab;
using PicoElderCare.Rehab.Tracking;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking.Testing;
using UnityEditor;
using UnityEngine;

public static class PicoWristObjectTrackingSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run Wrist Object Tracking Self Tests")]
    public static void RunAll()
    {
        PicoWristTrackingLifecycleSelfTests.RunAll();
        Provider_ZeroTrackersNotReady();
        Provider_OneTrackerNotReady();
        Provider_TwoUnboundRequiresBinding();
        Provider_BoundInvalidPoseNotReady();
        Provider_StabilizesThenOutputsExactlyThreeJoints();
        Provider_LossClearsAndRecoveryRestabilizes();
        Api_StartTrackingDoesNotRequestTrackerSetup();
        Api_SetupRequestInFlightRejectsDuplicateRequest();
        Api_CompletionAndImmediateFailureReleaseSetupRequest();
        StatusPanel_FirstOpenRequestsTrackerSetupOnce();
        StatusPanel_RepeatedOpenDoesNotRepeatTrackerSetup();
        StatusPanel_CloseThenOpenStartsNewSetupSession();
        StatusPanel_ExplicitReconfigureIsASeparateUserAction();
        Binding_DefaultTimingAllowsVrReactionTime();
        Binding_UsesMovementAndStableIdsNotArrayIndex();
        Binding_DiagnosticsExposePendingLeftAndPersistentFailure();
        Binding_PersistsAcrossManagerRestart();
        QuickVerification_WarnsWhenRightTrackerMoves();
        Calibration_AppliesMountOffsets();
        Selector_AutoFallsBackThenReturnsToWrist();
        Selector_ControllersOnlyNeverUsesPrimary();
        Selector_WristOnlyNeverFallsBack();
        Selector_ProviderChangedFiresOncePerActualSwitch();
        MovementEvaluator_TransientResetPreservesMovementAndResults();
        Session_ProviderSwitchResetsCurrentMovementOnce();
        Runtime_EnsureIsDuplicateSafe();
        Debug.Log("PICO wrist Object Tracking self tests passed.");
    }

    private static void Provider_ZeroTrackersNotReady()
    {
        WithProvider(0, false, false, delegate(Context context)
        {
            AssertFalse(context.provider.TryGetSample(context.sample), "Zero trackers must not produce a wrist sample.");
            AssertEqual(WristTrackerSetupState.NoTracker, context.provider.SetupState, "Zero trackers should report NoTracker.");
        });
    }

    private static void Provider_OneTrackerNotReady()
    {
        WithProvider(1, false, false, delegate(Context context)
        {
            AssertFalse(context.provider.TryGetSample(context.sample), "One tracker must not produce a wrist sample.");
            AssertEqual(WristTrackerSetupState.OneTrackerOnly, context.provider.SetupState, "One tracker should report OneTrackerOnly.");
        });
    }

    private static void Provider_TwoUnboundRequiresBinding()
    {
        WithProvider(2, false, false, delegate(Context context)
        {
            AssertFalse(context.provider.TryGetSample(context.sample), "Unbound trackers must not become ready.");
            AssertEqual(WristTrackerSetupState.BindingRequired, context.provider.SetupState, "Two unbound trackers should require binding.");
        });
    }

    private static void Provider_BoundInvalidPoseNotReady()
    {
        WithProvider(2, true, true, delegate(Context context)
        {
            context.api.SetPoseValid(1, false);
            AssertFalse(context.provider.TryGetSample(context.sample), "An invalid bound pose must invalidate the wrist provider.");
            AssertEqual(WristTrackerSetupState.PoseLost, context.provider.SetupState, "Invalid pose should report PoseLost.");
        });
    }

    private static void Provider_StabilizesThenOutputsExactlyThreeJoints()
    {
        WithProvider(2, true, true, delegate(Context context)
        {
            context.provider.RequiredStableFrames = 3;
            AssertFalse(context.provider.TryGetSample(context.sample), "First valid frame should stabilize.");
            AssertFalse(context.provider.TryGetSample(context.sample), "Second valid frame should stabilize.");
            AssertTrue(context.provider.TryGetSample(context.sample), "Stable threshold should produce a sample.");
            AssertEqual(3, context.sample.validJointCount, "Wrist provider must expose exactly Head and two wrists.");

            RehabJointPose pose;
            AssertTrue(context.sample.TryGetJoint(RehabJoint.Head, out pose), "Head must be valid.");
            AssertEqual(RehabTrackingSource.HmdDirect, pose.source, "Head source should be HMD direct.");
            AssertTrue(context.sample.TryGetJoint(RehabJoint.LeftWrist, out pose), "LeftWrist must be valid.");
            AssertEqual(RehabTrackingSource.ObjectTrackerDirect, pose.source, "Left wrist source should be Object Tracker direct.");
            AssertTrue(context.sample.TryGetJoint(RehabJoint.RightWrist, out pose), "RightWrist must be valid.");
            AssertFalse(context.sample.TryGetJoint(RehabJoint.LeftElbow, out pose), "Elbow must remain invalid.");
            AssertFalse(context.sample.TryGetJoint(RehabJoint.Chest, out pose), "Chest must remain invalid.");
            AssertFalse(context.sample.TryGetJoint(RehabJoint.LeftHand, out pose), "Hand alias must remain invalid.");
            AssertFalse(context.sample.TryGetJoint(RehabJoint.Hips, out pose), "Hips must remain invalid.");
        });
    }

    private static void Provider_LossClearsAndRecoveryRestabilizes()
    {
        WithProvider(2, true, true, delegate(Context context)
        {
            context.provider.RequiredStableFrames = 2;
            context.provider.TryGetSample(context.sample);
            AssertTrue(context.provider.TryGetSample(context.sample), "Provider should first reach Ready.");
            context.api.SetPoseValid(0, false);
            AssertFalse(context.provider.TryGetSample(context.sample), "Lost pose must fail immediately.");
            AssertEqual(0, context.sample.validJointCount, "Lost pose must not retain the last sample.");
            context.api.SetPoseValid(0, true);
            AssertFalse(context.provider.TryGetSample(context.sample), "Recovery must stabilize again.");
            AssertTrue(context.provider.TryGetSample(context.sample), "Recovery should become ready only after the stable threshold.");
        });
    }

    private static void StatusPanel_FirstOpenRequestsTrackerSetupOnce()
    {
        var root = new GameObject("TrackerSettingsSessionTest", typeof(RectTransform), typeof(Canvas));
        try
        {
            var service = new FakeWristTrackerSetupService();
            var panel = root.AddComponent<PicoWristTrackingStatusPanel>();
            panel.Configure(null, service);
            panel.Open();
            AssertEqual(1, service.SetupRequestCount, "Opening tracker settings should issue exactly one explicit setup request.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void StatusPanel_RepeatedOpenDoesNotRepeatTrackerSetup()
    {
        WithStatusPanel(delegate(PicoWristTrackingStatusPanel panel, FakeWristTrackerSetupService service)
        {
            panel.Open();
            panel.Open();
            panel.Open();
            AssertEqual(1, service.SetupRequestCount, "Repeated Open calls in one visible settings session must not repeat setup.");
        });
    }

    private static void StatusPanel_CloseThenOpenStartsNewSetupSession()
    {
        WithStatusPanel(delegate(PicoWristTrackingStatusPanel panel, FakeWristTrackerSetupService service)
        {
            panel.Open();
            panel.Close();
            panel.Open();
            AssertEqual(2, service.SetupRequestCount, "Closing and reopening settings should allow one new setup request.");
        });
    }

    private static void StatusPanel_ExplicitReconfigureIsASeparateUserAction()
    {
        WithStatusPanel(delegate(PicoWristTrackingStatusPanel panel, FakeWristTrackerSetupService service)
        {
            panel.Open();
            var setup = panel.transform.Find("TrackerSettingsPanel/WoodFrame/Actions/Setup");
            var button = setup != null ? setup.GetComponent<UnityEngine.UI.Button>() : null;
            AssertTrue(button != null, "Tracker settings should expose the explicit reconfigure button.");
            button.onClick.Invoke();
            AssertEqual(2, service.SetupRequestCount, "Clicking reconfigure is a new explicit user setup action.");
        });
    }

    private static void Api_StartTrackingDoesNotRequestTrackerSetup()
    {
        var api = new FakePicoObjectTrackingApi();
        AssertTrue(api.StartTracking(), "Supported fake API should start.");
        AssertEqual(0, api.Diagnostics.setupRequestCount, "Starting the tracking runtime must not request PICO tracker setup.");
        api.Dispose();
    }

    private static void Api_SetupRequestInFlightRejectsDuplicateRequest()
    {
        var api = new FakePicoObjectTrackingApi();
        api.StartTracking();
        AssertTrue(api.RequestTrackerSetup(), "First explicit setup request should start.");
        AssertFalse(api.RequestTrackerSetup(), "A setup request already in flight must reject duplicate requests.");
        AssertEqual(1, api.Diagnostics.setupRequestCount, "Duplicate in-flight requests must not reach the SDK facade.");
        api.Dispose();
    }

    private static void Api_CompletionAndImmediateFailureReleaseSetupRequest()
    {
        var api = new FakePicoObjectTrackingApi();
        api.StartTracking();
        AssertTrue(api.RequestTrackerSetup(), "First explicit request should start.");
        api.CompleteSetupRequest();
        AssertTrue(api.RequestTrackerSetup(), "SDK completion should allow a later explicit request.");
        api.CompleteSetupRequest();

        api.SetupRequestSucceeds = false;
        AssertFalse(api.RequestTrackerSetup(), "Immediate SDK setup failure should be reported.");
        api.SetupRequestSucceeds = true;
        AssertTrue(api.RequestTrackerSetup(), "Immediate failure must release the in-flight guard.");
        AssertEqual(4, api.Diagnostics.setupRequestCount, "Each accepted explicit action should reach the SDK facade exactly once.");
        api.Dispose();
    }

    private static void WithStatusPanel(Action<PicoWristTrackingStatusPanel, FakeWristTrackerSetupService> action)
    {
        var root = new GameObject("TrackerSettingsSessionTest", typeof(RectTransform), typeof(Canvas));
        try
        {
            var service = new FakeWristTrackerSetupService();
            var panel = root.AddComponent<PicoWristTrackingStatusPanel>();
            panel.Configure(null, service);
            action(panel, service);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void Binding_UsesMovementAndStableIdsNotArrayIndex()
    {
        var api = CreateStartedApi(2);
        var store = new MemoryWristTrackerBindingStore();
        var binding = new WristTrackerBindingManager(api, store) { preparationSeconds = 0f, sampleWindowSeconds = 0.25f, minimumValidSamples = 3 };
        AssertTrue(binding.BeginBinding(), "Binding should start with two trackers.");
        MoveForWindow(api, binding, 1, 0, 5);
        AssertEqual(WristTrackerSetupState.BindingRight, binding.State, "Moving array slot one should identify left by id, not by index.");
        MoveForWindow(api, binding, 0, 1, 5);
        AssertEqual("tracker-B", binding.Profile.leftTrackerId, "The moved tracker id should bind to LeftWrist even when it is slot one.");
        AssertEqual("tracker-A", binding.Profile.rightTrackerId, "The remaining confirmed tracker id should bind to RightWrist.");
        api.Dispose();
    }

    private static void Binding_DefaultTimingAllowsVrReactionTime()
    {
        var api = CreateStartedApi(2);
        var binding = new WristTrackerBindingManager(api, new MemoryWristTrackerBindingStore());
        AssertTrue(binding.preparationSeconds >= 0.5f, "Binding should provide a readable preparation period before sampling.");
        AssertTrue(binding.sampleWindowSeconds >= 2f, "Binding should leave enough time for a VR user to move one wrist.");
        AssertTrue(binding.minimumTravelMeters <= 0.06f, "Binding travel should tolerate a comfortable wrist gesture.");
        AssertTrue(binding.winnerRatio <= 1.5f, "Binding dominance should remain clear without being excessively strict.");
        api.Dispose();
    }

    private static void Binding_PersistsAcrossManagerRestart()
    {
        var api = CreateStartedApi(2);
        var store = new MemoryWristTrackerBindingStore { left = "tracker-B", right = "tracker-A" };
        var restarted = new WristTrackerBindingManager(api, store);
        AssertTrue(restarted.IsBindingReady, "A new manager should restore saved roles from stable ids.");
        AssertEqual("tracker-B", restarted.Profile.leftTrackerId, "Restored left id should remain stable.");
        api.Dispose();
    }

    private static void Binding_DiagnosticsExposePendingLeftAndPersistentFailure()
    {
        var api = CreateStartedApi(2);
        var binding = new WristTrackerBindingManager(api, new MemoryWristTrackerBindingStore())
        {
            preparationSeconds = 0f,
            sampleWindowSeconds = 0.25f,
            minimumValidSamples = 3
        };
        AssertTrue(binding.BeginBinding(), "Diagnostic binding should start.");
        MoveForWindow(api, binding, 0, 1, 5);
        AssertEqual(WristTrackerSetupState.BindingRight, binding.State, "First stage should enter right binding.");
        AssertEqual("tracker-A", binding.PendingLeftTrackerId, "Pending left id must be visible before final save.");

        WristBindingSampleDiagnostics sample;
        AssertTrue(binding.TryGetSampleDiagnostics(0, out sample), "Right-stage raw sample diagnostics should be queryable.");
        AssertEqual("tracker-A", sample.trackerId, "Diagnostics should preserve stable tracker ids.");

        for (var i = 0; i < 5; i++) binding.Tick(0.06f);
        AssertTrue(binding.LastResultFailed, "An inconclusive right stage must retain a failed result.");
        AssertEqual("tracker-A", binding.LastPendingLeftTrackerId, "Failed final stage should retain the prior left candidate for diagnosis.");
        AssertTrue(binding.LastResultMessage.Contains("移动距离不足"), "Failure should identify the unmet movement criterion.");

        binding.Tick(0.01f);
        AssertEqual(binding.LastResultMessage, binding.StatusMessage, "The next idle tick must not overwrite the concrete failure reason.");
        api.Dispose();
    }

    private static void QuickVerification_WarnsWhenRightTrackerMoves()
    {
        var api = CreateStartedApi(2);
        var store = new MemoryWristTrackerBindingStore { left = "tracker-A", right = "tracker-B" };
        var binding = new WristTrackerBindingManager(api, store) { preparationSeconds = 0f, sampleWindowSeconds = 0.25f, minimumValidSamples = 3 };
        AssertTrue(binding.BeginQuickVerification(), "Verification should start with a valid binding.");
        MoveForWindow(api, binding, 1, 0, 5);
        AssertTrue(binding.StatusMessage.Contains("佩戴反了"), "Moving right during the left check should warn, not silently swap roles.");
        api.Dispose();
    }

    private static void Calibration_AppliesMountOffsets()
    {
        var api = CreateStartedApi(2);
        var bindingStore = new MemoryWristTrackerBindingStore { left = "tracker-A", right = "tracker-B" };
        var binding = new WristTrackerBindingManager(api, bindingStore);
        var calibrationStore = new MemoryWristTrackerCalibrationStore();
        calibrationStore.saved.leftReady = true;
        calibrationStore.saved.rightReady = true;
        calibrationStore.saved.leftPositionOffset = new Vector3(0.1f, 0f, 0f);
        calibrationStore.saved.leftRotationOffset = Quaternion.Euler(0f, 15f, 0f);
        calibrationStore.saved.rightRotationOffset = Quaternion.identity;
        var calibration = new WristTrackerCalibration(api, binding, calibrationStore);
        PicoObjectTrackerPose raw;
        PicoObjectTrackerPose wrist;
        api.TryGetTrackerPose("tracker-A", out raw);
        AssertTrue(calibration.TryApplyLeft(raw, out wrist), "Ready calibration should apply.");
        AssertVector(raw.position + raw.rotation * calibration.Profile.leftPositionOffset, wrist.position, "Position offset should be tracker local.");
        api.Dispose();
    }

    private static void Selector_AutoFallsBackThenReturnsToWrist()
    {
        WithSelector(delegate(GameObject root, ToggleProvider primary, ToggleProvider fallback, RehabPoseProviderSelector selector, RehabBodySample sample)
        {
            primary.available = false;
            fallback.available = true;
            selector.Preference = RehabTrackingPreference.Auto;
            AssertTrue(selector.TryGetSample(sample), "Auto should use a valid controller fallback.");
            AssertEqual(fallback, selector.CurrentProvider, "Fallback should be active while wrist is unavailable.");
            primary.available = true;
            AssertTrue(selector.TryGetSample(sample), "Auto should use wrist when it becomes valid.");
            AssertEqual(primary, selector.CurrentProvider, "Primary wrist provider should regain priority.");
        });
    }

    private static void Selector_ControllersOnlyNeverUsesPrimary()
    {
        WithSelector(delegate(GameObject root, ToggleProvider primary, ToggleProvider fallback, RehabPoseProviderSelector selector, RehabBodySample sample)
        {
            primary.available = fallback.available = true;
            selector.Preference = RehabTrackingPreference.ControllersOnly;
            AssertTrue(selector.TryGetSample(sample), "ControllersOnly should produce a controller sample.");
            AssertEqual(fallback, selector.CurrentProvider, "ControllersOnly must never use the wrist primary.");
        });
    }

    private static void Selector_WristOnlyNeverFallsBack()
    {
        WithSelector(delegate(GameObject root, ToggleProvider primary, ToggleProvider fallback, RehabPoseProviderSelector selector, RehabBodySample sample)
        {
            primary.available = false;
            fallback.available = true;
            selector.Preference = RehabTrackingPreference.WristTrackersOnly;
            AssertFalse(selector.TryGetSample(sample), "WristTrackersOnly must not fallback to controllers.");
            AssertEqual(RehabTrackingMode.Unavailable, selector.CurrentTrackingMode, "Unavailable wrist-only input should expose no current mode.");
        });
    }

    private static void Selector_ProviderChangedFiresOncePerActualSwitch()
    {
        WithSelector(delegate(GameObject root, ToggleProvider primary, ToggleProvider fallback, RehabPoseProviderSelector selector, RehabBodySample sample)
        {
            var eventCount = 0;
            selector.ProviderChanged += delegate { eventCount++; };
            primary.available = false;
            fallback.available = true;
            selector.TryGetSample(sample);
            var afterFallback = eventCount;
            selector.TryGetSample(sample);
            AssertEqual(afterFallback, eventCount, "Repeated samples from one provider must not repeat ProviderChanged.");
            primary.available = true;
            selector.TryGetSample(sample);
            AssertEqual(afterFallback + 1, eventCount, "One actual fallback-to-primary switch should emit exactly one event.");
        });
    }

    private static void MovementEvaluator_TransientResetPreservesMovementAndResults()
    {
        var root = new GameObject("MovementTransientResetTest");
        try
        {
            var evaluator = root.AddComponent<MovementEvaluator>();
            evaluator.ResetEvaluation();
            var movement = evaluator.CurrentMovement;
            evaluator.ResetCurrentMovementTransientState();
            AssertEqual(movement, evaluator.CurrentMovement, "Transient reset must preserve the current movement.");
            AssertEqual(0, evaluator.MovementResults.Count, "Transient reset must not synthesize or clear unrelated session results.");
            AssertTrue(Mathf.Approximately(0f, evaluator.CurrentCompletion), "Transient completion should reset to zero.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Session_ProviderSwitchResetsCurrentMovementOnce()
    {
        var root = new GameObject("SessionProviderSwitchTest");
        try
        {
            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            var handTracker = root.AddComponent<HandPoseTracker>();
            handTracker.hmdTransform = head;
            handTracker.leftControllerTransform = new GameObject("Left").transform;
            handTracker.leftControllerTransform.SetParent(root.transform, false);
            handTracker.rightControllerTransform = new GameObject("Right").transform;
            handTracker.rightControllerTransform.SetParent(root.transform, false);

            var primary = root.AddComponent<ToggleProvider>();
            var fallbackObject = new GameObject("Fallback");
            fallbackObject.transform.SetParent(root.transform, false);
            var fallback = fallbackObject.AddComponent<ToggleProvider>();
            primary.available = false;
            fallback.available = true;

            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = primary;
            selector.FallbackProvider = fallback;
            selector.StartTracking();

            var evaluator = root.AddComponent<MovementEvaluator>();
            var safety = root.AddComponent<SafetyMonitor>();
            safety.hmdTransform = head;
            var recorder = root.AddComponent<TrainingResultRecorder>();
            var session = root.AddComponent<RehabSessionManager>();
            session.handPoseTracker = handTracker;
            session.poseProviderSelector = selector;
            session.movementEvaluator = evaluator;
            session.safetyMonitor = safety;
            session.resultRecorder = recorder;
            session.autoCreateVirtualCoach = false;
            session.autoStartSession = false;
            session.placeTrainingAreaOnStart = false;
            session.BeginSession();

            var sample = new RehabBodySample();
            selector.TryGetSample(sample);
            var beforeSwitch = session.TrackingSourceResetCount;
            primary.available = true;
            selector.TryGetSample(sample);
            AssertEqual(beforeSwitch + 1, session.TrackingSourceResetCount, "One provider switch should reset Session transient state exactly once.");
            selector.TryGetSample(sample);
            AssertEqual(beforeSwitch + 1, session.TrackingSourceResetCount, "Stable provider samples must not repeat Session reset.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Runtime_EnsureIsDuplicateSafe()
    {
        var first = WristTrackingRuntime.EnsureInstance();
        var second = WristTrackingRuntime.EnsureInstance();
        AssertEqual(first, second, "Runtime Ensure should reuse the singleton.");
        if (first != null) UnityEngine.Object.DestroyImmediate(first.gameObject);
    }

    private static void WithProvider(int trackerCount, bool bound, bool calibrated, Action<Context> action)
    {
        var root = new GameObject("WristProviderTest");
        try
        {
            var api = CreateStartedApi(trackerCount);
            var bindingStore = new MemoryWristTrackerBindingStore();
            if (bound) { bindingStore.left = "tracker-A"; bindingStore.right = "tracker-B"; }
            var binding = new WristTrackerBindingManager(api, bindingStore);
            var calibrationStore = new MemoryWristTrackerCalibrationStore();
            if (calibrated)
            {
                calibrationStore.saved.leftReady = true;
                calibrationStore.saved.rightReady = true;
                calibrationStore.saved.identityCalibrationExplicitlyAccepted = true;
            }
            var calibration = new WristTrackerCalibration(api, binding, calibrationStore);
            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.position = new Vector3(0f, 1.65f, 0f);
            var provider = root.AddComponent<PicoWristObjectTrackingProvider>();
            provider.Configure(api, binding, calibration, head, null);
            provider.StartTracking();
            action(new Context { api = api, provider = provider, sample = new RehabBodySample() });
            api.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static FakePicoObjectTrackingApi CreateStartedApi(int trackerCount)
    {
        var api = new FakePicoObjectTrackingApi();
        if (trackerCount > 0) api.SetTracker(0, "tracker-A", new Vector3(-0.25f, 1.2f, 0.5f), Quaternion.identity);
        if (trackerCount > 1) api.SetTracker(1, "tracker-B", new Vector3(0.25f, 1.2f, 0.5f), Quaternion.identity);
        api.StartTracking();
        return api;
    }

    private static void MoveForWindow(FakePicoObjectTrackingApi api, WristTrackerBindingManager binding, int movingSlot, int stillSlot, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            api.SetPosition(movingSlot, new Vector3(movingSlot == 0 ? -0.25f : 0.25f, 1.2f + i * 0.05f, 0.5f));
            api.SetPosition(stillSlot, new Vector3(stillSlot == 0 ? -0.25f : 0.25f, 1.2f, 0.5f));
            binding.Tick(0.06f);
        }
    }

    private static void WithSelector(Action<GameObject, ToggleProvider, ToggleProvider, RehabPoseProviderSelector, RehabBodySample> action)
    {
        var root = new GameObject("WristSelectorTest");
        try
        {
            var primary = root.AddComponent<ToggleProvider>();
            var fallbackObject = new GameObject("Fallback");
            fallbackObject.transform.SetParent(root.transform, false);
            var fallback = fallbackObject.AddComponent<ToggleProvider>();
            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = primary;
            selector.FallbackProvider = fallback;
            selector.StartTracking();
            action(root, primary, fallback, selector, new RehabBodySample());
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private sealed class Context
    {
        public FakePicoObjectTrackingApi api;
        public PicoWristObjectTrackingProvider provider;
        public RehabBodySample sample;
    }

    private sealed class ToggleProvider : RehabPoseProviderBase
    {
        public bool available;
        private bool _running;
        public override bool IsSupported { get { return true; } }
        public override bool IsRunning { get { return _running; } }
        public override RehabTrackingState TrackingState { get { return available ? RehabTrackingState.Valid : RehabTrackingState.Lost; } }
        public override string StatusMessage { get { return available ? "Ready" : "Lost"; } }
        public override void StartTracking() { _running = true; }
        public override void StopTracking() { _running = false; }
        public override bool TryGetSample(RehabBodySample target)
        {
            target.Clear();
            target.trackingState = TrackingState;
            if (!_running || !available) return false;
            target.SetJoint(RehabJoint.Head, Pose(Vector3.up));
            target.SetJoint(RehabJoint.LeftWrist, Pose(Vector3.left));
            target.SetJoint(RehabJoint.RightWrist, Pose(Vector3.right));
            return true;
        }
        private static RehabJointPose Pose(Vector3 position) { return new RehabJointPose { valid = true, confidence = 1f, position = position, rotation = Quaternion.identity }; }
    }

    private static void AssertTrue(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void AssertFalse(bool condition, string message) { AssertTrue(!condition, message); }
    private static void AssertEqual<T>(T expected, T actual, string message) { if (!Equals(expected, actual)) throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual); }
    private static void AssertVector(Vector3 expected, Vector3 actual, string message) { if (Vector3.Distance(expected, actual) > 0.0001f) throw new InvalidOperationException(message); }
}
