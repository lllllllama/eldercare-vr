#if UNITY_EDITOR
using PicoElderCare.Rehab;
using PicoElderCare.Rehab.Tracking;
using PicoElderCare.Rehab.Tracking.Pico;
using TMPro;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.UI;

public static class PicoBodyTrackingSelfTests
{
    public static void RunAll()
    {
        PicoProvider_ReportsUnsupported();
        PicoProvider_StartsWhenSupported();
        PicoProvider_HandlesStartFailure();
        PicoProvider_ReportsWaitingForCalibration();
        PicoStatusMapper_TreatsUnknownInvalidStateAsCalibrationRequired();
        PicoProvider_CanRequestCalibration();
        PicoProvider_MapsValidBodyData();
        PicoProvider_ReportsLimitedState();
        PicoProvider_ClearsSampleWhenTrackingLost();
        PicoProvider_HandlesGetDataFailure();
        PicoProvider_StopsOnlyAfterStarting();
        PicoJointMapper_MapsRequiredUpperBodyJoints();
        PicoJointMapper_MapsRequiredLowerBodyJoints();
        PicoJointMapper_RejectsNoneRole();
        PicoProvider_ReusesSampleStorage();
        PicoProvider_DoesNotLeakSdkTypesIntoBodySample();
        PicoProvider_ConvertsLocalPoseToWorldSpace();
        PicoApi_ConvertsNativeCoordinatesWithoutWritingPastMotionVectors();
        ProviderSelector_DoesNotFallbackFromLimitedPicoSample();
        PicoStatusPanel_CreatesHeadLockedWorldCanvas();
        PicoStatusPanel_ReusesUiObjects();
        PicoDebugRenderer_DoesNotOwnStatusUi();
        Debug.Log("PICO body tracking self tests passed.");
    }

    private static void PicoProvider_ReportsUnsupported()
    {
        var root = new GameObject("PicoProviderUnsupportedTest");
        try
        {
            var fake = CreateValidFake();
            fake.supported = false;
            var provider = CreateProvider(root, fake);
            provider.StartTracking();

            AssertTrue(!provider.IsSupported, "Unsupported devices should report IsSupported=false.");
            AssertTrue(!provider.IsRunning, "Unsupported devices must not start body tracking.");
            AssertTrue(provider.TrackingState == RehabTrackingState.Unsupported, "Unsupported devices should expose Unsupported state.");
            AssertTrue(fake.startCallCount == 0, "Unsupported devices must not call StartBodyTracking.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_StartsWhenSupported()
    {
        var root = new GameObject("PicoProviderStartsTest");
        try
        {
            var fake = CreateValidFake();
            var provider = CreateProvider(root, fake);
            provider.StartTracking();

            AssertTrue(provider.IsSupported, "Supported devices should report IsSupported=true.");
            AssertTrue(provider.IsRunning, "A successful start should mark the provider running.");
            AssertTrue(fake.startCallCount == 1, "Supported providers should start exactly once.");
            AssertTrue(provider.TrackingState == RehabTrackingState.Valid, "Initial valid SDK state should map to Valid.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_HandlesStartFailure()
    {
        var root = new GameObject("PicoProviderStartFailureTest");
        try
        {
            var fake = CreateValidFake();
            fake.startResult = 1;
            fake.errorMessage = "Fake start failure";
            var provider = CreateProvider(root, fake);
            provider.StartTracking();

            AssertTrue(!provider.IsRunning, "A failed start must not mark the provider running.");
            AssertTrue(provider.TrackingState == RehabTrackingState.Error, "A failed start should report Error.");
            AssertTrue(provider.Diagnostics.lastError == fake.errorMessage, "Fake API error details should reach diagnostics.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_ReportsWaitingForCalibration()
    {
        var root = new GameObject("PicoProviderCalibrationStateTest");
        try
        {
            var fake = CreateValidFake();
            fake.trackingState = CreateState(
                false,
                BodyTrackingStatusCode.BT_INVALID,
                BodyTrackingMessage.BT_MESSAGE_TRACKER_NOT_CALIBRATED);
            var provider = CreateProvider(root, fake);
            provider.StartTracking();

            AssertTrue(provider.IsRunning, "Tracking service can run while waiting for calibration.");
            AssertTrue(provider.TrackingState == RehabTrackingState.WaitingForCalibration, "Not-calibrated SDK state should map to WaitingForCalibration.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_CanRequestCalibration()
    {
        var root = new GameObject("PicoProviderCalibrationRequestTest");
        try
        {
            var fake = CreateValidFake();
            fake.trackingState = CreateState(
                false,
                BodyTrackingStatusCode.BT_INVALID,
                BodyTrackingMessage.BT_MESSAGE_TRACKER_NOT_CALIBRATED);
            var provider = CreateProvider(root, fake);
            provider.StartTracking();

            AssertTrue(provider.RequestCalibration(), "Explicit calibration requests should report SDK success.");
            AssertTrue(fake.calibrationCallCount == 1, "Calibration app should open only when explicitly requested.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoStatusMapper_TreatsUnknownInvalidStateAsCalibrationRequired()
    {
        var state = CreateState(
            false,
            BodyTrackingStatusCode.BT_INVALID,
            BodyTrackingMessage.BT_MESSAGE_UNKNOWN);
        AssertTrue(
            PicoBodyTrackingStatusMapper.Map(state) == RehabTrackingState.WaitingForCalibration,
            "The SDK's initial unknown/invalid state should request calibration, matching the installed SDK sample.");
    }

    private static void PicoProvider_MapsValidBodyData()
    {
        var root = new GameObject("PicoProviderValidDataTest");
        try
        {
            var fake = CreateValidFake();
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0.1f)));
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.SPINE3, new Vector3(0f, 1.25f, 0f)));
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.Pelvis, new Vector3(0f, 0.95f, 0f)));
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.LEFT_WRIST, new Vector3(-0.4f, 1.2f, 0.2f)));
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.RIGHT_WRIST, new Vector3(0.4f, 1.2f, 0.2f)));
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            var sample = new RehabBodySample();

            AssertTrue(provider.TryGetSample(sample), "Valid PICO body data should produce a sample.");
            AssertTrue(sample.trackingState == RehabTrackingState.Valid && sample.IsTrackingUsable, "Valid body samples should be usable for training.");
            AssertJointPosition(sample, RehabJoint.Head, new Vector3(0f, 1.7f, 0.1f), "HEAD should map to Head.");
            AssertJointPosition(sample, RehabJoint.Chest, new Vector3(0f, 1.25f, 0f), "SPINE3 should map to Chest.");
            AssertJointPosition(sample, RehabJoint.Hips, new Vector3(0f, 0.95f, 0f), "Pelvis should map to Hips.");
            AssertJointPosition(sample, RehabJoint.LeftWrist, new Vector3(-0.4f, 1.2f, 0.2f), "LEFT_WRIST should map to LeftWrist.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_ReportsLimitedState()
    {
        var root = new GameObject("PicoProviderLimitedTest");
        try
        {
            var fake = CreateValidFake();
            fake.trackingState = CreateState(
                true,
                BodyTrackingStatusCode.BT_LIMITED,
                BodyTrackingMessage.BT_MESSAGE_UNKNOWN);
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0f)));
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            var sample = new RehabBodySample();

            AssertTrue(provider.TryGetSample(sample), "Limited tracking should still expose joints for diagnostics.");
            AssertTrue(sample.trackingState == RehabTrackingState.Limited, "Limited SDK state should remain Limited.");
            AssertTrue(!sample.IsTrackingUsable, "Limited samples must not be usable for action timing.");
            RehabJointPose head;
            sample.TryGetJoint(RehabJoint.Head, out head);
            AssertTrue(Mathf.Abs(head.confidence - 0.5f) < 0.0001f, "Limited confidence should be an engineering quality marker.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_ClearsSampleWhenTrackingLost()
    {
        var root = new GameObject("PicoProviderLostTest");
        try
        {
            var fake = CreateValidFake();
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0f)));
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            var sample = new RehabBodySample();
            AssertTrue(provider.TryGetSample(sample), "Initial valid sample should be available.");

            fake.trackingState = CreateState(
                false,
                BodyTrackingStatusCode.BT_INVALID,
                BodyTrackingMessage.BT_MESSAGE_TRACKER_PERSISTENT_INVISIBILITY);
            AssertTrue(!provider.TryGetSample(sample), "Lost tracking should not return a usable sample.");
            AssertTrue(sample.trackingState == RehabTrackingState.Lost, "Persistent invisibility should map to Lost.");
            AssertTrue(sample.validJointCount == 0, "Lost tracking must clear all joints from the previous frame.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_HandlesGetDataFailure()
    {
        var root = new GameObject("PicoProviderDataFailureTest");
        try
        {
            var fake = CreateValidFake();
            fake.dataResult = 1;
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            var sample = new RehabBodySample();

            AssertTrue(!provider.TryGetSample(sample), "SDK data failures should not return stale data.");
            AssertTrue(provider.TrackingState == RehabTrackingState.Error, "SDK data failures should report Error.");
            AssertTrue(sample.validJointCount == 0, "SDK data failures must leave all joints invalid.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_StopsOnlyAfterStarting()
    {
        var unsupportedRoot = new GameObject("PicoProviderNoStopTest");
        var runningRoot = new GameObject("PicoProviderStopTest");
        try
        {
            var unsupportedFake = CreateValidFake();
            unsupportedFake.supported = false;
            var unsupported = CreateProvider(unsupportedRoot, unsupportedFake);
            unsupported.StartTracking();
            unsupported.StopTracking();
            AssertTrue(unsupportedFake.stopCallCount == 0, "A provider that never started must not call StopBodyTracking.");

            var runningFake = CreateValidFake();
            var running = CreateProvider(runningRoot, runningFake);
            running.StartTracking();
            running.StopTracking();
            running.StopTracking();
            AssertTrue(runningFake.stopCallCount == 1, "A started provider should stop exactly once.");
        }
        finally
        {
            Object.DestroyImmediate(unsupportedRoot);
            Object.DestroyImmediate(runningRoot);
        }
    }

    private static void PicoJointMapper_MapsRequiredUpperBodyJoints()
    {
        AssertMapping(BodyTrackerRole.HEAD, RehabJoint.Head);
        AssertMapping(BodyTrackerRole.NECK, RehabJoint.Neck);
        AssertMapping(BodyTrackerRole.SPINE3, RehabJoint.Chest);
        AssertMapping(BodyTrackerRole.LEFT_COLLAR, RehabJoint.LeftShoulder);
        AssertMapping(BodyTrackerRole.LEFT_SHOULDER, RehabJoint.LeftUpperArm);
        AssertMapping(BodyTrackerRole.LEFT_ELBOW, RehabJoint.LeftElbow);
        AssertMapping(BodyTrackerRole.LEFT_WRIST, RehabJoint.LeftWrist);
        AssertMapping(BodyTrackerRole.RIGHT_COLLAR, RehabJoint.RightShoulder);
        AssertMapping(BodyTrackerRole.RIGHT_ELBOW, RehabJoint.RightElbow);
        AssertMapping(BodyTrackerRole.RIGHT_WRIST, RehabJoint.RightWrist);
    }

    private static void PicoJointMapper_MapsRequiredLowerBodyJoints()
    {
        AssertMapping(BodyTrackerRole.Pelvis, RehabJoint.Hips);
        AssertMapping(BodyTrackerRole.LEFT_HIP, RehabJoint.LeftHip);
        AssertMapping(BodyTrackerRole.LEFT_KNEE, RehabJoint.LeftKnee);
        AssertMapping(BodyTrackerRole.LEFT_ANKLE, RehabJoint.LeftAnkle);
        AssertMapping(BodyTrackerRole.LEFT_FOOT, RehabJoint.LeftFoot);
        AssertMapping(BodyTrackerRole.RIGHT_HIP, RehabJoint.RightHip);
        AssertMapping(BodyTrackerRole.RIGHT_KNEE, RehabJoint.RightKnee);
        AssertMapping(BodyTrackerRole.RIGHT_ANKLE, RehabJoint.RightAnkle);
        AssertMapping(BodyTrackerRole.RIGHT_FOOT, RehabJoint.RightFoot);
    }

    private static void PicoJointMapper_RejectsNoneRole()
    {
        RehabJoint joint;
        AssertTrue(!PicoBodyJointMapper.TryMap(BodyTrackerRole.NONE_ROLE, out joint), "NONE_ROLE must not map to a RehabJoint.");
        AssertTrue(joint == RehabJoint.Count, "Rejected roles should return RehabJoint.Count.");
    }

    private static void PicoProvider_ReusesSampleStorage()
    {
        var root = new GameObject("PicoProviderReuseTest");
        try
        {
            var fake = CreateValidFake();
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0f)));
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            var sample = new RehabBodySample();
            var jointStorage = sample.joints;
            var fakeJointStorage = fake.bodyData.joints;

            provider.TryGetSample(sample);
            provider.TryGetSample(sample);
            AssertTrue(ReferenceEquals(jointStorage, sample.joints), "Provider sampling must reuse RehabBodySample joint storage.");
            AssertTrue(ReferenceEquals(fakeJointStorage, fake.bodyData.joints), "Fake SDK frame should retain one joint array.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoProvider_DoesNotLeakSdkTypesIntoBodySample()
    {
        var sdkNamespace = typeof(BodyTrackerRole).Namespace;
        var fields = typeof(RehabBodySample).GetFields();
        for (var i = 0; i < fields.Length; i++)
        {
            AssertTrue(fields[i].FieldType.Namespace != sdkNamespace, "RehabBodySample fields must not expose Unity.XR.PXR types.");
        }

        var properties = typeof(RehabBodySample).GetProperties();
        for (var i = 0; i < properties.Length; i++)
        {
            var propertyType = properties[i].PropertyType;
            var elementType = propertyType.IsArray ? propertyType.GetElementType() : propertyType;
            AssertTrue(elementType == null || elementType.Namespace != sdkNamespace, "RehabBodySample properties must not expose Unity.XR.PXR types.");
        }
    }

    private static void PicoProvider_ConvertsLocalPoseToWorldSpace()
    {
        var root = new GameObject("PicoProviderCoordinateTest");
        var origin = new GameObject("XR Origin");
        try
        {
            origin.transform.position = new Vector3(1f, 0.5f, 2f);
            origin.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var fake = CreateValidFake();
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1f, 1f)));
            var provider = CreateProvider(root, fake);
            provider.XrOrigin = origin.transform;
            provider.OutputSpace = PicoBodyTrackingOutputSpace.World;
            provider.StartTracking();
            var sample = new RehabBodySample();
            provider.TryGetSample(sample);

            AssertJointPosition(
                sample,
                RehabJoint.Head,
                origin.transform.TransformPoint(new Vector3(0f, 1f, 1f)),
                "World output should transform localPose through XR Origin exactly once.");
        }
        finally
        {
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoApi_ConvertsNativeCoordinatesWithoutWritingPastMotionVectors()
    {
        var nativePose = new BodyTrackerTransPose
        {
            PosX = 1d,
            PosY = 2d,
            PosZ = 3d,
            RotQx = 0.1d,
            RotQy = 0.2d,
            RotQz = 0.3d,
            RotQw = 0.4d
        };

        AssertVectorApproximately(
            PicoBodyTrackingApi.ConvertPositionToUnity(nativePose),
            new Vector3(1f, 2f, -3f),
            "Native body position conversion should negate only Z.");
        AssertQuaternionApproximately(
            PicoBodyTrackingApi.ConvertRotationToUnity(nativePose),
            new Quaternion(0.1f, 0.2f, -0.3f, -0.4f),
            "Native body rotation conversion should match PICO's intended Unity handedness conversion.");
        AssertVectorApproximately(
            PicoBodyTrackingApi.ConvertMotionVectorToUnity(new Vector3(4f, 5f, 6f)),
            new Vector3(4f, 5f, -6f),
            "Motion-vector conversion should read and negate valid Z index 2 without writing index 3.");
    }

    private static void ProviderSelector_DoesNotFallbackFromLimitedPicoSample()
    {
        var root = new GameObject("PicoLimitedSelectorTest");
        try
        {
            var fake = CreateValidFake();
            fake.trackingState = CreateState(true, BodyTrackingStatusCode.BT_LIMITED, BodyTrackingMessage.BT_MESSAGE_UNKNOWN);
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0f)));
            var pico = CreateProvider(root, fake);
            pico.StartTracking();

            var tracker = root.AddComponent<HandPoseTracker>();
            tracker.autoResolveReferences = false;
            tracker.hmdTransform = CreateChild(root.transform, "Head", new Vector3(0f, 1.6f, 0f));
            tracker.leftControllerTransform = CreateChild(root.transform, "Left Controller", Vector3.left);
            tracker.rightControllerTransform = CreateChild(root.transform, "Right Controller", Vector3.right);
            var controller = root.AddComponent<ControllerPoseProvider>();
            controller.HandPoseTracker = tracker;
            controller.StartTracking();

            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = pico;
            selector.FallbackProvider = controller;
            selector.AllowAutomaticFallback = true;
            selector.StartTracking();
            var sample = new RehabBodySample();

            AssertTrue(selector.TryGetSample(sample), "Limited PICO samples should remain available for diagnostics.");
            AssertTrue(selector.CurrentProvider == pico, "Limited PICO state must not silently switch action input to controllers.");
            AssertTrue(sample.trackingState == RehabTrackingState.Limited && !sample.IsTrackingUsable, "Limited selector output should pause action timing.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PicoStatusPanel_CreatesHeadLockedWorldCanvas()
    {
        var root = new GameObject("PicoStatusPanelTest");
        var cameraObject = new GameObject("Status Camera");
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            var fake = CreateValidFake();
            fake.bodyData.SetJoint(CreateJoint(BodyTrackerRole.HEAD, new Vector3(0f, 1.7f, 0f)));
            var provider = CreateProvider(root, fake);
            provider.StartTracking();
            provider.TryGetSample(new RehabBodySample());

            var panel = root.AddComponent<PicoBodyTrackingStatusPanel>();
            panel.Provider = provider;
            panel.TargetCamera = camera;
            panel.StatusPanelEnabled = true;
            panel.RefreshNow();

            AssertTrue(panel.StatusCanvas != null, "Status panel should create a reusable Canvas.");
            AssertTrue(panel.StatusCanvas.renderMode == RenderMode.WorldSpace, "Status Canvas must use World Space mode.");
            AssertTrue(panel.StatusCanvas.transform.parent == camera.transform, "Status Canvas must be parented to the target camera.");
            AssertTrue(Vector3.Distance(panel.StatusCanvas.transform.localPosition, new Vector3(0f, -0.18f, 1.2f)) < 0.0001f, "Status Canvas should use the default head-local position.");
            AssertTrue(Quaternion.Angle(panel.StatusCanvas.transform.localRotation, Quaternion.identity) < 0.001f, "Status Canvas should face forward in camera-local space.");
            AssertTrue(Vector3.Distance(panel.StatusCanvas.transform.localScale, Vector3.one * 0.0015f) < 0.0001f, "Status Canvas should use the default scale.");

            var canvasRect = panel.StatusCanvas.transform as RectTransform;
            AssertTrue(canvasRect != null && Vector2.Distance(canvasRect.sizeDelta, new Vector2(900f, 420f)) < 0.001f, "Status Canvas should use the default 900 x 420 size.");
            AssertTrue(panel.StatusText != null && panel.StatusText is TextMeshProUGUI, "Status text must use TextMeshProUGUI.");
            AssertTrue(Mathf.Abs(panel.StatusText.fontSize - 72f) < 0.001f, "Status text should use the default 72 font size.");
            AssertTrue(!panel.StatusText.enableAutoSizing, "Status text auto sizing must remain disabled.");
            AssertTrue(panel.StatusText.alignment == TextAlignmentOptions.MidlineLeft, "Status text should be left aligned and vertically centered.");
            AssertTrue(panel.StatusText.outlineWidth > 0f, "Status text should have a black outline.");

            var background = panel.StatusCanvas.GetComponent<Image>();
            AssertTrue(background != null && Mathf.Abs(background.color.a - 0.8f) < 0.001f, "Status panel should have the default semi-transparent background.");
            AssertTrue(panel.StatusText.text.Contains("Tracking State: Valid"), "Status panel should display the tracking state.");
            AssertTrue(panel.StatusText.text.Contains("Valid Joint Count: 1"), "Status panel should display the latest valid joint count.");
            AssertTrue(panel.StatusText.text.Contains("Successful Sample Count: 1"), "Status panel should display sample diagnostics.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static void PicoStatusPanel_ReusesUiObjects()
    {
        var root = new GameObject("PicoStatusPanelReuseTest");
        var cameraObject = new GameObject("Status Camera");
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            var panel = root.AddComponent<PicoBodyTrackingStatusPanel>();
            panel.TargetCamera = camera;
            panel.StatusPanelEnabled = true;
            panel.RefreshNow();
            var canvas = panel.StatusCanvas;
            var text = panel.StatusText;

            panel.RefreshNow();
            AssertTrue(ReferenceEquals(canvas, panel.StatusCanvas), "Status panel must reuse its Canvas.");
            AssertTrue(ReferenceEquals(text, panel.StatusText), "Status panel must reuse its TextMeshProUGUI component.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static void PicoDebugRenderer_DoesNotOwnStatusUi()
    {
        var fields = typeof(PicoBodyTrackingDebugRenderer).GetFields(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        for (var i = 0; i < fields.Length; i++)
        {
            AssertTrue(!typeof(TMP_Text).IsAssignableFrom(fields[i].FieldType), "Debug renderer must not retain status text fields.");
            AssertTrue(!typeof(Canvas).IsAssignableFrom(fields[i].FieldType), "Debug renderer must not retain status Canvas fields.");
        }
    }

    private static FakePicoBodyTrackingApi CreateValidFake()
    {
        return new FakePicoBodyTrackingApi
        {
            supported = true,
            trackingState = CreateState(
                true,
                BodyTrackingStatusCode.BT_VALID,
                BodyTrackingMessage.BT_MESSAGE_UNKNOWN)
        };
    }

    private static PicoBodyTrackingProvider CreateProvider(GameObject root, FakePicoBodyTrackingApi fake)
    {
        var provider = root.AddComponent<PicoBodyTrackingProvider>();
        provider.AutoStartOnEnable = false;
        provider.SetApiForTesting(fake);
        provider.OutputSpace = PicoBodyTrackingOutputSpace.XrOriginLocal;
        return provider;
    }

    private static PicoBodyTrackingApiState CreateState(
        bool isTracking,
        BodyTrackingStatusCode statusCode,
        BodyTrackingMessage message)
    {
        return new PicoBodyTrackingApiState
        {
            isTracking = isTracking,
            statusCode = statusCode,
            message = message
        };
    }

    private static PicoBodyJointData CreateJoint(BodyTrackerRole role, Vector3 position)
    {
        return new PicoBodyJointData
        {
            valid = true,
            role = role,
            timestamp = 1234L,
            position = position,
            rotation = Quaternion.identity,
            velocity = new Vector3(0.1f, 0.2f, 0.3f),
            acceleration = new Vector3(0.4f, 0.5f, 0.6f),
            angularVelocity = new Vector3(0.7f, 0.8f, 0.9f)
        };
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.position = position;
        return child.transform;
    }

    private static void AssertMapping(BodyTrackerRole role, RehabJoint expected)
    {
        RehabJoint actual;
        AssertTrue(PicoBodyJointMapper.TryMap(role, out actual), role + " should map to a RehabJoint.");
        AssertTrue(actual == expected, role + " should map to " + expected + ".");
    }

    private static void AssertJointPosition(
        RehabBodySample sample,
        RehabJoint joint,
        Vector3 expected,
        string message)
    {
        RehabJointPose pose;
        AssertTrue(sample.TryGetJoint(joint, out pose), message + " Joint is missing.");
        AssertTrue(Vector3.Distance(pose.position, expected) < 0.0001f, message);
    }

    private static void AssertVectorApproximately(Vector3 actual, Vector3 expected, string message)
    {
        AssertTrue(Vector3.Distance(actual, expected) < 0.0001f, message);
    }

    private static void AssertQuaternionApproximately(Quaternion actual, Quaternion expected, string message)
    {
        AssertTrue(
            Mathf.Abs(actual.x - expected.x) < 0.0001f &&
            Mathf.Abs(actual.y - expected.y) < 0.0001f &&
            Mathf.Abs(actual.z - expected.z) < 0.0001f &&
            Mathf.Abs(actual.w - expected.w) < 0.0001f,
            message);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
#endif
