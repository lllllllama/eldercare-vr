using PicoElderCare.Rehab;
using PicoElderCare.Rehab.Tracking;
using PicoElderCare.Rehab.Tracking.Pico;
using Unity.XR.CoreUtils;
using Unity.XR.PXR;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RehabSceneBuilderSelfTests
{
    public static void RunAll()
    {
        SynchronizeRehabScene_PreservesCurrentContentAndReusesTrackingObjects();
        Debug.Log("Rehab scene builder self tests passed.");
    }

    private static void SynchronizeRehabScene_PreservesCurrentContentAndReusesTrackingObjects()
    {
        var scene = EditorSceneManager.NewPreviewScene();

        try
        {
            var sentinel = new GameObject("CurrentAuthoredVideoPanel");
            SceneManager.MoveGameObjectToScene(sentinel, scene);
            sentinel.transform.position = new Vector3(1.25f, 1.5f, 2.75f);
            var sentinelPosition = sentinel.transform.position;

            var xrOriginObject = new GameObject("Current XR Origin");
            SceneManager.MoveGameObjectToScene(xrOriginObject, scene);
            var xrOrigin = xrOriginObject.AddComponent<XROrigin>();
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginObject.transform, false);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            xrOrigin.Camera = camera;
            xrOrigin.CameraFloorOffsetObject = cameraOffset;

            var leftController = new GameObject("Left Controller");
            leftController.transform.SetParent(xrOriginObject.transform, false);
            var rightController = new GameObject("Right Controller");
            rightController.transform.SetParent(xrOriginObject.transform, false);

            var pxrManager = xrOriginObject.AddComponent<PXR_Manager>();
            pxrManager.bodyTracking = false;
            pxrManager.useRecommendedAntiAliasingLevel = false;
            xrOriginObject.AddComponent<PXR_Manager>();

            var rehabRoot = new GameObject("Rehab");
            SceneManager.MoveGameObjectToScene(rehabRoot, scene);
            var managers = new GameObject("RehabManagers");
            managers.transform.SetParent(rehabRoot.transform, false);
            var poseTracker = managers.AddComponent<HandPoseTracker>();
            var session = managers.AddComponent<RehabSessionManager>();

            var bodyTrackingSystem = new GameObject("BodyTrackingSystem");
            bodyTrackingSystem.SetActive(false);
            bodyTrackingSystem.transform.SetParent(xrOriginObject.transform, false);
            var picoProvider = bodyTrackingSystem.AddComponent<PicoBodyTrackingProvider>();
            picoProvider.AutoStartOnEnable = false;
            bodyTrackingSystem.SetActive(true);
            picoProvider.AutoStartOnEnable = true;

            var debugObject = new GameObject("BodyTrackingDebug");
            debugObject.transform.SetParent(bodyTrackingSystem.transform, false);
            debugObject.transform.localPosition = Vector3.forward;
            debugObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var debugRenderer = debugObject.AddComponent<PicoBodyTrackingDebugRenderer>();
            debugRenderer.Provider = picoProvider;
            debugRenderer.DebugSkeletonEnabled = true;

            AssertTrue(RehabSceneBuilder.SynchronizeRehabScene(scene), "Current rehab scene content should synchronize successfully.");
            AssertTrue(sentinel != null && sentinel.scene == scene, "Synchronization must preserve authored scene objects.");
            AssertTrue(Vector3.Distance(sentinel.transform.position, sentinelPosition) < 0.0001f, "Synchronization must preserve authored transforms.");
            AssertTrue(ReferenceEquals(FindSceneComponent<PicoBodyTrackingProvider>(scene), picoProvider), "Existing PICO provider should be reused.");
            AssertTrue(ReferenceEquals(FindSceneComponent<PicoBodyTrackingDebugRenderer>(scene), debugRenderer), "Existing debug renderer should be reused.");
            AssertTrue(debugRenderer.DebugSkeletonEnabled, "Existing debug skeleton visibility should be preserved.");
            AssertTrue(ReferenceEquals(debugRenderer.Provider, picoProvider), "Debug renderer should remain bound to the PICO provider.");
            AssertTrue(picoProvider.XrOrigin == xrOrigin.transform, "PICO provider should use the current XR Origin.");
            AssertTrue(picoProvider.OutputSpace == PicoBodyTrackingOutputSpace.XrOriginLocal, "PICO provider should output XR Origin local coordinates.");
            AssertTrue(pxrManager.bodyTracking, "Scene PXR Manager should explicitly enable Body Tracking.");
            AssertTrue(!pxrManager.useRecommendedAntiAliasingLevel, "Synchronization should preserve unrelated PXR Manager settings.");
            AssertTrue(poseTracker.hmdTransform == camera.transform, "Controller fallback should use the current Main Camera.");
            AssertTrue(poseTracker.leftControllerTransform == leftController.transform, "Controller fallback should reuse the current left controller.");
            AssertTrue(poseTracker.rightControllerTransform == rightController.transform, "Controller fallback should reuse the current right controller.");

            var controllerProvider = FindSceneComponent<ControllerPoseProvider>(scene);
            var selector = FindSceneComponent<RehabPoseProviderSelector>(scene);
            var statusPanel = FindSceneComponent<PicoBodyTrackingStatusPanel>(scene);
            AssertTrue(controllerProvider != null && controllerProvider.HandPoseTracker == poseTracker, "Controller provider should be created and bound.");
            AssertTrue(selector != null && selector.PrimaryProvider == picoProvider, "Selector should use PICO tracking as primary input.");
            AssertTrue(selector.FallbackProvider == controllerProvider && selector.AllowAutomaticFallback, "Selector should retain controller fallback.");
            AssertTrue(statusPanel != null && statusPanel.Provider == picoProvider, "Head-locked status panel should be created and bound.");
            AssertTrue(statusPanel.TargetCamera == camera && statusPanel.StatusPanelEnabled, "Status panel should target the current Main Camera.");
            AssertTrue(statusPanel.StatusFontAsset != null && statusPanel.StatusFontAsset.name == "RehabChineseTMP", "Status panel should use the project Chinese TMP font.");
            AssertTrue(Mathf.Abs(statusPanel.StatusFontSize - 44f) < 0.001f, "Status panel should use the verified compact font size.");
            AssertTrue(Vector2.Distance(statusPanel.StatusPanelSize, new Vector2(1200f, 720f)) < 0.001f, "Status panel should fit every diagnostics row without overlap.");
            AssertTrue(Vector3.Distance(statusPanel.StatusPanelScale, Vector3.one * 0.001f) < 0.0001f, "Status panel should preserve a comfortable world-space size.");
            AssertTrue(session.handPoseTracker == poseTracker && session.poseProviderSelector == selector, "Session should use the synchronized providers.");

            AssertTrue(RehabSceneBuilder.SynchronizeRehabScene(scene), "Repeated synchronization should remain successful.");
            AssertTrue(CountSceneComponents<PicoBodyTrackingProvider>(scene) == 1, "Repeated synchronization must not duplicate the PICO provider.");
            AssertTrue(CountSceneComponents<PicoBodyTrackingDebugRenderer>(scene) == 1, "Repeated synchronization must not duplicate the debug renderer.");
            AssertTrue(CountSceneComponents<PicoBodyTrackingStatusPanel>(scene) == 1, "Repeated synchronization must not duplicate the status panel.");
            AssertTrue(CountSceneComponents<ControllerPoseProvider>(scene) == 1, "Repeated synchronization must not duplicate the controller provider.");
            AssertTrue(CountSceneComponents<RehabPoseProviderSelector>(scene) == 1, "Repeated synchronization must not duplicate the provider selector.");
            AssertTrue(CountSceneComponents<PXR_Manager>(scene) == 1, "Repeated synchronization must keep exactly one PXR Manager.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static int CountSceneComponents<T>(Scene scene) where T : Component
    {
        var count = 0;
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            count += roots[i].GetComponentsInChildren<T>(true).Length;
        }

        return count;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
