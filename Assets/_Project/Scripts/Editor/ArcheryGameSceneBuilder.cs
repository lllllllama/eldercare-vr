using PicoElderCare.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public static class ArcheryGameSceneBuilder
{
    private const string ArcheryTrainingScenePath = "Assets/_Project/Scenes/03_ArcheryTraining.unity";
    private const string MaterialRoot = "Assets/_Project/Materials/Archery";
    private const string ElderCareUiFontPath = "Assets/_Project/Fonts/NotoSansCJKsc-Regular.otf";
    private const float BowStringLocalZ = -0.055f;

    // 与主入口 / 乒乓统一面板同一套视觉语言（ElderCareUiTheme）。
    private static readonly Color PanelBackgroundColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.08f), 1f);
    private static readonly Color RowChipColor = new Color(0.045f, 0.085f, 0.115f, 0.85f);
    private static readonly Color DifficultySelectedFill = Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Gold, 0.5f);
    private static readonly Color DifficultySelectedOutline = new Color(1f, 0.82f, 0.35f, 0.9f);

    [MenuItem("Tools/PICO ElderCare/Build Archery Training Scene")]
    public static void BuildArcheryTrainingScene()
    {
        if (!EnsureEditMode()) return;

        BuildArcheryTrainingSceneInternal();
        RehabSceneBuilder.ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Archery", "独立射箭训练场景已生成。", "OK");
        }
    }

    [MenuItem("Tools/PICO ElderCare/Archery/Repair Bow Orientation")]
    public static void RepairBowOrientation()
    {
        if (!EnsureEditMode()) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(ArcheryTrainingScenePath, OpenSceneMode.Single);
        var archeryRoot = FindTransformInScene(scene, "Archery");
        if (archeryRoot == null)
        {
            Debug.LogError($"Could not repair the bow because 'Archery' was not found in {ArcheryTrainingScenePath}.");
            return;
        }

        var arrowTemplate = FindDescendantByName(archeryRoot, "ArrowTemplate");
        var arrowContainer = FindDescendantByName(archeryRoot, "ArrowContainer");
        var trajectoryTransform = FindDescendantByName(archeryRoot, "TrajectoryHint");
        var trajectoryHint = trajectoryTransform != null
            ? trajectoryTransform.GetComponent<ArcheryTrajectoryHint>()
            : null;

        var bow = BuildBow(
            archeryRoot,
            arrowTemplate != null ? arrowTemplate.gameObject : null,
            arrowContainer,
            trajectoryHint);

        if (bow == null)
        {
            Debug.LogError("Bow orientation repair failed because BowController could not be configured.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ArcheryTrainingScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Archery bow orientation repaired without rebuilding the rest of the scene.");
    }

    internal static void BuildArcheryTrainingSceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RehabSceneBuilder.CreateMixedRealitySceneFoundation(
            "Managers",
            out var xrOrigin,
            out _);

        var manager = BuildArcheryModule();
        if (manager == null)
        {
            Debug.LogError("Archery training scene generation failed because ArcheryGameManager could not be created.");
            return;
        }

        EditorUtility.SetDirty(manager.gameObject);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);
        EditorSceneManager.SaveScene(scene, ArcheryTrainingScenePath);
    }

    public static ArcheryGameManager BuildArcheryModule()
    {
        EnsureFolderPath(MaterialRoot);

        var archeryRoot = GetOrCreate("Archery");
        archeryRoot.transform.position = Vector3.zero;
        archeryRoot.transform.rotation = Quaternion.identity;
        EnsureComponent<MrKeepVisible>(archeryRoot);

        BuildLaneVisuals(archeryRoot.transform);
        var target = BuildTargetRig(archeryRoot.transform, out var targetRig, out var targetHeightPivot);
        var arrowTemplate = BuildArrowTemplate(archeryRoot.transform);
        var arrowContainer = GetOrCreateChild("ArrowContainer", archeryRoot.transform).transform;
        var trajectoryHint = BuildTrajectoryHint(archeryRoot.transform);
        var bow = BuildBow(archeryRoot.transform, arrowTemplate, arrowContainer, trajectoryHint);
        var goldParticles = BuildHitParticles(archeryRoot.transform, "GoldHitParticles", new Color(1f, 0.84f, 0.25f), 1.9f, 0.05f);
        var dustParticles = BuildHitParticles(archeryRoot.transform, "HitDustParticles", new Color(0.92f, 0.86f, 0.7f), 0.9f, 0.035f);
        var panel = BuildScoreCanvas(archeryRoot.transform);

        var managers = GetOrCreate("Managers");
        var managerObject = GetOrCreateChild("ArcheryGameManager", managers.transform);
        var manager = EnsureComponent<ArcheryGameManager>(managerObject);
        if (manager == null) return null;

        manager.bow = bow;
        manager.target = target;
        manager.laneRoot = archeryRoot.transform;
        manager.targetRig = targetRig;
        manager.targetHeightPivot = targetHeightPivot;
        manager.headTransform = Camera.main != null ? Camera.main.transform : null;
        manager.arrowContainer = arrowContainer;
        manager.scorePanel = panel;
        manager.goldHitParticles = goldParticles;
        manager.hitDustParticles = dustParticles;
        manager.arrowsPerRound = 10;
        manager.difficulty = ArcheryDifficulty.Medium;
        manager.autoStartSessionOnStart = true;
        manager.alignLaneToUserOnStart = true;
        manager.calibrateTargetHeightOnStart = true;
        manager.spawnScorePopups = true;
        manager.enableAimAssist = true;
        manager.aimAssistDegrees = ArcheryGeometry.AimAssistDefaultDegrees;
        manager.popupFont = AssetDatabase.LoadAssetAtPath<Font>(ElderCareUiFontPath);

        BuildAudioManager(managers.transform, bow);

        if (panel != null)
        {
            panel.manager = manager;
            panel.bow = bow;
            EditorUtility.SetDirty(panel.gameObject);
        }

        EditorUtility.SetDirty(archeryRoot);
        EditorUtility.SetDirty(managerObject);
        return manager;
    }

    private static void BuildLaneVisuals(Transform archeryRoot)
    {
        var mat = CreatePrimitiveChild("ShootingMat", archeryRoot, PrimitiveType.Cube,
            new Vector3(0f, 0.006f, 0f), Vector3.zero, new Vector3(0.9f, 0.012f, 0.9f),
            CreateOrLoadMaterial("ArcheryShootingMat", new Color(0.13f, 0.19f, 0.38f)));
        RemovePrimitiveCollider(mat);

        var laneLength = ArcheryGeometry.FarTargetDistanceMeters + 1f;
        var lane = CreatePrimitiveChild("LaneStrip", archeryRoot, PrimitiveType.Cube,
            new Vector3(0f, 0.003f, laneLength * 0.5f + 0.6f), Vector3.zero, new Vector3(0.5f, 0.006f, laneLength),
            CreateOrLoadMaterial("ArcheryLaneStrip", new Color(0.16f, 0.34f, 0.22f)));
        RemovePrimitiveCollider(lane);
    }

    private static ArcheryTarget BuildTargetRig(Transform archeryRoot, out Transform targetRig, out Transform targetHeightPivot)
    {
        var rig = GetOrCreateChild("TargetRig", archeryRoot);
        rig.transform.localPosition = new Vector3(0f, 0f, ArcheryGeometry.MediumTargetDistanceMeters);
        rig.transform.localRotation = Quaternion.identity;
        targetRig = rig.transform;

        var standMaterial = CreateOrLoadMaterial("ArcheryStandWood", new Color(0.5f, 0.36f, 0.2f));
        var stand = GetOrCreateChild("TargetStand", rig.transform);
        stand.transform.localPosition = Vector3.zero;
        stand.transform.localRotation = Quaternion.identity;
        var legLeft = CreatePrimitiveChild("LegLeft", stand.transform, PrimitiveType.Cube,
            new Vector3(-0.42f, 0.95f, 0.12f), new Vector3(12f, 0f, 0f), new Vector3(0.05f, 1.95f, 0.05f), standMaterial);
        RemovePrimitiveCollider(legLeft);
        var legRight = CreatePrimitiveChild("LegRight", stand.transform, PrimitiveType.Cube,
            new Vector3(0.42f, 0.95f, 0.12f), new Vector3(12f, 0f, 0f), new Vector3(0.05f, 1.95f, 0.05f), standMaterial);
        RemovePrimitiveCollider(legRight);
        var crossBar = CreatePrimitiveChild("CrossBar", stand.transform, PrimitiveType.Cube,
            new Vector3(0f, 0.5f, 0.16f), Vector3.zero, new Vector3(0.9f, 0.05f, 0.05f), standMaterial);
        RemovePrimitiveCollider(crossBar);

        var heightPivot = GetOrCreateChild("TargetHeightPivot", rig.transform);
        heightPivot.transform.localPosition = new Vector3(0f, ArcheryGeometry.DefaultSeatedEyeHeightMeters, 0f);
        heightPivot.transform.localRotation = Quaternion.identity;
        targetHeightPivot = heightPivot.transform;

        var face = GetOrCreateChild("TargetFace", heightPivot.transform);
        face.transform.localPosition = Vector3.zero;
        face.transform.localRotation = Quaternion.identity;

        var collider = EnsureComponent<BoxCollider>(face);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = new Vector3(
                ArcheryGeometry.TargetBackboardSizeMeters,
                ArcheryGeometry.TargetBackboardSizeMeters,
                ArcheryGeometry.TargetBackboardThicknessMeters);
        }

        var backboard = CreatePrimitiveChild("Backboard", face.transform, PrimitiveType.Cube,
            Vector3.zero, Vector3.zero,
            new Vector3(ArcheryGeometry.TargetBackboardSizeMeters, ArcheryGeometry.TargetBackboardSizeMeters, 0.05f),
            CreateOrLoadMaterial("ArcheryBackboard", new Color(0.87f, 0.78f, 0.56f)));
        RemovePrimitiveCollider(backboard);

        var ringColors = new[]
        {
            new Color(0.95f, 0.95f, 0.93f),
            new Color(0.13f, 0.13f, 0.15f),
            new Color(0.21f, 0.45f, 0.83f),
            new Color(0.85f, 0.2f, 0.17f),
            new Color(0.96f, 0.8f, 0.16f)
        };
        var ringNames = new[] { "Ring_White", "Ring_Black", "Ring_Blue", "Ring_Red", "Ring_Gold" };
        for (var i = 0; i < ringColors.Length; i++)
        {
            var diameter = ArcheryGeometry.TargetFaceRadiusMeters * 2f * (ArcheryGeometry.TargetScoreBands - i) / ArcheryGeometry.TargetScoreBands;
            var ring = CreatePrimitiveChild(ringNames[i], face.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0f, -0.028f - 0.003f * i), new Vector3(90f, 0f, 0f),
                new Vector3(diameter, 0.006f, diameter),
                CreateOrLoadMaterial($"Archery{ringNames[i]}", ringColors[i]));
            RemovePrimitiveCollider(ring);
        }

        var stuckArrows = GetOrCreateChild("StuckArrows", face.transform);
        stuckArrows.transform.localPosition = Vector3.zero;
        stuckArrows.transform.localRotation = Quaternion.identity;

        var target = EnsureComponent<ArcheryTarget>(face);
        if (target != null)
        {
            target.faceCenter = face.transform;
            target.faceRadiusMeters = ArcheryGeometry.TargetFaceRadiusMeters;
            target.scoreBands = ArcheryGeometry.TargetScoreBands;
            target.maxRingScore = ArcheryGeometry.TargetMaxRingScore;
            target.stickParent = stuckArrows.transform;
        }

        EditorUtility.SetDirty(rig);
        return target;
    }

    private static GameObject BuildArrowTemplate(Transform archeryRoot)
    {
        var template = GetOrCreateChild("ArrowTemplate", archeryRoot);
        template.transform.localPosition = new Vector3(0f, -10f, 0f);
        template.transform.localRotation = Quaternion.identity;

        BuildArrowVisualChildren(template.transform);
        BuildArrowTrail(template.transform);

        var projectile = EnsureComponent<ArrowProjectile>(template);
        if (projectile != null)
        {
            projectile.gravityMetersPerSecondSquared = ArcheryGeometry.ArrowGravityMetersPerSecondSquared;
            projectile.linearDragPerSecond = ArcheryGeometry.ArrowLinearDragPerSecond;
            projectile.arrowLengthMeters = ArcheryGeometry.ArrowLengthMeters;
            projectile.castRadiusMeters = ArcheryGeometry.ArrowRadiusMeters;
            projectile.stickDepthMeters = ArcheryGeometry.ArrowStickDepthMeters;
            projectile.maxFlightSeconds = ArcheryGeometry.ArrowMaxFlightSeconds;
            projectile.missFloorY = -0.5f;
            projectile.hitLayers = BuildArrowHitLayerMask();
            EditorUtility.SetDirty(projectile);
        }

        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static void BuildArrowTrail(Transform arrowRoot)
    {
        var trailGo = GetOrCreateChild("Trail", arrowRoot);
        trailGo.transform.localPosition = new Vector3(0f, 0f, ArcheryGeometry.ArrowLengthMeters * 0.5f);
        trailGo.transform.localRotation = Quaternion.identity;

        var trail = EnsureComponent<TrailRenderer>(trailGo);
        if (trail == null) return;

        trail.time = 0.22f;
        trail.startWidth = 0.016f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = CreateOrLoadLineMaterial("ArcheryArrowTrail", new Color(0.85f, 0.95f, 1f, 0.8f));

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.55f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
    }

    private static ArcheryTrajectoryHint BuildTrajectoryHint(Transform archeryRoot)
    {
        var hintGo = GetOrCreateChild("TrajectoryHint", archeryRoot);
        hintGo.transform.localPosition = Vector3.zero;
        hintGo.transform.localRotation = Quaternion.identity;

        var line = EnsureComponent<LineRenderer>(hintGo);
        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.startWidth = 0.014f;
            line.endWidth = 0.006f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = CreateOrLoadLineMaterial("ArcheryTrajectory", new Color(0.45f, 0.92f, 0.85f, 0.7f));

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.45f, 0.92f, 0.85f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.92f, 0.85f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.65f, 0f),
                    new GradientAlphaKey(0.05f, 1f)
                });
            line.colorGradient = gradient;
            line.enabled = false;
        }

        var hint = EnsureComponent<ArcheryTrajectoryHint>(hintGo);
        if (hint != null)
        {
            hint.line = line;
            hint.gravityMetersPerSecondSquared = ArcheryGeometry.ArrowGravityMetersPerSecondSquared;
            hint.linearDragPerSecond = ArcheryGeometry.ArrowLinearDragPerSecond;
            hint.stepSeconds = ArcheryGeometry.TrajectoryPreviewStepSeconds;
            hint.maxSeconds = ArcheryGeometry.TrajectoryPreviewMaxSeconds;
        }

        EditorUtility.SetDirty(hintGo);
        return hint;
    }

    private static BowController BuildBow(Transform archeryRoot, GameObject arrowTemplate, Transform arrowContainer, ArcheryTrajectoryHint trajectoryHint)
    {
        var bowObject = GetOrCreateChild("Bow", archeryRoot);
        bowObject.transform.localPosition = new Vector3(0.2f, 1.2f, 0.4f);
        bowObject.transform.localRotation = Quaternion.identity;

        // Bow +Z is always the launch direction. Only the authored bow body needs the
        // visual correction; string and nock references stay in Bow's functional space.
        var visualRoot = GetOrCreateUniqueBowSection("BowVisualRoot", bowObject.transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visualRoot.transform.localScale = Vector3.one;

        var stringRig = GetOrCreateUniqueBowSection("BowStringRig", bowObject.transform);
        stringRig.transform.localPosition = Vector3.zero;
        stringRig.transform.localRotation = Quaternion.identity;
        stringRig.transform.localScale = Vector3.one;

        // Migrate older generated hierarchies before creating or configuring anything.
        // This makes the builder repeatable and prevents one functional node from living
        // under both BowVisualRoot and BowStringRig.
        MoveUniqueBowNode("Riser", bowObject.transform, visualRoot.transform);
        MoveUniqueBowNode("Grip", bowObject.transform, visualRoot.transform);
        MoveUniqueBowNode("UpperLimb", bowObject.transform, visualRoot.transform);
        MoveUniqueBowNode("LowerLimb", bowObject.transform, visualRoot.transform);
        MoveUniqueBowNode("StringTopAnchor", bowObject.transform, stringRig.transform);
        MoveUniqueBowNode("StringBottomAnchor", bowObject.transform, stringRig.transform);
        MoveUniqueBowNode("NockRest", bowObject.transform, stringRig.transform);
        MoveUniqueBowNode("BowString", bowObject.transform, stringRig.transform);
        MoveUniqueBowNode("NockedArrowVisual", bowObject.transform, stringRig.transform);

        var riserMaterial = CreateOrLoadMaterial("ArcheryBowRiser", new Color(0.42f, 0.28f, 0.16f));
        var limbMaterial = CreateOrLoadMaterial("ArcheryBowLimb", new Color(0.56f, 0.4f, 0.24f));

        var riser = CreatePrimitiveChild("Riser", visualRoot.transform, PrimitiveType.Cube,
            Vector3.zero, Vector3.zero, new Vector3(0.035f, 0.34f, 0.05f), riserMaterial);
        RemovePrimitiveCollider(riser);

        var grip = CreatePrimitiveChild("Grip", visualRoot.transform, PrimitiveType.Cube,
            new Vector3(0f, -0.02f, -0.008f), Vector3.zero, new Vector3(0.045f, 0.15f, 0.06f), riserMaterial);
        RemovePrimitiveCollider(grip);

        var upperLimb = CreatePrimitiveChild("UpperLimb", visualRoot.transform, PrimitiveType.Cube,
            new Vector3(0f, 0.32f, 0.02f), new Vector3(10f, 0f, 0f), new Vector3(0.028f, 0.46f, 0.02f), limbMaterial);
        RemovePrimitiveCollider(upperLimb);
        var lowerLimb = CreatePrimitiveChild("LowerLimb", visualRoot.transform, PrimitiveType.Cube,
            new Vector3(0f, -0.32f, 0.02f), new Vector3(-10f, 0f, 0f), new Vector3(0.028f, 0.46f, 0.02f), limbMaterial);
        RemovePrimitiveCollider(lowerLimb);

        var stringTop = GetOrCreateChild("StringTopAnchor", stringRig.transform);
        stringTop.transform.localPosition = new Vector3(0f, 0.53f, BowStringLocalZ);
        stringTop.transform.localRotation = Quaternion.identity;
        stringTop.transform.localScale = Vector3.one;
        var stringBottom = GetOrCreateChild("StringBottomAnchor", stringRig.transform);
        stringBottom.transform.localPosition = new Vector3(0f, -0.53f, BowStringLocalZ);
        stringBottom.transform.localRotation = Quaternion.identity;
        stringBottom.transform.localScale = Vector3.one;
        var nockRest = GetOrCreateChild("NockRest", stringRig.transform);
        nockRest.transform.localPosition = new Vector3(0f, 0f, BowStringLocalZ);
        nockRest.transform.localRotation = Quaternion.identity;
        nockRest.transform.localScale = Vector3.one;

        var stringObject = GetOrCreateChild("BowString", stringRig.transform);
        stringObject.transform.localPosition = Vector3.zero;
        stringObject.transform.localRotation = Quaternion.identity;
        stringObject.transform.localScale = Vector3.one;
        var stringLine = EnsureComponent<LineRenderer>(stringObject);
        if (stringLine != null)
        {
            stringLine.useWorldSpace = true;
            stringLine.positionCount = 3;
            stringLine.widthMultiplier = 0.006f;
            stringLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stringLine.receiveShadows = false;
            stringLine.sharedMaterial = CreateOrLoadLineMaterial("ArcheryBowString", new Color(0.92f, 0.92f, 0.9f, 1f));
            stringLine.SetPosition(0, stringTop.transform.position);
            stringLine.SetPosition(1, nockRest.transform.position);
            stringLine.SetPosition(2, stringBottom.transform.position);
        }

        var nockedArrow = GetOrCreateChild("NockedArrowVisual", stringRig.transform);
        nockedArrow.transform.localPosition = new Vector3(0f, 0f, BowStringLocalZ);
        nockedArrow.transform.localRotation = Quaternion.identity;
        nockedArrow.transform.localScale = Vector3.one;
        BuildArrowVisualChildren(nockedArrow.transform);
        nockedArrow.SetActive(false);

        BuildBowAudioSources(bowObject.transform);

        var bow = EnsureComponent<BowController>(bowObject);
        if (bow != null)
        {
            bow.bowHandTransform = FindControllerTransform(false);
            bow.stringHandTransform = FindControllerTransform(true);
            bow.bowHandNode = XRNode.LeftHand;
            bow.stringHandNode = XRNode.RightHand;
            bow.bowInLeftHand = true;
            bow.autoCreateDrawInputSource = true;
            bow.headTransform = Camera.main != null ? Camera.main.transform : null;
            bow.keepForwardAwayFromUserAtRest = true;
            bow.nockRest = nockRest.transform;
            bow.stringTopAnchor = stringTop.transform;
            bow.stringBottomAnchor = stringBottom.transform;
            bow.upperLimbTransform = upperLimb.transform;
            bow.lowerLimbTransform = lowerLimb.transform;
            bow.limbBendDegrees = ArcheryGeometry.BowLimbBendDegrees;
            bow.stringLine = stringLine;
            bow.nockedArrowVisual = nockedArrow.transform;
            bow.restSeparationMeters = ArcheryGeometry.DrawRestSeparationMeters;
            bow.maxDrawLengthMeters = ArcheryGeometry.MaxDrawLengthMeters;
            bow.minFireDraw01 = ArcheryGeometry.MinFireDraw01;
            bow.nockCatchRadiusMeters = ArcheryGeometry.NockCatchRadiusMeters;
            bow.aimSmoothingSeconds = ArcheryGeometry.AimSmoothingSeconds;
            bow.arrowTemplate = arrowTemplate;
            bow.arrowContainer = arrowContainer;
            bow.minLaunchSpeed = ArcheryGeometry.MinLaunchSpeedMetersPerSecond;
            bow.maxLaunchSpeed = ArcheryGeometry.MaxLaunchSpeedMetersPerSecond;
            bow.trajectoryHint = trajectoryHint;
            bow.showTrajectoryPreview = true;

            if (bow.bowHandTransform == null)
            {
                Debug.Log("Left hand controller not auto-bound for archery bow. Please assign XR Origin left controller to BowController.bowHandTransform manually.");
            }

            if (bow.stringHandTransform == null)
            {
                Debug.Log("Right hand controller not auto-bound for archery bow. Please assign XR Origin right controller to BowController.stringHandTransform manually.");
            }
        }

        EditorUtility.SetDirty(visualRoot);
        EditorUtility.SetDirty(stringRig);
        EditorUtility.SetDirty(bowObject);
        return bow;
    }

    private static void BuildArrowVisualChildren(Transform arrowRoot)
    {
        var shaft = CreatePrimitiveChild("Shaft", arrowRoot, PrimitiveType.Cylinder,
            new Vector3(0f, 0f, ArcheryGeometry.ArrowLengthMeters * 0.5f), new Vector3(90f, 0f, 0f),
            new Vector3(0.012f, ArcheryGeometry.ArrowLengthMeters * 0.5f - 0.01f, 0.012f),
            CreateOrLoadMaterial("ArcheryArrowShaft", new Color(0.76f, 0.6f, 0.35f)));
        RemovePrimitiveCollider(shaft);

        var tip = CreatePrimitiveChild("Tip", arrowRoot, PrimitiveType.Sphere,
            new Vector3(0f, 0f, ArcheryGeometry.ArrowLengthMeters), Vector3.zero,
            new Vector3(0.022f, 0.022f, 0.022f),
            CreateOrLoadMaterial("ArcheryArrowTip", new Color(0.55f, 0.57f, 0.6f)));
        RemovePrimitiveCollider(tip);

        var fletchMaterial = CreateOrLoadMaterial("ArcheryArrowFletch", new Color(0.85f, 0.22f, 0.2f));
        var fletchVertical = CreatePrimitiveChild("Fletch_Vertical", arrowRoot, PrimitiveType.Cube,
            new Vector3(0f, 0f, 0.07f), Vector3.zero, new Vector3(0.003f, 0.06f, 0.08f), fletchMaterial);
        RemovePrimitiveCollider(fletchVertical);
        var fletchHorizontal = CreatePrimitiveChild("Fletch_Horizontal", arrowRoot, PrimitiveType.Cube,
            new Vector3(0f, 0f, 0.07f), new Vector3(0f, 0f, 90f), new Vector3(0.003f, 0.06f, 0.08f), fletchMaterial);
        RemovePrimitiveCollider(fletchHorizontal);

        var nock = CreatePrimitiveChild("Nock", arrowRoot, PrimitiveType.Sphere,
            Vector3.zero, Vector3.zero, new Vector3(0.016f, 0.016f, 0.016f), fletchMaterial);
        RemovePrimitiveCollider(nock);
    }

    private static void BuildBowAudioSources(Transform bowTransform)
    {
        ConfigureBowAudioSource(GetOrCreateChild("BowAudio_Main", bowTransform));
        ConfigureBowAudioSource(GetOrCreateChild("BowAudio_Tick", bowTransform));
    }

    private static void ConfigureBowAudioSource(GameObject audioObject)
    {
        audioObject.transform.localPosition = Vector3.zero;
        var source = EnsureComponent<AudioSource>(audioObject);
        if (source == null) return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = 0.6f;
        source.maxDistance = 18f;
        source.rolloffMode = AudioRolloffMode.Linear;
        EditorUtility.SetDirty(audioObject);
    }

    private static void BuildAudioManager(Transform managers, BowController bow)
    {
        var audioObject = GetOrCreateChild("ArcheryAudioManager", managers);
        var audioManager = EnsureComponent<ArcheryAudioManager>(audioObject);
        if (audioManager == null) return;

        var bowTransform = bow != null ? bow.transform : null;
        audioManager.bowSource = FindChildComponent<AudioSource>(bowTransform, "BowAudio_Main");
        audioManager.drawTickSource = FindChildComponent<AudioSource>(bowTransform, "BowAudio_Tick");
        audioManager.volume = 0.85f;
        audioManager.drawTickInterval01 = 0.08f;
        EditorUtility.SetDirty(audioObject);
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null) return null;

        var child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static ParticleSystem BuildHitParticles(Transform archeryRoot, string name, Color color, float speed, float size)
    {
        var particleGo = GetOrCreateChild(name, archeryRoot);
        particleGo.transform.localPosition = new Vector3(0f, -5f, 0f);
        particleGo.transform.localRotation = Quaternion.identity;

        var particles = EnsureComponent<ParticleSystem>(particleGo);
        if (particles == null) return null;

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.55f;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.55f;
        main.maxParticles = 256;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.045f;

        var renderer = particleGo.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = LoadParticleMaterial();
        }

        EditorUtility.SetDirty(particleGo);
        return particles;
    }

    private static Material LoadParticleMaterial()
    {
        // 不依赖内置资源名（各 Unity 版本不一致），自己生成软圆光斑贴图 + 内置管线粒子材质，
        // 确保被打进包且粒子不是方块。
        var matPath = $"{MaterialRoot}/ArcheryParticle.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;

        var texture = CreateOrLoadSoftCircleTexture();
        var shader =
            Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return CreateOrLoadLineMaterial("ArcheryParticleFallback", Color.white);
        }

        material = new Material(shader);
        if (texture != null)
        {
            material.mainTexture = texture;
        }

        EnsureFolderPath(MaterialRoot);
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    private static Texture2D CreateOrLoadSoftCircleTexture()
    {
        var texturePath = $"{MaterialRoot}/ArcherySoftParticle.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (existing != null) return existing;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var radial = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                var alpha = Mathf.Clamp01(1f - radial);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        EnsureFolderPath(MaterialRoot);
        System.IO.File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(texturePath);

        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    }

    private static ArcheryScorePanel BuildScoreCanvas(Transform archeryRoot)
    {
        var canvasGo = GetOrCreateChild("ArcheryScoreCanvas", archeryRoot);
        var canvas = EnsureComponent<Canvas>(canvasGo);
        if (canvas == null) return null;

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 18;

        var canvasRect = ConfigureRect(canvasGo, new Vector2(900f, 980f), Vector2.zero);
        canvasGo.transform.localPosition = new Vector3(-1.4f, 1.5f, 2.1f);
        canvasGo.transform.localRotation = Quaternion.Euler(0f, -26f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.0014f;

        EnsureComponent<GraphicRaycaster>(canvasGo);
        AddComponentIfTypeExists(canvasGo, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        EnsureUiEventSystem();

        // 画布内容全部由本方法生成：重建前清空旧子对象，
        // 避免旧版布局残留导致重叠错排，同时保证渲染顺序 = 创建顺序。
        for (var i = canvasRect.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(canvasRect.GetChild(i).gameObject);
        }

        // ---- 背板：与主入口面板同款（PanelStrong 底 + 青色描边 + 内辉光 + 氛围线 + 星点）----
        var background = CreateRoundedPanel(canvasRect, "Background", new Vector2(900f, 980f), Vector2.zero, PanelBackgroundColor, 44f);
        if (background != null)
        {
            var backgroundOutline = EnsureComponent<Outline>(background.gameObject);
            backgroundOutline.effectColor = WithAlpha(ElderCareUiTheme.PanelStroke, 0.72f);
            backgroundOutline.effectDistance = new Vector2(3f, -3f);
        }

        CreateRoundedPanel(canvasRect, "PanelInnerGlow", new Vector2(856f, 936f), Vector2.zero, WithAlpha(ElderCareUiTheme.Cyan, 0.055f), 40f);
        CreateRoundedPanel(canvasRect, "AmbientLineTop", new Vector2(800f, 3f), new Vector2(0f, 476f), WithAlpha(ElderCareUiTheme.Cyan, 0.2f), 2f);
        CreateRoundedPanel(canvasRect, "AmbientLineBottom", new Vector2(800f, 3f), new Vector2(0f, -466f), WithAlpha(ElderCareUiTheme.Cyan, 0.13f), 2f);
        CreateRoundedPanel(canvasRect, "AmbientLineLeft", new Vector2(3f, 800f), new Vector2(-432f, -40f), WithAlpha(ElderCareUiTheme.Cyan, 0.14f), 2f);
        CreateRoundedPanel(canvasRect, "AmbientLineRight", new Vector2(3f, 800f), new Vector2(432f, -40f), WithAlpha(ElderCareUiTheme.Green, 0.13f), 2f);

        CreateRoundedPanel(canvasRect, "Star_A", new Vector2(8f, 8f), new Vector2(-378f, 448f), new Color(1f, 1f, 1f, 0.42f), 4f);
        CreateRoundedPanel(canvasRect, "Star_B", new Vector2(7f, 7f), new Vector2(386f, 442f), new Color(1f, 1f, 1f, 0.34f), 4f);
        CreateRoundedPanel(canvasRect, "Star_C", new Vector2(6f, 6f), new Vector2(-370f, -446f), new Color(1f, 1f, 1f, 0.28f), 3f);
        CreateRoundedPanel(canvasRect, "Star_D", new Vector2(8f, 8f), new Vector2(376f, -440f), new Color(1f, 1f, 1f, 0.36f), 4f);

        // ---- 标题区：箭靶线性图标 + 标题 + 副标题 + 青色分隔线 ----
        var titleIconHalo = CreateRoundedPanel(canvasRect, "TitleIconHalo", new Vector2(76f, 76f), new Vector2(-238f, 428f), WithAlpha(ElderCareUiTheme.Gold, 0.12f), 38f);
        titleIconHalo.raycastTarget = false;
        var titleIconGo = GetOrCreateChild("TitleIcon", canvasRect);
        var titleIcon = EnsureComponent<ElderCareLineIcon>(titleIconGo);
        titleIcon.iconType = ElderCareIconType.Target;
        titleIcon.strokeWidth = 6f;
        titleIcon.color = WithAlpha(ElderCareUiTheme.Gold, 0.95f);
        titleIcon.raycastTarget = false;
        ConfigureRect(titleIconGo, new Vector2(52f, 52f), new Vector2(-238f, 428f));

        CreateText(canvasRect, "Title", "射箭训练", new Vector2(30f, 434f), new Vector2(480f, 72f), 54, FontStyle.Bold, ElderCareUiTheme.TextPrimary, TextAnchor.MiddleCenter);
        CreateText(canvasRect, "Subtitle", "双手拉弓 · 坐姿可玩", new Vector2(30f, 384f), new Vector2(480f, 38f), 24, FontStyle.Normal, ElderCareUiTheme.TextSecondary, TextAnchor.MiddleCenter);
        CreateRoundedPanel(canvasRect, "TitleDivider", new Vector2(420f, 4f), new Vector2(0f, 352f), WithAlpha(ElderCareUiTheme.Cyan, 0.48f), 3f);

        // ---- 数据行：行底片 + 彩色侧条 + 左标签 / 右数值 ----
        var scoreValue = CreateStatRow(canvasRect, "ScoreRow", "总分", "0 分", new Vector2(0f, 298f), ElderCareUiTheme.Gold, 46, ElderCareUiTheme.Gold);
        var arrowsValue = CreateStatRow(canvasRect, "ArrowsRow", "剩余箭数", "10 / 10", new Vector2(0f, 226f), ElderCareUiTheme.Cyan, 40, ElderCareUiTheme.TextPrimary);
        var lastHitValue = CreateStatRow(canvasRect, "LastHitRow", "上一箭", "--", new Vector2(0f, 154f), ElderCareUiTheme.Green, 40, ElderCareUiTheme.TextPrimary);
        var bestValue = CreateStatRow(canvasRect, "BestRow", "历史最佳", "--", new Vector2(0f, 82f), ElderCareUiTheme.Blue, 38, new Color(0.65f, 0.92f, 1f));
        var difficultyValue = CreateStatRow(canvasRect, "DifficultyRow", "目标距离", "中距 6 米", new Vector2(0f, 10f), ElderCareUiTheme.Violet, 36, ElderCareUiTheme.TextPrimary);

        // ---- 按钮区：难度（蓝，选中金色高亮）/ 辅助功能（青）/ 主操作（绿）+ 返回 ----
        var nearButton = CreateActionButton(canvasRect, "DifficultyNearButton", "近距", new Vector2(250f, 84f), new Vector2(-290f, -100f), ElderCareUiTheme.Blue, 0.05f, out _);
        var mediumButton = CreateActionButton(canvasRect, "DifficultyMediumButton", "中距", new Vector2(250f, 84f), new Vector2(0f, -100f), ElderCareUiTheme.Blue, 0.1f, out _);
        var farButton = CreateActionButton(canvasRect, "DifficultyFarButton", "远距", new Vector2(250f, 84f), new Vector2(290f, -100f), ElderCareUiTheme.Blue, 0.15f, out _);

        var assistButton = CreateActionButton(canvasRect, "AssistToggleButton", "辅助瞄准：开", new Vector2(280f, 84f), new Vector2(-300f, -205f), ElderCareUiTheme.Cyan, 0.2f, out var assistLabel);
        var handednessButton = CreateActionButton(canvasRect, "HandednessButton", "持弓手：左手", new Vector2(280f, 84f), new Vector2(0f, -205f), ElderCareUiTheme.Cyan, 0.25f, out var handednessLabel);
        var recenterButton = CreateActionButton(canvasRect, "RecenterButton", "重新对准", new Vector2(280f, 84f), new Vector2(300f, -205f), ElderCareUiTheme.Cyan, 0.3f, out _);

        var restartButton = CreateActionButton(canvasRect, "RestartButton", "再来一轮", new Vector2(380f, 100f), new Vector2(-210f, -322f), ElderCareUiTheme.Green, 0.35f, out _);
        var homeButton = CreateActionButton(canvasRect, "HomeButton", "返回健康游戏", new Vector2(380f, 100f), new Vector2(210f, -322f), ElderCareUiTheme.Violet, 0.4f, out var homeLabel);
        AddBackIconToButton(homeButton, homeLabel);

        // ---- 状态条 ----
        CreateRoundedPanel(canvasRect, "StatusChip", new Vector2(840f, 58f), new Vector2(0f, -430f), RowChipColor, 16f);
        var statusText = CreateText(canvasRect, "StatusText", "握紧右手手柄搭弦，向后拉再松开放箭", new Vector2(0f, -430f), new Vector2(800f, 52f), 26, FontStyle.Normal, ElderCareUiTheme.TextSecondary, TextAnchor.MiddleCenter);

        var panel = EnsureComponent<ArcheryScorePanel>(canvasGo);
        if (panel != null)
        {
            panel.uiFont = CreateReadableUiFont(64);
            panel.scoreValueText = scoreValue;
            panel.arrowsValueText = arrowsValue;
            panel.lastHitValueText = lastHitValue;
            panel.bestScoreValueText = bestValue;
            panel.difficultyValueText = difficultyValue;
            panel.statusText = statusText;
            panel.assistButtonLabel = assistLabel;
            panel.handednessButtonLabel = handednessLabel;
            panel.restartButton = restartButton;
            panel.homeButton = homeButton;
            panel.difficultyNearButton = nearButton;
            panel.difficultyMediumButton = mediumButton;
            panel.difficultyFarButton = farButton;
            panel.assistToggleButton = assistButton;
            panel.handednessButton = handednessButton;
            panel.recenterButton = recenterButton;
            panel.difficultyNormalFill = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.42f), 1f);
            panel.difficultyNormalOutline = WithAlpha(ElderCareUiTheme.Blue, 0.66f);
            panel.difficultySelectedFill = WithAlpha(DifficultySelectedFill, 1f);
            panel.difficultySelectedOutline = DifficultySelectedOutline;
        }

        EditorUtility.SetDirty(canvasGo);
        return panel;
    }

    private static Text CreateStatRow(
        RectTransform parent,
        string rowName,
        string label,
        string initialValue,
        Vector2 position,
        Color accentColor,
        int valueFontSize,
        Color valueColor)
    {
        var row = GetOrCreateChild(rowName, parent);
        var rowRect = ConfigureRect(row, new Vector2(830f, 64f), position);

        var chip = CreateRoundedPanel(rowRect, "Chip", new Vector2(830f, 64f), Vector2.zero, RowChipColor, 16f);
        chip.raycastTarget = false;

        var accent = CreateRoundedPanel(rowRect, "SideAccent", new Vector2(5f, 38f), new Vector2(-398f, 0f), WithAlpha(accentColor, 0.75f), 3f);
        accent.raycastTarget = false;

        CreateText(rowRect, "Label", label, new Vector2(-232f, 0f), new Vector2(280f, 56f), 30, FontStyle.Normal, ElderCareUiTheme.TextSecondary, TextAnchor.MiddleLeft);
        return CreateText(rowRect, "Value", initialValue, new Vector2(150f, 0f), new Vector2(460f, 60f), valueFontSize, FontStyle.Bold, valueColor, TextAnchor.MiddleRight);
    }

    private static Button CreateActionButton(
        RectTransform parent,
        string name,
        string label,
        Vector2 size,
        Vector2 position,
        Color baseColor,
        float entranceDelay,
        out Text labelText)
    {
        var root = GetOrCreateChild(name, parent);
        var rootRect = ConfigureRect(root, size, position);

        // 与主入口模块卡同款按钮：外辉光 + 主题渐变底 + 彩色描边 + 顶部青色高光线。
        var glow = CreateRoundedPanel(rootRect, "Glow", size + new Vector2(24f, 24f), Vector2.zero, WithAlpha(baseColor, 0.1f), 30f);
        glow.raycastTarget = false;

        var fillColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, baseColor, 0.42f), 1f);
        var panel = CreateRoundedPanel(rootRect, "Panel", size, Vector2.zero, fillColor, 22f);
        if (panel != null)
        {
            panel.raycastTarget = true;
        }

        var outline = EnsureComponent<Outline>(panel.gameObject);
        outline.effectColor = WithAlpha(baseColor, 0.66f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        var topHighlight = CreateRoundedPanel(rootRect, "TopHighlight", new Vector2(size.x - 42f, 3f), new Vector2(0f, size.y * 0.5f - 8f), WithAlpha(ElderCareUiTheme.Cyan, 0.28f), 2f);
        topHighlight.raycastTarget = false;

        labelText = CreateText(rootRect, "Label", label, Vector2.zero, new Vector2(size.x - 30f, size.y - 16f), Mathf.RoundToInt(size.y * 0.38f), FontStyle.Bold, ElderCareUiTheme.TextPrimary, TextAnchor.MiddleCenter);

        var button = EnsureComponent<Button>(root);
        if (button != null)
        {
            // 悬停/按压反馈交给 TechModuleCardMotion（与主入口卡片一致），Button 自身不做 tint。
            button.transition = Selectable.Transition.None;
            button.targetGraphic = panel;
        }

        var motion = EnsureComponent<TechModuleCardMotion>(root);
        if (motion != null)
        {
            motion.cardTransform = rootRect;
            motion.canvasGroup = EnsureComponent<CanvasGroup>(root);
            motion.cardGraphic = panel;
            motion.glowGraphic = glow;
            motion.edgeGraphic = topHighlight;
            motion.interactable = true;
            motion.playEntrance = true;
            motion.entranceDelay = entranceDelay;
            motion.ambientMotion = false;
            motion.normalColor = fillColor;
            motion.hoverColor = WithAlpha(Color.Lerp(fillColor, baseColor, 0.32f), 0.98f);
            motion.pressedColor = Color.Lerp(fillColor, Color.black, 0.18f);
            motion.glowColor = WithAlpha(baseColor, 0.22f);
            motion.edgeColor = WithAlpha(ElderCareUiTheme.Cyan, 0.36f);
            motion.hoverScale = ElderCareUiTheme.HoverScale;
            motion.pressedScale = ElderCareUiTheme.PressedScale;
        }

        EditorUtility.SetDirty(root);
        return button;
    }

    private static void AddBackIconToButton(Button button, Text label)
    {
        if (button == null) return;

        var rootRect = button.transform as RectTransform;
        if (rootRect == null) return;

        var iconGo = GetOrCreateChild("BackIcon", rootRect);
        var icon = EnsureComponent<ElderCareLineIcon>(iconGo);
        icon.iconType = ElderCareIconType.ArrowLeft;
        icon.strokeWidth = 6f;
        icon.color = ElderCareUiTheme.TextPrimary;
        icon.raycastTarget = false;
        ConfigureRect(iconGo, new Vector2(34f, 34f), new Vector2(-150f, 0f));

        if (label != null)
        {
            // 给左侧图标让位，文字整体右移。
            var labelRect = label.rectTransform;
            labelRect.anchoredPosition = new Vector2(22f, 0f);
            labelRect.sizeDelta = new Vector2(rootRect.sizeDelta.x - 110f, labelRect.sizeDelta.y);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static LayerMask BuildArrowHitLayerMask()
    {
        // RoomSensing 是 MR 房间感知网格层：它的渲染体被透视背景抑制器隐藏，
        // 碰撞体却还在——不剔除的话箭会撞上“隐形墙”凭空消失，MR 下基本不可玩。
        var mask = ~0;
        foreach (var layerName in new[] { "Controller", "Racket", "PlayerBody", "TableSafetyZone", "RoomSensing" })
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                mask &= ~(1 << layer);
            }
        }

        return mask;
    }

    private static GameObject CreatePrimitiveChild(
        string name,
        Transform parent,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localEuler,
        Vector3 localScale,
        Material material)
    {
        var existing = parent != null ? parent.Find(name) : null;
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
            go.transform.SetParent(parent, false);
        }

        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return go;
    }

    private static void RemovePrimitiveCollider(GameObject go)
    {
        if (go == null) return;

        foreach (var collider in go.GetComponents<Collider>())
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static Transform FindControllerTransform(bool rightHand)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            var n = t.name.ToLowerInvariant();
            if (rightHand && (n.Contains("righthand") || n.Contains("right controller") || n.Contains("rightcontroller") || n == "right"))
            {
                return t;
            }

            if (!rightHand && (n.Contains("lefthand") || n.Contains("left controller") || n.Contains("leftcontroller") || n == "left"))
            {
                return t;
            }
        }

        return null;
    }

    private static ElderCareRoundedPanel CreateRoundedPanel(RectTransform parent, string name, Vector2 size, Vector2 position, Color color, float radius)
    {
        var go = GetOrCreateChild(name, parent);
        var images = go.GetComponents<Image>();
        foreach (var image in images)
        {
            Object.DestroyImmediate(image);
        }

        var panel = EnsureComponent<ElderCareRoundedPanel>(go);
        ConfigureRect(go, size, position);
        if (panel != null)
        {
            panel.color = color;
            panel.cornerRadius = radius;
            panel.raycastTarget = false;
        }

        return panel;
    }

    private static Text CreateText(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        var go = GetOrCreateChild(name, parent);
        var text = EnsureComponent<Text>(go);
        ConfigureRect(go, size, position);

        if (text != null)
        {
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.font = CreateReadableUiFont(Mathf.Max(32, fontSize));
        }

        return text;
    }

    private static Font CreateReadableUiFont(int size)
    {
        var bundledFont = AssetDatabase.LoadAssetAtPath<Font>(ElderCareUiFontPath);
        if (bundledFont != null) return bundledFont;

        var font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" },
            Mathf.Max(16, size));
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = go.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void EnsureUiEventSystem()
    {
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }

        var xrUiModule = AddComponentIfTypeExists(eventSystem.gameObject, "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrUiModule == null && eventSystem.GetComponent<BaseInputModule>() == null)
        {
            var inputSystemModule = AddComponentIfTypeExists(eventSystem.gameObject, "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule == null)
            {
                EnsureComponent<StandaloneInputModule>(eventSystem.gameObject);
            }
        }

        EditorUtility.SetDirty(eventSystem.gameObject);
    }

    private static Component AddComponentIfTypeExists(GameObject target, string typeName)
    {
        if (target == null || string.IsNullOrEmpty(typeName)) return null;

        var type = System.Type.GetType(typeName);
        if (type == null || !typeof(Component).IsAssignableFrom(type)) return null;

        var existing = target.GetComponent(type);
        return existing != null ? existing : target.AddComponent(type);
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color)
    {
        var matPath = $"{MaterialRoot}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;

        var shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Legacy Shaders/Diffuse");

        if (shader == null)
        {
            Debug.LogError($"Could not find a valid shader for material: {materialName}");
            return null;
        }

        material = new Material(shader);
        material.color = color;
        EnsureFolderPath(MaterialRoot);
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    private static Material CreateOrLoadLineMaterial(string materialName, Color color)
    {
        var matPath = $"{MaterialRoot}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;

        var shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        if (shader == null)
        {
            return CreateOrLoadMaterial(materialName, color);
        }

        material = new Material(shader);
        material.color = color;
        EnsureFolderPath(MaterialRoot);
        AssetDatabase.CreateAsset(material, matPath);
        return material;
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

    private static bool EnsureEditMode()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return true;

        Debug.LogError("Archery scene builder tools must be run in Edit Mode. Exit Play Mode and run the tool again.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Archery", "Please exit Play Mode before running this tool.", "OK");
        }

        return false;
    }

    private static Transform FindTransformInScene(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var match = FindDescendantByName(root.transform, name);
            if (match != null) return match;
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null) return null;

        foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == name) return candidate;
        }

        return null;
    }

    private static GameObject GetOrCreateUniqueBowSection(string name, Transform bowRoot)
    {
        var matches = FindNamedBowDescendants(bowRoot, name);
        Transform selected = null;
        foreach (var match in matches)
        {
            if (match.parent == bowRoot)
            {
                selected = match;
                break;
            }
        }

        if (selected == null && matches.Count > 0)
        {
            selected = matches[0];
        }

        if (selected == null)
        {
            selected = new GameObject(name).transform;
        }

        selected.SetParent(bowRoot, false);
        selected.name = name;

        foreach (var duplicate in matches)
        {
            if (duplicate == null || duplicate == selected) continue;

            while (duplicate.childCount > 0)
            {
                duplicate.GetChild(0).SetParent(selected, false);
            }

            Object.DestroyImmediate(duplicate.gameObject);
        }

        return selected.gameObject;
    }

    private static GameObject MoveUniqueBowNode(string name, Transform bowRoot, Transform targetParent)
    {
        var matches = FindNamedBowDescendants(bowRoot, name);
        Transform selected = null;
        foreach (var match in matches)
        {
            if (match.parent == targetParent)
            {
                selected = match;
                break;
            }
        }

        if (selected == null && matches.Count > 0)
        {
            selected = matches[0];
        }

        if (selected == null) return null;

        selected.SetParent(targetParent, false);
        foreach (var duplicate in matches)
        {
            if (duplicate == null || duplicate == selected) continue;
            Object.DestroyImmediate(duplicate.gameObject);
        }

        return selected.gameObject;
    }

    private static System.Collections.Generic.List<Transform> FindNamedBowDescendants(Transform bowRoot, string name)
    {
        var matches = new System.Collections.Generic.List<Transform>();
        if (bowRoot == null) return matches;

        foreach (var candidate in bowRoot.GetComponentsInChildren<Transform>(true))
        {
            if (candidate != bowRoot && candidate.name == name)
            {
                matches.Add(candidate);
            }
        }

        return matches;
    }

    private static GameObject GetOrCreate(string name, Transform parent = null)
    {
        // GameObject.Find 找不到被停用的对象：若场景在 Archery 根被停用时保存过，
        // 重建会生成重复模块并撕裂菜单接线。先按名扫描全部根对象（含 inactive）。
        GameObject go = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root != null && root.name == name)
            {
                go = root;
                break;
            }
        }

        if (go == null)
        {
            go = GameObject.Find(name) ?? new GameObject(name);
        }

        if (!go.activeSelf)
        {
            go.SetActive(true);
        }

        if (parent != null) go.transform.SetParent(parent);
        return go;
    }

    private static GameObject GetOrCreateChild(string name, Transform parent)
    {
        if (parent == null) return GameObject.Find(name) ?? new GameObject(name);

        var child = parent.Find(name);
        if (child != null) return child.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;

        var component = go.GetComponent<T>();
        if (component != null) return component;

        try
        {
            component = go.AddComponent<T>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Could not add {typeof(T).Name} component to '{go.name}'. {ex.GetType().Name}: {ex.Message}");
            return null;
        }

        if (component == null)
        {
            Debug.LogError($"Could not add {typeof(T).Name} component to '{go.name}'.");
        }

        return component;
    }
}
