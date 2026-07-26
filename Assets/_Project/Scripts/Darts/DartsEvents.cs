using System;
using UnityEngine;

public enum DartsDifficulty
{
    Near = 0,
    Standard = 1,
    Far = 2
}

public enum DartsMissReason
{
    Unknown,
    HitEnvironment,
    FellShort,
    TimedOut
}

public struct DartThrownInfo
{
    public GameObject dart;
    public Vector3 origin;
    public Vector3 velocity;
    public float handSpeed;

    public float Speed => velocity.magnitude;

    public DartThrownInfo(GameObject dart, Vector3 origin, Vector3 velocity, float handSpeed)
    {
        this.dart = dart;
        this.origin = origin;
        this.velocity = velocity;
        this.handSpeed = handSpeed;
    }
}

public struct DartHitInfo
{
    public GameObject dart;
    public int score;
    public int ringIndex;
    public Vector3 hitPoint;
    public float radialDistanceMeters;

    public bool InsideScoringFace => score > 0;

    public DartHitInfo(GameObject dart, int score, int ringIndex, Vector3 hitPoint, float radialDistanceMeters)
    {
        this.dart = dart;
        this.score = score;
        this.ringIndex = ringIndex;
        this.hitPoint = hitPoint;
        this.radialDistanceMeters = radialDistanceMeters;
    }
}

public struct DartMissedInfo
{
    public GameObject dart;
    public Vector3 position;
    public DartsMissReason reason;

    public DartMissedInfo(GameObject dart, Vector3 position, DartsMissReason reason)
    {
        this.dart = dart;
        this.position = position;
        this.reason = reason;
    }
}

public struct DartsRoundResult
{
    public int totalScore;
    public int dartsPerRound;
    public int stars;
    public bool isNewBest;

    public DartsRoundResult(int totalScore, int dartsPerRound, int stars, bool isNewBest)
    {
        this.totalScore = totalScore;
        this.dartsPerRound = dartsPerRound;
        this.stars = stars;
        this.isNewBest = isNewBest;
    }
}

public static class DartsEvents
{
    public static event Action OnDartGrabbed;
    public static event Action OnDartHoldCancelled;
    public static event Action<DartThrownInfo> OnDartThrown;
    public static event Action<DartHitInfo> OnDartHit;
    public static event Action<DartMissedInfo> OnDartMissed;
    public static event Action OnSessionStarted;
    public static event Action OnSessionFinished;
    public static event Action<DartsRoundResult> OnRoundFinished;

    public static void DartGrabbed() => OnDartGrabbed?.Invoke();
    public static void DartHoldCancelled() => OnDartHoldCancelled?.Invoke();
    public static void DartThrown(DartThrownInfo info) => OnDartThrown?.Invoke(info);
    public static void DartHit(DartHitInfo info) => OnDartHit?.Invoke(info);
    public static void DartMissed(DartMissedInfo info) => OnDartMissed?.Invoke(info);
    public static void SessionStarted() => OnSessionStarted?.Invoke();
    public static void SessionFinished() => OnSessionFinished?.Invoke();
    public static void RoundFinished(DartsRoundResult result) => OnRoundFinished?.Invoke(result);
}
