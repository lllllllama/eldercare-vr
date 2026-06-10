using System.Collections.Generic;
using System.IO;
using PicoElderCare.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.PXR;
using UnityEngine.XR;

public static class PingPongDemoSceneBuilder
{
    private const string DemoScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string PrefabRoot = "Assets/_Project/Prefabs/PingPong";
    private const string MaterialRoot = "Assets/_Project/Materials/PingPong";
    private const string FontRoot = "Assets/_Project/Fonts";
    private const string ElderCareUiFontPath = FontRoot + "/NotoSansCJKsc-Regular.otf";
    private const string RehabChineseFontAssetPath = "Assets/_Project/Materials/Rehab/RehabChineseTMP.asset";
    private const string ExternalRoot = "Assets/_Project/External/VRTableTennis";
    private const string OriginalRoot = ExternalRoot + "/Original";
    private const string OriginalModelRoot = OriginalRoot + "/Models";
    private const string OriginalAudioRoot = OriginalRoot + "/Audio";
    private const string AdaptedRoot = ExternalRoot + "/Adapted";
    private const string AdaptedMaterialRoot = AdaptedRoot + "/Materials";
    private const string CustomRoot = "Assets/_Project/External/CustomPingPong";
    private const string CustomModelRoot = CustomRoot + "/Models";
    private static readonly string[] CustomPaddleModelPaths =
    {
        CustomModelRoot + "/PingPongPaddle.prefab",
        CustomModelRoot + "/PingPongPaddle.fbx",
        CustomModelRoot + "/PingPongPaddle.obj",
        CustomModelRoot + "/Paddle.prefab",
        CustomModelRoot + "/Paddle.fbx",
        CustomModelRoot + "/Paddle.obj",
        CustomModelRoot + "/Racket.prefab",
        CustomModelRoot + "/Racket.fbx",
        CustomModelRoot + "/Racket.obj"
    };
    private static readonly string[] CustomHandModelPaths =
    {
        CustomModelRoot + "/LeftHand.prefab",
        CustomModelRoot + "/LeftHand.fbx",
        CustomModelRoot + "/LeftHand.obj",
        CustomModelRoot + "/Hand.prefab",
        CustomModelRoot + "/Hand.fbx",
        CustomModelRoot + "/Hand.obj",
        CustomModelRoot + "/GrabHand.prefab",
        CustomModelRoot + "/GrabHand.fbx",
        CustomModelRoot + "/GrabHand.obj"
    };
    private static readonly string[] CustomTableModelPaths =
    {
        CustomModelRoot + "/PingPongTable.prefab",
        CustomModelRoot + "/PingPongTable.fbx",
        CustomModelRoot + "/PingPongTable.obj",
        CustomModelRoot + "/Table.prefab",
        CustomModelRoot + "/Table.fbx",
        CustomModelRoot + "/Table.obj",
        CustomModelRoot + "/TennisTable.prefab",
        CustomModelRoot + "/TennisTable.fbx",
        CustomModelRoot + "/TennisTable.obj"
    };
    private static readonly string[] CustomBallModelPaths =
    {
        CustomModelRoot + "/PingPongBall.prefab",
        CustomModelRoot + "/PingPongBall.fbx",
        CustomModelRoot + "/PingPongBall.obj",
        CustomModelRoot + "/Ball.prefab",
        CustomModelRoot + "/Ball.fbx",
        CustomModelRoot + "/Ball.obj"
    };
    private static readonly Vector3 TableColliderWorldSize = PingPongGeometry.TableColliderWorldSize;
    private static readonly Vector3 NetColliderWorldSize = PingPongGeometry.NetColliderWorldSize;
    private static readonly Vector3 PaddleColliderCenter = PingPongGeometry.PaddleColliderCenter;
    private static readonly Vector3 PaddleColliderSize = PingPongGeometry.PaddleColliderSize;
    private static readonly Vector3 PaddleHitZoneCenter = PingPongGeometry.PaddleHitZoneCenter;
    private static readonly Vector3 PaddleHitZoneSize = PingPongGeometry.PaddleHitZoneSize;
    private static readonly Vector3 HandVisualTargetSize = new Vector3(0.16f, 0.18f, 0.22f);
    private static readonly Vector3 DefaultPaddleVisualOffsetPosition = Vector3.zero;
    private static readonly Vector3 DefaultPaddleVisualOffsetRotation = new Vector3(0f, 90f, 0f);
    private static readonly Vector3 DefaultPaddleVisualOffsetScale = Vector3.one;
    private static readonly Vector3 DefaultHandVisualOffsetPosition = Vector3.zero;
    private static readonly Vector3 DefaultHandVisualOffsetRotation = new Vector3(0f, 90f, 0f);
    private static readonly Vector3 DefaultHandVisualOffsetScale = Vector3.one;
    private static readonly Vector3 DefaultTableVisualOffsetPosition = Vector3.zero;
    private static readonly Vector3 DefaultTableVisualOffsetRotation = Vector3.zero;
    private static readonly Vector3 DefaultTableVisualOffsetScale = Vector3.one;
    private const float TunedTableTopY = PingPongGeometry.TableTopHeight;

    [MenuItem("Tools/PICO ElderCare/Build VRTableTennis Adapted Assets")]
    public static void BuildVrTableTennisAdaptedAssets()
    {
        if (!EnsureEditMode()) return;

        EnsureFolders();
        RemoveRootLevelGeneratedBallObjects();
        TryCreateOrUpdateAdaptedPrefabs(true);
        RemoveRootLevelGeneratedBallObjects();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("PingPong", "VRTableTennis adapted prefab assets are ready.", "OK");
        }
    }

    [MenuItem("Tools/PICO ElderCare/Build PingPong Demo Scene")]
    public static void BuildDemoScene()
    {
        BuildDemoSceneInternal(false);
    }

    [MenuItem("Tools/PICO ElderCare/Build PingPong Mixed Reality Scene")]
    public static void BuildMixedRealityDemoScene()
    {
        BuildDemoSceneInternal(true);
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Apply Gameplay Tuning Only")]
    public static void ApplyPingPongGameplayTuningOnly()
    {
        if (!EnsureEditMode()) return;

        var table = FindTableInOpenScene();
        if (table == null)
        {
            Debug.LogError("Table not found. Please open or build the ping pong scene first.");
            return;
        }

        var tableCollider = table.GetComponent<BoxCollider>();
        if (tableCollider == null)
        {
            Debug.LogError("Table BoxCollider not found. PingPong gameplay tuning was not applied.");
            return;
        }

        var previousPosition = table.transform.position;
        var deltaY = TunedTableTopY - tableCollider.bounds.max.y;
        table.transform.position = previousPosition + Vector3.up * deltaY;

        TuneTableDragHandles(table.transform, TunedTableTopY);
        TuneTablePassiveMotionLock(table, TunedTableTopY);
        SyncStandaloneNetColliderHeight(table.transform, deltaY);
        TuneBallSpawners(table.transform, TunedTableTopY);
        TuneControllerTableLimiters(table.transform, TunedTableTopY);
        TuneTableSafety(table.transform, TunedTableTopY);

        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(tableCollider);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("PingPong gameplay tuning applied:\n- Table collider top height restored to PingPongGeometry.TableTopHeight\n- BallSpawner bounce height and net clearance synchronized\n- Spawned ball mass/drag/bounciness tuned for lighter rebound");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Restore Table Gameplay Height")]
    public static void RestoreTableGameplayHeight()
    {
        if (!EnsureEditMode()) return;

        var table = FindTableInOpenScene();
        if (table == null)
        {
            Debug.LogError("Table not found. Please open or build the ping pong scene first.");
            return;
        }

        var tableCollider = table.GetComponent<BoxCollider>();
        if (tableCollider == null)
        {
            Debug.LogError("Table BoxCollider not found. Table gameplay height was not restored.");
            return;
        }

        var deltaY = PingPongGeometry.TableTopHeight - tableCollider.bounds.max.y;
        table.transform.position += Vector3.up * deltaY;

        TuneTableDragHandles(table.transform, PingPongGeometry.TableTopHeight);
        TuneTablePassiveMotionLock(table, PingPongGeometry.TableTopHeight);
        SyncStandaloneNetColliderHeight(table.transform, deltaY);
        TuneBallSpawners(table.transform, PingPongGeometry.TableTopHeight);
        TuneControllerTableLimiters(table.transform, PingPongGeometry.TableTopHeight);
        TuneTableSafety(table.transform, PingPongGeometry.TableTopHeight);
        MarkPingPongSpatialRepairDirty(table);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Table gameplay height restored to PingPongGeometry.TableTopHeight = 0.76m.");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Recenter Table In Front Of Player Once")]
    public static void RecenterTableInFrontOfPlayerOnce()
    {
        var table = FindTableInOpenScene();
        if (table == null)
        {
            Debug.LogError("Table not found. Please open the ping pong scene first.");
            return;
        }

        var head = PingPongTableRecenterOnEnter.FindHeadTransform();
        if (head == null)
        {
            Debug.LogWarning("Cannot recenter table because Camera.main / HMD transform was not found.");
            return;
        }

        var recentered = PingPongTableRecenterOnEnter.RecenterTableInFrontOfPlayer(
            tableRoot: table.transform,
            headTransform: head,
            tableDistanceInFront: 1.7f,
            targetTableTopY: PingPongGeometry.TableTopHeight,
            preserveCurrentTableTopHeight: true,
            rotateTableToFacePlayer: true,
            yawOffsetDegrees: 0f,
            syncBallSpawner: true,
            syncServeTransforms: true,
            syncDifficultyPanel: true,
            syncTableHelpers: true,
            acceptPassiveLockAfterMove: true,
            logResult: true,
            report: out _);

        if (!recentered) return;

        EditorUtility.SetDirty(table);
        var spawners = Object.FindObjectsOfType<BallSpawner>(true);
        MarkObjectsDirty(spawners);
        MarkBallSpawnerServeTransformsDirty(spawners);
        MarkObjectsDirty(Object.FindObjectsOfType<TableDragHandle>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<ControllerTableCollisionLimiter>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<PingPongPlayerTableSafety>(true));
        var difficultyPanel = FindObjectByNameIncludingInactive("DifficultyPanel");
        if (difficultyPanel != null)
        {
            MarkObjectsDirty(difficultyPanel.GetComponentsInChildren<Component>(true));
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Apply Stable Serve Tuning Only")]
    public static void ApplyStableServeTuningOnly()
    {
        if (!EnsureEditMode()) return;

        var spawners = Object.FindObjectsOfType<BallSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("No BallSpawner found. Stable serve tuning was not applied.");
            return;
        }

        var table = FindTableInOpenScene();
        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;

            spawner.bounceOnTableBeforePlayer = true;
            spawner.tableTransform = table != null ? table.transform : spawner.tableTransform;
            spawner.useTableRelativeServeTargets = true;
            spawner.netLocalZ = 0f;
            spawner.serveSpeed = 3.1f;
            spawner.upwardArc = 0.55f;
            spawner.tableBounceWorldY = PingPongGeometry.TableTopHeight + PingPongGeometry.BallRadius;
            spawner.tableBounceWorldZ = 1.35f;
            spawner.tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
            spawner.minimumNetClearanceHeight = PingPongGeometry.TableTopHeight + PingPongGeometry.NetHeight + 0.16f;
            spawner.horizontalRandomRange = 0.08f;
            spawner.verticalRandomRange = 0.02f;
            spawner.serveSpinRandomness = 0.08f;
            spawner.serveProfile = PingPongServeProfile.Basic;
            spawner.spawnedBallMass = PingPongGeometry.BallMass;
            spawner.spawnedBallDrag = 0.015f;
            spawner.spawnedBallAngularDrag = 0.04f;
            spawner.spawnedBallBounciness = 0.86f;
            spawner.spawnedBallDynamicFriction = 0.01f;
            spawner.spawnedBallStaticFriction = 0.01f;
            spawner.serveNetClearanceSafetyMargin = 0.03f;
            EditorUtility.SetDirty(spawner);
        }

        if (table != null)
        {
            foreach (var dragHandle in Object.FindObjectsOfType<TableDragHandle>(true))
            {
                if (dragHandle == null) continue;

                dragHandle.tableRoot = table.transform;
                dragHandle.tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
                dragHandle.minimumNetClearanceAboveNet = 0.16f;
                dragHandle.SyncHeightDependentValues();
                EditorUtility.SetDirty(dragHandle);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Stable ping pong serve tuning applied. Serves now target one table bounce before clearing the net.");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Repair Difficulty Panel Drag Only")]
    public static void RepairDifficultyPanelDragOnly()
    {
        if (!EnsureEditMode()) return;

        var panel = FindObjectByNameIncludingInactive("DifficultyPanel");
        if (panel == null)
        {
            Debug.LogWarning("DifficultyPanel not found. Open the ping pong scene before repairing panel drag.");
            return;
        }

        if (!PingPongDifficultyController.RepairPanelDrag(panel))
        {
            Debug.LogWarning("DifficultyPanel drag repair could not be applied.");
            return;
        }

        EditorUtility.SetDirty(panel);
        MarkObjectsDirty(panel.GetComponentsInChildren<Component>(true));
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("DifficultyPanel drag handle repaired. Dragging targets DifficultyPanel only.");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Repair Unified Control Panel Only")]
    public static void RepairUnifiedControlPanelOnly()
    {
        if (!EnsureEditMode()) return;

        var canvasObject = FindObjectByNameIncludingInactive("WorldSpaceCanvas");
        if (canvasObject == null)
        {
            Debug.LogError("WorldSpaceCanvas not found. Open the ping pong scene before repairing the unified control panel.");
            return;
        }

        var scoreManager = Object.FindObjectOfType<ScoreManager>(true);
        var spawner = Object.FindObjectOfType<BallSpawner>(true);
        var homeMenu = Object.FindObjectOfType<ElderCareHomeMenu>(true);
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager not found. Unified control panel repair was not applied.");
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("BallSpawner not found. Unified control panel repair was not applied.");
            return;
        }

        EnsureWorldCanvasRaycasters(canvasObject);
        ConfigureWorldCanvasInteraction(canvasObject);
        RemoveLegacyPingPongControlUi(canvasObject.transform);

        scoreManager.ballSpawner = spawner;
        scoreManager.uiFont = LoadPingPongTmpFont();
        scoreManager.autoCreateDifficultyControls = false;
        scoreManager.useUnifiedControlPanel = true;
        scoreManager.enhanceHudReadability = false;
        scoreManager.accuracyText = null;
        scoreManager.lastSpeedText = null;
        scoreManager.hitText = null;
        scoreManager.servedText = null;
        scoreManager.missedText = null;
        scoreManager.lastSpinText = null;

        var difficultyController = BuildDifficultyControllerState(canvasObject.transform, spawner);
        var remoteTableDrag = Object.FindObjectOfType<RemoteTableDragController>(true);
        var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(
            canvasObject.transform,
            scoreManager,
            spawner,
            difficultyController,
            remoteTableDrag,
            homeMenu,
            LoadPingPongTmpFont());
        if (panel == null)
        {
            Debug.LogError("Unified control panel could not be created.");
            return;
        }

        if (homeMenu != null)
        {
            homeMenu.scoreManager = scoreManager;
            homeMenu.ballSpawner = spawner;
            var roots = homeMenu.pingPongGameplayRoots;
            var hasCanvasRoot = false;
            if (roots != null)
            {
                for (var i = 0; i < roots.Length; i++)
                {
                    if (roots[i] == canvasObject)
                    {
                        hasCanvasRoot = true;
                        break;
                    }
                }
            }

            if (!hasCanvasRoot)
            {
                var oldLength = roots != null ? roots.Length : 0;
                var updatedRoots = new GameObject[oldLength + 1];
                for (var i = 0; i < oldLength; i++)
                {
                    updatedRoots[i] = roots[i];
                }

                updatedRoots[oldLength] = canvasObject;
                homeMenu.pingPongGameplayRoots = updatedRoots;
            }
        }
        else
        {
            Debug.LogWarning("ElderCareHomeMenu not found. The unified panel was repaired, but its Return Home button will need a menu reference.");
        }

        EditorUtility.SetDirty(canvasObject);
        EditorUtility.SetDirty(scoreManager);
        EditorUtility.SetDirty(spawner);
        if (difficultyController != null) EditorUtility.SetDirty(difficultyController);
        if (homeMenu != null) EditorUtility.SetDirty(homeMenu);
        EditorUtility.SetDirty(panel);
        MarkActiveSceneDirtyAndSaveForBatch();

        Debug.Log("Unified ping pong control panel repaired only. Gameplay table, paddles, balls, serve points, and MR objects were not rebuilt.");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Apply PingPong Gameplay Fixes Only")]
    public static void ApplyPingPongGameplayFixesOnly()
    {
        if (!EnsureEditMode()) return;

        RestoreTableGameplayHeight();
        ApplyStableServeTuningOnly();
        RepairDifficultyPanelDragOnly();
        Debug.Log("PingPong gameplay fixes applied only: table height restored, stable serve tuned, and DifficultyPanel drag repaired.");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Repair Spatial Placement Only")]
    public static void RepairPingPongSpatialPlacementOnly()
    {
        if (!EnsureEditMode()) return;

        var table = FindTableInOpenScene();
        if (table == null)
        {
            Debug.LogError("Table not found. Please open or build the ping pong scene first.");
            return;
        }

        if (table.GetComponent<BoxCollider>() == null)
        {
            Debug.LogError("Table BoxCollider not found. PingPong spatial placement repair was not applied.");
            return;
        }

        var sanitizer = EnsurePingPongSpatialSanitizer();
        if (sanitizer != null)
        {
            sanitizer.targetTableTopY = TunedTableTopY;
            sanitizer.uiPreferredY = 1.5f;
            sanitizer.uiMinY = 1.25f;
            sanitizer.uiMaxY = 1.75f;
            sanitizer.fixTableHeight = true;
            sanitizer.fixUiHeight = true;
            sanitizer.ignoreCeilingPlanes = true;
            sanitizer.sanitizeOnStart = true;
            EditorUtility.SetDirty(sanitizer);
        }

        var report = PingPongSpatialSanitizer.RepairScene(
            TunedTableTopY,
            1.5f,
            1.25f,
            1.75f,
            true,
            true);

        MarkPingPongSpatialRepairDirty(table);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            "PingPong spatial placement repaired:\n" +
            "- UI root height clamped to [1.25, 1.75], preferred 1.5\n" +
            "- Table collider top restored to PingPongGeometry.TableTopHeight\n" +
            "- BallSpawner bounce height synchronized\n" +
            "- Table interaction helpers synchronized\n" +
            $"- Table bounds max Y: {FormatSpatialValue(report.tableTopY)}\n" +
            $"- WorldSpaceCanvas world Y: {FormatSpatialValue(report.worldSpaceCanvasY)}\n" +
            $"- DifficultyPanel world Y: {FormatSpatialValue(report.difficultyPanelY)}\n" +
            $"- PingPongHomeCanvas world Y: {FormatSpatialValue(report.pingPongHomeCanvasY)}\n" +
            $"- PingPongHomeMenu world Y: {FormatSpatialValue(report.pingPongHomeMenuY)}");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Log Spatial Diagnostics")]
    public static void LogPingPongSpatialDiagnostics()
    {
        var camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>(true);
        var xrOrigin = FindObjectByNameContainsIncludingInactive("XR Origin") ??
                       FindObjectByNameContainsIncludingInactive("XR Rig") ??
                       FindObjectByNameContainsIncludingInactive("XROrigin");
        var pingPong = GameObject.Find("PingPong");
        var table = FindTableInOpenScene();
        var tableCollider = table != null ? table.GetComponent<BoxCollider>() : null;
        var worldSpaceCanvas = FindObjectByNameIncludingInactive("WorldSpaceCanvas");
        var pingPongHomeCanvas = FindObjectByNameIncludingInactive("PingPongHomeCanvas");
        var pingPongHomeMenu = FindObjectByNameIncludingInactive("PingPongHomeMenu");
        var difficultyPanel = FindObjectByNameIncludingInactive("DifficultyPanel");
        var tableTopY = tableCollider != null ? tableCollider.bounds.max.y : float.NaN;
        var uiY = worldSpaceCanvas != null ? worldSpaceCanvas.transform.position.y : float.NaN;
        var heightDelta = !float.IsNaN(tableTopY) && !float.IsNaN(uiY) ? uiY - tableTopY : float.NaN;

        var diagnostics =
            "PingPong spatial diagnostics:\n" +
            $"- Camera.main.position: {FormatVector(camera != null ? camera.transform.position : (Vector3?)null)}\n" +
            $"- XR Origin position: {FormatVector(xrOrigin != null ? xrOrigin.transform.position : (Vector3?)null)}\n" +
            $"- PingPong root position: {FormatVector(pingPong != null ? pingPong.transform.position : (Vector3?)null)}\n" +
            $"- Table position: {FormatVector(table != null ? table.transform.position : (Vector3?)null)}\n" +
            $"- Table BoxCollider.bounds.max.y: {FormatSpatialValue(tableTopY)}\n" +
            $"- WorldSpaceCanvas position: {FormatVector(worldSpaceCanvas != null ? worldSpaceCanvas.transform.position : (Vector3?)null)}\n" +
            $"- PingPongHomeCanvas position: {FormatVector(pingPongHomeCanvas != null ? pingPongHomeCanvas.transform.position : (Vector3?)null)}\n" +
            $"- PingPongHomeMenu position: {FormatVector(pingPongHomeMenu != null ? pingPongHomeMenu.transform.position : (Vector3?)null)}\n" +
            $"- DifficultyPanel world position: {FormatVector(difficultyPanel != null ? difficultyPanel.transform.position : (Vector3?)null)}\n" +
            $"- UI/Table height delta: {FormatSpatialValue(heightDelta)}";

        Debug.Log(diagnostics);

        WarnIfHighUi("WorldSpaceCanvas", worldSpaceCanvas);
        WarnIfHighUi("PingPongHomeCanvas", pingPongHomeCanvas);
        WarnIfHighUi("PingPongHomeMenu", pingPongHomeMenu);
        WarnIfHighUi("DifficultyPanel", difficultyPanel);

        if (!float.IsNaN(tableTopY) && tableTopY > 1.2f)
        {
            Debug.LogWarning($"Table top is suspiciously high: {tableTopY:0.###}m.");
        }

        WarnIfSuspiciousPlaneHeights(camera);
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Align Paddle Visual To Collider")]
    public static void AlignPaddleVisualToCollider()
    {
        if (!EnsureEditMode()) return;

        var paddle = FindRightPaddleInOpenScene();
        if (paddle == null)
        {
            Debug.LogError("Scene object Paddle_Right not found. Please build or open the ping pong scene first.");
            return;
        }

        var paddleCollider = paddle.GetComponent<BoxCollider>();
        if (paddleCollider == null)
        {
            Debug.LogError("Paddle_Right BoxCollider not found. Paddle visual alignment was not applied.");
            return;
        }

        if (!TryResolvePaddleVisualHierarchy(paddle, out var visualOffset, out var visual))
        {
            Debug.LogError("Visual_CustomRacket not found under Paddle_Right. Replace the paddle visual first.");
            return;
        }

        if (!TryGetRendererBounds(visual, out var visualBounds))
        {
            Debug.LogError("Visual_CustomRacket has no renderers. Paddle visual alignment was not applied.");
            return;
        }

        var targetCenter = paddleCollider.bounds.center;
        var deltaWorld = targetCenter - visualBounds.center;
        var movedTransform = visualOffset != null ? visualOffset : visual.transform;
        movedTransform.position += deltaWorld;
        SyncVisualPoseOffsetFields(movedTransform);

        EditorUtility.SetDirty(paddle);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(movedTransform.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            "Paddle visual aligned to collider.\n" +
            $"- Moved: {movedTransform.name}\n" +
            $"- Applied world delta: {FormatVector(deltaWorld)}\n" +
            $"- Paddle collider center: {FormatVector(targetCenter)}\n" +
            $"- Previous visual center: {FormatVector(visualBounds.center)}");
    }

    [MenuItem("Tools/PICO ElderCare/PingPong/Log Paddle Alignment")]
    public static void LogPaddleAlignment()
    {
        var paddle = FindRightPaddleInOpenScene();
        if (paddle == null)
        {
            Debug.LogError("Scene object Paddle_Right not found. Please build or open the ping pong scene first.");
            return;
        }

        var paddleCollider = paddle.GetComponent<BoxCollider>();
        if (paddleCollider == null)
        {
            Debug.LogError("Paddle_Right BoxCollider not found.");
            return;
        }

        var visual = TryFindPaddleVisual(paddle, out _);
        var visualBounds = default(Bounds);
        var hasVisualBounds = visual != null && TryGetRendererBounds(visual, out visualBounds);
        var hitZoneCollider = FindPaddleHitZoneCollider(paddle.transform);
        var hasHitZone = hitZoneCollider != null;

        var paddleBounds = paddleCollider.bounds;
        var hitZoneBounds = hasHitZone ? hitZoneCollider.bounds : default(Bounds);
        Debug.Log(
            "Paddle alignment diagnostics:\n" +
            $"- Paddle_Right BoxCollider center / size: {FormatVector(paddleBounds.center)} / {FormatVector(paddleBounds.size)}\n" +
            $"- PaddleHitZone center / size: {(hasHitZone ? FormatVector(hitZoneBounds.center) : "not found")} / {(hasHitZone ? FormatVector(hitZoneBounds.size) : "not found")}\n" +
            $"- Visual_CustomRacket bounds center / size: {(hasVisualBounds ? FormatVector(visualBounds.center) : "not found")} / {(hasVisualBounds ? FormatVector(visualBounds.size) : "not found")}\n" +
            $"- HitZone minus BoxCollider center: {(hasHitZone ? FormatVector(hitZoneBounds.center - paddleBounds.center) : "not found")}\n" +
            $"- Visual minus BoxCollider center: {(hasVisualBounds ? FormatVector(visualBounds.center - paddleBounds.center) : "not found")}\n" +
            $"- Visual minus HitZone center: {(hasVisualBounds && hasHitZone ? FormatVector(visualBounds.center - hitZoneBounds.center) : "not found")}");
    }

    [MenuItem("Tools/PICO ElderCare/Replace Paddle Visual Only")]
    public static void ReplacePaddleVisualOnly()
    {
        if (!EnsureEditMode()) return;

        var sourceModel = LoadFirstModel(CustomPaddleModelPaths);
        if (sourceModel == null)
        {
            EditorUtility.DisplayDialog("PingPong", "Custom paddle model not found in Assets/_Project/External/CustomPingPong/Models.", "OK");
            return;
        }

        var paddle = FindRightPaddleInOpenScene();
        if (paddle == null)
        {
            EditorUtility.DisplayDialog("PingPong", "Scene object Paddle_Right not found. Please build or open the ping pong scene first.", "OK");
            Debug.LogWarning("Scene object Paddle_Right not found. Please build or open the ping pong scene first.");
            return;
        }

        ReplacePaddleVisualOnly(paddle, sourceModel);
        EditorUtility.SetDirty(paddle);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Paddle visual replaced only. Gameplay components were preserved.");
    }

    [MenuItem("Tools/PICO ElderCare/Replace Left Hand Visual Only")]
    public static void ReplaceLeftHandVisualOnly()
    {
        if (!EnsureEditMode()) return;

        var sourceModel = LoadPreferredHandModel();
        if (sourceModel == null)
        {
            const string message = "No custom hand model found. Put LeftHand.prefab or Hand.prefab under Assets/_Project/External/CustomPingPong/Models/";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
            Debug.LogError(message);
            return;
        }

        var hand = FindLeftGrabHandInOpenScene();
        if (hand == null)
        {
            const string message = "Left_GrabHand not found. Please open or build the ping pong scene first.";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
            Debug.LogError(message);
            return;
        }

        ReplaceLeftHandVisualOnly(hand, sourceModel);
        EditorUtility.SetDirty(hand);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Left hand visual replaced only. Controller following and gameplay logic were preserved.");
    }

    [MenuItem("Tools/PICO ElderCare/Replace Table Visual Only")]
    public static void ReplaceTableVisualOnly()
    {
        if (!EnsureEditMode()) return;

        var sourceModel = LoadPreferredTableModel();
        if (sourceModel == null)
        {
            const string message = "No custom table model found. Put Table.prefab or PingPongTable.prefab under Assets/_Project/External/CustomPingPong/Models/";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
            Debug.LogError(message);
            return;
        }

        var table = FindTableInOpenScene();
        if (table == null)
        {
            const string message = "Table not found. Please open or build the ping pong scene first.";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
            Debug.LogError(message);
            return;
        }

        ReplaceTableVisualOnly(table, sourceModel);
        EditorUtility.SetDirty(table);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Table visual replaced only. Table physics, net collider, and gameplay logic were preserved.");
    }

    [MenuItem("Tools/PICO ElderCare/Reset Paddle Visual Pose Offset")]
    public static void ResetPaddleVisualPoseOffset()
    {
        if (!EnsureEditMode()) return;

        var paddle = FindRightPaddleInOpenScene();
        if (paddle == null)
        {
            EditorUtility.DisplayDialog("PingPong", "Scene object Paddle_Right not found. Please build or open the ping pong scene first.", "OK");
            Debug.LogWarning("Scene object Paddle_Right not found. Please build or open the ping pong scene first.");
            return;
        }

        var offset = GetOrCreateVisualOffset(
            paddle.transform,
            "PaddleVisualOffset",
            DefaultPaddleVisualOffsetPosition,
            DefaultPaddleVisualOffsetRotation,
            DefaultPaddleVisualOffsetScale,
            true);

        EditorUtility.SetDirty(offset.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Paddle visual pose offset reset.");
    }

    [MenuItem("Tools/PICO ElderCare/Reset Left Hand Visual Pose Offset")]
    public static void ResetLeftHandVisualPoseOffset()
    {
        if (!EnsureEditMode()) return;

        var hand = FindLeftGrabHandInOpenScene();
        if (hand == null)
        {
            const string message = "Left_GrabHand not found. Please open or build the ping pong scene first.";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
            Debug.LogError(message);
            return;
        }

        var offset = GetOrCreateVisualOffset(
            hand.transform,
            "HandVisualOffset",
            DefaultHandVisualOffsetPosition,
            DefaultHandVisualOffsetRotation,
            DefaultHandVisualOffsetScale,
            true);

        EditorUtility.SetDirty(offset.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Left hand visual pose offset reset.");
    }

    private static void BuildDemoSceneInternal(bool mixedRealityMode)
    {
        if (!EnsureEditMode()) return;

        OpenDemoSceneForBatchMode();
        EnsureFolders();
        if (mixedRealityMode)
        {
            ConfigureMixedRealityProjectSettings();
        }
        else
        {
            ConfigureVirtualRealityProjectSettings();
        }

        RemoveRootLevelGeneratedBallObjects();
        TryCreateOrUpdateAdaptedPrefabs(true);
        OpenDemoSceneForBatchMode();
        RemoveRootLevelGeneratedBallObjects();
        RepairExistingBallObjectsInScene();

        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        var managers = GetOrCreate("Managers");
        var uiRoot = GetOrCreate("UI");

        EnsureLight(environment.transform);
        if (mixedRealityMode)
        {
            DisableVirtualRoomSurfaces(environment.transform);
        }
        else
        {
            DisableMixedRealitySceneState();
            EnsureFloor(environment.transform);
            DisableBackWall(environment.transform);
            ConfigureMainCameraForVirtualReality();
        }

        var tablePrefab = LoadAdaptedPrefab("PingPongTable") ??
                          LoadOrCreatePrefabAsset("PingPongTable", PrimitiveType.Cube, TableColliderWorldSize, CreateOrLoadMaterial("TableBlue", new Color(0.07f, 0.3f, 0.47f)));
        var paddlePrefab = LoadAdaptedPrefab("PingPongPaddle") ??
                           LoadOrCreatePrefabAsset("PingPongPaddle", PrimitiveType.Cube, PaddleColliderSize, CreateOrLoadMaterial("PaddleRed", new Color(0.66f, 0.11f, 0.11f)));
        var ballPrefab = LoadAdaptedBallPrefab() ?? CreateOrUpdateBallPrefab();
        if (ballPrefab == null)
        {
            Debug.LogError("Could not create or load PingPong ball prefab. Demo scene generation stopped.");
            return;
        }

        RemoveGeneratedObject("Table");
        RemoveGeneratedObject("Net");
        var table = InstantiateOrReuse("Table", tablePrefab, pingPong.transform, PingPongGeometry.TableCenter, GetInstanceScale(tablePrefab, TableColliderWorldSize));
        var net = SetupOptionalNet(tablePrefab, pingPong.transform);
        var rightPaddle = InstantiateOrReuse("Paddle_Right", paddlePrefab, pingPong.transform, new Vector3(0.35f, 1.1f, 0.5f), GetInstanceScale(paddlePrefab, PaddleColliderSize));
        RemoveGeneratedObject("Paddle_Left");
        var leftHand = SetupLeftHandGrabVisual(pingPong.transform);
        SetLayerRecursively(leftHand, "Controller");
        SetLayerRecursively(table, "Table");
        if (net != null) SetLayerRecursively(net, "Table");
        SetLayerRecursively(rightPaddle, "Racket");
        SetLayerRecursively(ballPrefab, "Ball");

        var spawn = GetOrCreate("BallSpawnPoint", pingPong.transform);
        spawn.transform.position = new Vector3(0f, 1.25f, 3.05f);
        var target = GetOrCreate("BallTargetPoint", pingPong.transform);
        target.transform.position = new Vector3(0.2f, 1.15f, 0.7f);
        var ballContainer = GetOrCreate("BallContainer", pingPong.transform);

        SetupPaddle(rightPaddle);
        SetupTablePhysics(table);
        var tableBlocker = SetupPlayerTableBlocker(pingPong.transform, table.transform);
        SetupControllerTableLimiter(rightPaddle, table.transform);
        SetupControllerTableLimiter(leftHand, table.transform);

        var spawnerObject = GetOrCreate("BallSpawner", managers.transform);
        var spawner = EnsureComponent<BallSpawner>(spawnerObject);
        if (spawner == null) return;
        spawner.ballPrefab = ballPrefab;
        spawner.spawnPoint = spawn.transform;
        spawner.targetPoint = target.transform;
        spawner.tableTransform = table.transform;
        spawner.ballContainer = ballContainer.transform;
        spawner.autoStartOnPlay = false;
        spawner.serveSpeed = PingPongDifficultyController.GetSpeed(PingPongDifficulty.Normal);
        spawner.serveInterval = PingPongDifficultyController.GetServeInterval(PingPongDifficulty.Normal);
        spawner.serveProfile = PingPongServeProfile.Basic;
        spawner.useTableRelativeServeTargets = true;
        spawner.netLocalZ = 0f;
        spawner.tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
        spawner.upwardArc = 0.55f;
        spawner.minimumNetClearanceHeight = PingPongGeometry.TableTopHeight + PingPongGeometry.NetHeight + 0.16f;
        spawner.netWorldZ = PingPongGeometry.TableCenter.z;
        spawner.bounceOnTableBeforePlayer = true;
        spawner.tableBounceWorldY = PingPongGeometry.TableTopHeight + PingPongGeometry.BallRadius;
        spawner.tableBounceWorldZ = 1.35f;
        spawner.horizontalRandomRange = 0.08f;
        spawner.verticalRandomRange = 0.02f;
        spawner.topspinRadiansPerSecond = 95f;
        spawner.backspinRadiansPerSecond = 80f;
        spawner.sidespinRadiansPerSecond = 50f;
        spawner.serveSpinRandomness = 0.08f;
        spawner.maxServeSpin = 140f;
        spawner.serveNetClearanceSafetyMargin = 0.03f;
        spawner.spawnedBallMass = PingPongGeometry.BallMass;
        spawner.spawnedBallDrag = 0.015f;
        spawner.spawnedBallAngularDrag = 0.04f;
        spawner.spawnedBallBounciness = 0.86f;
        spawner.spawnedBallDynamicFriction = 0.01f;
        spawner.spawnedBallStaticFriction = 0.01f;
        ValidateBallSpawnerBindings(spawner);
        var playerBodyProxy = mixedRealityMode ? SetupPlayerBodyProxy(managers.transform) : null;
        SetupPlayerTableSafety(tableBlocker, table.transform, spawner, null, playerBodyProxy);

        var scoreObject = GetOrCreate("ScoreManager", managers.transform);
        var scoreManager = EnsureComponent<ScoreManager>(scoreObject);
        if (scoreManager == null) return;
        scoreManager.uiFont = LoadPingPongTmpFont();
        scoreManager.ballSpawner = spawner;
        scoreManager.autoCreateDifficultyControls = false;
        scoreManager.useUnifiedControlPanel = true;
        scoreManager.enhanceHudReadability = false;

        var feedback = GetOrCreate("HitFeedbackManager", managers.transform);
        var feedbackManager = EnsureComponent<HitFeedbackManager>(feedback);
        if (feedbackManager == null) return;
        SetupFeedbackAudio(feedback, feedbackManager);

        BuildUi(uiRoot.transform, scoreManager, spawner, out var difficultyController, out var unifiedControlPanel);
        BindController(rightPaddle.GetComponent<PaddleFollower>(), true);
        var leftController = BindController(leftHand.GetComponent<ControllerTransformFollower>(), false);
        var gripState = SetupSimpleGripInteractionState(managers.transform);
        var leftBallGrabber = SetupControllerBallGrabber(managers.transform, leftController, gripState);
        var uiCanvas = GameObject.Find("WorldSpaceCanvas")?.transform;
        var dragHandle = SetupTableDragHandle(
            pingPong.transform,
            table,
            leftController,
            leftBallGrabber,
            spawner,
            spawn.transform,
            target.transform,
            tableBlocker != null ? tableBlocker.transform : null,
            mixedRealityMode,
            net != null ? net.transform : null);
        var remoteTableDrag = SetupRemoteTableDragController(managers.transform, table.transform, dragHandle, leftBallGrabber, gripState);
        SetupPlayerTableSafety(tableBlocker, table.transform, spawner, dragHandle, playerBodyProxy);
        SetupInitialViewAligner(managers.transform, mixedRealityMode);
        SetupPingPongSpatialSanitizer(managers.transform);
        SetupPingPongTableRecenterOnEnter(managers.transform, table.transform);
        var homeMenu = BuildElderCareHomeMenu(
            uiRoot.transform,
            managers.transform,
            pingPong,
            uiCanvas != null ? uiCanvas.gameObject : null,
            spawner,
            scoreManager,
            unifiedControlPanel);
        if (unifiedControlPanel != null)
        {
            unifiedControlPanel.Bind(scoreManager, spawner, difficultyController, remoteTableDrag, homeMenu);
            EditorUtility.SetDirty(unifiedControlPanel);
        }

        if (mixedRealityMode)
        {
            SetupMixedRealityMode(managers.transform, environment.transform, table.transform, dragHandle, leftBallGrabber, gripState);
            remoteTableDrag = Object.FindObjectOfType<RemoteTableDragController>(true);
            if (unifiedControlPanel != null)
            {
                unifiedControlPanel.Bind(scoreManager, spawner, difficultyController, remoteTableDrag, homeMenu);
                EditorUtility.SetDirty(unifiedControlPanel);
            }
        }

        EditorUtility.SetDirty(table);
        if (net != null) EditorUtility.SetDirty(net);
        EditorUtility.SetDirty(rightPaddle);
        EditorUtility.SetDirty(leftHand);
        EditorUtility.SetDirty(spawnerObject);
        MarkActiveSceneDirtyAndSaveForBatch();
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
        {
            var message = mixedRealityMode
                ? "PingPong Mixed Reality scene objects, passthrough, placement, and room sensing helpers are ready."
                : "PingPong Demo scene objects and prefab assets are ready.";
            EditorUtility.DisplayDialog("PingPong", message, "OK");
        }
    }

    [MenuItem("Tools/PICO ElderCare/Repair PingPong Demo Scene Objects")]
    public static void RepairPingPongDemoSceneObjects()
    {
        if (!EnsureEditMode()) return;

        OpenDemoSceneForBatchMode();
        EnsureFolders();
        TryCreateOrUpdateAdaptedPrefabs(false);
        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        DisableBackWall(environment.transform);
        RemoveGeneratedObject("Paddle_Left");
        var leftHand = SetupLeftHandGrabVisual(pingPong.transform);
        var table = GameObject.Find("Table");
        GameObject tableBlocker = null;
        if (table != null)
        {
            SetupTablePhysics(table);
            tableBlocker = SetupPlayerTableBlocker(pingPong.transform, table.transform);
            SetLayerRecursively(table, "Table");
            SetLayerRecursively(tableBlocker, "TableSafetyZone");
            SetupControllerTableLimiter(leftHand, table.transform);
            var rightPaddle = GameObject.Find("Paddle_Right");
            if (rightPaddle != null)
            {
                SetLayerRecursively(rightPaddle, "Racket");
                SetupControllerTableLimiter(rightPaddle, table.transform);
            }
        }
        else
        {
            tableBlocker = SetupPlayerTableBlocker(pingPong.transform);
        }
        var leftController = BindController(leftHand.GetComponent<ControllerTransformFollower>(), false);
        var managers = GetOrCreate("Managers").transform;
        var gripState = SetupSimpleGripInteractionState(managers);
        var leftBallGrabber = SetupControllerBallGrabber(managers, leftController, gripState);
        var spawner = Object.FindObjectOfType<BallSpawner>();
        SetupPlayerTableSafety(tableBlocker, table != null ? table.transform : null, spawner);
        if (table != null && spawner != null)
        {
            SetupTableDragHandle(pingPong.transform, table, leftController, leftBallGrabber, spawner, spawner.spawnPoint, spawner.targetPoint, tableBlocker != null ? tableBlocker.transform : null, false);
        }
        if (table != null)
        {
            SetupPingPongTableRecenterOnEnter(managers, table.transform);
        }
        RemoveRootLevelGeneratedBallObjects();
        RepairExistingBallObjectsInScene();
        MarkActiveSceneDirtyAndSaveForBatch();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("PingPong", "PingPong Demo scene objects have been repaired.", "OK");
    }

    private static GameObject FindRightPaddleInOpenScene()
    {
        var pingPong = GameObject.Find("PingPong");
        var child = pingPong != null ? pingPong.transform.Find("Paddle_Right") : null;
        return child != null
            ? child.gameObject
            : GameObject.Find("Paddle_Right") ?? GameObject.Find("PingPong/Paddle_Right");
    }

    private static bool TryResolvePaddleVisualHierarchy(GameObject paddle, out Transform visualOffset, out GameObject visual)
    {
        visualOffset = null;
        visual = null;
        if (paddle == null) return false;

        visualOffset = paddle.transform.Find("PaddleVisualOffset");
        if (visualOffset != null)
        {
            visual = FindVisualCustomRacket(visualOffset);
            if (visual != null) return true;
        }

        var directVisual = FindVisualCustomRacket(paddle.transform);
        if (visualOffset == null)
        {
            visualOffset = GetOrCreateVisualOffset(
                paddle.transform,
                "PaddleVisualOffset",
                DefaultPaddleVisualOffsetPosition,
                DefaultPaddleVisualOffsetRotation,
                DefaultPaddleVisualOffsetScale,
                false);
        }

        if (directVisual != null && directVisual.transform.parent != visualOffset)
        {
            directVisual.transform.SetParent(visualOffset, true);
            EditorUtility.SetDirty(directVisual);
            EditorUtility.SetDirty(visualOffset.gameObject);
        }

        visual = FindVisualCustomRacket(visualOffset);
        return visual != null;
    }

    private static GameObject TryFindPaddleVisual(GameObject paddle, out Transform visualOffset)
    {
        visualOffset = null;
        if (paddle == null) return null;

        visualOffset = paddle.transform.Find("PaddleVisualOffset");
        var visual = visualOffset != null ? FindVisualCustomRacket(visualOffset) : null;
        return visual != null ? visual : FindVisualCustomRacket(paddle.transform);
    }

    private static GameObject FindVisualCustomRacket(Transform parent)
    {
        if (parent == null) return null;

        var exact = parent.Find("Visual_CustomRacket");
        if (exact != null) return exact.gameObject;

        foreach (Transform child in parent)
        {
            if (child == null || child.name == "PaddleHitZone" || child.name == "PaddleVisualOffset") continue;
            if (!IsPaddleVisualChildName(child.name)) continue;
            if (!TryGetRendererBounds(child.gameObject, out _)) continue;
            return child.gameObject;
        }

        return null;
    }

    private static Collider FindPaddleHitZoneCollider(Transform paddle)
    {
        if (paddle == null) return null;

        var hitZone = paddle.Find("PaddleHitZone");
        return hitZone != null ? hitZone.GetComponent<Collider>() : null;
    }

    private static void SyncVisualPoseOffsetFields(Transform movedTransform)
    {
        if (movedTransform == null) return;

        var pose = movedTransform.GetComponent<VisualPoseOffset>();
        if (pose == null) return;

        var target = pose.visualRoot != null ? pose.visualRoot : movedTransform;
        pose.localPositionOffset = target.localPosition;
        pose.localRotationOffsetEuler = target.localEulerAngles;
        pose.localScale = target.localScale;
        EditorUtility.SetDirty(pose);
    }

    private static void ReplacePaddleVisualOnly(GameObject paddle, GameObject sourceModel)
    {
        if (paddle == null || sourceModel == null) return;

        RemoveOldDirectPaddleVisualChildren(paddle.transform);
        var offset = GetOrCreateVisualOffset(
            paddle.transform,
            "PaddleVisualOffset",
            DefaultPaddleVisualOffsetPosition,
            DefaultPaddleVisualOffsetRotation,
            DefaultPaddleVisualOffsetScale,
            false);
        RemoveOldPaddleVisualChildren(offset);

        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual == null)
        {
            visual = Object.Instantiate(sourceModel);
        }

        if (visual == null)
        {
            Debug.LogError($"Could not instantiate custom paddle model at '{AssetDatabase.GetAssetPath(sourceModel)}'.");
            return;
        }

        visual.name = "Visual_CustomRacket";
        visual.transform.SetParent(offset, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripVisualGameplayComponents(visual);
        FitVisualToTarget(visual, PaddleColliderCenter, PaddleColliderSize, 0.95f, false);

        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(offset.gameObject);
    }

    private static void RemoveOldDirectPaddleVisualChildren(Transform paddle)
    {
        if (paddle == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in paddle)
        {
            if (child == null || child.name == "PaddleHitZone" || child.name == "PaddleVisualOffset") continue;
            if (IsPaddleVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static void RemoveOldPaddleVisualChildren(Transform visualOffset)
    {
        if (visualOffset == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in visualOffset)
        {
            if (child == null) continue;
            if (child.name == "Visual_CustomRacket" || IsPaddleVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static bool IsPaddleVisualChildName(string childName)
    {
        return !string.IsNullOrEmpty(childName) &&
               (childName.StartsWith("Visual_") ||
                childName.StartsWith("CustomPaddleVisual") ||
                childName.StartsWith("RacketVisual") ||
                childName.StartsWith("PaddleVisual"));
    }

    private static GameObject FindLeftGrabHandInOpenScene()
    {
        var pingPong = GameObject.Find("PingPong");
        var child = pingPong != null ? pingPong.transform.Find("Left_GrabHand") : null;
        return child != null
            ? child.gameObject
            : GameObject.Find("Left_GrabHand") ?? GameObject.Find("PingPong/Left_GrabHand");
    }

    private static void ReplaceLeftHandVisualOnly(GameObject hand, GameObject sourceModel)
    {
        if (hand == null || sourceModel == null) return;

        RemoveOldDirectLeftHandVisualChildren(hand.transform);
        var offset = GetOrCreateVisualOffset(
            hand.transform,
            "HandVisualOffset",
            DefaultHandVisualOffsetPosition,
            DefaultHandVisualOffsetRotation,
            DefaultHandVisualOffsetScale,
            false);
        RemoveOldLeftHandVisualChildren(offset);

        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual == null)
        {
            visual = Object.Instantiate(sourceModel);
        }

        if (visual == null)
        {
            Debug.LogError($"Could not instantiate custom hand model at '{AssetDatabase.GetAssetPath(sourceModel)}'.");
            return;
        }

        visual.name = "HandVisual";
        visual.transform.SetParent(offset, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripVisualGameplayComponents(visual);
        FitVisualToTarget(visual, Vector3.zero, HandVisualTargetSize, 0.95f, true);
        ConfigureVisualHandGripAnimator(hand, visual.transform);

        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(offset.gameObject);
    }

    private static void ConfigureVisualHandGripAnimator(GameObject hand, Transform handVisual)
    {
        if (hand == null) return;

        var visualAnimator = EnsureComponent<VisualHandGripAnimator>(hand);
        if (visualAnimator != null)
        {
            visualAnimator.controllerNode = XRNode.LeftHand;
            visualAnimator.handVisual = handVisual;
            visualAnimator.autoFindFingerBones = true;
            visualAnimator.closedPoseSpeed = 12f;
            visualAnimator.RebuildPoseCache();
        }

        var generatedPoseAnimator = hand.GetComponent<GrabHandPoseAnimator>();
        if (generatedPoseAnimator != null)
        {
            generatedPoseAnimator.enabled = false;
        }
    }

    private static void RemoveOldDirectLeftHandVisualChildren(Transform hand)
    {
        if (hand == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in hand)
        {
            if (child == null || child.name == "HandVisualOffset") continue;
            if (IsLeftHandVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static void RemoveOldLeftHandVisualChildren(Transform visualOffset)
    {
        if (visualOffset == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in visualOffset)
        {
            if (child == null) continue;
            if (child.name == "HandVisual" || IsLeftHandVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static bool IsLeftHandVisualChildName(string childName)
    {
        return !string.IsNullOrEmpty(childName) &&
               (childName == "Palm" ||
                childName == "Thumb" ||
                childName == "IndexFinger" ||
                childName == "MiddleFinger" ||
                childName == "RingFinger" ||
                childName == "LittleFinger" ||
                childName == "HandVisual" ||
                childName.StartsWith("Visual_") ||
                childName.StartsWith("CustomHandVisual") ||
                childName.StartsWith("LeftHandVisual"));
    }

    private static GameObject FindTableInOpenScene()
    {
        var pingPong = GameObject.Find("PingPong");
        var child = pingPong != null ? pingPong.transform.Find("Table") : null;
        return child != null
            ? child.gameObject
            : GameObject.Find("Table") ?? GameObject.Find("PingPong/Table");
    }

    private static void SyncStandaloneNetColliderHeight(Transform tableTransform, float deltaY)
    {
        if (tableTransform == null || Mathf.Abs(deltaY) <= 0.000001f) return;

        var childNetCollider = tableTransform.Find("NetCollider");
        if (childNetCollider != null)
        {
            return;
        }

        var netCollider = GameObject.Find("NetCollider");
        if (netCollider == null || netCollider.transform.IsChildOf(tableTransform)) return;

        netCollider.transform.position += Vector3.up * deltaY;
        EditorUtility.SetDirty(netCollider);
    }

    private static void TuneBallSpawners(Transform tableTransform, float targetTableTopY)
    {
        var spawners = Object.FindObjectsOfType<BallSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("No BallSpawner found. Table height was tuned, but serve bounce settings were not synchronized.");
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;

            spawner.serveSpeed = Mathf.Max(spawner.serveSpeed, 3.0f);
            spawner.upwardArc = Mathf.Max(spawner.upwardArc, 0.42f);
            spawner.tableBounceWorldY = targetTableTopY + PingPongGeometry.BallRadius;
            spawner.minimumNetClearanceHeight = targetTableTopY + PingPongGeometry.NetHeight + 0.16f;
            if (tableTransform != null)
            {
                spawner.tableTransform = tableTransform;
                spawner.useTableRelativeServeTargets = true;
                spawner.netLocalZ = 0f;
                spawner.tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
                spawner.netWorldZ = tableTransform.TransformPoint(new Vector3(0f, 0f, spawner.netLocalZ)).z;
                spawner.tableBounceWorldZ = tableTransform.TransformPoint(new Vector3(0f, 0f, spawner.tableBounceLocalZ)).z;
            }

            if (Mathf.Approximately(spawner.horizontalRandomRange, 0f))
            {
                spawner.horizontalRandomRange = 0.18f;
            }

            spawner.spawnedBallMass = PingPongGeometry.BallMass;
            spawner.spawnedBallDrag = 0.015f;
            spawner.spawnedBallAngularDrag = 0.04f;
            spawner.spawnedBallBounciness = 0.86f;
            spawner.spawnedBallDynamicFriction = 0.01f;
            spawner.spawnedBallStaticFriction = 0.01f;

            EditorUtility.SetDirty(spawner);
        }
    }

    private static void TuneControllerTableLimiters(Transform tableTransform, float targetTableTopY)
    {
        var limiters = Object.FindObjectsOfType<ControllerTableCollisionLimiter>(true);
        foreach (var limiter in limiters)
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableTransform;
            limiter.tableTopY = targetTableTopY;
            EditorUtility.SetDirty(limiter);
        }
    }

    private static void TuneTableDragHandles(Transform tableTransform, float targetTableTopY)
    {
        var dragHandles = Object.FindObjectsOfType<TableDragHandle>(true);
        foreach (var dragHandle in dragHandles)
        {
            if (dragHandle == null) continue;

            dragHandle.tableRoot = tableTransform;
            dragHandle.standardTableTopHeight = targetTableTopY;
            dragHandle.tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
            dragHandle.minimumNetClearanceAboveNet = 0.16f;
            dragHandle.SyncHeightDependentValues();
            EditorUtility.SetDirty(dragHandle);
        }

    }

    private static void TuneTablePassiveMotionLock(GameObject table, float targetTableTopY)
    {
        if (table == null) return;

        var passiveLock = table.GetComponent<TablePassiveMotionLock>();
        if (passiveLock == null) return;

        passiveLock.standardTableTopHeight = targetTableTopY;
        passiveLock.AcceptCurrentTransform();
        EditorUtility.SetDirty(passiveLock);
    }

    private static void TuneTableSafety(Transform tableTransform, float targetTableTopY)
    {
        var tableCenterHeightAboveFloor = targetTableTopY - PingPongGeometry.TableThickness * 0.5f;
        var safeties = Object.FindObjectsOfType<PingPongPlayerTableSafety>(true);
        foreach (var safety in safeties)
        {
            if (safety == null) continue;

            safety.tableTransform = tableTransform;
            safety.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
            EditorUtility.SetDirty(safety);
        }
    }

    private static PingPongSpatialSanitizer SetupPingPongSpatialSanitizer(Transform parent)
    {
        var host = parent != null ? parent.gameObject : GameObject.Find("Managers") ?? GameObject.Find("PingPong");
        if (host == null) return null;

        var sanitizer = EnsureComponent<PingPongSpatialSanitizer>(host);
        if (sanitizer == null) return null;

        sanitizer.sanitizeOnStart = true;
        sanitizer.targetTableTopY = TunedTableTopY;
        sanitizer.uiPreferredY = 1.5f;
        sanitizer.uiMinY = 1.25f;
        sanitizer.uiMaxY = 1.75f;
        sanitizer.fixTableHeight = true;
        sanitizer.fixUiHeight = true;
        sanitizer.ignoreCeilingPlanes = true;
        EditorUtility.SetDirty(host);
        return sanitizer;
    }

    private static PingPongSpatialSanitizer EnsurePingPongSpatialSanitizer()
    {
        var host = GameObject.Find("Managers") ?? GameObject.Find("PingPong");
        if (host == null)
        {
            host = new GameObject("PingPongManagers");
        }

        return SetupPingPongSpatialSanitizer(host.transform);
    }

    private static PingPongTableRecenterOnEnter SetupPingPongTableRecenterOnEnter(Transform parent, Transform table)
    {
        var host = parent != null ? parent.gameObject : GameObject.Find("Managers") ?? GameObject.Find("PingPong");
        if (host == null) return null;

        var recenter = EnsureComponent<PingPongTableRecenterOnEnter>(host);
        if (recenter == null) return null;

        recenter.recenterOnStart = true;
        recenter.tableRoot = table != null ? table : PingPongTableRecenterOnEnter.FindTableRoot();
        recenter.headTransform = Camera.main != null ? Camera.main.transform : null;
        recenter.tableDistanceInFront = 1.7f;
        recenter.targetTableTopY = PingPongGeometry.TableTopHeight;
        recenter.preserveCurrentTableTopHeight = true;
        recenter.rotateTableToFacePlayer = true;
        recenter.yawOffsetDegrees = 0f;
        recenter.syncBallSpawner = true;
        recenter.syncServeTransforms = true;
        recenter.syncDifficultyPanel = true;
        recenter.syncTableHelpers = true;
        recenter.acceptPassiveLockAfterMove = true;
        recenter.startDelaySeconds = 0.15f;
        EditorUtility.SetDirty(host);
        EditorUtility.SetDirty(recenter);
        return recenter;
    }

    private static void MarkPingPongSpatialRepairDirty(GameObject table)
    {
        if (table != null)
        {
            EditorUtility.SetDirty(table);
            var tableCollider = table.GetComponent<BoxCollider>();
            if (tableCollider != null)
            {
                EditorUtility.SetDirty(tableCollider);
            }

            var passiveLock = table.GetComponent<TablePassiveMotionLock>();
            if (passiveLock != null)
            {
                EditorUtility.SetDirty(passiveLock);
            }
        }

        MarkObjectsDirty(Object.FindObjectsOfType<BallSpawner>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<ControllerTableCollisionLimiter>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<TableDragHandle>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<PingPongPlayerTableSafety>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<PingPongOpenSpaceTablePlacer>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<PingPongRoomPlaneAligner>(true));
        MarkObjectsDirty(Object.FindObjectsOfType<ComfortWorldSpaceUIPlacer>(true));
    }

    private static void MarkObjectsDirty<T>(T[] objects) where T : Object
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }

    private static void MarkBallSpawnerServeTransformsDirty(BallSpawner[] spawners)
    {
        if (spawners == null) return;

        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;

            if (spawner.spawnPoint != null)
            {
                EditorUtility.SetDirty(spawner.spawnPoint);
            }

            if (spawner.targetPoint != null)
            {
                EditorUtility.SetDirty(spawner.targetPoint);
            }
        }
    }

    private static string FormatSpatialValue(float value)
    {
        return float.IsNaN(value) ? "not found" : value.ToString("0.###");
    }

    private static string FormatVector(Vector3? value)
    {
        if (!value.HasValue)
        {
            return "not found";
        }

        var vector = value.Value;
        return $"({vector.x:0.###}, {vector.y:0.###}, {vector.z:0.###})";
    }

    private static void WarnIfHighUi(string objectName, GameObject uiObject)
    {
        if (uiObject == null) return;
        if (uiObject.transform.position.y > 1.75f)
        {
            Debug.LogWarning($"{objectName} is above comfort height: {uiObject.transform.position.y:0.###}m.");
        }
    }

    private static void WarnIfSuspiciousPlaneHeights(Camera camera)
    {
        var headY = camera != null ? camera.transform.position.y : 1.6f;
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null) continue;
            if (!IsPotentialMrPlaneName(transform.name)) continue;

            var y = transform.position.y;
            if (y > 0.4f || y > headY - 0.6f)
            {
                Debug.LogWarning($"Potential MR plane '{transform.name}' has suspicious height {y:0.###}m and may be a ceiling/wall candidate.");
            }
        }
    }

    private static bool IsPotentialMrPlaneName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        return objectName == "MRDetectedPlaneTemplate" ||
               objectName.StartsWith("MRDetectedPlaneTemplate", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXR_Plane", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXRPlane", System.StringComparison.Ordinal) ||
               objectName == "Plane" ||
               objectName.StartsWith("Plane ", System.StringComparison.Ordinal) ||
               objectName.StartsWith("Plane(", System.StringComparison.Ordinal);
    }

    private static GameObject FindObjectByNameIncludingInactive(string objectName)
    {
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindObjectByNameContainsIncludingInactive(string partialName)
    {
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name.Contains(partialName))
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static void ReplaceTableVisualOnly(GameObject table, GameObject sourceModel)
    {
        if (table == null || sourceModel == null) return;

        RemoveOldDirectTableVisualChildren(table.transform);
        var offset = GetOrCreateVisualOffset(
            table.transform,
            "TableVisualOffset",
            DefaultTableVisualOffsetPosition,
            DefaultTableVisualOffsetRotation,
            DefaultTableVisualOffsetScale,
            false);
        RemoveOldTableVisualChildren(offset);

        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual == null)
        {
            visual = Object.Instantiate(sourceModel);
        }

        if (visual == null)
        {
            Debug.LogError($"Could not instantiate custom table model at '{AssetDatabase.GetAssetPath(sourceModel)}'.");
            return;
        }

        visual.name = "Visual_CustomTable";
        visual.transform.SetParent(offset, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripVisualGameplayComponents(visual);
        FitVisualToTarget(visual, Vector3.zero, TableColliderWorldSize, 0.98f, true);

        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(offset.gameObject);
    }

    private static void RemoveOldDirectTableVisualChildren(Transform table)
    {
        if (table == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in table)
        {
            if (child == null || child.name == "NetCollider" || child.name == "TableVisualOffset") continue;
            if (IsTableVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static void RemoveOldTableVisualChildren(Transform visualOffset)
    {
        if (visualOffset == null) return;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in visualOffset)
        {
            if (child == null) continue;
            if (child.name == "Visual_CustomTable" || IsTableVisualChildName(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static bool IsTableVisualChildName(string childName)
    {
        return !string.IsNullOrEmpty(childName) &&
               (childName == "TableTopVisual" ||
                childName == "NetVisual" ||
                childName == "LegFrontLeft" ||
                childName == "LegFrontRight" ||
                childName == "LegBackLeft" ||
                childName == "LegBackRight" ||
                childName == "NetPostLeft" ||
                childName == "NetPostRight" ||
                childName == "CustomTableVisual" ||
                childName == "TableVisual" ||
                childName == "Visual_CustomTable" ||
                childName == "TableVisualOffset");
    }

    private static Transform GetOrCreateVisualOffset(
        Transform parent,
        string childName,
        Vector3 defaultPosition,
        Vector3 defaultRotationEuler,
        Vector3 defaultScale,
        bool forceDefaultPose)
    {
        var offset = parent.Find(childName);
        var created = false;
        if (offset == null)
        {
            offset = new GameObject(childName).transform;
            offset.SetParent(parent, false);
            created = true;
        }

        var existingPose = offset.GetComponent<VisualPoseOffset>();
        var pose = EnsureComponent<VisualPoseOffset>(offset.gameObject);
        if (pose != null)
        {
            pose.visualRoot = offset;
            pose.applyOnValidate = true;
            pose.applyOnStart = true;

            if (created || forceDefaultPose)
            {
                pose.localPositionOffset = defaultPosition;
                pose.localRotationOffsetEuler = defaultRotationEuler;
                pose.localScale = defaultScale;
                pose.ApplyPose();
            }
            else if (existingPose == null)
            {
                pose.localPositionOffset = offset.localPosition;
                pose.localRotationOffsetEuler = offset.localEulerAngles;
                pose.localScale = offset.localScale;
            }
        }
        else if (created || forceDefaultPose)
        {
            offset.localPosition = defaultPosition;
            offset.localRotation = Quaternion.Euler(defaultRotationEuler);
            offset.localScale = defaultScale;
        }

        return offset;
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath("Assets/_Project");
        EnsureFolderPath("Assets/_Project/Prefabs");
        EnsureFolderPath(PrefabRoot);
        EnsureFolderPath("Assets/_Project/Materials");
        EnsureFolderPath(MaterialRoot);
        EnsureFolderPath(FontRoot);
        EnsureFolderPath(ExternalRoot);
        EnsureFolderPath(OriginalRoot);
        EnsureFolderPath(AdaptedRoot);
        EnsureFolderPath(AdaptedMaterialRoot);
        EnsureFolderPath(CustomRoot);
        EnsureFolderPath(CustomModelRoot);
    }

    private static void OpenDemoSceneForBatchMode()
    {
        if (!Application.isBatchMode) return;

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.path == DemoScenePath) return;

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(DemoScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
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

    private static void ConfigureVirtualRealityProjectSettings()
    {
        var config = PXR_ProjectSetting.GetProjectConfig();
        if (config == null) return;

        config.openMRC = false;
        config.videoSeeThrough = false;
        config.spatialAnchor = false;
        config.sceneCapture = false;
        config.spatialMesh = false;
        config.planeDetection = false;
        config.mrSafeguard = false;
        PXR_ProjectSetting.SaveAssets();
    }

    private static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderPath(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color)
    {
        return CreateOrLoadMaterial(materialName, color, MaterialRoot);
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color, string materialRoot)
    {
        var matPath = $"{materialRoot}/{materialName}.mat";
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
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    private static GameObject LoadAdaptedPrefab(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{AdaptedRoot}/{assetName}_Adapted.prefab");
    }

    private static GameObject LoadAdaptedBallPrefab()
    {
        var path = $"{AdaptedRoot}/PingPongBall_Adapted.prefab";
        RepairBallPrefabAsset(path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Vector3 GetInstanceScale(GameObject prefab, Vector3 fallbackScale)
    {
        return IsAdaptedPrefab(prefab)
            ? Vector3.one
            : fallbackScale;
    }

    private static bool IsAdaptedPrefab(GameObject prefab)
    {
        var path = AssetDatabase.GetAssetPath(prefab);
        return !string.IsNullOrEmpty(path) && path.StartsWith(AdaptedRoot);
    }

    private static GameObject LoadPreferredPaddleModel()
    {
        return LoadFirstModel(CustomPaddleModelPaths) ??
               AssetDatabase.LoadAssetAtPath<GameObject>($"{OriginalModelRoot}/PPPaddle.fbx");
    }

    private static GameObject LoadPreferredHandModel()
    {
        return LoadFirstModel(CustomHandModelPaths);
    }

    private static GameObject LoadPreferredTableModel()
    {
        return LoadFirstModel(CustomTableModelPaths);
    }

    private static GameObject LoadPreferredBallModel()
    {
        return LoadFirstModel(CustomBallModelPaths);
    }

    private static GameObject LoadFirstModel(IEnumerable<string> modelPaths)
    {
        foreach (var modelPath in modelPaths)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model != null) return model;
        }

        return null;
    }

    private static bool IsCustomModel(GameObject model)
    {
        var path = AssetDatabase.GetAssetPath(model);
        return !string.IsNullOrEmpty(path) && path.StartsWith(CustomModelRoot);
    }

    private static void CreateOrUpdateAdaptedPrefabs()
    {
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var paddleMaterial = CreateOrLoadMaterial("VRTableTennis_PaddleRed", new Color(0.66f, 0.08f, 0.07f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        var ballMaterial = CreateOrLoadMaterial("VRTableTennis_BallWhite", Color.white, AdaptedMaterialRoot);

        CreateOrUpdateTablePrefab(tableMaterial, netMaterial, darkMaterial);
        CreateOrUpdateNetPrefab(netMaterial);
        CreateOrUpdatePaddlePrefab(paddleMaterial, darkMaterial);
        CreateOrUpdateAdaptedBallPrefab(ballMaterial);
    }

    private static void TryCreateOrUpdateAdaptedPrefabs(bool includeBall)
    {
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var paddleMaterial = CreateOrLoadMaterial("VRTableTennis_PaddleRed", new Color(0.66f, 0.08f, 0.07f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        var ballMaterial = CreateOrLoadMaterial("VRTableTennis_BallWhite", Color.white, AdaptedMaterialRoot);

        TryRunAssetStep("PingPongTable_Adapted", () => CreateOrUpdateTablePrefab(tableMaterial, netMaterial, darkMaterial));
        TryRunAssetStep("PingPongNet_Adapted", () => CreateOrUpdateNetPrefab(netMaterial));
        TryRunAssetStep("PingPongPaddle_Adapted", () => CreateOrUpdatePaddlePrefab(paddleMaterial, darkMaterial));

        if (includeBall)
        {
            TryRunAssetStep("PingPongBall_Adapted", () => CreateOrUpdateAdaptedBallPrefab(ballMaterial));
        }
    }

    private static void TryRunAssetStep(string stepName, System.Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{stepName} generation failed and was skipped. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void CreateOrUpdateTablePrefab(Material tableMaterial, Material netMaterial, Material darkMaterial)
    {
        var root = new GameObject("PingPongTable_Adapted");
        BuildStandardTableVisual(root.transform, tableMaterial, netMaterial, darkMaterial);

        var tableCollider = root.AddComponent<BoxCollider>();
        tableCollider.size = TableColliderWorldSize;
        tableCollider.center = Vector3.zero;
        ConfigureSurface(root, PingPongSurfaceType.Table);

        var netCollider = new GameObject("NetCollider");
        netCollider.transform.SetParent(root.transform, false);
        netCollider.transform.localPosition = PingPongGeometry.NetLocalCenter;
        var box = netCollider.AddComponent<BoxCollider>();
        box.size = NetColliderWorldSize;
        box.isTrigger = true;
        ConfigureSurface(netCollider, PingPongSurfaceType.Net);

        SaveAdaptedPrefab(root, "PingPongTable");
    }

    private static void BuildStandardTableVisual(Transform root, Material tableMaterial, Material netMaterial, Material darkMaterial)
    {
        ConfigureVisualPrimitive(root, "TableTopVisual", PrimitiveType.Cube, Vector3.zero, Vector3.zero, TableColliderWorldSize, tableMaterial);
        ConfigureVisualPrimitive(root, "NetVisual", PrimitiveType.Cube, PingPongGeometry.NetLocalCenter, Vector3.zero, NetColliderWorldSize, netMaterial);

        var legHeight = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness;
        var legCenterY = -PingPongGeometry.TableThickness * 0.5f - legHeight * 0.5f;
        var legOffsetX = PingPongGeometry.TableWidth * 0.5f - 0.09f;
        var legOffsetZ = PingPongGeometry.TableLength * 0.5f - 0.16f;
        var legSize = new Vector3(0.045f, legHeight, 0.045f);

        ConfigureVisualPrimitive(root, "LegFrontLeft", PrimitiveType.Cube, new Vector3(-legOffsetX, legCenterY, -legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegFrontRight", PrimitiveType.Cube, new Vector3(legOffsetX, legCenterY, -legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegBackLeft", PrimitiveType.Cube, new Vector3(-legOffsetX, legCenterY, legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegBackRight", PrimitiveType.Cube, new Vector3(legOffsetX, legCenterY, legOffsetZ), Vector3.zero, legSize, darkMaterial);

        var postHeight = PingPongGeometry.NetHeight + 0.06f;
        var postCenterY = PingPongGeometry.TableThickness * 0.5f + postHeight * 0.5f;
        var postOffsetX = PingPongGeometry.TableWidth * 0.5f + 0.02f;
        var postSize = new Vector3(0.025f, postHeight, 0.025f);

        ConfigureVisualPrimitive(root, "NetPostLeft", PrimitiveType.Cube, new Vector3(-postOffsetX, postCenterY, 0f), Vector3.zero, postSize, darkMaterial);
        ConfigureVisualPrimitive(root, "NetPostRight", PrimitiveType.Cube, new Vector3(postOffsetX, postCenterY, 0f), Vector3.zero, postSize, darkMaterial);
    }

    private static void CreateOrUpdateNetPrefab(Material netMaterial)
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "PingPongNet_Adapted";
        temp.transform.localScale = NetColliderWorldSize;
        var collider = temp.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        ConfigureSurface(temp, PingPongSurfaceType.Net);

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = netMaterial;

        SaveAdaptedPrefab(temp, "PingPongNet");
    }

    private static void CreateOrUpdatePaddlePrefab(Material paddleMaterial, Material darkMaterial)
    {
        var sourceModel = LoadPreferredPaddleModel();
        if (sourceModel == null) return;

        var root = new GameObject("PingPongPaddle_Adapted");
        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual != null)
        {
            var sourcePath = AssetDatabase.GetAssetPath(sourceModel);
            visual.name = $"Visual_{Path.GetFileNameWithoutExtension(sourcePath)}";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            if (IsCustomModel(sourceModel))
            {
                StripVisualGameplayComponents(visual);
                FitVisualToTarget(visual, PaddleColliderCenter, PaddleColliderSize, 0.95f, false);
            }
            else
            {
                visual.transform.localScale = Vector3.one * 4f;
                AssignMaterialsByName(visual, paddleMaterial, paddleMaterial, darkMaterial);
            }
        }

        var collider = root.AddComponent<BoxCollider>();
        collider.center = PaddleColliderCenter;
        collider.size = PaddleColliderSize;
        ConfigureSurface(root, PingPongSurfaceType.PaddleBody);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var hitZone = new GameObject("PaddleHitZone");
        hitZone.transform.SetParent(root.transform, false);
        var hitZoneCollider = hitZone.AddComponent<BoxCollider>();
        hitZoneCollider.center = PaddleHitZoneCenter;
        hitZoneCollider.size = PaddleHitZoneSize;
        hitZoneCollider.isTrigger = true;
        ConfigureSurface(hitZone, PingPongSurfaceType.PaddleHitZone);

        root.AddComponent<PaddleFollower>();
        ConfigurePaddleTracker(root.AddComponent<PaddleVelocityTracker>());

        SaveAdaptedPrefab(root, "PingPongPaddle");
    }

    private static void CreateOrUpdateAdaptedBallPrefab(Material ballMaterial)
    {
        GameObject temp = null;

        try
        {
            temp = new GameObject("PingPongBall_Adapted");
            temp.name = "PingPongBall_Adapted";
            temp.transform.localScale = Vector3.one;

            var visualModel = LoadPreferredBallModel();
            if (visualModel != null)
            {
                var visual = PrefabUtility.InstantiatePrefab(visualModel) as GameObject;
                if (visual != null)
                {
                    var sourcePath = AssetDatabase.GetAssetPath(visualModel);
                    visual.name = $"Visual_{Path.GetFileNameWithoutExtension(sourcePath)}";
                    visual.transform.SetParent(temp.transform, false);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;
                    StripVisualGameplayComponents(visual);
                    FitVisualToTarget(visual, Vector3.zero, Vector3.one, 0.96f, true);
                }
            }

            if (temp.transform.childCount == 0)
            {
                CreateFallbackBallVisual(temp.transform, ballMaterial);
            }

            var collider = temp.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            ConfigureBallComponents(temp);
            AttachBounceAudio(temp);

            SaveAdaptedPrefab(temp, "PingPongBall");
            temp = null;
        }
        finally
        {
            if (temp != null)
            {
                Object.DestroyImmediate(temp);
            }
        }
    }

    private static void CreateFallbackBallVisual(Transform parent, Material ballMaterial)
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual_Sphere";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = ballMaterial;
    }

    private static void StripVisualGameplayComponents(GameObject visual)
    {
        if (visual == null) return;

        foreach (var child in visual.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        foreach (var joint in visual.GetComponentsInChildren<Joint>(true))
        {
            Object.DestroyImmediate(joint);
        }

        foreach (var rb in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rb);
        }

        foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null)
            {
                Object.DestroyImmediate(behaviour);
            }
        }
    }

    private static void FitVisualToTarget(GameObject visual, Vector3 targetCenter, Vector3 targetSize, float fill, bool includeY)
    {
        if (visual == null) return;
        if (!TryGetRendererBounds(visual, out var bounds)) return;

        var scale = float.PositiveInfinity;
        AddScaleCandidate(ref scale, targetSize.x, bounds.size.x);
        AddScaleCandidate(ref scale, targetSize.z, bounds.size.z);
        if (includeY)
        {
            AddScaleCandidate(ref scale, targetSize.y, bounds.size.y);
        }

        if (float.IsInfinity(scale) || scale <= 0f) return;

        visual.transform.localScale *= scale * Mathf.Max(0.01f, fill);

        if (!TryGetRendererBounds(visual, out bounds)) return;
        var parent = visual.transform.parent;
        var localCenter = parent != null ? parent.InverseTransformPoint(bounds.center) : bounds.center;
        visual.transform.localPosition += targetCenter - localCenter;
    }

    private static void AddScaleCandidate(ref float scale, float targetSize, float sourceSize)
    {
        if (targetSize <= 0.0001f || sourceSize <= 0.0001f) return;
        scale = Mathf.Min(scale, targetSize / sourceSize);
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        var hasBounds = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void AssignMaterialsByName(GameObject root, Material primary, Material net, Material dark)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var sourceName = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;
                if (sourceName.Contains("net") || sourceName.Contains("white"))
                {
                    materials[i] = net;
                }
                else if (sourceName.Contains("black") || sourceName.Contains("dark"))
                {
                    materials[i] = dark;
                }
                else
                {
                    materials[i] = primary;
                }
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static void SaveAdaptedPrefab(GameObject root, string assetName)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{AdaptedRoot}/{assetName}_Adapted.prefab");
        Object.DestroyImmediate(root);
    }

    private static bool EnsureEditMode()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return true;

        Debug.LogError("PingPong scene builder tools must be run in Edit Mode. Exit Play Mode and run the tool again.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("PingPong", "Please exit Play Mode before running this tool.", "OK");
        }

        return false;
    }

    private static void AttachBounceAudio(GameObject target)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        if (clip == null) return;

        var source = EnsureComponent<AudioSource>(target);
        if (source == null) return;
        source.clip = clip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
    }

    private static void SetupFeedbackAudio(GameObject feedback, HitFeedbackManager feedbackManager)
    {
        if (feedbackManager == null) return;

        var bounceClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        var whooshClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/ping_pong_whoosh.mp3");

        var source = feedbackManager.hitAudioSource;
        if (source == null)
        {
            source = EnsureComponent<AudioSource>(feedback);
        }

        if (source == null) return;
        source.clip = bounceClip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        feedbackManager.hitAudioSource = source;
        feedbackManager.paddleHitClip = bounceClip;
        feedbackManager.tableBounceClip = bounceClip;
        feedbackManager.netBounceClip = bounceClip;
        feedbackManager.fastSwingClip = whooshClip;
        feedbackManager.minAudibleSpeed = 0.25f;
        feedbackManager.fullVolumeSpeed = 8f;
        feedbackManager.fastSwingSpeed = 5.2f;
        feedbackManager.fastSwingVolume = 0.28f;

        var bounceObject = GetOrCreateChild("BounceAudioSource", feedback.transform);
        var bounceSource = EnsureComponent<AudioSource>(bounceObject);
        if (bounceSource != null)
        {
            bounceSource.clip = bounceClip;
            bounceSource.playOnAwake = false;
            bounceSource.spatialBlend = 1f;
            feedbackManager.bounceAudioSource = bounceSource;
        }
    }

    private static GameObject LoadOrCreatePrefabAsset(string assetName, PrimitiveType primitiveType, Vector3 scale, Material material)
    {
        var path = $"{PrefabRoot}/{assetName}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var temp = GameObject.CreatePrimitive(primitiveType);
        temp.name = assetName;
        temp.transform.localScale = scale;

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject CreateOrUpdateBallPrefab()
    {
        var path = $"{PrefabRoot}/PingPongBall.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var ballMaterial = CreateOrLoadMaterial("BallWhite", Color.white);
        GameObject temp = null;

        try
        {
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            temp.name = "PingPongBall";
            temp.transform.localScale = PingPongGeometry.BallPrefabScale;

            var renderer = temp.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = ballMaterial;

            var collider = temp.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = temp.AddComponent<SphereCollider>();
            }
            collider.radius = 0.5f;

            ConfigureBallComponents(temp);

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            return saved;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PingPongBall prefab generation failed. {ex.GetType().Name}: {ex.Message}");
            return prefab;
        }
        finally
        {
            if (temp != null)
            {
                Object.DestroyImmediate(temp);
            }
        }
    }

    private static void ConfigureBallComponents(GameObject ball)
    {
        if (ball == null) return;

        var rb = EnsureComponent<Rigidbody>(ball);
        if (rb == null) return;

        ball.transform.localScale = PingPongGeometry.BallPrefabScale;
        SetLayerRecursively(ball, "Ball");
        rb.mass = PingPongGeometry.BallMass;
        var pingPongBall = EnsureComponent<PingPongBall>(ball);
        rb.drag = pingPongBall != null && pingPongBall.useAerodynamics ? 0f : PingPongGeometry.BallDrag;
        rb.angularDrag = PingPongGeometry.BallAngularDrag;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        PingPongBall.ConfigureSpinLimit(rb, pingPongBall != null ? pingPongBall.maxAngularVelocity : PingPongBall.DefaultMaxAngularVelocity);

        var collider = EnsureComponent<SphereCollider>(ball);
        if (collider != null)
        {
            collider.radius = 0.5f;
            collider.isTrigger = false;
        }

        if (pingPongBall != null)
        {
            pingPongBall.ConfigureGameplayCollisionFilter(true);
        }

        EnsureComponent<BallLifetime>(ball);
    }

    private static void RepairBallPrefabAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;

        try
        {
            ConfigureBallComponents(root);
            AttachBounceAudio(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            RestoreActiveScene(previousScene);
        }
    }

    private static void RestoreActiveScene(UnityEngine.SceneManagement.Scene previousScene)
    {
        if (!previousScene.IsValid()) return;

        if (previousScene.isLoaded)
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
        }
        else if (!string.IsNullOrEmpty(previousScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(previousScene.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
    }

    private static void RepairExistingBallObjectsInScene()
    {
        foreach (var ball in Object.FindObjectsOfType<PingPongBall>(true))
        {
            ConfigureBallComponents(ball.gameObject);
            EditorUtility.SetDirty(ball.gameObject);
        }

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name == "PingPongBall" || transform.name == "PingPongBall_Adapted")
            {
                ConfigureBallComponents(transform.gameObject);
                EditorUtility.SetDirty(transform.gameObject);
            }
        }
    }

    private static void ValidateBallSpawnerBindings(BallSpawner spawner)
    {
        if (spawner == null) return;

        var hasError = false;
        if (spawner.ballPrefab == null)
        {
            Debug.LogError("BallSpawner.ballPrefab is not assigned.");
            hasError = true;
        }
        else if (spawner.ballPrefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogError($"BallSpawner.ballPrefab '{spawner.ballPrefab.name}' has no Rigidbody.");
            hasError = true;
        }

        if (spawner.spawnPoint == null)
        {
            Debug.LogError("BallSpawner.spawnPoint is not assigned.");
            hasError = true;
        }

        if (spawner.targetPoint == null)
        {
            Debug.LogError("BallSpawner.targetPoint is not assigned.");
            hasError = true;
        }

        if (spawner.ballContainer == null)
        {
            Debug.LogError("BallSpawner.ballContainer is not assigned.");
            hasError = true;
        }

        if (!hasError)
        {
            Debug.Log("BallSpawner bindings are valid.");
        }
    }

    private static void RemoveRootLevelGeneratedBallObjects()
    {
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.parent != null) continue;
            if (transform.name != "PingPongBall" && transform.name != "PingPongBall_Adapted") continue;

            Object.DestroyImmediate(transform.gameObject);
        }
    }

    private static GameObject InstantiateOrReuse(string name, GameObject prefab, Transform parent, Vector3 position, Vector3 scale)
    {
        var existing = GameObject.Find(name);
        var go = existing != null ? existing : PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (go == null)
        {
            go = new GameObject(name);
        }

        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go);
        return go;
    }

    private static void SetupPaddle(GameObject paddle)
    {
        var rb = EnsureComponent<Rigidbody>(paddle);
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var paddleCollider = EnsureComponent<BoxCollider>(paddle);
        if (paddleCollider != null)
        {
            paddleCollider.center = PaddleColliderCenter;
            paddleCollider.size = PaddleColliderSize;
            paddleCollider.isTrigger = false;
        }
        ConfigureSurface(paddle, PingPongSurfaceType.PaddleBody);

        var hitZone = GetOrCreateChild("PaddleHitZone", paddle.transform);
        hitZone.transform.localPosition = Vector3.zero;
        hitZone.transform.localRotation = Quaternion.identity;
        hitZone.transform.localScale = Vector3.one;
        var hitZoneCollider = EnsureComponent<BoxCollider>(hitZone);
        if (hitZoneCollider != null)
        {
            hitZoneCollider.center = PaddleHitZoneCenter;
            hitZoneCollider.size = PaddleHitZoneSize;
            hitZoneCollider.isTrigger = true;
        }
        ConfigureSurface(hitZone, PingPongSurfaceType.PaddleHitZone);

        EnsureComponent<PaddleFollower>(paddle);
        ConfigurePaddleTracker(EnsureComponent<PaddleVelocityTracker>(paddle));
    }

    private static void ConfigurePaddleTracker(PaddleVelocityTracker tracker)
    {
        if (tracker == null) return;

        tracker.autoAlignColliders = true;
        tracker.bodyColliderCenter = PaddleColliderCenter;
        tracker.bodyColliderSize = PaddleColliderSize;
        tracker.hitZoneColliderCenter = PaddleHitZoneCenter;
        tracker.hitZoneColliderSize = PaddleHitZoneSize;
    }

    private static void ConfigureSurface(GameObject target, PingPongSurfaceType surfaceType)
    {
        var surface = EnsureComponent<PingPongSurface>(target);
        if (surface == null) return;

        surface.useTypeDefaults = true;
        surface.Configure(surfaceType);
        EditorUtility.SetDirty(target);
    }

    private static GameObject SetupLeftHandGrabVisual(Transform parent)
    {
        var hand = GetOrCreate("Left_GrabHand", parent);
        hand.transform.position = new Vector3(-0.35f, 1.1f, 0.5f);
        hand.transform.localScale = Vector3.one;

        RemoveComponentIfExists<Rigidbody>(hand);
        RemoveComponentIfExists<PaddleFollower>(hand);
        RemoveComponentIfExists<PaddleVelocityTracker>(hand);

        var collider = hand.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var oldVisual = hand.transform.Find("HandVisual");
        if (oldVisual != null)
        {
            Object.DestroyImmediate(oldVisual.gameObject);
        }

        var handMaterial = CreateOrLoadMaterial("LeftHandSkin", new Color(0.95f, 0.72f, 0.55f));
        ConfigureVisualPrimitive(hand.transform, "Palm", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(0.09f, 0.06f, 0.12f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "Thumb", PrimitiveType.Capsule, new Vector3(0.065f, -0.01f, 0.015f), new Vector3(35f, 0f, -55f), new Vector3(0.022f, 0.05f, 0.022f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "IndexFinger", PrimitiveType.Capsule, new Vector3(0.045f, 0.005f, 0.09f), new Vector3(90f, 0f, 0f), new Vector3(0.018f, 0.065f, 0.018f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "MiddleFinger", PrimitiveType.Capsule, new Vector3(0.015f, 0.008f, 0.1f), new Vector3(90f, 0f, 0f), new Vector3(0.019f, 0.075f, 0.019f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "RingFinger", PrimitiveType.Capsule, new Vector3(-0.017f, 0.005f, 0.09f), new Vector3(90f, 0f, 0f), new Vector3(0.018f, 0.065f, 0.018f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "LittleFinger", PrimitiveType.Capsule, new Vector3(-0.047f, 0f, 0.078f), new Vector3(90f, 0f, 0f), new Vector3(0.016f, 0.052f, 0.016f), handMaterial);

        var follower = EnsureComponent<ControllerTransformFollower>(hand);
        if (follower != null)
        {
            follower.positionOffset = Vector3.zero;
            follower.rotationOffsetEuler = Vector3.zero;
        }

        var poseAnimator = EnsureComponent<GrabHandPoseAnimator>(hand);
        if (poseAnimator != null)
        {
            poseAnimator.controllerNode = XRNode.LeftHand;
            poseAnimator.closedPoseSpeed = 12f;
            poseAnimator.readControllerGrip = true;
            poseAnimator.mirrorX = true;
            poseAnimator.RebuildPoseCache();
        }

        EditorUtility.SetDirty(hand);
        return hand;
    }

    private static void ConfigureVisualPrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
    {
        var visual = GetOrCreateChild(name, parent);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.Euler(localRotation);
        visual.transform.localScale = localScale;

        var meshFilter = EnsureComponent<MeshFilter>(visual);
        var meshRenderer = EnsureComponent<MeshRenderer>(visual);
        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        if (meshFilter != null)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            meshFilter.sharedMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
        }

        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial = material;
        }
    }

    private static void SetupTablePhysics(GameObject table)
    {
        if (table == null) return;

        RemoveChildIfExists(table.transform, "Visual_PingPongTable");
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        BuildStandardTableVisual(table.transform, tableMaterial, netMaterial, darkMaterial);

        var rb = EnsureComponent<Rigidbody>(table);
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        var passiveLock = EnsureComponent<TablePassiveMotionLock>(table);
        if (passiveLock != null)
        {
            passiveLock.normalizeTableHeightOnEnable = true;
            passiveLock.standardTableTopHeight = PingPongGeometry.TableTopHeight;
            passiveLock.AcceptCurrentTransform();
        }

        var tableCollider = EnsureComponent<BoxCollider>(table);
        if (tableCollider != null)
        {
            tableCollider.center = Vector3.zero;
            tableCollider.size = LocalSizeForWorldSize(table.transform, TableColliderWorldSize);
            tableCollider.isTrigger = false;
        }
        ConfigureSurface(table, PingPongSurfaceType.Table);

        var netColliderObject = GetOrCreateChild("NetCollider", table.transform);
        netColliderObject.transform.localPosition = PingPongGeometry.NetLocalCenter;
        netColliderObject.transform.localRotation = Quaternion.identity;
        netColliderObject.transform.localScale = Vector3.one;
        var netCollider = EnsureComponent<BoxCollider>(netColliderObject);
        if (netCollider != null)
        {
            netCollider.center = Vector3.zero;
            netCollider.size = LocalSizeForWorldSize(netColliderObject.transform, NetColliderWorldSize);
            netCollider.isTrigger = true;
        }
        ConfigureSurface(netColliderObject, PingPongSurfaceType.Net);

        foreach (var collider in table.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider == tableCollider) continue;

            var lowerName = collider.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("net"))
            {
                collider.isTrigger = true;
                collider.size = LocalSizeForWorldSize(collider.transform, NetColliderWorldSize);
                ConfigureSurface(collider.gameObject, PingPongSurfaceType.Net);
            }
        }

        EditorUtility.SetDirty(table);
    }

    private static GameObject SetupPlayerTableBlocker(Transform parent, Transform tableTransform = null)
    {
        var blocker = GetOrCreate("TablePlayerBlocker", parent);
        var tableCenter = tableTransform != null ? tableTransform.position : PingPongGeometry.TableCenter;
        blocker.transform.position = tableCenter + Vector3.up * 0.1f;
        blocker.transform.localRotation = Quaternion.identity;
        blocker.transform.localScale = Vector3.one;

        var collider = EnsureComponent<BoxCollider>(blocker);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = new Vector3(TableColliderWorldSize.x + 0.24f, 1.4f, TableColliderWorldSize.z + 0.24f);
            collider.isTrigger = true;
        }

        var renderer = blocker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        SetLayerRecursively(blocker, "TableSafetyZone");

        var boundary = EnsureComponent<PlayerTableBoundary>(blocker);
        if (boundary != null)
        {
            boundary.tableTransform = tableTransform;
            boundary.tableCenter = tableCenter;
            boundary.tableSize = PingPongGeometry.TableBlockerSize(0.24f);
            boundary.margin = 0.12f;
            boundary.moveRigWhenInside = false;
        }

        var surface = EnsureComponent<PingPongSurface>(blocker);
        if (surface != null)
        {
            surface.useTypeDefaults = true;
            surface.Configure(PingPongSurfaceType.Unknown);
        }

        EditorUtility.SetDirty(blocker);
        return blocker;
    }

    private static void SetupPlayerTableSafety(GameObject blocker, Transform tableTransform, BallSpawner spawner, TableDragHandle dragHandle = null, PingPongPlayerBodyProxy playerBodyProxy = null)
    {
        if (blocker == null) return;

        var safety = EnsureComponent<PingPongPlayerTableSafety>(blocker);
        if (safety == null) return;

        safety.tableTransform = tableTransform;
        safety.tableDragHandle = dragHandle != null ? dragHandle : Object.FindObjectOfType<TableDragHandle>(true);
        safety.hmdTransform = Camera.main != null ? Camera.main.transform : null;
        safety.playerBodyProxy = playerBodyProxy != null ? playerBodyProxy : Object.FindObjectOfType<PingPongPlayerBodyProxy>(true);
        safety.ballSpawners = spawner != null ? new[] { spawner } : Object.FindObjectsOfType<BallSpawner>(true);
        safety.tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
        safety.safetyMargin = 0.12f;
        safety.hardMargin = 0.08f;
        safety.repulsionStrength = 0.6f;
        safety.maxRepulsionSpeed = 0.4f;
        safety.warningOnlyDistance = 0.45f;
        safety.hardPauseDistance = 0.08f;
        safety.blockedMarginMeters = 0.08f;
        safety.warningMarginMeters = 0.45f;
        safety.resumeStableSeconds = 0.5f;
        safety.tableCenterHeightAboveFloor = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness * 0.5f;
        safety.controlServing = true;
        safety.allowAutomaticResumeServing = false;
        safety.clearBallsOnBlock = true;
        safety.moveRigWhenInside = false;
        safety.moveTableWhenInside = false;
        safety.createRuntimePrompt = true;
        safety.createRuntimeBoundary = true;
        safety.useDefaultSafetyMarginsWhenUnset = true;
        safety.defaultWarningMarginMeters = 0.45f;
        safety.defaultBlockedMarginMeters = 0.08f;
        safety.defaultRepulsionMarginMeters = 0.12f;
        safety.promptHeightMeters = 1.35f;
        safety.promptOuterOffsetMeters = 0.45f;
        safety.hapticAmplitude = 0.12f;
        safety.hapticDurationSeconds = 0.08f;
        safety.hapticIntervalSeconds = 0.75f;
        EditorUtility.SetDirty(blocker);
    }

    private static Vector3 LocalSizeForWorldSize(Transform transform, Vector3 worldSize)
    {
        if (transform == null) return worldSize;

        var scale = transform.lossyScale;
        return new Vector3(
            worldSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.001f),
            worldSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.001f),
            worldSize.z / Mathf.Max(Mathf.Abs(scale.z), 0.001f));
    }

    private static void SetupNet(GameObject net)
    {
        if (net == null) return;

        net.transform.position = PingPongGeometry.TableCenter + PingPongGeometry.NetLocalCenter;
        net.transform.localScale = NetColliderWorldSize;

        var collider = EnsureComponent<BoxCollider>(net);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = LocalSizeForWorldSize(net.transform, NetColliderWorldSize);
            collider.isTrigger = true;
        }
        ConfigureSurface(net, PingPongSurfaceType.Net);

        EditorUtility.SetDirty(net);
    }

    private static GameObject SetupOptionalNet(GameObject tablePrefab, Transform parent)
    {
        var existing = GameObject.Find("Net");
        var tableHasNet = IsAdaptedPrefab(tablePrefab);

        if (tableHasNet)
        {
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            return null;
        }

        var netPrefab = LoadAdaptedPrefab("PingPongNet") ??
                        LoadOrCreatePrefabAsset("PingPongNet", PrimitiveType.Cube, NetColliderWorldSize, CreateOrLoadMaterial("NetWhite", new Color(0.88f, 0.88f, 0.88f)));
        var net = InstantiateOrReuse("Net", netPrefab, parent, PingPongGeometry.TableCenter + PingPongGeometry.NetLocalCenter, GetInstanceScale(netPrefab, NetColliderWorldSize));
        SetupNet(net);
        return net;
    }

    private static void BuildUi(
        Transform parent,
        ScoreManager score,
        BallSpawner spawner,
        out PingPongDifficultyController difficultyController,
        out PingPongUnifiedControlPanel unifiedControlPanel)
    {
        difficultyController = null;
        unifiedControlPanel = null;

        var canvasGo = GetOrCreate("WorldSpaceCanvas", parent);
        var canvas = EnsureComponent<Canvas>(canvasGo);
        if (canvas == null) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        canvasGo.transform.position = new Vector3(-0.92f, 1.48f, 1.18f);
        canvasGo.transform.rotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.002f;

        var scaler = EnsureComponent<CanvasScaler>(canvasGo);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        EnsureWorldCanvasRaycasters(canvasGo);
        ConfigureWorldCanvasInteraction(canvasGo);

        score.hudPanelSize = ElderCareUiTheme.PingPongHudSize;
        score.hudPanelColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.96f);

        score.accuracyText = null;
        score.lastSpeedText = null;
        score.hitText = null;
        score.servedText = null;
        score.missedText = null;
        score.lastSpinText = null;

        RemoveLegacyPingPongControlUi(canvasGo.transform);
        difficultyController = BuildDifficultyControllerState(canvasGo.transform, spawner);
        unifiedControlPanel = PingPongUnifiedControlPanel.EnsureRuntimePanel(
            canvasGo.transform,
            score,
            spawner,
            difficultyController,
            null,
            LoadPingPongTmpFont());
        EditorUtility.SetDirty(canvasGo);
        return;

#if false
        CreateScoreHudBackdrop(canvasGo.transform);
        CreateScoreMetricCard(canvasGo.transform, "AccuracyMetricCard", new Vector2(292f, 142f), new Vector2(-722f, 293f), ElderCareUiTheme.Cyan, 0.16f, 24f);
        CreateScoreMetricCard(canvasGo.transform, "SpeedMetricCard", new Vector2(292f, 142f), new Vector2(-418f, 293f), ElderCareUiTheme.Blue, 0.14f, 24f);
        CreateScoreMetricCard(canvasGo.transform, "HitMetricCard", new Vector2(186f, 82f), new Vector2(-780f, 145f), ElderCareUiTheme.Green, 0.09f, 18f);
        CreateScoreMetricCard(canvasGo.transform, "ServedMetricCard", new Vector2(186f, 82f), new Vector2(-570f, 145f), ElderCareUiTheme.Cyan, 0.08f, 18f);
        CreateScoreMetricCard(canvasGo.transform, "MissedMetricCard", new Vector2(186f, 82f), new Vector2(-360f, 145f), ElderCareUiTheme.Orange, 0.08f, 18f);
        CreateScoreMetricCard(canvasGo.transform, "SpinMetricCard", new Vector2(606f, 58f), new Vector2(-570f, 51f), ElderCareUiTheme.Violet, 0.08f, 18f);

        score.accuracyText = CreateScoreText(canvasGo.transform, "AccuracyText", "<size=28>命中率</size>\n<size=72><b>0.0</b></size><size=28>%</size>", new Vector2(-722f, 293f), new Vector2(292f, 142f), ElderCareUiTheme.HudPrimary + 18f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.Cyan);
        score.lastSpeedText = CreateScoreText(canvasGo.transform, "LastSpeedText", "<size=28>回球速度</size>\n<size=72><b>0.0</b></size> <size=28>m/s</size>", new Vector2(-418f, 293f), new Vector2(292f, 142f), ElderCareUiTheme.HudPrimary + 10f, FontStyles.Bold, TextAlignmentOptions.Center, Color.Lerp(ElderCareUiTheme.TextPrimary, ElderCareUiTheme.Blue, 0.24f));
        score.hitText = CreateScoreText(canvasGo.transform, "HitText", "<size=22>命中</size>\n<size=40><b>0</b></size>", new Vector2(-780f, 145f), new Vector2(186f, 82f), ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextPrimary);
        score.servedText = CreateScoreText(canvasGo.transform, "ServedText", "<size=22>发球</size>\n<size=40><b>0</b></size>", new Vector2(-570f, 145f), new Vector2(186f, 82f), ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextSecondary);
        score.missedText = CreateScoreText(canvasGo.transform, "MissedText", "<size=22>漏球</size>\n<size=40><b>0</b></size>", new Vector2(-360f, 145f), new Vector2(186f, 82f), ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextSecondary);
        score.lastSpinText = CreateScoreText(canvasGo.transform, "LastSpinText", "<size=22>旋转</size>  <size=32><b>0</b></size> <size=20>rad/s</size>", new Vector2(-570f, 51f), new Vector2(606f, 58f), ElderCareUiTheme.HudSecondary, FontStyles.Normal, TextAlignmentOptions.Center, ElderCareUiTheme.TextMuted);

        BuildDifficultyUi(canvasGo.transform, spawner);
#endif
    }

    private static void RemoveLegacyPingPongControlUi(Transform canvasTransform)
    {
        if (canvasTransform == null) return;

        RemoveChildIfExists(canvasTransform, "PingPongUnifiedControlPanel");
        RemoveChildIfExists(canvasTransform, "ScoreHudBackdrop");
        RemoveChildIfExists(canvasTransform, "AccuracyMetricCard");
        RemoveChildIfExists(canvasTransform, "SpeedMetricCard");
        RemoveChildIfExists(canvasTransform, "HitMetricCard");
        RemoveChildIfExists(canvasTransform, "ServedMetricCard");
        RemoveChildIfExists(canvasTransform, "MissedMetricCard");
        RemoveChildIfExists(canvasTransform, "SpinMetricCard");
        RemoveChildIfExists(canvasTransform, "AccuracyText");
        RemoveChildIfExists(canvasTransform, "LastSpeedText");
        RemoveChildIfExists(canvasTransform, "HitText");
        RemoveChildIfExists(canvasTransform, "ServedText");
        RemoveChildIfExists(canvasTransform, "MissedText");
        RemoveChildIfExists(canvasTransform, "LastSpinText");
        RemoveChildIfExists(canvasTransform, "DifficultyPanel");
        RemoveChildIfExists(canvasTransform, "BackHomeButton");
    }

    private static PingPongDifficultyController BuildDifficultyControllerState(Transform canvasTransform, BallSpawner spawner)
    {
        if (canvasTransform == null) return null;

        var root = GetOrCreate("PingPongDifficultyControllerState", canvasTransform);
        ConfigureRect(root, Vector2.zero, Vector2.zero);

        var controller = EnsureComponent<PingPongDifficultyController>(root);
        if (controller == null) return null;

        controller.ballSpawner = spawner;
        controller.difficultyText = null;
        controller.speedText = null;
        controller.hintText = null;
        controller.decreaseButton = null;
        controller.increaseButton = null;
        controller.resetButton = null;
        controller.startingDifficulty = PingPongDifficulty.Normal;
        controller.controlServeInterval = true;
        controller.enhancePanelReadability = false;
        controller.displayStandalonePanel = false;
        controller.showScreenButtons = false;
        controller.enableControllerSpeedButtons = false;
        controller.ApplyLoadedDifficulty();

        EditorUtility.SetDirty(root);
        return controller;
    }

    private static void ConfigureWorldCanvasInteraction(GameObject canvasGo)
    {
        if (canvasGo == null) return;

        var comfortPlacer = EnsureComponent<ComfortWorldSpaceUIPlacer>(canvasGo);
        if (comfortPlacer == null) return;

        comfortPlacer.headTransform = Camera.main != null ? Camera.main.transform : null;
        comfortPlacer.uiRoot = canvasGo.transform;
        comfortPlacer.distanceMeters = ElderCareUiTheme.HudDistanceMeters;
        comfortPlacer.hmdHeightOffsetMeters = 0.12f;
        comfortPlacer.placeOnStart = false;
        comfortPlacer.placeOnEnable = false;
        comfortPlacer.recenterDuringStartup = false;
        comfortPlacer.clampWorldHeight = true;
        comfortPlacer.minWorldHeight = 1.25f;
        comfortPlacer.maxWorldHeight = 1.75f;
        comfortPlacer.preferredWorldHeight = 1.5f;
        comfortPlacer.usePreferredHeightInsteadOfHeadHeight = true;
        comfortPlacer.enableRayDrag = true;
        comfortPlacer.enableThumbstickNavigation = true;
        comfortPlacer.invertThumbstickHorizontal = false;
        comfortPlacer.comfortFollowEnabled = false;
        comfortPlacer.EnsureWorldSpaceInteractionHelpers();
        EditorUtility.SetDirty(canvasGo);
    }

    private static PingPongDifficultyController BuildDifficultyUi(Transform canvasTransform, BallSpawner spawner)
    {
        var root = GetOrCreate("DifficultyPanel", canvasTransform);
        var rootRect = ConfigureRect(root, new Vector2(560f, 260f), new Vector2(520f, 174f));
        RemoveChildIfExists(root.transform, "TopScanLine");

        var controller = EnsureComponent<PingPongDifficultyController>(root);
        if (controller == null) return null;

        var background = CreateRoundedPanel(rootRect, "Background", new Vector2(560f, 260f), Vector2.zero, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.22f), 1f), 26f);
        background.raycastTarget = false;

        var glow = CreateRoundedPanel(rootRect, "Glow", new Vector2(584f, 286f), Vector2.zero, WithAlpha(ElderCareUiTheme.Cyan, 0.05f), 32f);
        glow.raycastTarget = false;
        glow.transform.SetAsFirstSibling();

        CreateRoundedPanel(rootRect, "TopTrace", new Vector2(420f, 3f), new Vector2(0f, 104f), WithAlpha(ElderCareUiTheme.Cyan, 0.22f), 2f);
        CreateRoundedPanel(rootRect, "BottomTrace", new Vector2(320f, 2f), new Vector2(0f, -106f), WithAlpha(ElderCareUiTheme.Blue, 0.16f), 2f);

        var title = CreateDifficultyText(rootRect, "Title", "发球速度", new Vector2(0f, 72f), new Vector2(480f, 44f), ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
        var difficulty = CreateDifficultyText(rootRect, "DifficultyText", "当前难度：标准", new Vector2(0f, 24f), new Vector2(480f, 42f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Cyan, TextAlignmentOptions.Center);
        var speed = CreateDifficultyText(rootRect, "SpeedText", "发球速度 3.0 m/s", new Vector2(0f, -20f), new Vector2(480f, 44f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
        var hint = CreateDifficultyText(rootRect, "HintText", "使用面板按钮调整难度", new Vector2(0f, -82f), new Vector2(500f, 40f), ElderCareUiTheme.BodySmall, FontStyles.Normal, ElderCareUiTheme.TextSecondary, TextAlignmentOptions.Center);

        var decrease = CreateDifficultyButton(rootRect, "DecreaseButton", "-", new Vector2(-166f, -66f), new Vector2(104f, 68f));
        var reset = CreateDifficultyButton(rootRect, "ResetButton", "标准", new Vector2(0f, -66f), new Vector2(150f, 68f));
        var increase = CreateDifficultyButton(rootRect, "IncreaseButton", "+", new Vector2(166f, -66f), new Vector2(104f, 68f));
        if (decrease != null) decrease.gameObject.SetActive(false);
        if (reset != null) reset.gameObject.SetActive(false);
        if (increase != null) increase.gameObject.SetActive(false);

        ConfigureDifficultyText(title, "发球速度", new Vector2(0f, 72f), new Vector2(480f, 44f), ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        ConfigureDifficultyText(difficulty, "当前难度：标准", new Vector2(0f, 24f), new Vector2(480f, 42f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Cyan);
        ConfigureDifficultyText(speed, "发球速度 3.0 m/s", new Vector2(0f, -20f), new Vector2(480f, 44f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        ConfigureDifficultyText(hint, "使用面板按钮调整难度", new Vector2(0f, -82f), new Vector2(500f, 40f), ElderCareUiTheme.BodySmall, FontStyles.Normal, ElderCareUiTheme.TextSecondary);

        controller.ballSpawner = spawner;
        controller.difficultyText = difficulty;
        controller.speedText = speed;
        controller.hintText = hint;
        controller.decreaseButton = null;
        controller.increaseButton = null;
        controller.resetButton = null;
        controller.startingDifficulty = PingPongDifficulty.Normal;
        controller.controlServeInterval = true;
        controller.showScreenButtons = false;
        controller.enableControllerSpeedButtons = false;

        var motion = EnsureComponent<TechModuleCardMotion>(root);
        if (motion != null)
        {
            motion.cardTransform = rootRect;
            motion.canvasGroup = EnsureComponent<CanvasGroup>(root);
            motion.cardGraphic = background;
            motion.glowGraphic = glow;
            motion.normalColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.16f), 0.88f);
            motion.hoverColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.18f), 0.92f);
            motion.pressedColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.94f);
            motion.glowColor = WithAlpha(ElderCareUiTheme.Cyan, 0.14f);
            motion.hoverScale = 1.012f;
            motion.pressedScale = 0.99f;
            motion.entranceDelay = 0.18f;
        }

        if (title != null)
        {
            title.raycastTarget = false;
        }

        WorldSpaceUiRayDragHandle.EnsureOnSurface(background, canvasTransform, canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>());
        EditorUtility.SetDirty(root);
        return controller;
    }

    private static PingPongDifficultyController BuildDifficultyUiLegacy(Transform canvasTransform, BallSpawner spawner)
    {
        var root = GetOrCreate("DifficultyPanel", canvasTransform);
        var rootRect = ConfigureRect(root, new Vector2(560f, 280f), new Vector2(630f, 148f));

        var controller = EnsureComponent<PingPongDifficultyController>(root);
        if (controller == null) return null;

        var background = CreateRoundedPanel(rootRect, "Background", new Vector2(560f, 280f), Vector2.zero, new Color(0.015f, 0.04f, 0.07f, 0.94f), 26f);
        background.raycastTarget = false;

        var glow = CreateRoundedPanel(rootRect, "Glow", new Vector2(590f, 310f), Vector2.zero, new Color(0.2f, 0.82f, 1f, 0.1f), 32f);
        glow.raycastTarget = false;
        glow.transform.SetAsFirstSibling();

        CreateRoundedPanel(rootRect, "TopScanLine", new Vector2(486f, 4f), new Vector2(0f, 112f), new Color(0.42f, 0.92f, 1f, 0.72f), 2f);

        var title = CreateDifficultyText(rootRect, "Title", "发球速度", new Vector2(0f, 116f), new Vector2(480f, 54f), 34f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.98f), TextAlignmentOptions.Center);
        var difficulty = CreateDifficultyText(rootRect, "DifficultyText", "难度：标准", new Vector2(0f, 66f), new Vector2(480f, 48f), 28f, FontStyles.Bold, new Color(0.62f, 0.96f, 1f, 0.98f), TextAlignmentOptions.Center);
        var speed = CreateDifficultyText(rootRect, "SpeedText", "速度 3.0 m/s", new Vector2(0f, 20f), new Vector2(480f, 46f), 26f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.94f), TextAlignmentOptions.Center);
        var hint = CreateDifficultyText(rootRect, "HintText", "使用 +/- 调节下一次发球速度", new Vector2(0f, -126f), new Vector2(500f, 44f), 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.78f), TextAlignmentOptions.Center);

        var decrease = CreateDifficultyButton(rootRect, "DecreaseButton", "-", new Vector2(-166f, -66f), new Vector2(104f, 68f));
        var reset = CreateDifficultyButton(rootRect, "ResetButton", "标准", new Vector2(0f, -66f), new Vector2(150f, 68f));
        var increase = CreateDifficultyButton(rootRect, "IncreaseButton", "+", new Vector2(166f, -66f), new Vector2(104f, 68f));
        if (decrease != null) decrease.gameObject.SetActive(false);
        if (reset != null) reset.gameObject.SetActive(false);
        if (increase != null) increase.gameObject.SetActive(false);
        ConfigureDifficultyText(title, "发球速度", new Vector2(0f, 86f), new Vector2(480f, 54f), 34f);
        ConfigureDifficultyText(difficulty, "难度：标准", new Vector2(0f, 36f), new Vector2(480f, 48f), 28f);
        ConfigureDifficultyText(speed, "速度 3.0 m/s", new Vector2(0f, -8f), new Vector2(480f, 46f), 26f);
        ConfigureDifficultyText(hint, "使用面板按钮调整难度", new Vector2(0f, -86f), new Vector2(500f, 44f), 22f);

        controller.ballSpawner = spawner;
        controller.difficultyText = difficulty;
        controller.speedText = speed;
        controller.hintText = hint;
        controller.decreaseButton = null;
        controller.increaseButton = null;
        controller.resetButton = null;
        controller.startingDifficulty = PingPongDifficulty.Normal;
        controller.controlServeInterval = true;
        controller.showScreenButtons = false;
        controller.enableControllerSpeedButtons = false;

        var motion = EnsureComponent<TechModuleCardMotion>(root);
        if (motion != null)
        {
            motion.cardTransform = rootRect;
            motion.canvasGroup = EnsureComponent<CanvasGroup>(root);
            motion.cardGraphic = background;
            motion.glowGraphic = glow;
            motion.normalColor = new Color(0.015f, 0.04f, 0.07f, 0.94f);
            motion.hoverColor = new Color(0.03f, 0.08f, 0.13f, 0.98f);
            motion.pressedColor = new Color(0.01f, 0.035f, 0.06f, 0.98f);
            motion.glowColor = new Color(0.25f, 0.9f, 1f, 0.2f);
            motion.hoverScale = 1.015f;
            motion.pressedScale = 0.99f;
            motion.entranceDelay = 0.22f;
        }

        if (title != null)
        {
            title.raycastTarget = false;
        }

        WorldSpaceUiRayDragHandle.EnsureOnSurface(background, canvasTransform, canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>());
        EditorUtility.SetDirty(root);
        return controller;
    }

    private static ElderCareHomeMenu BuildElderCareHomeMenu(
        Transform uiParent,
        Transform managerParent,
        GameObject pingPongRoot,
        GameObject scoreCanvas,
        BallSpawner spawner,
        ScoreManager score,
        PingPongUnifiedControlPanel controlPanel)
    {
        RemoveChildIfExists(uiParent, "ElderCareHomeCanvas");
        RemoveChildIfExists(managerParent, "ElderCareHomeMenu");

        var controllerGo = GetOrCreate("ElderCareHomeMenu", managerParent);
        var menu = EnsureComponent<ElderCareHomeMenu>(controllerGo);
        if (menu == null) return null;

        var canvasGo = GetOrCreateChild("ElderCareHomeCanvas", uiParent);
        var canvas = EnsureComponent<Canvas>(canvasGo);
        if (canvas == null) return menu;

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 20;
        canvasGo.transform.position = new Vector3(0f, 1.5f, 1.18f);
        canvasGo.transform.rotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.0015f;

        var comfortPlacer = EnsureComponent<ComfortWorldSpaceUIPlacer>(canvasGo);
        if (comfortPlacer != null)
        {
            comfortPlacer.headTransform = Camera.main != null ? Camera.main.transform : null;
            comfortPlacer.uiRoot = canvasGo.transform;
            comfortPlacer.distanceMeters = 2f;
            comfortPlacer.hmdHeightOffsetMeters = -0.1f;
            comfortPlacer.placeOnStart = false;
            comfortPlacer.placeOnEnable = false;
            comfortPlacer.recenterDuringStartup = true;
            comfortPlacer.startupRecenterSeconds = 0.35f;
            comfortPlacer.startupRecenterFrames = 4;
            comfortPlacer.clampWorldHeight = true;
            comfortPlacer.minWorldHeight = 1.25f;
            comfortPlacer.maxWorldHeight = 1.75f;
            comfortPlacer.preferredWorldHeight = 1.5f;
            comfortPlacer.usePreferredHeightInsteadOfHeadHeight = true;
            comfortPlacer.enableRayDrag = true;
            comfortPlacer.enableThumbstickNavigation = true;
            comfortPlacer.invertThumbstickHorizontal = false;
            comfortPlacer.comfortFollowEnabled = false;
            comfortPlacer.followYawThresholdDegrees = 35f;
            comfortPlacer.followPositionThresholdMeters = 0.8f;
            comfortPlacer.followSmoothTime = 0.35f;
            comfortPlacer.followRotationSlerpSpeed = 4f;
            comfortPlacer.maxFollowSpeedMetersPerSecond = 1.25f;
        }

        var canvasRect = ConfigureRect(canvasGo, new Vector2(1920f, 1080f), Vector2.zero);
        var scaler = EnsureComponent<CanvasScaler>(canvasGo);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        EnsureWorldCanvasRaycasters(canvasGo);
        EnsureUiEventSystem();

        CreateRoundedPanel(canvasRect, "Background", new Vector2(1920f, 1080f), Vector2.zero, new Color(0.05f, 0.08f, 0.14f, 0.96f), 0f);
        CreateRoundedPanel(canvasRect, "CenterGlow", new Vector2(1340f, 760f), new Vector2(0f, 20f), new Color(0.22f, 0.31f, 0.45f, 0.16f), 260f);
        CreateHomeStars(canvasRect);

        CreateHomeText(canvasRect, "Title", "VR康养服务", new Vector2(0f, 395f), new Vector2(1200f, 120f), 92, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateRoundedPanel(canvasRect, "TitleDivider", new Vector2(260f, 4f), new Vector2(0f, 320f), new Color(1f, 1f, 1f, 0.55f), 2f);

        var cards = new List<ElderCareModuleCard>
        {
            CreateHomeModuleCard(canvasRect, menu, "Module_HealthGame", "pingpong", "健康游戏", "乒乓球、投篮等趣味运动", ElderCareIconType.Gamepad, new Vector2(-330f, 95f), new Color(0.18f, 0.46f, 0.91f), new Color(0.28f, 0.57f, 1f), new Color(0.23f, 0.51f, 0.96f, 0.55f)),
            CreateHomeModuleCard(canvasRect, menu, "Module_Rehab", "rehab", "康复运动", "太极拳、八段锦养生功法", ElderCareIconType.Heart, new Vector2(330f, 95f), new Color(0.15f, 0.66f, 0.34f), new Color(0.25f, 0.79f, 0.43f), new Color(0.13f, 0.78f, 0.36f, 0.55f)),
            CreateHomeModuleCard(canvasRect, menu, "Module_Travel", "travel", "VR旅游", "长城、故宫名胜古迹", ElderCareIconType.MapPin, new Vector2(-330f, -215f), new Color(0.55f, 0.29f, 0.89f), new Color(0.66f, 0.39f, 1f), new Color(0.62f, 0.33f, 0.97f, 0.55f)),
            CreateHomeModuleCard(canvasRect, menu, "Module_Video", "video", "场景视频", "VR看房、生活场景体验", ElderCareIconType.Video, new Vector2(330f, -215f), new Color(0.91f, 0.42f, 0.12f), new Color(1f, 0.55f, 0.22f), new Color(0.98f, 0.45f, 0.12f, 0.55f))
        };
        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                cards[i].entranceDelay = 0.05f + i * 0.08f;
            }
        }

        var footer = CreateHomeText(canvasRect, "FooterHint", "使用手柄或手势选择功能", new Vector2(0f, -492f), new Vector2(900f, 70f), 30, FontStyle.Normal, new Color(1f, 1f, 1f, 0.62f), TextAnchor.MiddleCenter);
        if (scoreCanvas != null)
        {
            RemoveChildIfExists(scoreCanvas.transform, "BackHomeButton");
        }

        menu.homeRoot = canvasGo;
        menu.pingPongGameplayRoots = new[] { pingPongRoot, scoreCanvas };
        menu.ballSpawner = spawner;
        menu.scoreManager = score;
        menu.initialViewAligner = Object.FindObjectOfType<VrInitialViewAligner>(true);
        menu.uiPlacer = comfortPlacer;
        menu.statusText = footer;
        menu.moduleCards = cards.ToArray();
        menu.uiFont = CreateReadableUiFont(64);
        menu.showHomeOnStart = true;
        menu.clearBallsWhenLeavingPingPong = true;
        menu.placeHomeUiOnShow = true;
        if (controlPanel != null)
        {
            controlPanel.Bind(score, spawner, controlPanel.difficultyController, menu);
        }

        EditorUtility.SetDirty(canvasGo);
        EditorUtility.SetDirty(controllerGo);
        return menu;
    }

    private static ElderCareModuleCard CreateHomeModuleCard(
        RectTransform parent,
        ElderCareHomeMenu menu,
        string name,
        string moduleId,
        string title,
        string description,
        ElderCareIconType iconType,
        Vector2 position,
        Color normalColor,
        Color hoverColor,
        Color glowColor)
    {
        var root = GetOrCreateChild(name, parent);
        var rootRect = ConfigureRect(root, new Vector2(570f, 260f), position);

        var glow = CreateRoundedPanel(rootRect, "Glow", new Vector2(620f, 310f), Vector2.zero, new Color(glowColor.r, glowColor.g, glowColor.b, 0f), 48f);
        glow.raycastTarget = false;

        var panel = CreateRoundedPanel(rootRect, "Panel", new Vector2(570f, 260f), Vector2.zero, normalColor, 36f);
        panel.raycastTarget = true;
        var outline = EnsureComponent<Outline>(panel.gameObject);
        if (outline != null)
        {
            outline.effectColor = new Color(1f, 1f, 1f, 0.24f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        var icon = GetOrCreateChild("Icon", panel.transform);
        var iconGraphic = EnsureComponent<ElderCareLineIcon>(icon);
        iconGraphic.iconType = iconType;
        iconGraphic.strokeWidth = 10f;
        iconGraphic.color = Color.white;
        iconGraphic.raycastTarget = false;
        ConfigureRect(icon, new Vector2(122f, 122f), new Vector2(0f, 58f));

        CreateHomeText(panel.transform as RectTransform, "Title", title, new Vector2(0f, -35f), new Vector2(510f, 72f), 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateHomeText(panel.transform as RectTransform, "Description", description, new Vector2(0f, -92f), new Vector2(510f, 48f), 26, FontStyle.Normal, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);

        var indicator = CreateRoundedPanel(panel.transform as RectTransform, "SelectIndicator", new Vector2(56f, 56f), new Vector2(0f, -126f), new Color(1f, 1f, 1f, 0.18f), 28f);
        indicator.raycastTarget = false;
        var checkGo = GetOrCreateChild("Check", indicator.transform);
        var check = EnsureComponent<ElderCareLineIcon>(checkGo);
        check.iconType = ElderCareIconType.Check;
        check.strokeWidth = 7f;
        check.color = Color.white;
        check.raycastTarget = false;
        ConfigureRect(checkGo, new Vector2(36f, 36f), Vector2.zero);

        var button = EnsureComponent<Button>(root);
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = panel;
        }

        var card = EnsureComponent<ElderCareModuleCard>(root);
        if (card != null)
        {
            card.menu = menu;
            card.moduleId = moduleId;
            card.moduleTitle = title;
            card.cardTransform = rootRect;
            card.canvasGroup = EnsureComponent<CanvasGroup>(root);
            card.cardGraphic = panel;
            card.glowGraphic = glow;
            card.normalColor = normalColor;
            card.hoverColor = hoverColor;
            card.glowColor = glowColor;
            card.hoverScale = 1.05f;
            card.pressedScale = 0.96f;
            card.selectedScale = 1.03f;
            card.hoverLiftY = 9f;
            card.selectedLiftY = 5f;
            card.playEntrance = true;
            card.ambientMotion = true;
            card.ambientFloatY = 3.5f;
            card.ambientPulseSpeed = 1.25f;
        }

        EditorUtility.SetDirty(root);
        return card;
    }

    private static void CreateGameplayHomeButton(Transform canvasTransform, ElderCareHomeMenu menu)
    {
        if (canvasTransform == null || menu == null) return;

        var root = GetOrCreateChild("BackHomeButton", canvasTransform);
        var rootRect = ConfigureRect(root, new Vector2(300f, 78f), new Vector2(-760f, 500f));

        var panel = CreateRoundedPanel(rootRect, "Panel", new Vector2(300f, 78f), Vector2.zero, new Color(0.06f, 0.12f, 0.2f, 0.88f), 22f);
        panel.raycastTarget = true;

        var iconGo = GetOrCreateChild("Icon", panel.transform);
        var icon = EnsureComponent<ElderCareLineIcon>(iconGo);
        if (icon != null)
        {
            icon.iconType = ElderCareIconType.ArrowLeft;
            icon.strokeWidth = 7f;
            icon.color = Color.white;
            icon.raycastTarget = false;
        }
        ConfigureRect(iconGo, new Vector2(42f, 42f), new Vector2(-104f, 0f));

        CreateHomeText(panel.transform as RectTransform, "Label", "返回首页", new Vector2(36f, 0f), new Vector2(190f, 58f), 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var button = EnsureComponent<Button>(root);
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = panel;
        }

        var homeButton = EnsureComponent<ElderCareHomeButton>(root);
        if (homeButton != null)
        {
            homeButton.menu = menu;
            homeButton.applySafeGameplayLayout = true;
            homeButton.safeAnchoredPosition = new Vector2(-760f, 500f);
            homeButton.safeSize = new Vector2(300f, 78f);
        }

        EditorUtility.SetDirty(root);
    }

    private static Text CreateHomeText(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        var go = GetOrCreateChild(name, parent);
        var text = EnsureComponent<Text>(go);
        ConfigureRect(go, size, position);

        if (text != null)
        {
            text.text = value;
            text.font = CreateReadableUiFont(fontSize);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        return text;
    }

    private static ElderCareRoundedPanel CreateRoundedPanel(RectTransform parent, string name, Vector2 size, Vector2 position, Color color, float radius)
    {
        var go = GetOrCreateChild(name, parent);
        var panel = EnsureSingleRoundedPanel(go);
        ConfigureRect(go, size, position);

        if (panel != null)
        {
            panel.color = color;
            panel.cornerRadius = radius;
            panel.raycastTarget = false;
        }

        return panel;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static ElderCareRoundedPanel EnsureSingleRoundedPanel(GameObject go)
    {
        if (go == null) return null;

        var images = go.GetComponents<Image>();
        foreach (var image in images)
        {
            if (image != null)
            {
                Object.DestroyImmediate(image);
            }
        }

        var panels = go.GetComponents<ElderCareRoundedPanel>();
        for (var i = 1; i < panels.Length; i++)
        {
            Object.DestroyImmediate(panels[i]);
        }

        if (panels.Length > 0 && panels[0] != null)
        {
            return panels[0];
        }

        return EnsureComponent<ElderCareRoundedPanel>(go);
    }

    private static void CreateHomeStars(RectTransform parent)
    {
        CreateRoundedPanel(parent, "Star_A", new Vector2(8f, 8f), new Vector2(-520f, 245f), new Color(1f, 1f, 1f, 0.33f), 4f);
        CreateRoundedPanel(parent, "Star_B", new Vector2(7f, 7f), new Vector2(575f, 260f), new Color(1f, 1f, 1f, 0.28f), 4f);
        CreateRoundedPanel(parent, "Star_C", new Vector2(6f, 6f), new Vector2(-650f, -210f), new Color(1f, 1f, 1f, 0.24f), 4f);
        CreateRoundedPanel(parent, "Star_D", new Vector2(9f, 9f), new Vector2(610f, -245f), new Color(1f, 1f, 1f, 0.26f), 5f);
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

    private static Font CreateReadableUiFont(int size)
    {
        var bundledFont = AssetDatabase.LoadAssetAtPath<Font>(ElderCareUiFontPath);
        if (bundledFont != null) return bundledFont;

        var font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" },
            Mathf.Max(16, size));
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void EnsureWorldCanvasRaycasters(GameObject canvasGo)
    {
        EnsureComponent<GraphicRaycaster>(canvasGo);
        AddComponentIfTypeExists(canvasGo, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
    }

    private static void EnsureUiEventSystem()
    {
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }

        var xrUiModule = AddComponentIfTypeExists(eventSystem.gameObject, "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrUiModule != null)
        {
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                if (module == null || module == xrUiModule) continue;
                Object.DestroyImmediate(module);
            }
        }
        else if (eventSystem.GetComponent<BaseInputModule>() == null)
        {
            var inputSystemModule = AddComponentIfTypeExists(eventSystem.gameObject, "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule == null && eventSystem.GetComponent<BaseInputModule>() == null)
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

    private static TMP_Text CreateScoreText(
        Transform canvasTransform,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        Color color)
    {
        var go = GetOrCreate(name, canvasTransform);
        var text = EnsureComponent<TextMeshProUGUI>(go);
        if (text == null) return null;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.12f;
        text.enableWordWrapping = false;
        text.richText = true;
        var fontAsset = LoadPingPongTmpFont();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.raycastTarget = false;
        return text;
    }

    private static void CreateScoreHudBackdrop(Transform canvasTransform)
    {
        var go = GetOrCreate("ScoreHudBackdrop", canvasTransform);
        ConfigureRect(go, ElderCareUiTheme.PingPongHudSize, new Vector2(-570f, 215f));
        var panel = EnsureSingleRoundedPanel(go);
        if (panel != null)
        {
            panel.color = WithAlpha(ElderCareUiTheme.PanelStrong, 1f);
            panel.cornerRadius = 30f;
            panel.raycastTarget = true;
            WorldSpaceUiRayDragHandle.EnsureOnSurface(panel, canvasTransform, canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>());
        }

        var outline = EnsureComponent<Outline>(go);
        if (outline != null)
        {
            outline.effectColor = WithAlpha(ElderCareUiTheme.PanelStroke, 0.58f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        go.transform.SetAsFirstSibling();
    }

    private static void CreateScoreMetricCard(Transform canvasTransform, string name, Vector2 size, Vector2 position, Color accent, float alpha, float radius)
    {
        var root = GetOrCreate(name, canvasTransform);
        var rect = ConfigureRect(root, size, position);
        RemoveChildIfExists(root.transform, "TopTrace");
        RemoveChildIfExists(root.transform, name + "_TopTrace");
        var panel = EnsureSingleRoundedPanel(root);
        if (panel != null)
        {
            panel.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.28f), alpha + 0.56f);
            panel.cornerRadius = radius;
            panel.raycastTarget = false;
        }

        var outline = EnsureComponent<Outline>(root);
        if (outline != null)
        {
            outline.effectColor = WithAlpha(accent, alpha + 0.32f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        root.transform.SetSiblingIndex(1);
    }

    private static TMP_Text CreateDifficultyText(
        RectTransform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        var go = GetOrCreateChild(name, parent);
        var text = EnsureComponent<TextMeshProUGUI>(go);
        ConfigureRect(go, size, position);
        if (text == null) return null;

        var fontAsset = LoadPingPongTmpFont();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureDifficultyText(TMP_Text text, string value, Vector2 position, Vector2 size, float fontSize)
    {
        if (text == null) return;

        ConfigureRect(text.gameObject, size, position);
        text.text = value;
        text.fontSize = fontSize;
        text.raycastTarget = false;
    }

    private static void ConfigureDifficultyText(TMP_Text text, string value, Vector2 position, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        if (text == null) return;

        ConfigureRect(text.gameObject, size, position);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    private static Button CreateDifficultyButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = GetOrCreateChild(name, parent);
        var rect = ConfigureRect(go, size, position);
        var panel = EnsureSingleRoundedPanel(go);
        if (panel != null)
        {
            panel.color = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 0.9f);
            panel.cornerRadius = 20f;
            panel.raycastTarget = true;
        }

        var outline = EnsureComponent<Outline>(go);
        if (outline != null)
        {
            outline.effectColor = WithAlpha(ElderCareUiTheme.Cyan, 0.28f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        var button = EnsureComponent<Button>(go);
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = panel;
        }

        var text = CreateDifficultyText(rect, "Label", label, Vector2.zero, size, label.Length > 1 ? ElderCareUiTheme.BodySmall : ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
        if (text != null)
        {
            text.raycastTarget = false;
        }

        var motion = EnsureComponent<TechModuleCardMotion>(go);
        if (motion != null)
        {
            motion.cardTransform = rect;
            motion.canvasGroup = EnsureComponent<CanvasGroup>(go);
            motion.cardGraphic = panel;
            motion.normalColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 0.9f);
            motion.hoverColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.34f), 0.94f);
            motion.pressedColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.96f);
            motion.glowColor = WithAlpha(ElderCareUiTheme.Cyan, 0.12f);
            motion.hoverScale = ElderCareUiTheme.HoverScale;
            motion.pressedScale = ElderCareUiTheme.PressedScale;
            motion.playEntrance = false;
        }

        return button;
    }

    private static TMP_FontAsset LoadPingPongTmpFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RehabChineseFontAssetPath);
    }

    private static Transform BindController(PaddleFollower follower, bool rightHand)
    {
        if (follower == null) return null;

        var controller = FindControllerTransform(rightHand);
        if (controller != null)
        {
            follower.controllerTransform = controller;
            return controller;
        }

        Debug.Log($"{(rightHand ? "Right" : "Left")} hand controller not auto-bound. Please assign XR Origin controller to PaddleFollower.controllerTransform manually.");
        return null;
    }

    private static Transform BindController(ControllerTransformFollower follower, bool rightHand)
    {
        if (follower == null) return null;

        var controller = FindControllerTransform(rightHand);
        if (controller != null)
        {
            follower.controllerTransform = controller;
            return controller;
        }

        Debug.Log($"{(rightHand ? "Right" : "Left")} hand controller not auto-bound. Please assign XR Origin controller to ControllerTransformFollower.controllerTransform manually.");
        return null;
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

    private static SimpleGripInteractionState SetupSimpleGripInteractionState(Transform parent)
    {
        var stateObject = GetOrCreate("SimpleGripInteractionState", parent);
        var state = EnsureComponent<SimpleGripInteractionState>(stateObject);
        if (state == null) return null;

        state.ResetState();
        EditorUtility.SetDirty(stateObject);
        return state;
    }

    private static ControllerBallGrabber SetupControllerBallGrabber(Transform parent, Transform leftController, SimpleGripInteractionState gripState)
    {
        var grabberObject = GetOrCreate("LeftBallGrabber", parent);
        var grabber = EnsureComponent<ControllerBallGrabber>(grabberObject);
        if (grabber == null) return null;

        grabber.controllerTransform = leftController;
        grabber.controllerNode = XRNode.LeftHand;
        grabber.grabRadius = 0.28f;
        grabber.releaseSpeedMultiplier = 1.0f;
        grabber.minimumReleaseSpeed = 0.35f;
        grabber.grabScanInterval = 0.04f;
        grabber.grabLayers = ~0;
        grabber.holdOffset = new Vector3(0f, 0f, 0.08f);
        grabber.interactionState = gripState;
        grabber.gripInputSourceBehaviour = SetupControllerGripInputSource(leftController, XRNode.LeftHand);
        grabber.autoCreatePicoGripInputSource = true;
        EditorUtility.SetDirty(grabberObject);
        return grabber;
    }

    private static MonoBehaviour SetupControllerGripInputSource(Transform controller, XRNode node)
    {
        if (controller == null) return null;

        var behaviours = controller.GetComponents<MonoBehaviour>();
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IGripInputSource)
            {
                return behaviours[i];
            }
        }

        var picoGrip = EnsureComponent<PicoGripInputSource>(controller.gameObject);
        if (picoGrip == null) return null;

        picoGrip.controllerNode = node;
        EditorUtility.SetDirty(controller.gameObject);
        return picoGrip;
    }

    private static PingPongPlayerBodyProxy SetupPlayerBodyProxy(Transform parent)
    {
        var proxyObject = GetOrCreate("PlayerBodyProxy", parent);
        var proxy = EnsureComponent<PingPongPlayerBodyProxy>(proxyObject);
        if (proxy == null) return null;

        proxy.hmdTransform = Camera.main != null ? Camera.main.transform : null;
        proxy.floorY = 0f;
        proxy.bodyHeightMeters = 1.2f;
        proxy.bodyRadiusMeters = 0.18f;
        proxy.playerBodyTag = "PlayerBody";
        proxy.playerBodyLayerName = "PlayerBody";
        SetLayerRecursively(proxyObject, "PlayerBody");
        EditorUtility.SetDirty(proxyObject);
        return proxy;
    }

    private static void SetupControllerTableLimiter(GameObject controllerVisual, Transform tableTransform)
    {
        if (controllerVisual == null) return;

        var limiter = EnsureComponent<ControllerTableCollisionLimiter>(controllerVisual);
        if (limiter == null) return;

        limiter.tableTransform = tableTransform;
        limiter.tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
        limiter.tableTopY = tableTransform != null
            ? tableTransform.position.y + PingPongGeometry.TableThickness * 0.5f
            : PingPongGeometry.TableTopHeight;
        limiter.horizontalMargin = 0.04f;
        limiter.verticalMargin = 0.03f;
        EditorUtility.SetDirty(controllerVisual);
    }

    private static TableDragHandle SetupTableDragHandle(Transform parent, GameObject table, Transform leftController, ControllerBallGrabber leftBallGrabber, BallSpawner spawner, Transform spawn, Transform target, Transform tableBlocker, bool mixedRealityPlacement, params Transform[] extraSyncedTransforms)
    {
        if (table == null) return null;

        var handle = GetOrCreate("LeftTableDragHandle", parent);
        handle.transform.SetParent(table.transform, false);
        handle.transform.localPosition = new Vector3(-PingPongGeometry.TableWidth * 0.5f - 0.08f, PingPongGeometry.TableThickness * 0.5f + 0.08f, -PingPongGeometry.TableLength * 0.5f + 0.16f);
        handle.transform.localRotation = Quaternion.identity;
        handle.transform.localScale = Vector3.one;

        RemoveChildIfExists(handle.transform, "HandleVisual");
        RemoveComponentIfExists<SphereCollider>(handle);

        var dragHandle = EnsureComponent<TableDragHandle>(handle);
        if (dragHandle != null)
        {
            var tableBounceLocalZ = 1.35f - PingPongGeometry.TableCenter.z;
            dragHandle.tableRoot = table.transform;
            dragHandle.controllerTransform = leftController;
            dragHandle.controllerNode = XRNode.LeftHand;
            dragHandle.ballGrabber = leftBallGrabber;
            dragHandle.hmdTransform = Camera.main != null ? Camera.main.transform : null;
            dragHandle.syncedTransforms = BuildSyncedTransformList(spawn, target, tableBlocker, extraSyncedTransforms);
            dragHandle.syncedSpawners = spawner != null ? new[] { spawner } : null;
            dragHandle.syncDifficultyPanel = true;
            dragHandle.activationRadius = 0.2f;
            dragHandle.tableBounceLocalZ = tableBounceLocalZ;
            dragHandle.minimumNetClearanceAboveNet = 0.16f;
            dragHandle.lockTableHeight = true;
            dragHandle.constrainToBounds = !mixedRealityPlacement;
            dragHandle.xBounds = mixedRealityPlacement ? new Vector2(-3f, 3f) : new Vector2(-1.5f, 1.5f);
            dragHandle.zBounds = mixedRealityPlacement ? new Vector2(0.35f, 4.5f) : new Vector2(0.55f, 3.8f);
            dragHandle.loadSavedPlacementOnEnable = false;
            dragHandle.savePlacementOnRelease = false;
            dragHandle.placementSaveKey = "PingPong.MixedReality.Table";
            dragHandle.syncedControllerLimiters = Object.FindObjectsOfType<ControllerTableCollisionLimiter>(true);
            dragHandle.positionSensitivity = 1.0f;
            dragHandle.rotationSensitivity = 0.35f;
            dragHandle.maxMoveSpeedMetersPerSecond = 3.0f;
            dragHandle.positionSmoothingSeconds = 0.025f;
            dragHandle.dragDeadZoneMeters = 0.005f;
            dragHandle.minUserTableDistanceMeters = 0.5f;
            dragHandle.maxUserTableDistanceMeters = 3f;
            dragHandle.enableLocalHandleDrag = false;
            dragHandle.hideLocalHandleVisuals = true;
            dragHandle.enforceStandardTableHeightOnEnable = true;
            dragHandle.standardTableTopHeight = PingPongGeometry.TableTopHeight;
            dragHandle.ConfigureLocalHandleInteraction();

            if (spawner != null)
            {
                spawner.tableTransform = table.transform;
                spawner.useTableRelativeServeTargets = true;
                spawner.netLocalZ = 0f;
                spawner.tableBounceLocalZ = tableBounceLocalZ;
                spawner.netWorldZ = table.transform.TransformPoint(new Vector3(0f, 0f, spawner.netLocalZ)).z;
                spawner.tableBounceWorldY = table.transform.position.y + PingPongGeometry.TableThickness * 0.5f + PingPongGeometry.BallRadius;
                spawner.tableBounceWorldZ = table.transform.TransformPoint(new Vector3(0f, 0f, tableBounceLocalZ)).z;
                EditorUtility.SetDirty(spawner);
            }

            dragHandle.SyncHeightDependentValues();
        }

        var passiveLock = EnsureComponent<TablePassiveMotionLock>(table);
        if (passiveLock != null)
        {
            passiveLock.dragHandle = dragHandle;
            passiveLock.AcceptCurrentTransform();
            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(handle);
        return dragHandle;
    }

    private static Transform[] BuildSyncedTransformList(Transform spawn, Transform target, Transform tableBlocker, Transform[] extraSyncedTransforms)
    {
        var transforms = new List<Transform> { spawn, target, tableBlocker };
        if (extraSyncedTransforms != null)
        {
            foreach (var syncedTransform in extraSyncedTransforms)
            {
                if (syncedTransform != null)
                {
                    if (TableDragHandle.IsDetachedWorldUiTransform(syncedTransform))
                    {
                        continue;
                    }

                    transforms.Add(syncedTransform);
                }
            }
        }

        return transforms.ToArray();
    }

    private static void SetupInitialViewAligner(Transform parent, bool mixedRealityMode = false)
    {
        var alignerObject = GetOrCreate("InitialViewAligner", parent);
        var aligner = EnsureComponent<VrInitialViewAligner>(alignerObject);
        if (aligner == null) return;

        aligner.desiredHeadWorldPosition = new Vector3(0f, 1.6f, 0.25f);
        aligner.lookAtWorldPosition = new Vector3(0f, PingPongGeometry.TableTopHeight + 0.35f, PingPongGeometry.TableCenter.z);
        aligner.alignOnStart = !mixedRealityMode;
        aligner.alignPosition = !mixedRealityMode;
        EditorUtility.SetDirty(alignerObject);
    }

    private static void SetupMixedRealityMode(Transform managers, Transform environment, Transform table, TableDragHandle dragHandle, ControllerBallGrabber leftBallGrabber, SimpleGripInteractionState gripState)
    {
        var mrObject = GetOrCreate("MixedRealityManager", managers);
        var mrManager = EnsureComponent<PingPongMixedRealityManager>(mrObject);
        if (mrManager != null)
        {
            mrManager.enableOnStart = true;
            mrManager.enableVideoSeeThrough = true;
            mrManager.configureTransparentCamera = true;
            mrManager.disableVirtualEnvironment = true;
            mrManager.suppressBackgroundVisuals = true;
            mrManager.targetCamera = Camera.main;
            mrManager.virtualEnvironmentObjects = CollectVirtualEnvironmentObjects(environment);
            EditorUtility.SetDirty(mrObject);
        }

        var backgroundSuppressor = EnsureComponent<MrBackgroundVisualSuppressor>(mrObject);
        if (backgroundSuppressor != null)
        {
            backgroundSuppressor.hideAllEnvironmentRenderers = true;
            backgroundSuppressor.hideAllRoomSensingRenderers = true;
            backgroundSuppressor.scanIntervalSeconds = 0.15f;
            EditorUtility.SetDirty(mrObject);
        }

        DestroyNamedObjectsIncludingInactive("RoomPlaneAligner");
        RemoveSceneComponents<PingPongRoomPlaneAligner>();

        var placerObject = GetOrCreate("TableOpenSpacePlacer", managers);
        var tablePlacer = EnsureComponent<PingPongOpenSpaceTablePlacer>(placerObject);
        if (tablePlacer != null)
        {
            tablePlacer.tableRoot = table;
            tablePlacer.tableDragHandle = dragHandle;
            tablePlacer.hmdTransform = Camera.main != null ? Camera.main.transform : null;
            tablePlacer.remoteDragControllerTransform = dragHandle != null ? dragHandle.controllerTransform : null;
            tablePlacer.ballGrabber = leftBallGrabber != null ? leftBallGrabber : (dragHandle != null ? dragHandle.ballGrabber : Object.FindObjectOfType<ControllerBallGrabber>(true));
            tablePlacer.interactionState = gripState != null ? gripState : Object.FindObjectOfType<SimpleGripInteractionState>(true);
            tablePlacer.ballSpawners = Object.FindObjectsOfType<BallSpawner>(true);
            tablePlacer.autoPlaceOnStart = false;
            tablePlacer.clearSavedPlacementOnStart = true;
            tablePlacer.controlServing = true;
            tablePlacer.allowAutomaticResumeServing = false;
            tablePlacer.clearBallsWhenTableMoves = true;
            tablePlacer.startServingAfterClearPlacement = false;
            tablePlacer.startServingAfterManualPlacement = false;
            tablePlacer.startServingAfterConfirmedPlacementOnly = false;
            tablePlacer.disableSpatialTablePlacementForNow = true;
            tablePlacer.requireRoomSensingColliderForAutoPlacement = true;
            tablePlacer.minimumRoomSensingColliderCount = 1;
            tablePlacer.desiredDistanceMeters = 2.05f;
            tablePlacer.minDistanceMeters = 1.35f;
            tablePlacer.maxDistanceMeters = 3.8f;
            tablePlacer.clearanceRadiusMeters = 1.65f;
            tablePlacer.clearanceHeightMeters = 1.15f;
            tablePlacer.fallbackFloorY = 0f;
            tablePlacer.tableCenterHeightAboveFloor = TunedTableTopY - PingPongGeometry.TableThickness * 0.5f;
            tablePlacer.ignoreCeilingPlanes = true;
            tablePlacer.maxAcceptedFloorY = 0.4f;
            tablePlacer.minimumFloorBelowHeadMeters = 0.6f;
            tablePlacer.searchDurationSeconds = 8f;
            tablePlacer.searchIntervalSeconds = 0.5f;
            tablePlacer.enableRemoteDrag = true;
            tablePlacer.remoteDragControllerNode = XRNode.LeftHand;
            tablePlacer.remoteGrabSelectableRadiusMeters = 2.35f;
            tablePlacer.remoteGrabMaxDistanceMeters = 8f;
            tablePlacer.remoteDragMaxRayDistanceMeters = 8f;
            tablePlacer.remoteDragActivationRadiusMeters = 2.35f;
            tablePlacer.positionSensitivity = 1.0f;
            tablePlacer.rotationSensitivity = 0.35f;
            tablePlacer.maxMoveSpeedMetersPerSecond = 3.0f;
            tablePlacer.positionSmoothingSeconds = 0.025f;
            tablePlacer.dragDeadZoneMeters = 0.005f;
            tablePlacer.minUserTableDistanceMeters = 0.5f;
            tablePlacer.maxUserTableDistanceMeters = 3f;
            EditorUtility.SetDirty(placerObject);
        }

        var remoteTableDrag = SetupRemoteTableDragController(managers, table, dragHandle, leftBallGrabber, gripState);
        if (tablePlacer != null)
        {
            tablePlacer.remoteTableDragController = remoteTableDrag;
            EditorUtility.SetDirty(placerObject);
        }

        SetupPicoRoomSensingManagers(managers);
        ConfigureMainCameraForPassthrough();
    }

    private static RemoteTableDragController SetupRemoteTableDragController(Transform managers, Transform table, TableDragHandle dragHandle, ControllerBallGrabber leftBallGrabber, SimpleGripInteractionState gripState)
    {
        var remoteObject = GetOrCreate("RemoteTableDragController", managers);
        var remoteDrag = EnsureComponent<RemoteTableDragController>(remoteObject);
        if (remoteDrag == null) return null;

        remoteDrag.enableRemoteDrag = false;
        remoteDrag.disableRemoteTableDragForNow = true;
        remoteDrag.tableRoot = table;
        remoteDrag.tableDragHandle = dragHandle;
        remoteDrag.controllerTransform = dragHandle != null ? dragHandle.controllerTransform : null;
        remoteDrag.controllerNode = XRNode.LeftHand;
        remoteDrag.gripInputSourceBehaviour = leftBallGrabber != null ? leftBallGrabber.gripInputSourceBehaviour : null;
        remoteDrag.autoCreatePicoGripInputSource = true;
        remoteDrag.hmdTransform = Camera.main != null ? Camera.main.transform : null;
        remoteDrag.ballGrabber = leftBallGrabber != null ? leftBallGrabber : (dragHandle != null ? dragHandle.ballGrabber : Object.FindObjectOfType<ControllerBallGrabber>(true));
        remoteDrag.interactionState = gripState != null ? gripState : Object.FindObjectOfType<SimpleGripInteractionState>(true);
        remoteDrag.openSpaceTablePlacer = Object.FindObjectOfType<PingPongOpenSpaceTablePlacer>(true);
        remoteDrag.ballSpawners = Object.FindObjectsOfType<BallSpawner>(true);
        remoteDrag.remoteGrabMaxDistanceMeters = 8f;
        remoteDrag.positionSensitivity = 1.0f;
        remoteDrag.maxMoveSpeed = 3.0f;
        remoteDrag.positionSmoothing = 0.025f;
        remoteDrag.dragDeadZone = 0.005f;
        remoteDrag.minDistanceFromUser = 0.7f;
        remoteDrag.maxDistanceFromUser = 3.0f;
        remoteDrag.controlServing = true;
        remoteDrag.allowAutomaticResumeServing = false;
        remoteDrag.clearBallsWhenDragging = true;
        remoteDrag.resumeServingOnRelease = false;
        remoteDrag.SetRemoteDragEnabled(false);
        EditorUtility.SetDirty(remoteObject);
        return remoteDrag;
    }

    private static void SetupPicoRoomSensingManagers(Transform managers)
    {
        var sensingRoot = GetOrCreate("MRSpaceSensing", managers);
        sensingRoot.transform.localPosition = Vector3.zero;
        sensingRoot.transform.localRotation = Quaternion.identity;
        sensingRoot.transform.localScale = Vector3.one;
        SetLayerRecursively(sensingRoot, "RoomSensing");

        var planeTemplate = SetupRoomSensingTemplate(
            sensingRoot.transform,
            "MRDetectedPlaneTemplate",
            CreateOrLoadTransparentMaterial("MRDetectedPlaneCyan", new Color(0.15f, 0.85f, 1f, 0.22f)));
        var planeManager = EnsureComponent<PXR_PlaneDetectionManager>(sensingRoot);
        if (planeManager != null)
        {
            planeManager.planePrefab = planeTemplate;
        }

        var meshTemplate = SetupRoomSensingTemplate(
            sensingRoot.transform,
            "MRSpatialMeshTemplate",
            CreateOrLoadTransparentMaterial("MRSpatialMeshBlue", new Color(0.25f, 0.5f, 1f, 0.12f)));
        var meshManager = EnsureComponent<PXR_SpatialMeshManager>(sensingRoot);
        if (meshManager != null)
        {
            meshManager.meshPrefab = meshTemplate;
        }

        var visibilityGuard = EnsureComponent<PingPongRoomSensingVisibilityGuard>(sensingRoot);
        if (visibilityGuard != null)
        {
            visibilityGuard.roomSensingRoot = sensingRoot.transform;
            visibilityGuard.hideAllRenderersUnderRoot = true;
            visibilityGuard.addMissingMeshColliders = true;
            visibilityGuard.ignoreBallCollision = true;
            visibilityGuard.roomSensingPhysicsLayerName = "RoomSensing";
            visibilityGuard.ballPhysicsLayerName = "Ball";
            visibilityGuard.scanIntervalSeconds = 0.15f;
        }

        EditorUtility.SetDirty(sensingRoot);
    }

    private static GameObject SetupRoomSensingTemplate(Transform parent, string name, Material material)
    {
        var template = GetOrCreateChild(name, parent);
        template.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.identity;
        template.transform.localScale = Vector3.one;
        SetLayerRecursively(template, "RoomSensing");

        EnsureComponent<MeshFilter>(template);
        var renderer = EnsureComponent<MeshRenderer>(template);
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        EnsureComponent<MeshCollider>(template);
        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static Material CreateOrLoadTransparentMaterial(string materialName, Color color)
    {
        var material = CreateOrLoadMaterial(materialName, color);
        if (material == null) return null;

        material.color = color;
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureMainCameraForPassthrough()
    {
        var camera = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (camera == null) return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        var clearColor = camera.backgroundColor;
        clearColor.a = 0f;
        camera.backgroundColor = clearColor;
        EditorUtility.SetDirty(camera);
    }

    private static void ConfigureMainCameraForVirtualReality()
    {
        var camera = FindMainCameraIncludingInactive();
        if (camera == null) return;

        camera.clearFlags = CameraClearFlags.Skybox;
        var clearColor = camera.backgroundColor;
        clearColor.a = 1f;
        camera.backgroundColor = clearColor;
        EditorUtility.SetDirty(camera);
    }

    private static Camera FindMainCameraIncludingInactive()
    {
        var camera = Camera.main;
        if (camera != null) return camera;

        var cameras = Object.FindObjectsOfType<Camera>(true);
        foreach (var candidate in cameras)
        {
            if (candidate != null && candidate.CompareTag("MainCamera"))
            {
                return candidate;
            }
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static GameObject[] CollectVirtualEnvironmentObjects(Transform environment)
    {
        var objects = new List<GameObject>();
        if (environment != null)
        {
            foreach (var renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.gameObject != null && !objects.Contains(renderer.gameObject))
                {
                    objects.Add(renderer.gameObject);
                }
            }
        }

        return objects.ToArray();
    }

    private static void AddNamedEnvironmentObject(List<GameObject> objects, string name, Transform environment)
    {
        var go = FindObjectByNameIncludingInactive(name, environment);

        if (go != null && !objects.Contains(go))
        {
            objects.Add(go);
        }
    }

    private static void DisableMixedRealitySceneState()
    {
        DestroyNamedObjectsIncludingInactive("MixedRealityManager", "RoomPlaneAligner", "TableOpenSpacePlacer", "RemoteTableDragController", "MRSpaceSensing");
        RemoveSceneComponents<PingPongMixedRealityManager>();
        RemoveSceneComponents<PingPongRoomPlaneAligner>();
        RemoveSceneComponents<PingPongOpenSpaceTablePlacer>();
        RemoveSceneComponents<RemoteTableDragController>();
        RemoveSceneComponents<PXR_PlaneDetectionManager>();
        RemoveSceneComponents<PXR_SpatialMeshManager>();
    }

    private static void DestroyNamedObjectsIncludingInactive(params string[] names)
    {
        var targets = new List<GameObject>();
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null) continue;

            foreach (var name in names)
            {
                if (transform.name != name) continue;

                targets.Add(transform.gameObject);
                break;
            }
        }

        foreach (var target in targets)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }

    private static void RemoveSceneComponents<T>() where T : Component
    {
        foreach (var component in Object.FindObjectsOfType<T>(true))
        {
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }
    }

    private static void DisableVirtualRoomSurfaces(Transform environment)
    {
        var floor = GetOrCreateSingleEnvironmentSurface("Floor", environment, PrimitiveType.Plane);
        floor.transform.SetParent(environment);
        floor.SetActive(false);

        DisableBackWall(environment);

        if (environment != null)
        {
            foreach (var renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        EditorUtility.SetDirty(floor);
    }

    private static GameObject GetOrCreateSingleEnvironmentSurface(string name, Transform environment, PrimitiveType fallbackPrimitive)
    {
        GameObject primary = null;
        var duplicates = new List<GameObject>();

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name != name) continue;

            if (primary == null || (environment != null && transform.parent == environment))
            {
                if (primary != null)
                {
                    duplicates.Add(primary);
                }

                primary = transform.gameObject;
            }
            else
            {
                duplicates.Add(transform.gameObject);
            }
        }

        foreach (var duplicate in duplicates)
        {
            if (duplicate != null && duplicate != primary)
            {
                Object.DestroyImmediate(duplicate);
            }
        }

        if (primary != null) return primary;

        primary = GameObject.CreatePrimitive(fallbackPrimitive);
        primary.name = name;
        return primary;
    }

    private static GameObject FindObjectByNameIncludingInactive(string name, Transform preferredParent)
    {
        GameObject fallback = null;
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name != name) continue;
            if (preferredParent != null && transform.parent == preferredParent)
            {
                return transform.gameObject;
            }

            if (fallback == null)
            {
                fallback = transform.gameObject;
            }
        }

        return fallback;
    }

    private static void EnsureFloor(Transform parent)
    {
        var floor = GetOrCreateSingleEnvironmentSurface("Floor", parent, PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = Vector3.zero;
        floor.transform.rotation = Quaternion.identity;
        floor.transform.localScale = Vector3.one * 3f;
        floor.SetActive(true);
        EnsureEnvironmentSurfaceCollider(floor, false);
        ConfigureSurface(floor, PingPongSurfaceType.Floor);
        EditorUtility.SetDirty(floor);
    }

    private static void EnsureLight(Transform parent)
    {
        GameObject primary = null;

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name != "Directional Light" && transform.name != "DirectionalLight") continue;

            if (primary == null) primary = transform.gameObject;
            ConfigureDirectionalLight(transform.gameObject);
        }

        if (primary == null)
        {
            primary = new GameObject("Directional Light");
            ConfigureDirectionalLight(primary);
        }

        primary.name = "Directional Light";
        primary.transform.SetParent(parent);
        primary.transform.position = Vector3.zero;
        primary.transform.localScale = Vector3.one;
        ConfigureDirectionalLight(primary);
    }

    private static void ConfigureDirectionalLight(GameObject lightGo)
    {
        if (lightGo == null) return;

        var light = EnsureComponent<Light>(lightGo);
        if (light == null) return;

        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        EditorUtility.SetDirty(lightGo);
    }

    private static void DisableBackWall(Transform parent)
    {
        var backWall = FindObjectByNameIncludingInactive("BackWall", parent);
        if (backWall == null) return;

        backWall.transform.SetParent(parent);
        backWall.SetActive(false);

        var renderer = backWall.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        var collider = backWall.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        EditorUtility.SetDirty(backWall);
    }

    private static void EnsureEnvironmentSurfaceCollider(GameObject surface, bool preferBoxCollider)
    {
        if (surface == null || surface.GetComponent<Collider>() != null) return;

        if (preferBoxCollider)
        {
            surface.AddComponent<BoxCollider>();
            return;
        }

        var meshFilter = surface.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            surface.AddComponent<MeshCollider>();
            return;
        }

        var fallback = surface.AddComponent<BoxCollider>();
        fallback.size = new Vector3(1f, 0.02f, 1f);
    }

    private static GameObject GetOrCreate(string name, Transform parent = null)
    {
        var go = GameObject.Find(name) ?? new GameObject(name);
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

    private static void SetLayerRecursively(GameObject root, string layerName)
    {
        if (root == null || string.IsNullOrEmpty(layerName)) return;

        var layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return;

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform != null)
            {
                transform.gameObject.layer = layer;
            }
        }

        root.layer = layer;
        EditorUtility.SetDirty(root);
    }

    private static void RemoveGeneratedObject(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void RemoveChildIfExists(Transform parent, string childName)
    {
        if (parent == null) return;

        var child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void RemoveComponentIfExists<T>(GameObject go) where T : Component
    {
        if (go == null) return;

        var component = go.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
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
