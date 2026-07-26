using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public static class ArcheryGameSceneBuilder
{
    private const string MaterialRoot = "Assets/_Project/Materials/Archery";
    private const string ElderCareUiFontPath = "Assets/_Project/Fonts/NotoSansCJKsc-Regular.otf";

    private static readonly Color PanelBackgroundColor = new Color(0.05f, 0.08f, 0.14f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.32f, 0.55f, 0.95f);
    private static readonly Color ButtonAccentColor = new Color(0.06f, 0.52f, 0.5f, 0.95f);
    private static readonly Color ButtonSecondaryColor = new Color(0.2f, 0.26f, 0.4f, 0.95f);

    [MenuItem("Tools/PICO ElderCare/Build Archery Game Objects")]
    public static void BuildArcheryGameObjectsMenu()
    {
        if (!EnsureEditMode()) return;

        var menu = Object.FindObjectOfType<ElderCareHomeMenu>(true);
        BuildArcheryModule(menu);
        MarkActiveSceneDirtyAndSaveForBatch();
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Archery", "射箭训练场景对象已生成/修复。", "OK");
        }
    }

    public static ArcheryGameManager BuildArcheryModule(ElderCareHomeMenu menu)
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
        var panel = BuildScoreCanvas(archeryRoot.transform, menu);

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
            panel.homeMenu = menu;
            panel.bow = bow;
            EditorUtility.SetDirty(panel.gameObject);
        }

        if (menu != null)
        {
            menu.archeryGameplayRoots = new[] { archeryRoot };
            menu.archeryGameManager = manager;
            EditorUtility.SetDirty(menu);
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

        var riserMaterial = CreateOrLoadMaterial("ArcheryBowRiser", new Color(0.42f, 0.28f, 0.16f));
        var limbMaterial = CreateOrLoadMaterial("ArcheryBowLimb", new Color(0.56f, 0.4f, 0.24f));

        var riser = CreatePrimitiveChild("Riser", bowObject.transform, PrimitiveType.Cube,
            Vector3.zero, Vector3.zero, new Vector3(0.035f, 0.34f, 0.05f), riserMaterial);
        RemovePrimitiveCollider(riser);

        var grip = CreatePrimitiveChild("Grip", bowObject.transform, PrimitiveType.Cube,
            new Vector3(0f, -0.02f, -0.008f), Vector3.zero, new Vector3(0.045f, 0.15f, 0.06f), riserMaterial);
        RemovePrimitiveCollider(grip);

        var upperLimb = CreatePrimitiveChild("UpperLimb", bowObject.transform, PrimitiveType.Cube,
            new Vector3(0f, 0.32f, 0.02f), new Vector3(10f, 0f, 0f), new Vector3(0.028f, 0.46f, 0.02f), limbMaterial);
        RemovePrimitiveCollider(upperLimb);
        var lowerLimb = CreatePrimitiveChild("LowerLimb", bowObject.transform, PrimitiveType.Cube,
            new Vector3(0f, -0.32f, 0.02f), new Vector3(-10f, 0f, 0f), new Vector3(0.028f, 0.46f, 0.02f), limbMaterial);
        RemovePrimitiveCollider(lowerLimb);

        var stringTop = GetOrCreateChild("StringTopAnchor", bowObject.transform);
        stringTop.transform.localPosition = new Vector3(0f, 0.53f, 0.055f);
        stringTop.transform.localRotation = Quaternion.identity;
        var stringBottom = GetOrCreateChild("StringBottomAnchor", bowObject.transform);
        stringBottom.transform.localPosition = new Vector3(0f, -0.53f, 0.055f);
        stringBottom.transform.localRotation = Quaternion.identity;
        var nockRest = GetOrCreateChild("NockRest", bowObject.transform);
        nockRest.transform.localPosition = new Vector3(0f, 0f, 0.055f);
        nockRest.transform.localRotation = Quaternion.identity;

        var stringObject = GetOrCreateChild("BowString", bowObject.transform);
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

        var nockedArrow = GetOrCreateChild("NockedArrowVisual", bowObject.transform);
        nockedArrow.transform.localPosition = new Vector3(0f, 0f, 0.055f);
        nockedArrow.transform.localRotation = Quaternion.identity;
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

    private static ArcheryScorePanel BuildScoreCanvas(Transform archeryRoot, ElderCareHomeMenu menu)
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

        CreateRoundedPanel(canvasRect, "Background", new Vector2(900f, 980f), Vector2.zero, PanelBackgroundColor, 36f);
        CreateText(canvasRect, "Title", "射箭训练", new Vector2(0f, 430f), new Vector2(700f, 84f), 58, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateRoundedPanel(canvasRect, "TitleDivider", new Vector2(220f, 4f), new Vector2(0f, 378f), new Color(1f, 1f, 1f, 0.5f), 2f);

        CreateText(canvasRect, "ScoreLabel", "总分", new Vector2(-230f, 305f), new Vector2(280f, 70f), 38, FontStyle.Normal, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        var scoreValue = CreateText(canvasRect, "ScoreValue", "0 分", new Vector2(140f, 305f), new Vector2(420f, 80f), 54, FontStyle.Bold, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter);

        CreateText(canvasRect, "ArrowsLabel", "剩余箭数", new Vector2(-230f, 225f), new Vector2(280f, 60f), 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        var arrowsValue = CreateText(canvasRect, "ArrowsValue", "10 / 10", new Vector2(140f, 225f), new Vector2(420f, 64f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        CreateText(canvasRect, "LastHitLabel", "上一箭", new Vector2(-230f, 150f), new Vector2(280f, 60f), 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        var lastHitValue = CreateText(canvasRect, "LastHitValue", "--", new Vector2(140f, 150f), new Vector2(420f, 64f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        CreateText(canvasRect, "BestLabel", "历史最佳", new Vector2(-230f, 75f), new Vector2(280f, 60f), 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        var bestValue = CreateText(canvasRect, "BestValue", "--", new Vector2(140f, 75f), new Vector2(420f, 64f), 40, FontStyle.Bold, new Color(0.65f, 0.92f, 1f), TextAnchor.MiddleCenter);

        CreateText(canvasRect, "DifficultyLabel", "目标距离", new Vector2(-230f, 0f), new Vector2(280f, 60f), 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        var difficultyValue = CreateText(canvasRect, "DifficultyValue", "中距 6 米", new Vector2(140f, 0f), new Vector2(420f, 64f), 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var nearButton = CreateActionButton(canvasRect, "DifficultyNearButton", "近距", new Vector2(250f, 84f), new Vector2(-290f, -95f), ButtonColor, out _);
        var mediumButton = CreateActionButton(canvasRect, "DifficultyMediumButton", "中距", new Vector2(250f, 84f), new Vector2(0f, -95f), ButtonColor, out _);
        var farButton = CreateActionButton(canvasRect, "DifficultyFarButton", "远距", new Vector2(250f, 84f), new Vector2(290f, -95f), ButtonColor, out _);

        var assistButton = CreateActionButton(canvasRect, "AssistToggleButton", "辅助瞄准：开", new Vector2(280f, 84f), new Vector2(-300f, -200f), ButtonSecondaryColor, out var assistLabel);
        var handednessButton = CreateActionButton(canvasRect, "HandednessButton", "持弓手：左手", new Vector2(280f, 84f), new Vector2(0f, -200f), ButtonSecondaryColor, out var handednessLabel);
        var recenterButton = CreateActionButton(canvasRect, "RecenterButton", "重新对准", new Vector2(280f, 84f), new Vector2(300f, -200f), ButtonSecondaryColor, out _);

        var restartButton = CreateActionButton(canvasRect, "RestartButton", "再来一轮", new Vector2(380f, 100f), new Vector2(-210f, -315f), ButtonAccentColor, out _);
        var homeButton = CreateActionButton(canvasRect, "HomeButton", "返回首页", new Vector2(380f, 100f), new Vector2(210f, -315f), new Color(0.34f, 0.22f, 0.5f, 0.95f), out _);

        var statusText = CreateText(canvasRect, "StatusText", "握紧右手手柄搭弦，向后拉再松开放箭", new Vector2(0f, -425f), new Vector2(840f, 60f), 30, FontStyle.Normal, new Color(1f, 1f, 1f, 0.66f), TextAnchor.MiddleCenter);

        var panel = EnsureComponent<ArcheryScorePanel>(canvasGo);
        if (panel != null)
        {
            panel.homeMenu = menu;
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
        }

        EditorUtility.SetDirty(canvasGo);
        return panel;
    }

    private static Button CreateActionButton(RectTransform parent, string name, string label, Vector2 size, Vector2 position, Color color, out Text labelText)
    {
        var root = GetOrCreateChild(name, parent);
        var rootRect = ConfigureRect(root, size, position);

        var panel = CreateRoundedPanel(rootRect, "Panel", size, Vector2.zero, color, 24f);
        if (panel != null)
        {
            panel.raycastTarget = true;
        }

        labelText = CreateText(rootRect, "Label", label, Vector2.zero, new Vector2(size.x - 30f, size.y - 16f), Mathf.RoundToInt(size.y * 0.4f), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var button = EnsureComponent<Button>(root);
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = panel;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
        }

        EditorUtility.SetDirty(root);
        return button;
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

    private static void MarkActiveSceneDirtyAndSaveForBatch()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        if (Application.isBatchMode)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        }
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
