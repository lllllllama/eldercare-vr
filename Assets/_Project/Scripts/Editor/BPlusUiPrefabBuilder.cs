using System.Collections.Generic;
using PicoElderCare.Rehab;
using PicoElderCare.UI;
using PicoElderCare.UI.BPlus;
using TMPro;
using UnityEditor;
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

        SavePrefab(BuildMainEntryCanvas(), "BPlusMainEntryCanvas.prefab");
        SavePrefab(BuildPingPongTrainingPanel(), "BPlusPingPongTrainingPanel.prefab");
        SavePrefab(BuildRehabSelectPanel(), "BPlusRehabSelectPanel.prefab");
        SavePrefab(BuildRehabTrainingPanel(), "BPlusRehabTrainingPanel.prefab");
        SavePrefab(BuildSceneVideoPanel(), "BPlusSceneVideoPanel.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("B+ UI static phase 1 prefabs generated.");
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

    private static GameObject BuildMainEntryCanvas()
    {
        var root = CreateWorldCanvasRoot("BPlusMainEntryCanvas", ElderCareUiTheme.MainEntryDistanceMeters);
        var content = root.transform as RectTransform;
        CreatePanel(content, "Background", CanvasSize, Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusPanel, 0.98f), 44f, false);
        CreateText(content, "Title", "VR康养服务", new Vector2(600f, 76f), new Vector2(0f, 154f), 62f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Center);

        CreateButton(content, "Button_PingPong", "乒乓球训练", new Vector2(292f, 126f), new Vector2(-156f, 48f), ElderCareUiTheme.PingPongButton, "MainEntry", 0f);
        CreateButton(content, "Button_Rehab", "康复运动", new Vector2(292f, 126f), new Vector2(156f, 48f), ElderCareUiTheme.RehabButton, "MainEntry", 0.04f);
        CreateButton(content, "Button_Travel", "VR旅游", new Vector2(292f, 126f), new Vector2(-156f, -102f), ElderCareUiTheme.SoftButton, "MainEntry", 0.08f);
        CreateButton(content, "Button_SceneVideo", "场景视频", new Vector2(292f, 126f), new Vector2(156f, -102f), ElderCareUiTheme.SoftButton, "MainEntry", 0.12f);
        CreateStatusBadge(content, "TravelPendingBadge", "待接入", new Vector2(-48f, -58f), ElderCareUiTheme.StatusWarn);
        CreateText(content, "StatusText", "请选择服务", new Vector2(560f, 26f), new Vector2(0f, -194f), 18f, FontStyles.Bold, ElderCareUiTheme.BPlusMuted, TextAlignmentOptions.Center);
        var controller = root.AddComponent<BPlusMainEntryController>();
        controller.pingPongButton = FindButton(root.transform, "Button_PingPong");
        controller.rehabButton = FindButton(root.transform, "Button_Rehab");
        controller.travelButton = FindButton(root.transform, "Button_Travel");
        controller.sceneVideoButton = FindButton(root.transform, "Button_SceneVideo");
        controller.statusText = FindText(root.transform, "StatusText");
        return root;
    }

    private static GameObject BuildPingPongTrainingPanel()
    {
        var root = CreateWorldCanvasRoot("BPlusPingPongTrainingPanel", ElderCareUiTheme.HudDistanceMeters);
        var content = root.transform as RectTransform;
        CreatePanel(content, "Background", CanvasSize, Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusPanel, 0.98f), 44f, false);
        CreateHeader(content, "乒乓球训练", "待开始");

        CreateMetricCard(content, "Metric_Accuracy", "命中率", "0%", new Vector2(-178f, 72f), ElderCareUiTheme.PingPongButton);
        CreateMetricCard(content, "Metric_Difficulty", "难度", "标准", new Vector2(178f, 72f), ElderCareUiTheme.RehabButton);
        CreateMetricCard(content, "Metric_Hits", "有效击球", "0", new Vector2(-178f, -46f), ElderCareUiTheme.RehabButton);
        CreateMetricCard(content, "Metric_Time", "时长", "00:00", new Vector2(178f, -46f), ElderCareUiTheme.SoftButton);
        CreateHintStrip(content, "Hint", "当前难度适合热身和基础训练。", new Vector2(0f, -132f));

        CreateButton(content, "Button_Start", "开始训练", new Vector2(154f, 58f), new Vector2(-243f, -181f), ElderCareUiTheme.PingPongButton, "PingPongTraining", 0f);
        CreateButton(content, "Button_Return", "返回主页", new Vector2(154f, 58f), new Vector2(-81f, -181f), ElderCareUiTheme.SoftButton, "PingPongTraining", 0.03f);
        CreateButton(content, "Button_IncreaseDifficulty", "难度增加", new Vector2(154f, 58f), new Vector2(81f, -181f), ElderCareUiTheme.RehabButton, "PingPongTraining", 0.06f);
        CreateButton(content, "Button_DecreaseDifficulty", "难度降低", new Vector2(154f, 58f), new Vector2(243f, -181f), ElderCareUiTheme.SoftButton, "PingPongTraining", 0.09f);
        var controller = root.AddComponent<BPlusPingPongTrainingPanelController>();
        controller.startButton = FindButton(root.transform, "Button_Start");
        controller.returnButton = FindButton(root.transform, "Button_Return");
        controller.increaseDifficultyButton = FindButton(root.transform, "Button_IncreaseDifficulty");
        controller.decreaseDifficultyButton = FindButton(root.transform, "Button_DecreaseDifficulty");
        controller.statusText = FindText(root.transform, "Status/Text");
        controller.accuracyText = FindText(root.transform, "Metric_Accuracy/Value");
        controller.difficultyText = FindText(root.transform, "Metric_Difficulty/Value");
        controller.hitsText = FindText(root.transform, "Metric_Hits/Value");
        controller.durationText = FindText(root.transform, "Metric_Time/Value");
        return root;
    }

    private static GameObject BuildRehabSelectPanel()
    {
        var root = CreateWorldCanvasRoot("BPlusRehabSelectPanel", ElderCareUiTheme.RehabUiDistanceMeters);
        var content = root.transform as RectTransform;
        CreatePanel(content, "Background", CanvasSize, Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusPanel, 0.98f), 44f, false);
        CreateText(content, "Title", "请选择康复训练", new Vector2(600f, 64f), new Vector2(0f, 148f), 46f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Center);

        CreateTrainingOption(content, "Button_Baduanjin", "八段锦训练", "约 5 分钟", new Vector2(-160f, 22f), ElderCareUiTheme.RehabButton);
        CreateTrainingOption(content, "Button_TaiChi", "太极训练", "约 5 分钟", new Vector2(160f, 22f), ElderCareUiTheme.PingPongButton);
        CreateButton(content, "Button_Return", "返回", new Vector2(600f, 72f), new Vector2(0f, -146f), ElderCareUiTheme.SoftButton, "RehabSelect", 0.08f);
        var controller = root.AddComponent<BPlusRehabSelectController>();
        controller.baduanjinButton = FindButton(root.transform, "Button_Baduanjin");
        controller.taiChiButton = FindButton(root.transform, "Button_TaiChi");
        controller.returnButton = FindButton(root.transform, "Button_Return");
        return root;
    }

    private static GameObject BuildRehabTrainingPanel()
    {
        var root = CreateWorldCanvasRoot("BPlusRehabTrainingPanel", ElderCareUiTheme.RehabUiDistanceMeters);
        var content = root.transform as RectTransform;
        CreatePanel(content, "Background", CanvasSize, Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusPanel, 0.98f), 44f, false);
        CreateText(content, "Title", "八段锦：双手托天理三焦", new Vector2(600f, 46f), new Vector2(0f, 166f), 35f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Center);
        CreateText(content, "Description", "请保持舒适幅度，跟随提示完成动作。", new Vector2(600f, 34f), new Vector2(0f, 126f), 22f, FontStyles.Normal, ElderCareUiTheme.BPlusMuted, TextAlignmentOptions.Center);

        CreateMetricCard(content, "CountdownCard", "倒计时", "04:18", new Vector2(-188f, 40f), ElderCareUiTheme.PingPongButton);
        CreateProgressRing(content, "ProgressRing", new Vector2(188f, 36f), 0.38f);
        CreateHintStrip(content, "SafetyHint", "保持呼吸平稳，肩部不要用力过猛", new Vector2(0f, -104f));
        CreateText(content, "DevInfo", "开发信息：动作识别数据接入后刷新完成度", new Vector2(600f, 28f), new Vector2(0f, -144f), 18f, FontStyles.Normal, WithAlpha(ElderCareUiTheme.BPlusMuted, 0.66f), TextAlignmentOptions.Center);
        CreateButton(content, "Button_Return", "返回", new Vector2(600f, 60f), new Vector2(0f, -184f), ElderCareUiTheme.SoftButton, "RehabTraining", 0.08f);
        var controller = root.AddComponent<BPlusRehabTrainingPanelController>();
        controller.returnButton = FindButton(root.transform, "Button_Return");
        controller.titleText = FindText(root.transform, "Title");
        controller.descriptionText = FindText(root.transform, "Description");
        controller.countdownText = FindText(root.transform, "CountdownCard/Value");
        controller.progressText = FindText(root.transform, "ProgressRing/Center/Percent");
        controller.safetyText = FindText(root.transform, "SafetyHint/Text");
        controller.devInfoText = FindText(root.transform, "DevInfo");
        controller.progressFillImage = FindImage(root.transform, "ProgressRing/RingFill");
        return root;
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

    private static void CreateMetricCard(RectTransform parent, string name, string label, string value, Vector2 position, Color accent)
    {
        var panel = CreatePanel(parent, name, new Vector2(292f, 104f), position, WithAlpha(Color.Lerp(ElderCareUiTheme.BPlusCard, accent, 0.12f), 0.92f), 24f, false);
        var rect = panel.rectTransform;
        CreateText(rect, "Label", label, new Vector2(250f, 30f), new Vector2(0f, 24f), 22f, FontStyles.Bold, ElderCareUiTheme.BPlusMuted, TextAlignmentOptions.Center);
        CreateText(rect, "Value", value, new Vector2(250f, 48f), new Vector2(0f, -18f), 42f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
    }

    private static void CreateTrainingOption(RectTransform parent, string name, string title, string subtitle, Vector2 position, Color accent)
    {
        var button = CreateButton(parent, name, title, new Vector2(292f, 156f), position, accent, "RehabSelect", 0f);
        var rect = button.transform as RectTransform;
        var label = rect.Find("Label") as RectTransform;
        if (label != null)
        {
            label.anchoredPosition = new Vector2(0f, 18f);
            label.sizeDelta = new Vector2(250f, 58f);
        }

        CreateText(rect, "Subtitle", subtitle, new Vector2(250f, 30f), new Vector2(0f, -42f), 22f, FontStyles.Bold, ElderCareUiTheme.ButtonTextDark, TextAlignmentOptions.Center);
    }

    private static void CreateHintStrip(RectTransform parent, string name, string value, Vector2 position)
    {
        var panel = CreatePanel(parent, name, new Vector2(600f, 50f), position, WithAlpha(ElderCareUiTheme.StatusWarn, 0.14f), 18f, false);
        CreateText(panel.rectTransform, "Text", value, new Vector2(560f, 34f), Vector2.zero, 24f, FontStyles.Bold, ElderCareUiTheme.StatusWarn, TextAlignmentOptions.Center);
    }

    private static void CreateStatusBadge(RectTransform parent, string name, string value, Vector2 position, Color accent)
    {
        var panel = CreatePanel(parent, name, new Vector2(156f, 40f), position, Color.Lerp(ElderCareUiTheme.SoftButton, accent, 0.68f), 20f, false);
        CreateText(panel.rectTransform, "Text", value, new Vector2(132f, 28f), Vector2.zero, 20f, FontStyles.Bold, ElderCareUiTheme.ButtonTextDark, TextAlignmentOptions.Center);
    }

    private static void CreateProgressRing(RectTransform parent, string name, Vector2 position, float progress)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(148f, 148f);
        rect.anchoredPosition = position;

        var bg = CreateCircleImage(rect, "RingBackground", new Vector2(148f, 148f), WithAlpha(ElderCareUiTheme.SoftButton, 0.14f));
        bg.type = Image.Type.Filled;
        bg.fillMethod = Image.FillMethod.Radial360;
        bg.fillAmount = 1f;
        bg.raycastTarget = false;

        var fill = CreateCircleImage(rect, "RingFill", new Vector2(148f, 148f), ElderCareUiTheme.RehabButton);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = 2;
        fill.fillAmount = Mathf.Clamp01(progress);
        fill.raycastTarget = false;

        var center = CreatePanel(rect, "Center", new Vector2(100f, 100f), Vector2.zero, WithAlpha(ElderCareUiTheme.BPlusCard, 0.96f), 50f, false);
        CreateText(center.rectTransform, "Percent", "38%", new Vector2(82f, 44f), new Vector2(0f, 10f), 38f, FontStyles.Bold, ElderCareUiTheme.RehabButton, TextAlignmentOptions.Center);
        CreateText(center.rectTransform, "Label", "完成度", new Vector2(82f, 28f), new Vector2(0f, -24f), 18f, FontStyles.Bold, ElderCareUiTheme.BPlusText, TextAlignmentOptions.Center);
    }

    private static Image CreateCircleImage(RectTransform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
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
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureDefaultSceneObjects();
        var mainEntry = InstantiateBPlusPrefab("BPlusMainEntryCanvas.prefab");
        var controller = mainEntry.GetComponent<BPlusMainEntryController>();
        if (controller != null)
        {
            controller.loadScenes = true;
            controller.pingPongSceneName = "01_PingPongDemo_BPlusUI";
            controller.rehabSceneName = "MR_Rehab_Main_BPlusUI";
            controller.sceneVideoSceneName = "SceneVideo_BPlusUI";
            controller.sceneVideoAvailable = true;
        }

        SaveScene(scene, "00_MainEntry_BPlus.unity");
    }

    private static void GeneratePingPongScene()
    {
        const string destination = SceneFolder + "/01_PingPongDemo_BPlusUI.unity";
        var scene = CopyAndOpenScene("Assets/_Project/Scenes/01_PingPongDemo.unity", destination);
        EnsureDefaultSceneObjects();
        var bPlusPanel = InstantiateBPlusPrefab("BPlusPingPongTrainingPanel.prefab");
        var ballSpawner = Object.FindObjectOfType<BallSpawner>(true);
        var difficultyController = Object.FindObjectOfType<PingPongDifficultyController>(true);
        if (ballSpawner != null)
        {
            ballSpawner.autoStartOnPlay = false;
            EditorUtility.SetDirty(ballSpawner);
        }

        var controller = bPlusPanel.GetComponent<BPlusPingPongTrainingPanelController>();
        if (controller != null)
        {
            controller.ballSpawner = ballSpawner;
            controller.difficultyController = difficultyController;
            controller.returnBySceneLoad = true;
            controller.mainEntrySceneName = "00_MainEntry_BPlus";
        }

        var legacyRoots = HideLegacyCanvases(new[] { bPlusPanel });
        AddModeSwitch(new[] { bPlusPanel }, legacyRoots);
        SaveScene(scene, "01_PingPongDemo_BPlusUI.unity");
    }

    private static void GenerateRehabScene()
    {
        const string destination = SceneFolder + "/MR_Rehab_Main_BPlusUI.unity";
        var scene = CopyAndOpenScene("Assets/_Project/Scenes/MR_Rehab_Main.unity", destination);
        EnsureDefaultSceneObjects();
        var selectPanel = InstantiateBPlusPrefab("BPlusRehabSelectPanel.prefab");
        var trainingPanel = InstantiateBPlusPrefab("BPlusRehabTrainingPanel.prefab");
        trainingPanel.SetActive(false);

        var modeSelect = Object.FindObjectOfType<RehabModeSelectUI>(true);
        var sessionManager = Object.FindObjectOfType<RehabSessionManager>(true);
        var movementEvaluator = Object.FindObjectOfType<MovementEvaluator>(true);
        var uiController = Object.FindObjectOfType<RehabUIController>(true);
        if (modeSelect != null)
        {
            modeSelect.showTrainingSelectOnStart = false;
            EditorUtility.SetDirty(modeSelect);
        }

        var selectController = selectPanel.GetComponent<BPlusRehabSelectController>();
        if (selectController != null)
        {
            selectController.modeSelectUI = modeSelect;
            selectController.sessionManager = sessionManager;
            selectController.rehabTrainingPanel = trainingPanel;
            selectController.returnBySceneLoad = true;
            selectController.mainEntrySceneName = "00_MainEntry_BPlus";
        }

        var trainingController = trainingPanel.GetComponent<BPlusRehabTrainingPanelController>();
        if (trainingController != null)
        {
            trainingController.sessionManager = sessionManager;
            trainingController.movementEvaluator = movementEvaluator;
            trainingController.uiControllerAdapter = uiController;
            trainingController.rehabSelectPanel = selectPanel;
            trainingController.returnBySceneLoad = false;
        }

        var legacyRoots = HideLegacyCanvases(new[] { selectPanel, trainingPanel });
        AddModeSwitch(new[] { selectPanel, trainingPanel }, legacyRoots);
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

    private static GameObject[] HideLegacyCanvases(GameObject[] bPlusRoots)
    {
        var legacyRoots = new List<GameObject>();
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        for (var i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || IsInsideAny(canvas.transform, bPlusRoots)) continue;
            legacyRoots.Add(canvas.gameObject);
            canvas.gameObject.SetActive(false);
            EditorUtility.SetDirty(canvas.gameObject);
        }

        return legacyRoots.ToArray();
    }

    private static bool IsInsideAny(Transform target, GameObject[] roots)
    {
        if (target == null || roots == null) return false;
        for (var i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && target.IsChildOf(roots[i].transform))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddModeSwitch(GameObject[] bPlusRoots, GameObject[] legacyRoots)
    {
        var switchRoot = new GameObject("BPlusUiModeSwitch");
        var modeSwitch = switchRoot.AddComponent<BPlusUiModeSwitch>();
        modeSwitch.applyOnValidate = false;
        modeSwitch.useBPlusUi = true;
        modeSwitch.bPlusUiRoots = bPlusRoots;
        modeSwitch.legacyUiRoots = legacyRoots;
        modeSwitch.Apply();
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
