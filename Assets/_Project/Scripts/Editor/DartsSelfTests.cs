using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DartsSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run Darts Self Tests")]
    public static void RunAll()
    {
        ThrowRequiresMinimumHandSpeed();
        ThrowSpeedScalesAndClamps();
        ThrowKeepsHandDirection();
        TrackedVelocityMatchesDisplacement();
        LateReleaseFallsBackToPeakSwingVelocity();
        BoardScoringBandsMatchRings();
        BoardScoreIsZeroOutsideFace();
        AssistedStandardThrowLandsOnBoard();
        BallisticAssistSnapsGentleArcsToBull();
        BallisticAssistIgnoresWildAndUnreachableThrows();
        BoardHeightCalibrationClampsToComfortRange();
        DifficultyDistancesAreOrdered();
        BoardRegisterHitRaisesScoredEvent();
        ThrowHandSwapSwapsRolesAndNodes();
        PanelUsesHealthGameSceneRouting();
        ManagerAutoStartDefaultsOff();
        DartHitMaskExcludesInvisibleAndBodyLayers();
        Debug.Log("Darts self tests passed.");
    }

    private static void ThrowRequiresMinimumHandSpeed()
    {
        var state = DartsSolver.ComputeThrow(
            Vector3.forward * (DartsGeometry.MinThrowHandSpeedMetersPerSecond * 0.5f),
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);

        AssertTrue(!state.isThrow, "A slow release should count as putting the dart back, not a throw.");
        AssertTrue(state.velocity == Vector3.zero, "A cancelled throw should carry no velocity.");
    }

    private static void ThrowSpeedScalesAndClamps()
    {
        // 门槛以上必须单调：旧的 乘数+Clamp 方案在 1.2-2.5m/s 有一段无反馈死区。
        var soft = DartsSolver.ComputeThrow(
            Vector3.forward * 1.3f,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);
        var firmer = DartsSolver.ComputeThrow(
            Vector3.forward * 2.4f,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);
        AssertTrue(firmer.velocity.magnitude > soft.velocity.magnitude + 0.1f, "Hand speed above the threshold must map monotonically to dart speed (no dead zone).");

        var gentle = DartsSolver.ComputeThrow(
            Vector3.forward * 1.5f,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);
        AssertTrue(gentle.isThrow, "A gentle swing above the minimum hand speed should throw.");
        AssertTrue(
            gentle.velocity.magnitude >= DartsGeometry.MinDartSpeedMetersPerSecond - 0.001f,
            "Dart speed should never fall below the playable minimum.");

        var medium = DartsSolver.ComputeThrow(
            Vector3.forward * 3f,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);
        AssertTrue(medium.velocity.magnitude > gentle.velocity.magnitude, "Faster swings should throw faster darts.");

        var wild = DartsSolver.ComputeThrow(
            Vector3.forward * 12f,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);
        AssertTrue(
            wild.velocity.magnitude <= DartsGeometry.MaxDartSpeedMetersPerSecond + 0.001f,
            "Dart speed should clamp to the maximum for very fast swings.");
    }

    private static void ThrowKeepsHandDirection()
    {
        var handVelocity = new Vector3(0.5f, 0.4f, 2.5f);
        var state = DartsSolver.ComputeThrow(
            handVelocity,
            DartsGeometry.MinThrowHandSpeedMetersPerSecond,
            DartsGeometry.HandSpeedMultiplier,
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond);

        AssertTrue(state.isThrow, "A normal swing should throw.");
        AssertTrue(
            Vector3.Angle(state.velocity, handVelocity) < 0.01f,
            "The dart should leave along the tracked hand direction.");
    }

    private static void TrackedVelocityMatchesDisplacement()
    {
        var velocity = DartsSolver.ComputeTrackedVelocity(
            new Vector3(0f, 1f, 0f),
            new Vector3(0.2f, 1.1f, 0.3f),
            0.1f);
        AssertTrue((velocity - new Vector3(2f, 1f, 3f)).magnitude < 0.001f, "Tracked velocity should equal displacement over elapsed time.");

        AssertTrue(
            DartsSolver.ComputeTrackedVelocity(Vector3.zero, Vector3.one, 0f) == Vector3.zero,
            "Zero elapsed time should yield zero velocity instead of dividing by zero.");
    }

    private static void LateReleaseFallsBackToPeakSwingVelocity()
    {
        var peak = new Vector3(0f, 1.5f, 4f);
        var lateRelease = new Vector3(0f, -0.8f, 0.6f);

        var selected = DartsSolver.SelectReleaseVelocity(lateRelease, peak, 0.1f, 0.25f);
        AssertTrue(selected == peak, "A decayed late release should fall back to the recent peak swing velocity.");

        var stalePeak = DartsSolver.SelectReleaseVelocity(lateRelease, peak, 0.6f, 0.25f);
        AssertTrue(stalePeak == lateRelease, "A stale peak outside the forgiveness window should not hijack a slow release.");

        var fastRelease = new Vector3(0f, 1.4f, 4.2f);
        var keepCurrent = DartsSolver.SelectReleaseVelocity(fastRelease, peak, 0.05f, 0.25f);
        AssertTrue(keepCurrent == fastRelease, "A clean fast release should use the live window velocity.");
    }

    private static void BoardScoringBandsMatchRings()
    {
        var radius = DartsGeometry.BoardFaceRadiusMeters;
        var bands = DartsGeometry.BoardScoreBands;
        var maxScore = DartsGeometry.BoardMaxRingScore;
        var bandWidth = radius / bands;

        AssertTrue(ArcherySolver.ScoreForRadialDistance(0f, radius, bands, maxScore) == 10, "Bullseye should score 10.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(bandWidth * 0.5f, radius, bands, maxScore) == 10, "Inside the bull band should score 10.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(bandWidth * 1.5f, radius, bands, maxScore) == 8, "Second band should score 8.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(radius - 0.005f, radius, bands, maxScore) == 2, "Outermost band should score 2.");
    }

    private static void BoardScoreIsZeroOutsideFace()
    {
        var radius = DartsGeometry.BoardFaceRadiusMeters;
        AssertTrue(
            ArcherySolver.ScoreForRadialDistance(radius + 0.01f, radius, DartsGeometry.BoardScoreBands, DartsGeometry.BoardMaxRingScore) == 0,
            "Hit outside the board face should score 0.");
    }

    private static void AssistedStandardThrowLandsOnBoard()
    {
        var origin = new Vector3(0f, DartsGeometry.DefaultSeatedEyeHeightMeters, 0f);
        var boardCenter = new Vector3(0f, DartsGeometry.DefaultSeatedEyeHeightMeters, DartsGeometry.StandardBoardDistanceMeters);
        var levelThrow = Vector3.forward * 8f;

        var assisted = DartsSolver.ComputeAssistedVelocityBallistic(
            origin,
            levelThrow,
            boardCenter,
            DartsGeometry.AimAssistDefaultDegrees,
            DartsGeometry.DartGravityMetersPerSecondSquared);

        var reached = ArcherySolver.PredictImpactOnPlaneZ(
            origin,
            assisted,
            DartsGeometry.StandardBoardDistanceMeters,
            DartsGeometry.DartGravityMetersPerSecondSquared,
            DartsGeometry.DartLinearDragPerSecond,
            3f,
            out var impact);

        AssertTrue(reached, "An assisted level throw should reach the standard board distance.");
        AssertTrue(
            Mathf.Abs(DartsGeometry.DefaultSeatedEyeHeightMeters - impact.y) < DartsGeometry.BoardFaceRadiusMeters,
            "An assisted level throw at standard distance should land on the board face.");
    }

    private static void BallisticAssistSnapsGentleArcsToBull()
    {
        var gravity = DartsGeometry.DartGravityMetersPerSecondSquared;
        var origin = new Vector3(0f, DartsGeometry.DefaultSeatedEyeHeightMeters, 0f);

        // 标准距 2.4 米、最轻出手 5.5 m/s：精确弹道理想仰角约 25.6°。
        // 玩家以接近正确的 20° 抛物线出手（偏差 5.6° < 8° 纠偏预算）应被吸附到理想方向，
        // 落点进入盘心区——这正是旧的直线时间近似做不到的（会拉低到恒定 4 环）。
        var standardCenter = new Vector3(0f, origin.y, DartsGeometry.StandardBoardDistanceMeters);
        var aim20 = new Vector3(0f, Mathf.Sin(20f * Mathf.Deg2Rad), Mathf.Cos(20f * Mathf.Deg2Rad));
        var assisted = DartsSolver.ComputeAssistedVelocityBallistic(
            origin,
            aim20 * DartsGeometry.MinDartSpeedMetersPerSecond,
            standardCenter,
            DartsGeometry.AimAssistDefaultDegrees,
            gravity);
        var reached = ArcherySolver.PredictImpactOnPlaneZ(
            origin, assisted, DartsGeometry.StandardBoardDistanceMeters,
            gravity, DartsGeometry.DartLinearDragPerSecond, 3f, out var impact);
        AssertTrue(reached, "An assisted gentle arc should reach the standard board.");
        AssertTrue(Mathf.Abs(origin.y - impact.y) < 0.08f, "An assisted correct-arc gentle throw should land near the bull at standard distance.");

        // 远距 3 米、5.5 m/s：理想仰角约 38.3°，玩家 33° 出手同样应被吸附上盘。
        var farCenter = new Vector3(0f, origin.y, DartsGeometry.FarBoardDistanceMeters);
        var aim33 = new Vector3(0f, Mathf.Sin(33f * Mathf.Deg2Rad), Mathf.Cos(33f * Mathf.Deg2Rad));
        var assistedFar = DartsSolver.ComputeAssistedVelocityBallistic(
            origin,
            aim33 * DartsGeometry.MinDartSpeedMetersPerSecond,
            farCenter,
            DartsGeometry.AimAssistDefaultDegrees,
            gravity);
        var reachedFar = ArcherySolver.PredictImpactOnPlaneZ(
            origin, assistedFar, DartsGeometry.FarBoardDistanceMeters,
            gravity, DartsGeometry.DartLinearDragPerSecond, 3f, out var impactFar);
        AssertTrue(reachedFar, "An assisted gentle arc should reach the far board.");
        AssertTrue(
            Mathf.Abs(origin.y - impactFar.y) < DartsGeometry.BoardFaceRadiusMeters * 0.5f,
            "An assisted correct-arc gentle throw should land on the board face at far distance.");
    }

    private static void BallisticAssistIgnoresWildAndUnreachableThrows()
    {
        var gravity = DartsGeometry.DartGravityMetersPerSecondSquared;
        var origin = new Vector3(0f, 1.2f, 0f);
        var center = new Vector3(0f, 1.2f, DartsGeometry.StandardBoardDistanceMeters);

        var wild = Vector3.down * 6f;
        AssertTrue(
            DartsSolver.ComputeAssistedVelocityBallistic(origin, wild, center, DartsGeometry.AimAssistDefaultDegrees, gravity) == wild,
            "Aim assist should not hijack throws aimed far away from the board.");

        var tooSlow = Vector3.forward * 4f;
        var farCenter = new Vector3(0f, 1.2f, DartsGeometry.FarBoardDistanceMeters);
        AssertTrue(
            DartsSolver.ComputeAssistedVelocityBallistic(origin, tooSlow, farCenter, DartsGeometry.AimAssistDefaultDegrees, gravity) == tooSlow,
            "Aim assist should leave physically unreachable throws untouched.");

        var anyThrow = Vector3.forward * 6f;
        AssertTrue(
            DartsSolver.ComputeAssistedVelocityBallistic(origin, anyThrow, center, 0f, gravity) == anyThrow,
            "Zero correction degrees should disable aim assist entirely.");
    }

    private static void BoardHeightCalibrationClampsToComfortRange()
    {
        AssertTrue(
            Mathf.Approximately(DartsSolver.ComputeBoardCenterHeight(0f), DartsGeometry.DefaultSeatedEyeHeightMeters),
            "Missing head height should fall back to the seated default.");
        AssertTrue(
            Mathf.Approximately(DartsSolver.ComputeBoardCenterHeight(0.5f), DartsGeometry.MinBoardCenterHeightMeters),
            "Very low head height should clamp to the comfortable minimum.");
        AssertTrue(
            Mathf.Approximately(DartsSolver.ComputeBoardCenterHeight(2.4f), DartsGeometry.MaxBoardCenterHeightMeters),
            "Very high head height should clamp to the comfortable maximum.");
        AssertTrue(
            Mathf.Approximately(DartsSolver.ComputeBoardCenterHeight(1.3f), 1.3f),
            "Seated head height inside the range should be used as-is.");
    }

    private static void DifficultyDistancesAreOrdered()
    {
        var near = DartsGeometry.BoardDistanceForDifficulty(DartsDifficulty.Near);
        var standard = DartsGeometry.BoardDistanceForDifficulty(DartsDifficulty.Standard);
        var far = DartsGeometry.BoardDistanceForDifficulty(DartsDifficulty.Far);

        AssertTrue(near < standard && standard < far, "Difficulty distances should increase from near to far.");
        AssertTrue(near >= 1.5f, "Even the near board should sit a comfortable distance away.");
    }

    private static void BoardRegisterHitRaisesScoredEvent()
    {
        var boardObject = new GameObject("DartsTestBoard");
        try
        {
            var board = boardObject.AddComponent<DartsBoard>();
            board.faceCenter = boardObject.transform;
            board.faceRadiusMeters = DartsGeometry.BoardFaceRadiusMeters;
            board.scoreBands = DartsGeometry.BoardScoreBands;
            board.maxRingScore = DartsGeometry.BoardMaxRingScore;

            var receivedScore = -1;
            var receivedRing = -2;
            System.Action<DartHitInfo> handler = info =>
            {
                receivedScore = info.score;
                receivedRing = info.ringIndex;
            };

            DartsEvents.OnDartHit += handler;
            try
            {
                var returnedScore = board.RegisterHit(boardObject.transform.position + boardObject.transform.right * 0.03f, null);
                AssertTrue(returnedScore == 10, "A hit 3cm from the bull should score 10.");
                AssertTrue(receivedScore == 10, "The hit event should carry the same score.");
                AssertTrue(receivedRing == 0, "The hit event should report the innermost ring.");
            }
            finally
            {
                DartsEvents.OnDartHit -= handler;
            }
        }
        finally
        {
            Object.DestroyImmediate(boardObject);
        }
    }

    private static void ThrowHandSwapSwapsRolesAndNodes()
    {
        var throwerObject = new GameObject("DartsTestThrower");
        var rightHand = new GameObject("DartsTestRightHand");
        var leftHand = new GameObject("DartsTestLeftHand");
        try
        {
            var thrower = throwerObject.AddComponent<HandDartThrower>();
            thrower.throwHandTransform = rightHand.transform;
            thrower.offHandTransform = leftHand.transform;
            thrower.throwHandNode = UnityEngine.XR.XRNode.RightHand;
            thrower.offHandNode = UnityEngine.XR.XRNode.LeftHand;
            thrower.throwWithRightHand = true;

            thrower.SwapHands();
            AssertTrue(thrower.throwHandTransform == leftHand.transform, "Swapping hands should move the dart to the other hand.");
            AssertTrue(thrower.throwHandNode == UnityEngine.XR.XRNode.LeftHand, "Swapping hands should swap the haptic/input nodes.");
            AssertTrue(!thrower.throwWithRightHand, "Swapping hands should flip the handedness flag.");

            thrower.SetThrowWithRightHand(false);
            AssertTrue(!thrower.throwWithRightHand, "Setting the current handedness again should be a no-op.");

            thrower.SetThrowWithRightHand(true);
            AssertTrue(thrower.throwWithRightHand && thrower.throwHandTransform == rightHand.transform, "Setting handedness back should restore the original assignment.");
        }
        finally
        {
            Object.DestroyImmediate(throwerObject);
            Object.DestroyImmediate(rightHand);
            Object.DestroyImmediate(leftHand);
        }
    }

    private static void PanelUsesHealthGameSceneRouting()
    {
        AssertTrue(
            DartsScorePanel.DefaultHealthGameMenuSceneName == "02_HealthGameMenu",
            "The darts return route should target the health game menu scene.");

        var returnMethod = typeof(DartsScorePanel).GetMethod("ReturnToHealthGameMenu", BindingFlags.Public | BindingFlags.Instance);
        AssertTrue(returnMethod != null, "DartsScorePanel should expose a health game menu return action for its button.");

        var panelObject = new GameObject("DartsTestScorePanel");
        try
        {
            var panel = panelObject.AddComponent<DartsScorePanel>();
            AssertTrue(
                panel.HealthGameMenuSceneName == DartsScorePanel.DefaultHealthGameMenuSceneName,
                "DartsScorePanel should default to the standalone health game menu scene.");
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void ManagerAutoStartDefaultsOff()
    {
        var managerObject = new GameObject("DartsTestManager");
        try
        {
            var manager = managerObject.AddComponent<DartsGameManager>();
            AssertTrue(!manager.autoStartSessionOnStart, "Auto-start must stay opt-in so embedded usages control their own session flow.");
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    private static void DartHitMaskExcludesInvisibleAndBodyLayers()
    {
        var mask = DartsGameSceneBuilder.BuildDartHitLayerMask();
        foreach (var layerName in new[] { "Controller", "Racket", "PlayerBody", "TableSafetyZone", "RoomSensing" })
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                AssertTrue((mask.value & (1 << layer)) == 0, $"Dart hit mask should exclude the {layerName} layer.");
            }
        }

        AssertTrue((mask.value & 1) != 0, "Dart hit mask should keep the Default layer so the board remains hittable.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
