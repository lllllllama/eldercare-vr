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

    private static readonly string[] LegacyBaduanjinMovementNames =
    {
        "\u4e24\u624b\u6258\u5929\u7406\u4e09\u7126",
        "\u5de6\u53f3\u5f00\u5f13\u4f3c\u5c04\u96d5",
        "\u8c03\u7406\u813e\u80c3\u987b\u5355\u4e3e",
        "\u4e94\u52b3\u4e03\u4f24\u5f80\u540e\u77a7",
        "\u6447\u5934\u6446\u5c3e\u53bb\u5fc3\u706b",
        "\u4e24\u624b\u6500\u8db3\u56fa\u80be\u8170",
        "\u6512\u62f3\u6012\u76ee\u589e\u6c14\u529b",
        "\u80cc\u540e\u4e03\u98a0\u767e\u75c5\u6d88"
    };
    private static readonly string[] LegacyBaduanjinVideoPaths =
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

    private static readonly RehabMovementId[] BaduanjinMovementIds =
    {
        RehabMovementId.Baduanjin_Guoti_00_WujiZhuang,
        RehabMovementId.Baduanjin_Guoti_01_BaoqiuZhuang,
        RehabMovementId.Baduanjin_Guoti_02_LiangshouTuotian,
        RehabMovementId.Baduanjin_Guoti_03_YouKaigong,
        RehabMovementId.Baduanjin_Guoti_04_YouKaigongBingbu,
        RehabMovementId.Baduanjin_Guoti_05_ZuoKaigong,
        RehabMovementId.Baduanjin_Guoti_06_ZuoKaigongBingbu,
        RehabMovementId.Baduanjin_Guoti_07_YouShangju,
        RehabMovementId.Baduanjin_Guoti_08_YouXialuo,
        RehabMovementId.Baduanjin_Guoti_09_ZuoShangju,
        RehabMovementId.Baduanjin_Guoti_10_ZuoXialuo,
        RehabMovementId.Baduanjin_Guoti_11_YouHouqiao,
        RehabMovementId.Baduanjin_Guoti_12_YouHouqiaoZhuanzheng,
        RehabMovementId.Baduanjin_Guoti_13_ZuoHouqiao,
        RehabMovementId.Baduanjin_Guoti_14_ZuoHouqiaoZhuanzheng,
        RehabMovementId.Baduanjin_Guoti_15_ShangtuoXiaan,
        RehabMovementId.Baduanjin_Guoti_16_YouxuanYaotouBaiwei,
        RehabMovementId.Baduanjin_Guoti_17_ZuoxuanYaotouBaiwei,
        RehabMovementId.Baduanjin_Guoti_18_LiangshouPanzu,
        RehabMovementId.Baduanjin_Guoti_19_TaishouFanchuan,
        RehabMovementId.Baduanjin_Guoti_20_FanchuanPanzu,
        RehabMovementId.Baduanjin_Guoti_21_PanzuJushou,
        RehabMovementId.Baduanjin_Guoti_22_JushouXiaanFuwei,
        RehabMovementId.Baduanjin_Guoti_23_CuanquanMabu,
        RehabMovementId.Baduanjin_Guoti_24_ChuquanShouquan,
        RehabMovementId.Baduanjin_Guoti_25_HuanshouChuquanShouquan,
        RehabMovementId.Baduanjin_Guoti_26_JieshuFuwei,
        RehabMovementId.Baduanjin_Guoti_27_Tizhong,
        RehabMovementId.Baduanjin_Guoti_28_ShuangshouBaofu,
        RehabMovementId.Baduanjin_Guoti_29_ShoushiTiaoxi
    };

    private static readonly string[] BaduanjinMovementNames =
    {
        "\u65e0\u6781\u6869",
        "\u62b1\u7403\u6869\uff08\u9884\u5907\u52bf\uff09",
        "\u4e24\u624b\u6258\u5929\u7406\u4e09\u7126\uff086 \u6b21\uff09",
        "\u53f3\u5f00\u5f13",
        "\u53f3\u5f00\u5de5\u5e76\u6b65",
        "\u5de6\u5f00\u5de5",
        "\u5de6\u5f00\u5f13\u5e76\u6b65",
        "\u53f3\u4e0a\u4e3e",
        "\u53f3\u4e0b\u843d",
        "\u5de6\u4e0a\u4e3e",
        "\u5de6\u4e0b\u843d",
        "\u53f3\u540e\u77a7",
        "\u53f3\u540e\u77a7\u8f6c\u6b63",
        "\u5de6\u540e\u77a7",
        "\u5de6\u540e\u77a7\u8f6c\u6b63",
        "\u4e0a\u6258\u4e0b\u6309",
        "\u53f3\u65cb\u6447\u5934\u6446\u5c3e",
        "\u5de6\u65cb\u6447\u5934\u6446\u5c3e",
        "\u4e24\u624b\u6500\u8db3\u56fa\u80be\u8170",
        "\u62ac\u624b\u53cd\u7a7f",
        "\u53cd\u7a7f\u6500\u8db3",
        "\u6500\u8db3\u4e3e\u624b",
        "\u4e3e\u624b\u4e0b\u6309\u590d\u4f4d",
        "\u6512\u62f3\u9a6c\u6b65",
        "\u51fa\u62f3\u6536\u62f3",
        "\u6362\u624b\u51fa\u62f3\u6536\u62f3",
        "\u7ed3\u675f\u590d\u4f4d",
        "\u63d0\u8e35",
        "\u53cc\u624b\u62b1\u8179",
        "\u6536\u52bf\u8c03\u606f"
    };

    private static readonly string[] BaduanjinVideoPaths =
    {
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/00_wuji_zhuang.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/01_baoqiu_zhuang.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/02_liangshou_tuotian.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/03_you_kaigong.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/04_you_kaigong_bingbu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/05_zuo_kaigong.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/06_zuo_kaigong_bingbu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/07_you_shangju.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/08_you_xialuo.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/09_zuo_shangju.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/10_zuo_xialuo.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/11_you_houqiao.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/12_you_houqiao_zhuanzheng.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/13_zuo_houqiao.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/14_zuo_houqiao_zhuanzheng.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/15_shangtuo_xiaan.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/16_youxuan_yaotou_baiwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/17_zuoxuan_yaotou_baiwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/18_liangshou_panzu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/19_taishou_fanchuan.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/20_fanchuan_panzu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/21_panzu_jushou.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/22_jushou_xiaan_fuwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/23_cuanquan_mabu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/24_chuquan_shouquan.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/25_huanshou_chuquan_shouquan.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/26_jieshu_fuwei.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/27_tizhong.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/28_shuangshou_baofu.mp4",
        "Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed/29_shoushi_tiaoxi.mp4"
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
        var count = Mathf.Min(
            BaduanjinMovementIds.Length,
            Mathf.Min(BaduanjinMovementNames.Length, BaduanjinVideoPaths.Length));
        var bindings = new RehabMovementVideoBinding[count];
        for (var i = 0; i < count; i++)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(BaduanjinVideoPaths[i]);
            if (clip == null)
            {
                Debug.LogWarning($"Missing detailed Baduanjin video clip: {BaduanjinVideoPaths[i]}");
            }

            bindings[i] = new RehabMovementVideoBinding
            {
                movementId = BaduanjinMovementIds[i].ToString(),
                movementName = BaduanjinMovementNames[i],
                videoClip = clip
            };
        }

        if (count != 30)
        {
            Debug.LogWarning($"Detailed Baduanjin binding catalog expected 30 entries, but generated {count}.");
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
