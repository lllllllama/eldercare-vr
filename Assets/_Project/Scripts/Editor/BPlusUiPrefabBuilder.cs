using System.Collections.Generic;
using PicoElderCare.Rehab;
using PicoElderCare.UI;
using PicoElderCare.UI.BPlus;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class BPlusUiPrefabBuilder
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/UI/BPlus";
    private const string SceneFolder = "Assets/_Project/Scenes/BPlus";
    private const string MainEntryScenePath = "Assets/_Project/Scenes/00_MainEntry.unity";
    private const string PingPongScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string RehabScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private static readonly Vector2 CanvasSize = ElderCareUiTheme.MainEntryCanvasSize;

    [MenuItem("PICO ElderCare/B+ UI/Generate All B+ Prefabs And Test Scenes")]
    public static void GenerateAllBPlusPrefabsAndTestScenes()
    {
        GenerateStaticPhaseOnePrefabs();
        GenerateBPlusTestScenes();
    }

    [MenuItem("PICO ElderCare/B+ UI/Generate Static Phase 1 Prefabs")]
    public static void GenerateStaticPhaseOnePrefabs()
    {
        EnsureFolder("Assets/_Project/Prefabs", "UI");
        EnsureFolder("Assets/_Project/Prefabs/UI", "BPlus");

        RemoveDeprecatedTakeoverPrefabs();
        SavePrefab(BuildSceneVideoPanel(), "BPlusSceneVideoPanel.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("B+ UI extension prefabs generated.");
    }

    [MenuItem("PICO ElderCare/B+ UI/Generate B+ Test Scenes")]
    public static void GenerateBPlusTestScenes()
    {
        EnsureFolder("Assets/_Project/Scenes", "BPlus");
        GenerateMainEntryScene();
        GeneratePingPongScene();
        GenerateRehabScene();
        GenerateSceneVideoScene();
        AddBPlusScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("B+ UI test scenes generated.");
    }

    private static GameObject BuildSceneVideoPanel()
    {
        var root = CreateWorldCanvasRoot("BPlusSceneVideoPanel", ElderCareUiTheme.MainEntryDistanceMeters);
        var content = root.transform as RectTransform;
        CreatePanel(content, "Background", CanvasSize, Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusPanel, 0.98f), 44f, false);
        CreateHeader(content, "场景视频", "视频播放");

        var videoRoot = CreatePanel(content, "VideoRoot", new Vector2(610f, 226f), new Vector2(0f, 30f), WithAlpha(ElderCareUiTheme.BPlusBackground, 0.86f), 24f, true);
        CreateVideoRawImage(videoRoot.rectTransform);
        CreatePanel(videoRoot.rectTransform, "VideoGlow", new Vector2(560f, 4f), new Vector2(0f, -54f), WithAlpha(ElderCareUiTheme.RehabButton, 0.34f), 2f, false);
        CreateText(videoRoot.rectTransform, "VideoPlaceholder", "场景视频画面", new Vector2(280f, 42f), new Vector2(-148f, -84f), 30f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Left);
        CreateStatusBadge(videoRoot.rectTransform, "PlayingBadge", "播放中", new Vector2(232f, 78f), ElderCareUiTheme.RehabButton);

        CreateButton(content, "Button_DecreaseSize", "画面缩小", new Vector2(192f, 58f), new Vector2(-206f, -108f), ElderCareUiTheme.SoftButton, "SceneVideo", 0f);
        CreateButton(content, "Button_IncreaseSize", "画面放大", new Vector2(192f, 58f), new Vector2(0f, -108f), ElderCareUiTheme.PingPongButton, "SceneVideo", 0.03f);
        CreateButton(content, "Button_Return", "返回", new Vector2(192f, 58f), new Vector2(206f, -108f), ElderCareUiTheme.SoftButton, "SceneVideo", 0.06f);
        CreateButton(content, "Button_DecreaseVolume", "音量降低", new Vector2(192f, 58f), new Vector2(-206f, -174f), ElderCareUiTheme.SoftButton, "SceneVideo", 0.09f);
        CreateButton(content, "Button_IncreaseVolume", "音量提高", new Vector2(192f, 58f), new Vector2(0f, -174f), ElderCareUiTheme.RehabButton, "SceneVideo", 0.12f);
        CreateVideoReadout(content, new Vector2(206f, -174f));
        var audioSource = videoRoot.gameObject.AddComponent<AudioSource>();
        var videoPlayer = videoRoot.gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Project/Videos/Rehab/Baduanjin/01_shuangshoutuotian_pico_720p.mp4");
        if (clip == null)
        {
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Project/Videos/Rehab/Baduanjin/01_shuangshoutuotian.mp4");
        }

        videoPlayer.clip = clip;

        var controller = root.AddComponent<BPlusSceneVideoPanelController>();
        controller.videoRoot = videoRoot.rectTransform;
        controller.videoImage = FindRawImage(root.transform, "VideoRoot/VideoRawImage");
        controller.videoPlayer = videoPlayer;
        controller.audioSource = audioSource;
        controller.decreaseSizeButton = FindButton(root.transform, "Button_DecreaseSize");
        controller.increaseSizeButton = FindButton(root.transform, "Button_IncreaseSize");
        controller.decreaseVolumeButton = FindButton(root.transform, "Button_DecreaseVolume");
        controller.increaseVolumeButton = FindButton(root.transform, "Button_IncreaseVolume");
        controller.returnButton = FindButton(root.transform, "Button_Return");
        controller.sizeReadoutText = FindText(root.transform, "VideoReadout/Size");
        controller.volumeReadoutText = FindText(root.transform, "VideoReadout/Volume");
        return root;
    }

    private static GameObject CreateWorldCanvasRoot(string name, float distanceMeters)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ComfortWorldSpaceUIPlacer));
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = CanvasSize;
        rect.localScale = Vector3.one * 0.0018f;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = CanvasSize;
        scaler.matchWidthOrHeight = 0.5f;

        var placer = root.GetComponent<ComfortWorldSpaceUIPlacer>();
        placer.uiRoot = root.transform;
        placer.distanceMeters = distanceMeters;
        placer.hmdHeightOffsetMeters = ElderCareUiTheme.DefaultUiHeightOffsetMeters;
        placer.placeOnStart = true;
        placer.placeOnEnable = false;
        placer.recenterDuringStartup = true;
        placer.startupRecenterSeconds = 1.25f;
        placer.startupRecenterFrames = 18;
        placer.enableRayDrag = true;
        placer.enableThumbstickNavigation = true;
        placer.comfortFollowEnabled = false;
        return root;
    }

    private static void CreateHeader(RectTransform parent, string titleText, string statusText)
    {
        CreateText(parent, "Title", titleText, new Vector2(420f, 46f), new Vector2(-96f, 170f), 40f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Left);
        CreateStatusBadge(parent, "Status", statusText, new Vector2(238f, 170f), ElderCareUiTheme.RehabButton);
    }

    private static ElderCareRoundedPanel CreatePanel(RectTransform parent, string name, Vector2 size, Vector2 position, Color color, float radius, bool raycastTarget)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(ElderCareRoundedPanel), typeof(Outline));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var panel = go.GetComponent<ElderCareRoundedPanel>();
        panel.cornerRadius = radius;
        panel.cornerSegments = 12;
        panel.color = color;
        panel.raycastTarget = raycastTarget;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = WithAlpha(ElderCareUiTheme.Stroke, 0.38f);
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static TMP_Text CreateText(RectTransform parent, string name, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 size, Vector2 position, Color accent, string panelName, float entranceDelay)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(ElderCareRoundedPanel), typeof(Outline), typeof(Button), typeof(CanvasGroup), typeof(TechModuleCardMotion), typeof(BPlusStaticButtonLogger));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, ElderCareUiTheme.BPlusMinButtonHeight));
        rect.anchoredPosition = position;

        var graphic = go.GetComponent<ElderCareRoundedPanel>();
        graphic.cornerRadius = 20f;
        graphic.cornerSegments = 10;
        graphic.color = Color.Lerp(ElderCareUiTheme.SoftButton, accent, 0.72f);
        graphic.raycastTarget = true;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = WithAlpha(ElderCareUiTheme.Stroke, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = graphic;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
        colors.pressedColor = new Color(0.86f, 0.9f, 0.94f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = WithAlpha(ElderCareUiTheme.Disabled, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var labelText = CreateText(rect, "Label", label, size, Vector2.zero, Mathf.Clamp(size.y * 0.42f, 24f, 36f), FontStyles.Bold, ElderCareUiTheme.ButtonTextDark, TextAlignmentOptions.Center);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 22f;
        labelText.fontSizeMax = Mathf.Clamp(size.y * 0.42f, 24f, 36f);

        var logger = go.GetComponent<BPlusStaticButtonLogger>();
        logger.panelName = panelName;
        logger.buttonLabel = label;
        var motion = go.GetComponent<TechModuleCardMotion>();
        motion.cardTransform = rect;
        motion.canvasGroup = go.GetComponent<CanvasGroup>();
        motion.cardGraphic = graphic;
        motion.normalColor = Color.Lerp(ElderCareUiTheme.SoftButton, accent, 0.72f);
        motion.hoverColor = Color.Lerp(ElderCareUiTheme.SoftButton, accent, 0.86f);
        motion.pressedColor = Color.Lerp(ElderCareUiTheme.BPlusCard, accent, 0.42f);
        motion.glowColor = WithAlpha(accent, 0.18f);
        motion.entranceDelay = entranceDelay;
        motion.hoverScale = 1.03f;
        motion.pressedScale = 0.96f;
        motion.ambientMotion = false;
        return button;
    }

    private static void CreateStatusBadge(RectTransform parent, string name, string value, Vector2 position, Color accent)
    {
        var panel = CreatePanel(parent, name, new Vector2(156f, 40f), position, Color.Lerp(ElderCareUiTheme.SoftButton, accent, 0.68f), 20f, false);
        CreateText(panel.rectTransform, "Text", value, new Vector2(132f, 28f), Vector2.zero, 20f, FontStyles.Bold, ElderCareUiTheme.ButtonTextDark, TextAlignmentOptions.Center);
    }

    private static void CreateVideoReadout(RectTransform parent, Vector2 position)
    {
        var panel = CreatePanel(parent, "VideoReadout", new Vector2(192f, 58f), position, WithAlpha(ElderCareUiTheme.BPlusCard, 0.86f), 18f, false);
        CreateText(panel.rectTransform, "Size", "画面大小 100%", new Vector2(168f, 24f), new Vector2(0f, 12f), 18f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Center);
        CreateText(panel.rectTransform, "Volume", "音量 54%", new Vector2(168f, 24f), new Vector2(0f, -12f), 18f, FontStyles.Bold, ElderCareUiTheme.PingPongButton, TextAlignmentOptions.Center);
    }

    private static RawImage CreateVideoRawImage(RectTransform parent)
    {
        var go = new GameObject("VideoRawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<RawImage>();
        image.color = new Color(1f, 1f, 1f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    private static void GenerateMainEntryScene()
    {
        const string destination = SceneFolder + "/00_MainEntry_BPlus.unity";
        var scene = CopyAndOpenScene(MainEntryScenePath, destination);
        EnsureDefaultSceneObjects();
        ConfigureUnifiedEntryMenusForBPlus();
        ConfigureSceneVideoEntry();
        SaveScene(scene, "00_MainEntry_BPlus.unity");
    }

    private static void GeneratePingPongScene()
    {
        const string destination = SceneFolder + "/01_PingPongDemo_BPlusUI.unity";
        var scene = CopyAndOpenScene(PingPongScenePath, destination);
        EnsureDefaultSceneObjects();
        ConfigureModuleHomeMenusForBPlus();
        SaveScene(scene, "01_PingPongDemo_BPlusUI.unity");
    }

    private static void GenerateRehabScene()
    {
        const string destination = SceneFolder + "/MR_Rehab_Main_BPlusUI.unity";
        var scene = CopyAndOpenScene(RehabScenePath, destination);
        EnsureDefaultSceneObjects();
        ConfigureModuleHomeMenusForBPlus();
        SaveScene(scene, "MR_Rehab_Main_BPlusUI.unity");
    }

    private static void GenerateSceneVideoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureDefaultSceneObjects();
        var panel = InstantiateBPlusPrefab("BPlusSceneVideoPanel.prefab");
        var controller = panel.GetComponent<BPlusSceneVideoPanelController>();
        if (controller != null)
        {
            controller.returnBySceneLoad = true;
            controller.mainEntrySceneName = "00_MainEntry_BPlus";
        }

        SaveScene(scene, "SceneVideo_BPlusUI.unity");
    }

    private static Scene CopyAndOpenScene(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destinationPath) != null)
        {
            AssetDatabase.DeleteAsset(destinationPath);
        }

        AssetDatabase.CopyAsset(sourcePath, destinationPath);
        AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
        return EditorSceneManager.OpenScene(destinationPath, OpenSceneMode.Single);
    }

    private static GameObject InstantiateBPlusPrefab(string fileName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + fileName);
        if (prefab == null)
        {
            Debug.LogError("Cannot instantiate missing B+ prefab: " + fileName);
            return new GameObject(fileName.Replace(".prefab", string.Empty));
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance != null)
        {
            instance.name = prefab.name;
        }

        return instance;
    }

    private static void RemoveDeprecatedTakeoverPrefabs()
    {
        DeleteAssetIfExists(PrefabFolder + "/BPlusMainEntryCanvas.prefab");
        DeleteAssetIfExists(PrefabFolder + "/BPlusPingPongTrainingPanel.prefab");
        DeleteAssetIfExists(PrefabFolder + "/BPlusRehabSelectPanel.prefab");
        DeleteAssetIfExists(PrefabFolder + "/BPlusRehabTrainingPanel.prefab");
    }

    private static void ConfigureUnifiedEntryMenusForBPlus()
    {
        var menus = Object.FindObjectsOfType<UnifiedEntryMenu>(true);
        for (var i = 0; i < menus.Length; i++)
        {
            if (menus[i] == null) continue;
            menus[i].healthGameMenuSceneName = "02_HealthGameMenu";
            menus[i].pingPongSceneName = "01_PingPongDemo_BPlusUI";
            menus[i].rehabSceneName = "MR_Rehab_Main_BPlusUI";
            EditorUtility.SetDirty(menus[i]);
        }
    }

    private static void ConfigureModuleHomeMenusForBPlus()
    {
        var moduleHomeMenus = Object.FindObjectsOfType<ModuleHomeMenu>(true);
        for (var i = 0; i < moduleHomeMenus.Length; i++)
        {
            if (moduleHomeMenus[i] == null) continue;
            moduleHomeMenus[i].mainEntrySceneName = "00_MainEntry_BPlus";
            EditorUtility.SetDirty(moduleHomeMenus[i]);
        }
    }

    private static void ConfigureSceneVideoEntry()
    {
        var moduleVideo = FindSceneObjectByName("Module_Video");
        if (moduleVideo == null)
        {
            Debug.LogWarning("B+ scene video entry was not configured because Module_Video was not found.");
            return;
        }

        var link = moduleVideo.GetComponent<BPlusSceneVideoEntryLink>();
        if (link == null)
        {
            link = moduleVideo.AddComponent<BPlusSceneVideoEntryLink>();
        }

        link.sceneName = "SceneVideo_BPlusUI";

        var button = moduleVideo.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
            UnityEventTools.AddPersistentListener(button.onClick, link.LoadSceneVideo);
            EditorUtility.SetDirty(button);
        }

        var motion = moduleVideo.GetComponent<TechModuleCardMotion>();
        if (motion != null)
        {
            motion.interactable = true;
            EditorUtility.SetDirty(motion);
        }

        SetDirectChildActive(moduleVideo.transform, "StatusBadgePanel", false);
        SetDirectChildActive(moduleVideo.transform, "StatusBadge", false);
        EditorUtility.SetDirty(moduleVideo);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var result = FindChildByName(roots[i].transform, objectName);
            if (result != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindChildByName(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void SetDirectChildActive(Transform root, string childName, bool active)
    {
        if (root == null) return;
        var child = root.Find(childName);
        if (child == null) return;
        child.gameObject.SetActive(active);
        EditorUtility.SetDirty(child.gameObject);
    }

    private static void DeleteAssetIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void EnsureDefaultSceneObjects()
    {
        if (Camera.main == null)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 1.62f, -0.25f);
            cameraGo.transform.rotation = Quaternion.identity;
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ElderCareUiTheme.BPlusBackground;
        }

        if (Object.FindObjectOfType<Light>(true) == null)
        {
            var lightGo = new GameObject("Directional Light", typeof(Light));
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGo.GetComponent<Light>().type = LightType.Directional;
        }

        if (Object.FindObjectOfType<EventSystem>(true) == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static void SaveScene(Scene scene, string fileName)
    {
        EditorSceneManager.SaveScene(scene, SceneFolder + "/" + fileName);
    }

    private static void AddBPlusScenesToBuildSettings()
    {
        var targetScenes = new[]
        {
            SceneFolder + "/00_MainEntry_BPlus.unity",
            SceneFolder + "/01_PingPongDemo_BPlusUI.unity",
            SceneFolder + "/MR_Rehab_Main_BPlusUI.unity",
            SceneFolder + "/SceneVideo_BPlusUI.unity"
        };

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (var i = 0; i < targetScenes.Length; i++)
        {
            var scenePath = targetScenes[i];
            var exists = false;
            for (var j = 0; j < scenes.Count; j++)
            {
                if (scenes[j].path == scenePath)
                {
                    scenes[j] = new EditorBuildSettingsScene(scenePath, true);
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SavePrefab(GameObject root, string fileName)
    {
        var path = PrefabFolder + "/" + fileName;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Button FindButton(Transform root, string path)
    {
        return FindComponent<Button>(root, path);
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        return FindComponent<TMP_Text>(root, path);
    }

    private static Image FindImage(Transform root, string path)
    {
        return FindComponent<Image>(root, path);
    }

    private static RawImage FindRawImage(Transform root, string path)
    {
        return FindComponent<RawImage>(root, path);
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;
        var direct = root.Find(path);
        if (direct != null)
        {
            return direct.GetComponent<T>();
        }

        var components = root.GetComponentsInChildren<T>(true);
        for (var i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == path)
            {
                return components[i];
            }
        }

        return null;
    }

    private static void EnsureFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
