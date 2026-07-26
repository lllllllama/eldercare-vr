using PicoElderCare.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public static class DartsGameSceneBuilder
{
    private const string DartsTrainingScenePath = "Assets/_Project/Scenes/04_DartsTraining.unity";
    private const string MaterialRoot = "Assets/_Project/Materials/Darts";
    private const string ElderCareUiFontPath = "Assets/_Project/Fonts/NotoSansCJKsc-Regular.otf";

    // 与主入口 / 乒乓 / 射箭同一套视觉语言（ElderCareUiTheme）。
    private static readonly Color PanelBackgroundColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.08f), 1f);
    private static readonly Color RowChipColor = new Color(0.045f, 0.085f, 0.115f, 0.85f);
    private static readonly Color DifficultySelectedFill = Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Gold, 0.5f);
    private static readonly Color DifficultySelectedOutline = new Color(1f, 0.82f, 0.35f, 0.9f);

    [MenuItem("Tools/PICO ElderCare/Build Darts Training Scene")]
    public static void BuildDartsTrainingScene()
    {
        if (!EnsureEditMode()) return;

        BuildDartsTrainingSceneInternal();
        // 单独跑这一条菜单项也要注册 Build Settings，否则实机上飞镖入口是死按钮。
        RehabSceneBuilder.ConfigureBuildSettings();
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Darts", "飞镖训练场景已生成。", "OK");
        }
    }

    internal static void BuildDartsTrainingSceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RehabSceneBuilder.CreateMixedRealitySceneFoundation(
            "Managers",
            out var xrOrigin,
            out _);

        var manager = BuildDartsModule();
        if (manager == null)
        {
            Debug.LogError("Darts training scene generation failed because DartsGameManager could not be created.");
            return;
        }

        EditorUtility.SetDirty(manager.gameObject);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);
        EditorSceneManager.SaveScene(scene, DartsTrainingScenePath);
    }

    public static DartsGameManager BuildDartsModule()
    {
        EnsureFolderPath(MaterialRoot);

        var dartsRoot = GetOrCreate("Darts");
        dartsRoot.transform.position = Vector3.zero;
        dartsRoot.transform.rotation = Quaternion.identity;
        EnsureComponent<MrKeepVisible>(dartsRoot);

        BuildLaneVisuals(dartsRoot.transform);
        var board = BuildBoardRig(dartsRoot.transform, out var boardRig, out var boardHeightPivot);
        var dartTemplate = BuildDartTemplate(dartsRoot.transform);
        var dartContainer = GetOrCreateChild("DartContainer", dartsRoot.transform).transform;
        var thrower = BuildThrower(dartsRoot.transform, dartTemplate, dartContainer);
        var goldParticles = BuildHitParticles(dartsRoot.transform, "GoldHitParticles", new Color(1f, 0.84f, 0.25f), 1.9f, 0.045f);
        var dustParticles = BuildHitParticles(dartsRoot.transform, "HitDustParticles", new Color(0.92f, 0.86f, 0.7f), 0.9f, 0.03f);
        var panel = BuildScoreCanvas(dartsRoot.transform);

        var managers = GetOrCreate("Managers");
        var managerObject = GetOrCreateChild("DartsGameManager", managers.transform);
        var manager = EnsureComponent<DartsGameManager>(managerObject);
        if (manager == null) return null;

        manager.thrower = thrower;
        manager.board = board;
        manager.laneRoot = dartsRoot.transform;
        manager.boardRig = boardRig;
        manager.boardHeightPivot = boardHeightPivot;
        manager.headTransform = Camera.main != null ? Camera.main.transform : null;
        manager.dartContainer = dartContainer;
        manager.scorePanel = panel;
        manager.goldHitParticles = goldParticles;
        manager.hitDustParticles = dustParticles;
        manager.dartsPerRound = 10;
        manager.difficulty = DartsDifficulty.Standard;
        manager.autoStartSessionOnStart = true;
        manager.alignLaneToUserOnStart = true;
        manager.calibrateBoardHeightOnStart = true;
        manager.spawnScorePopups = true;
        manager.enableAimAssist = true;
        manager.aimAssistDegrees = DartsGeometry.AimAssistDefaultDegrees;
        manager.popupFont = AssetDatabase.LoadAssetAtPath<Font>(ElderCareUiFontPath);

        BuildAudioManager(managers.transform, thrower);

        if (panel != null)
        {
            panel.manager = manager;
            panel.thrower = thrower;
            if (thrower != null)
            {
                // 指针悬停面板时不抓镖：封死“按住按钮→射线滑出→快速松手误投”的路径。
                thrower.uiHoverGuardBehaviour = panel;
                EditorUtility.SetDirty(thrower);
            }

            EditorUtility.SetDirty(panel.gameObject);
        }

        EditorUtility.SetDirty(dartsRoot);
        EditorUtility.SetDirty(managerObject);
        return manager;
    }

    private static void BuildLaneVisuals(Transform dartsRoot)
    {
        var mat = CreatePrimitiveChild("ThrowingMat", dartsRoot, PrimitiveType.Cube,
            new Vector3(0f, 0.006f, 0f), Vector3.zero, new Vector3(0.9f, 0.012f, 0.9f),
            CreateOrLoadMaterial("DartsThrowingMat", new Color(0.13f, 0.19f, 0.38f)));
        RemovePrimitiveCollider(mat);

        var laneLength = DartsGeometry.FarBoardDistanceMeters + 0.6f;
        var lane = CreatePrimitiveChild("LaneStrip", dartsRoot, PrimitiveType.Cube,
            new Vector3(0f, 0.003f, laneLength * 0.5f + 0.5f), Vector3.zero, new Vector3(0.4f, 0.006f, laneLength),
            CreateOrLoadMaterial("DartsLaneStrip", new Color(0.24f, 0.2f, 0.32f)));
        RemovePrimitiveCollider(lane);
    }

    private static DartsBoard BuildBoardRig(Transform dartsRoot, out Transform boardRig, out Transform boardHeightPivot)
    {
        var rig = GetOrCreateChild("BoardRig", dartsRoot);
        rig.transform.localPosition = new Vector3(0f, 0f, DartsGeometry.StandardBoardDistanceMeters);
        rig.transform.localRotation = Quaternion.identity;
        boardRig = rig.transform;

        var standMaterial = CreateOrLoadMaterial("DartsStandWood", new Color(0.42f, 0.3f, 0.18f));
        var stand = GetOrCreateChild("BoardStand", rig.transform);
        stand.transform.localPosition = Vector3.zero;
        stand.transform.localRotation = Quaternion.identity;
        var legLeft = CreatePrimitiveChild("LegLeft", stand.transform, PrimitiveType.Cube,
            new Vector3(-0.32f, 0.95f, 0.1f), new Vector3(10f, 0f, 0f), new Vector3(0.045f, 1.95f, 0.045f), standMaterial);
        RemovePrimitiveCollider(legLeft);
        var legRight = CreatePrimitiveChild("LegRight", stand.transform, PrimitiveType.Cube,
            new Vector3(0.32f, 0.95f, 0.1f), new Vector3(10f, 0f, 0f), new Vector3(0.045f, 1.95f, 0.045f), standMaterial);
        RemovePrimitiveCollider(legRight);
        var crossBar = CreatePrimitiveChild("CrossBar", stand.transform, PrimitiveType.Cube,
            new Vector3(0f, 0.5f, 0.13f), Vector3.zero, new Vector3(0.7f, 0.045f, 0.045f), standMaterial);
        RemovePrimitiveCollider(crossBar);

        var heightPivot = GetOrCreateChild("BoardHeightPivot", rig.transform);
        heightPivot.transform.localPosition = new Vector3(0f, DartsGeometry.DefaultSeatedEyeHeightMeters, 0f);
        heightPivot.transform.localRotation = Quaternion.identity;
        boardHeightPivot = heightPivot.transform;

        var face = GetOrCreateChild("BoardFace", heightPivot.transform);
        face.transform.localPosition = Vector3.zero;
        face.transform.localRotation = Quaternion.identity;

        var collider = EnsureComponent<BoxCollider>(face);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = new Vector3(
                DartsGeometry.BoardBackboardSizeMeters,
                DartsGeometry.BoardBackboardSizeMeters,
                DartsGeometry.BoardBackboardThicknessMeters);
        }

        var backboard = CreatePrimitiveChild("Backboard", face.transform, PrimitiveType.Cube,
            Vector3.zero, Vector3.zero,
            new Vector3(DartsGeometry.BoardBackboardSizeMeters, DartsGeometry.BoardBackboardSizeMeters, 0.05f),
            CreateOrLoadMaterial("DartsBackboard", new Color(0.28f, 0.22f, 0.16f)));
        RemovePrimitiveCollider(backboard);

        // 经典镖盘配色（外→内），盘心用金色呼应“金环”反馈与金色粒子。
        var ringColors = new[]
        {
            new Color(0.92f, 0.87f, 0.7f),
            new Color(0.12f, 0.12f, 0.14f),
            new Color(0.13f, 0.55f, 0.3f),
            new Color(0.8f, 0.15f, 0.12f),
            new Color(1f, 0.84f, 0.25f)
        };
        var ringNames = new[] { "Ring_Cream", "Ring_Black", "Ring_Green", "Ring_Red", "Ring_GoldBull" };
        for (var i = 0; i < ringColors.Length; i++)
        {
            var diameter = DartsGeometry.BoardFaceRadiusMeters * 2f * (DartsGeometry.BoardScoreBands - i) / DartsGeometry.BoardScoreBands;
            var ring = CreatePrimitiveChild(ringNames[i], face.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0f, -0.028f - 0.003f * i), new Vector3(90f, 0f, 0f),
                new Vector3(diameter, 0.006f, diameter),
                CreateOrLoadMaterial($"Darts{ringNames[i]}", ringColors[i]));
            RemovePrimitiveCollider(ring);
        }

        var stuckDarts = GetOrCreateChild("StuckDarts", face.transform);
        stuckDarts.transform.localPosition = Vector3.zero;
        stuckDarts.transform.localRotation = Quaternion.identity;

        var board = EnsureComponent<DartsBoard>(face);
        if (board != null)
        {
            board.faceCenter = face.transform;
            board.faceRadiusMeters = DartsGeometry.BoardFaceRadiusMeters;
            board.scoreBands = DartsGeometry.BoardScoreBands;
            board.maxRingScore = DartsGeometry.BoardMaxRingScore;
            board.stickParent = stuckDarts.transform;
        }

        EditorUtility.SetDirty(rig);
        return board;
    }

    private static GameObject BuildDartTemplate(Transform dartsRoot)
    {
        var template = GetOrCreateChild("DartTemplate", dartsRoot);
        template.transform.localPosition = new Vector3(0f, -10f, 0f);
        template.transform.localRotation = Quaternion.identity;

        BuildDartVisualChildren(template.transform);
        BuildDartTrail(template.transform);

        var projectile = EnsureComponent<DartProjectile>(template);
        if (projectile != null)
        {
            projectile.gravityMetersPerSecondSquared = DartsGeometry.DartGravityMetersPerSecondSquared;
            projectile.linearDragPerSecond = DartsGeometry.DartLinearDragPerSecond;
            projectile.dartLengthMeters = DartsGeometry.DartLengthMeters;
            projectile.castRadiusMeters = DartsGeometry.DartRadiusMeters;
            projectile.stickDepthMeters = DartsGeometry.DartStickDepthMeters;
            projectile.maxFlightSeconds = DartsGeometry.DartMaxFlightSeconds;
            projectile.missFloorY = -0.5f;
            projectile.hitLayers = BuildDartHitLayerMask();
        }

        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static void BuildDartVisualChildren(Transform dartRoot)
    {
        var barrel = CreatePrimitiveChild("Barrel", dartRoot, PrimitiveType.Cylinder,
            new Vector3(0f, 0f, DartsGeometry.DartLengthMeters * 0.45f), new Vector3(90f, 0f, 0f),
            new Vector3(0.014f, DartsGeometry.DartLengthMeters * 0.32f, 0.014f),
            CreateOrLoadMaterial("DartsBarrel", new Color(0.72f, 0.55f, 0.25f)));
        RemovePrimitiveCollider(barrel);

        var tip = CreatePrimitiveChild("Tip", dartRoot, PrimitiveType.Cylinder,
            new Vector3(0f, 0f, DartsGeometry.DartLengthMeters * 0.9f), new Vector3(90f, 0f, 0f),
            new Vector3(0.005f, DartsGeometry.DartLengthMeters * 0.12f, 0.005f),
            CreateOrLoadMaterial("DartsTip", new Color(0.6f, 0.62f, 0.66f)));
        RemovePrimitiveCollider(tip);

        var flightMaterial = CreateOrLoadMaterial("DartsFlight", new Color(0.85f, 0.22f, 0.2f));
        var flightVertical = CreatePrimitiveChild("Flight_Vertical", dartRoot, PrimitiveType.Cube,
            new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.002f, 0.04f, 0.045f), flightMaterial);
        RemovePrimitiveCollider(flightVertical);
        var flightHorizontal = CreatePrimitiveChild("Flight_Horizontal", dartRoot, PrimitiveType.Cube,
            new Vector3(0f, 0f, 0.02f), new Vector3(0f, 0f, 90f), new Vector3(0.002f, 0.04f, 0.045f), flightMaterial);
        RemovePrimitiveCollider(flightHorizontal);
    }

    private static void BuildDartTrail(Transform dartRoot)
    {
        var trailGo = GetOrCreateChild("Trail", dartRoot);
        trailGo.transform.localPosition = new Vector3(0f, 0f, DartsGeometry.DartLengthMeters * 0.5f);
        trailGo.transform.localRotation = Quaternion.identity;

        var trail = EnsureComponent<TrailRenderer>(trailGo);
        if (trail == null) return;

        trail.time = 0.16f;
        trail.startWidth = 0.01f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.03f;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = CreateOrLoadLineMaterial("DartsTrail", new Color(0.85f, 0.95f, 1f, 0.8f));

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.45f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
    }

    private static HandDartThrower BuildThrower(Transform dartsRoot, GameObject dartTemplate, Transform dartContainer)
    {
        var throwerObject = GetOrCreateChild("Thrower", dartsRoot);
        throwerObject.transform.localPosition = Vector3.zero;
        throwerObject.transform.localRotation = Quaternion.identity;

        var heldVisual = GetOrCreateChild("HeldDartVisual", throwerObject.transform);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;
        BuildDartVisualChildren(heldVisual.transform);
        heldVisual.SetActive(false);

        var audioObject = GetOrCreateChild("ThrowAudio", throwerObject.transform);
        audioObject.transform.localPosition = new Vector3(0f, 1.2f, 0.2f);
        var audioSource = EnsureComponent<AudioSource>(audioObject);
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 0.8f;
            audioSource.maxDistance = 16f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        var thrower = EnsureComponent<HandDartThrower>(throwerObject);
        if (thrower != null)
        {
            thrower.throwHandTransform = FindControllerTransform(true);
            thrower.offHandTransform = FindControllerTransform(false);
            thrower.throwHandNode = XRNode.RightHand;
            thrower.offHandNode = XRNode.LeftHand;
            thrower.throwWithRightHand = true;
            thrower.autoCreateHoldInputSource = true;
            thrower.heldDartVisual = heldVisual.transform;
            thrower.holdForwardOffsetMeters = DartsGeometry.HoldForwardOffsetMeters;
            thrower.dartTemplate = dartTemplate;
            thrower.dartContainer = dartContainer;
            thrower.handSpeedMultiplier = DartsGeometry.HandSpeedMultiplier;
            thrower.minThrowHandSpeed = DartsGeometry.MinThrowHandSpeedMetersPerSecond;
            thrower.minDartSpeed = DartsGeometry.MinDartSpeedMetersPerSecond;
            thrower.maxDartSpeed = DartsGeometry.MaxDartSpeedMetersPerSecond;
            thrower.velocitySampleWindowSeconds = DartsGeometry.VelocitySampleWindowSeconds;

            if (thrower.throwHandTransform == null)
            {
                Debug.Log("Right hand controller not auto-bound for darts. Please assign XR Origin right controller to HandDartThrower.throwHandTransform manually.");
            }

            if (thrower.offHandTransform == null)
            {
                Debug.Log("Left hand controller not auto-bound for darts. Please assign XR Origin left controller to HandDartThrower.offHandTransform manually.");
            }
        }

        EditorUtility.SetDirty(throwerObject);
        return thrower;
    }

    private static void BuildAudioManager(Transform managers, HandDartThrower thrower)
    {
        var audioObject = GetOrCreateChild("DartsAudioManager", managers);
        var audioManager = EnsureComponent<DartsAudioManager>(audioObject);
        if (audioManager == null) return;

        var throwerTransform = thrower != null ? thrower.transform : null;
        audioManager.handSource = FindChildComponent<AudioSource>(throwerTransform, "ThrowAudio");
        audioManager.volume = 0.85f;
        EditorUtility.SetDirty(audioObject);
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null) return null;

        var child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static ParticleSystem BuildHitParticles(Transform dartsRoot, string name, Color color, float speed, float size)
    {
        var particleGo = GetOrCreateChild(name, dartsRoot);
        particleGo.transform.localPosition = new Vector3(0f, -5f, 0f);
        particleGo.transform.localRotation = Quaternion.identity;

        var particles = EnsureComponent<ParticleSystem>(particleGo);
        if (particles == null) return null;

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.5f;
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
        shape.radius = 0.04f;

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
        var matPath = $"{MaterialRoot}/DartsParticle.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;

        var texture = CreateOrLoadSoftCircleTexture();
        var shader =
            Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return CreateOrLoadLineMaterial("DartsParticleFallback", Color.white);
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
        // 软圆光斑贴图与射箭共用同一张资源，避免重复资产。
        var archeryTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Materials/Archery/ArcherySoftParticle.png");
        if (archeryTexture != null) return archeryTexture;

        var texturePath = $"{MaterialRoot}/DartsSoftParticle.png";
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

    private static DartsScorePanel BuildScoreCanvas(Transform dartsRoot)
    {
        var canvasGo = GetOrCreateChild("DartsScoreCanvas", dartsRoot);
        var canvas = EnsureComponent<Canvas>(canvasGo);
        if (canvas == null) return null;

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 18;

        var canvasRect = ConfigureRect(canvasGo, new Vector2(900f, 980f), Vector2.zero);
        canvasGo.transform.localPosition = new Vector3(-1.25f, 1.5f, 1.6f);
        canvasGo.transform.localRotation = Quaternion.Euler(0f, -28f, 0f);
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
        CreateRoundedPanel(canvasRect, "AmbientLineRight", new Vector2(3f, 800f), new Vector2(432f, -40f), WithAlpha(ElderCareUiTheme.Orange, 0.13f), 2f);

        CreateRoundedPanel(canvasRect, "Star_A", new Vector2(8f, 8f), new Vector2(-378f, 448f), new Color(1f, 1f, 1f, 0.42f), 4f);
        CreateRoundedPanel(canvasRect, "Star_B", new Vector2(7f, 7f), new Vector2(386f, 442f), new Color(1f, 1f, 1f, 0.34f), 4f);
        CreateRoundedPanel(canvasRect, "Star_C", new Vector2(6f, 6f), new Vector2(-370f, -446f), new Color(1f, 1f, 1f, 0.28f), 3f);
        CreateRoundedPanel(canvasRect, "Star_D", new Vector2(8f, 8f), new Vector2(376f, -440f), new Color(1f, 1f, 1f, 0.36f), 4f);

        // ---- 标题区 ----
        var titleIconHalo = CreateRoundedPanel(canvasRect, "TitleIconHalo", new Vector2(76f, 76f), new Vector2(-238f, 428f), WithAlpha(ElderCareUiTheme.Red, 0.14f), 38f);
        titleIconHalo.raycastTarget = false;
        var titleIconGo = GetOrCreateChild("TitleIcon", canvasRect);
        var titleIcon = EnsureComponent<ElderCareLineIcon>(titleIconGo);
        titleIcon.iconType = ElderCareIconType.Target;
        titleIcon.strokeWidth = 6f;
        titleIcon.color = WithAlpha(ElderCareUiTheme.Red, 0.95f);
        titleIcon.raycastTarget = false;
        ConfigureRect(titleIconGo, new Vector2(52f, 52f), new Vector2(-238f, 428f));

        CreateText(canvasRect, "Title", "飞镖训练", new Vector2(30f, 434f), new Vector2(480f, 72f), 54, FontStyle.Bold, ElderCareUiTheme.TextPrimary, TextAnchor.MiddleCenter);
        CreateText(canvasRect, "Subtitle", "单手投掷 · 坐姿可玩", new Vector2(30f, 384f), new Vector2(480f, 38f), 24, FontStyle.Normal, ElderCareUiTheme.TextSecondary, TextAnchor.MiddleCenter);
        CreateRoundedPanel(canvasRect, "TitleDivider", new Vector2(420f, 4f), new Vector2(0f, 352f), WithAlpha(ElderCareUiTheme.Cyan, 0.48f), 3f);

        // ---- 数据行 ----
        var scoreValue = CreateStatRow(canvasRect, "ScoreRow", "总分", "0 分", new Vector2(0f, 298f), ElderCareUiTheme.Gold, 46, ElderCareUiTheme.Gold);
        var dartsValue = CreateStatRow(canvasRect, "DartsRow", "剩余镖数", "10 / 10", new Vector2(0f, 226f), ElderCareUiTheme.Cyan, 40, ElderCareUiTheme.TextPrimary);
        var lastHitValue = CreateStatRow(canvasRect, "LastHitRow", "上一镖", "--", new Vector2(0f, 154f), ElderCareUiTheme.Green, 40, ElderCareUiTheme.TextPrimary);
        var bestValue = CreateStatRow(canvasRect, "BestRow", "历史最佳", "--", new Vector2(0f, 82f), ElderCareUiTheme.Blue, 38, new Color(0.65f, 0.92f, 1f));
        var difficultyValue = CreateStatRow(canvasRect, "DifficultyRow", "目标距离", "标准 2.4 米", new Vector2(0f, 10f), ElderCareUiTheme.Violet, 36, ElderCareUiTheme.TextPrimary);

        // ---- 按钮区 ----
        var nearButton = CreateActionButton(canvasRect, "DifficultyNearButton", "近距", new Vector2(250f, 84f), new Vector2(-290f, -100f), ElderCareUiTheme.Blue, 0.05f, out _);
        var standardButton = CreateActionButton(canvasRect, "DifficultyStandardButton", "标准", new Vector2(250f, 84f), new Vector2(0f, -100f), ElderCareUiTheme.Blue, 0.1f, out _);
        var farButton = CreateActionButton(canvasRect, "DifficultyFarButton", "远距", new Vector2(250f, 84f), new Vector2(290f, -100f), ElderCareUiTheme.Blue, 0.15f, out _);

        var assistButton = CreateActionButton(canvasRect, "AssistToggleButton", "辅助瞄准：开", new Vector2(280f, 84f), new Vector2(-300f, -205f), ElderCareUiTheme.Cyan, 0.2f, out var assistLabel);
        var throwHandButton = CreateActionButton(canvasRect, "ThrowHandButton", "投掷手：右手", new Vector2(280f, 84f), new Vector2(0f, -205f), ElderCareUiTheme.Cyan, 0.25f, out var throwHandLabel);
        var recenterButton = CreateActionButton(canvasRect, "RecenterButton", "重新对准", new Vector2(280f, 84f), new Vector2(300f, -205f), ElderCareUiTheme.Cyan, 0.3f, out _);

        var restartButton = CreateActionButton(canvasRect, "RestartButton", "再来一轮", new Vector2(380f, 100f), new Vector2(-210f, -322f), ElderCareUiTheme.Green, 0.35f, out _);
        var homeButton = CreateActionButton(canvasRect, "HomeButton", "返回健康游戏", new Vector2(380f, 100f), new Vector2(210f, -322f), ElderCareUiTheme.Violet, 0.4f, out var homeLabel);
        AddBackIconToButton(homeButton, homeLabel);

        // ---- 状态条 ----
        CreateRoundedPanel(canvasRect, "StatusChip", new Vector2(840f, 58f), new Vector2(0f, -430f), RowChipColor, 16f);
        var statusText = CreateText(canvasRect, "StatusText", "右手握紧手柄拿镖，挥臂松手投出", new Vector2(0f, -430f), new Vector2(800f, 52f), 26, FontStyle.Normal, ElderCareUiTheme.TextSecondary, TextAnchor.MiddleCenter);

        var panel = EnsureComponent<DartsScorePanel>(canvasGo);
        if (panel != null)
        {
            panel.uiFont = CreateReadableUiFont(64);
            panel.scoreValueText = scoreValue;
            panel.dartsValueText = dartsValue;
            panel.lastHitValueText = lastHitValue;
            panel.bestScoreValueText = bestValue;
            panel.difficultyValueText = difficultyValue;
            panel.statusText = statusText;
            panel.assistButtonLabel = assistLabel;
            panel.throwHandButtonLabel = throwHandLabel;
            panel.restartButton = restartButton;
            panel.homeButton = homeButton;
            panel.difficultyNearButton = nearButton;
            panel.difficultyStandardButton = standardButton;
            panel.difficultyFarButton = farButton;
            panel.assistToggleButton = assistButton;
            panel.throwHandButton = throwHandButton;
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
            var labelRect = label.rectTransform;
            labelRect.anchoredPosition = new Vector2(22f, 0f);
            labelRect.sizeDelta = new Vector2(rootRect.sizeDelta.x - 110f, labelRect.sizeDelta.y);
        }
    }

    internal static LayerMask BuildDartHitLayerMask()
    {
        // RoomSensing 是 MR 房间感知网格层：渲染体被隐藏而碰撞体还在，
        // 不剔除的话镖会撞上“隐形墙”凭空消失。
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

        Debug.LogError("Darts scene builder tools must be run in Edit Mode. Exit Play Mode and run the tool again.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Darts", "Please exit Play Mode before running this tool.", "OK");
        }

        return false;
    }

    private static GameObject GetOrCreate(string name, Transform parent = null)
    {
        // GameObject.Find 找不到被停用的对象：先按名扫描全部根对象（含 inactive）。
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

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
