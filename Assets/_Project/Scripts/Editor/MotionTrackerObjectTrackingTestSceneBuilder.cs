using System.IO;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking;
using PicoElderCare.Rehab;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class MotionTrackerObjectTrackingTestSceneBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/Debug/MotionTracker_ObjectTracking_Test.unity";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string ChineseFontPath = "Assets/_Project/Materials/Rehab/RehabChineseTMP.asset";

    [MenuItem("Tools/PICO ElderCare/Build Motion Tracker Object Tracking Test Scene")]
    public static void BuildScene()
    {
        var directory = Path.GetDirectoryName(ScenePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var origin = CreateXrOrigin(scene);
        var camera = EnsureMainCamera(origin, scene);
        CreateWorldCanvas(scene, camera != null ? camera.transform : null);
        EnsureLighting(scene);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[MotionTrackerObjectTrackingTestSceneBuilder] Saved: " + ScenePath);
    }

    /// <summary>Batch entry used by automated validation.</summary>
    public static void BuildSceneBatch()
    {
        BuildScene();
    }

    [MenuItem("Tools/PICO ElderCare/Validate Motion Tracker Object Tracking Test Scene")]
    public static void ValidateScene()
    {
        if (!File.Exists(ScenePath)) throw new FileNotFoundException("Debug scene has not been generated.", ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvases = Object.FindObjectsOfType<MotionTrackerObjectTrackingDebugPanel>(true);
        var placers = Object.FindObjectsOfType<ComfortWorldSpaceUIPlacer>(true);
        var origins = Object.FindObjectsOfType<XROrigin>(true);
        if (canvases.Length != 1 || placers.Length != 1 || origins.Length != 1)
        {
            throw new System.InvalidOperationException(
                "Debug scene must contain exactly one debug panel, startup placer, and XR Origin.");
        }

        var roots = scene.GetRootGameObjects();
        for (var r = 0; r < roots.Length; r++)
        {
            var behaviours = roots[r].GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                    throw new MissingReferenceException("Debug scene contains a Missing Script under " + roots[r].name + ".");
            }
        }

        Debug.Log("[MotionTrackerObjectTrackingTestSceneBuilder] Validation passed: " + ScenePath);
    }

    public static void ValidateSceneBatch()
    {
        ValidateScene();
    }

    private static GameObject CreateXrOrigin(Scene scene)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
        GameObject root;
        if (prefab != null)
        {
            root = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        }
        else
        {
            root = new GameObject("[Debug] XR Origin (XR Rig)");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<XROrigin>();
        }

        root.name = "[Debug] PICO Controller Tracking XR Origin (XR Rig)";
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return root;
    }

    private static Camera EnsureMainCamera(GameObject origin, Scene scene)
    {
        var camera = origin != null ? origin.GetComponentInChildren<Camera>(true) : null;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            if (origin != null) cameraObject.transform.SetParent(origin.transform, false); else SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camera = cameraObject.GetComponent<Camera>();
        }

        camera.tag = "MainCamera";
        var xrOrigin = origin != null ? origin.GetComponent<XROrigin>() : null;
        if (xrOrigin != null) xrOrigin.Camera = camera;
        return camera;
    }

    private static void CreateWorldCanvas(Scene scene, Transform camera)
    {
        var canvasObject = new GameObject(
            "ObjectTrackingDebugCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(MotionTrackerObjectTrackingDebugPanel));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera != null ? camera.GetComponent<Camera>() : null;
        canvas.sortingOrder = 50;
        var rect = canvasObject.GetComponent<RectTransform>();
        var debugPanel = canvasObject.GetComponent<MotionTrackerObjectTrackingDebugPanel>();
        debugPanel.uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        rect.sizeDelta = new Vector2(1000f, 640f);
        rect.localScale = Vector3.one * 0.00165f;

        var forward = camera != null ? Vector3.ProjectOnPlane(camera.forward, Vector3.up) : Vector3.forward;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();
        rect.position = (camera != null ? camera.position : new Vector3(0f, 1.65f, 0f)) + forward * 2.0f;
        rect.position = new Vector3(rect.position.x, camera != null ? camera.position.y - 0.05f : 1.6f, rect.position.z);
        rect.rotation = Quaternion.LookRotation(forward, Vector3.up);

        var placer = canvasObject.AddComponent<ComfortWorldSpaceUIPlacer>();
        placer.headTransform = camera;
        placer.uiRoot = rect;
        placer.placeOnStart = true;
        placer.recenterDuringStartup = true;
        placer.startupRecenterSeconds = 1.25f;
        placer.startupRecenterFrames = 18;
        placer.distanceMeters = 2f;
        placer.usePreferredHeightInsteadOfHeadHeight = false;
        placer.hmdHeightOffsetMeters = -0.05f;
        placer.clampWorldHeight = true;
        placer.minWorldHeight = 1.25f;
        placer.maxWorldHeight = 1.75f;
        placer.comfortFollowEnabled = false;
        placer.enableRayDrag = false;
        placer.enableThumbstickNavigation = false;
    }

    private static void EnsureLighting(Scene scene)
    {
        var lightObject = new GameObject("Debug Directional Light", typeof(Light));
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
    }
}
