using System;
using UnityEngine;

public enum ArcheryDifficulty
{
    Near = 0,
    Medium = 1,
    Far = 2
}

public enum ArcheryMissReason
{
    Unknown,
    HitEnvironment,
    FellShort,
    TimedOut
}

public struct ArrowReleasedInfo
{
    public GameObject arrow;
    public Vector3 origin;
    public Vector3 velocity;
    public float draw01;

    public float Speed => velocity.magnitude;

    public ArrowReleasedInfo(GameObject arrow, Vector3 origin, Vector3 velocity, float draw01)
    {
        this.arrow = arrow;
        this.origin = origin;
        this.velocity = velocity;
        this.draw01 = draw01;
    }
}

public struct ArrowHitInfo
{
    public GameObject arrow;
    public int score;
    public int ringIndex;
    public Vector3 hitPoint;
    public float radialDistanceMeters;

    public bool InsideScoringFace => score > 0;

    public ArrowHitInfo(GameObject arrow, int score, int ringIndex, Vector3 hitPoint, float radialDistanceMeters)
    {
        this.arrow = arrow;
        this.score = score;
        this.ringIndex = ringIndex;
        this.hitPoint = hitPoint;
        this.radialDistanceMeters = radialDistanceMeters;
    }
}

public struct ArrowMissedInfo
{
    public GameObject arrow;
    public Vector3 position;
    public ArcheryMissReason reason;

    public ArrowMissedInfo(GameObject arrow, Vector3 position, ArcheryMissReason reason)
    {
        this.arrow = arrow;
        this.position = position;
        this.reason = reason;
    }
}

public struct ArcheryRoundResult
{
    public int totalScore;
    public int arrowsPerRound;
    public int stars;
    public bool isNewBest;

    public ArcheryRoundResult(int totalScore, int arrowsPerRound, int stars, bool isNewBest)
    {
        this.totalScore = totalScore;
        this.arrowsPerRound = arrowsPerRound;
        this.stars = stars;
        this.isNewBest = isNewBest;
    }
}

public static class ArcheryEvents
{
    public static event Action OnArrowNocked;
    public static event Action<float> OnDrawChanged;
    public static event Action<ArrowReleasedInfo> OnArrowReleased;
    public static event Action<ArrowHitInfo> OnArrowHit;
    public static event Action<ArrowMissedInfo> OnArrowMissed;
    public static event Action OnSessionStarted;
    public static event Action OnSessionFinished;
    public static event Action<ArcheryRoundResult> OnRoundFinished;

    public static void ArrowNocked() => OnArrowNocked?.Invoke();
    public static void DrawChanged(float draw01) => OnDrawChanged?.Invoke(draw01);
    public static void ArrowReleased(ArrowReleasedInfo info) => OnArrowReleased?.Invoke(info);
    public static void ArrowHit(ArrowHitInfo info) => OnArrowHit?.Invoke(info);
    public static void ArrowMissed(ArrowMissedInfo info) => OnArrowMissed?.Invoke(info);
    public static void SessionStarted() => OnSessionStarted?.Invoke();
    public static void SessionFinished() => OnSessionFinished?.Invoke();
    public static void RoundFinished(ArcheryRoundResult result) => OnRoundFinished?.Invoke(result);
}
