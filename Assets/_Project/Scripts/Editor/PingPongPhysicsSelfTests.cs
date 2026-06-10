using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PingPongPhysicsSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run PingPong Physics Self Tests")]
    public static void RunAll()
    {
        HeldServeHitUsesPaddleVelocity();
        SideSwipeDoesNotLaunchHeldBall();
        TableBounceReflectsUpward();
        TableBounceKeepsPlayableLiftForSlowImpact();
        TableSurfaceUsesPlayableElasticity();
        SolverClampsMaximumSpeed();
        ContactPlacementChangesLateralDirection();
        ServeProfilesCreateOppositeSpin();
        DifficultyPresetsIncreaseServeSpeed();
        AerodynamicsDragAndTopspinAreDirectional();
        RigidbodySpinLimitCoversServeSpin();
        ControllerBallGrabberReportsNearbyBall();
        SimpleGripStatePreventsModeOverlap();
        TableDragHandleDisablesLocalInteraction();
        TableHeightNormalizesToStandardHeight();
        DifficultyPanelUsesControllerButtonsOnly();
        BallGeometryUsesElderReadableSize();
        TableSurfaceCorrectionRaisesEmbeddedBall();
        TableDragHandleDoesNotSyncWorldUiCanvas();
        ScoreCanvasInstallsRayDragHandle();
        ScoreManagerResetExposesZeroedProperties();
        BallSpawnerServingStateTransitions();
        UnifiedPanelServingButtonTogglesState();
        UnifiedPanelResetDoesNotStopServing();
        UnifiedPanelHomeButtonCallsShowHome();
        ServingAutomationStaysPanelControlled();
        SpatialTablePlacementIsDisabledButManualDragStaysEnabled();
        OpenSpacePlacementWaitsForRoomSensingColliders();
        OpenSpacePlacementAvoidsTableObstacle();
        OpenSpaceTablePlacementMovesServeReferences();
        PlayerTableSafetyUsesTableFootprintOnly();
        PlayerTableSafetyUsesHeadPositionOnly();
        Debug.Log("PingPong physics self tests passed.");
    }

    private static void HeldServeHitUsesPaddleVelocity()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.forward * 2.4f);
        input.minimumClosingSpeed = 0.15f;
        input.minimumSpeed = 3.2f;
        input.maximumSpeed = 9f;
        input.biasTowardPreferredForward = true;
        input.minimumForwardDot = 0.38f;
        input.forwardBlend = 0.82f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Held serve hit should be accepted when paddle moves into its face normal.");
        AssertTrue(result.velocity.z > 3f, "Held serve hit should launch toward the far side.");
    }

    private static void SideSwipeDoesNotLaunchHeldBall()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.right * 4f);
        input.minimumClosingSpeed = 0.15f;
        input.minimumSpeed = 3.2f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(!result.accepted, "Side swipe should not launch a held ball when there is no closing speed.");
    }

    private static void TableBounceReflectsUpward()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.down * 3f, Vector3.zero, Vector3.up, Vector3.zero);
        input.normalRestitution = 0.93f;
        input.tangentialFriction = 0.05f;
        input.maximumSpeed = 9f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Table bounce should be accepted for downward velocity.");
        AssertTrue(result.velocity.y > 2.7f, "Table bounce should reflect upward with playable restitution.");
    }

    private static void TableBounceKeepsPlayableLiftForSlowImpact()
    {
        var adjusted = PingPongBall.EnsureMinimumTableBounceVelocity(
            new Vector3(0.2f, 0.32f, -0.7f),
            new Vector3(0.2f, -0.42f, -0.7f),
            1.35f,
            0.12f,
            9f);

        AssertTrue(adjusted.y >= 1.35f, "A slow tabletop bounce should still lift enough to remain playable.");
        AssertTrue(adjusted.magnitude <= 9.001f, "Minimum tabletop lift should still respect the ball speed cap.");
    }

    private static void TableSurfaceUsesPlayableElasticity()
    {
        var surfaceObject = new GameObject("TableSurfaceDefaults");
        try
        {
            var surface = surfaceObject.AddComponent<PingPongSurface>();
            surface.Configure(PingPongSurfaceType.Table);

            AssertTrue(surface.normalRestitution >= 0.92f, "Table surface restitution should be high enough for normal play.");
            AssertTrue(surface.tangentialFriction <= 0.06f, "Table tangential friction should not over-damp the bounce.");
        }
        finally
        {
            Object.DestroyImmediate(surfaceObject);
        }
    }

    private static void SolverClampsMaximumSpeed()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.back * 12f, Vector3.zero, Vector3.forward, Vector3.forward * 8f);
        input.maximumSpeed = 9f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Fast paddle hit should be accepted.");
        AssertTrue(result.velocity.magnitude <= 9.001f, "Solver should clamp maximum speed.");
    }

    private static void ContactPlacementChangesLateralDirection()
    {
        var velocity = PingPongHitSolver.ApplyPaddleContactPlacement(Vector3.forward * 4f, new Vector3(0.12f, 0f, 0f), 1.15f, 0.35f);
        AssertTrue(velocity.x < -0.01f, "Right-side contact should add leftward lateral direction.");
        AssertTrue(Mathf.Abs(velocity.magnitude - 4f) < 0.001f, "Contact placement should preserve speed.");
    }

    private static void ServeProfilesCreateOppositeSpin()
    {
        var launchVelocity = Vector3.back * 3f;
        var topspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Topspin, launchVelocity, 95f, 80f, 50f);
        var backspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Backspin, launchVelocity, 95f, 80f, 50f);

        AssertTrue(topspin.sqrMagnitude > 1f, "Topspin serve should create angular velocity.");
        AssertTrue(backspin.sqrMagnitude > 1f, "Backspin serve should create angular velocity.");
        AssertTrue(Vector3.Dot(topspin.normalized, backspin.normalized) < -0.99f, "Topspin and backspin should use opposite spin axes.");
    }

    private static void DifficultyPresetsIncreaseServeSpeed()
    {
        var easy = PingPongDifficultyController.GetSpeed(PingPongDifficulty.Easy);
        var normal = PingPongDifficultyController.GetSpeed(PingPongDifficulty.Normal);
        var advanced = PingPongDifficultyController.GetSpeed(PingPongDifficulty.Advanced);
        var challenge = PingPongDifficultyController.GetSpeed(PingPongDifficulty.Challenge);

        AssertTrue(easy < normal, "Easy difficulty should be slower than normal.");
        AssertTrue(normal < advanced, "Advanced difficulty should be faster than normal.");
        AssertTrue(advanced < challenge, "Challenge difficulty should be the fastest preset.");
        AssertTrue(PingPongDifficultyController.GetServeInterval(PingPongDifficulty.Easy) > PingPongDifficultyController.GetServeInterval(PingPongDifficulty.Challenge), "Easy difficulty should leave more time between serves.");
    }

    private static void AerodynamicsDragAndTopspinAreDirectional()
    {
        var velocity = Vector3.back * 6f;
        var topspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Topspin, velocity, 95f, 80f, 50f);
        var acceleration = PingPongBall.CalculateAerodynamicAcceleration(
            velocity,
            topspin,
            PingPongGeometry.BallRadius,
            PingPongGeometry.BallMass,
            1.27f,
            0.5f,
            0.28f,
            45f);

        AssertTrue(Vector3.Dot(acceleration, velocity) < 0f, "Aerodynamic drag should oppose ball velocity.");
        AssertTrue(acceleration.y < 0f, "Topspin moving toward the player should add downward Magnus acceleration.");
    }

    private static void RigidbodySpinLimitCoversServeSpin()
    {
        var ballObject = new GameObject("SpinLimitTestBall");
        try
        {
            var rb = ballObject.AddComponent<Rigidbody>();
            rb.maxAngularVelocity = 7f;
            PingPongBall.ConfigureSpinLimit(rb, 140f);
            AssertTrue(rb.maxAngularVelocity >= PingPongBall.DefaultMaxAngularVelocity, "Spin limit should cover the 180 rad/s ball spin clamp.");

            rb.maxAngularVelocity = 7f;
            PingPongBall.ConfigureSpinLimit(rb, 240f);
            AssertTrue(rb.maxAngularVelocity >= 240f, "Spin limit should cover configured serve spin values above the default clamp.");
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
        }
    }

    private static void ControllerBallGrabberReportsNearbyBall()
    {
        var controller = new GameObject("LeftController");
        var ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            controller.transform.position = Vector3.zero;
            ballObject.name = "NearbyBall";
            ballObject.transform.position = new Vector3(0.08f, 0f, 0f);
            ballObject.AddComponent<Rigidbody>();
            ballObject.AddComponent<PingPongBall>();
            Physics.SyncTransforms();

            var grabber = controller.AddComponent<ControllerBallGrabber>();
            grabber.controllerTransform = controller.transform;
            grabber.grabRadius = 0.28f;

            AssertTrue(grabber.HasNearbyGrabbableBall(), "ControllerBallGrabber should report a grabbable ball inside grab radius.");

            ballObject.transform.position = new Vector3(1.2f, 0f, 0f);
            Physics.SyncTransforms();
            AssertTrue(!grabber.HasNearbyGrabbableBall(), "ControllerBallGrabber should not report a ball outside grab radius.");
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
            Object.DestroyImmediate(controller);
        }
    }

    private static void SimpleGripStatePreventsModeOverlap()
    {
        var stateObject = new GameObject("SimpleGripInteractionStateTest");
        try
        {
            var state = stateObject.AddComponent<SimpleGripInteractionState>();
            state.ResetState();

            AssertTrue(state.TryBegin(SimpleGripInteractionMode.BallGrab), "Grip state should enter BallGrab from None.");
            AssertTrue(!state.TryBegin(SimpleGripInteractionMode.RemoteTableDrag), "Grip state should reject RemoteTableDrag while BallGrab is active.");
            AssertTrue(state.End(SimpleGripInteractionMode.BallGrab), "Grip state should leave BallGrab on release.");
            AssertTrue(state.TryBegin(SimpleGripInteractionMode.RemoteTableDrag), "Grip state should enter RemoteTableDrag after BallGrab ends.");
            AssertTrue(state.End(SimpleGripInteractionMode.RemoteTableDrag), "Grip state should leave RemoteTableDrag on release.");
        }
        finally
        {
            Object.DestroyImmediate(stateObject);
        }
    }

    private static void TableDragHandleDisablesLocalInteraction()
    {
        var handleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            var handle = handleObject.AddComponent<TableDragHandle>();
            handle.enableLocalHandleDrag = false;
            handle.hideLocalHandleVisuals = true;
            handle.ConfigureLocalHandleInteraction();

            var collider = handleObject.GetComponent<Collider>();
            var renderer = handleObject.GetComponent<Renderer>();
            AssertTrue(collider != null && !collider.enabled, "Local table handle collider should be disabled.");
            AssertTrue(renderer != null && !renderer.enabled, "Local table handle visual should be hidden.");
        }
        finally
        {
            Object.DestroyImmediate(handleObject);
        }
    }

    private static void TableHeightNormalizesToStandardHeight()
    {
        var tableObject = new GameObject("Table");
        try
        {
            tableObject.transform.position = new Vector3(0f, 1.25f, 2f);
            var lockComponent = tableObject.AddComponent<TablePassiveMotionLock>();
            lockComponent.NormalizeTableHeightIfNeeded();

            var tableTopY = tableObject.transform.position.y + PingPongGeometry.TableThickness * 0.5f;
            AssertTrue(Mathf.Abs(tableTopY - PingPongGeometry.TableTopHeight) < 0.001f, "Runtime table lock should normalize the table top to the standard height.");
        }
        finally
        {
            Object.DestroyImmediate(tableObject);
        }
    }

    private static void DifficultyPanelUsesControllerButtonsOnly()
    {
        var canvasObject = new GameObject("DifficultyCanvas", typeof(RectTransform));
        var spawnerObject = new GameObject("BallSpawner");
        try
        {
            var spawner = spawnerObject.AddComponent<BallSpawner>();
            var controller = PingPongDifficultyController.EnsureRuntimePanel(canvasObject.transform, spawner, null);

            AssertTrue(controller != null, "Difficulty panel should be created.");
            AssertTrue(!controller.showScreenButtons, "Difficulty panel should hide +/- screen buttons.");
            AssertTrue(controller.enableControllerSpeedButtons, "Difficulty panel should use controller A/B buttons.");
            AssertTrue(!IsChildActive(canvasObject.transform, "DifficultyPanel/DecreaseButton"), "Decrease screen button should be inactive.");
            AssertTrue(!IsChildActive(canvasObject.transform, "DifficultyPanel/IncreaseButton"), "Increase screen button should be inactive.");

            var background = canvasObject.transform.Find("DifficultyPanel/Background");
            AssertTrue(background != null, "Difficulty panel should expose a visible drag surface.");
            var backgroundGraphic = background.GetComponent<Graphic>();
            AssertTrue(backgroundGraphic != null && backgroundGraphic.raycastTarget, "Difficulty panel background should accept ray hits.");
            var dragHandle = background.GetComponent<WorldSpaceUiRayDragHandle>();
            AssertTrue(dragHandle != null, "Difficulty panel background should be ray-draggable.");
            AssertTrue(dragHandle.targetRoot == controller.transform, "Difficulty panel drag should move the difficulty panel only.");
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void BallGeometryUsesElderReadableSize()
    {
        AssertTrue(PingPongGeometry.BallRadius >= 0.028f, "Ping-pong ball radius should be enlarged for elder users.");
        AssertTrue(Mathf.Abs(PingPongGeometry.BallPrefabScale.x - PingPongGeometry.BallDiameter) < 0.0001f, "Ball prefab scale should follow the configured readable diameter.");
    }

    private static void TableSurfaceCorrectionRaisesEmbeddedBall()
    {
        var tableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            tableObject.name = "Table";
            tableObject.transform.position = PingPongGeometry.TableCenter;
            tableObject.transform.localScale = PingPongGeometry.TableColliderWorldSize;
            var surface = tableObject.AddComponent<PingPongSurface>();
            surface.Configure(PingPongSurfaceType.Table);

            ballObject.name = "EmbeddedBall";
            ballObject.transform.localScale = PingPongGeometry.BallPrefabScale;
            var rigidbody = ballObject.AddComponent<Rigidbody>();
            rigidbody.velocity = Vector3.down;
            var ball = ballObject.AddComponent<PingPongBall>();

            Physics.SyncTransforms();
            var tableCollider = tableObject.GetComponent<Collider>();
            var embeddedCenterY = tableCollider.bounds.max.y + PingPongGeometry.BallRadius * 0.25f;
            ballObject.transform.position = new Vector3(0f, embeddedCenterY, PingPongGeometry.TableCenter.z);
            Physics.SyncTransforms();

            AssertTrue(ball.CorrectSurfacePenetrationIfNeeded(surface, tableCollider, Vector3.up, tableCollider.bounds.max), "Embedded table ball should be corrected out of the tabletop.");
            AssertTrue(ballObject.transform.position.y >= tableCollider.bounds.max.y + PingPongGeometry.BallRadius, "Corrected ball center should sit above the tabletop.");
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
            Object.DestroyImmediate(tableObject);
        }
    }

    private static void TableDragHandleDoesNotSyncWorldUiCanvas()
    {
        var tableObject = new GameObject("Table");
        var uiObject = new GameObject("WorldSpaceCanvas", typeof(RectTransform), typeof(Canvas));
        var handleObject = new GameObject("TableHandle");
        try
        {
            tableObject.transform.position = PingPongGeometry.TableCenter;
            uiObject.transform.position = new Vector3(-0.5f, 1.55f, 2.8f);
            uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            var initialUiPosition = uiObject.transform.position;

            var dragHandle = handleObject.AddComponent<TableDragHandle>();
            dragHandle.tableRoot = tableObject.transform;
            dragHandle.syncedTransforms = new[] { uiObject.transform };
            dragHandle.lockTableHeight = false;
            dragHandle.constrainToBounds = false;
            dragHandle.SetTablePosition(tableObject.transform.position + Vector3.right);

            AssertTrue(Vector3.Distance(uiObject.transform.position, initialUiPosition) < 0.001f, "Detached world-space UI canvas should not be moved by table placement.");
        }
        finally
        {
            Object.DestroyImmediate(handleObject);
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(tableObject);
        }
    }

    private static void ScoreCanvasInstallsRayDragHandle()
    {
        var canvasObject = new GameObject("ScoreCanvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            canvasObject.SetActive(false);
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 560f);
            var hitText = CreateTestScoreText(canvasObject.transform, "HitText", new Vector2(0f, 150f));
            var servedText = CreateTestScoreText(canvasObject.transform, "ServedText", new Vector2(0f, 90f));
            var missedText = CreateTestScoreText(canvasObject.transform, "MissedText", new Vector2(0f, 30f));
            var accuracyText = CreateTestScoreText(canvasObject.transform, "AccuracyText", new Vector2(0f, -30f));
            var speedText = CreateTestScoreText(canvasObject.transform, "LastSpeedText", new Vector2(0f, -90f));
            var spinText = CreateTestScoreText(canvasObject.transform, "LastSpinText", new Vector2(0f, -150f));

            var score = canvasObject.AddComponent<ScoreManager>();
            score.autoCreateDifficultyControls = false;
            score.hitText = hitText;
            score.servedText = servedText;
            score.missedText = missedText;
            score.accuracyText = accuracyText;
            score.lastSpeedText = speedText;
            score.lastSpinText = spinText;

            canvasObject.SetActive(true);
            typeof(ScoreManager)
                .GetMethod("EnsureReadableHud", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(score, null);
            score.EnsureDisplayCanvasInteraction();

            AssertTrue(canvasObject.GetComponent<ComfortWorldSpaceUIPlacer>() != null, "Score canvas should get a comfort placer for ray manipulation.");
            AssertTrue(rect.Find("RayDragHandle") != null, "Score canvas should expose a ray-drag handle.");

            var backdrop = rect.Find("ScoreHudBackdrop");
            AssertTrue(backdrop != null, "Score HUD should create a readable backdrop.");
            var backdropGraphic = backdrop.GetComponent<Graphic>();
            AssertTrue(backdropGraphic != null && backdropGraphic.raycastTarget, "Score HUD backdrop should accept ray hits.");
            var panelDrag = backdrop.GetComponent<WorldSpaceUiRayDragHandle>();
            AssertTrue(panelDrag != null, "Score HUD backdrop should be ray-draggable.");
            AssertTrue(panelDrag.targetRoot == canvasObject.transform, "Score HUD drag should move the detached world UI canvas.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void ScoreManagerResetExposesZeroedProperties()
    {
        var scoreObject = new GameObject("ScoreManagerProperties");
        try
        {
            var score = scoreObject.AddComponent<ScoreManager>();
            score.useUnifiedControlPanel = true;
            SetPrivateField(score, "_servedCount", 1);
            SetPrivateField(score, "_hitCount", 1);
            SetPrivateField(score, "_missedCount", 1);
            SetPrivateField(score, "_lastHitSpeed", 4.2f);
            SetPrivateField(score, "_lastSpinSpeed", 18f);

            AssertTrue(score.ServedCount == 1, "ScoreManager should expose served count.");
            AssertTrue(score.HitCount == 1, "ScoreManager should expose hit count.");
            AssertTrue(score.MissedCount == 1, "ScoreManager should expose missed count.");
            AssertTrue(score.LastHitSpeed > 4f, "ScoreManager should expose last hit speed.");
            AssertTrue(score.LastSpinSpeed > 17f, "ScoreManager should expose last spin speed.");
            AssertTrue(score.Accuracy > 99f, "ScoreManager should expose accuracy as a percentage.");

            score.ResetScore();

            AssertTrue(score.ServedCount == 0, "ResetScore should clear served count.");
            AssertTrue(score.HitCount == 0, "ResetScore should clear hit count.");
            AssertTrue(score.MissedCount == 0, "ResetScore should clear missed count.");
            AssertTrue(score.LastHitSpeed == 0f, "ResetScore should clear last hit speed.");
            AssertTrue(score.LastSpinSpeed == 0f, "ResetScore should clear last spin speed.");
            AssertTrue(score.Accuracy == 0f, "ResetScore should clear accuracy.");
        }
        finally
        {
            Object.DestroyImmediate(scoreObject);
        }
    }

    private static void BallSpawnerServingStateTransitions()
    {
        var spawnerObject = new GameObject("BallSpawnerState");
        try
        {
            var spawner = spawnerObject.AddComponent<BallSpawner>();
            AssertTrue(!spawner.IsServing, "BallSpawner should start idle.");

            spawner.StartServing();
            AssertTrue(spawner.IsServing, "StartServing should mark the spawner as serving.");

            spawner.StopServing();
            AssertTrue(!spawner.IsServing, "StopServing should mark the spawner as stopped.");
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    private static void UnifiedPanelServingButtonTogglesState()
    {
        var canvasObject = new GameObject("UnifiedPanelCanvas", typeof(RectTransform), typeof(Canvas));
        var scoreObject = new GameObject("UnifiedPanelScore");
        var spawnerObject = new GameObject("UnifiedPanelSpawner");
        var difficultyObject = new GameObject("UnifiedPanelDifficulty");
        try
        {
            var score = scoreObject.AddComponent<ScoreManager>();
            score.useUnifiedControlPanel = true;
            var spawner = spawnerObject.AddComponent<BallSpawner>();
            var difficulty = difficultyObject.AddComponent<PingPongDifficultyController>();
            difficulty.ballSpawner = spawner;
            difficulty.displayStandalonePanel = false;
            difficulty.enhancePanelReadability = false;
            difficulty.ApplyLoadedDifficulty();

            var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvasObject.transform, score, spawner, difficulty, null, null);
            AssertTrue(panel != null && panel.servingToggleButton != null, "Unified panel should create a serving toggle button.");
            AssertTrue(panel.titleText != null && panel.titleText.text == "\u4e52\u4e53\u7403\u8bad\u7ec3", "Unified panel should show a readable Chinese title.");
            AssertTrue(panel.resetButton != null && panel.resetButton.gameObject.activeSelf, "Unified panel should show the reset button.");
            AssertTrue(panel.homeButton != null && panel.homeButton.gameObject.activeSelf, "Unified panel should show the home button.");

            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(spawner.IsServing, "Unified panel serving button should start serving.");

            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(!spawner.IsServing, "Unified panel serving button should stop serving.");
        }
        finally
        {
            Object.DestroyImmediate(difficultyObject);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(scoreObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void UnifiedPanelResetDoesNotStopServing()
    {
        var canvasObject = new GameObject("UnifiedPanelResetCanvas", typeof(RectTransform), typeof(Canvas));
        var scoreObject = new GameObject("UnifiedPanelResetScore");
        var spawnerObject = new GameObject("UnifiedPanelResetSpawner");
        try
        {
            var score = scoreObject.AddComponent<ScoreManager>();
            score.useUnifiedControlPanel = true;
            var spawner = spawnerObject.AddComponent<BallSpawner>();
            var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvasObject.transform, score, spawner, null, null, null);

            PingPongEvents.BallServed(new BallServedInfo(scoreObject, Vector3.zero, Vector3.forward, Vector3.zero, PingPongServeProfile.Basic));
            spawner.StartServing();
            panel.resetButton.onClick.Invoke();

            AssertTrue(spawner.IsServing, "Unified panel reset button should not stop serving.");
            AssertTrue(score.ServedCount == 0 && score.HitCount == 0 && score.MissedCount == 0, "Unified panel reset button should reset score data.");
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(scoreObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void UnifiedPanelHomeButtonCallsShowHome()
    {
        var canvasObject = new GameObject("UnifiedPanelHomeCanvas", typeof(RectTransform), typeof(Canvas));
        var homeRoot = new GameObject("HomeRoot");
        var pingPongRoot = new GameObject("PingPongRoot");
        var scoreObject = new GameObject("UnifiedPanelHomeScore");
        var spawnerObject = new GameObject("UnifiedPanelHomeSpawner");
        var menuObject = new GameObject("UnifiedPanelHomeMenu");
        try
        {
            homeRoot.SetActive(false);
            pingPongRoot.SetActive(true);

            var score = scoreObject.AddComponent<ScoreManager>();
            score.useUnifiedControlPanel = true;
            var spawner = spawnerObject.AddComponent<BallSpawner>();
            spawner.StartServing();

            var menu = menuObject.AddComponent<ElderCareHomeMenu>();
            menu.homeRoot = homeRoot;
            menu.pingPongGameplayRoots = new[] { pingPongRoot, canvasObject };
            menu.ballSpawner = spawner;
            menu.scoreManager = score;
            menu.placeHomeUiOnShow = false;

            var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvasObject.transform, score, spawner, null, menu, null);
            panel.homeButton.onClick.Invoke();

            AssertTrue(homeRoot.activeSelf, "Unified panel home button should call ShowHome and show the home root.");
            AssertTrue(!pingPongRoot.activeSelf, "Unified panel home button should call ShowHome and hide gameplay roots.");
            AssertTrue(!spawner.IsServing, "ShowHome should remain responsible for stopping serving.");
        }
        finally
        {
            Object.DestroyImmediate(menuObject);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(scoreObject);
            Object.DestroyImmediate(pingPongRoot);
            Object.DestroyImmediate(homeRoot);
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void ServingAutomationStaysPanelControlled()
    {
        var canvasObject = new GameObject("PanelOnlyServingCanvas", typeof(RectTransform), typeof(Canvas));
        var scoreObject = new GameObject("PanelOnlyServingScore");
        var spawnerObject = new GameObject("PanelOnlyServingSpawner");
        var ballContainerObject = new GameObject("PanelOnlyBallContainer");
        var menuObject = new GameObject("PanelOnlyServingMenu");
        var homeRoot = new GameObject("PanelOnlyHomeRoot");
        var pingPongRoot = new GameObject("PanelOnlyPingPongRoot");
        var tableObject = new GameObject("PanelOnlyTable");
        var hmdObject = new GameObject("PanelOnlyHmd");
        var controllerObject = new GameObject("PanelOnlyController");
        var remoteDragObject = new GameObject("PanelOnlyRemoteDrag");
        var safetyObject = new GameObject("PanelOnlySafety");
        var existingGripState = Object.FindObjectOfType<SimpleGripInteractionState>(true);
        try
        {
            var score = scoreObject.AddComponent<ScoreManager>();
            score.useUnifiedControlPanel = true;

            var spawner = spawnerObject.AddComponent<BallSpawner>();
            spawner.ballContainer = ballContainerObject.transform;
            AssertTrue(!spawner.autoStartOnPlay, "BallSpawner should not auto-start serving on play.");

            var menu = menuObject.AddComponent<ElderCareHomeMenu>();
            menu.homeRoot = homeRoot;
            menu.pingPongGameplayRoots = new[] { pingPongRoot };
            menu.ballSpawner = spawner;
            menu.scoreManager = score;
            menu.placeHomeUiOnShow = false;
            menu.StartPingPongModule();
            AssertTrue(!spawner.IsServing, "Opening PingPong should leave serving stopped until the unified panel button is pressed.");

            var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvasObject.transform, score, spawner, null, menu, null);
            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(spawner.IsServing, "Unified panel serving button should start serving.");

            var missedBeforePanelPause = score.MissedCount;
            AddLifetimeBall(ballContainerObject.transform, "PanelPauseCleanupBall");
            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(!spawner.IsServing, "Unified panel serving button should pause serving.");
            AssertTrue(ballContainerObject.transform.childCount == 0, "Unified panel pause should clear existing balls.");
            AssertTrue(score.MissedCount == missedBeforePanelPause, "Unified panel pause cleanup should not count cleared balls as missed.");

            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(spawner.IsServing, "Unified panel serving button should resume serving after panel pause.");

            var missedBeforeRemoteDrag = score.MissedCount;
            AddLifetimeBall(ballContainerObject.transform, "RemoteDragCleanupBall");
            var remoteDrag = remoteDragObject.AddComponent<RemoteTableDragController>();
            remoteDrag.tableRoot = tableObject.transform;
            remoteDrag.controllerTransform = controllerObject.transform;
            remoteDrag.ballSpawners = new[] { spawner };
            remoteDrag.controlServing = true;
            remoteDrag.allowAutomaticResumeServing = false;
            remoteDrag.resumeServingOnRelease = true;
            InvokePrivateMethod(remoteDrag, "BeginRemoteDrag");
            InvokePrivateMethod(remoteDrag, "EndRemoteDrag");
            AssertTrue(!spawner.IsServing, "Remote table drag should pause serving.");
            AssertTrue(ballContainerObject.transform.childCount == 0, "Remote table drag pause should clear existing balls.");
            AssertTrue(score.MissedCount == missedBeforeRemoteDrag, "Remote table drag cleanup should not count cleared balls as missed.");

            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(spawner.IsServing, "Unified panel should resume serving after remote table drag paused it.");

            var missedBeforeSafety = score.MissedCount;
            AddLifetimeBall(ballContainerObject.transform, "SafetyCleanupBall");
            var safety = safetyObject.AddComponent<PingPongPlayerTableSafety>();
            safety.tableTransform = tableObject.transform;
            safety.hmdTransform = hmdObject.transform;
            safety.ballSpawners = new[] { spawner };
            safety.controlServing = true;
            safety.allowAutomaticResumeServing = false;
            safety.resumeStableSeconds = 0f;
            safety.createRuntimePrompt = false;
            safety.createRuntimeBoundary = false;
            hmdObject.transform.position = tableObject.transform.position;
            InvokePrivateMethod(safety, "LateUpdate");
            AssertTrue(!spawner.IsServing, "Table safety should pause serving when the HMD enters the boundary.");
            AssertTrue(ballContainerObject.transform.childCount == 0, "Table safety pause should clear existing balls.");
            AssertTrue(score.MissedCount == missedBeforeSafety, "Table safety cleanup should not count cleared balls as missed.");

            hmdObject.transform.position = tableObject.transform.position + Vector3.right * 3f;
            InvokePrivateMethod(safety, "LateUpdate");
            AssertTrue(!spawner.IsServing, "Leaving the table safety boundary should not auto-resume serving.");

            panel.servingToggleButton.onClick.Invoke();
            AssertTrue(spawner.IsServing, "Unified panel should resume serving after safety paused it.");
        }
        finally
        {
            if (existingGripState == null)
            {
                var generatedGripState = Object.FindObjectOfType<SimpleGripInteractionState>(true);
                if (generatedGripState != null)
                {
                    Object.DestroyImmediate(generatedGripState.gameObject);
                }
            }

            Object.DestroyImmediate(safetyObject);
            Object.DestroyImmediate(remoteDragObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(hmdObject);
            Object.DestroyImmediate(tableObject);
            Object.DestroyImmediate(pingPongRoot);
            Object.DestroyImmediate(homeRoot);
            Object.DestroyImmediate(menuObject);
            Object.DestroyImmediate(ballContainerObject);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(scoreObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void AddLifetimeBall(Transform container, string name)
    {
        var ball = new GameObject(name);
        ball.transform.SetParent(container, false);
        ball.AddComponent<BallLifetime>();
    }

    private static TextMeshProUGUI CreateTestScoreText(Transform parent, string name, Vector2 anchoredPosition)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(480f, 48f);
        rect.anchoredPosition = anchoredPosition;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = name;
        return text;
    }

    private static void SpatialTablePlacementIsDisabledButManualDragStaysEnabled()
    {
        var placerObject = new GameObject("TableOpenSpacePlacer");
        var remoteDragObject = new GameObject("RemoteTableDragController");
        var existingGripState = Object.FindObjectOfType<SimpleGripInteractionState>(true);
        try
        {
            var placer = placerObject.AddComponent<PingPongOpenSpaceTablePlacer>();
            var remoteDrag = remoteDragObject.AddComponent<RemoteTableDragController>();

            AssertTrue(placer.disableSpatialTablePlacementForNow, "Open-space table placement should be disabled until placement calibration is re-enabled intentionally.");
            AssertTrue(remoteDrag.enableRemoteDrag, "Manual remote table drag should stay available even while automatic spatial placement is disabled.");
            AssertTrue(!remoteDrag.disableRemoteTableDragForNow, "Manual remote table drag should not be tied to automatic table calibration.");
        }
        finally
        {
            if (existingGripState == null)
            {
                var generatedGripState = Object.FindObjectOfType<SimpleGripInteractionState>(true);
                if (generatedGripState != null)
                {
                    Object.DestroyImmediate(generatedGripState.gameObject);
                }
            }

            Object.DestroyImmediate(remoteDragObject);
            Object.DestroyImmediate(placerObject);
        }
    }

    private static void OpenSpacePlacementWaitsForRoomSensingColliders()
    {
        var placerObject = new GameObject("TableOpenSpacePlacer");
        var sensingRoot = new GameObject("MRSpaceSensing");
        var sensingCollider = new GameObject("RuntimeMeshCollider");
        try
        {
            var placer = placerObject.AddComponent<PingPongOpenSpaceTablePlacer>();
            placer.requireRoomSensingColliderForAutoPlacement = true;
            placer.minimumRoomSensingColliderCount = 1;

            AssertTrue(!placer.HasRequiredRoomSensingColliders(), "Open-space placement should wait when no room-sensing colliders are available.");

            sensingCollider.transform.SetParent(sensingRoot.transform, false);
            sensingCollider.AddComponent<BoxCollider>();
            placer.roomSensingRoot = sensingRoot.transform;

            AssertTrue(placer.HasRequiredRoomSensingColliders(), "Open-space placement should proceed once room-sensing colliders are available.");
        }
        finally
        {
            Object.DestroyImmediate(sensingCollider);
            Object.DestroyImmediate(sensingRoot);
            Object.DestroyImmediate(placerObject);
        }
    }

    private static void OpenSpacePlacementAvoidsTableObstacle()
    {
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            obstacle.name = "PingPongPlacementObstacle";
            obstacle.transform.position = new Vector3(0f, 0.55f, 81.5f);
            obstacle.transform.localScale = new Vector3(1.4f, 1.1f, 1.4f);

            var result = PicoElderCare.Rehab.OpenSpacePlacementSolver.FindBestPlacement(
                new Vector3(0f, 1.6f, 80f),
                Quaternion.identity,
                0f,
                1.5f,
                1.2f,
                2.4f,
                0.75f,
                1.2f,
                ~0);

            var obstacleHorizontal = new Vector2(obstacle.transform.position.x, obstacle.transform.position.z);
            var resultHorizontal = new Vector2(result.center.x, result.center.z);
            AssertTrue(result.foundClearSpace, "PingPong open-space placement should find a clear table candidate.");
            AssertTrue(Vector2.Distance(obstacleHorizontal, resultHorizontal) > 1.0f, "PingPong table placement should avoid the obstacle in front.");
        }
        finally
        {
            Object.DestroyImmediate(obstacle);
        }
    }

    private static void OpenSpaceTablePlacementMovesServeReferences()
    {
        var tableObject = new GameObject("Table");
        var handleObject = new GameObject("TableHandle");
        var spawnObject = new GameObject("SpawnPoint");
        var targetObject = new GameObject("TargetPoint");
        var placerObject = new GameObject("TableOpenSpacePlacer");
        try
        {
            tableObject.transform.position = new Vector3(0f, PingPongGeometry.TableCenter.y, 1.7f);
            spawnObject.transform.position = new Vector3(0f, 1.2f, 2.4f);
            targetObject.transform.position = new Vector3(0f, 0.9f, 0.6f);

            var dragHandle = handleObject.AddComponent<TableDragHandle>();
            dragHandle.tableRoot = tableObject.transform;
            dragHandle.syncedTransforms = new[] { spawnObject.transform, targetObject.transform };
            dragHandle.lockTableHeight = true;
            dragHandle.constrainToBounds = false;

            var placer = placerObject.AddComponent<PingPongOpenSpaceTablePlacer>();
            placer.tableRoot = tableObject.transform;
            placer.tableDragHandle = dragHandle;
            placer.controlServing = false;
            placer.tableCenterHeightAboveFloor = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness * 0.5f;
            placer.SetTableCenterOnFloor(new Vector3(1.1f, 0f, 2.2f), true);

            AssertTrue(Mathf.Abs(tableObject.transform.position.x - 1.1f) < 0.001f, "Manual remote table placement should move the table X position.");
            AssertTrue(Mathf.Abs(tableObject.transform.position.z - 2.2f) < 0.001f, "Manual remote table placement should move the table Z position.");
            AssertTrue(Mathf.Abs(spawnObject.transform.position.x - 1.1f) < 0.001f, "Manual remote table placement should sync the serve spawn point.");
            AssertTrue(Mathf.Abs(targetObject.transform.position.x - 1.1f) < 0.001f, "Manual remote table placement should sync the serve target point.");
        }
        finally
        {
            Object.DestroyImmediate(placerObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(spawnObject);
            Object.DestroyImmediate(handleObject);
            Object.DestroyImmediate(tableObject);
        }
    }

    private static void PlayerTableSafetyUsesTableFootprintOnly()
    {
        var table = new GameObject("Table");
        var safetyObject = new GameObject("TablePlayerBlocker");
        try
        {
            table.transform.position = PingPongGeometry.TableCenter;
            var safety = safetyObject.AddComponent<PingPongPlayerTableSafety>();
            safety.tableTransform = table.transform;
            safety.tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
            safety.safetyMargin = 0f;
            safety.hardMargin = 0f;
            safety.warningOnlyDistance = 0f;

            AssertTrue(
                safety.EvaluateHeadPosition(table.transform.position) == PingPongTableSafetyState.Blocked,
                "Safety boundary should block when the HMD is inside the table footprint.");

            var warningPoint = table.transform.position + Vector3.right * (PingPongGeometry.TableWidth * 0.5f - 0.01f);
            AssertTrue(
                safety.EvaluateHeadPosition(warningPoint) == PingPongTableSafetyState.Blocked,
                "Safety boundary should block inside the table footprint.");

            var clearPoint = table.transform.position + Vector3.right * (PingPongGeometry.TableWidth * 0.5f + 0.01f);
            AssertTrue(
                safety.EvaluateHeadPosition(clearPoint) == PingPongTableSafetyState.Clear,
                "Safety boundary should clear outside the table footprint.");
        }
        finally
        {
            Object.DestroyImmediate(safetyObject);
            Object.DestroyImmediate(table);
        }
    }

    private static void PlayerTableSafetyUsesHeadPositionOnly()
    {
        var table = new GameObject("Table");
        var hmd = new GameObject("Main Camera");
        var paddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var safetyObject = new GameObject("TablePlayerBlocker");
        try
        {
            table.transform.position = PingPongGeometry.TableCenter;
            hmd.transform.position = table.transform.position + Vector3.right * (PingPongGeometry.TableWidth * 0.5f + 1.2f);
            paddle.name = "Paddle_Right";
            paddle.transform.position = table.transform.position;

            var safety = safetyObject.AddComponent<PingPongPlayerTableSafety>();
            safety.tableTransform = table.transform;
            safety.hmdTransform = hmd.transform;
            safety.tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
            AssertTrue(!safety.moveTableWhenInside, "Safety boundary should not move the table by default.");

            AssertTrue(
                safety.EvaluateHeadPosition(hmd.transform.position) == PingPongTableSafetyState.Clear,
                "Safety boundary should use the HMD/body position, not a paddle that moves into the table area.");
        }
        finally
        {
            Object.DestroyImmediate(safetyObject);
            Object.DestroyImmediate(paddle);
            Object.DestroyImmediate(hmd);
            Object.DestroyImmediate(table);
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new System.Exception($"Missing private field {fieldName}.");
        }

        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new System.Exception($"Missing private method {methodName}.");
        }

        method.Invoke(target, null);
    }

    private static bool IsChildActive(Transform parent, string path)
    {
        var child = parent != null ? parent.Find(path) : null;
        return child != null && child.gameObject.activeSelf;
    }
}
