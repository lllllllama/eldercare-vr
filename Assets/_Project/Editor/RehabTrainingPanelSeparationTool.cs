using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PicoElderCare.Rehab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class RehabTrainingPanelSeparationTool
{
    private const string ScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string SplitMenuPath = "Tools/ElderCare/Rehab/Split Training Panels";
    private const string ValidateMenuPath = "Tools/ElderCare/Rehab/Validate Training Panel Separation";

    private sealed class SceneContext
    {
        public Scene Scene;
        public RehabModeSelectUI ModeSelect;
        public RehabPanelPlacementController Placement;
        public RehabVideoPanelLayoutController VideoLayout;
        public RehabVideoGuideController VideoGuide;
        public RehabSpatialRayControl SpatialControl;
        public RehabSessionManager Session;
        public GameObject SelectionPanel;
        public GameObject TrainingPanel;
        public GameObject ResultPanel;
        public GameObject VideoPanel;
        public Transform PageCanvasRoot;
        public Transform RehabRoot;
        public Transform RehabUiRoot;
        public Transform SelectionRoot;
        public Transform TrainingLayoutAnchor;
        public Transform TrainingRoot;
        public Transform ResultRoot;
        public Transform ViewTransform;
        public bool HadSeparatedPageRoots;
    }

    [MenuItem(SplitMenuPath)]
    public static void SplitTrainingPanels()
    {
        RunSplit(true);
    }

    [MenuItem(ValidateMenuPath)]
    public static void ValidateTrainingPanelSeparation()
    {
        RunValidation(true);
    }

    public static void SplitTrainingPanelsBatch()
    {
        if (!RunSplit(false))
        {
            throw new InvalidOperationException("Rehab training panel separation failed. See the Unity Console for details.");
        }
    }

    public static void ValidateTrainingPanelSeparationBatch()
    {
        if (!RunValidation(false))
        {
            throw new InvalidOperationException("Rehab training panel separation validation failed. See the Unity Console for details.");
        }
    }

    private static bool RunSplit(bool interactive)
    {
        try
        {
            var scene = OpenTargetScene(interactive);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (!TryResolveContext(scene, out var context, out var error))
            {
                Debug.LogError("[RehabPanelSeparation] Migration stopped: " + error);
                return false;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Split Rehab Training Panels");

            try
            {
                BuildSeparatedHierarchy(context);
                ApplySceneAuthoredLayout(context);
                RebindSerializedReferences(context);
                ConfigureVideoPresentation(context);
                ConfigureInitialVisibility(context);

                MarkContextDirty(context);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Unity could not save " + ScenePath + ".");
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            if (!TryResolveContext(scene, out var migratedContext, out error))
            {
                Debug.LogError("[RehabPanelSeparation] Migration saved, but post-migration resolution failed: " + error);
                return false;
            }

            var valid = ValidateContext(migratedContext, true);
            if (valid)
            {
                Debug.Log("[RehabPanelSeparation] Split completed and saved. " + DescribeHierarchy(migratedContext));
            }

            return valid;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    private static bool RunValidation(bool interactive)
    {
        try
        {
            var scene = OpenTargetScene(interactive);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (!TryResolveContext(scene, out var context, out var error))
            {
                Debug.LogError("[RehabPanelSeparation] Validation failed: " + error);
                return false;
            }

            return ValidateContext(context, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    private static Scene OpenTargetScene(bool interactive)
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[RehabPanelSeparation] Scene not found: " + ScenePath);
            return default;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded && activeScene.path == ScenePath)
        {
            return activeScene;
        }

        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[RehabPanelSeparation] Operation cancelled before opening the rehab scene.");
            return default;
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static bool TryResolveContext(Scene scene, out SceneContext context, out string error)
    {
        context = new SceneContext { Scene = scene };
        error = string.Empty;

        var modeSelects = FindSceneComponents<RehabModeSelectUI>(scene);
        if (modeSelects.Count != 1)
        {
            error = "Expected exactly one RehabModeSelectUI, found " + modeSelects.Count + ".";
            return false;
        }

        context.ModeSelect = modeSelects[0];
        context.SelectionPanel = ResolveReferencedOrUniqueNamed(
            scene,
            context.ModeSelect.rehabTrainingSelectPanel,
            "RehabTrainingSelectPanel",
            out error);
        if (context.SelectionPanel == null) return false;

        context.TrainingPanel = ResolveReferencedOrUniqueNamed(
            scene,
            context.ModeSelect.rehabTrainingPanel,
            "RehabTrainingPanel",
            out error);
        if (context.TrainingPanel == null) return false;

        context.ResultPanel = ResolveReferencedOrUniqueNamed(
            scene,
            context.ModeSelect.trainingResultPanel,
            "TrainingResultPanel",
            out error);
        if (context.ResultPanel == null) return false;

        if (context.SelectionPanel == context.TrainingPanel ||
            context.SelectionPanel == context.ResultPanel ||
            context.TrainingPanel == context.ResultPanel)
        {
            error = "Selection, training and result pages must be three distinct GameObjects.";
            return false;
        }

        context.Session = IsSceneComponent(context.ModeSelect.sessionManager, scene)
            ? context.ModeSelect.sessionManager
            : FindUniqueSceneComponent<RehabSessionManager>(scene, out error);
        if (context.Session == null) return false;

        context.VideoGuide = IsSceneComponent(context.ModeSelect.videoGuideController, scene)
            ? context.ModeSelect.videoGuideController
            : context.Session.videoGuideController;
        if (!IsSceneComponent(context.VideoGuide, scene))
        {
            context.VideoGuide = FindUniqueSceneComponent<RehabVideoGuideController>(scene, out error);
        }
        if (context.VideoGuide == null) return false;

        context.VideoPanel = context.VideoGuide.videoPanel;
        if (!IsSceneObject(context.VideoPanel, scene))
        {
            error = "RehabVideoGuideController.videoPanel is not assigned to a scene object.";
            return false;
        }

        context.VideoLayout = IsSceneComponent(context.VideoGuide.layoutController, scene)
            ? context.VideoGuide.layoutController
            : context.VideoPanel.GetComponent<RehabVideoPanelLayoutController>();
        if (context.VideoLayout == null)
        {
            context.VideoLayout = FindUniqueSceneComponent<RehabVideoPanelLayoutController>(scene, out error);
        }
        if (context.VideoLayout == null) return false;

        context.Placement = IsSceneComponent(context.ModeSelect.panelPlacementController, scene)
            ? context.ModeSelect.panelPlacementController
            : context.Session.panelPlacementController;
        if (context.Placement == null)
        {
            context.Placement = FindUniqueSceneComponent<RehabPanelPlacementController>(scene, out error);
        }
        if (context.Placement == null) return false;

        var spatialControls = FindSceneComponents<RehabSpatialRayControl>(scene);
        if (spatialControls.Count > 1)
        {
            error = "Expected at most one RehabSpatialRayControl, found " + spatialControls.Count + ".";
            return false;
        }
        context.SpatialControl = spatialControls.Count == 1 ? spatialControls[0] : null;

        context.SelectionRoot = FindExpectedPageRoot(scene, context.SelectionPanel, "SelectionPanelRoot", out error);
        if (!string.IsNullOrEmpty(error)) return false;
        context.TrainingRoot = FindExpectedPageRoot(scene, context.TrainingPanel, "TrainingFunctionPanelRoot", out error);
        if (!string.IsNullOrEmpty(error)) return false;
        context.ResultRoot = FindExpectedPageRoot(scene, context.ResultPanel, "ResultPanelRoot", out error);
        if (!string.IsNullOrEmpty(error)) return false;
        context.HadSeparatedPageRoots = context.SelectionRoot != null &&
                                        context.TrainingRoot != null &&
                                        context.ResultRoot != null;

        var selectionCanvas = FindOwningCanvasTransform(context.SelectionPanel.transform);
        var trainingCanvas = FindOwningCanvasTransform(context.TrainingPanel.transform);
        var resultCanvas = FindOwningCanvasTransform(context.ResultPanel.transform);
        if (selectionCanvas == null || resultCanvas == null)
        {
            error = "Selection and result pages must both resolve to an existing Canvas.";
            return false;
        }

        var selectionUsesOwnCanvas = context.SelectionRoot != null && selectionCanvas == context.SelectionRoot;
        if (selectionCanvas != resultCanvas && !selectionUsesOwnCanvas)
        {
            error = "Selection page must use the shared page Canvas before migration or its own SelectionPanelRoot Canvas after migration.";
            return false;
        }

        var trainingUsesOwnCanvas = context.TrainingRoot != null && trainingCanvas == context.TrainingRoot;
        var sharedPageCanvas = resultCanvas;
        if (trainingCanvas == null || (trainingCanvas != sharedPageCanvas && !trainingUsesOwnCanvas))
        {
            error = "Training page must use the selection Canvas before migration or its own TrainingFunctionPanelRoot Canvas after migration.";
            return false;
        }
        context.PageCanvasRoot = sharedPageCanvas;

        var existingRehabUiRoot = FindAncestorNamed(context.PageCanvasRoot, "RehabUIRoot");
        if (existingRehabUiRoot != null)
        {
            context.RehabUiRoot = existingRehabUiRoot;
            context.RehabRoot = existingRehabUiRoot.parent;
        }
        else
        {
            context.RehabRoot = FindLowestCommonAncestor(context.PageCanvasRoot, context.VideoPanel.transform);
        }

        if (context.RehabRoot == null)
        {
            error = "Could not determine a common Rehab root for the UI Canvas and RehabVideoPanel.";
            return false;
        }

        var anchorMatches = FindSceneTransforms(scene)
            .Where(transform => transform.name == "TrainingLayoutAnchor")
            .ToList();
        if (anchorMatches.Count > 1)
        {
            error = "Ambiguous TrainingLayoutAnchor: found " + anchorMatches.Count + " objects.";
            return false;
        }
        context.TrainingLayoutAnchor = anchorMatches.Count == 1 ? anchorMatches[0] : null;
        context.ViewTransform = ResolveMainCameraTransform(scene, out error);
        if (context.ViewTransform == null) return false;

        return true;
    }

    private static void BuildSeparatedHierarchy(SceneContext context)
    {
        if (context.RehabUiRoot == null)
        {
            context.RehabUiRoot = CreateTransformRoot("RehabUIRoot", context.RehabRoot);
        }

        var uiBranch = GetDirectChildBelow(context.PageCanvasRoot, context.RehabRoot);
        if (uiBranch != null && uiBranch != context.RehabUiRoot && !uiBranch.IsChildOf(context.RehabUiRoot))
        {
            ReparentPreservingWorld(uiBranch, context.RehabUiRoot, "Move rehab UI branch under RehabUIRoot");
        }

        context.SelectionRoot = EnsureRectPageRoot(
            context.PageCanvasRoot,
            context.SelectionRoot,
            "SelectionPanelRoot",
            true);
        context.TrainingRoot = EnsureRectPageRoot(
            context.PageCanvasRoot,
            context.TrainingRoot,
            "TrainingFunctionPanelRoot",
            true);
        context.ResultRoot = EnsureRectPageRoot(context.PageCanvasRoot, context.ResultRoot, "ResultPanelRoot");

        ReparentPreservingWorld(context.SelectionPanel.transform, context.SelectionRoot, "Move selection page");
        ReparentPreservingWorld(context.TrainingPanel.transform, context.TrainingRoot, "Move training page");
        ReparentPreservingWorld(context.ResultPanel.transform, context.ResultRoot, "Move result page");

        if (!context.HadSeparatedPageRoots)
        {
            ApplyLegacyFixedSlotsBeforeAnchoring(context);
        }

        ReparentPreservingWorld(
            context.SelectionRoot,
            context.RehabUiRoot,
            "Move SelectionPanelRoot under RehabUIRoot");
        EnsureStandalonePageCanvas(context, context.SelectionRoot, "selection");

        if (context.TrainingLayoutAnchor == null)
        {
            context.TrainingLayoutAnchor = CreateTransformRoot("TrainingLayoutAnchor", context.RehabUiRoot);
            ApplyWorldPose(
                context.TrainingLayoutAnchor,
                new Pose(context.VideoPanel.transform.position, context.VideoPanel.transform.rotation),
                "Place TrainingLayoutAnchor at authored video slot");
        }
        else if (context.TrainingLayoutAnchor.parent != context.RehabUiRoot)
        {
            ReparentPreservingWorld(
                context.TrainingLayoutAnchor,
                context.RehabUiRoot,
                "Move TrainingLayoutAnchor under RehabUIRoot");
        }

        ReparentPreservingWorld(
            context.TrainingRoot,
            context.TrainingLayoutAnchor,
            "Move TrainingFunctionPanelRoot under TrainingLayoutAnchor");
        ReparentPreservingWorld(
            context.VideoPanel.transform,
            context.TrainingLayoutAnchor,
            "Move RehabVideoPanel under TrainingLayoutAnchor");
        EnsureStandalonePageCanvas(context, context.TrainingRoot, "training function");
    }

    private static void ApplySceneAuthoredLayout(SceneContext context)
    {
        var placement = context.Placement;
        Undo.RecordObject(placement, "Configure scene-authored rehab layout");

        var originalSelectionDistance = placement.useSceneAuthoredTrainingLayout
            ? placement.selectionPanelDistance
            : placement.promptPanelDistance;
        var originalSelectionHeight = placement.useSceneAuthoredTrainingLayout
            ? placement.selectionPanelHeight
            : placement.panelHeight;

        placement.selectionPanelDistance = Mathf.Max(0.1f, originalSelectionDistance);
        placement.selectionPanelHeight = originalSelectionHeight;
        placement.trainingLayoutDistance = 1.8f;
        placement.trainingLayoutHeightOffset = -0.1f;
        placement.promptPanelDistance = 1.8f;
        placement.videoPanelDistance = 2.2f;
        placement.videoPanelYawOffsetDegrees = 40f;
        placement.panelHeight = 1.45f;
        placement.compactTrainingFunctionScale = Mathf.Clamp(placement.compactTrainingFunctionScale, 0.5f, 1.2f);
        if (Mathf.Approximately(placement.compactTrainingFunctionScale, 1f))
        {
            placement.compactTrainingFunctionScale = 0.84f;
        }
        placement.minPanelSeparationMeters = Mathf.Max(0.25f, placement.minPanelSeparationMeters);
        placement.useSceneAuthoredTrainingLayout = true;
    }

    private static void ApplyLegacyFixedSlotsBeforeAnchoring(SceneContext context)
    {
        var placement = context.Placement;
        var view = context.ViewTransform;
        var viewPosition = view != null ? view.position : new Vector3(0f, 1.55f, 0f);
        var forward = view != null
            ? Vector3.ProjectOnPlane(view.forward, Vector3.up)
            : Vector3.forward;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        var selectionPose = CreateSlotPose(
            viewPosition,
            forward,
            Mathf.Max(0.1f, placement.selectionPanelDistance),
            0f,
            placement.selectionPanelHeight);
        var videoPose = CreateSlotPose(viewPosition, forward, 1.8f, 0f, 1.45f);
        var trainingPose = CreateSlotPose(viewPosition, forward, 2.2f, 40f, 1.45f);

        ApplyWorldPose(context.SelectionRoot, selectionPose, "Place SelectionPanelRoot");
        ApplyWorldPose(context.ResultRoot, selectionPose, "Place ResultPanelRoot");
        ApplyWorldPose(context.VideoPanel.transform, videoPose, "Place RehabVideoPanel in authored video slot");
        ApplyWorldPose(context.TrainingRoot, trainingPose, "Place TrainingFunctionPanelRoot in authored function slot");

        Undo.RecordObject(context.SelectionRoot, "Keep selection page scale");
        context.SelectionRoot.localScale = Vector3.one;
        Undo.RecordObject(context.ResultRoot, "Keep result page scale");
        context.ResultRoot.localScale = Vector3.one;
        Undo.RecordObject(context.TrainingRoot, "Set compact training function scale");
        context.TrainingRoot.localScale = Vector3.one * 0.84f;

        var trainingSlotDirection = Quaternion.AngleAxis(40f, Vector3.up) * forward;
        EnsureMinimumTrainingPanelSeparation(context, trainingSlotDirection);
    }

    private static void EnsureStandalonePageCanvas(SceneContext context, Transform pageRoot, string pageLabel)
    {
        var sourceCanvas = context.PageCanvasRoot.GetComponent<Canvas>();
        if (sourceCanvas == null)
        {
            throw new InvalidOperationException("The selection page Canvas is missing.");
        }

        var targetCanvas = pageRoot.GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = Undo.AddComponent<Canvas>(pageRoot.gameObject);
        }

        Undo.RecordObject(targetCanvas, "Configure " + pageLabel + " World Space Canvas");
        targetCanvas.renderMode = RenderMode.WorldSpace;
        targetCanvas.worldCamera = sourceCanvas.worldCamera != null
            ? sourceCanvas.worldCamera
            : context.ViewTransform.GetComponent<Camera>();
        targetCanvas.planeDistance = sourceCanvas.planeDistance;
        targetCanvas.sortingLayerID = sourceCanvas.sortingLayerID;
        targetCanvas.sortingOrder = sourceCanvas.sortingOrder;
        targetCanvas.additionalShaderChannels = sourceCanvas.additionalShaderChannels;
        EditorUtility.SetDirty(targetCanvas);

        var sourceScaler = context.PageCanvasRoot.GetComponent<CanvasScaler>();
        if (sourceScaler != null)
        {
            var targetScaler = pageRoot.GetComponent<CanvasScaler>();
            if (targetScaler == null)
            {
                targetScaler = Undo.AddComponent<CanvasScaler>(pageRoot.gameObject);
            }

            Undo.RecordObject(targetScaler, "Copy " + pageLabel + " CanvasScaler settings");
            EditorUtility.CopySerialized(sourceScaler, targetScaler);
            EditorUtility.SetDirty(targetScaler);
        }

        foreach (var sourceRaycaster in context.PageCanvasRoot.GetComponents<BaseRaycaster>())
        {
            var raycasterType = sourceRaycaster.GetType();
            var targetRaycaster = pageRoot.GetComponent(raycasterType);
            if (targetRaycaster == null)
            {
                targetRaycaster = Undo.AddComponent(pageRoot.gameObject, raycasterType);
            }

            Undo.RecordObject(targetRaycaster, "Copy " + pageLabel + " Canvas raycaster settings");
            EditorUtility.CopySerialized(sourceRaycaster, targetRaycaster);
            EditorUtility.SetDirty(targetRaycaster);
        }
    }

    private static void RebindSerializedReferences(SceneContext context)
    {
        var placement = context.Placement;
        Undo.RecordObject(placement, "Bind separated rehab panels");
        placement.viewTransform = context.ViewTransform;
        placement.selectionPanelRoot = context.SelectionRoot;
        placement.trainingLayoutAnchor = context.TrainingLayoutAnchor;
        placement.trainingFunctionPanelRoot = context.TrainingRoot;
        placement.promptPanelRoot = context.SelectionRoot;
        placement.videoPanelRoot = context.VideoPanel.transform;
        placement.videoLayoutController = context.VideoLayout;
        placement.trainingLayoutDistance = 1.8f;
        placement.trainingLayoutHeightOffset = -0.1f;
        placement.useSceneAuthoredTrainingLayout = true;
        EditorUtility.SetDirty(placement);

        Undo.RecordObject(context.ModeSelect, "Bind separated rehab pages");
        context.ModeSelect.panelPlacementController = placement;
        var modeSerialized = new SerializedObject(context.ModeSelect);
        modeSerialized.Update();
        SetObjectReference(modeSerialized, "selectionPanelRoot", context.SelectionRoot.gameObject);
        SetObjectReference(modeSerialized, "trainingFunctionPanelRoot", context.TrainingRoot.gameObject);
        SetObjectReference(modeSerialized, "resultPanelRoot", context.ResultRoot.gameObject);
        SetObjectReference(modeSerialized, "videoPanelRoot", context.VideoPanel);
        modeSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(context.ModeSelect);

        Undo.RecordObject(context.Session, "Bind rehab panel placement");
        context.Session.panelPlacementController = placement;
        context.Session.placePromptCanvasWithTrainingArea = false;
        EditorUtility.SetDirty(context.Session);

        Undo.RecordObject(context.VideoGuide, "Bind fixed rehab video placement");
        context.VideoGuide.panelPlacementController = placement;
        context.VideoGuide.autoCreateVideoFrame = false;
        EditorUtility.SetDirty(context.VideoGuide);

        if (context.SpatialControl != null)
        {
            Undo.RecordObject(context.SpatialControl, "Bind fixed rehab spatial controls");
            context.SpatialControl.panelPlacementController = placement;
            context.SpatialControl.createVisibleVideoControls = false;
            context.SpatialControl.showDebugVideoControls = false;
            if (context.SpatialControl.controlCanvasRoot != null)
            {
                SetActiveWithUndo(context.SpatialControl.controlCanvasRoot, false, "Hide rehab video controls");
            }
            EditorUtility.SetDirty(context.SpatialControl);
        }
    }

    private static void ConfigureVideoPresentation(SceneContext context)
    {
        var layout = context.VideoLayout;
        Undo.RecordObject(layout, "Configure large rehab video");
        layout.panelRoot = context.VideoPanel.transform;
        layout.videoWidth = 1.22f;
        layout.videoHeight = 0.69f;
        layout.videoScale = 1f;
        layout.minVideoScale = 0.85f;
        layout.maxVideoScale = 1.15f;
        layout.videoScaleStep = 0.1f;
        layout.createClosedVideoFrame = true;
        layout.frameThickness = 0.012f;
        layout.framePadding = 0.035f;
        layout.frameColor = new Color(0.33f, 0.94f, 1f, 0.78f);

        if (layout.videoQuad == null)
        {
            throw new InvalidOperationException("RehabVideoPanelLayoutController.videoQuad is not assigned.");
        }

        if (layout.videoQuad.parent != context.VideoPanel.transform)
        {
            throw new InvalidOperationException("VideoQuad is not a direct child of RehabVideoPanel; migration stopped to avoid changing video hierarchy.");
        }

        EnsureClosedFrameHierarchy(layout.videoQuad.parent);
        Undo.RecordObject(layout.videoQuad, "Resize rehab VideoQuad");
        layout.ApplyVideoSize();
        EditorUtility.SetDirty(layout);
    }

    private static void ConfigureInitialVisibility(SceneContext context)
    {
        SetActiveWithUndo(context.SelectionPanel, true, "Enable selection page");
        SetActiveWithUndo(context.TrainingPanel, true, "Prepare training page");
        SetActiveWithUndo(context.ResultPanel, true, "Prepare result page");
        SetActiveWithUndo(context.TrainingLayoutAnchor.gameObject, true, "Enable training layout anchor");
        SetActiveWithUndo(context.SelectionRoot.gameObject, true, "Show selection root");
        SetActiveWithUndo(context.TrainingRoot.gameObject, false, "Hide training root");
        SetActiveWithUndo(context.ResultRoot.gameObject, false, "Hide result root");
        SetActiveWithUndo(context.VideoPanel, false, "Hide video on selection page");
    }

    private static bool ValidateContext(SceneContext context, bool logResult)
    {
        var issues = new List<string>();

        ValidatePageRoot(context.SelectionRoot, context.SelectionPanel, "SelectionPanelRoot", issues);
        ValidatePageRoot(context.TrainingRoot, context.TrainingPanel, "TrainingFunctionPanelRoot", issues);
        ValidatePageRoot(context.ResultRoot, context.ResultPanel, "ResultPanelRoot", issues);
        ValidateUniqueNamedObject(context.Scene, "RehabUIRoot", context.RehabUiRoot != null ? context.RehabUiRoot.gameObject : null, issues);
        ValidateUniqueNamedObject(context.Scene, "SelectionPanelRoot", context.SelectionRoot != null ? context.SelectionRoot.gameObject : null, issues);
        ValidateUniqueNamedObject(
            context.Scene,
            "TrainingLayoutAnchor",
            context.TrainingLayoutAnchor != null ? context.TrainingLayoutAnchor.gameObject : null,
            issues);
        ValidateUniqueNamedObject(context.Scene, "TrainingFunctionPanelRoot", context.TrainingRoot != null ? context.TrainingRoot.gameObject : null, issues);
        ValidateUniqueNamedObject(context.Scene, "ResultPanelRoot", context.ResultRoot != null ? context.ResultRoot.gameObject : null, issues);
        ValidateUniqueNamedObject(context.Scene, "RehabVideoPanel", context.VideoPanel, issues);

        if (context.RehabUiRoot == null || context.VideoPanel == null || !context.VideoPanel.transform.IsChildOf(context.RehabUiRoot))
        {
            issues.Add("RehabVideoPanel must be under the unique RehabUIRoot after migration.");
        }

        if (context.SelectionRoot == null || context.SelectionRoot.parent != context.RehabUiRoot)
        {
            issues.Add("SelectionPanelRoot must be a direct child of RehabUIRoot.");
        }
        else if (context.SelectionRoot.GetComponent<Canvas>() == null)
        {
            issues.Add("SelectionPanelRoot needs its own World Space Canvas after leaving the shared page Canvas.");
        }

        if (context.TrainingLayoutAnchor == null || context.TrainingLayoutAnchor.parent != context.RehabUiRoot)
        {
            issues.Add("TrainingLayoutAnchor must be a direct child of RehabUIRoot.");
        }
        else
        {
            if (context.TrainingRoot == null || context.TrainingRoot.parent != context.TrainingLayoutAnchor)
            {
                issues.Add("TrainingFunctionPanelRoot must be a direct child of TrainingLayoutAnchor.");
            }

            if (context.VideoPanel == null || context.VideoPanel.transform.parent != context.TrainingLayoutAnchor)
            {
                issues.Add("RehabVideoPanel must be a direct child of TrainingLayoutAnchor.");
            }
        }

        if (context.TrainingRoot != null && context.TrainingRoot.GetComponent<Canvas>() == null)
        {
            issues.Add("TrainingFunctionPanelRoot needs its own World Space Canvas after leaving the selection Canvas.");
        }

        var videoPanels = FindSceneComponents<RehabVideoGuideController>(context.Scene)
            .Select(controller => controller.videoPanel)
            .Where(panel => panel != null)
            .Distinct()
            .ToList();
        if (videoPanels.Count != 1 || videoPanels[0] != context.VideoPanel)
        {
            issues.Add("RehabVideoPanel must remain unique and bound to the single RehabVideoGuideController.");
        }

        var videoPlayers = FindSceneComponents<VideoPlayer>(context.Scene);
        if (videoPlayers.Count != 1)
        {
            issues.Add("Expected exactly one VideoPlayer, found " + videoPlayers.Count + ".");
        }

        var eventSystems = FindSceneComponents<EventSystem>(context.Scene);
        if (eventSystems.Count != 1)
        {
            issues.Add("Expected exactly one EventSystem, found " + eventSystems.Count + ".");
        }

        if (context.SelectionRoot != null && context.TrainingRoot != null &&
            context.SelectionRoot.gameObject.activeInHierarchy && context.TrainingRoot.gameObject.activeInHierarchy)
        {
            issues.Add("SelectionPanelRoot and TrainingFunctionPanelRoot are active at the same time.");
        }

        if (context.VideoLayout == null || context.VideoLayout.videoQuad == null)
        {
            issues.Add("Video layout or VideoQuad reference is missing.");
        }
        else
        {
            if (context.VideoLayout.videoQuad.parent != context.VideoPanel.transform)
            {
                issues.Add("VideoQuad direct parent changed; expected RehabVideoPanel.");
            }

            var frame = context.VideoLayout.videoQuad.parent.Find("VideoClosedFrame");
            if (frame == null)
            {
                issues.Add("VideoClosedFrame is missing.");
            }
            else
            {
                var requiredBars = new[] { "FrameTop", "FrameBottom", "FrameLeft", "FrameRight" };
                for (var i = 0; i < requiredBars.Length; i++)
                {
                    var bar = frame.Find(requiredBars[i]);
                    if (bar == null)
                    {
                        issues.Add("VideoClosedFrame/" + requiredBars[i] + " is missing.");
                    }
                    else if (bar.GetComponent<Collider>() != null)
                    {
                        issues.Add("VideoClosedFrame/" + requiredBars[i] + " must not have a Collider.");
                    }
                }
            }
        }

        if (context.VideoGuide == null || context.VideoGuide.videoPanel != context.VideoPanel)
        {
            issues.Add("RehabVideoGuideController.videoPanel must reference the unique RehabVideoPanel.");
        }
        else
        {
            var guideQuad = context.VideoGuide.videoQuad;
            if (guideQuad == null || guideQuad.transform.parent != context.VideoPanel.transform)
            {
                issues.Add("RehabVideoGuideController.videoQuad must remain a direct child of RehabVideoPanel.");
            }

            if (guideQuad != null && context.VideoGuide.displayRoot != null &&
                guideQuad.transform.IsChildOf(context.VideoGuide.displayRoot.transform))
            {
                issues.Add("VideoQuad must not be a child of displayRoot/VideoCanvas.");
            }

            if (context.VideoGuide.videoPlayer == null || context.VideoGuide.renderTexture == null ||
                context.VideoGuide.videoPlayer.targetTexture != context.VideoGuide.renderTexture)
            {
                issues.Add("VideoPlayer.targetTexture and RehabVideoGuideController.renderTexture must reference the same RenderTexture.");
            }

            if (context.VideoGuide.rawImage != null &&
                context.VideoGuide.rawImage.texture != context.VideoGuide.renderTexture)
            {
                issues.Add("VideoCanvas RawImage must reference the configured rehab RenderTexture.");
            }

            var quadMaterial = context.VideoGuide.videoQuadRenderer != null
                ? context.VideoGuide.videoQuadRenderer.sharedMaterial
                : null;
            if (quadMaterial == null || quadMaterial.mainTexture != context.VideoGuide.renderTexture)
            {
                issues.Add("VideoQuad material mainTexture must reference the configured rehab RenderTexture.");
            }
        }

        if (context.Placement == null ||
            context.Placement.viewTransform == null ||
            context.Placement.selectionPanelRoot == null ||
            context.Placement.trainingLayoutAnchor == null ||
            context.Placement.trainingFunctionPanelRoot == null ||
            context.Placement.videoPanelRoot == null ||
            context.Placement.videoLayoutController == null ||
            !context.Placement.useSceneAuthoredTrainingLayout)
        {
            issues.Add("RehabPanelPlacementController separated-layout references are incomplete.");
        }
        else if (context.Placement.viewTransform != context.ViewTransform ||
                 context.Placement.selectionPanelRoot != context.SelectionRoot ||
                 context.Placement.trainingLayoutAnchor != context.TrainingLayoutAnchor ||
                 context.Placement.trainingFunctionPanelRoot != context.TrainingRoot ||
                 context.Placement.videoPanelRoot != context.VideoPanel.transform ||
                 context.Placement.videoLayoutController != context.VideoLayout)
        {
            issues.Add("RehabPanelPlacementController separated-layout references do not match the migrated objects.");
        }

        if (context.ModeSelect == null ||
            context.ModeSelect.SelectionPanelRoot == null ||
            context.ModeSelect.TrainingFunctionPanelRoot == null ||
            context.ModeSelect.ResultPanelRoot == null ||
            context.ModeSelect.VideoPanelRoot == null)
        {
            issues.Add("RehabModeSelectUI separated page-root references are incomplete.");
        }
        else if (context.ModeSelect.SelectionPanelRoot != context.SelectionRoot.gameObject ||
                 context.ModeSelect.TrainingFunctionPanelRoot != context.TrainingRoot.gameObject ||
                 context.ModeSelect.ResultPanelRoot != context.ResultRoot.gameObject ||
                 context.ModeSelect.VideoPanelRoot != context.VideoPanel)
        {
            issues.Add("RehabModeSelectUI page-root references do not match the migrated objects.");
        }

        if (context.VideoLayout != null && context.VideoLayout.videoQuad != null && context.TrainingPanel != null)
        {
            var videoCenter = context.VideoLayout.videoQuad.position;
            var trainingRect = context.TrainingPanel.transform as RectTransform;
            var trainingCenter = trainingRect != null
                ? trainingRect.TransformPoint(trainingRect.rect.center)
                : context.TrainingRoot.position;
            var direction = Vector3.ProjectOnPlane(trainingCenter - videoCenter, Vector3.up);
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                var gap = Vector3.Distance(
                              Vector3.ProjectOnPlane(videoCenter, Vector3.up),
                              Vector3.ProjectOnPlane(trainingCenter, Vector3.up)) -
                          GetVideoProjectedExtent(context.VideoLayout.videoQuad, direction) -
                          GetRectProjectedExtent(trainingRect, trainingCenter, direction);
                if (gap + 0.01f < context.Placement.minPanelSeparationMeters)
                {
                    issues.Add("TrainingFunctionPanelRoot and RehabVideoPanel visual gap is only " + gap.ToString("0.000") + " m.");
                }
            }
        }

        if (context.SpatialControl != null &&
            (context.SpatialControl.createVisibleVideoControls || context.SpatialControl.showDebugVideoControls))
        {
            issues.Add("Visible/debug video controls must be disabled by default.");
        }

        foreach (var root in context.Scene.GetRootGameObjects())
        {
            foreach (var gameObject in root.GetComponentsInChildren<Transform>(true).Select(transform => transform.gameObject))
            {
                var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missingCount > 0)
                {
                    issues.Add(GetHierarchyPath(gameObject.transform) + " has " + missingCount + " missing script(s).");
                }
            }
        }

        if (issues.Count == 0)
        {
            if (logResult)
            {
                Debug.Log("[RehabPanelSeparation] Validation passed. " + DescribeHierarchy(context));
            }
            return true;
        }

        if (logResult)
        {
            Debug.LogError("[RehabPanelSeparation] Validation found " + issues.Count + " issue(s):\n- " + string.Join("\n- ", issues));
        }
        return false;
    }

    private static void ValidatePageRoot(Transform root, GameObject expectedPage, string rootName, List<string> issues)
    {
        if (root == null)
        {
            issues.Add(rootName + " is missing.");
            return;
        }

        if (root.name != rootName)
        {
            issues.Add("Expected root name " + rootName + ", found " + root.name + ".");
        }

        if (expectedPage == null || expectedPage.transform.parent != root)
        {
            issues.Add(rootName + " does not directly contain its expected page.");
        }

        if (root.childCount != 1)
        {
            issues.Add(rootName + " must contain only its page, found " + root.childCount + " direct children.");
        }
    }

    private static void ValidateUniqueNamedObject(Scene scene, string exactName, GameObject expected, List<string> issues)
    {
        var matches = FindSceneTransforms(scene)
            .Where(transform => transform.name == exactName)
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToList();
        if (matches.Count != 1 || matches[0] != expected)
        {
            issues.Add("Expected one " + exactName + " matching the migrated reference, found " + matches.Count + ".");
        }
    }

    private static Transform EnsureRectPageRoot(
        Transform canvasRoot,
        Transform existingRoot,
        string name,
        bool preserveExistingParent = false)
    {
        var canvasRect = canvasRoot as RectTransform;
        var desiredSize = canvasRect != null ? canvasRect.rect.size : Vector2.zero;
        if (existingRoot != null)
        {
            if (!preserveExistingParent && existingRoot.parent != canvasRoot)
            {
                ReparentPreservingWorld(existingRoot, canvasRoot, "Restore " + name + " under page Canvas");
            }

            if (existingRoot is RectTransform existingRect)
            {
                Undo.RecordObject(existingRect, "Configure " + name + " bounds");
                existingRect.anchorMin = new Vector2(0.5f, 0.5f);
                existingRect.anchorMax = new Vector2(0.5f, 0.5f);
                existingRect.pivot = new Vector2(0.5f, 0.5f);
                existingRect.sizeDelta = desiredSize;
            }
            return existingRoot;
        }

        var rootObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootObject, "Create " + name);
        var rectTransform = rootObject.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasRoot, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition3D = Vector3.zero;
        rectTransform.sizeDelta = desiredSize;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static Transform CreateTransformRoot(string name, Transform parent)
    {
        var rootObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(rootObject, "Create " + name);
        rootObject.transform.SetParent(parent, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private static void EnsureClosedFrameHierarchy(Transform videoParent)
    {
        var frame = videoParent.Find("VideoClosedFrame");
        if (frame == null)
        {
            frame = CreateTransformRoot("VideoClosedFrame", videoParent);
        }

        var barNames = new[] { "FrameTop", "FrameBottom", "FrameLeft", "FrameRight" };
        for (var i = 0; i < barNames.Length; i++)
        {
            var bar = frame.Find(barNames[i]);
            if (bar == null)
            {
                var barObject = new GameObject(barNames[i], typeof(MeshFilter), typeof(MeshRenderer));
                Undo.RegisterCreatedObjectUndo(barObject, "Create " + barNames[i]);
                barObject.transform.SetParent(frame, false);
                bar = barObject.transform;
            }

            foreach (var collider in bar.GetComponents<Collider>())
            {
                Undo.DestroyObjectImmediate(collider);
            }
        }
    }

    private static void EnsureMinimumTrainingPanelSeparation(SceneContext context, Vector3 trainingSlotForward)
    {
        var minimumGap = Mathf.Max(0f, context.Placement.minPanelSeparationMeters);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var videoCenter = context.VideoLayout.videoQuad != null
                ? context.VideoLayout.videoQuad.position
                : context.VideoPanel.transform.position;
            var trainingRect = context.TrainingPanel.transform as RectTransform;
            var trainingCenter = trainingRect != null
                ? trainingRect.TransformPoint(trainingRect.rect.center)
                : context.TrainingRoot.position;
            var between = trainingCenter - videoCenter;
            between.y = 0f;
            if (between.sqrMagnitude < 0.0001f)
            {
                between = trainingSlotForward;
            }
            var separationDirection = between.normalized;
            var centerDistance = between.magnitude;
            var videoExtent = GetVideoProjectedExtent(context.VideoLayout.videoQuad, separationDirection);
            var trainingExtent = GetRectProjectedExtent(trainingRect, trainingCenter, separationDirection);
            var visualGap = centerDistance - videoExtent - trainingExtent;
            if (visualGap >= minimumGap) return;

            var slotDirection = Vector3.ProjectOnPlane(trainingSlotForward, Vector3.up).normalized;
            var effectiveness = Mathf.Max(0.2f, Vector3.Dot(slotDirection, separationDirection));
            var moveDistance = (minimumGap - visualGap + 0.01f) / effectiveness;
            Undo.RecordObject(context.TrainingRoot, "Separate training function panel");
            context.TrainingRoot.position += slotDirection * moveDistance;
        }
    }

    private static float GetVideoProjectedExtent(Transform videoQuad, Vector3 direction)
    {
        if (videoQuad == null) return 0f;
        var halfRight = videoQuad.TransformVector(Vector3.right * 0.5f);
        var halfUp = videoQuad.TransformVector(Vector3.up * 0.5f);
        return Mathf.Abs(Vector3.Dot(halfRight, direction)) + Mathf.Abs(Vector3.Dot(halfUp, direction));
    }

    private static float GetRectProjectedExtent(RectTransform rect, Vector3 center, Vector3 direction)
    {
        if (rect == null) return 0f;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var maximum = 0f;
        for (var i = 0; i < corners.Length; i++)
        {
            maximum = Mathf.Max(maximum, Mathf.Abs(Vector3.Dot(corners[i] - center, direction)));
        }
        return maximum;
    }

    private static Pose CreateSlotPose(Vector3 headPosition, Vector3 forward, float distance, float yawDegrees, float height)
    {
        var direction = Quaternion.AngleAxis(yawDegrees, Vector3.up) * forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = forward;
        direction.Normalize();
        var position = headPosition + direction * Mathf.Max(0.1f, distance);
        position.y = height;
        return new Pose(position, Quaternion.LookRotation(direction, Vector3.up));
    }

    private static void ApplyWorldPose(Transform target, Pose pose, string undoName)
    {
        Undo.RecordObject(target, undoName);
        target.position = pose.position;
        target.rotation = pose.rotation;
    }

    private static void ReparentPreservingWorld(Transform child, Transform newParent, string undoName)
    {
        if (child == null || newParent == null || child.parent == newParent) return;
        var position = child.position;
        var rotation = child.rotation;
        var scale = child.lossyScale;
        Undo.SetTransformParent(child, newParent, undoName);
        child.position = position;
        child.rotation = rotation;
        SetWorldScale(child, scale);
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        var parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.000001f ? value / divisor : value;
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }
        property.objectReferenceValue = value;
    }

    private static void SetActiveWithUndo(GameObject target, bool active, string undoName)
    {
        if (target == null || target.activeSelf == active) return;
        Undo.RecordObject(target, undoName);
        target.SetActive(active);
        EditorUtility.SetDirty(target);
    }

    private static void MarkContextDirty(SceneContext context)
    {
        EditorUtility.SetDirty(context.ModeSelect);
        EditorUtility.SetDirty(context.Placement);
        EditorUtility.SetDirty(context.VideoLayout);
        EditorUtility.SetDirty(context.VideoGuide);
        EditorUtility.SetDirty(context.Session);
        if (context.SpatialControl != null) EditorUtility.SetDirty(context.SpatialControl);
    }

    private static GameObject ResolveReferencedOrUniqueNamed(
        Scene scene,
        GameObject referenced,
        string exactName,
        out string error)
    {
        error = string.Empty;
        if (IsSceneObject(referenced, scene)) return referenced;

        var matches = FindSceneTransforms(scene)
            .Where(transform => transform.name == exactName)
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToList();
        if (matches.Count == 1)
        {
            Debug.LogWarning("[RehabPanelSeparation] Used exact-name fallback for unassigned " + exactName + ".");
            return matches[0];
        }

        error = matches.Count == 0
            ? "Could not resolve " + exactName + " from the serialized reference or exact name."
            : "Ambiguous exact-name fallback for " + exactName + ": found " + matches.Count + " objects.";
        return null;
    }

    private static T FindUniqueSceneComponent<T>(Scene scene, out string error) where T : Component
    {
        var components = FindSceneComponents<T>(scene);
        if (components.Count == 1)
        {
            error = string.Empty;
            return components[0];
        }

        error = "Expected exactly one " + typeof(T).Name + ", found " + components.Count + ".";
        return null;
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        var results = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return results;
    }

    private static Transform ResolveMainCameraTransform(Scene scene, out string error)
    {
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject.scene == scene)
        {
            error = string.Empty;
            return mainCamera.transform;
        }

        var taggedCameras = FindSceneComponents<Camera>(scene)
            .Where(cameraComponent => cameraComponent.CompareTag("MainCamera"))
            .ToList();
        if (taggedCameras.Count == 1)
        {
            error = string.Empty;
            return taggedCameras[0].transform;
        }

        error = "Expected exactly one XR Camera tagged MainCamera, found " + taggedCameras.Count + ".";
        return null;
    }

    private static List<Transform> FindSceneTransforms(Scene scene)
    {
        return FindSceneComponents<Transform>(scene);
    }

    private static bool IsSceneComponent(Component component, Scene scene)
    {
        return component != null && component.gameObject.scene == scene;
    }

    private static bool IsSceneObject(GameObject gameObject, Scene scene)
    {
        return gameObject != null && gameObject.scene == scene;
    }

    private static Transform FindOwningCanvasTransform(Transform transform)
    {
        var current = transform;
        while (current != null)
        {
            if (current.GetComponent<Canvas>() != null) return current;
            current = current.parent;
        }
        return null;
    }

    private static Transform FindExpectedPageRoot(Scene scene, GameObject page, string rootName, out string error)
    {
        error = string.Empty;
        if (page != null && page.transform.parent != null && page.transform.parent.name == rootName)
        {
            return page.transform.parent;
        }

        var matches = FindSceneTransforms(scene).Where(transform => transform.name == rootName).ToList();
        if (matches.Count > 1)
        {
            error = "Ambiguous " + rootName + ": found " + matches.Count + " objects.";
            return null;
        }
        if (matches.Count == 1 && page != null && !page.transform.IsChildOf(matches[0]))
        {
            error = rootName + " exists but does not contain " + page.name + "; migration stopped to avoid guessing.";
            return null;
        }
        return matches.Count == 1 ? matches[0] : null;
    }

    private static Transform FindAncestorNamed(Transform start, string name)
    {
        var current = start;
        while (current != null)
        {
            if (current.name == name) return current;
            current = current.parent;
        }
        return null;
    }

    private static Transform FindLowestCommonAncestor(Transform first, Transform second)
    {
        var firstAncestors = new HashSet<Transform>();
        var current = first;
        while (current != null)
        {
            firstAncestors.Add(current);
            current = current.parent;
        }

        current = second;
        while (current != null)
        {
            if (firstAncestors.Contains(current)) return current;
            current = current.parent;
        }
        return null;
    }

    private static Transform GetDirectChildBelow(Transform descendant, Transform ancestor)
    {
        if (descendant == null || ancestor == null || descendant == ancestor) return null;
        var current = descendant;
        while (current.parent != null && current.parent != ancestor)
        {
            current = current.parent;
        }
        return current.parent == ancestor ? current : null;
    }

    private static string DescribeHierarchy(SceneContext context)
    {
        return "Selection=" + GetHierarchyPath(context.SelectionPanel.transform) +
               ", TrainingAnchor=" + GetHierarchyPath(context.TrainingLayoutAnchor) +
               ", Training=" + GetHierarchyPath(context.TrainingPanel.transform) +
               ", Result=" + GetHierarchyPath(context.ResultPanel.transform) +
               ", Video=" + GetHierarchyPath(context.VideoPanel.transform) + ".";
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null) return "<null>";
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
