using UnityEngine;

public class DartsBoard : MonoBehaviour
{
    public Transform faceCenter;
    public float faceRadiusMeters = DartsGeometry.BoardFaceRadiusMeters;
    public int scoreBands = DartsGeometry.BoardScoreBands;
    public int maxRingScore = DartsGeometry.BoardMaxRingScore;
    public Transform stickParent;

    public Transform StickParent => stickParent != null ? stickParent : transform;

    public int RegisterHit(Vector3 worldHitPoint, GameObject dart)
    {
        var center = faceCenter != null ? faceCenter.position : transform.position;
        var normal = faceCenter != null ? faceCenter.forward : transform.forward;
        var radialDistance = Vector3.ProjectOnPlane(worldHitPoint - center, normal).magnitude;

        var score = ArcherySolver.ScoreForRadialDistance(radialDistance, faceRadiusMeters, scoreBands, maxRingScore);
        var ringIndex = ArcherySolver.RingIndexForRadialDistance(radialDistance, faceRadiusMeters, scoreBands);

        DartsEvents.DartHit(new DartHitInfo(dart, score, ringIndex, worldHitPoint, radialDistance));
        return score;
    }
}
