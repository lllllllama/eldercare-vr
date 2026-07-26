using UnityEngine;

public class ArcheryTarget : MonoBehaviour
{
    public Transform faceCenter;
    public float faceRadiusMeters = ArcheryGeometry.TargetFaceRadiusMeters;
    public int scoreBands = ArcheryGeometry.TargetScoreBands;
    public int maxRingScore = ArcheryGeometry.TargetMaxRingScore;
    public Transform stickParent;

    public Transform StickParent => stickParent != null ? stickParent : transform;

    public int RegisterHit(Vector3 worldHitPoint, GameObject arrow)
    {
        var center = faceCenter != null ? faceCenter.position : transform.position;
        var normal = faceCenter != null ? faceCenter.forward : transform.forward;
        var radialDistance = Vector3.ProjectOnPlane(worldHitPoint - center, normal).magnitude;

        var score = ArcherySolver.ScoreForRadialDistance(radialDistance, faceRadiusMeters, scoreBands, maxRingScore);
        var ringIndex = ArcherySolver.RingIndexForRadialDistance(radialDistance, faceRadiusMeters, scoreBands);

        ArcheryEvents.ArrowHit(new ArrowHitInfo(arrow, score, ringIndex, worldHitPoint, radialDistance));
        return score;
    }
}
