using PicoElderCare.Rehab;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RehabSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run Rehab Self Tests")]
    public static void RunAll()
    {
        TwoHandsAboveHeadPoseAccumulatesHold();
        LowHandFailsPose();
        UnevenHandsFailPose();
        HoldMustReachMinimumDuration();
        BaduanjinDefinitionsContainEightMovements();
        BaduanjinCorePosesCanComplete();
        LookBackRejectsFastTurnUntilRecentered();
        MovementTimeoutSkipsAndRecordsResult();
        FinalizeCurrentMovementRecordsPartialResult();
        TaiChiDefinitionsContainSixMovements();
        TaiChiCorePosesValidate();
        TaiChiClosingRequiresLoweringAfterMovementStart();
        MovementEvaluatorSwitchesDefaultDefinitionsByTrainingMode();
        SafetyMonitorPausesAndResumesWithHysteresis();
        TrainingResultSerializesToJson();
        OpenSpacePlacementAvoidsObstacleInFront();
        OpenSpacePlacementCanIgnoreExistingModuleObjects();
        TrainingAreaDragHandleIsDisabledByDefault();
        ManualTrainingAreaPlacementUpdatesSessionCenter();
        PromptPanelStaysOutsideTrainingCircle();
        RunComfortPlacementTests();
        RunPageLayoutTests();
        RunVisualSkinScopeTests();
        ComfortUiCreatesRayDragAndThumbstickHelpers();
        ComfortUiRayDragKeepsStableHeightWhenDraggedFar();
        HtmlStyleMainEntryPanelUsesVrReadableScale();
        ThumbstickNavigatorMovesLeftToLeftCard();
        VideoPanelLayoutIsDecoupledFromTrainingAreaByDefault();
        VideoPanelScalingClampsAndKeepsPanelUpright();
        SpatialRayControlCanPlaceTrainingAreaExplicitly();
        VideoSpatialControlsStayHiddenUntilVideoGuideShows();
        VideoGuidePauseKeepsDisplayVisible();
        SpatialRayControlDragsVideoOnlyWhileTriggerHeld();
        Debug.Log("Rehab self tests passed.");
    }

    public static void RunComfortPlacementTests()
    {
        ComfortUiUsesHmdRelativeHeight();
        ComfortUiClampsMinimumHeight();
        ComfortUiClampsMaximumHeight();
        ComfortUiPlacementUsesCurrentHeadYaw();
        ComfortUiStartupRecenterFollowsSettledHeadPose();
        ComfortUiStopsFollowingAfterStartupWindow();
        TrainingSelectPanelRecenterUsesConfiguredComfortPlacement();
        TrainingLayoutRecenterRemainsIndependentOfSelectionPlacement();
        Debug.Log("Rehab comfort placement tests passed.");
    }

    public static void RunPageLayoutTests()
    {
        SelectionPageHidesTrainingContent();
        StartingTrainingShowsTrainingContent();
        ReturningToSelectionHidesTrainingContent();
        ResultPageHidesTrainingContent();
        TrainingEnvironmentRecenterPreservesSessionProgress();
        TrainingEnvironmentRecenterUsesCurrentHmdPose();
        TrainingRecenterButtonUsesUnifiedPlacement();
        TrainingLayoutRecenterUsesYawOnly();
        ResultPanelRecenterUsesCurrentHmdPose();
        ResultPanelRecenterClampsMinimumHeight();
        ResultPanelRecenterClampsMaximumHeight();
        ResultPanelRecenterDoesNotMoveSelectionPanel();
        Debug.Log("Rehab page layout tests passed.");
    }

    public static void RunVisualSkinScopeTests()
    {
        var root = new GameObject("RehabVisualSkinScopeFixture");
        root.SetActive(false);

        var selectionPanel = new GameObject("RehabTrainingSelectPanel", typeof(RectTransform));
        var trainingPanel = new GameObject("RehabTrainingPanel", typeof(RectTransform));
        var resultPanel = new GameObject("TrainingResultPanel", typeof(RectTransform));
        var videoPanel = new GameObject("RehabVideoPanel");
        selectionPanel.transform.SetParent(root.transform, false);
        trainingPanel.transform.SetParent(root.transform, false);
        resultPanel.transform.SetParent(root.transform, false);
        videoPanel.transform.SetParent(root.transform, false);

        var selectionPosition = new Vector3(-1f, 2f, 3f);
        var videoPosition = new Vector3(4f, 5f, 6f);
        selectionPanel.transform.localPosition = selectionPosition;
        videoPanel.transform.localPosition = videoPosition;
        var selectionChildCount = selectionPanel.transform.childCount;
        var videoChildCount = videoPanel.transform.childCount;

        try
        {
            var modeUi = root.AddComponent<RehabModeSelectUI>();
            modeUi.applyHtmlStylePanels = false;
            modeUi.applyTrainingAndResultVisualSkin = true;
            modeUi.rehabTrainingSelectPanel = selectionPanel;
            modeUi.rehabTrainingPanel = trainingPanel;
            modeUi.trainingResultPanel = resultPanel;

            HtmlStyleRehabVisualSkin.ApplyTrainingAndResultPanels(modeUi);

            AssertTrue(trainingPanel.transform.Find("HtmlVisual_PanelRoot") != null, "Training-only skin should add the warm visual root to the original training panel.");
            AssertTrue(resultPanel.transform.Find("HtmlVisual_PanelRoot") != null, "Training-only skin should add the warm visual root to the original result panel.");
            AssertTrue(selectionPanel.transform.childCount == selectionChildCount, "Training/result-only skin must not add selection-page visuals.");
            AssertTrue(videoPanel.transform.childCount == videoChildCount, "Training/result-only skin must not add video-panel visuals.");
            AssertTrue(Vector3.Distance(selectionPanel.transform.localPosition, selectionPosition) < 0.0001f, "Training/result-only skin must not move the selection panel.");
            AssertTrue(Vector3.Distance(videoPanel.transform.localPosition, videoPosition) < 0.0001f, "Training/result-only skin must not move the video panel.");
            Debug.Log("Rehab visual skin scope tests passed.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void TwoHandsAboveHeadPoseAccumulatesHold()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            ConfigureSingleTwoHandsMovement(evaluator);
            evaluator.minimumHoldSeconds = 2f;
            evaluator.ResetEvaluation();

            var sample = CreateSample(1.6f, 1.82f, 1.84f);
            var result = evaluator.Evaluate(sample, 1f, false, 1f);
            AssertTrue(result.poseValid, "Two hands above the head with a small height difference should be valid.");
            AssertTrue(result.currentHoldSeconds > 0.99f && result.currentHoldSeconds < 1.01f, "Valid pose should accumulate hold time.");
            AssertTrue(!result.completed, "One second of hold should not complete a two second movement.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void LowHandFailsPose()
    {
        var sample = CreateSample(1.6f, 1.8f, 1.7f);
        var valid = MovementEvaluator.IsTwoHandsLiftHeavenPoseValid(sample, 0.15f, 0.18f);
        AssertTrue(!valid, "A hand below head plus threshold should fail the pose.");
    }

    private static void UnevenHandsFailPose()
    {
        var sample = CreateSample(1.6f, 1.86f, 2.08f);
        var valid = MovementEvaluator.IsTwoHandsLiftHeavenPoseValid(sample, 0.15f, 0.18f);
        AssertTrue(!valid, "Hands with a height difference over the threshold should fail the pose.");
    }

    private static void HoldMustReachMinimumDuration()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorDurationTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            ConfigureSingleTwoHandsMovement(evaluator);
            evaluator.minimumHoldSeconds = 2f;
            evaluator.maximumHoldSeconds = 5f;
            evaluator.ResetEvaluation();

            var sample = CreateSample(1.6f, 1.82f, 1.83f);
            var first = evaluator.Evaluate(sample, 1.5f, false, 1.5f);
            AssertTrue(!first.completed, "Hold shorter than the minimum duration should not complete.");

            var second = evaluator.Evaluate(sample, 0.5f, false, 2f);
            AssertTrue(second.targetReached, "Hold at the minimum duration should mark the target as reached.");
            AssertTrue(second.completion01 > 0.99f, "Reached target should report full current movement completion.");
            AssertTrue(!second.completed, "Reached target should not complete the movement before the timer expires.");

            evaluator.AdvanceCurrentStepByTimer(25f, 0);
            AssertTrue(evaluator.Completed, "The movement sequence should complete only after timer-driven advancement.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void BaduanjinDefinitionsContainEightMovements()
    {
        var definitions = BaduanjinEvaluator.CreateDefaultMovements();
        AssertTrue(definitions.Length == 8, "Baduanjin mode should define eight movements.");
        AssertTrue(definitions[0].movementId == RehabMovementId.Baduanjin_TwoHandsLiftHeaven, "The existing first movement should remain first.");
        AssertTrue(definitions[7].movementId == RehabMovementId.Baduanjin_HeelRaiseFinish, "The final movement should be the simplified heel-raise finish.");
    }

    private static void BaduanjinCorePosesCanComplete()
    {
        var evaluatorObject = new GameObject("BaduanjinEvaluatorPoseTest");
        try
        {
            var baduanjin = evaluatorObject.AddComponent<BaduanjinEvaluator>();
            var definitions = BaduanjinEvaluator.CreateDefaultMovements();

            AssertStepValid(baduanjin, definitions[0], 0, CreateSample(1.6f, 1.82f, 1.83f), "Two hands lift should validate.");
            AssertStepValid(baduanjin, definitions[1], 0, CreateSampleWithHands(1.6f, new Vector3(-0.52f, 1.2f, 0.15f), new Vector3(0.08f, 1.2f, 0.15f)), "Left draw-bow should validate.");
            AssertStepValid(baduanjin, definitions[2], 0, CreateSampleWithHands(1.6f, new Vector3(-0.2f, 1.78f, 0.2f), new Vector3(0.2f, 1.15f, 0.2f)), "Single raise should validate.");
            AssertStepValid(baduanjin, definitions[3], 0, CreateSampleWithHeadYaw(1.6f, -28f), "Gentle left look-back should validate.");
            AssertStepValid(baduanjin, definitions[4], 0, CreateSampleWithHeadPosition(new Vector3(-0.16f, 1.6f, 0f)), "Gentle left sway should validate.");
            AssertStepValid(baduanjin, definitions[5], 0, CreateSample(1.6f, 0.82f, 0.83f), "Simplified reach-down should validate.");
            AssertStepValid(baduanjin, definitions[6], 0, CreateSampleWithHands(1.6f, new Vector3(-0.18f, 1.18f, 0.48f), new Vector3(0.18f, 1.18f, 0.48f)), "Gentle punch should validate.");
            AssertStepValid(baduanjin, definitions[7], 0, CreateSampleWithHeadPosition(new Vector3(0f, 1.66f, 0f)), "Heel raise or seated finish should validate.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void MovementTimeoutSkipsAndRecordsResult()
    {
        var evaluatorObject = new GameObject("MovementTimeoutTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.autoCreateDefaultBaduanjinDefinitions = false;
            evaluator.movementDefinitions = new[]
            {
                new MovementDefinition(
                    RehabMovementId.Baduanjin_TouchKneesStrengthenKidneys,
                    "两手攀足固肾腰",
                    "timeout test",
                    new MovementStepDefinition("下探", "双手下探", 1f, 0.5f))
            };

            evaluator.ResetEvaluation();
            var invalidPose = CreateSample(1.6f, 1.45f, 1.45f);
            var result = evaluator.Evaluate(invalidPose, 1.1f, false, 1.1f, 2);
            AssertTrue(!result.stepTimedOut, "Evaluator should not time out movement flow by itself.");
            AssertTrue(!result.completed, "A movement should not complete until the session advances it by timer.");
            AssertTrue(evaluator.MovementResults.Count == 0, "Evaluation should not record until timer advancement.");

            evaluator.AdvanceCurrentStepByTimer(1.1f, 2);
            AssertTrue(evaluator.Completed, "A one-step movement sequence should complete after timer advancement.");
            AssertTrue(evaluator.MovementResults.Count == 1, "Timeout should still record the movement result.");
            AssertTrue(evaluator.MovementResults[0].skippedByTimeout, "Movement result should flag timeout skip.");
            AssertTrue(evaluator.MovementResults[0].completion < 0.01f, "Timed-out movement completion should stay at zero.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void LookBackRejectsFastTurnUntilRecentered()
    {
        var evaluatorObject = new GameObject("LookBackSpeedTest");
        try
        {
            var baduanjin = evaluatorObject.AddComponent<BaduanjinEvaluator>();
            var movement = BaduanjinEvaluator.CreateDefaultMovements()[3];
            var baseline = CreateSampleWithHeadYaw(1.6f, 0f);
            var leftTurn = CreateSampleWithHeadYaw(1.6f, -28f);

            baduanjin.ResetForMovement(movement.movementId, baseline);
            var fastTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 0.1f);
            AssertTrue(!fastTurn.poseValid, "Fast look-back turn should be rejected.");

            var heldAfterFastTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 1f);
            AssertTrue(!heldAfterFastTurn.poseValid, "Holding after a fast turn should stay rejected until recentered.");

            baduanjin.EvaluateStep(movement, 0, baseline, 1f);
            var slowTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 1f);
            AssertTrue(slowTurn.poseValid, "Slow look-back turn should validate after recentering.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void FinalizeCurrentMovementRecordsPartialResult()
    {
        var evaluatorObject = new GameObject("MovementFinalizePartialTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.autoCreateDefaultBaduanjinDefinitions = false;
            evaluator.movementDefinitions = new[]
            {
                new MovementDefinition(
                    RehabMovementId.Baduanjin_DrawBowShootHawk,
                    "左右开弓似射雕",
                    "partial test",
                    new MovementStepDefinition("向左开弓", "左手向左侧打开", 0.5f, 10f),
                    new MovementStepDefinition("向右开弓", "右手向右侧打开", 0.5f, 10f))
            };

            evaluator.ResetEvaluation();
            var leftBow = CreateSampleWithHands(1.6f, new Vector3(-0.52f, 1.2f, 0.15f), new Vector3(0.08f, 1.2f, 0.15f));
            var firstStep = evaluator.Evaluate(leftBow, 0.5f, false, 0.5f, 1);
            AssertTrue(firstStep.stepIndex == 1, "Completing the first step should advance to the next step without ending the movement.");
            AssertTrue(firstStep.completion01 > 0.49f && firstStep.completion01 < 0.51f, "First step completion should report half of a two-step movement.");
            evaluator.FinalizeCurrentMovement(0.8f, 2);

            AssertTrue(evaluator.MovementResults.Count == 1, "Finalizing should record the in-progress movement.");
            AssertTrue(evaluator.MovementResults[0].completion > 0.49f && evaluator.MovementResults[0].completion < 0.51f, "Partial result should preserve completed step ratio.");
            AssertTrue(evaluator.MovementResults[0].safetyWarningCount == 1, "Partial result should include warnings since movement start.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void TaiChiDefinitionsContainSixMovements()
    {
        var definitions = TaiChiEvaluator.CreateDefaultMovements();
        AssertTrue(definitions.Length == 6, "TaiChiTraining should define six base movements.");
        AssertTrue(definitions[0].movementId == RehabMovementId.Taiji_Opening, "TaiChiTraining should start with opening.");
        AssertTrue(definitions[5].movementId == RehabMovementId.Taiji_Closing, "TaiChiTraining should end with closing.");
    }

    private static void TaiChiCorePosesValidate()
    {
        var evaluatorObject = new GameObject("TaiChiEvaluatorPoseTest");
        try
        {
            var taiChi = evaluatorObject.AddComponent<TaiChiEvaluator>();
            var definitions = TaiChiEvaluator.CreateDefaultMovements();
            var baseline = CreateTaiChiSample(
                new Vector3(-0.2f, -0.45f, 0.25f),
                new Vector3(0.2f, -0.45f, 0.25f));

            AssertTaiChiStepValid(taiChi, definitions[0], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.18f, 0.28f), new Vector3(0.2f, -0.18f, 0.28f)), "Opening raise should validate.");
            AssertTaiChiStepValid(taiChi, definitions[0], 1, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.40f, 0.28f), new Vector3(0.2f, -0.40f, 0.28f)), "Opening lower should validate.");
            AssertTaiChiStepValid(taiChi, definitions[1], 0, baseline, CreateTaiChiSample(new Vector3(-0.35f, -0.22f, 0.35f), new Vector3(-0.25f, -0.24f, 0.35f)), "Cloud hands left should validate.");
            AssertTaiChiStepValid(taiChi, definitions[1], 1, baseline, CreateTaiChiSample(new Vector3(0.25f, -0.22f, 0.35f), new Vector3(0.35f, -0.24f, 0.35f)), "Cloud hands right should validate.");
            AssertTaiChiStepValid(taiChi, definitions[2], 0, baseline, CreateTaiChiSample(new Vector3(-0.18f, -0.24f, 0.42f), new Vector3(0.18f, -0.28f, -0.25f)), "Wild horse left-forward should validate.");
            AssertTaiChiStepValid(taiChi, definitions[3], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, 0.24f, 0.2f), new Vector3(0.2f, -0.42f, 0.2f)), "White crane should validate.");
            AssertTaiChiStepValid(taiChi, definitions[4], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.45f, 0.15f), new Vector3(0.2f, -0.24f, 0.42f)), "Brush knee should validate.");
            AssertTaiChiStepValid(
                taiChi,
                definitions[5],
                0,
                CreateTaiChiSample(new Vector3(-0.2f, -0.18f, 0.22f), new Vector3(0.2f, -0.18f, 0.22f)),
                CreateTaiChiSample(new Vector3(-0.2f, -0.36f, 0.22f), new Vector3(0.2f, -0.36f, 0.22f)),
                "Closing should validate after the hands lower from the starting pose.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void TaiChiClosingRequiresLoweringAfterMovementStart()
    {
        var evaluatorObject = new GameObject("TaiChiClosingRegressionTest");
        try
        {
            var taiChi = evaluatorObject.AddComponent<TaiChiEvaluator>();
            var closing = TaiChiEvaluator.CreateDefaultMovements()[5];
            var brushKneeEnd = CreateTaiChiSample(
                new Vector3(-0.2f, -0.45f, 0.15f),
                new Vector3(0.2f, -0.24f, 0.42f));

            taiChi.ResetForMovement(closing.movementId, brushKneeEnd);
            var unchanged = taiChi.EvaluateStep(closing, 0, brushKneeEnd, 1f);
            AssertTrue(!unchanged.poseValid, "Closing should not validate just because the previous pose already leaves both hands low.");

            var lowered = CreateTaiChiSample(
                new Vector3(-0.2f, -0.52f, 0.18f),
                new Vector3(0.2f, -0.42f, 0.25f));
            var afterLowering = taiChi.EvaluateStep(closing, 0, lowered, 1f);
            AssertTrue(afterLowering.poseValid, "Closing should validate after the hands visibly lower from the movement start pose.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void MovementEvaluatorSwitchesDefaultDefinitionsByTrainingMode()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorTrainingModeTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.trainingMode = RehabTrainingMode.TaiChiTraining;
            evaluator.ResetEvaluation();

            AssertTrue(evaluator.CurrentMovement != null && evaluator.CurrentMovement.movementId == RehabMovementId.Taiji_Opening, "TaiChiTraining mode should auto-load TaiChi default definitions.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void SafetyMonitorPausesAndResumesWithHysteresis()
    {
        var monitorObject = new GameObject("SafetyMonitorTest");
        try
        {
            var monitor = monitorObject.AddComponent<SafetyMonitor>();
            monitor.pauseDistanceMeters = 1.2f;
            monitor.resumeDistanceMeters = 1.1f;
            monitor.ResetMonitor();

            var center = Vector3.zero;
            var paused = monitor.Evaluate(new Vector3(1.21f, 1.6f, 0f), center, true);
            AssertTrue(paused.isPaused, "Head distance over pause threshold should pause the session.");
            AssertTrue(paused.pauseCount == 1, "First safety pause should increment pause count.");

            var stillPaused = monitor.Evaluate(new Vector3(1.15f, 1.6f, 0f), center, true);
            AssertTrue(stillPaused.isPaused, "Head distance between pause and resume thresholds should stay paused.");

            var resumed = monitor.Evaluate(new Vector3(1.0f, 1.6f, 0f), center, true);
            AssertTrue(!resumed.isPaused, "Head distance under resume threshold should resume the session.");
        }
        finally
        {
            Object.DestroyImmediate(monitorObject);
        }
    }

    private static void TrainingResultSerializesToJson()
    {
        var result = RehabTrainingResult.CreateStarted(
            RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
            "八段锦：双手托天理三焦",
            300f);
        result.Finish(RehabSessionEndReason.Completed, true, 2.1f, 2.1f, 2.1f, 1, 1.25f);

        var json = JsonUtility.ToJson(result, true);
        AssertTrue(json.Contains("sessionId"), "Serialized result should include sessionId.");
        AssertTrue(json.Contains("movementId"), "Serialized result should include movementId.");
        AssertTrue(json.Contains("maxHeadDistanceFromCenterMeters"), "Serialized result should include max head distance.");
    }

    private static void OpenSpacePlacementAvoidsObstacleInFront()
    {
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            obstacle.name = "OpenSpacePlacementObstacle";
            obstacle.transform.position = new Vector3(0f, 0.55f, 51.5f);
            obstacle.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);

            var result = OpenSpacePlacementSolver.FindBestPlacement(
                new Vector3(0f, 1.6f, 50f),
                Quaternion.identity,
                0f,
                1.5f,
                1.2f,
                2.2f,
                0.65f,
                1.7f,
                ~0);

            var obstacleHorizontal = new Vector2(obstacle.transform.position.x, obstacle.transform.position.z);
            var resultHorizontal = new Vector2(result.center.x, result.center.z);
            AssertTrue(result.foundClearSpace, "Open-space solver should find an alternate clear candidate.");
            AssertTrue(Vector2.Distance(obstacleHorizontal, resultHorizontal) > 0.9f, "Open-space solver should not place the center inside the obstacle in front.");
        }
        finally
        {
            Object.DestroyImmediate(obstacle);
        }
    }

    private static void OpenSpacePlacementCanIgnoreExistingModuleObjects()
    {
        var existingModuleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            existingModuleObject.name = "ExistingModuleObject";
            existingModuleObject.transform.position = new Vector3(0f, 0.55f, 61.5f);
            existingModuleObject.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);

            var result = OpenSpacePlacementSolver.FindBestPlacement(
                new Vector3(0f, 1.6f, 60f),
                Quaternion.identity,
                0f,
                1.5f,
                1.5f,
                1.5f,
                0.65f,
                1.7f,
                ~0,
                new[] { existingModuleObject.transform });

            AssertTrue(result.foundClearSpace, "Open-space solver should ignore existing module objects when requested.");
            AssertTrue(Vector3.Distance(result.center, new Vector3(0f, 0f, 61.5f)) < 0.05f, "Ignored module objects should not force a different placement.");
        }
        finally
        {
            Object.DestroyImmediate(existingModuleObject);
        }
    }

    private static void ManualTrainingAreaPlacementUpdatesSessionCenter()
    {
        var sessionObject = new GameObject("RehabSessionManualPlacementTest");
        var areaObject = new GameObject("TrainingArea");
        var promptObject = new GameObject("PromptCanvas");
        try
        {
            var session = sessionObject.AddComponent<RehabSessionManager>();
            session.trainingAreaRoot = areaObject.transform;
            session.promptCanvas = promptObject.transform;
            session.trainingFloorY = 0f;
            session.promptHeightMeters = 1.65f;
            session.promptForwardOffsetMeters = 0.85f;

            var requestedCenter = new Vector3(1.2f, 2f, 2.4f);
            session.SetTrainingAreaCenter(requestedCenter, Vector3.forward, new Vector3(0f, 1.6f, 0f));

            AssertTrue(Vector3.Distance(session.TrainingCenter, new Vector3(1.2f, 0f, 2.4f)) < 0.001f, "Manual placement should update the safety/evaluation training center.");
            AssertTrue(Vector3.Distance(areaObject.transform.position, session.TrainingCenter) < 0.001f, "Manual placement should move the training area root.");
            AssertTrue(Mathf.Abs(promptObject.transform.position.y - 1.65f) < 0.001f, "Manual placement should keep the prompt at the configured height.");
        }
        finally
        {
            Object.DestroyImmediate(promptObject);
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(sessionObject);
        }
    }

    private static void TrainingAreaDragHandleIsDisabledByDefault()
    {
        var handleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            handleObject.name = "TrainingAreaDragHandle";
            var handle = handleObject.AddComponent<RehabTrainingAreaDragHandle>();
            handle.allowUserPlacementDrag = true;
            handle.ApplyInteractionState();

            AssertTrue(!handle.allowUserPlacementDrag, "Legacy training-area drag should stay disabled even if an old scene serialized it as enabled.");
            AssertTrue(!handleObject.GetComponent<Renderer>().enabled, "Disabled training-area drag handle should hide its visible affordance.");
            AssertTrue(!handleObject.GetComponent<Collider>().enabled, "Disabled training-area drag handle should disable its collider.");
        }
        finally
        {
            Object.DestroyImmediate(handleObject);
        }
    }

    private static void PromptPanelStaysOutsideTrainingCircle()
    {
        var sessionObject = new GameObject("RehabPromptPlacementTest");
        var areaObject = new GameObject("TrainingArea");
        var promptObject = new GameObject("PromptCanvas");
        try
        {
            var session = sessionObject.AddComponent<RehabSessionManager>();
            session.trainingAreaRoot = areaObject.transform;
            session.promptCanvas = promptObject.transform;
            session.trainingFloorY = 0f;
            session.promptHeightMeters = 1.65f;
            session.promptForwardOffsetMeters = 0.85f;

            var center = new Vector3(0f, 0f, 1.5f);
            session.SetTrainingAreaCenter(center, Vector3.forward, new Vector3(0f, 1.6f, 0f));

            var horizontalOffset = new Vector2(
                promptObject.transform.position.x - center.x,
                promptObject.transform.position.z - center.z).magnitude;

            AssertTrue(horizontalOffset > 0.8f, "Prompt panel should sit outside the training circle center.");
            AssertTrue(promptObject.transform.position.z > center.z, "Prompt panel should sit in front of the training circle.");
            AssertTrue(Mathf.Abs(promptObject.transform.position.y - 1.65f) < 0.001f, "Prompt panel should use eye-level height.");
        }
        finally
        {
            Object.DestroyImmediate(promptObject);
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(sessionObject);
        }
    }

    private static void ComfortUiUsesHmdRelativeHeight()
    {
        AssertComfortUiHeight(1.65f, 1.55f, "Comfort UI should use HMD height plus the configured offset.");
    }

    private static void ComfortUiClampsMinimumHeight()
    {
        AssertComfortUiHeight(1.2f, 1.25f, "Comfort UI should clamp low HMD-relative placement to the minimum world height.");
    }

    private static void ComfortUiClampsMaximumHeight()
    {
        AssertComfortUiHeight(1.95f, 1.75f, "Comfort UI should clamp high HMD-relative placement to the maximum world height.");
    }

    private static void ComfortUiPlacementUsesCurrentHeadYaw()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUi");
        try
        {
            headObject.transform.position = new Vector3(1f, 1.6f, 2f);
            headObject.transform.rotation = Quaternion.Euler(-30f, 45f, 0f);

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = uiObject.transform;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.PlaceInFrontOfUser();

            var expectedForward = Quaternion.Euler(0f, 45f, 0f) * Vector3.forward;
            var expectedPosition = headObject.transform.position + expectedForward * 2f;
            expectedPosition.y = placer.preferredWorldHeight;

            AssertTrue(Vector3.Distance(uiObject.transform.position, expectedPosition) < 0.001f, "Comfort UI should place from yaw only even when the HMD has pitch.");
            AssertTrue(Quaternion.Angle(uiObject.transform.rotation, Quaternion.LookRotation(expectedForward, Vector3.up)) < 0.1f, "Comfort UI rotation should preserve yaw without HMD pitch or roll.");
            AssertTrue(Vector3.Dot(uiObject.transform.up, Vector3.up) > 0.999f, "Comfort UI should remain upright when the user looks up or down.");
        }
        finally
        {
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void ComfortUiStartupRecenterFollowsSettledHeadPose()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUiStartup");
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = uiObject.transform;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.startupRecenterSeconds = 0f;
            placer.startupRecenterFrames = 1;
            placer.PlaceInFrontOfUser();

            headObject.transform.position = new Vector3(0.25f, 1.7f, 0.1f);
            headObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            placer.BeginStartupRecenterWindow();
            placer.RefreshStartupPlacementIfNeeded();

            var expectedPosition = new Vector3(-1.75f, 1.6f, 0.1f);
            AssertTrue(Vector3.Distance(uiObject.transform.position, expectedPosition) < 0.001f, "Startup recenter should update position and yaw after the XR head pose settles.");
            AssertTrue(Quaternion.Angle(uiObject.transform.rotation, Quaternion.LookRotation(Vector3.left, Vector3.up)) < 0.1f, "Startup recenter should refresh the horizontal menu rotation.");
        }
        finally
        {
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void ComfortUiStopsFollowingAfterStartupWindow()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUiStartupFreeze");
        try
        {
            headObject.transform.position = new Vector3(0f, 1.65f, 0f);
            headObject.transform.rotation = Quaternion.identity;

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = uiObject.transform;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.comfortFollowEnabled = false;
            placer.startupRecenterSeconds = 0f;
            placer.startupRecenterFrames = 1;

            placer.BeginStartupRecenterWindow();
            placer.RefreshStartupPlacementIfNeeded();
            var frozenPosition = uiObject.transform.position;
            var frozenRotation = uiObject.transform.rotation;

            headObject.transform.position = new Vector3(1f, 1.8f, -0.5f);
            headObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            placer.RefreshStartupPlacementIfNeeded();

            AssertTrue(Vector3.Distance(uiObject.transform.position, frozenPosition) < 0.001f, "Comfort UI should stop following position after the startup window ends.");
            AssertTrue(Quaternion.Angle(uiObject.transform.rotation, frozenRotation) < 0.1f, "Comfort UI should stop following yaw after the startup window ends.");
        }
        finally
        {
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void TrainingLayoutRecenterRemainsIndependentOfSelectionPlacement()
    {
        var existingMainCamera = Camera.main;
        var cameraObject = existingMainCamera == null ? new GameObject("Main Camera", typeof(Camera)) : null;
        var viewTransform = existingMainCamera != null ? existingMainCamera.transform : cameraObject.transform;
        var originalPosition = viewTransform.position;
        var originalRotation = viewTransform.rotation;
        var anchorObject = new GameObject("TrainingLayoutAnchorTest");
        var functionPanelObject = new GameObject("TrainingFunctionPanel");
        var videoPanelObject = new GameObject("VideoPanel");
        var selectionPanelObject = new GameObject("SelectionPanel");
        var controllerObject = new GameObject("RehabPanelPlacementControllerTest");
        try
        {
            if (cameraObject != null)
            {
                cameraObject.tag = "MainCamera";
            }

            viewTransform.position = new Vector3(1f, 1.7f, 2f);
            viewTransform.rotation = Quaternion.Euler(-20f, 90f, 0f);
            functionPanelObject.transform.SetParent(anchorObject.transform, false);
            videoPanelObject.transform.SetParent(anchorObject.transform, false);
            functionPanelObject.transform.localPosition = new Vector3(-0.4f, 0.1f, 0f);
            videoPanelObject.transform.localPosition = new Vector3(0.5f, 0.2f, 0.15f);
            selectionPanelObject.transform.position = new Vector3(-3f, 1.4f, -2f);

            var originalFunctionLocalPosition = functionPanelObject.transform.localPosition;
            var originalVideoLocalPosition = videoPanelObject.transform.localPosition;
            var originalSelectionPosition = selectionPanelObject.transform.position;
            var videoLayout = videoPanelObject.AddComponent<RehabVideoPanelLayoutController>();
            videoLayout.panelRoot = videoPanelObject.transform;

            var controller = controllerObject.AddComponent<RehabPanelPlacementController>();
            controller.headTransform = viewTransform;
            controller.viewTransform = viewTransform;
            controller.selectionPanelRoot = selectionPanelObject.transform;
            controller.trainingLayoutAnchor = anchorObject.transform;
            controller.trainingFunctionPanelRoot = functionPanelObject.transform;
            controller.videoPanelRoot = videoPanelObject.transform;
            controller.videoLayoutController = videoLayout;
            controller.trainingLayoutDistance = 1.8f;
            controller.trainingLayoutHeightOffset = -0.1f;

            controller.RecenterTrainingLayout();

            var expectedAnchorPosition = new Vector3(2.8f, 1.6f, 2f);
            AssertTrue(Vector3.Distance(anchorObject.transform.position, expectedAnchorPosition) < 0.001f, "Training layout recenter should continue to update TrainingLayoutAnchor from the HMD yaw.");
            AssertTrue(Quaternion.Angle(anchorObject.transform.rotation, Quaternion.LookRotation(Vector3.right, Vector3.up)) < 0.1f, "Training layout should remain upright and face the HMD yaw.");
            AssertTrue(Vector3.Distance(functionPanelObject.transform.localPosition, originalFunctionLocalPosition) < 0.001f, "Training function panel should keep its authored offset under TrainingLayoutAnchor.");
            AssertTrue(Vector3.Distance(videoPanelObject.transform.localPosition, originalVideoLocalPosition) < 0.001f, "Video panel should keep its authored offset under TrainingLayoutAnchor.");
            AssertTrue(Vector3.Distance(selectionPanelObject.transform.position, originalSelectionPosition) < 0.001f, "Training layout recenter should not move the selection menu.");
        }
        finally
        {
            viewTransform.position = originalPosition;
            viewTransform.rotation = originalRotation;
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(selectionPanelObject);
            Object.DestroyImmediate(videoPanelObject);
            Object.DestroyImmediate(functionPanelObject);
            Object.DestroyImmediate(anchorObject);
            if (cameraObject != null)
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }

    private static void SelectionPageHidesTrainingContent()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.modeUi.ShowTrainingSelectPanel();

            AssertTrue(fixture.selectionRoot.activeSelf, "Selection page should show SelectionPanelRoot.");
            AssertTrue(!fixture.trainingArea.activeSelf, "Selection page should hide the training area.");
            AssertTrue(!fixture.trainingRoot.activeSelf, "Selection page should hide the training function panel.");
            AssertTrue(!fixture.videoPanel.activeSelf, "Selection page should hide the rehab video panel.");
            AssertTrue(!fixture.resultRoot.activeSelf, "Selection page should hide ResultPanelRoot.");
            AssertTrue(!fixture.coachRoot.activeSelf, "Selection page should hide the virtual coach.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void StartingTrainingShowsTrainingContent()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.modeUi.StartBaduanjinTraining();

            AssertTrue(fixture.session.IsSessionActive, "Starting a rehab mode should begin the session.");
            AssertTrue(fixture.trainingArea.activeSelf, "Starting training should show the training area.");
            AssertTrue(fixture.trainingRoot.activeSelf, "Starting training should show the training function panel.");
            AssertTrue(fixture.videoPanel.activeSelf, "Starting training should show the rehab video panel shell.");
            AssertTrue(fixture.coachRoot.activeSelf, "Starting training should show the virtual coach.");
            AssertTrue(!fixture.selectionRoot.activeSelf && !fixture.resultRoot.activeSelf, "Training should hide selection and result pages.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void ReturningToSelectionHidesTrainingContent()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.modeUi.StartBaduanjinTraining();
            fixture.modeUi.ShowTrainingSelectPanel();

            AssertTrue(!fixture.session.IsSessionActive, "Returning to selection should cancel the active session.");
            AssertTrue(!fixture.trainingArea.activeSelf, "Returning to selection should hide the training area.");
            AssertTrue(!fixture.trainingRoot.activeSelf && !fixture.videoPanel.activeSelf, "Returning to selection should hide training UI and video.");
            AssertTrue(!fixture.coachRoot.activeSelf, "Returning to selection should hide the virtual coach.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void ResultPageHidesTrainingContent()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.modeUi.StartBaduanjinTraining();
            fixture.modeUi.ShowTrainingResultPanel();

            AssertTrue(fixture.resultRoot.activeSelf, "Result page should show ResultPanelRoot.");
            AssertTrue(!fixture.trainingArea.activeSelf, "Result page should hide the training area.");
            AssertTrue(!fixture.trainingRoot.activeSelf && !fixture.videoPanel.activeSelf, "Result page should hide training UI and video.");
            AssertTrue(!fixture.coachRoot.activeSelf, "Result page should hide the virtual coach.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void TrainingEnvironmentRecenterPreservesSessionProgress()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.session.StartTraining(RehabTrainingType.Baduanjin);
            var sample = CreateSample(1.6f, 1.9f, 1.9f);
            var before = fixture.evaluator.Evaluate(sample, 0.5f, false, 0.5f);
            var movementBefore = fixture.evaluator.CurrentMovement;

            fixture.modeUi.RecenterTrainingEnvironment();
            var after = fixture.evaluator.Evaluate(sample, 0f, false, 0.5f);

            AssertTrue(fixture.session.IsSessionActive, "Recenter should not stop or restart the current session.");
            AssertTrue(fixture.evaluator.CurrentMovement == movementBefore, "Recenter should preserve the current movement.");
            AssertTrue(after.currentHoldSeconds >= before.currentHoldSeconds - 0.001f, "Recenter should preserve movement evaluation progress.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void TrainingEnvironmentRecenterUsesCurrentHmdPose()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.head.position = new Vector3(1f, 1.7f, -2f);
            fixture.head.rotation = Quaternion.Euler(-25f, 90f, 0f);
            fixture.modeUi.RecenterTrainingEnvironment();

            AssertTrue(Vector3.Distance(fixture.trainingArea.transform.position, new Vector3(1f, 0f, -2f)) < 0.001f, "Training recenter should move the training center below the current HMD position.");
            AssertTrue(Vector3.Distance(fixture.trainingLayoutAnchor.position, new Vector3(2.8f, 1.6f, -2f)) < 0.001f, "Training recenter should move TrainingLayoutAnchor from the current HMD yaw.");
            AssertTrue(Mathf.Abs(fixture.coachRoot.transform.position.x - 3f) < 0.001f, "Training recenter should move the coach in front of the current HMD yaw.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void TrainingLayoutRecenterUsesYawOnly()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.head.rotation = Quaternion.Euler(-30f, 45f, 18f);
            fixture.panelPlacement.RecenterTrainingLayout();

            var expectedForward = Quaternion.Euler(0f, 45f, 0f) * Vector3.forward;
            AssertTrue(Quaternion.Angle(fixture.trainingLayoutAnchor.rotation, Quaternion.LookRotation(expectedForward, Vector3.up)) < 0.1f, "Training layout rotation should use HMD yaw only.");
            AssertTrue(Vector3.Dot(fixture.trainingLayoutAnchor.up, Vector3.up) > 0.999f, "Training layout should remain upright when HMD pitch or roll changes.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void TrainingRecenterButtonUsesUnifiedPlacement()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.modeUi.StartBaduanjinTraining();
            fixture.head.position = new Vector3(-1f, 1.7f, 2f);
            fixture.head.rotation = Quaternion.Euler(20f, -90f, 0f);

            AssertTrue(fixture.modeUi.trainingRecenterButton != null, "Training page should expose the RecenterButton route.");
            fixture.modeUi.trainingRecenterButton.onClick.Invoke();

            AssertTrue(Vector3.Distance(fixture.trainingArea.transform.position, new Vector3(-1f, 0f, 2f)) < 0.001f, "RecenterButton should update the training center.");
            AssertTrue(Vector3.Distance(fixture.trainingLayoutAnchor.position, new Vector3(-2.8f, 1.6f, 2f)) < 0.001f, "RecenterButton should update TrainingLayoutAnchor.");
            AssertTrue(Mathf.Abs(fixture.coachRoot.transform.position.x + 3f) < 0.001f, "RecenterButton should update the virtual coach through the unified route.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void ResultPanelRecenterUsesCurrentHmdPose()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.head.position = new Vector3(0.5f, 1.65f, -0.25f);
            fixture.head.rotation = Quaternion.Euler(-30f, 45f, 12f);
            fixture.panelPlacement.RecenterResultPanel();

            var forward = Quaternion.Euler(0f, 45f, 0f) * Vector3.forward;
            var expected = fixture.head.position + forward * 2f;
            expected.y = 1.55f;
            AssertTrue(Vector3.Distance(fixture.resultRoot.transform.position, expected) < 0.001f, "Result panel should use current HMD position, yaw, distance and height offset.");
            AssertTrue(Quaternion.Angle(fixture.resultRoot.transform.rotation, Quaternion.LookRotation(forward, Vector3.up)) < 0.1f, "Result panel should remain upright and use HMD yaw only.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void ResultPanelRecenterClampsMinimumHeight()
    {
        AssertResultPanelHeight(1.2f, 1.25f, "Result panel should clamp low HMD-relative height.");
    }

    private static void ResultPanelRecenterClampsMaximumHeight()
    {
        AssertResultPanelHeight(1.95f, 1.75f, "Result panel should clamp high HMD-relative height.");
    }

    private static void ResultPanelRecenterDoesNotMoveSelectionPanel()
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.selectionRoot.transform.position = new Vector3(-3f, 1.4f, -2f);
            var selectionPosition = fixture.selectionRoot.transform.position;
            fixture.head.position = new Vector3(0.5f, 1.65f, 0.25f);
            fixture.modeUi.ShowTrainingResultPanel();

            AssertTrue(Vector3.Distance(fixture.selectionRoot.transform.position, selectionPosition) < 0.001f, "Result recenter should not move SelectionPanelRoot.");
            AssertTrue(Vector3.Distance(fixture.resultRoot.transform.position, new Vector3(0.5f, 1.55f, 2.25f)) < 0.001f, "Showing results should recenter ResultPanelRoot from the current HMD pose.");
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void AssertResultPanelHeight(float headHeight, float expectedHeight, string message)
    {
        var fixture = new RehabPageLayoutFixture();
        try
        {
            fixture.head.position = new Vector3(0f, headHeight, 0f);
            fixture.panelPlacement.RecenterResultPanel();
            AssertTrue(Mathf.Abs(fixture.resultRoot.transform.position.y - expectedHeight) < 0.001f, message);
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private static void AssertComfortUiHeight(float headHeight, float expectedHeight, string message)
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUiHeight");
        try
        {
            headObject.transform.position = new Vector3(0f, headHeight, 0f);
            headObject.transform.rotation = Quaternion.identity;

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = uiObject.transform;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.clampWorldHeight = true;
            placer.minWorldHeight = 1.25f;
            placer.maxWorldHeight = 1.75f;
            placer.PlaceInFrontOfUser();

            AssertTrue(Mathf.Abs(uiObject.transform.position.y - expectedHeight) < 0.001f, message);
        }
        finally
        {
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void ComfortUiCreatesRayDragAndThumbstickHelpers()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUiHelpers", typeof(RectTransform));
        try
        {
            var rect = uiObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 500f);
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = rect;
            placer.enableRayDrag = true;
            placer.enableThumbstickNavigation = true;
            placer.EnsureWorldSpaceInteractionHelpers();

            var handle = rect.Find("RayDragHandle");
            AssertTrue(handle != null, "Comfort UI should create a ray-drag handle for world-space manipulation.");
            AssertTrue(handle.GetComponent<WorldSpaceUiRayDragHandle>() != null, "Ray-drag handle should own the drag behavior.");
            AssertTrue(uiObject.GetComponent<WorldSpaceUiThumbstickNavigator>() != null, "Comfort UI should install the corrected thumbstick navigator.");
        }
        finally
        {
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void ComfortUiRayDragKeepsStableHeightWhenDraggedFar()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("ComfortUiStableHeight", typeof(RectTransform));
        var handleObject = new GameObject("RayDragHandle", typeof(RectTransform), typeof(Image), typeof(WorldSpaceUiRayDragHandle));
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;
            uiObject.transform.position = new Vector3(0f, 1.5f, 2f);
            uiObject.transform.rotation = Quaternion.identity;

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = uiObject.transform;
            placer.hmdHeightOffsetMeters = -0.1f;

            handleObject.transform.SetParent(uiObject.transform, false);
            var handle = handleObject.GetComponent<WorldSpaceUiRayDragHandle>();
            handle.placer = placer;
            handle.targetRoot = uiObject.transform;
            handle.headTransform = headObject.transform;
            handle.lockHeightToComfortOffset = true;
            handle.lockedHeightToleranceMeters = 0.08f;

            handle.MoveTargetToWorldPoint(new Vector3(0f, 3.4f, 8f));

            AssertTrue(uiObject.transform.position.z <= headObject.transform.position.z + handle.maxDistanceMeters + 0.001f, "Far ray drag should still respect the maximum panel distance.");
            AssertTrue(uiObject.transform.position.y <= 1.58f, "Far ray drag should not lift the panel above the comfort height band.");
            AssertTrue(uiObject.transform.position.y >= 1.42f, "Far ray drag should keep the panel near its starting comfort height.");
        }
        finally
        {
            Object.DestroyImmediate(handleObject);
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void HtmlStyleMainEntryPanelUsesVrReadableScale()
    {
        var canvasObject = new GameObject("MainEntryCanvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var panel = HtmlStyleMainEntryPanel.Ensure(canvasObject.transform, null, null);
            var rect = canvasObject.GetComponent<RectTransform>();
            var healthCard = canvasObject.transform.Find("Panel/Module_HealthGame");
            var rehabCard = canvasObject.transform.Find("Panel/Module_Rehab");
            var travelCard = canvasObject.transform.Find("Panel/Module_Travel");
            var memoryCard = canvasObject.transform.Find("Panel/Module_Memory");
            var healthGroup = healthCard != null ? healthCard.GetComponent<CanvasGroup>() : null;
            var healthIcon = healthCard != null ? healthCard.Find("Icon")?.GetComponent<Image>() : null;
            var greetingText = canvasObject.transform.Find("Panel/Greeting")?.GetComponent<TMPro.TMP_Text>();

            AssertTrue(panel != null, "HTML-style main entry panel should be created.");
            AssertTrue(rect != null && Mathf.Abs(rect.sizeDelta.x - 1120f) < 0.01f, "Main entry panel should use the wide HTML-style canvas pixel size.");
            AssertTrue(canvasObject.transform.localScale.x <= 0.0015f, "Main entry panel should normalize its world scale for PICO readability.");
            AssertTrue(canvasObject.GetComponent<MrKeepVisible>() != null, "Main entry panel should be protected from MR background visual suppression.");
            AssertTrue(healthCard != null && rehabCard != null && travelCard != null && memoryCard != null, "Main entry panel should create all four HTML home cards.");
            AssertTrue(Mathf.Abs(((RectTransform)healthCard).anchoredPosition.y - ((RectTransform)memoryCard).anchoredPosition.y) < 0.01f, "Main entry home cards should stay in one row like the HTML layout.");
            AssertTrue(greetingText != null && canvasObject.transform.Find("Panel/WeatherTime") != null, "Main entry panel should include the HTML greeting and weather/time top bar.");
            AssertTrue(greetingText.font != null && greetingText.font.name == "RehabChineseTMP", "Main entry text should bind to the project Chinese TMP font instead of the default missing-glyph font.");
            AssertTrue(healthIcon != null && healthIcon.sprite != null, "Main entry runtime GUI should use the provided SVG-derived table tennis icon sprite.");
            AssertTrue(healthGroup == null || healthGroup.alpha > 0.99f, "Main entry cards should be visible immediately instead of waiting on entrance animation.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static void ThumbstickNavigatorMovesLeftToLeftCard()
    {
        var previousEventSystem = EventSystem.current;
        var eventSystemObject = previousEventSystem == null ? new GameObject("EventSystem") : null;
        var rootObject = new GameObject("NavigatorRoot", typeof(RectTransform));
        var leftObject = CreateNavigationButton(rootObject.transform, "Left", new Vector2(-120f, 0f));
        var middleObject = CreateNavigationButton(rootObject.transform, "Middle", Vector2.zero);
        var rightObject = CreateNavigationButton(rootObject.transform, "Right", new Vector2(120f, 0f));
        try
        {
            if (eventSystemObject != null)
            {
                eventSystemObject.AddComponent<EventSystem>();
            }

            var navigator = rootObject.AddComponent<WorldSpaceUiThumbstickNavigator>();
            navigator.selectableRoot = rootObject.GetComponent<RectTransform>();
            navigator.selectables = new Selectable[]
            {
                leftObject.GetComponent<Button>(),
                middleObject.GetComponent<Button>(),
                rightObject.GetComponent<Button>()
            };

            var eventSystem = EventSystem.current != null
                ? EventSystem.current
                : eventSystemObject != null
                    ? eventSystemObject.GetComponent<EventSystem>()
                    : Object.FindObjectOfType<EventSystem>();
            AssertTrue(eventSystem != null, "Thumbstick navigator test should have an EventSystem.");
            navigator.eventSystemOverride = eventSystem;

            eventSystem.SetSelectedGameObject(middleObject);
            AssertTrue(navigator.NavigateForInput(Vector2.left), "Navigator should consume a left thumbstick input.");
            AssertTrue(eventSystem.currentSelectedGameObject == leftObject, "Left thumbstick input should select the card on the user's left, not the right card.");

            eventSystem.SetSelectedGameObject(middleObject);
            AssertTrue(navigator.NavigateForInput(Vector2.right), "Navigator should consume a right thumbstick input.");
            AssertTrue(eventSystem.currentSelectedGameObject == rightObject, "Right thumbstick input should select the card on the user's right.");
        }
        finally
        {
            Object.DestroyImmediate(rightObject);
            Object.DestroyImmediate(middleObject);
            Object.DestroyImmediate(leftObject);
            Object.DestroyImmediate(rootObject);
            if (eventSystemObject != null)
            {
                Object.DestroyImmediate(eventSystemObject);
            }
        }
    }

    private static void VideoPanelLayoutIsDecoupledFromTrainingAreaByDefault()
    {
        var headObject = new GameObject("Head");
        var panelObject = new GameObject("RehabVideoPanel");
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var trainingAreaObject = new GameObject("TrainingArea");
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;
            quadObject.name = "VideoQuad";
            quadObject.transform.SetParent(panelObject.transform, false);

            var layout = panelObject.AddComponent<RehabVideoPanelLayoutController>();
            layout.panelRoot = panelObject.transform;
            layout.videoQuad = quadObject.transform;
            layout.headTransform = headObject.transform;
            layout.trainingAreaRoot = trainingAreaObject.transform;
            layout.preferPromptCanvasLayout = false;
            layout.followTrainingAreaRoot = false;
            layout.panelDistance = 1.8f;
            layout.videoRightOffset = 0.75f;

            trainingAreaObject.transform.position = new Vector3(2f, 0f, 2f);
            layout.PlaceInRightFrontOfUserOnce();
            var firstPosition = panelObject.transform.position;

            trainingAreaObject.transform.position = new Vector3(-2f, 0f, 4f);
            layout.PlaceInRightFrontOfUserOnce();

            AssertTrue(Vector3.Distance(panelObject.transform.position, firstPosition) < 0.001f, "Video panel should not follow training-area movement by default.");
        }
        finally
        {
            Object.DestroyImmediate(trainingAreaObject);
            Object.DestroyImmediate(quadObject);
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void VideoPanelScalingClampsAndKeepsPanelUpright()
    {
        var headObject = new GameObject("Head");
        var panelObject = new GameObject("RehabVideoPanel");
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;
            quadObject.name = "VideoQuad";
            quadObject.transform.SetParent(panelObject.transform, false);

            var layout = panelObject.AddComponent<RehabVideoPanelLayoutController>();
            layout.panelRoot = panelObject.transform;
            layout.videoQuad = quadObject.transform;
            layout.headTransform = headObject.transform;
            layout.videoWidth = 0.62f;
            layout.videoHeight = 0.35f;
            layout.minVideoScale = 0.65f;
            layout.maxVideoScale = 1.7f;

            layout.SetVideoScale(99f);
            AssertTrue(Mathf.Abs(quadObject.transform.localScale.x - layout.videoWidth * layout.maxVideoScale) < 0.001f, "Video scale should clamp to the configured maximum.");

            layout.SetVideoScale(0.01f);
            AssertTrue(Mathf.Abs(quadObject.transform.localScale.y - layout.videoHeight * layout.minVideoScale) < 0.001f, "Video scale should clamp to the configured minimum.");

            layout.MoveVideoToWorldPoint(new Vector3(0.8f, 1.7f, 1.9f), headObject.transform.position);
            AssertTrue(Vector3.Dot(panelObject.transform.up, Vector3.up) > 0.999f, "Video panel movement should keep the display upright without tilt.");
        }
        finally
        {
            Object.DestroyImmediate(quadObject);
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void SpatialRayControlCanPlaceTrainingAreaExplicitly()
    {
        var sessionObject = new GameObject("RehabSessionSpatialPlacementTest");
        var areaObject = new GameObject("TrainingArea");
        var controlObject = new GameObject("RehabSpatialRayControl");
        try
        {
            var session = sessionObject.AddComponent<RehabSessionManager>();
            session.trainingAreaRoot = areaObject.transform;
            session.trainingFloorY = 0f;

            var control = controlObject.AddComponent<RehabSpatialRayControl>();
            control.sessionManager = session;
            control.floorY = 0f;
            control.maxRayDistanceMeters = 10f;
            control.SelectTrainingAreaTarget();
            var placedByRay = control.TryPlaceTrainingAreaFromRay(new Ray(new Vector3(1.25f, 3f, 2.5f), Vector3.down));

            AssertTrue(control.PlacementArmed, "Selecting the training-area target should arm explicit placement.");
            AssertTrue(placedByRay, "Explicit spatial placement should accept a floor ray.");
            AssertTrue(Vector3.Distance(session.TrainingCenter, new Vector3(1.25f, 0f, 2.5f)) < 0.001f, "Explicit spatial placement should update the session training center.");
            AssertTrue(Vector3.Distance(areaObject.transform.position, session.TrainingCenter) < 0.001f, "Explicit spatial placement should move the training area root.");
        }
        finally
        {
            Object.DestroyImmediate(controlObject);
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(sessionObject);
        }
    }

    private static void SpatialRayControlDragsVideoOnlyWhileTriggerHeld()
    {
        var headObject = new GameObject("Head");
        var panelObject = new GameObject("RehabVideoPanel");
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var controlObject = new GameObject("RehabSpatialRayControl");
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;
            panelObject.transform.position = new Vector3(0f, 1.6f, 2f);
            quadObject.name = "VideoQuad";
            quadObject.transform.SetParent(panelObject.transform, false);

            var layout = panelObject.AddComponent<RehabVideoPanelLayoutController>();
            layout.panelRoot = panelObject.transform;
            layout.videoQuad = quadObject.transform;
            layout.headTransform = headObject.transform;
            layout.minPanelDistance = 0.5f;
            layout.maxPanelDistance = 5f;

            var control = controlObject.AddComponent<RehabSpatialRayControl>();
            control.videoLayoutController = layout;
            control.hmdTransform = headObject.transform;
            control.maxRayDistanceMeters = 5f;
            control.EnsureControlCanvas();
            control.EnsureVideoRayTarget();
            control.SetControlCanvasVisible(true);

            AssertTrue(panelObject.transform.Find("RehabSpatialControls/MoveVideoButton") == null, "Video move button should be removed because the video surface is dragged directly.");

            var beginRay = new Ray(new Vector3(0f, 1.6f, 0f), Vector3.forward);
            AssertTrue(control.TryBeginDirectVideoPanelDrag(beginRay), "Direct video drag should begin from a ray hit on the video surface.");
            AssertTrue(control.DirectVideoDragActive, "Direct video drag should stay active while the trigger is held.");

            var before = panelObject.transform.position;
            var dragRay = new Ray(new Vector3(0.25f, 1.72f, 0f), Vector3.forward);
            AssertTrue(control.UpdateDirectVideoPanelDrag(dragRay), "Direct video drag should update from the current controller ray.");
            AssertTrue(Vector3.Distance(panelObject.transform.position, before) > 0.05f, "Direct video drag should move the panel.");

            control.EndDirectVideoPanelDrag();
            AssertTrue(!control.DirectVideoDragActive, "Direct video drag should end when the trigger is released.");
            var afterRelease = panelObject.transform.position;
            control.UpdateDirectVideoPanelDrag(new Ray(new Vector3(-0.4f, 1.2f, 0f), Vector3.forward));
            AssertTrue(Vector3.Distance(panelObject.transform.position, afterRelease) < 0.001f, "Released video panel should keep its last position.");
        }
        finally
        {
            Object.DestroyImmediate(controlObject);
            Object.DestroyImmediate(quadObject);
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private static void VideoSpatialControlsStayHiddenUntilVideoGuideShows()
    {
        var panelObject = new GameObject("RehabVideoPanel");
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var controlObject = new GameObject("RehabSpatialRayControl");
        try
        {
            quadObject.name = "VideoQuad";
            quadObject.transform.SetParent(panelObject.transform, false);

            var layout = panelObject.AddComponent<RehabVideoPanelLayoutController>();
            layout.panelRoot = panelObject.transform;
            layout.videoQuad = quadObject.transform;

            var control = controlObject.AddComponent<RehabSpatialRayControl>();
            control.videoLayoutController = layout;
            control.EnsureControlCanvas();

            var guide = panelObject.AddComponent<RehabVideoGuideController>();
            guide.videoPanel = panelObject;
            guide.videoQuad = quadObject;
            guide.layoutController = layout;

            AssertTrue(control.controlCanvasRoot != null, "Video spatial control canvas should be created for video mode.");
            AssertTrue(!control.controlCanvasRoot.activeSelf, "Video spatial controls should stay hidden before the video guide is shown.");

            var applyVisible = typeof(RehabVideoGuideController).GetMethod(
                "ApplyDisplayVisible",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            AssertTrue(applyVisible != null, "Video guide should expose an internal display visibility gate.");

            applyVisible.Invoke(guide, new object[] { true });
            AssertTrue(control.controlCanvasRoot.activeSelf, "Video spatial controls should appear when the video guide display is visible.");

            guide.StopAndHide();
            AssertTrue(!control.controlCanvasRoot.activeSelf, "Video spatial controls should hide again when the video guide stops.");
        }
        finally
        {
            Object.DestroyImmediate(controlObject);
            Object.DestroyImmediate(quadObject);
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void VideoGuidePauseKeepsDisplayVisible()
    {
        var panelObject = new GameObject("RehabVideoPanel");
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var controlObject = new GameObject("RehabSpatialRayControl");
        try
        {
            quadObject.name = "VideoQuad";
            quadObject.transform.SetParent(panelObject.transform, false);
            quadObject.SetActive(false);

            var layout = panelObject.AddComponent<RehabVideoPanelLayoutController>();
            layout.panelRoot = panelObject.transform;
            layout.videoQuad = quadObject.transform;

            var control = controlObject.AddComponent<RehabSpatialRayControl>();
            control.videoLayoutController = layout;
            control.EnsureControlCanvas();
            control.SetControlCanvasVisible(false);

            var guide = panelObject.AddComponent<RehabVideoGuideController>();
            guide.videoPanel = panelObject;
            guide.videoQuad = quadObject;
            guide.layoutController = layout;

            guide.EnsureDisplayVisibleWhilePaused();

            AssertTrue(quadObject.activeSelf, "Pausing a rehab movement should keep the video display visible while time remains.");
            AssertTrue(control.controlCanvasRoot != null && control.controlCanvasRoot.activeSelf, "Paused video display should keep the spatial controls visible.");
        }
        finally
        {
            Object.DestroyImmediate(controlObject);
            Object.DestroyImmediate(quadObject);
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void TrainingSelectPanelRecenterUsesConfiguredComfortPlacement()
    {
        var headObject = new GameObject("Head");
        var uiObject = new GameObject("RehabModeSelectCanvas", typeof(RectTransform));
        var selectPanel = new GameObject("TrainingSelectPanel");
        try
        {
            headObject.transform.position = new Vector3(0f, 1.6f, 0f);
            headObject.transform.rotation = Quaternion.identity;
            selectPanel.transform.SetParent(uiObject.transform, false);

            var rect = uiObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 560f);

            var placer = uiObject.AddComponent<ComfortWorldSpaceUIPlacer>();
            placer.headTransform = headObject.transform;
            placer.uiRoot = rect;
            placer.distanceMeters = 2f;
            placer.hmdHeightOffsetMeters = -0.1f;
            placer.usePreferredHeightInsteadOfHeadHeight = false;
            placer.clampWorldHeight = true;
            placer.minWorldHeight = 1.25f;
            placer.maxWorldHeight = 1.75f;
            placer.enableRayDrag = true;
            placer.enableThumbstickNavigation = true;

            var modeUi = uiObject.AddComponent<RehabModeSelectUI>();
            modeUi.applyTrainingAndResultVisualSkin = false;
            modeUi.uiPlacer = placer;
            modeUi.rehabTrainingSelectPanel = selectPanel;
            modeUi.placeUiOnTrainingSelectOpen = true;
            modeUi.trainingSelectDistanceMeters = 2.45f;
            modeUi.trainingSelectHeightOffsetMeters = 0.08f;

            modeUi.ShowTrainingSelectPanel();

            AssertTrue(Mathf.Abs(uiObject.transform.position.z - 2f) < 0.001f, "Training selection panel recenter should keep the configured rehab distance.");
            AssertTrue(Mathf.Abs(uiObject.transform.position.y - 1.5f) < 0.001f, "Training selection panel recenter should use HMD-relative height.");
            AssertTrue(Mathf.Abs(placer.distanceMeters - 2f) < 0.001f, "Opening the selection page should not replace the startup distance configuration.");
            AssertTrue(Mathf.Abs(placer.hmdHeightOffsetMeters + 0.1f) < 0.001f, "Opening the selection page should not replace the HMD height offset.");
            AssertTrue(rect.Find("RayDragHandle") != null, "Training selection panel should expose a ray-drag handle.");
        }
        finally
        {
            Object.DestroyImmediate(selectPanel);
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(headObject);
        }
    }

    private sealed class RehabPageLayoutFixture
    {
        public readonly GameObject root;
        public readonly Transform head;
        public readonly GameObject trainingArea;
        public readonly GameObject selectionRoot;
        public readonly GameObject trainingRoot;
        public readonly GameObject resultRoot;
        public readonly GameObject videoPanel;
        public readonly GameObject coachRoot;
        public readonly Transform trainingLayoutAnchor;
        public readonly MovementEvaluator evaluator;
        public readonly RehabSessionManager session;
        public readonly RehabPanelPlacementController panelPlacement;
        public readonly RehabModeSelectUI modeUi;

        public RehabPageLayoutFixture()
        {
            root = new GameObject("RehabPageLayoutFixture");
            var headObject = new GameObject("RehabPageLayoutHead");
            head = headObject.transform;
            head.position = new Vector3(0f, 1.6f, 0f);
            head.rotation = Quaternion.identity;

            trainingArea = CreateChild(root.transform, "TrainingArea");
            selectionRoot = CreateChild(root.transform, "SelectionPanelRoot");
            var selectionPanel = CreateChild(selectionRoot.transform, "RehabTrainingSelectPanel");
            trainingLayoutAnchor = CreateChild(root.transform, "TrainingLayoutAnchor").transform;
            trainingRoot = CreateChild(trainingLayoutAnchor, "TrainingFunctionPanelRoot");
            var trainingPanel = CreateChild(trainingRoot.transform, "RehabTrainingPanel");
            videoPanel = CreateChild(trainingLayoutAnchor, "RehabVideoPanel");
            resultRoot = CreateChild(root.transform, "ResultPanelRoot");
            var resultPanel = CreateChild(resultRoot.transform, "TrainingResultPanel");

            coachRoot = CreateChild(root.transform, "VirtualCoach");
            var coach = coachRoot.AddComponent<VirtualCoachController>();
            coach.userHeadTransform = head;
            coach.coachRoot = coachRoot.transform;
            coach.preferredDistanceMeters = 2f;
            coach.minDistanceMeters = 1.8f;
            coach.maxDistanceMeters = 2.2f;
            coach.floorY = 0f;
            coach.keepInFrontOfUser = false;
            coach.placeInFrontOnStart = false;

            var circleAnchor = root.AddComponent<TrainingCircleAnchor>();
            circleAnchor.headTransform = head;
            circleAnchor.trainingAreaRoot = trainingArea.transform;
            circleAnchor.fallbackFloorY = 0f;
            circleAnchor.useRaycastFloorHeight = false;

            panelPlacement = root.AddComponent<RehabPanelPlacementController>();
            panelPlacement.headTransform = head;
            panelPlacement.viewTransform = head;
            panelPlacement.selectionPanelRoot = selectionRoot.transform;
            panelPlacement.trainingLayoutAnchor = trainingLayoutAnchor;
            panelPlacement.trainingFunctionPanelRoot = trainingRoot.transform;
            panelPlacement.resultPanelRoot = resultRoot.transform;
            panelPlacement.videoPanelRoot = videoPanel.transform;
            panelPlacement.trainingLayoutDistance = 1.8f;
            panelPlacement.trainingLayoutHeightOffset = -0.1f;
            panelPlacement.resultPanelDistance = 2f;
            panelPlacement.resultPanelHeightOffset = -0.1f;
            panelPlacement.clampResultPanelHeight = true;
            panelPlacement.minResultPanelHeight = 1.25f;
            panelPlacement.maxResultPanelHeight = 1.75f;

            var safety = root.AddComponent<SafetyMonitor>();
            safety.hmdTransform = head;
            evaluator = root.AddComponent<MovementEvaluator>();
            evaluator.baduanjinEvaluator = root.AddComponent<BaduanjinEvaluator>();
            evaluator.taiChiEvaluator = root.AddComponent<TaiChiEvaluator>();
            var recorder = root.AddComponent<TrainingResultRecorder>();

            session = root.AddComponent<RehabSessionManager>();
            session.safetyMonitor = safety;
            session.movementEvaluator = evaluator;
            session.resultRecorder = recorder;
            session.virtualCoachController = coach;
            session.trainingCircleAnchor = circleAnchor;
            session.panelPlacementController = panelPlacement;
            session.trainingAreaRoot = trainingArea.transform;
            session.autoCreateVirtualCoach = false;
            session.autoStartSession = false;
            session.placeTrainingAreaOnStart = false;

            modeUi = root.AddComponent<RehabModeSelectUI>();
            modeUi.rehabTrainingSelectPanel = selectionPanel;
            modeUi.rehabTrainingPanel = trainingPanel;
            modeUi.trainingResultPanel = resultPanel;
            modeUi.panelPlacementController = panelPlacement;
            modeUi.sessionManager = session;
            modeUi.placeUiOnTrainingSelectOpen = false;
            modeUi.applyHtmlStylePanels = false;
            modeUi.applyTrainingAndResultVisualSkin = false;
            session.modeSelectUI = modeUi;
        }

        public void Destroy()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (head != null)
            {
                Object.DestroyImmediate(head.gameObject);
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }

    private static RehabPoseSample CreateSample(float headY, float leftY, float rightY)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = new Vector3(0f, headY, 0f),
            headRotation = Quaternion.identity,
            leftHandPosition = new Vector3(-0.2f, leftY, 0.4f),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = new Vector3(0.2f, rightY, 0.4f),
            rightHandRotation = Quaternion.identity
        };
    }

    private static GameObject CreateNavigationButton(Transform parent, string name, Vector2 anchoredPosition)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 64f);
        rect.anchoredPosition = anchoredPosition;
        return buttonObject;
    }

    private static RehabPoseSample CreateSampleWithHands(float headY, Vector3 leftLocal, Vector3 rightLocal)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = new Vector3(0f, headY, 0f),
            headRotation = Quaternion.identity,
            leftHandPosition = new Vector3(leftLocal.x, headY + leftLocal.y - 1.6f, leftLocal.z),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = new Vector3(rightLocal.x, headY + rightLocal.y - 1.6f, rightLocal.z),
            rightHandRotation = Quaternion.identity
        };
    }

    private static RehabPoseSample CreateSampleWithHeadYaw(float headY, float yawDegrees)
    {
        var sample = CreateSample(headY, headY - 0.3f, headY - 0.3f);
        sample.headRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        return sample;
    }

    private static RehabPoseSample CreateSampleWithHeadPosition(Vector3 headPosition)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = headPosition,
            headRotation = Quaternion.identity,
            leftHandPosition = headPosition + new Vector3(-0.2f, -0.45f, 0.2f),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = headPosition + new Vector3(0.2f, -0.45f, 0.2f),
            rightHandRotation = Quaternion.identity
        };
    }

    private static RehabPoseSample CreateTaiChiSample(Vector3 leftLocal, Vector3 rightLocal)
    {
        var head = new Vector3(0f, 1.6f, 0f);
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = head,
            headRotation = Quaternion.identity,
            leftHandPosition = head + leftLocal,
            leftHandRotation = Quaternion.identity,
            rightHandPosition = head + rightLocal,
            rightHandRotation = Quaternion.identity
        };
    }

    private static void ConfigureSingleTwoHandsMovement(MovementEvaluator evaluator)
    {
        evaluator.autoCreateDefaultBaduanjinDefinitions = false;
        evaluator.movementDefinitions = new[]
        {
            new MovementDefinition(
                RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
                "双手托天理三焦",
                "test",
                new MovementStepDefinition("上举保持", "双手举至头顶上方", 2f, 25f))
        };
    }

    private static void AssertStepValid(BaduanjinEvaluator evaluator, MovementDefinition movement, int stepIndex, RehabPoseSample sample, string message)
    {
        evaluator.ResetForMovement(movement.movementId, CreateSample(1.6f, 1.2f, 1.2f));
        var result = evaluator.EvaluateStep(movement, stepIndex, sample, 1f);
        AssertTrue(result.poseValid, message);
    }

    private static void AssertTaiChiStepValid(TaiChiEvaluator evaluator, MovementDefinition movement, int stepIndex, RehabPoseSample baseline, RehabPoseSample sample, string message)
    {
        evaluator.ResetForMovement(movement.movementId, baseline);
        var result = evaluator.EvaluateStep(movement, stepIndex, sample, 1f);
        AssertTrue(result.poseValid, message);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
