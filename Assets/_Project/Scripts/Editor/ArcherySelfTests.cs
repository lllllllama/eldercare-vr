using UnityEditor;
using UnityEngine;

public static class ArcherySelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run Archery Self Tests")]
    public static void RunAll()
    {
        DrawLengthClampsToMaximum();
        AimPointsFromStringHandToBowHand();
        DrawBelowThresholdCannotFire();
        LaunchSpeedScalesWithDraw();
        ReleaseVelocityIsZeroWithoutFireableDraw();
        RingScoresMatchConcentricBands();
        RingScoreIsZeroOutsideFace();
        ArrowFlightDropsWithGravity();
        SeatedArrowStillReachesMediumTarget();
        TargetHeightCalibrationClampsToComfortRange();
        LaneRotationFollowsHeadYawOnly();
        TargetRegisterHitRaisesScoredEvent();
        HomeMenuArcheryModuleTogglesGameplayRoots();
        DifficultyDistancesAreOrdered();
        Debug.Log("Archery self tests passed.");
    }

    private static void DrawLengthClampsToMaximum()
    {
        var state = ArcherySolver.ComputeDraw(
            Vector3.zero,
            new Vector3(0f, 0f, -2f),
            Vector3.forward,
            ArcheryGeometry.DrawRestSeparationMeters,
            ArcheryGeometry.MaxDrawLengthMeters,
            ArcheryGeometry.MinFireDraw01);

        AssertTrue(Mathf.Approximately(state.drawLengthMeters, ArcheryGeometry.MaxDrawLengthMeters), "Draw length should clamp to the configured maximum.");
        AssertTrue(Mathf.Approximately(state.draw01, 1f), "Fully drawn state should normalize to 1.");
        AssertTrue(state.canFire, "Fully drawn state should be fireable.");
    }

    private static void AimPointsFromStringHandToBowHand()
    {
        var bowHand = new Vector3(0.2f, 1.3f, 0.6f);
        var stringHand = new Vector3(0.2f, 1.3f, 0.2f);
        var state = ArcherySolver.ComputeDraw(
            bowHand,
            stringHand,
            Vector3.forward,
            ArcheryGeometry.DrawRestSeparationMeters,
            ArcheryGeometry.MaxDrawLengthMeters,
            ArcheryGeometry.MinFireDraw01);

        AssertTrue(Vector3.Dot(state.aimDirection, Vector3.forward) > 0.99f, "Aim direction should point from the string hand toward the bow hand.");
    }

    private static void DrawBelowThresholdCannotFire()
    {
        var shortDrawDistance = ArcheryGeometry.DrawRestSeparationMeters + ArcheryGeometry.MaxDrawLengthMeters * ArcheryGeometry.MinFireDraw01 * 0.5f;
        var state = ArcherySolver.ComputeDraw(
            Vector3.zero,
            new Vector3(0f, 0f, -shortDrawDistance),
            Vector3.forward,
            ArcheryGeometry.DrawRestSeparationMeters,
            ArcheryGeometry.MaxDrawLengthMeters,
            ArcheryGeometry.MinFireDraw01);

        AssertTrue(!state.canFire, "A draw below the fire threshold should not be fireable.");
    }

    private static void LaunchSpeedScalesWithDraw()
    {
        var slow = ArcherySolver.ComputeLaunchSpeed(0.2f, ArcheryGeometry.MinLaunchSpeedMetersPerSecond, ArcheryGeometry.MaxLaunchSpeedMetersPerSecond);
        var fast = ArcherySolver.ComputeLaunchSpeed(0.9f, ArcheryGeometry.MinLaunchSpeedMetersPerSecond, ArcheryGeometry.MaxLaunchSpeedMetersPerSecond);

        AssertTrue(fast > slow, "Launch speed should grow with draw.");
        AssertTrue(slow >= ArcheryGeometry.MinLaunchSpeedMetersPerSecond - 0.001f, "Launch speed should not fall below the minimum.");
        AssertTrue(fast <= ArcheryGeometry.MaxLaunchSpeedMetersPerSecond + 0.001f, "Launch speed should not exceed the maximum.");
    }

    private static void ReleaseVelocityIsZeroWithoutFireableDraw()
    {
        var state = new ArcherySolver.DrawState
        {
            aimDirection = Vector3.forward,
            drawLengthMeters = 0.02f,
            draw01 = 0.04f,
            canFire = false
        };

        var velocity = ArcherySolver.ComputeReleaseVelocity(state, ArcheryGeometry.MinLaunchSpeedMetersPerSecond, ArcheryGeometry.MaxLaunchSpeedMetersPerSecond);
        AssertTrue(velocity == Vector3.zero, "Release velocity should be zero when the draw cannot fire.");
    }

    private static void RingScoresMatchConcentricBands()
    {
        var radius = ArcheryGeometry.TargetFaceRadiusMeters;
        var bands = ArcheryGeometry.TargetScoreBands;
        var maxScore = ArcheryGeometry.TargetMaxRingScore;
        var bandWidth = radius / bands;

        AssertTrue(ArcherySolver.ScoreForRadialDistance(0f, radius, bands, maxScore) == 10, "Center hit should score 10.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(bandWidth * 0.5f, radius, bands, maxScore) == 10, "Hit inside the innermost band should score 10.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(bandWidth * 1.5f, radius, bands, maxScore) == 8, "Hit in the second band should score 8.");
        AssertTrue(ArcherySolver.ScoreForRadialDistance(radius - 0.005f, radius, bands, maxScore) == 2, "Hit in the outermost band should score 2.");
    }

    private static void RingScoreIsZeroOutsideFace()
    {
        var radius = ArcheryGeometry.TargetFaceRadiusMeters;
        AssertTrue(
            ArcherySolver.ScoreForRadialDistance(radius + 0.01f, radius, ArcheryGeometry.TargetScoreBands, ArcheryGeometry.TargetMaxRingScore) == 0,
            "Hit outside the target face should score 0.");
        AssertTrue(
            ArcherySolver.RingIndexForRadialDistance(radius + 0.01f, radius, ArcheryGeometry.TargetScoreBands) == -1,
            "Ring index outside the face should be invalid.");
    }

    private static void ArrowFlightDropsWithGravity()
    {
        var origin = new Vector3(0f, 1.4f, 0f);
        var velocity = Vector3.forward * 12f;
        var reached = ArcherySolver.PredictImpactOnPlaneZ(
            origin,
            velocity,
            ArcheryGeometry.MediumTargetDistanceMeters,
            ArcheryGeometry.ArrowGravityMetersPerSecondSquared,
            ArcheryGeometry.ArrowLinearDragPerSecond,
            5f,
            out var impact);

        AssertTrue(reached, "A level shot should reach the medium target plane.");
        AssertTrue(impact.y < origin.y, "Gravity should pull the arrow below its launch height.");
        AssertTrue(origin.y - impact.y > 0.2f, "The drop over six meters should be clearly visible.");
    }

    private static void SeatedArrowStillReachesMediumTarget()
    {
        var seatedOrigin = new Vector3(0f, ArcheryGeometry.DefaultSeatedEyeHeightMeters, 0f);
        var velocity = Vector3.forward * ArcheryGeometry.MaxLaunchSpeedMetersPerSecond;
        var reached = ArcherySolver.PredictImpactOnPlaneZ(
            seatedOrigin,
            velocity,
            ArcheryGeometry.MediumTargetDistanceMeters,
            ArcheryGeometry.ArrowGravityMetersPerSecondSquared,
            ArcheryGeometry.ArrowLinearDragPerSecond,
            5f,
            out var impact);

        AssertTrue(reached, "A full-draw level shot should reach the medium target plane from seated height.");

        var radialFromCenter = Mathf.Abs(ArcheryGeometry.DefaultSeatedEyeHeightMeters - impact.y);
        AssertTrue(
            radialFromCenter < ArcheryGeometry.TargetFaceRadiusMeters,
            "A full-draw level shot from seated height should land on the target face at medium distance.");
    }

    private static void TargetHeightCalibrationClampsToComfortRange()
    {
        AssertTrue(
            Mathf.Approximately(ArcherySolver.ComputeTargetCenterHeight(0f), ArcheryGeometry.DefaultSeatedEyeHeightMeters),
            "Missing head height should fall back to the seated default.");
        AssertTrue(
            Mathf.Approximately(ArcherySolver.ComputeTargetCenterHeight(0.5f), ArcheryGeometry.MinTargetCenterHeightMeters),
            "Very low head height should clamp to the comfortable minimum.");
        AssertTrue(
            Mathf.Approximately(ArcherySolver.ComputeTargetCenterHeight(2.4f), ArcheryGeometry.MaxTargetCenterHeightMeters),
            "Very high head height should clamp to the comfortable maximum.");
        AssertTrue(
            Mathf.Approximately(ArcherySolver.ComputeTargetCenterHeight(1.25f), 1.25f),
            "Seated head height inside the range should be used as-is.");
    }

    private static void LaneRotationFollowsHeadYawOnly()
    {
        var rotation = ArcherySolver.ComputeLaneRotationFromHeadForward(new Vector3(1f, -0.6f, 1f), Quaternion.identity);
        var forward = rotation * Vector3.forward;

        AssertTrue(Mathf.Abs(forward.y) < 0.001f, "Lane rotation should stay level even when the player looks down.");
        AssertTrue(Vector3.Dot(forward, new Vector3(1f, 0f, 1f).normalized) > 0.99f, "Lane rotation should follow the head yaw.");

        var fallback = ArcherySolver.ComputeLaneRotationFromHeadForward(Vector3.down, Quaternion.Euler(0f, 45f, 0f));
        AssertTrue(Quaternion.Angle(fallback, Quaternion.Euler(0f, 45f, 0f)) < 0.01f, "A vertical head forward should keep the fallback rotation.");
    }

    private static void TargetRegisterHitRaisesScoredEvent()
    {
        var targetObject = new GameObject("ArcheryTestTarget");
        try
        {
            var target = targetObject.AddComponent<ArcheryTarget>();
            target.faceCenter = targetObject.transform;
            target.faceRadiusMeters = ArcheryGeometry.TargetFaceRadiusMeters;
            target.scoreBands = ArcheryGeometry.TargetScoreBands;
            target.maxRingScore = ArcheryGeometry.TargetMaxRingScore;

            var receivedScore = -1;
            var receivedRing = -2;
            System.Action<ArrowHitInfo> handler = info =>
            {
                receivedScore = info.score;
                receivedRing = info.ringIndex;
            };

            ArcheryEvents.OnArrowHit += handler;
            try
            {
                var returnedScore = target.RegisterHit(targetObject.transform.position + targetObject.transform.right * 0.04f, null);
                AssertTrue(returnedScore == 10, "A hit 4cm from the center should score 10.");
                AssertTrue(receivedScore == 10, "The hit event should carry the same score.");
                AssertTrue(receivedRing == 0, "The hit event should report the innermost ring.");
            }
            finally
            {
                ArcheryEvents.OnArrowHit -= handler;
            }
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
        }
    }

    private static void HomeMenuArcheryModuleTogglesGameplayRoots()
    {
        var menuObject = new GameObject("ArcheryTestHomeMenu");
        var homeRoot = new GameObject("ArcheryTestHomeRoot");
        var pingPongRoot = new GameObject("ArcheryTestPingPongRoot");
        var archeryRoot = new GameObject("ArcheryTestArcheryRoot");
        try
        {
            var menu = menuObject.AddComponent<ElderCareHomeMenu>();
            menu.homeRoot = homeRoot;
            menu.pingPongGameplayRoots = new[] { pingPongRoot };
            menu.archeryGameplayRoots = new[] { archeryRoot };
            menu.placeHomeUiOnShow = false;

            menu.SelectModule("archery", "射箭游戏");
            AssertTrue(!homeRoot.activeSelf, "Selecting archery should hide the home page.");
            AssertTrue(archeryRoot.activeSelf, "Selecting archery should activate the archery gameplay root.");
            AssertTrue(!pingPongRoot.activeSelf, "Selecting archery should deactivate ping pong gameplay.");

            menu.SelectModule("pingpong", "健康游戏");
            AssertTrue(pingPongRoot.activeSelf, "Selecting ping pong should activate ping pong gameplay.");
            AssertTrue(!archeryRoot.activeSelf, "Selecting ping pong should deactivate archery gameplay.");

            menu.ShowHome();
            AssertTrue(homeRoot.activeSelf, "Returning home should show the home page.");
            AssertTrue(!archeryRoot.activeSelf, "Returning home should deactivate archery gameplay.");
            AssertTrue(!pingPongRoot.activeSelf, "Returning home should deactivate ping pong gameplay.");
        }
        finally
        {
            Object.DestroyImmediate(menuObject);
            Object.DestroyImmediate(homeRoot);
            Object.DestroyImmediate(pingPongRoot);
            Object.DestroyImmediate(archeryRoot);
        }
    }

    private static void DifficultyDistancesAreOrdered()
    {
        var near = ArcheryGeometry.TargetDistanceForDifficulty(ArcheryDifficulty.Near);
        var medium = ArcheryGeometry.TargetDistanceForDifficulty(ArcheryDifficulty.Medium);
        var far = ArcheryGeometry.TargetDistanceForDifficulty(ArcheryDifficulty.Far);

        AssertTrue(near < medium && medium < far, "Difficulty distances should increase from near to far.");
        AssertTrue(near >= 3f, "Even the near target should sit a comfortable distance away.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
