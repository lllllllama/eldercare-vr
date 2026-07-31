using PicoElderCare.Rehab;
using PicoElderCare.Rehab.Tracking;
using UnityEngine;

public static class RehabTrackingSelfTests
{
    public static void RunAll()
    {
        RehabBodySample_CanStoreAndReadJoint();
        RehabBodySample_ReturnsFalseForMissingJoint();
        RehabBodySample_ClearInvalidatesAllJoints();
        ControllerProvider_MapsHead();
        ControllerProvider_MapsLeftControllerToLeftWrist();
        ControllerProvider_MapsRightControllerToRightWrist();
        ControllerProvider_DoesNotInventBodyJoints();
        ProviderSelector_UsesPrimaryWhenValid();
        ProviderSelector_FallsBackWhenPrimaryUnavailable();
        ProviderSelector_DoesNotFallbackWhenDisabled();
        BodySampleToLegacyAdapter_ProducesValidLegacySample();
        BodySampleToLegacyAdapter_RejectsMissingRequiredPoints();
        Debug.Log("Rehab tracking self tests passed.");
    }

    private static void RehabBodySample_CanStoreAndReadJoint()
    {
        var sample = new RehabBodySample();
        var expected = CreatePose(new Vector3(1f, 2f, 3f));
        sample.trackingState = RehabTrackingState.Valid;
        sample.SetJoint(RehabJoint.Head, expected);

        RehabJointPose actual;
        AssertTrue(sample.TryGetJoint(RehabJoint.Head, out actual), "Stored head joint should be readable.");
        AssertTrue(Vector3.Distance(expected.position, actual.position) < 0.0001f, "Stored joint position should be preserved.");
        AssertTrue(sample.validJointCount == 1, "A stored valid joint should increment the valid joint count.");
        AssertTrue(sample.IsTrackingUsable, "A valid tracking state with at least one joint should be usable.");
        AssertTrue(
            sample.HasRequiredJoints(new[] { RehabJoint.Head }),
            "Required-joint queries should accept a valid stored joint.");
    }

    private static void RehabBodySample_ReturnsFalseForMissingJoint()
    {
        var sample = new RehabBodySample();
        RehabJointPose pose;
        AssertTrue(!sample.TryGetJoint(RehabJoint.LeftElbow, out pose), "An unset joint should not be reported as valid.");
        AssertTrue(
            !sample.HasRequiredJoints(new[] { RehabJoint.Head, RehabJoint.LeftElbow }),
            "Required-joint queries should reject missing joints.");
    }

    private static void RehabBodySample_ClearInvalidatesAllJoints()
    {
        var sample = new RehabBodySample();
        var reusedJointArray = sample.joints;
        sample.trackingState = RehabTrackingState.Valid;
        sample.timestamp = 123d;
        sample.isSeatedMode = true;
        sample.SetJoint(RehabJoint.Head, CreatePose(Vector3.one));
        sample.SetJoint(RehabJoint.LeftWrist, CreatePose(Vector3.left));

        sample.Clear();

        AssertTrue(ReferenceEquals(reusedJointArray, sample.joints), "Clear should reuse the existing joint array.");
        AssertTrue(sample.validJointCount == 0, "Clear should reset the valid joint count.");
        AssertTrue(sample.trackingState == RehabTrackingState.Unavailable, "Clear should reset tracking state.");
        AssertTrue(!sample.isSeatedMode && sample.timestamp == 0d, "Clear should reset sample metadata.");
        for (var i = 0; i < (int)RehabJoint.Count; i++)
        {
            AssertTrue(!sample.joints[i].valid, "Clear should invalidate every body joint.");
        }
    }

    private static void ControllerProvider_MapsHead()
    {
        var root = new GameObject("ControllerProviderMapsHeadTest");
        try
        {
            var provider = CreateControllerProvider(
                root.transform,
                new Vector3(0.25f, 1.65f, 0.5f),
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f),
                true,
                true,
                true);
            var sample = new RehabBodySample();

            AssertTrue(provider.TryGetSample(sample), "A complete controller sample should be available.");
            RehabJointPose head;
            AssertTrue(sample.TryGetJoint(RehabJoint.Head, out head), "The headset should map to Head.");
            AssertTrue(Vector3.Distance(head.position, new Vector3(0.25f, 1.65f, 0.5f)) < 0.0001f, "Head position should match the HMD transform.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ControllerProvider_MapsLeftControllerToLeftWrist()
    {
        var root = new GameObject("ControllerProviderMapsLeftTest");
        try
        {
            var expected = new Vector3(-0.35f, 1.25f, 0.6f);
            var provider = CreateCompleteControllerProvider(root.transform, expected, new Vector3(0.2f, 1.2f, 0.4f));
            var sample = new RehabBodySample();
            provider.TryGetSample(sample);

            RehabJointPose wrist;
            RehabJointPose hand;
            AssertTrue(sample.TryGetJoint(RehabJoint.LeftWrist, out wrist), "The left controller should map to LeftWrist.");
            AssertTrue(sample.TryGetJoint(RehabJoint.LeftHand, out hand), "The left controller should also map to LeftHand.");
            AssertTrue(Vector3.Distance(wrist.position, expected) < 0.0001f, "LeftWrist should use the left controller position.");
            AssertTrue(Vector3.Distance(hand.position, expected) < 0.0001f, "LeftHand should use the left controller position.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ControllerProvider_MapsRightControllerToRightWrist()
    {
        var root = new GameObject("ControllerProviderMapsRightTest");
        try
        {
            var expected = new Vector3(0.35f, 1.25f, 0.6f);
            var provider = CreateCompleteControllerProvider(root.transform, new Vector3(-0.2f, 1.2f, 0.4f), expected);
            var sample = new RehabBodySample();
            provider.TryGetSample(sample);

            RehabJointPose wrist;
            RehabJointPose hand;
            AssertTrue(sample.TryGetJoint(RehabJoint.RightWrist, out wrist), "The right controller should map to RightWrist.");
            AssertTrue(sample.TryGetJoint(RehabJoint.RightHand, out hand), "The right controller should also map to RightHand.");
            AssertTrue(Vector3.Distance(wrist.position, expected) < 0.0001f, "RightWrist should use the right controller position.");
            AssertTrue(Vector3.Distance(hand.position, expected) < 0.0001f, "RightHand should use the right controller position.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ControllerProvider_DoesNotInventBodyJoints()
    {
        var root = new GameObject("ControllerProviderMissingBodyTest");
        try
        {
            var provider = CreateCompleteControllerProvider(
                root.transform,
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f));
            var sample = new RehabBodySample();
            provider.TryGetSample(sample);

            RehabJointPose pose;
            AssertTrue(sample.validJointCount == 5, "Controller input should expose only Head, both wrists, and both hands.");
            AssertTrue(!sample.TryGetJoint(RehabJoint.Chest, out pose), "Controller input must not invent a chest joint.");
            AssertTrue(!sample.TryGetJoint(RehabJoint.LeftElbow, out pose), "Controller input must not invent elbow joints.");
            AssertTrue(!sample.TryGetJoint(RehabJoint.Hips, out pose), "Controller input must not invent hip joints.");
            AssertTrue(!sample.TryGetJoint(RehabJoint.RightAnkle, out pose), "Controller input must not invent lower-body joints.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ProviderSelector_UsesPrimaryWhenValid()
    {
        var root = new GameObject("ProviderSelectorPrimaryTest");
        try
        {
            var primary = CreateControllerProvider(
                root.transform,
                new Vector3(1f, 1.6f, 0f),
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f),
                true,
                true,
                true);
            var fallback = CreateControllerProvider(
                root.transform,
                new Vector3(2f, 1.6f, 0f),
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f),
                true,
                true,
                true);
            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = primary;
            selector.FallbackProvider = fallback;
            selector.AllowAutomaticFallback = true;
            selector.StartTracking();

            var sample = new RehabBodySample();
            AssertTrue(selector.TryGetSample(sample), "Selector should return a valid primary sample.");
            AssertTrue(selector.CurrentProvider == primary, "Selector should use the primary provider while it is valid.");
            RehabJointPose head;
            sample.TryGetJoint(RehabJoint.Head, out head);
            AssertTrue(Mathf.Abs(head.position.x - 1f) < 0.0001f, "The selected sample should come from the primary provider.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ProviderSelector_FallsBackWhenPrimaryUnavailable()
    {
        var root = new GameObject("ProviderSelectorFallbackTest");
        try
        {
            var primary = CreateControllerProvider(
                root.transform,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                false,
                false,
                false);
            var fallback = CreateControllerProvider(
                root.transform,
                new Vector3(2f, 1.6f, 0f),
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f),
                true,
                true,
                true);
            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = primary;
            selector.FallbackProvider = fallback;
            selector.AllowAutomaticFallback = true;
            selector.StartTracking();

            var sample = new RehabBodySample();
            AssertTrue(selector.TryGetSample(sample), "Selector should return the fallback sample when primary is unavailable.");
            AssertTrue(selector.CurrentProvider == fallback, "Selector should expose the fallback as the active provider.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ProviderSelector_DoesNotFallbackWhenDisabled()
    {
        var root = new GameObject("ProviderSelectorNoFallbackTest");
        try
        {
            var primary = CreateControllerProvider(
                root.transform,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                false,
                false,
                false);
            var fallback = CreateCompleteControllerProvider(
                root.transform,
                new Vector3(-0.2f, 1.2f, 0.4f),
                new Vector3(0.2f, 1.2f, 0.4f));
            var selector = root.AddComponent<RehabPoseProviderSelector>();
            selector.PrimaryProvider = primary;
            selector.FallbackProvider = fallback;
            selector.AllowAutomaticFallback = false;
            selector.StartTracking();

            var sample = new RehabBodySample();
            AssertTrue(!selector.TryGetSample(sample), "Selector should not use fallback when automatic fallback is disabled.");
            AssertTrue(selector.CurrentProvider == null, "No provider should be active when primary fails and fallback is disabled.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BodySampleToLegacyAdapter_ProducesValidLegacySample()
    {
        var sample = CreateLegacyCompatibleBodySample();
        RehabPoseSample legacySample;
        AssertTrue(
            BodySampleToLegacyAdapter.TryConvert(sample, out legacySample),
            "Adapter should convert a body sample containing head and both wrists.");
        AssertTrue(legacySample.IsValid, "Converted legacy sample should be valid.");
        AssertTrue(Vector3.Distance(legacySample.headPosition, new Vector3(0f, 1.6f, 0f)) < 0.0001f, "Adapter should preserve head position.");
        AssertTrue(Vector3.Distance(legacySample.leftHandPosition, new Vector3(-0.2f, 1.2f, 0.4f)) < 0.0001f, "Adapter should map LeftWrist to the legacy left hand.");
        AssertTrue(Vector3.Distance(legacySample.rightHandPosition, new Vector3(0.2f, 1.2f, 0.4f)) < 0.0001f, "Adapter should map RightWrist to the legacy right hand.");
    }

    private static void BodySampleToLegacyAdapter_RejectsMissingRequiredPoints()
    {
        var sample = CreateLegacyCompatibleBodySample();
        sample.SetJoint(RehabJoint.RightWrist, default(RehabJointPose));

        RehabPoseSample legacySample;
        AssertTrue(
            !BodySampleToLegacyAdapter.TryConvert(sample, out legacySample),
            "Adapter should reject samples missing head or either wrist.");
        AssertTrue(!legacySample.IsValid, "A rejected conversion should return an invalid legacy sample.");
    }

    private static ControllerPoseProvider CreateCompleteControllerProvider(
        Transform parent,
        Vector3 leftPosition,
        Vector3 rightPosition)
    {
        return CreateControllerProvider(
            parent,
            new Vector3(0f, 1.6f, 0f),
            leftPosition,
            rightPosition,
            true,
            true,
            true);
    }

    private static ControllerPoseProvider CreateControllerProvider(
        Transform parent,
        Vector3 headPosition,
        Vector3 leftPosition,
        Vector3 rightPosition,
        bool includeHead,
        bool includeLeft,
        bool includeRight)
    {
        var providerObject = new GameObject("ControllerPoseProviderTestObject");
        providerObject.transform.SetParent(parent, false);
        var tracker = providerObject.AddComponent<HandPoseTracker>();
        tracker.autoResolveReferences = false;
        tracker.hmdTransform = includeHead ? CreateChildTransform(providerObject.transform, "Head", headPosition) : null;
        tracker.leftControllerTransform = includeLeft ? CreateChildTransform(providerObject.transform, "Left Controller", leftPosition) : null;
        tracker.rightControllerTransform = includeRight ? CreateChildTransform(providerObject.transform, "Right Controller", rightPosition) : null;

        var provider = providerObject.AddComponent<ControllerPoseProvider>();
        provider.HandPoseTracker = tracker;
        provider.StartTracking();
        return provider;
    }

    private static Transform CreateChildTransform(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.position = position;
        child.transform.rotation = Quaternion.identity;
        return child.transform;
    }

    private static RehabBodySample CreateLegacyCompatibleBodySample()
    {
        var sample = new RehabBodySample();
        sample.trackingState = RehabTrackingState.Valid;
        sample.SetJoint(RehabJoint.Head, CreatePose(new Vector3(0f, 1.6f, 0f)));
        sample.SetJoint(RehabJoint.LeftWrist, CreatePose(new Vector3(-0.2f, 1.2f, 0.4f)));
        sample.SetJoint(RehabJoint.RightWrist, CreatePose(new Vector3(0.2f, 1.2f, 0.4f)));
        return sample;
    }

    private static RehabJointPose CreatePose(Vector3 position)
    {
        return new RehabJointPose
        {
            valid = true,
            confidence = 1f,
            position = position,
            rotation = Quaternion.identity,
            velocity = Vector3.zero,
            acceleration = Vector3.zero,
            angularVelocity = Vector3.zero
        };
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
