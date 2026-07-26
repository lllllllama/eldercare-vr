using UnityEngine;

public static class ArcherySolver
{
    public struct DrawState
    {
        public Vector3 aimDirection;
        public float drawLengthMeters;
        public float draw01;
        public bool canFire;
    }

    public static DrawState ComputeDraw(
        Vector3 bowHandPosition,
        Vector3 stringHandPosition,
        Vector3 fallbackAimDirection,
        float restSeparationMeters,
        float maxDrawLengthMeters,
        float minFireDraw01)
    {
        var state = new DrawState();
        var offset = bowHandPosition - stringHandPosition;
        var distance = offset.magnitude;

        state.aimDirection = distance > 0.001f
            ? offset / distance
            : (fallbackAimDirection.sqrMagnitude > 0.001f ? fallbackAimDirection.normalized : Vector3.forward);

        var usableMaxDraw = Mathf.Max(0.01f, maxDrawLengthMeters);
        state.drawLengthMeters = Mathf.Clamp(distance - restSeparationMeters, 0f, usableMaxDraw);
        state.draw01 = Mathf.Clamp01(state.drawLengthMeters / usableMaxDraw);
        state.canFire = state.draw01 >= Mathf.Clamp01(minFireDraw01);
        return state;
    }

    public static float ComputeLaunchSpeed(float draw01, float minSpeed, float maxSpeed)
    {
        return Mathf.Lerp(Mathf.Max(0f, minSpeed), Mathf.Max(minSpeed, maxSpeed), Mathf.Clamp01(draw01));
    }

    public static Vector3 ComputeReleaseVelocity(DrawState state, float minSpeed, float maxSpeed)
    {
        if (!state.canFire) return Vector3.zero;

        return state.aimDirection.normalized * ComputeLaunchSpeed(state.draw01, minSpeed, maxSpeed);
    }

    public static int RingIndexForRadialDistance(float radialDistanceMeters, float faceRadiusMeters, int scoreBands)
    {
        if (scoreBands <= 0 || faceRadiusMeters <= 0f) return -1;
        if (radialDistanceMeters < 0f || radialDistanceMeters > faceRadiusMeters) return -1;

        var bandWidth = faceRadiusMeters / scoreBands;
        return Mathf.Min(scoreBands - 1, Mathf.FloorToInt(radialDistanceMeters / bandWidth));
    }

    public static int ScoreForRadialDistance(float radialDistanceMeters, float faceRadiusMeters, int scoreBands, int maxRingScore)
    {
        var ringIndex = RingIndexForRadialDistance(radialDistanceMeters, faceRadiusMeters, scoreBands);
        if (ringIndex < 0) return 0;

        var step = scoreBands > 1 ? Mathf.Max(1, maxRingScore / scoreBands) : maxRingScore;
        return Mathf.Max(0, maxRingScore - ringIndex * step);
    }

    public static void SimulateArrowStep(
        ref Vector3 position,
        ref Vector3 velocity,
        float gravityMetersPerSecondSquared,
        float linearDragPerSecond,
        float deltaSeconds)
    {
        velocity += Vector3.down * (gravityMetersPerSecondSquared * deltaSeconds);
        velocity *= Mathf.Clamp01(1f - linearDragPerSecond * deltaSeconds);
        position += velocity * deltaSeconds;
    }

    public static bool PredictImpactOnPlaneZ(
        Vector3 origin,
        Vector3 velocity,
        float planeZ,
        float gravityMetersPerSecondSquared,
        float linearDragPerSecond,
        float maxSeconds,
        out Vector3 impactPoint)
    {
        impactPoint = origin;
        if (maxSeconds <= 0f) return false;

        const float step = 0.005f;
        var position = origin;
        var elapsed = 0f;
        while (elapsed < maxSeconds)
        {
            var previous = position;
            SimulateArrowStep(ref position, ref velocity, gravityMetersPerSecondSquared, linearDragPerSecond, step);
            elapsed += step;

            var travel = position.z - previous.z;
            if (Mathf.Abs(travel) > Mathf.Epsilon &&
                (previous.z - planeZ) * (position.z - planeZ) <= 0f &&
                travel != 0f)
            {
                var t = Mathf.Clamp01((planeZ - previous.z) / travel);
                impactPoint = Vector3.Lerp(previous, position, t);
                return true;
            }
        }

        return false;
    }

    public static Vector3 ComputeAssistedVelocity(
        Vector3 origin,
        Vector3 velocity,
        Vector3 targetCenter,
        float maxCorrectionDegrees,
        float gravityMetersPerSecondSquared)
    {
        var speed = velocity.magnitude;
        if (speed < 0.01f || maxCorrectionDegrees <= 0f) return velocity;

        var toTarget = targetCenter - origin;
        var distance = toTarget.magnitude;
        if (distance < 0.5f) return velocity;

        // 用无阻力近似补偿重力下坠，得到指向“抬高后的瞄准点”的理想方向。
        var flightSeconds = distance / speed;
        var dropCompensation = 0.5f * gravityMetersPerSecondSquared * flightSeconds * flightSeconds;
        var idealDirection = (targetCenter + Vector3.up * dropCompensation - origin).normalized;

        var currentDirection = velocity / speed;
        if (Vector3.Angle(currentDirection, idealDirection) > ArcheryGeometry.AimAssistMaxAngleFromIdealDegrees)
        {
            return velocity;
        }

        var assistedDirection = Vector3.RotateTowards(
            currentDirection,
            idealDirection,
            maxCorrectionDegrees * Mathf.Deg2Rad,
            0f);
        return assistedDirection * speed;
    }

    public static int ComputeStarRating(int totalScore, int arrowsPerRound, int maxRingScore)
    {
        var maxScore = Mathf.Max(1, arrowsPerRound * maxRingScore);
        var ratio = Mathf.Clamp01((float)totalScore / maxScore);
        if (ratio >= 0.9f) return 5;
        if (ratio >= 0.7f) return 4;
        if (ratio >= 0.5f) return 3;
        if (ratio >= 0.3f) return 2;
        return totalScore > 0 ? 1 : 0;
    }

    public static int SampleTrajectory(
        Vector3 origin,
        Vector3 velocity,
        float gravityMetersPerSecondSquared,
        float linearDragPerSecond,
        float stepSeconds,
        float maxSeconds,
        Vector3[] buffer)
    {
        if (buffer == null || buffer.Length == 0 || stepSeconds <= 0f) return 0;

        var position = origin;
        var count = 0;
        var elapsed = 0f;
        buffer[count++] = position;
        while (count < buffer.Length && elapsed < maxSeconds)
        {
            SimulateArrowStep(ref position, ref velocity, gravityMetersPerSecondSquared, linearDragPerSecond, stepSeconds);
            elapsed += stepSeconds;
            buffer[count++] = position;
            if (position.y < 0f) break;
        }

        return count;
    }

    public static float ComputeTargetCenterHeight(float headHeightMeters)
    {
        var height = headHeightMeters > 0.05f
            ? headHeightMeters
            : ArcheryGeometry.DefaultSeatedEyeHeightMeters;
        return Mathf.Clamp(height, ArcheryGeometry.MinTargetCenterHeightMeters, ArcheryGeometry.MaxTargetCenterHeightMeters);
    }

    public static Quaternion ComputeLaneRotationFromHeadForward(Vector3 headForward, Quaternion fallbackRotation)
    {
        var flatForward = new Vector3(headForward.x, 0f, headForward.z);
        if (flatForward.sqrMagnitude < 0.0001f) return fallbackRotation;

        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }
}
