using PicoElderCare.Rehab;
using PicoElderCare.Rehab.Tracking;
using PicoElderCare.Rehab.Tracking.Pico;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking;
using PicoElderCare.HealthGame;
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
    private const string HealthGameMenuScenePath = "Assets/_Project/Scenes/02_HealthGameMenu.unity";
    private const string PingPongScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string ArcheryTrainingScenePath = "Assets/_Project/Scenes/03_ArcheryTraining.unity";
    private const string DartsTrainingScenePath = "Assets/_Project/Scenes/04_DartsTraining.unity";
    private const string RehabScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string MaterialRoot = "Assets/_Project/Materials/Rehab";
    private const string FontRoot = "Assets/_Project/Fonts/Rehab";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string XrUiInputModulePresetPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Presets/XRI Default XR UI Input Module.preset";
    private const string XriDefaultInputActionsPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/XRI Default Input Actions.inputactions";
    private const string RehabChineseFontSourcePath = FontRoot + "/NotoSansSC-VF.ttf";
    private const string RehabChineseFontAssetPath = MaterialRoot + "/RehabChineseTMP.asset";
    private const string HealthSportSpriteRoot = "Assets/Resources/HealthSportsIcons/CoreSports/GeneratedSprites/";
    private const string HealthUiSpriteRoot = "Assets/Resources/UiIcons/Tabler/UnityWarm/GeneratedSprites/";
    private const float MainEntryPlacementDistanceMeters = 1.35f;
    private const float MainEntryHmdHeightOffsetMeters = -0.15f;
    private const float MainEntryMinWorldHeight = 1.10f;
    private const float MainEntryMaxWorldHeight = 1.55f;
    private const float MainEntryStartupRecenterSeconds = 1.25f;
    private const int MainEntryStartupRecenterFrames = 18;

    private static TMP_FontAsset rehabFontAsset;

    private struct RehabTrainingUi
    {
        public GameObject canvas;
        public GameObject mainMenuPanel;
        public GameObject selectionPanelRoot;
        public GameObject rehabTrainingSelectPanel;
        public GameObject trainingLayoutAnchor;
        public GameObject trainingFunctionPanelRoot;
        public GameObject rehabTrainingPanel;
        public GameObject resultPanelRoot;
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
        public Button trainingRecenterButton;
        public Button resultBackButton;
    }
    [MenuItem("Tools/PICO ElderCare/Build Main Entry Scene")]
    public static void BuildMainEntryScene()
    {
        if (!EnsureEditMode()) return;
        SynchronizeAuthoredMainEntryScene();
    }

    [MenuItem("Tools/PICO ElderCare/Build Health Game Menu Scene")]
    public static void BuildHealthGameMenuScene()
    {
        if (!EnsureEditMode()) return;
        SynchronizeAuthoredHealthGameMenuScene();
    }

    [MenuItem("Tools/PICO ElderCare/Build MR Rehab Main Scene")]
    public static void BuildMrRehabMainScene()
    {
        if (!EnsureEditMode()) return;
        SynchronizeAuthoredRehabScene();
    }

    [MenuItem("Tools/PICO ElderCare/Build Unified MVP Scenes")]
    public static void BuildUnifiedMvpScenes()
    {
        if (!EnsureEditMode()) return;
        SynchronizeAuthoredMainEntryScene();
        SynchronizeAuthoredHealthGameMenuScene();
        SynchronizeAuthoredRehabScene();
    }

    private static void SynchronizeAuthoredMainEntryScene()
    {
        if (!TryOpenAuthoredScene(MainEntryScenePath, "main entry", out var scene)) return;

        var menu = FindSingleSceneComponent<UnifiedEntryMenu>(scene, "UnifiedEntryMenu");
        var placement = FindSingleSceneComponent<RehabPanelPlacementController>(scene, "main-entry RehabPanelPlacementController");
        var placer = FindSingleSceneComponent<ComfortWorldSpaceUIPlacer>(scene, "main-entry ComfortWorldSpaceUIPlacer");
        var canvas = FindSceneGameObject(scene, "MainEntryCanvas");
        var canvasComponent = canvas != null ? canvas.GetComponent<Canvas>() : null;
        var uiRoot = canvas != null ? canvas.transform.parent : null;
        var headTransform = canvasComponent != null && canvasComponent.worldCamera != null
            ? canvasComponent.worldCamera.transform
            : null;
        if (menu == null || placement == null || placer == null || canvas == null ||
            uiRoot == null || headTransform == null ||
            FindSceneGameObject(scene, "Module_HealthGame") == null ||
            FindSceneGameObject(scene, "Module_Rehab") == null)
        {
            Debug.LogError("[RehabSceneBuilder] Main-entry authored baseline is incomplete. No scene changes were saved.");
            return;
        }

        var menuChanged = menu.applyHtmlStyleMainPanel ||
                          menu.recenterPanelsOnEnable ||
                          menu.panelPlacementController != placement ||
                          menu.htmlStyleMainCanvas != canvas.transform;
        var placementChanged = placement.placeOnStart;
        var placerChanged = !placer.enabled ||
                            placer.headTransform != headTransform ||
                            placer.uiRoot != uiRoot ||
                            !Mathf.Approximately(placer.distanceMeters, MainEntryPlacementDistanceMeters) ||
                            !Mathf.Approximately(placer.hmdHeightOffsetMeters, MainEntryHmdHeightOffsetMeters) ||
                            !placer.placeOnStart ||
                            placer.placeOnEnable ||
                            !placer.recenterDuringStartup ||
                            !Mathf.Approximately(placer.startupRecenterSeconds, MainEntryStartupRecenterSeconds) ||
                            placer.startupRecenterFrames != MainEntryStartupRecenterFrames ||
                            placer.usePreferredHeightInsteadOfHeadHeight ||
                            !placer.clampWorldHeight ||
                            !Mathf.Approximately(placer.minWorldHeight, MainEntryMinWorldHeight) ||
                            !Mathf.Approximately(placer.maxWorldHeight, MainEntryMaxWorldHeight) ||
                            placer.comfortFollowEnabled ||
                            placer.enableRayDrag ||
                            placer.enableThumbstickNavigation;

        if (menuChanged)
        {
            Undo.RecordObject(menu, "Synchronize authored main-entry behavior");
            menu.applyHtmlStyleMainPanel = false;
            menu.recenterPanelsOnEnable = false;
            menu.panelPlacementController = placement;
            menu.htmlStyleMainCanvas = canvas.transform;
            EditorUtility.SetDirty(menu);
        }

        if (placementChanged)
        {
            Undo.RecordObject(placement, "Synchronize authored main-entry placement");
            placement.placeOnStart = false;
            EditorUtility.SetDirty(placement);
        }

        if (placerChanged)
        {
            Undo.RecordObject(placer, "Synchronize authored main-entry HMD-relative placement");
            placer.enabled = true;
            placer.headTransform = headTransform;
            placer.uiRoot = uiRoot;
            placer.distanceMeters = MainEntryPlacementDistanceMeters;
            placer.hmdHeightOffsetMeters = MainEntryHmdHeightOffsetMeters;
            placer.placeOnStart = true;
            placer.placeOnEnable = false;
            placer.recenterDuringStartup = true;
            placer.startupRecenterSeconds = MainEntryStartupRecenterSeconds;
            placer.startupRecenterFrames = MainEntryStartupRecenterFrames;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.clampWorldHeight = true;
            placer.minWorldHeight = MainEntryMinWorldHeight;
            placer.maxWorldHeight = MainEntryMaxWorldHeight;
            placer.comfortFollowEnabled = false;
            placer.enableRayDrag = false;
            placer.enableThumbstickNavigation = false;
            EditorUtility.SetDirty(placer);
        }

        var trackingSettingsChanged = SynchronizeMainEntryTrackerSettings(canvas.transform, menu);

        var strokeMigration = MigrateMainEntryNativeStrokes(canvas.transform);
        SaveAuthoredSceneIfChanged(
            scene,
            menuChanged || placementChanged || placerChanged || trackingSettingsChanged || strokeMigration.changed,
            "main entry");
    }

    internal static bool SynchronizeMainEntryTrackerSettings(Transform canvas, UnifiedEntryMenu menu)
    {
        if (canvas == null || menu == null) return false;

        var changed = false;
        var statusPanel = canvas.GetComponent<PicoWristTrackingStatusPanel>();
        if (statusPanel == null)
        {
            statusPanel = canvas.gameObject.AddComponent<PicoWristTrackingStatusPanel>();
            changed = true;
        }

        var existingPanel = canvas.Find("TrackerSettingsPanel");
        var previousEntryMenu = statusPanel.EntryMenu;
        changed |= statusPanel.BuildOrRepairAuthoredPanel(menu);
        if (existingPanel == null && canvas.Find("TrackerSettingsPanel") != null) changed = true;
        if (previousEntryMenu != menu)
        {
            EditorUtility.SetDirty(statusPanel);
            changed = true;
        }

        var settingsTransform = FindChildByName(canvas, "Settings");
        var settingsButton = settingsTransform != null ? settingsTransform.GetComponent<Button>() : null;
        if (settingsButton == null)
        {
            Debug.LogError("[RehabSceneBuilder] Main-entry Settings button is missing; tracker settings were not bound.");
            return changed;
        }

        if (!settingsButton.interactable || !HasPersistentListener(settingsButton, menu, "OpenTrackerSettings"))
        {
            Undo.RecordObject(settingsButton, "Bind tracker settings button");
            settingsButton.interactable = true;
            for (var i = settingsButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(settingsButton.onClick, i);
            }
            settingsButton.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menu.OpenTrackerSettings);
            EditorUtility.SetDirty(settingsButton);
            changed = true;
        }

        var group = settingsTransform.GetComponent<CanvasGroup>();
        if (group != null && (!group.interactable || !group.blocksRaycasts || !Mathf.Approximately(group.alpha, 1f)))
        {
            group.interactable = true;
            group.blocksRaycasts = true;
            group.alpha = 1f;
            EditorUtility.SetDirty(group);
            changed = true;
        }

        var motion = settingsTransform.GetComponent<TechModuleCardMotion>();
        if (motion != null && !motion.interactable)
        {
            motion.interactable = true;
            EditorUtility.SetDirty(motion);
            changed = true;
        }

        return changed;
    }

    private static void SynchronizeAuthoredHealthGameMenuScene()
    {
        if (!TryOpenAuthoredScene(HealthGameMenuScenePath, "health game menu", out var scene)) return;

        var controller = FindSingleSceneComponent<HealthGameMenuController>(scene, "HealthGameMenuController");
        var placer = FindSingleSceneComponent<ComfortWorldSpaceUIPlacer>(scene, "health-menu ComfortWorldSpaceUIPlacer");
        var canvas = FindSceneGameObject(scene, "HealthGameMenuCanvas");
        var menuRoot = FindSceneGameObject(scene, "HealthGameMenuRoot");
        if (controller == null || placer == null || canvas == null || menuRoot == null ||
            FindSceneGameObject(scene, "SportCards") == null ||
            FindSceneGameObject(scene, "PingPongCard") == null ||
            FindSceneGameObject(scene, "ArcheryCard") == null ||
            FindSceneGameObject(scene, "DartsCard") == null ||
            FindSceneGameObject(scene, "BottomDock") == null)
        {
            Debug.LogError("[RehabSceneBuilder] Health-menu authored baseline is incomplete. No scene changes were saved.");
            return;
        }

        var placerChanged = placer.uiRoot != menuRoot.transform ||
                            !Mathf.Approximately(placer.distanceMeters, 2.2f) ||
                            !Mathf.Approximately(placer.hmdHeightOffsetMeters, -0.1f) ||
                            !placer.placeOnStart ||
                            placer.placeOnEnable ||
                            !placer.recenterDuringStartup ||
                            !Mathf.Approximately(placer.startupRecenterSeconds, 1.25f) ||
                            placer.startupRecenterFrames != 18 ||
                            !placer.usePreferredHeightInsteadOfHeadHeight ||
                            !placer.clampWorldHeight ||
                            !Mathf.Approximately(placer.minWorldHeight, 1.25f) ||
                            !Mathf.Approximately(placer.maxWorldHeight, 1.75f) ||
                            placer.comfortFollowEnabled;
        if (placerChanged)
        {
            Undo.RecordObject(placer, "Synchronize authored health-menu placement");
            placer.uiRoot = menuRoot.transform;
            placer.distanceMeters = 2.2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.placeOnStart = true;
            placer.placeOnEnable = false;
            placer.recenterDuringStartup = true;
            placer.startupRecenterSeconds = 1.25f;
            placer.startupRecenterFrames = 18;
            placer.usePreferredHeightInsteadOfHeadHeight = true;
            placer.clampWorldHeight = true;
            placer.minWorldHeight = 1.25f;
            placer.maxWorldHeight = 1.75f;
            placer.comfortFollowEnabled = false;
            EditorUtility.SetDirty(placer);
        }

        var strokeMigration = MigrateSecondaryMenuNativeStrokes(
            menuRoot.transform,
            new[]
            {
                new ChoiceCardStrokeSpec("PingPongCard", ElderCareMenuDesignTokens.Jade, true),
                new ChoiceCardStrokeSpec("ArcheryCard", ElderCareMenuDesignTokens.Amber, false),
                new ChoiceCardStrokeSpec("DartsCard", ElderCareMenuDesignTokens.Coral, false)
            },
            false);
        SaveAuthoredSceneIfChanged(scene, placerChanged || strokeMigration.changed, "health game menu");
    }

    private static void SynchronizeAuthoredRehabScene()
    {
        if (!TryOpenAuthoredScene(RehabScenePath, "rehab", out var scene)) return;

        var modeSelect = FindSingleSceneComponent<RehabModeSelectUI>(scene, "RehabModeSelectUI");
        var placer = FindSingleSceneComponent<ComfortWorldSpaceUIPlacer>(scene, "rehab ComfortWorldSpaceUIPlacer");
        var placement = FindSingleSceneComponent<RehabPanelPlacementController>(scene, "rehab RehabPanelPlacementController");
        if (!ValidateAuthoredRehabBaseline(scene, modeSelect, placer, placement)) return;

        var modeChanged = modeSelect.applyHtmlStylePanels ||
                          !modeSelect.applyTrainingAndResultVisualSkin ||
                          modeSelect.placeUiOnStart ||
                          modeSelect.placeUiOnMainMenuOpen ||
                          !modeSelect.placeUiOnTrainingSelectOpen ||
                          modeSelect.uiPlacer != placer ||
                          modeSelect.panelPlacementController != placement;
        if (modeChanged)
        {
            Undo.RecordObject(modeSelect, "Synchronize authored rehab UI behavior");
            modeSelect.applyHtmlStylePanels = false;
            modeSelect.applyTrainingAndResultVisualSkin = true;
            modeSelect.placeUiOnStart = false;
            modeSelect.placeUiOnMainMenuOpen = false;
            modeSelect.placeUiOnTrainingSelectOpen = true;
            modeSelect.uiPlacer = placer;
            modeSelect.panelPlacementController = placement;
            EditorUtility.SetDirty(modeSelect);
        }

        var placerChanged = placer.uiRoot != modeSelect.SelectionPanelRoot.transform ||
                            !Mathf.Approximately(placer.distanceMeters, 2f) ||
                            !Mathf.Approximately(placer.hmdHeightOffsetMeters, -0.1f) ||
                            !placer.placeOnStart ||
                            placer.placeOnEnable ||
                            !placer.recenterDuringStartup ||
                            !Mathf.Approximately(placer.startupRecenterSeconds, 1.25f) ||
                            placer.startupRecenterFrames != 18 ||
                            placer.usePreferredHeightInsteadOfHeadHeight ||
                            !placer.clampWorldHeight ||
                            !Mathf.Approximately(placer.minWorldHeight, 1.25f) ||
                            !Mathf.Approximately(placer.maxWorldHeight, 1.75f) ||
                            placer.comfortFollowEnabled;
        if (placerChanged)
        {
            Undo.RecordObject(placer, "Synchronize authored rehab startup placement");
            placer.uiRoot = modeSelect.SelectionPanelRoot.transform;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.placeOnStart = true;
            placer.placeOnEnable = false;
            placer.recenterDuringStartup = true;
            placer.startupRecenterSeconds = 1.25f;
            placer.startupRecenterFrames = 18;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.clampWorldHeight = true;
            placer.minWorldHeight = 1.25f;
            placer.maxWorldHeight = 1.75f;
            placer.comfortFollowEnabled = false;
            EditorUtility.SetDirty(placer);
        }

        var placementChanged = placement.selectionPanelRoot != modeSelect.SelectionPanelRoot.transform ||
                               placement.trainingFunctionPanelRoot != modeSelect.TrainingFunctionPanelRoot.transform ||
                               placement.resultPanelRoot != modeSelect.ResultPanelRoot.transform ||
                               placement.promptPanelRoot != modeSelect.SelectionPanelRoot.transform ||
                               placement.videoPanelRoot != modeSelect.VideoPanelRoot.transform ||
                               !placement.useSceneAuthoredTrainingLayout ||
                               placement.placeOnStart;
        if (placementChanged)
        {
            Undo.RecordObject(placement, "Synchronize authored rehab panel references");
            placement.selectionPanelRoot = modeSelect.SelectionPanelRoot.transform;
            placement.trainingFunctionPanelRoot = modeSelect.TrainingFunctionPanelRoot.transform;
            placement.resultPanelRoot = modeSelect.ResultPanelRoot.transform;
            placement.promptPanelRoot = modeSelect.SelectionPanelRoot.transform;
            placement.videoPanelRoot = modeSelect.VideoPanelRoot.transform;
            placement.useSceneAuthoredTrainingLayout = true;
            placement.placeOnStart = false;
            EditorUtility.SetDirty(placement);
        }

        var selectionScale = ElderCareMenuDesignTokens.RehabSelectionWorldScaleCompensation;
        var selectionScaleChanged = !Approximately(
            modeSelect.SelectionPanelRoot.transform.localScale,
            Vector3.one * selectionScale);
        if (selectionScaleChanged)
        {
            Undo.RecordObject(modeSelect.SelectionPanelRoot.transform, "Synchronize rehab selection world-scale compensation");
            modeSelect.SelectionPanelRoot.transform.localScale = Vector3.one * selectionScale;
            EditorUtility.SetDirty(modeSelect.SelectionPanelRoot.transform);
        }

        var strokeMigration = MigrateSecondaryMenuNativeStrokes(
            modeSelect.rehabTrainingSelectPanel.transform,
            new[]
            {
                new ChoiceCardStrokeSpec("BaduanjinButton", ElderCareMenuDesignTokens.Amber, true),
                new ChoiceCardStrokeSpec("TaiChiButton", ElderCareMenuDesignTokens.Jade, false)
            },
            true);
        if (!SynchronizeRehabTracking(scene, out var trackingChanged)) return;
        SaveAuthoredSceneIfChanged(
            scene,
            modeChanged || placerChanged || placementChanged || selectionScaleChanged || strokeMigration.changed || trackingChanged,
            "rehab");
    }

    private static bool SynchronizeRehabTracking(Scene scene, out bool changed)
    {
        changed = false;
        var session = FindSceneComponent<RehabSessionManager>(scene);
        var xrOrigin = FindSceneComponent<XROrigin>(scene);
        var mainCamera = FindMainCameraInScene(scene);
        if (session == null || xrOrigin == null || mainCamera == null)
        {
            Debug.LogError(
                "[RehabSceneBuilder] Rehab tracking synchronization requires RehabSessionManager, XROrigin, and a Main Camera.");
            return false;
        }

        var managers = session.gameObject;
        var hmd = mainCamera.transform;
        var leftController = FindChildByName(xrOrigin.transform, "Left Controller");
        var rightController = FindChildByName(xrOrigin.transform, "Right Controller");

        var pxrManager = EnsureSinglePXRManager(scene, xrOrigin.gameObject, out var pxrManagerChanged);
        changed |= pxrManagerChanged;

        if (pxrManager.bodyTracking || !pxrManager.openMRC)
        {
            pxrManager.bodyTracking = false;
            pxrManager.openMRC = true;
            EditorUtility.SetDirty(pxrManager);
            changed = true;
        }

        var poseTracker = FindSceneComponent<HandPoseTracker>(scene);
        if (poseTracker == null)
        {
            poseTracker = managers.AddComponent<HandPoseTracker>();
            changed = true;
        }

        if (poseTracker.hmdTransform != hmd ||
            poseTracker.leftControllerTransform != leftController ||
            poseTracker.rightControllerTransform != rightController)
        {
            poseTracker.hmdTransform = hmd;
            poseTracker.leftControllerTransform = leftController;
            poseTracker.rightControllerTransform = rightController;
            EditorUtility.SetDirty(poseTracker);
            changed = true;
        }

        var controllerPoseProvider = FindSceneComponent<ControllerPoseProvider>(scene);
        if (controllerPoseProvider == null)
        {
            controllerPoseProvider = managers.AddComponent<ControllerPoseProvider>();
            changed = true;
        }

        if (controllerPoseProvider.HandPoseTracker != poseTracker)
        {
            controllerPoseProvider.HandPoseTracker = poseTracker;
            EditorUtility.SetDirty(controllerPoseProvider);
            changed = true;
        }

        var picoBodyTrackingProvider = FindSceneComponent<PicoBodyTrackingProvider>(scene);
        if (picoBodyTrackingProvider == null)
        {
            var providerObject = new GameObject("BodyTrackingSystem");
            providerObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(providerObject, scene);
            providerObject.transform.SetParent(xrOrigin.transform, false);
            picoBodyTrackingProvider = providerObject.AddComponent<PicoBodyTrackingProvider>();
            picoBodyTrackingProvider.AutoStartOnEnable = false;
            changed = true;
        }

        if (picoBodyTrackingProvider.XrOrigin != xrOrigin.transform ||
            picoBodyTrackingProvider.OutputSpace != PicoBodyTrackingOutputSpace.XrOriginLocal ||
            picoBodyTrackingProvider.AutoStartOnEnable ||
            picoBodyTrackingProvider.enabled)
        {
            picoBodyTrackingProvider.XrOrigin = xrOrigin.transform;
            picoBodyTrackingProvider.OutputSpace = PicoBodyTrackingOutputSpace.XrOriginLocal;
            picoBodyTrackingProvider.AutoStartOnEnable = false;
            picoBodyTrackingProvider.StopTracking();
            picoBodyTrackingProvider.enabled = false;
            EditorUtility.SetDirty(picoBodyTrackingProvider);
            changed = true;
        }

        var bodyTrackingDebugRenderer = FindSceneComponent<PicoBodyTrackingDebugRenderer>(scene);
        if (bodyTrackingDebugRenderer == null)
        {
            var debugObject = new GameObject("BodyTrackingDebug");
            SceneManager.MoveGameObjectToScene(debugObject, scene);
            debugObject.transform.SetParent(picoBodyTrackingProvider.transform, false);
            bodyTrackingDebugRenderer = debugObject.AddComponent<PicoBodyTrackingDebugRenderer>();
            bodyTrackingDebugRenderer.DebugSkeletonEnabled = false;
            changed = true;
        }

        if (bodyTrackingDebugRenderer.Provider != picoBodyTrackingProvider)
        {
            bodyTrackingDebugRenderer.Provider = picoBodyTrackingProvider;
            EditorUtility.SetDirty(bodyTrackingDebugRenderer);
            changed = true;
        }

        var bodyTrackingStatusPanel = FindSceneComponent<PicoBodyTrackingStatusPanel>(scene);
        if (bodyTrackingStatusPanel == null)
        {
            bodyTrackingStatusPanel = picoBodyTrackingProvider.gameObject.AddComponent<PicoBodyTrackingStatusPanel>();
            bodyTrackingStatusPanel.StatusPanelEnabled = false;
            changed = true;
        }

        var statusFont = GetRehabFontAsset();
        var statusPanelSize = new Vector2(1200f, 720f);
        var statusPanelScale = Vector3.one * 0.001f;
        if (bodyTrackingStatusPanel.Provider != picoBodyTrackingProvider ||
            bodyTrackingStatusPanel.StatusPanelEnabled ||
            bodyTrackingStatusPanel.TargetCamera != mainCamera ||
            bodyTrackingStatusPanel.StatusFontAsset != statusFont ||
            !Mathf.Approximately(bodyTrackingStatusPanel.StatusDistance, 1.2f) ||
            !Mathf.Approximately(bodyTrackingStatusPanel.StatusVerticalOffset, -0.18f) ||
            !Mathf.Approximately(bodyTrackingStatusPanel.StatusFontSize, 44f) ||
            bodyTrackingStatusPanel.StatusPanelSize != statusPanelSize ||
            bodyTrackingStatusPanel.StatusPanelScale != statusPanelScale)
        {
            bodyTrackingStatusPanel.Provider = picoBodyTrackingProvider;
            bodyTrackingStatusPanel.StatusPanelEnabled = false;
            bodyTrackingStatusPanel.TargetCamera = mainCamera;
            bodyTrackingStatusPanel.StatusFontAsset = statusFont;
            bodyTrackingStatusPanel.StatusDistance = 1.2f;
            bodyTrackingStatusPanel.StatusVerticalOffset = -0.18f;
            bodyTrackingStatusPanel.StatusFontSize = 44f;
            bodyTrackingStatusPanel.StatusPanelSize = statusPanelSize;
            bodyTrackingStatusPanel.StatusPanelScale = statusPanelScale;
            EditorUtility.SetDirty(bodyTrackingStatusPanel);
            changed = true;
        }

        var wristProvider = FindSceneComponent<PicoWristObjectTrackingProvider>(scene);
        if (wristProvider == null)
        {
            wristProvider = managers.AddComponent<PicoWristObjectTrackingProvider>();
            changed = true;
        }

        if (wristProvider.HmdTransform != hmd ||
            wristProvider.XrOrigin != xrOrigin.transform ||
            wristProvider.RequiredStableFrames != 20)
        {
            wristProvider.HmdTransform = hmd;
            wristProvider.XrOrigin = xrOrigin.transform;
            wristProvider.RequiredStableFrames = 20;
            EditorUtility.SetDirty(wristProvider);
            changed = true;
        }

        var poseProviderSelector = FindSceneComponent<RehabPoseProviderSelector>(scene);
        if (poseProviderSelector == null)
        {
            poseProviderSelector = managers.AddComponent<RehabPoseProviderSelector>();
            changed = true;
        }

        if (poseProviderSelector.PrimaryProvider != wristProvider ||
            poseProviderSelector.FallbackProvider != controllerPoseProvider ||
            !poseProviderSelector.AllowAutomaticFallback ||
            poseProviderSelector.Preference != RehabTrackingPreference.Auto)
        {
            poseProviderSelector.PrimaryProvider = wristProvider;
            poseProviderSelector.FallbackProvider = controllerPoseProvider;
            poseProviderSelector.AllowAutomaticFallback = true;
            poseProviderSelector.Preference = RehabTrackingPreference.Auto;
            EditorUtility.SetDirty(poseProviderSelector);
            changed = true;
        }

        if (session.handPoseTracker != poseTracker || session.poseProviderSelector != poseProviderSelector)
        {
            session.handPoseTracker = poseTracker;
            session.poseProviderSelector = poseProviderSelector;
            EditorUtility.SetDirty(session);
            changed = true;
        }

        return true;
    }

    private static bool ValidateAuthoredRehabBaseline(
        Scene scene,
        RehabModeSelectUI modeSelect,
        ComfortWorldSpaceUIPlacer placer,
        RehabPanelPlacementController placement)
    {
        if (modeSelect == null || placer == null || placement == null ||
            modeSelect.SelectionPanelRoot == null ||
            modeSelect.TrainingFunctionPanelRoot == null ||
            modeSelect.ResultPanelRoot == null ||
            modeSelect.VideoPanelRoot == null ||
            modeSelect.rehabTrainingSelectPanel == null ||
            modeSelect.rehabTrainingPanel == null ||
            modeSelect.trainingResultPanel == null ||
            modeSelect.baduanjinButton == null ||
            modeSelect.taiChiButton == null ||
            modeSelect.backButton == null)
        {
            Debug.LogError("[RehabSceneBuilder] Rehab authored baseline is missing one or more required page roots/panels. No scene changes were saved.");
            return false;
        }

        // The authored scene intentionally leaves the training/result navigation
        // references empty. RehabModeSelectUI resolves those buttons from the
        // existing page hierarchy at runtime (and creates the recenter button only
        // when it is absent), so treating these optional references as a broken
        // authored baseline would incorrectly block an otherwise safe sync.

        if (modeSelect.rehabTrainingSelectPanel.transform.parent != modeSelect.SelectionPanelRoot.transform ||
            modeSelect.rehabTrainingPanel.transform.parent != modeSelect.TrainingFunctionPanelRoot.transform ||
            modeSelect.trainingResultPanel.transform.parent != modeSelect.ResultPanelRoot.transform ||
            !RehabSelectionVisualSkin.IsBuilt(modeSelect.rehabTrainingSelectPanel))
        {
            Debug.LogError("[RehabSceneBuilder] Rehab authored page hierarchy or baked selection visual is incomplete. No scene changes were saved.");
            return false;
        }

        if (FindSceneGameObject(scene, "RehabPromptCanvas") == null ||
            FindSceneGameObject(scene, "RehabVideoPanel") != modeSelect.VideoPanelRoot)
        {
            Debug.LogError("[RehabSceneBuilder] Rehab authored canvas/video baseline is incomplete. No scene changes were saved.");
            return false;
        }

        return true;
    }

    private static bool TryOpenAuthoredScene(string scenePath, string label, out Scene scene)
    {
        scene = default;
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[RehabSceneBuilder] Authored {label} scene is missing: {scenePath}. Restore it from version control; destructive empty-scene reconstruction is disabled.");
            return false;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning($"[RehabSceneBuilder] Cancelled before synchronizing the authored {label} scene.");
            return false;
        }

        var active = SceneManager.GetActiveScene();
        scene = active.IsValid() && active.isLoaded && active.path == scenePath
            ? active
            : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"[RehabSceneBuilder] Could not open authored {label} scene: {scenePath}");
            return false;
        }

        return true;
    }

    private static T FindSingleSceneComponent<T>(Scene scene, string label) where T : Component
    {
        T found = null;
        var count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var components = root.GetComponentsInChildren<T>(true);
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;
                found = components[i];
                count++;
            }
        }

        if (count == 1) return found;
        Debug.LogError($"[RehabSceneBuilder] Expected exactly one {label} in {scene.path}, found {count}.");
        return null;
    }

    private static GameObject FindSceneGameObject(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }
        }

        return null;
    }

    private static bool HasPersistentListener(Button button, Object target, string methodName)
    {
        if (button == null || target == null) return false;
        var eventCount = button.onClick.GetPersistentEventCount();
        if (eventCount != 1) return false;
        return button.onClick.GetPersistentTarget(0) == target &&
               button.onClick.GetPersistentMethodName(0) == methodName;
    }

    private static void SaveAuthoredSceneIfChanged(Scene scene, bool changed, string label)
    {
        if (!changed)
        {
            Debug.Log($"[RehabSceneBuilder] Authored {label} scene already matches the safe baseline; no scene data was rewritten.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError($"[RehabSceneBuilder] Failed to save synchronized authored {label} scene: {scene.path}");
            return;
        }

        Debug.Log($"[RehabSceneBuilder] Synchronized safe settings in the authored {label} scene without rebuilding its hierarchy or spatial layout.");
    }

    private struct NativeStrokeMigrationResult
    {
        public bool changed;
        public int outlinesRemoved;
        public int outlinesRemaining;
    }

    private struct ChoiceCardStrokeSpec
    {
        public readonly string name;
        public readonly Color accent;
        public readonly bool recommended;

        public ChoiceCardStrokeSpec(string name, Color accent, bool recommended)
        {
            this.name = name;
            this.accent = accent;
            this.recommended = recommended;
        }
    }

    private static NativeStrokeMigrationResult MigrateMainEntryNativeStrokes(Transform canvas)
    {
        var result = new NativeStrokeMigrationResult();
        if (canvas == null) return result;

        MigrateNativeStroke(canvas, "Panel", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.52f), 2f, ref result);
        MigrateNativeStroke(canvas, "Panel/RicePaperPanel", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.68f), 1.4f, ref result);

        MigrateMainEntryModuleStroke(canvas, "Module_HealthGame", ElderCareMenuDesignTokens.Jade, true, ref result);
        MigrateMainEntryModuleStroke(canvas, "Module_Rehab", ElderCareMenuDesignTokens.Amber, true, ref result);
        MigrateMainEntryModuleStroke(canvas, "Module_Travel", ElderCareMenuDesignTokens.GoldDeep, false, ref result);
        MigrateMainEntryModuleStroke(canvas, "Module_Memory", ElderCareMenuDesignTokens.Coral, false, ref result);

        MigrateNativeStroke(canvas, "Panel/SafeBar/Background", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.46f), 1f, ref result);
        MigrateNativeStroke(canvas, "Panel/SafeBar/Settings/Surface", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.34f), 1.5f, ref result);
        MigrateNativeStroke(canvas, "Panel/SafeBar/Health/Surface", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.34f), 1.5f, ref result);
        MigrateNativeStroke(canvas, "Panel/SafeBar/Rank/Surface", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.34f), 1.5f, ref result);
        MigrateNativeStroke(canvas, "Minimize/Surface", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldDeep, 0.34f), 1.5f, ref result);
        MigrateNativeStroke(canvas, "Close/Surface", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.Coral, 0.34f), 1.5f, ref result);

        // MainEntry/Window keeps one large-offset Outline because it is an ambient
        // room-window shadow, not a rounded surface boundary.
        result.outlinesRemaining = canvas.GetComponentsInChildren<Outline>(true).Length;
        Debug.Log($"[RehabSceneBuilder] MainEntry native-stroke migration removed {result.outlinesRemoved} Outline component(s); {result.outlinesRemaining} decor shadow Outline component(s) remain.");
        return result;
    }

    private static void MigrateMainEntryModuleStroke(
        Transform canvas,
        string moduleName,
        Color accent,
        bool enabled,
        ref NativeStrokeMigrationResult result)
    {
        var prefix = "Panel/" + moduleName + "/";
        var boundaryColor = ElderCareMenuDesignTokens.WithAlpha(
            enabled ? accent : ElderCareMenuDesignTokens.GoldStroke,
            enabled ? 0.52f : 0.28f);
        MigrateNativeStroke(canvas, prefix + "Surface", boundaryColor, 1.5f, ref result);
        MigrateNativeStroke(canvas, prefix + "IconContainer", ElderCareMenuDesignTokens.WithAlpha(accent, 0.42f), 1f, ref result);
        MigrateNativeStroke(
            canvas,
            prefix + "StatusPanel",
            ElderCareMenuDesignTokens.WithAlpha(enabled ? accent : ElderCareMenuDesignTokens.GoldStroke, 0.52f),
            1f,
            ref result);
    }

    private static NativeStrokeMigrationResult MigrateSecondaryMenuNativeStrokes(
        Transform searchRoot,
        ChoiceCardStrokeSpec[] cards,
        bool suppressLegacyPanelRootSurface)
    {
        var result = new NativeStrokeMigrationResult();
        if (searchRoot == null) return result;

        var panel = searchRoot.name == "RehabTrainingSelectPanel"
            ? searchRoot
            : FindChildByName(searchRoot, "Panel");
        if (panel == null)
        {
            Debug.LogError("[RehabSceneBuilder] Secondary-menu panel was not found; native-stroke migration was skipped.");
            return result;
        }

        if (suppressLegacyPanelRootSurface)
        {
            RemoveOutlineAndDisableNativeStroke(panel, ref result);
            DisableLegacyPanelRootSurface(panel, ref result);
        }

        MigrateNativeStroke(panel, "VisualRoot/WoodFrame", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.48f), 2f, ref result);
        MigrateNativeStroke(panel, "VisualRoot/RicePaperPanel", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.72f), 1.5f, ref result);
        RemoveOutlineAndDisableNativeStroke(panel, "VisualRoot/RiceWarmEdge", ref result);

        if (cards != null)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                var spec = cards[i];
                var card = FindChildByName(panel, spec.name);
                if (card == null)
                {
                    Debug.LogError("[RehabSceneBuilder] Choice card was not found during native-stroke migration: " + spec.name);
                    continue;
                }

                MigrateNativeStroke(
                    card,
                    "Content/Background",
                    ElderCareMenuDesignTokens.WithAlpha(
                        spec.recommended ? ElderCareMenuDesignTokens.Amber : ElderCareMenuDesignTokens.GoldStroke,
                        0.72f),
                    1.5f,
                    ref result);
                MigrateNativeStroke(card, "Content/IconContainer", ElderCareMenuDesignTokens.WithAlpha(spec.accent, 0.46f), 1f, ref result);
                MigrateNativeStroke(card, "Content/StartButtonVisual", ElderCareMenuDesignTokens.WithAlpha(spec.accent, 0.76f), 1.2f, ref result);

                RemoveOutlineAndDisableNativeStroke(card, "Content/InnerRice", ref result);
                RemoveOutlineAndDisableNativeStroke(card, "Content/RecommendationRibbon", ref result);
                RemoveOutlineAndDisableNativeStroke(card, "Content/Metadata/DurationPill", ref result);
                RemoveOutlineAndDisableNativeStroke(card, "Content/Metadata/IntensityPill", ref result);
            }
        }

        MigrateNativeStroke(panel, "BottomDock", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.46f), 1f, ref result);
        MigrateNativeStroke(panel, "BottomDock/BackButton", ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.66f), 1.2f, ref result);

        result.outlinesRemaining = panel.GetComponentsInChildren<Outline>(true).Length;
        Debug.Log($"[RehabSceneBuilder] {panel.name} native-stroke migration removed {result.outlinesRemoved} Outline component(s); {result.outlinesRemaining} remain in this menu scope.");
        return result;
    }

    private static void DisableLegacyPanelRootSurface(
        Transform panelRoot,
        ref NativeStrokeMigrationResult result)
    {
        var legacySurface = panelRoot != null
            ? panelRoot.GetComponent<ElderCareRoundedPanel>()
            : null;
        if (legacySurface == null || (!legacySurface.enabled && !legacySurface.raycastTarget)) return;

        Undo.RecordObject(legacySurface, "Suppress obsolete rehab selection root surface");
        legacySurface.enabled = false;
        legacySurface.raycastTarget = false;
        EditorUtility.SetDirty(legacySurface);
        result.changed = true;
    }

    private static void MigrateNativeStroke(
        Transform root,
        string relativePath,
        Color strokeColor,
        float strokeWidth,
        ref NativeStrokeMigrationResult result)
    {
        var target = string.IsNullOrEmpty(relativePath) ? root : root.Find(relativePath);
        if (target == null)
        {
            Debug.LogError("[RehabSceneBuilder] Missing warm-menu surface: " + root.name + "/" + relativePath);
            return;
        }

        var panel = target.GetComponent<ElderCareRoundedPanel>();
        if (panel == null)
        {
            Debug.LogError("[RehabSceneBuilder] Warm-menu surface has no ElderCareRoundedPanel: " + target.name);
            return;
        }

        var panelChanged = !panel.DrawStroke ||
                           !Approximately(panel.StrokeColor, strokeColor) ||
                           !Mathf.Approximately(panel.StrokeWidth, strokeWidth);
        if (panelChanged)
        {
            Undo.RecordObject(panel, "Migrate warm-menu surface to native stroke");
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(panel, strokeColor, strokeWidth);
            EditorUtility.SetDirty(panel);
            result.changed = true;
        }

        RemoveOutlines(target, ref result);
    }

    private static void RemoveOutlineAndDisableNativeStroke(
        Transform root,
        string relativePath,
        ref NativeStrokeMigrationResult result)
    {
        var target = string.IsNullOrEmpty(relativePath) ? root : root.Find(relativePath);
        if (target == null)
        {
            Debug.LogError("[RehabSceneBuilder] Missing warm-menu no-stroke surface: " + root.name + "/" + relativePath);
            return;
        }

        RemoveOutlineAndDisableNativeStroke(target, ref result);
    }

    private static void RemoveOutlineAndDisableNativeStroke(
        Transform target,
        ref NativeStrokeMigrationResult result)
    {
        var panel = target.GetComponent<ElderCareRoundedPanel>();
        if (panel != null && (panel.DrawStroke || panel.StrokeWidth > 0f || panel.StrokeColor.a > 0f))
        {
            Undo.RecordObject(panel, "Remove redundant warm-menu stroke");
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(panel, Color.clear, 0f);
            EditorUtility.SetDirty(panel);
            result.changed = true;
        }

        RemoveOutlines(target, ref result);
    }

    private static void RemoveOutlines(Transform target, ref NativeStrokeMigrationResult result)
    {
        var outlines = target.GetComponents<Outline>();
        for (var i = outlines.Length - 1; i >= 0; i--)
        {
            if (outlines[i] == null) continue;
            Undo.DestroyObjectImmediate(outlines[i]);
            result.outlinesRemoved++;
            result.changed = true;
        }
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= 0.00000001f;
    }

    private static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= 0.0001f &&
               Mathf.Abs(a.g - b.g) <= 0.0001f &&
               Mathf.Abs(a.b - b.b) <= 0.0001f &&
               Mathf.Abs(a.a - b.a) <= 0.0001f;
    }

    [System.Obsolete("Destructive main-entry reconstruction is disabled. Use SynchronizeAuthoredMainEntryScene.", true)]
    private static void BuildMainEntrySceneFromScratchLegacy()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var xrOrigin = CreateXrOrigin();
        var mainCamera = FindMainCamera();

        EnsureLight();
        EnsureXrInteractionSupport();

        var managers = new GameObject("EntryManagers");
        var menu = managers.AddComponent<UnifiedEntryMenu>();
        menu.healthGameMenuSceneName = "02_HealthGameMenu";
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
        var entryCanvas = BuildBakedMainEntryCanvas(menu, null);
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
        menu.applyHtmlStyleMainPanel = false;
        menu.htmlStyleMainCanvas = entryCanvas.transform;

        EditorUtility.SetDirty(managers);
        EditorUtility.SetDirty(uiRoot);
        EditorUtility.SetDirty(entryCanvas);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);

        EditorSceneManager.SaveScene(scene, MainEntryScenePath);
    }

    [System.Obsolete("Destructive health-menu reconstruction is disabled. Use SynchronizeAuthoredHealthGameMenuScene.", true)]
    private static void BuildHealthGameMenuSceneFromScratchLegacy()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var managers = CreateMixedRealitySceneFoundation(
            "HealthGameMenuManagers",
            out var xrOrigin,
            out var mainCamera);

        var controllerObject = new GameObject("HealthGameMenuController");
        controllerObject.transform.SetParent(managers.transform, false);
        var controller = controllerObject.AddComponent<HealthGameMenuController>();

        var uiRoot = CreateUiRoot("HealthGameMenuRoot", null);
        EnsureComponent<MrKeepVisible>(uiRoot);
        var menuCanvas = BuildHealthGameMenuCanvas(controller, mainCamera != null ? mainCamera.transform : null);
        AttachUiToRoot(menuCanvas.transform, uiRoot.transform);
        ConfigureComfortUiPlacer(
            uiRoot,
            mainCamera != null ? mainCamera.transform : null,
            uiRoot.transform,
            ElderCareUiTheme.MainEntryDistanceMeters);

        EditorUtility.SetDirty(controllerObject);
        EditorUtility.SetDirty(managers);
        EditorUtility.SetDirty(uiRoot);
        EditorUtility.SetDirty(menuCanvas);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);

        EditorSceneManager.SaveScene(scene, HealthGameMenuScenePath);
    }

    private static void BuildPingPongSceneInternal()
    {
        if (!System.IO.File.Exists(PingPongScenePath))
        {
            Debug.LogError("PingPong scene was not found at " + PingPongScenePath);
            return;
        }

        var pingPongScene = EditorSceneManager.OpenScene(PingPongScenePath, OpenSceneMode.Single);
        PingPongDemoSceneBuilder.BuildMixedRealityDemoScene();
        EditorSceneManager.SaveScene(pingPongScene, PingPongScenePath);
    }

    internal static GameObject CreateMixedRealitySceneFoundation(
        string managersName,
        out GameObject xrOrigin,
        out Camera mainCamera)
    {
        ConfigureMixedRealityProjectSettings();
        xrOrigin = CreateXrOrigin();
        mainCamera = FindMainCamera();

        EnsureLight();
        EnsureXrInteractionSupport();

        var managers = new GameObject(string.IsNullOrWhiteSpace(managersName) ? "SceneManagers" : managersName);
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
        EditorUtility.SetDirty(managers);
        return managers;
    }

    private static GameObject BuildHealthGameMenuCanvas(
        HealthGameMenuController controller,
        Transform cameraTransform)
    {
        var canvasGo = CreateWorldCanvas(
            "HealthGameMenuCanvas",
            cameraTransform,
            new Vector3(0f, 1.45f, ElderCareUiTheme.MainEntryDistanceMeters),
            HealthGameMenuVisualSkin.CanvasSize);
        canvasGo.transform.localScale = Vector3.one * HealthGameMenuVisualSkin.CanvasWorldScale;
        EnsureComponent<MrKeepVisible>(canvasGo);

        var visual = HealthGameMenuVisualSkin.Build(
            canvasGo.transform,
            GetRehabFontAsset(),
            LoadHealthMenuSprite(HealthSportSpriteRoot + "table_tennis.png"),
            LoadHealthMenuSprite(HealthSportSpriteRoot + "bow_and_arrow.png"),
            LoadHealthMenuSprite(HealthSportSpriteRoot + "direct_hit.png"),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "arrow-left.png"),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "clock.png"),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "player-play.png"));

        UnityEventTools.AddPersistentListener(visual.pingPongButton.onClick, controller.LoadPingPong);
        UnityEventTools.AddPersistentListener(visual.archeryButton.onClick, controller.LoadArchery);
        UnityEventTools.AddPersistentListener(visual.dartsButton.onClick, controller.LoadDarts);
        UnityEventTools.AddPersistentListener(visual.backButton.onClick, controller.ReturnToMainEntry);
        return canvasGo;
    }

    [System.Obsolete("Destructive rehab reconstruction is disabled. Use SynchronizeAuthoredRehabScene.", true)]
    private static void BuildRehabSceneFromScratchLegacy()
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
        var rehabUiPlacer = ConfigureComfortUiPlacer(uiRoot, hmd, rehabUi.selectionPanelRoot.transform, 2f);
        rehabUiPlacer.placeOnStart = true;
        rehabUiPlacer.recenterDuringStartup = true;
        rehabUiPlacer.startupRecenterSeconds = 1.25f;
        rehabUiPlacer.startupRecenterFrames = 18;
        rehabUiPlacer.usePreferredHeightInsteadOfHeadHeight = false;
        rehabUiPlacer.hmdHeightOffsetMeters = -0.1f;
        rehabUiPlacer.clampWorldHeight = true;
        rehabUiPlacer.minWorldHeight = 1.25f;
        rehabUiPlacer.maxWorldHeight = 1.75f;
        rehabUiPlacer.comfortFollowEnabled = false;

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
        panelPlacement.selectionPanelRoot = rehabUi.selectionPanelRoot.transform;
        panelPlacement.trainingLayoutAnchor = rehabUi.trainingLayoutAnchor != null
            ? rehabUi.trainingLayoutAnchor.transform
            : null;
        panelPlacement.trainingFunctionPanelRoot = rehabUi.trainingFunctionPanelRoot.transform;
        panelPlacement.resultPanelRoot = rehabUi.resultPanelRoot.transform;
        panelPlacement.promptPanelRoot = rehabUi.selectionPanelRoot.transform;
        panelPlacement.trainingLayoutDistance = 1.8f;
        panelPlacement.trainingLayoutHeightOffset = -0.1f;
        panelPlacement.resultPanelDistance = 2f;
        panelPlacement.resultPanelHeightOffset = -0.1f;
        panelPlacement.clampResultPanelHeight = true;
        panelPlacement.minResultPanelHeight = 1.25f;
        panelPlacement.maxResultPanelHeight = 1.75f;
        panelPlacement.promptPanelDistance = 1.8f;
        panelPlacement.videoPanelDistance = 2.2f;
        panelPlacement.videoPanelYawOffsetDegrees = 40f;
        panelPlacement.panelHeight = 1.45f;
        panelPlacement.placeOnStart = false;

        var virtualCoach = BuildVirtualCoach(visualRoot.transform, hmd);
        virtualCoach.gameObject.SetActive(false);

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
        session.placeTrainingAreaOnStart = false;
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
        modeSelectUi.trainingRecenterButton = rehabUi.trainingRecenterButton;
        modeSelectUi.resultBackButton = rehabUi.resultBackButton;
        modeSelectUi.homeMenu = homeMenu;
        modeSelectUi.uiPlacer = rehabUiPlacer;
        modeSelectUi.panelPlacementController = panelPlacement;
        modeSelectUi.sessionManager = session;
        modeSelectUi.showTrainingSelectOnStart = true;
        modeSelectUi.placeUiOnStart = false;
        modeSelectUi.placeUiOnMainMenuOpen = true;
        modeSelectUi.placeUiOnTrainingSelectOpen = true;
        modeSelectUi.applyHtmlStylePanels = false;
        var modeSelectSerialized = new SerializedObject(modeSelectUi);
        modeSelectSerialized.FindProperty("selectionPanelRoot").objectReferenceValue = rehabUi.selectionPanelRoot;
        modeSelectSerialized.FindProperty("trainingFunctionPanelRoot").objectReferenceValue = rehabUi.trainingFunctionPanelRoot;
        modeSelectSerialized.FindProperty("resultPanelRoot").objectReferenceValue = rehabUi.resultPanelRoot;
        modeSelectSerialized.ApplyModifiedPropertiesWithoutUndo();
        session.modeSelectUI = modeSelectUi;
        RehabVideoGuideSceneRepair.EnsureRehabBaduanjinVideoGuideInOpenScene();
        AddPersistentButtonListener(rehabUi.baduanjinButton, modeSelectUi.StartBaduanjinTraining);
        AddPersistentButtonListener(rehabUi.taiChiButton, modeSelectUi.StartTaiChiTraining);
        AddPersistentButtonListener(rehabUi.backButton, modeSelectUi.ReturnToMainEntry);
        AddPersistentButtonListener(rehabUi.trainingBackButton, modeSelectUi.ShowTrainingSelectPanel);
        AddPersistentButtonListener(rehabUi.trainingRecenterButton, modeSelectUi.RecenterTrainingEnvironment);
        AddPersistentButtonListener(rehabUi.resultBackButton, modeSelectUi.ShowTrainingSelectPanel);

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
        EditorUtility.SetDirty(modeSelectUi);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);
        EditorSceneManager.SaveScene(scene, RehabScenePath);
    }

    internal static bool SynchronizeRehabScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Cannot synchronize an invalid or unloaded rehab scene.");
            return false;
        }

        return SynchronizeRehabTracking(scene, out _);
    }

    private static void AddPersistentButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;

        var target = action.Target as Object;
        var methodName = action.Method != null ? action.Method.Name : string.Empty;
        for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
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

    private static GameObject BuildBakedMainEntryCanvas(UnifiedEntryMenu menu, Transform cameraTransform)
    {
        var canvasGo = CreateWorldCanvas(
            "MainEntryCanvas",
            cameraTransform,
            new Vector3(0f, 1.45f, ElderCareUiTheme.MainEntryDistanceMeters),
            HtmlStyleMainEntryPanel.CanvasSize);
        EnsureComponent<MrKeepVisible>(canvasGo);

        var visual = HtmlStyleMainEntryPanel.Ensure(canvasGo.transform, menu, GetRehabFontAsset());
        visual.rebuildOnEnable = false;
        visual.normalizeWorldCanvasScale = true;
        visual.targetWorldWidthMeters = 1.55f;
        visual.BuildOrRepair();
        EditorUtility.SetDirty(visual);
        return canvasGo;
    }

    // Retained only as a migration reference for older generated scenes. New builds use BuildBakedMainEntryCanvas.
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
            menu.LoadHealthGames,
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
        root.SetActive(false);

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

        var selectionPanelRoot = CreatePageRoot(canvasGo.transform, "SelectionPanelRoot");
        var selectionRect = selectionPanelRoot.GetComponent<RectTransform>();
        selectionRect.anchorMin = new Vector2(0.5f, 0.5f);
        selectionRect.anchorMax = new Vector2(0.5f, 0.5f);
        selectionRect.pivot = new Vector2(0.5f, 0.5f);
        selectionRect.sizeDelta = RehabSelectionVisualSkin.CanvasSize;
        selectionRect.anchoredPosition = Vector2.zero;
        selectionRect.localScale = Vector3.one *
                                   ElderCareMenuDesignTokens.RehabSelectionWorldScaleCompensation;
        var trainingFunctionPanelRoot = CreatePageRoot(canvasGo.transform, "TrainingFunctionPanelRoot");
        var resultPanelRoot = CreatePageRoot(canvasGo.transform, "ResultPanelRoot");
        var ui = new RehabTrainingUi
        {
            canvas = canvasGo,
            mainMenuPanel = CreatePanel(canvasGo.transform, "MainMenuPanel"),
            selectionPanelRoot = selectionPanelRoot,
            rehabTrainingSelectPanel = CreatePageRoot(selectionPanelRoot.transform, "RehabTrainingSelectPanel"),
            trainingLayoutAnchor = null,
            trainingFunctionPanelRoot = trainingFunctionPanelRoot,
            rehabTrainingPanel = CreatePanel(trainingFunctionPanelRoot.transform, "RehabTrainingPanel"),
            resultPanelRoot = resultPanelRoot,
            trainingResultPanel = CreatePanel(resultPanelRoot.transform, "TrainingResultPanel")
        };

        CreateText(ui.mainMenuPanel.transform, "Title", "康复运动", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 104f), new Vector2(800f, 80f));
        ui.rehabButton = CreateButton(ui.mainMenuPanel.transform, "RehabButton", "康复运动", new Vector2(0f, -36f), new Vector2(400f, 88f));

        var selectionVisual = RehabSelectionVisualSkin.Build(
            ui.rehabTrainingSelectPanel.transform,
            GetRehabFontAsset(),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "arrow-left.png"),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "clock.png"),
            LoadHealthMenuSprite(HealthUiSpriteRoot + "player-play.png"));
        ui.baduanjinButton = selectionVisual.baduanjinButton;
        ui.taiChiButton = selectionVisual.taiChiButton;
        ui.backButton = selectionVisual.backButton;

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
        ui.startButton = CreateButton(ui.rehabTrainingPanel.transform, "StartButton", "开始", new Vector2(0f, -174f), new Vector2(186f, 82f));
        ui.trainingRecenterButton = CreateButton(ui.rehabTrainingPanel.transform, "RecenterButton", "重新对准", new Vector2(-232f, -174f), new Vector2(186f, 82f));
        ui.trainingBackButton = CreateButton(ui.rehabTrainingPanel.transform, "HomeButton", "返回", new Vector2(232f, -174f), new Vector2(186f, 82f));

        CreateText(ui.trainingResultPanel.transform, "Title", "训练结果", ElderCareUiTheme.Title, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 128f), new Vector2(800f, 76f));
        var summary = CreateText(ui.trainingResultPanel.transform, "Summary", "训练结束后结果会自动保存到本机", ElderCareUiTheme.Body, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 30f), new Vector2(560f, 100f));
        summary.color = ElderCareUiTheme.TextSecondary;
        ui.resultBackButton = CreateButton(ui.trainingResultPanel.transform, "BackButton", "返回选择", new Vector2(0f, -112f), new Vector2(292f, 82f));

        ui.mainMenuPanel.SetActive(false);
        ui.selectionPanelRoot.SetActive(true);
        ui.trainingFunctionPanelRoot.SetActive(false);
        ui.resultPanelRoot.SetActive(false);
        return ui;
    }

    private static GameObject CreatePageRoot(Transform parent, string name)
    {
        var root = CreateUiObject(name, parent);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        return root;
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

    private static void ConfigurePingPongReturnNavigationInternal()
    {
        if (!System.IO.File.Exists(PingPongScenePath))
        {
            Debug.LogWarning("PingPong scene was not found at " + PingPongScenePath);
            return;
        }

        var pingPongScene = SceneManager.GetActiveScene();
        if (!pingPongScene.IsValid() || pingPongScene.path != PingPongScenePath)
        {
            pingPongScene = EditorSceneManager.OpenScene(PingPongScenePath, OpenSceneMode.Single);
        }

        ModuleHomeMenu sceneHomeMenu = null;
        var homeMenus = Object.FindObjectsOfType<ModuleHomeMenu>(true);
        for (var i = 0; i < homeMenus.Length; i++)
        {
            var homeMenu = homeMenus[i];
            if (homeMenu == null || homeMenu.gameObject.scene != pingPongScene) continue;

            homeMenu.mainEntrySceneName = "02_HealthGameMenu";
            if (sceneHomeMenu == null)
            {
                sceneHomeMenu = homeMenu;
            }

            EditorUtility.SetDirty(homeMenu);
        }

        if (sceneHomeMenu == null)
        {
            var navigationObject = FindSceneObjectByName(pingPongScene, "PingPongHealthGameNavigation");
            if (navigationObject == null)
            {
                navigationObject = new GameObject("PingPongHealthGameNavigation");
                var managers = FindSceneObjectByName(pingPongScene, "Managers");
                if (managers != null)
                {
                    navigationObject.transform.SetParent(managers.transform, false);
                }
            }

            sceneHomeMenu = EnsureComponent<ModuleHomeMenu>(navigationObject);
            sceneHomeMenu.mainEntrySceneName = "02_HealthGameMenu";
            EditorUtility.SetDirty(navigationObject);
            EditorUtility.SetDirty(sceneHomeMenu);
        }

        var controlPanels = Object.FindObjectsOfType<PingPongUnifiedControlPanel>(true);
        for (var i = 0; i < controlPanels.Length; i++)
        {
            var controlPanel = controlPanels[i];
            if (controlPanel == null || controlPanel.gameObject.scene != pingPongScene) continue;

            controlPanel.mainEntrySceneName = "02_HealthGameMenu";
            controlPanel.moduleHomeMenu = sceneHomeMenu;
            controlPanel.loadMainEntryWhenHomeMenuMissing = true;
            EditorUtility.SetDirty(controlPanel);
        }

        EditorSceneManager.MarkSceneDirty(pingPongScene);
        EditorSceneManager.SaveScene(pingPongScene, PingPongScenePath);
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

        SetLayerRecursively(sensingRoot, "RoomSensing");
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
        SetLayerRecursively(template, "RoomSensing");
        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static void SetLayerRecursively(GameObject root, string layerName)
    {
        if (root == null) return;

        var layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogError($"Required layer is not configured: {layerName}");
            return;
        }

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
            EditorUtility.SetDirty(child.gameObject);
        }
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

    private static Sprite LoadHealthMenuSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return null;

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null &&
            (importer.textureType != TextureImporterType.Sprite ||
             importer.spriteImportMode != SpriteImportMode.Single ||
             importer.mipmapEnabled ||
             !importer.alphaIsTransparency))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogError("Health game menu sprite could not be loaded: " + assetPath);
        }

        return sprite;
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
        var eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        var eventSystem = EventSystem.current != null
            ? EventSystem.current
            : eventSystems.Length > 0
                ? eventSystems[0]
                : null;
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        for (var i = 0; i < eventSystems.Length; i++)
        {
            var candidate = eventSystems[i];
            if (candidate == null || candidate == eventSystem) continue;

            Object.DestroyImmediate(candidate.gameObject);
        }

        XRUIInputModule xrUiInputModule = null;
        var inputModules = eventSystem.GetComponents<BaseInputModule>();
        for (var i = 0; i < inputModules.Length; i++)
        {
            var module = inputModules[i];
            if (module == null) continue;

            var xrModule = module as XRUIInputModule;
            if (xrModule != null)
            {
                if (xrUiInputModule == null)
                {
                    xrUiInputModule = xrModule;
                }
                else
                {
                    Object.DestroyImmediate(xrModule);
                }

                continue;
            }

            Object.DestroyImmediate(module);
        }

        if (xrUiInputModule == null)
        {
            xrUiInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        ApplyXrUiInputModulePreset(xrUiInputModule);
        EditorUtility.SetDirty(eventSystem);
        EditorUtility.SetDirty(eventSystem.gameObject);
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

    internal static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainEntryScenePath, true),
            new EditorBuildSettingsScene(HealthGameMenuScenePath, true),
            new EditorBuildSettingsScene(PingPongScenePath, true),
            new EditorBuildSettingsScene(ArcheryTrainingScenePath, true),
            new EditorBuildSettingsScene(DartsTrainingScenePath, true),
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
        config.bodyTracking = true;
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

    private static PXR_Manager EnsureSinglePXRManager(
        Scene scene,
        GameObject preferredHost,
        out bool changed)
    {
        changed = false;
        PXR_Manager firstManager = null;
        PXR_Manager preferredManager = null;
        PXR_Manager inheritedPreferredManager = null;
        var roots = scene.GetRootGameObjects();

        for (var i = 0; i < roots.Length; i++)
        {
            var managers = roots[i].GetComponentsInChildren<PXR_Manager>(true);
            for (var j = 0; j < managers.Length; j++)
            {
                var manager = managers[j];
                if (manager == null) continue;

                if (firstManager == null) firstManager = manager;
                if (manager.gameObject != preferredHost) continue;

                if (preferredManager == null) preferredManager = manager;
                if (PrefabUtility.GetCorrespondingObjectFromSource(manager) != null)
                {
                    inheritedPreferredManager = manager;
                }
            }
        }

        var keeper = inheritedPreferredManager != null
            ? inheritedPreferredManager
            : preferredManager != null
                ? preferredManager
                : firstManager;
        if (keeper == null)
        {
            keeper = preferredHost.AddComponent<PXR_Manager>();
            changed = true;
        }

        for (var i = 0; i < roots.Length; i++)
        {
            var managers = roots[i].GetComponentsInChildren<PXR_Manager>(true);
            for (var j = 0; j < managers.Length; j++)
            {
                var manager = managers[j];
                if (manager != null && manager != keeper)
                {
                    Object.DestroyImmediate(manager);
                    changed = true;
                }
            }
        }

        return keeper;
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
