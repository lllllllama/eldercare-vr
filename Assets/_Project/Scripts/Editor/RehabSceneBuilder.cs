using PicoElderCare.Rehab;
using PicoElderCare.UI;
using TMPro;
using Unity.XR.CoreUtils;
using Unity.XR.PXR;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class RehabSceneBuilder
{
    private const string MainEntryScenePath = "Assets/_Project/Scenes/00_MainEntry.unity";
    private const string DeviceTestScenePath = "Assets/_Project/Scenes/00_DeviceTest.unity";
    private const string PingPongScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string RehabScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string MaterialRoot = "Assets/_Project/Materials/Rehab";
    private const string FontRoot = "Assets/_Project/Fonts/Rehab";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string XrUiInputModulePresetPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Presets/XRI Default XR UI Input Module.preset";
    private const string XriDefaultInputActionsPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/XRI Default Input Actions.inputactions";
    private const string RehabChineseFontSourcePath = FontRoot + "/NotoSansSC-VF.ttf";
    private const string RehabChineseFontAssetPath = MaterialRoot + "/RehabChineseTMP.asset";

    private static TMP_FontAsset rehabFontAsset;

    private struct RehabTrainingUi
    {
        public GameObject canvas;
        public GameObject mainMenuPanel;
        public GameObject rehabTrainingSelectPanel;
        public GameObject rehabTrainingPanel;
        public GameObject trainingResultPanel;
        public TMP_Text title;
        public TMP_Text status;
        public TMP_Text timer;
        public TMP_Text completion;
        public TMP_Text safety;
        public TMP_Text debug;
        public Button rehabButton;
        public Button baduanjinButton;
        public Button taiChiButton;
        public Button backButton;
        public Button startButton;
        public Button trainingBackButton;
        public Button resultBackButton;
    }

    [MenuItem("Tools/PICO ElderCare/Build Main Entry Scene")]
    public static void BuildMainEntryScene()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        BuildMainEntrySceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/PICO ElderCare/Build MR Rehab Main Scene")]
    public static void BuildMrRehabMainScene()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        BuildRehabSceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/PICO ElderCare/Build Unified MVP Scenes")]
    public static void BuildUnifiedMvpScenes()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        PingPongDemoSceneBuilder.BuildMixedRealityDemoScene();
        BuildMainEntrySceneInternal();
        BuildRehabSceneInternal();
        AddReturnHomePanelToPingPongSceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildMainEntrySceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var xrOrigin = CreateXrOrigin();
        var mainCamera = FindMainCamera();

        EnsureLight();
        EnsureXrInteractionSupport();

        var managers = new GameObject("EntryManagers");
        var menu = managers.AddComponent<UnifiedEntryMenu>();
        menu.pingPongSceneName = "01_PingPongDemo";
        menu.rehabSceneName = "MR_Rehab_Main";

        var mrManager = managers.AddComponent<RehabMixedRealityManager>();
        mrManager.targetCamera = mainCamera;
        mrManager.enableOnStart = true;
        mrManager.enableVideoSeeThrough = true;
        mrManager.configureTransparentCamera = true;
        mrManager.suppressBackgroundVisuals = true;

        var backgroundSuppressor = managers.AddComponent<MrBackgroundVisualSuppressor>();
        backgroundSuppressor.hideAllEnvironmentRenderers = true;
        backgroundSuppressor.hideAllRoomSensingRenderers = true;
        backgroundSuppressor.scanIntervalSeconds = 0.15f;

        SetupPicoRoomSensingManagers(managers.transform);

        var uiRoot = CreateUiRoot("UIRoot", null);
        var entryCanvas = BuildEntryCanvas(menu, null);
        AttachUiToRoot(entryCanvas.transform, uiRoot.transform);
        var entryUiPlacer = ConfigureComfortUiPlacer(uiRoot, mainCamera != null ? mainCamera.transform : null, uiRoot.transform, ElderCareUiTheme.MainEntryDistanceMeters);
        entryUiPlacer.placeOnStart = false;
        entryUiPlacer.recenterDuringStartup = false;

        var entryPanelPlacement = uiRoot.AddComponent<RehabPanelPlacementController>();
        entryPanelPlacement.headTransform = mainCamera != null ? mainCamera.transform : null;
        entryPanelPlacement.promptPanelRoot = uiRoot.transform;
        entryPanelPlacement.promptPanelDistance = ElderCareUiTheme.MainEntryDistanceMeters;
        entryPanelPlacement.videoPanelDistance = 2.2f;
        entryPanelPlacement.videoPanelYawOffsetDegrees = 40f;
        entryPanelPlacement.panelHeight = 1.45f;
        entryPanelPlacement.placeOnStart = false;
        menu.panelPlacementController = entryPanelPlacement;
        menu.recenterPanelsOnEnable = true;
        menu.recenterDelayFrames = 2;

        EditorUtility.SetDirty(managers);
        EditorUtility.SetDirty(uiRoot);
        EditorUtility.SetDirty(entryCanvas);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);

        EditorSceneManager.SaveScene(scene, MainEntryScenePath);
    }

    private static void BuildRehabSceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var xrOrigin = CreateXrOrigin();
        var mainCamera = FindMainCamera();
        var hmd = mainCamera != null ? mainCamera.transform : null;
        var leftController = FindChildByName(xrOrigin != null ? xrOrigin.transform : null, "Left Controller");
        var rightController = FindChildByName(xrOrigin != null ? xrOrigin.transform : null, "Right Controller");

        EnsureLight();
        EnsureXrInteractionSupport();

        var rehabRoot = new GameObject("Rehab");
        var visualRoot = new GameObject("RehabVisuals");
        visualRoot.transform.SetParent(rehabRoot.transform, false);
        var uiRoot = CreateUiRoot("UIRoot", rehabRoot.transform);
        var managers = new GameObject("RehabManagers");
        managers.transform.SetParent(rehabRoot.transform, false);
        var homeMenu = managers.AddComponent<ModuleHomeMenu>();

        var trainingArea = BuildTrainingArea(visualRoot.transform);
        var rehabUi = BuildRehabPromptCanvas(uiRoot.transform, mainCamera, homeMenu);
        var promptCanvas = rehabUi.canvas;
        var rehabUiPlacer = ConfigureComfortUiPlacer(uiRoot, hmd, uiRoot.transform, 2f);
        rehabUiPlacer.placeOnStart = false;
        rehabUiPlacer.recenterDuringStartup = false;

        var poseTracker = managers.AddComponent<HandPoseTracker>();
        poseTracker.hmdTransform = hmd;
        poseTracker.leftControllerTransform = leftController;
        poseTracker.rightControllerTransform = rightController;

        var safetyMonitor = managers.AddComponent<SafetyMonitor>();
        safetyMonitor.hmdTransform = hmd;
        safetyMonitor.pauseDistanceMeters = 1.2f;
        safetyMonitor.resumeDistanceMeters = 1.1f;

        var baduanjinEvaluator = managers.AddComponent<BaduanjinEvaluator>();
        var taiChiEvaluator = managers.AddComponent<TaiChiEvaluator>();
        var evaluator = managers.AddComponent<MovementEvaluator>();
        evaluator.trainingMode = RehabTrainingMode.Baduanjin;
        evaluator.baduanjinEvaluator = baduanjinEvaluator;
        evaluator.taiChiEvaluator = taiChiEvaluator;
        evaluator.movementDefinitions = BaduanjinEvaluator.CreateDefaultMovements();
        evaluator.movementId = RehabMovementId.Baduanjin_TwoHandsLiftHeaven;
        evaluator.movementName = "八段锦：双手托天理三焦";
        evaluator.handsAboveHeadMeters = 0.15f;
        evaluator.maximumHandHeightDifferenceMeters = 0.18f;
        evaluator.minimumHoldSeconds = 2f;
        evaluator.maximumHoldSeconds = 5f;

        var recorder = managers.AddComponent<TrainingResultRecorder>();
        var uiController = promptCanvas.AddComponent<RehabUIController>();
        uiController.movementNameText = rehabUi.title;
        uiController.stepText = rehabUi.status;
        uiController.remainingTimeText = rehabUi.timer;
        uiController.completionText = rehabUi.completion;
        uiController.safetyPromptText = rehabUi.safety;
        uiController.debugText = rehabUi.debug;
        uiController.startButton = rehabUi.startButton;

        var trainingCircleAnchor = managers.AddComponent<TrainingCircleAnchor>();
        trainingCircleAnchor.headTransform = hmd;
        trainingCircleAnchor.trainingAreaRoot = trainingArea.transform;
        trainingCircleAnchor.fallbackFloorY = 0f;

        var startFlow = managers.AddComponent<RehabStartFlowController>();
        startFlow.uiController = uiController;
        startFlow.startButton = rehabUi.startButton;
        startFlow.startPreparationDelaySeconds = 5f;
        startFlow.preMovementCountdownSeconds = 3f;
        startFlow.movementRecoveryDelaySeconds = 8f;

        var panelPlacement = managers.AddComponent<RehabPanelPlacementController>();
        panelPlacement.headTransform = hmd;
        panelPlacement.promptPanelRoot = promptCanvas.transform;
        panelPlacement.promptPanelDistance = 1.8f;
        panelPlacement.videoPanelDistance = 2.2f;
        panelPlacement.videoPanelYawOffsetDegrees = 40f;
        panelPlacement.panelHeight = 1.45f;

        var virtualCoach = BuildVirtualCoach(visualRoot.transform, hmd);

        var session = managers.AddComponent<RehabSessionManager>();
        session.handPoseTracker = poseTracker;
        session.safetyMonitor = safetyMonitor;
        session.movementEvaluator = evaluator;
        session.uiController = uiController;
        session.resultRecorder = recorder;
        session.virtualCoachController = virtualCoach;
        session.coachPlaybackStateOnMovementStart = CoachPlaybackState.Demonstration;
        session.trainingCircleAnchor = trainingCircleAnchor;
        session.startFlowController = startFlow;
        session.panelPlacementController = panelPlacement;
        session.trainingAreaRoot = trainingArea.transform;
        session.promptCanvas = promptCanvas.transform;
        session.placePromptCanvasWithTrainingArea = false;
        session.titleText = rehabUi.title;
        session.statusText = rehabUi.status;
        session.timerText = rehabUi.timer;
        session.debugText = rehabUi.debug;
        session.sessionDurationSeconds = 300f;
        session.autoStartSession = false;
        session.trainingDistanceMeters = 1.5f;
        session.trainingFloorY = 0f;
        session.promptHeightMeters = 1.65f;
        session.promptForwardOffsetMeters = 0.85f;
        session.useOpenSpacePlacement = false;
        session.refreshOpenSpaceAfterPlacement = false;
        session.openSpaceClearanceRadiusMeters = 0.85f;
        session.openSpaceClearanceHeightMeters = 1.7f;
        session.openSpaceMinDistanceMeters = 1.2f;
        session.openSpaceMaxDistanceMeters = 3.0f;
        session.openSpaceSearchDurationSeconds = 10f;
        session.openSpaceSearchIntervalSeconds = 0.5f;

        var modeSelectUi = promptCanvas.AddComponent<RehabModeSelectUI>();
        modeSelectUi.mainMenuPanel = rehabUi.mainMenuPanel;
        modeSelectUi.rehabTrainingSelectPanel = rehabUi.rehabTrainingSelectPanel;
        modeSelectUi.rehabTrainingPanel = rehabUi.rehabTrainingPanel;
        modeSelectUi.trainingResultPanel = rehabUi.trainingResultPanel;
        modeSelectUi.rehabButton = rehabUi.rehabButton;
        modeSelectUi.baduanjinButton = rehabUi.baduanjinButton;
        modeSelectUi.taiChiButton = rehabUi.taiChiButton;
        modeSelectUi.backButton = rehabUi.backButton;
        modeSelectUi.trainingBackButton = rehabUi.trainingBackButton;
        modeSelectUi.resultBackButton = rehabUi.resultBackButton;
        modeSelectUi.homeMenu = homeMenu;
        modeSelectUi.uiPlacer = rehabUiPlacer;
        modeSelectUi.panelPlacementController = panelPlacement;
        modeSelectUi.sessionManager = session;
        modeSelectUi.showTrainingSelectOnStart = true;
        modeSelectUi.placeUiOnStart = true;
        modeSelectUi.placeUiOnMainMenuOpen = true;
        session.modeSelectUI = modeSelectUi;
        if (rehabUi.trainingBackButton != null)
        {
            UnityEventTools.AddPersistentListener(rehabUi.trainingBackButton.onClick, modeSelectUi.ShowTrainingSelectPanel);
        }

        var mrManager = managers.AddComponent<RehabMixedRealityManager>();
        mrManager.targetCamera = mainCamera;
        mrManager.enableOnStart = true;
        mrManager.enableVideoSeeThrough = true;
        mrManager.configureTransparentCamera = true;
        mrManager.suppressBackgroundVisuals = true;

        var backgroundSuppressor = managers.AddComponent<MrBackgroundVisualSuppressor>();
        backgroundSuppressor.hideAllEnvironmentRenderers = true;
        backgroundSuppressor.hideAllRoomSensingRenderers = true;
        backgroundSuppressor.scanIntervalSeconds = 0.15f;

        SetupPicoRoomSensingManagers(managers.transform);

        EditorUtility.SetDirty(rehabRoot);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);
        EditorSceneManager.SaveScene(scene, RehabScenePath);
    }

    private static VirtualCoachController BuildVirtualCoach(Transform parent, Transform hmd)
    {
        var coach = new GameObject("VirtualCoach");
        coach.transform.SetParent(parent, false);
        coach.transform.localPosition = new Vector3(0f, 0f, 2f);
        coach.transform.localRotation = Quaternion.identity;

        var binder = coach.AddComponent<CoachAnimationBinder>();
        binder.SetDefaultMovementBindings();

        var controller = coach.AddComponent<VirtualCoachController>();
        controller.userHeadTransform = hmd;
        controller.coachRoot = coach.transform;
        controller.animationBinder = binder;
        controller.defaultMovementPlaybackState = CoachPlaybackState.Demonstration;
        controller.playbackState = CoachPlaybackState.Idle;
        controller.preferredDistanceMeters = 2f;
        controller.minDistanceMeters = 1.8f;
        controller.maxDistanceMeters = 2.2f;
        controller.floorY = 0f;
        controller.placeInFrontOnStart = true;
        controller.useComfortFollow = true;
        controller.followYawThresholdDegrees = 35f;
        controller.followPositionThresholdMeters = 0.8f;
        controller.followSmoothTime = 0.35f;
        controller.maxFollowSpeedMetersPerSecond = 1.25f;
        controller.followRotationSlerpSpeed = 4f;
        controller.autoCreatePlaceholderCue = false;

        EditorUtility.SetDirty(coach);
        return controller;
    }

    private static GameObject BuildEntryCanvas(UnifiedEntryMenu menu, Transform cameraTransform)
    {
        var canvasGo = CreateWorldCanvas("MainEntryCanvas", cameraTransform, new Vector3(0f, 1.45f, ElderCareUiTheme.MainEntryDistanceMeters), ElderCareUiTheme.MainEntryCanvasSize);
        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<ElderCareRoundedPanel>();
        panelImage.cornerRadius = 44f;
        panelImage.cornerSegments = 12;
        panelImage.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.08f), 1f);
        panelImage.raycastTarget = false;
        var panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = WithAlpha(ElderCareUiTheme.PanelStroke, 0.72f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        SendToBack(CreateEntryDivider(panel.transform, "PanelInnerGlow", Vector2.zero, new Vector2(640f, 390f), WithAlpha(ElderCareUiTheme.Cyan, 0.055f), 34f));
        SendToBack(CreateEntryDivider(panel.transform, "AmbientLineTop", new Vector2(0f, 146f), new Vector2(606f, 3f), WithAlpha(ElderCareUiTheme.Cyan, 0.2f), 2f));
        SendToBack(CreateEntryDivider(panel.transform, "AmbientLineBottom", new Vector2(0f, -152f), new Vector2(606f, 3f), WithAlpha(ElderCareUiTheme.Cyan, 0.13f), 2f));
        SendToBack(CreateEntryDivider(panel.transform, "AmbientLineLeft", new Vector2(-322f, -8f), new Vector2(3f, 300f), WithAlpha(ElderCareUiTheme.Cyan, 0.14f), 2f));
        SendToBack(CreateEntryDivider(panel.transform, "AmbientLineRight", new Vector2(322f, -8f), new Vector2(3f, 300f), WithAlpha(ElderCareUiTheme.Green, 0.13f), 2f));

        CreateEntryStar(panel.transform, "StarA", new Vector2(-260f, 118f), 8f, 0.42f);
        CreateEntryStar(panel.transform, "StarB", new Vector2(260f, 112f), 7f, 0.34f);
        CreateEntryStar(panel.transform, "StarC", new Vector2(-246f, -132f), 6f, 0.28f);
        CreateEntryStar(panel.transform, "StarD", new Vector2(262f, -128f), 8f, 0.36f);

        var title = CreateText(panel.transform, "Title", "VR康养服务", 58f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 214f), new Vector2(660f, 72f));
        title.characterSpacing = 6f;
        title.color = ElderCareUiTheme.TextPrimary;

        var subtitle = CreateText(panel.transform, "Subtitle", "PICO MR 康养交互系统", 28f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 154f), new Vector2(620f, 40f));
        subtitle.characterSpacing = 2f;
        subtitle.color = ElderCareUiTheme.TextSecondary;
        subtitle.raycastTarget = false;

        CreateEntryDivider(panel.transform, "TitleDivider", new Vector2(0f, 124f), new Vector2(420f, 4f), WithAlpha(ElderCareUiTheme.Cyan, 0.48f), 3f);

        CreateEntryModuleCard(
            panel.transform,
            "Module_HealthGame",
            "健康游戏",
            "乒乓球训练 · 速度可调",
            ElderCareIconType.Gamepad,
            new Vector2(-152f, 40f),
            new Vector2(292f, 142f),
            ElderCareUiTheme.Blue,
            true,
            menu.LoadPingPong,
            0.05f);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Rehab",
            "康复运动",
            "太极拳、八段锦养生功法",
            ElderCareIconType.Heart,
            new Vector2(152f, 40f),
            new Vector2(292f, 142f),
            ElderCareUiTheme.Green,
            true,
            menu.LoadRehab,
            0.12f);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Travel",
            "VR旅游",
            "长城、故宫名胜古迹",
            ElderCareIconType.MapPin,
            new Vector2(-152f, -118f),
            new Vector2(292f, 142f),
            ElderCareUiTheme.Violet,
            false,
            null,
            0.19f);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Video",
            "场景视频",
            "VR看房、生活场景体验",
            ElderCareIconType.Video,
            new Vector2(152f, -118f),
            new Vector2(292f, 142f),
            ElderCareUiTheme.Orange,
            false,
            null,
            0.26f);

        var footer = CreateText(panel.transform, "FooterHint", "对准卡片并按下扳机键进入功能", ElderCareUiTheme.BodySmall, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -218f), new Vector2(606f, 58f));
        footer.color = ElderCareUiTheme.TextSecondary;

        return canvasGo;
    }

    private static Button CreateEntryModuleCard(
        Transform parent,
        string name,
        string title,
        string description,
        ElderCareIconType iconType,
        Vector2 anchoredPosition,
        Vector2 size,
        Color baseColor,
        bool enabled,
        UnityEngine.Events.UnityAction onClick,
        float entranceDelay)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var compact = size.y <= 150f;
        var iconY = compact ? 36f : 52f;
        var iconHaloSize = compact ? 62f : 88f;
        var iconFont = compact ? 42f : 58f;
        var titleFont = compact ? 30f : 42f;
        var titleY = compact ? -20f : -30f;
        var descriptionFont = compact ? 18f : 23f;
        var descriptionY = compact ? -54f : -80f;
        var descriptionHeight = compact ? 34f : 50f;

        var glow = CreateUiObject("Glow", go.transform);
        var glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = size + (compact ? new Vector2(28f, 28f) : new Vector2(50f, 50f));
        var glowImage = glow.AddComponent<ElderCareRoundedPanel>();
        glowImage.cornerRadius = compact ? 28f : 40f;
        glowImage.cornerSegments = 10;
        glowImage.color = WithAlpha(baseColor, enabled ? 0.11f : 0.035f);
        glowImage.raycastTarget = false;
        glowImage.transform.SetAsFirstSibling();

        var panel = go.AddComponent<ElderCareRoundedPanel>();
        panel.cornerRadius = compact ? 24f : 34f;
        panel.cornerSegments = 10;
        panel.color = enabled
            ? WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, baseColor, 0.42f), 1f)
            : WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, baseColor, 0.18f), 0.78f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = enabled ? WithAlpha(baseColor, 0.66f) : WithAlpha(ElderCareUiTheme.PanelStroke, 0.3f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = panel;
        button.interactable = enabled;
        button.transition = Selectable.Transition.None;

        if (enabled && onClick != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        var edge = CreateEntryDivider(go.transform, "TopHighlight", new Vector2(0f, size.y * 0.5f - 7f), new Vector2(size.x - 42f, 4f), WithAlpha(ElderCareUiTheme.Cyan, enabled ? 0.28f : 0.12f), 3f);
        CreateEntryDivider(go.transform, "BottomTrace", new Vector2(0f, -size.y * 0.5f + 12f), new Vector2(size.x - 86f, 3f), WithAlpha(baseColor, enabled ? 0.3f : 0.12f), 2f);
        CreateEntryDivider(go.transform, "SideAccent", new Vector2(-size.x * 0.5f + 10f, 0f), new Vector2(4f, size.y - 48f), WithAlpha(baseColor, enabled ? 0.36f : 0.14f), 4f);

        SendToBack(CreateEntryDivider(go.transform, "IconHalo", new Vector2(0f, iconY), new Vector2(iconHaloSize, iconHaloSize), WithAlpha(baseColor, enabled ? 0.14f : 0.06f), iconHaloSize * 0.5f));
        var icon = CreateText(go.transform, "Icon", GetEntryIconText(iconType), iconFont, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, iconY + 1f), new Vector2(iconHaloSize + 20f, iconHaloSize));
        icon.color = enabled ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.64f);
        icon.raycastTarget = false;

        var cardTitle = CreateText(go.transform, "Title", title, titleFont, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, titleY), new Vector2(size.x - 30f, compact ? 40f : 58f));
        cardTitle.color = enabled ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.72f);
        cardTitle.raycastTarget = false;

        var cardDescription = CreateText(go.transform, "Description", description, descriptionFont, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, descriptionY), new Vector2(size.x - 36f, descriptionHeight));
        cardDescription.color = enabled ? ElderCareUiTheme.TextSecondary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.64f);
        cardDescription.lineSpacing = 6f;
        cardDescription.raycastTarget = false;

        if (!enabled)
        {
            CreateEntryDivider(go.transform, "StatusBadgePanel", new Vector2(size.x * 0.5f - 54f, size.y * 0.5f - 24f), new Vector2(84f, 32f), WithAlpha(ElderCareUiTheme.PanelStrong, 0.92f), 16f);
            var badge = CreateText(go.transform, "StatusBadge", "待接入", ElderCareUiTheme.Debug, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(size.x * 0.5f - 54f, size.y * 0.5f - 24f), new Vector2(84f, 32f));
            badge.color = WithAlpha(ElderCareUiTheme.TextPrimary, 0.78f);
            badge.raycastTarget = false;
        }

        var motion = go.AddComponent<TechModuleCardMotion>();
        motion.cardTransform = rect;
        motion.canvasGroup = go.AddComponent<CanvasGroup>();
        motion.cardGraphic = panel;
        motion.glowGraphic = glowImage;
        motion.edgeGraphic = edge;
        motion.interactable = enabled;
        motion.entranceDelay = entranceDelay;
        motion.normalColor = panel.color;
        motion.hoverColor = enabled ? WithAlpha(Color.Lerp(panel.color, baseColor, 0.32f), 0.98f) : panel.color;
        motion.pressedColor = Color.Lerp(panel.color, Color.black, 0.18f);
        motion.glowColor = WithAlpha(baseColor, enabled ? 0.22f : 0.06f);
        motion.edgeColor = WithAlpha(ElderCareUiTheme.Cyan, enabled ? 0.36f : 0.14f);
        motion.hoverScale = enabled ? ElderCareUiTheme.HoverScale : 1f;
        motion.pressedScale = enabled ? ElderCareUiTheme.PressedScale : 1f;

        return button;
    }

    private static string GetEntryIconText(ElderCareIconType iconType)
    {
        switch (iconType)
        {
            case ElderCareIconType.Gamepad:
                return "游";
            case ElderCareIconType.Heart:
                return "康";
            case ElderCareIconType.MapPin:
                return "旅";
            case ElderCareIconType.Video:
                return "影";
            default:
                return "·";
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Graphic CreateEntryDivider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, float cornerRadius = 2f)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var panel = go.AddComponent<ElderCareRoundedPanel>();
        panel.cornerRadius = Mathf.Min(cornerRadius, Mathf.Min(size.x, size.y) * 0.5f);
        panel.cornerSegments = 8;
        panel.color = color;
        panel.raycastTarget = false;
        return panel;
    }

    private static void CreateEntryStar(Transform parent, string name, Vector2 anchoredPosition, float size, float alpha)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
        var image = go.AddComponent<ElderCareRoundedPanel>();
        image.cornerRadius = size * 0.5f;
        image.cornerSegments = 8;
        image.color = WithAlpha(ElderCareUiTheme.Cyan, alpha);
        image.raycastTarget = false;
    }

    private static GameObject BuildTrainingArea(Transform parent)
    {
        var root = new GameObject("TrainingArea");
        root.transform.SetParent(parent, false);

        var circle = new GameObject("TrainingCircle");
        circle.transform.SetParent(root.transform, false);
        circle.transform.localPosition = Vector3.zero;

        var renderer = circle.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.loop = true;
        renderer.widthMultiplier = 0.025f;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 4;
        renderer.sharedMaterial = CreateOrLoadMaterial("RehabTrainingCircle", new Color(0.1f, 0.95f, 0.72f, 0.85f));

        const int segments = 96;
        const float radius = 0.6f;
        renderer.positionCount = segments;
        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            renderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.015f, Mathf.Sin(angle) * radius));
        }

        return root;
    }

    private static RehabTrainingUi BuildRehabPromptCanvas(
        Transform parent,
        Camera mainCamera,
        ModuleHomeMenu homeMenu)
    {
        var canvasGo = CreateWorldCanvas("RehabPromptCanvas", null, new Vector3(0f, 1.65f, 2.35f), ElderCareUiTheme.RehabCanvasSize);
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.worldCamera = mainCamera;

        var ui = new RehabTrainingUi
        {
            canvas = canvasGo,
            mainMenuPanel = CreatePanel(canvasGo.transform, "MainMenuPanel"),
            rehabTrainingSelectPanel = CreatePanel(canvasGo.transform, "RehabTrainingSelectPanel"),
            rehabTrainingPanel = CreatePanel(canvasGo.transform, "RehabTrainingPanel"),
            trainingResultPanel = CreatePanel(canvasGo.transform, "TrainingResultPanel")
        };

        CreateText(ui.mainMenuPanel.transform, "Title", "康复运动", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 104f), new Vector2(800f, 80f));
        ui.rehabButton = CreateButton(ui.mainMenuPanel.transform, "RehabButton", "康复运动", new Vector2(0f, -36f), new Vector2(400f, 88f));

        var selectTitle = CreateText(ui.rehabTrainingSelectPanel.transform, "Title", "请选择康复训练类型", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 154f), new Vector2(820f, 74f));
        selectTitle.color = ElderCareUiTheme.TextPrimary;
        CreateEntryDivider(ui.rehabTrainingSelectPanel.transform, "TitleTrace", new Vector2(0f, 112f), new Vector2(420f, 4f), WithAlpha(ElderCareUiTheme.Cyan, 0.38f), 3f);
        ui.baduanjinButton = CreateButton(ui.rehabTrainingSelectPanel.transform, "BaduanjinButton", "八段锦训练", new Vector2(-152f, 30f), new Vector2(292f, 142f));
        ui.taiChiButton = CreateButton(ui.rehabTrainingSelectPanel.transform, "TaiChiButton", "太极训练", new Vector2(152f, 30f), new Vector2(292f, 142f));
        ui.backButton = CreateButton(ui.rehabTrainingSelectPanel.transform, "BackButton", "返回", new Vector2(0f, -146f), new Vector2(606f, 58f));

        ui.title = CreateText(ui.rehabTrainingPanel.transform, "MovementTitle", "八段锦：双手托天理三焦", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 158f), new Vector2(620f, 58f));
        ui.status = CreateText(ui.rehabTrainingPanel.transform, "StatusText", "请准备：双手托天理三焦", ElderCareUiTheme.Body, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 104f), new Vector2(620f, 42f));
        ui.status.color = ElderCareUiTheme.TextSecondary;
        ui.timer = CreateRehabDataBlock(ui.rehabTrainingPanel.transform, "TimerBlock", "倒计时", "剩余 05:00", new Vector2(-152f, -4f), new Vector2(292f, 142f), ElderCareUiTheme.Cyan);
        ui.completion = CreateRehabDataBlock(ui.rehabTrainingPanel.transform, "CompletionBlock", "完成度", "完成度 0%", new Vector2(152f, -4f), new Vector2(292f, 142f), ElderCareUiTheme.Green);
        SendToBack(CreateEntryDivider(ui.rehabTrainingPanel.transform, "SafetyPanel", new Vector2(0f, -116f), new Vector2(606f, 58f), WithAlpha(ElderCareUiTheme.Gold, 0.22f), 20f));
        ui.safety = CreateText(ui.rehabTrainingPanel.transform, "SafetyPromptText", "保持舒适幅度，准备开始", ElderCareUiTheme.BodySmall, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -116f), new Vector2(586f, 52f));
        ui.safety.color = WithAlpha(ElderCareUiTheme.Gold, 0.96f);
        ui.debug = CreateText(ui.rehabTrainingPanel.transform, "DebugText", "距中心 0.00m", ElderCareUiTheme.Debug, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -174f), new Vector2(260f, 36f));
        ui.debug.color = WithAlpha(ElderCareUiTheme.TextMuted, 0.42f);
        ui.debug.enableWordWrapping = false;
        ui.debug.overflowMode = TextOverflowModes.Ellipsis;
        ui.startButton = CreateButton(ui.rehabTrainingPanel.transform, "StartButton", "开始", new Vector2(-232f, -174f), new Vector2(186f, 82f));
        ui.trainingBackButton = CreateButton(ui.rehabTrainingPanel.transform, "HomeButton", "返回", new Vector2(232f, -174f), new Vector2(186f, 82f));

        CreateText(ui.trainingResultPanel.transform, "Title", "训练结果", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 128f), new Vector2(800f, 76f));
        var summary = CreateText(ui.trainingResultPanel.transform, "Summary", "训练结束后结果会自动保存到本机", ElderCareUiTheme.Body, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 30f), new Vector2(560f, 100f));
        summary.color = ElderCareUiTheme.TextSecondary;
        ui.resultBackButton = CreateButton(ui.trainingResultPanel.transform, "BackButton", "返回选择", new Vector2(0f, -112f), new Vector2(292f, 82f));

        ui.mainMenuPanel.SetActive(false);
        ui.rehabTrainingSelectPanel.SetActive(true);
        ui.rehabTrainingPanel.SetActive(false);
        ui.trainingResultPanel.SetActive(false);
        return ui;
    }

    private static TMP_Text CreateRehabDataBlock(Transform parent, string name, string label, string value, Vector2 anchoredPosition, Vector2 size, Color accentColor)
    {
        var block = CreateUiObject(name, parent);
        var rect = block.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var background = block.AddComponent<ElderCareRoundedPanel>();
        background.cornerRadius = 22f;
        background.cornerSegments = 10;
        background.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accentColor, 0.28f), 1f);
        background.raycastTarget = false;
        background.transform.SetAsFirstSibling();

        var outline = block.AddComponent<Outline>();
        outline.effectColor = WithAlpha(accentColor, 0.36f);
        outline.effectDistance = new Vector2(2f, -2f);

        CreateEntryDivider(block.transform, "TopTrace", new Vector2(0f, size.y * 0.5f - 7f), new Vector2(size.x - 58f, 3f), WithAlpha(accentColor, 0.28f), 2f);
        var labelText = CreateText(block.transform, "Label", label, ElderCareUiTheme.BodySmall, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 24f), new Vector2(size.x - 34f, 32f));
        labelText.color = ElderCareUiTheme.TextSecondary;
        var valueText = CreateText(block.transform, "Value", value, ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -18f), new Vector2(size.x - 34f, 52f));
        valueText.color = ElderCareUiTheme.TextPrimary;
        return valueText;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        var panel = CreateUiObject(name, parent);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<ElderCareRoundedPanel>();
        panelImage.cornerRadius = 32f;
        panelImage.cornerSegments = 12;
        panelImage.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Green, 0.22f), 1f);
        panelImage.raycastTarget = false;
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = WithAlpha(ElderCareUiTheme.Cyan, 0.42f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        SendToBack(CreateEntryDivider(panel.transform, "PanelTopTrace", new Vector2(0f, 204f), new Vector2(700f, 3f), WithAlpha(ElderCareUiTheme.Cyan, 0.14f), 2f));
        SendToBack(CreateEntryDivider(panel.transform, "PanelBottomTrace", new Vector2(0f, -206f), new Vector2(560f, 2f), WithAlpha(ElderCareUiTheme.Green, 0.12f), 2f));
        return panel;
    }

    private static Graphic SendToBack(Graphic graphic)
    {
        if (graphic != null)
        {
            graphic.transform.SetAsFirstSibling();
        }

        return graphic;
    }

    private static GameObject CreateXrOrigin()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
        GameObject root;
        if (prefab != null)
        {
            root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            root = new GameObject("XR Origin (XR Rig)");
            root.AddComponent<XROrigin>();
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(root.transform, false);
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraOffset.transform, false);
            cameraGo.AddComponent<Camera>();
            new GameObject("Left Controller").transform.SetParent(cameraOffset.transform, false);
            new GameObject("Right Controller").transform.SetParent(cameraOffset.transform, false);
        }

        if (root == null) return null;

        root.name = "[Building Block] PICO Controller Tracking XR Origin (XR Rig)";
        var pxrManager = EnsureComponent<PXR_Manager>(root);
        pxrManager.openMRC = true;
        pxrManager.useRecommendedAntiAliasingLevel = true;

        var origin = root.GetComponent<XROrigin>();
        var camera = FindChildByName(root.transform, "Main Camera")?.GetComponent<Camera>();
            var cameraOffsetTransform = FindChildByName(root.transform, "Camera Offset");
            if (origin != null)
            {
                origin.Camera = camera;
                if (cameraOffsetTransform != null)
                {
                    origin.CameraFloorOffsetObject = cameraOffsetTransform.gameObject;
                }
            }

        if (camera != null)
        {
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            var clear = Color.black;
            clear.a = 0f;
            camera.backgroundColor = clear;
        }

        return root;
    }

    private static GameObject CreateWorldCanvas(string name, Transform cameraTransform, Vector3 fallbackPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() ?? FindMainCamera() : FindMainCamera();
        go.transform.localScale = Vector3.one * 0.002f;

        if (cameraTransform != null)
        {
            var forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            go.transform.position = cameraTransform.position + forward * 2.2f + Vector3.up * 0.1f;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        else
        {
            go.transform.position = fallbackPosition;
            go.transform.rotation = Quaternion.identity;
        }

        return go;
    }

    private static GameObject CreateUiRoot(string name, Transform parent)
    {
        var root = new GameObject(name);
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }

    private static void AttachUiToRoot(Transform uiTransform, Transform uiRoot)
    {
        if (uiTransform == null || uiRoot == null) return;

        uiTransform.SetParent(uiRoot, false);
        uiTransform.localPosition = Vector3.zero;
        uiTransform.localRotation = Quaternion.identity;
    }

    private static ComfortWorldSpaceUIPlacer ConfigureComfortUiPlacer(GameObject host, Transform headTransform, Transform uiRoot, float distanceMeters)
    {
        var placer = EnsureComponent<ComfortWorldSpaceUIPlacer>(host);
        placer.headTransform = headTransform;
        placer.uiRoot = uiRoot;
        placer.distanceMeters = distanceMeters;
        placer.hmdHeightOffsetMeters = -0.1f;
        placer.placeOnStart = true;
        placer.placeOnEnable = false;
        placer.recenterDuringStartup = true;
        placer.startupRecenterSeconds = 1.25f;
        placer.startupRecenterFrames = 18;
        placer.enableRayDrag = true;
        placer.enableThumbstickNavigation = true;
        placer.invertThumbstickHorizontal = false;
        placer.comfortFollowEnabled = false;
        placer.followYawThresholdDegrees = 35f;
        placer.followPositionThresholdMeters = 0.8f;
        placer.followSmoothTime = 0.35f;
        placer.followRotationSlerpSpeed = 4f;
        placer.maxFollowSpeedMetersPerSecond = 1.25f;
        return placer;
    }

    private static void AddReturnHomePanelToPingPongSceneInternal()
    {
        if (!System.IO.File.Exists(PingPongScenePath))
        {
            Debug.LogWarning("PingPong scene was not found at " + PingPongScenePath);
            return;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        var pingPongScene = EditorSceneManager.OpenScene(PingPongScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(pingPongScene);

        try
        {
            DestroySceneObjectIfFound(pingPongScene, "PingPongHomeCanvas");
            DestroySceneObjectIfFound(pingPongScene, "PingPongHomeMenu");
            DestroySceneObjectIfFound(pingPongScene, "PingPongHomeUIRoot");

            EditorSceneManager.SaveScene(pingPongScene);
        }
        finally
        {
            if (previousActiveScene.IsValid())
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            EditorSceneManager.CloseScene(pingPongScene, true);
        }
    }

    private static GameObject BuildModuleHomeCanvas(string canvasName, ModuleHomeMenu homeMenu, Transform cameraTransform, Vector3 fallbackPosition)
    {
        var canvasGo = CreateWorldCanvas(canvasName, cameraTransform, fallbackPosition, new Vector2(300f, 120f));
        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<ElderCareRoundedPanel>();
        panelImage.cornerRadius = 22f;
        panelImage.cornerSegments = 10;
        panelImage.color = new Color(0.03f, 0.05f, 0.06f, 0.78f);

        var homeButton = CreateButton(panel.transform, "HomeButton", "返回主页", Vector2.zero, new Vector2(240f, 72f));
        UnityEventTools.AddPersistentListener(homeButton.onClick, homeMenu.LoadMainEntry);
        return canvasGo;
    }

    private static void SetupPicoRoomSensingManagers(Transform parent)
    {
        var sensingRoot = new GameObject("MRSpaceSensing");
        sensingRoot.transform.SetParent(parent, false);
        sensingRoot.AddComponent<RehabRoomSensingStarter>();

        var sensingMaterial = CreateOrLoadMaterial("RehabRoomSensingHidden", new Color(0.25f, 0.5f, 1f, 0.04f));
        var planeTemplate = SetupRoomSensingTemplate(sensingRoot.transform, "MRDetectedPlaneTemplate", sensingMaterial);
        var planeManager = sensingRoot.AddComponent<PXR_PlaneDetectionManager>();
        planeManager.planePrefab = planeTemplate;

        var meshTemplate = SetupRoomSensingTemplate(sensingRoot.transform, "MRSpatialMeshTemplate", sensingMaterial);
        var meshManager = sensingRoot.AddComponent<PXR_SpatialMeshManager>();
        meshManager.meshPrefab = meshTemplate;

        EditorUtility.SetDirty(sensingRoot);
    }

    private static GameObject SetupRoomSensingTemplate(Transform parent, string name, Material material)
    {
        var template = new GameObject(name);
        template.transform.SetParent(parent, false);
        template.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.identity;
        template.transform.localScale = Vector3.one;

        template.AddComponent<MeshFilter>();
        var renderer = template.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.enabled = false;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        template.AddComponent<MeshCollider>();
        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var label = go.AddComponent<TextMeshProUGUI>();
        var fontAsset = GetRehabFontAsset();
        if (fontAsset != null)
        {
            label.font = fontAsset;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = ElderCareUiTheme.TextPrimary;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, ElderCareUiTheme.MinButtonHeightForElderly));
        var image = go.AddComponent<ElderCareRoundedPanel>();
        image.cornerRadius = 22f;
        image.cornerSegments = 10;
        image.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.36f), 1f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = WithAlpha(ElderCareUiTheme.Cyan, 0.48f);
        outline.effectDistance = new Vector2(2f, -2f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = WithAlpha(Color.Lerp(Color.white, ElderCareUiTheme.Cyan, 0.18f), 1f);
        colors.pressedColor = WithAlpha(Color.Lerp(Color.white, ElderCareUiTheme.Green, 0.22f), 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = WithAlpha(Color.white, ElderCareUiTheme.DisabledAlpha);
        button.colors = colors;

        var label = CreateText(go.transform, "Label", text, ElderCareUiTheme.Button, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, rect.sizeDelta);
        label.color = ElderCareUiTheme.TextPrimary;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void EnsureXrInteractionSupport()
    {
        EnsureInputActionManager();
        EnsureXrInteractionManager();
        EnsureEventSystem();
    }

    private static void EnsureInputActionManager()
    {
        var inputActionManager = Object.FindObjectOfType<InputActionManager>();
        if (inputActionManager == null)
        {
            var go = new GameObject("Input Action Manager");
            inputActionManager = go.AddComponent<InputActionManager>();
        }

        var actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriDefaultInputActionsPath);
        if (actionAsset == null)
        {
            Debug.LogWarning("Could not find XRI default input actions at " + XriDefaultInputActionsPath);
            return;
        }

        if (inputActionManager.actionAssets == null)
        {
            inputActionManager.actionAssets = new System.Collections.Generic.List<InputActionAsset>();
        }

        if (!inputActionManager.actionAssets.Contains(actionAsset))
        {
            inputActionManager.actionAssets.Add(actionAsset);
        }

        EditorUtility.SetDirty(inputActionManager);
    }

    private static void EnsureXrInteractionManager()
    {
        if (Object.FindObjectOfType<XRInteractionManager>() != null) return;

        var go = new GameObject("XR Interaction Manager");
        go.AddComponent<XRInteractionManager>();
        EditorUtility.SetDirty(go);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        var xrUiInputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (xrUiInputModule == null)
        {
            xrUiInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        ApplyXrUiInputModulePreset(xrUiInputModule);
        EditorUtility.SetDirty(eventSystem);
    }

    private static void ApplyXrUiInputModulePreset(XRUIInputModule inputModule)
    {
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(XrUiInputModulePresetPath);
        if (preset == null)
        {
            Debug.LogWarning("Could not find XRI UI input module preset at " + XrUiInputModulePresetPath);
            return;
        }

        if (!preset.CanBeAppliedTo(inputModule))
        {
            Debug.LogWarning("XRI UI input module preset could not be applied to " + inputModule.name);
            return;
        }

        preset.ApplyTo(inputModule);
        EditorUtility.SetDirty(inputModule);
    }

    private static void EnsureLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainEntryScenePath, true),
            new EditorBuildSettingsScene(DeviceTestScenePath, false),
            new EditorBuildSettingsScene(PingPongScenePath, true),
            new EditorBuildSettingsScene(RehabScenePath, true)
        };
    }

    private static void ConfigureMixedRealityProjectSettings()
    {
        var config = PXR_ProjectSetting.GetProjectConfig();
        if (config == null) return;

        config.openMRC = true;
        config.videoSeeThrough = true;
        config.spatialAnchor = true;
        config.sceneCapture = true;
        config.spatialMesh = true;
        config.planeDetection = true;
        config.mrSafeguard = true;
        config.meshLod = PxrMeshLod.Low;
        PXR_ProjectSetting.SaveAssets();
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath("Assets/_Project");
        EnsureFolderPath("Assets/_Project/Scenes");
        EnsureFolderPath("Assets/_Project/Materials");
        EnsureFolderPath(MaterialRoot);
        EnsureFolderPath("Assets/_Project/Fonts");
        EnsureFolderPath(FontRoot);
    }

    private static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderPath(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static TMP_FontAsset GetRehabFontAsset()
    {
        if (rehabFontAsset != null) return rehabFontAsset;

        rehabFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RehabChineseFontAssetPath);
        if (rehabFontAsset != null)
        {
            if (HasSavedFontAtlasAndMaterial(rehabFontAsset))
            {
                return rehabFontAsset;
            }

            AssetDatabase.DeleteAsset(RehabChineseFontAssetPath);
            rehabFontAsset = null;
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(RehabChineseFontSourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning("Could not find rehab Chinese font at " + RehabChineseFontSourcePath + ". TextMeshPro will use the project default font.");
            return null;
        }

        rehabFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        rehabFontAsset.name = "RehabChineseTMP";
        rehabFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        rehabFontAsset.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(rehabFontAsset, RehabChineseFontAssetPath);
        SaveGeneratedFontSubAssets(rehabFontAsset);
        EditorUtility.SetDirty(rehabFontAsset);
        return rehabFontAsset;
    }

    private static bool HasSavedFontAtlasAndMaterial(TMP_FontAsset fontAsset)
    {
        return fontAsset.material != null &&
               fontAsset.atlasTextures != null &&
               fontAsset.atlasTextures.Length > 0 &&
               fontAsset.atlasTextures[0] != null &&
               fontAsset.atlasWidth == 1024 &&
               fontAsset.atlasHeight == 1024;
    }

    private static void SaveGeneratedFontSubAssets(TMP_FontAsset fontAsset)
    {
        var atlasTextures = fontAsset.atlasTextures;
        if (atlasTextures != null && atlasTextures.Length > 0 && atlasTextures[0] != null)
        {
            atlasTextures[0].name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTextures[0], fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color)
    {
        var path = MaterialRoot + "/" + materialName + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        var shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Camera FindMainCamera()
    {
        var camera = Camera.main;
        if (camera != null) return camera;

        var cameras = Object.FindObjectsOfType<Camera>(true);
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static Camera FindMainCameraInScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (var j = 0; j < cameras.Length; j++)
            {
                if (cameras[j] != null && cameras[j].CompareTag("MainCamera"))
                {
                    return cameras[j];
                }
            }
        }

        for (var i = 0; i < roots.Length; i++)
        {
            var camera = roots[i].GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                return camera;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var found = FindChildByName(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
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

    private static void RemoveSceneComponentsInScene<T>(Scene scene) where T : Component
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var components = roots[i].GetComponentsInChildren<T>(true);
            for (var j = 0; j < components.Length; j++)
            {
                if (components[j] != null)
                {
                    Object.DestroyImmediate(components[j]);
                }
            }
        }
    }

    private static void DestroySceneObjectIfFound(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var found = FindChildByName(roots[i].transform, objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found.gameObject);
                return;
            }
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindChildByName(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static bool EnsureEditMode()
    {
        if (!Application.isPlaying) return true;
        Debug.LogWarning("Rehab scene builder can only run in edit mode.");
        return false;
    }
}
