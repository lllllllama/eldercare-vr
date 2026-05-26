using PicoElderCare.Rehab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class RehabVideoGuideSceneRepair
{
    private const string RehabScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string RenderTexturePath = "Assets/_Project/RenderTextures/RehabVideoRT.renderTexture";
    private const string VideoMaterialPath = "Assets/_Project/Materials/RehabVideoMaterial.mat";

    private static readonly string[] BaduanjinMovementNames =
    {
        "双手托天理三焦",
        "左右开弓似射雕",
        "调理脾胃须单举",
        "五劳七伤往后瞧",
        "摇头摆尾去心火",
        "两手攀足固肾腰",
        "攒拳怒目增气力",
        "背后七颠百病消"
    };

    private static readonly string[] BaduanjinVideoPaths =
    {
        "Assets/_Project/Videos/Rehab/Baduanjin/01_shuangshoutuotian.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/02_zuoyoukaigong.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/03_tiaolipiwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/04_wulaoqishang.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/05_yaotoubaiwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/06_liangshoupanzu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/07_cuanquannumu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/08_beihouqidian.mp4"
    };

    [MenuItem("Tools/PICO ElderCare/Repair Rehab Baduanjin Video Guide")]
    public static void RepairRehabBaduanjinVideoGuide()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before repairing the Rehab Baduanjin video guide.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(RehabScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"Could not open rehab scene: {RehabScenePath}");
            return;
        }

        EnsureFolder("Assets/_Project/RenderTextures");
        EnsureFolder("Assets/_Project/Materials");

        var renderTexture = LoadOrCreateRenderTexture();
        var videoMaterial = LoadOrCreateVideoMaterial(renderTexture);
        var panel = EnsurePanelHierarchy(out var visualsRoot);
        var videoPlayer = EnsureComponent<VideoPlayer>(panel);
        var audioSource = EnsureComponent<AudioSource>(panel);
        var layoutController = EnsureComponent<RehabVideoPanelLayoutController>(panel);
        var guideController = EnsureComponent<RehabVideoGuideController>(panel);
        var videoQuad = EnsureVideoQuad(panel.transform, videoMaterial);
        var videoCanvas = EnsureVideoCanvas(panel.transform);
        var rawImage = EnsureVideoRawImage(videoCanvas.transform, renderTexture);
        var quadRenderer = videoQuad.GetComponent<Renderer>();

        ConfigurePanelTransform(panel.transform);
        ConfigureVideoPlayer(videoPlayer, audioSource, renderTexture);
        ConfigureAudioSource(audioSource);
        ConfigureLayout(layoutController, panel.transform, videoQuad.transform);
        ConfigureGuideController(
            guideController,
            panel,
            videoCanvas,
            rawImage,
            videoPlayer,
            audioSource,
            videoQuad,
            quadRenderer,
            videoMaterial,
            renderTexture,
            layoutController);

        BindGuideToSceneControllers(guideController);

        videoQuad.SetActive(false);
        videoCanvas.SetActive(false);
        panel.SetActive(true);

        EditorUtility.SetDirty(visualsRoot);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(videoPlayer);
        EditorUtility.SetDirty(audioSource);
        EditorUtility.SetDirty(layoutController);
        EditorUtility.SetDirty(guideController);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("Rehab Baduanjin video guide repaired. RehabVideoPanel, VideoQuad, RehabVideoCanvas, RenderTexture, material, and controller references are ready.");
    }

    [MenuItem("Tools/PICO ElderCare/Rehab/Check Baduanjin Video Guide Only")]
    public static void CheckBaduanjinVideoGuideOnly()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != RehabScenePath)
        {
            Debug.LogWarning($"Active scene is '{activeScene.path}'. Open {RehabScenePath} to check the rehab video guide in place.");
        }

        var panel = FindSceneObject("RehabVideoPanel");
        var videoPlayer = panel != null ? panel.GetComponent<VideoPlayer>() : null;
        var guide = panel != null ? panel.GetComponent<RehabVideoGuideController>() : null;
        var bindingCount = guide != null && guide.bindings != null ? guide.bindings.Length : 0;

        var message =
            "Baduanjin video guide check:\n" +
            $"- RehabVideoPanel exists: {panel != null}\n" +
            $"- VideoPlayer exists: {videoPlayer != null}\n" +
            $"- RehabVideoGuideController exists: {guide != null}\n" +
            $"- bindings count: {bindingCount}";

        if (guide != null && guide.bindings != null)
        {
            for (var i = 0; i < guide.bindings.Length; i++)
            {
                var binding = guide.bindings[i];
                var movementName = binding != null ? binding.movementName : "<null binding>";
                var clipState = binding != null && binding.videoClip != null ? binding.videoClip.name : "EMPTY";
                message += $"\n  [{i}] {movementName}: {clipState}";
            }
        }

        Debug.Log(message);
    }

    private static GameObject EnsurePanelHierarchy(out GameObject visualsRoot)
    {
        var rehab = FindSceneObject("Rehab") ?? new GameObject("Rehab");
        visualsRoot = FindChild(rehab.transform, "RehabVisuals");
        if (visualsRoot == null)
        {
            visualsRoot = new GameObject("RehabVisuals");
            visualsRoot.transform.SetParent(rehab.transform, false);
        }

        var panel = FindSceneObject("RehabVideoPanel") ?? new GameObject("RehabVideoPanel");
        if (panel.transform.parent != visualsRoot.transform)
        {
            panel.transform.SetParent(visualsRoot.transform, false);
        }

        return panel;
    }

    private static GameObject EnsureVideoQuad(Transform panel, Material material)
    {
        var quad = FindChild(panel, "VideoQuad");
        if (quad == null)
        {
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "VideoQuad";
            quad.transform.SetParent(panel, false);
        }

        var collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(0.78f, 0.44f, 1f);

        var renderer = quad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return quad;
    }

    private static GameObject EnsureVideoCanvas(Transform panel)
    {
        var canvasObject = FindChild(panel, "RehabVideoCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("RehabVideoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(panel, false);
        }

        var rect = EnsureComponent<RectTransform>(canvasObject);
        rect.sizeDelta = new Vector2(960f, 540f);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.0015f;

        var canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        var scaler = EnsureComponent<CanvasScaler>(canvasObject);
        scaler.dynamicPixelsPerUnit = 10f;

        return canvasObject;
    }

    private static RawImage EnsureVideoRawImage(Transform canvas, RenderTexture renderTexture)
    {
        var rawObject = FindChild(canvas, "VideoRawImage");
        if (rawObject == null)
        {
            rawObject = new GameObject("VideoRawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            rawObject.transform.SetParent(canvas, false);
        }

        var rect = EnsureComponent<RectTransform>(rawObject);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        EnsureComponent<CanvasRenderer>(rawObject);
        var rawImage = EnsureComponent<RawImage>(rawObject);
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        return rawImage;
    }

    private static void ConfigurePanelTransform(Transform panel)
    {
        panel.localPosition = new Vector3(1.15f, 1.35f, 1.8f);
        panel.localRotation = Quaternion.Euler(0f, -25f, 0f);
        panel.localScale = Vector3.one;
    }

    private static void ConfigureVideoPlayer(VideoPlayer videoPlayer, AudioSource audioSource, RenderTexture renderTexture)
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.enabled = true;
    }

    private static void ConfigureAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.mute = false;
        audioSource.volume = 0.7f;
        audioSource.spatialBlend = 0f;
        audioSource.enabled = true;
    }

    private static void ConfigureLayout(RehabVideoPanelLayoutController layout, Transform panel, Transform quad)
    {
        layout.panelRoot = panel;
        layout.videoQuad = quad;
        layout.panelDistance = 2.15f;
        layout.videoRightOffset = 0.9f;
        layout.heightOffset = 0.08f;
        layout.videoWidth = 0.78f;
        layout.videoHeight = 0.44f;
        layout.videoScale = 1f;
        layout.faceUser = true;
        layout.ApplyVideoSize();
    }

    private static void ConfigureGuideController(
        RehabVideoGuideController guide,
        GameObject panel,
        GameObject displayRoot,
        RawImage rawImage,
        VideoPlayer videoPlayer,
        AudioSource audioSource,
        GameObject videoQuad,
        Renderer quadRenderer,
        Material material,
        RenderTexture renderTexture,
        RehabVideoPanelLayoutController layout)
    {
        guide.videoPanel = panel;
        guide.displayRoot = displayRoot;
        guide.rawImage = rawImage;
        guide.videoPlayer = videoPlayer;
        guide.audioSource = audioSource;
        guide.videoQuad = videoQuad;
        guide.videoQuadRenderer = quadRenderer;
        guide.videoMaterial = material;
        guide.renderTexture = renderTexture;
        guide.layoutController = layout;
        guide.displayMode = RehabVideoDisplayMode.QuadMaterial;
        guide.requireActiveSession = true;
        guide.muteAudio = false;
        guide.volume = 0.7f;
        guide.loopVideo = false;
        guide.showDebugFrame = false;
        guide.bindings = CreateBaduanjinBindings(guide.bindings);
    }

    private static RehabMovementVideoBinding[] CreateBaduanjinBindings(RehabMovementVideoBinding[] existingBindings)
    {
        var bindings = new RehabMovementVideoBinding[BaduanjinMovementNames.Length];
        for (var i = 0; i < BaduanjinMovementNames.Length; i++)
        {
            var existing = FindExistingBinding(existingBindings, BaduanjinMovementNames[i], i);
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(BaduanjinVideoPaths[i]);
            if (clip == null)
            {
                Debug.LogWarning($"Missing Baduanjin video clip: {BaduanjinVideoPaths[i]}");
            }

            bindings[i] = new RehabMovementVideoBinding
            {
                movementId = existing != null ? existing.movementId : string.Empty,
                movementName = BaduanjinMovementNames[i],
                videoClip = clip != null ? clip : existing != null ? existing.videoClip : null
            };
        }

        return bindings;
    }

    private static RehabMovementVideoBinding FindExistingBinding(RehabMovementVideoBinding[] bindings, string movementName, int index)
    {
        if (bindings == null) return null;

        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            if (binding != null && binding.movementName == movementName)
            {
                return binding;
            }
        }

        return index >= 0 && index < bindings.Length ? bindings[index] : null;
    }

    private static void BindGuideToSceneControllers(RehabVideoGuideController guide)
    {
        foreach (var sessionManager in Object.FindObjectsOfType<RehabSessionManager>(true))
        {
            if (sessionManager == null) continue;
            sessionManager.videoGuideController = guide;
            EditorUtility.SetDirty(sessionManager);
        }

        foreach (var modeSelect in Object.FindObjectsOfType<RehabModeSelectUI>(true))
        {
            if (modeSelect == null) continue;
            modeSelect.videoGuideController = guide;
            EditorUtility.SetDirty(modeSelect);
        }
    }

    private static RenderTexture LoadOrCreateRenderTexture()
    {
        var renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (renderTexture != null)
        {
            renderTexture.width = 1280;
            renderTexture.height = 720;
            EditorUtility.SetDirty(renderTexture);
            return renderTexture;
        }

        renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
        {
            name = "RehabVideoRT"
        };
        AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
        AssetDatabase.SaveAssets();
        return renderTexture;
    }

    private static Material LoadOrCreateVideoMaterial(RenderTexture renderTexture)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(VideoMaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Texture") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Standard");
            material = new Material(shader)
            {
                name = "RehabVideoMaterial"
            };
            AssetDatabase.CreateAsset(material, VideoMaterialPath);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", renderTexture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", renderTexture);
        }

        material.mainTexture = renderTexture;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        var child = parent.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.gameObject.scene.IsValid() && transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parent = System.IO.Path.GetDirectoryName(folderPath);
        var folder = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            parent = parent.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
