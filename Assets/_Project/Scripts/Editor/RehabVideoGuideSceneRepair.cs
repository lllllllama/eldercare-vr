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
    private const string VideoMaterialPath = "Assets/_Project/Materials/RehabVideoMaterial.mat";
    private const string RenderTexturePath = "Assets/_Project/RenderTextures/RehabVideoRT.renderTexture";

    private static readonly RehabMovementId[] BaduanjinMovementIds =
    {
        RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
        RehabMovementId.Baduanjin_DrawBowShootHawk,
        RehabMovementId.Baduanjin_SingleRaiseRegulateSpleen,
        RehabMovementId.Baduanjin_LookBackRelieveStrain,
        RehabMovementId.Baduanjin_SwayHeadTailClearHeartFire,
        RehabMovementId.Baduanjin_TouchKneesStrengthenKidneys,
        RehabMovementId.Baduanjin_ClenchFistsAngryEyes,
        RehabMovementId.Baduanjin_HeelRaiseFinish
    };

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
    public static void RepairRehabScene()
    {
        var scene = EditorSceneManager.OpenScene(RehabScenePath, OpenSceneMode.Single);
        var session = Object.FindObjectOfType<RehabSessionManager>(true);
        var modeSelect = Object.FindObjectOfType<RehabModeSelectUI>(true);
        if (session == null)
        {
            throw new System.InvalidOperationException("Cannot repair Baduanjin video guide because RehabSessionManager is missing.");
        }

        var visualRoot = FindOrCreateRoot("RehabVisuals");
        var head = ResolveHeadTransform(session);
        EnsureBaduanjinVideoGuideForScene(visualRoot, session, modeSelect, head);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static RehabVideoGuideController EnsureBaduanjinVideoGuideForScene(
        Transform parent,
        RehabSessionManager session,
        RehabModeSelectUI modeSelect,
        Transform headTransform)
    {
        if (session == null) return null;

        var guide = session.videoGuideController != null
            ? session.videoGuideController
            : Object.FindObjectOfType<RehabVideoGuideController>(true);

        GameObject panelObject;
        if (guide != null)
        {
            panelObject = guide.gameObject;
        }
        else
        {
            panelObject = GameObject.Find("RehabVideoPanel") ?? new GameObject("RehabVideoPanel");
        }

        if (parent != null && panelObject.transform.parent != parent)
        {
            panelObject.transform.SetParent(parent, false);
        }

        panelObject.transform.localPosition = new Vector3(0.95f, 1.58f, 2.25f);
        panelObject.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
        panelObject.transform.localScale = Vector3.one;
        panelObject.SetActive(true);

        var videoPlayer = GetOrAdd<VideoPlayer>(panelObject);
        var audioSource = GetOrAdd<AudioSource>(panelObject);
        var layout = GetOrAdd<RehabVideoPanelLayoutController>(panelObject);
        guide = GetOrAdd<RehabVideoGuideController>(panelObject);

        var renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(VideoMaterialPath);
        var videoQuad = EnsureVideoQuad(panelObject.transform, material, renderTexture);
        var rawImage = EnsureRawImageCanvas(panelObject.transform, renderTexture, out var displayRoot, out var debugBackground, out var debugBorder);

        ConfigureVideoPlayer(videoPlayer, audioSource, renderTexture);
        ConfigureLayout(layout, panelObject.transform, videoQuad.transform, session, headTransform);
        ConfigureGuide(guide, panelObject, displayRoot, rawImage, videoPlayer, audioSource, videoQuad, material, renderTexture, layout, session, debugBackground, debugBorder);

        session.videoGuideController = guide;
        if (modeSelect != null)
        {
            modeSelect.videoGuideController = guide;
        }

        EditorUtility.SetDirty(panelObject);
        EditorUtility.SetDirty(videoPlayer);
        EditorUtility.SetDirty(audioSource);
        EditorUtility.SetDirty(layout);
        EditorUtility.SetDirty(guide);
        EditorUtility.SetDirty(session);
        if (modeSelect != null) EditorUtility.SetDirty(modeSelect);

        return guide;
    }

    private static void ConfigureVideoPlayer(VideoPlayer videoPlayer, AudioSource audioSource, RenderTexture renderTexture)
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.isLooping = false;

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.7f;
        audioSource.mute = false;
    }

    private static void ConfigureLayout(
        RehabVideoPanelLayoutController layout,
        Transform panelRoot,
        Transform videoQuad,
        RehabSessionManager session,
        Transform headTransform)
    {
        layout.panelRoot = panelRoot;
        layout.videoQuad = videoQuad;
        layout.headTransform = headTransform;
        layout.promptCanvas = session.promptCanvas;
        layout.trainingAreaRoot = session.trainingAreaRoot;
        layout.panelDistance = 2.35f;
        layout.videoRightOffset = 1.0f;
        layout.heightOffset = 0.02f;
        layout.videoWidth = 0.72f;
        layout.videoHeight = 0.405f;
        layout.videoScale = 1f;
        layout.minVideoScale = 0.65f;
        layout.maxVideoScale = 1.7f;
        layout.preferPromptCanvasLayout = false;
        layout.followTrainingAreaRoot = false;
        layout.preserveUserPlacement = true;
        layout.faceUser = true;
        layout.ApplyVideoSize();
    }

    private static void ConfigureGuide(
        RehabVideoGuideController guide,
        GameObject videoPanel,
        GameObject displayRoot,
        RawImage rawImage,
        VideoPlayer videoPlayer,
        AudioSource audioSource,
        GameObject videoQuad,
        Material material,
        RenderTexture renderTexture,
        RehabVideoPanelLayoutController layout,
        RehabSessionManager session,
        GameObject debugBackground,
        GameObject debugBorder)
    {
        guide.videoPanel = videoPanel;
        guide.displayRoot = displayRoot;
        guide.rawImage = rawImage;
        guide.videoPlayer = videoPlayer;
        guide.audioSource = audioSource;
        guide.videoQuad = videoQuad;
        guide.videoQuadRenderer = videoQuad.GetComponent<Renderer>();
        guide.videoMaterial = material;
        guide.renderTexture = renderTexture;
        guide.layoutController = layout;
        guide.sessionManager = session;
        guide.displayMode = RehabVideoDisplayMode.QuadMaterial;
        guide.requireActiveSession = true;
        guide.muteAudio = false;
        guide.volume = 0.7f;
        guide.loopVideo = false;
        guide.showDebugFrame = false;
        guide.autoCreateVideoFrame = true;
        guide.debugBackground = debugBackground;
        guide.debugBorder = debugBorder;
        guide.bindings = CreateBaduanjinBindings();
    }

    private static RehabMovementVideoBinding[] CreateBaduanjinBindings()
    {
        var bindings = new RehabMovementVideoBinding[BaduanjinMovementNames.Length];
        for (var i = 0; i < bindings.Length; i++)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(BaduanjinVideoPaths[i]);
            if (clip == null)
            {
                Debug.LogWarning("Missing Baduanjin video clip: " + BaduanjinVideoPaths[i]);
            }

            bindings[i] = new RehabMovementVideoBinding
            {
                movementId = BaduanjinMovementIds[i].ToString(),
                movementName = BaduanjinMovementNames[i],
                videoClip = clip
            };
        }

        return bindings;
    }

    private static GameObject EnsureVideoQuad(Transform parent, Material material, RenderTexture renderTexture)
    {
        var existing = parent.Find("VideoQuad");
        var quad = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "VideoQuad";
        if (quad.transform.parent != parent)
        {
            quad.transform.SetParent(parent, false);
        }

        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(0.72f, 0.405f, 1f);
        quad.SetActive(false);

        var collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = quad.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            material.mainTexture = renderTexture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", renderTexture);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            renderer.sharedMaterial = material;
        }

        return quad;
    }

    private static RawImage EnsureRawImageCanvas(
        Transform parent,
        RenderTexture renderTexture,
        out GameObject displayRoot,
        out GameObject debugBackground,
        out GameObject debugBorder)
    {
        displayRoot = FindOrCreateChild(parent, "RehabVideoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = displayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        displayRoot.transform.localPosition = Vector3.zero;
        displayRoot.transform.localRotation = Quaternion.identity;
        displayRoot.transform.localScale = Vector3.one * 0.001f;
        displayRoot.SetActive(false);

        var rect = displayRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1280f, 720f);

        var rawObject = FindOrCreateChild(displayRoot.transform, "VideoRawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rawRect = rawObject.GetComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;
        var rawImage = rawObject.GetComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;

        debugBackground = FindOrCreateChild(displayRoot.transform, "DebugBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        debugBackground.SetActive(false);
        debugBorder = FindOrCreateChild(displayRoot.transform, "DebugBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Outline));
        debugBorder.SetActive(false);

        return rawImage;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var child = new GameObject(name, components);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Transform FindOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        var root = new GameObject(name);
        return root.transform;
    }

    private static Transform ResolveHeadTransform(RehabSessionManager session)
    {
        if (session != null && session.handPoseTracker != null && session.handPoseTracker.hmdTransform != null)
        {
            return session.handPoseTracker.hmdTransform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
