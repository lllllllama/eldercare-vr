using UnityEngine;

// 弹道积分、环数计分、限角辅助瞄准、坐姿高度校准、朝向对齐等通用数学
// 直接复用 ArcherySolver 的纯函数；这里只放投掷特有的解算。
public static class DartsSolver
{
    public struct ThrowState
    {
        public Vector3 velocity;
        public float handSpeed;
        public bool isThrow;
    }

    public static ThrowState ComputeThrow(
        Vector3 trackedHandVelocity,
        float minThrowHandSpeed,
        float handSpeedMultiplier,
        float minDartSpeed,
        float maxDartSpeed)
    {
        var state = new ThrowState();
        state.handSpeed = trackedHandVelocity.magnitude;

        // 慢速松手视为“把镖放回”，不算投掷——防止点面板按钮时误投。
        if (state.handSpeed < Mathf.Max(0.01f, minThrowHandSpeed))
        {
            state.isThrow = false;
            state.velocity = Vector3.zero;
            return state;
        }

        var direction = trackedHandVelocity / state.handSpeed;

        // 门槛以上线性重映射到 [minDartSpeed, maxDartSpeed]：
        // 若用 乘数+Clamp，门槛~下限之间会出现一段"挥多快都一样"的无反馈死区，
        // 老年玩家无法通过加力修正投低。映射上端取 maxDartSpeed/乘数 对应的手速。
        var safeMultiplier = Mathf.Max(0.1f, handSpeedMultiplier);
        var minSpeed = Mathf.Max(0.1f, minDartSpeed);
        var maxSpeed = Mathf.Max(minSpeed, maxDartSpeed);
        var handSpeedForMax = Mathf.Max(minThrowHandSpeed + 0.1f, maxSpeed / safeMultiplier);
        var speed = Mathf.Lerp(
            minSpeed,
            maxSpeed,
            Mathf.InverseLerp(minThrowHandSpeed, handSpeedForMax, state.handSpeed));

        state.isThrow = true;
        state.velocity = direction * speed;
        return state;
    }

    public static Vector3 ComputeTrackedVelocity(
        Vector3 oldestPosition,
        Vector3 newestPosition,
        float elapsedSeconds)
    {
        if (elapsedSeconds <= 0.0001f) return Vector3.zero;

        return (newestPosition - oldestPosition) / elapsedSeconds;
    }

    public static Vector3 ComputeAssistedVelocityBallistic(
        Vector3 origin,
        Vector3 velocity,
        Vector3 targetCenter,
        float maxCorrectionDegrees,
        float gravityMetersPerSecondSquared)
    {
        // 射箭用的"直线时间"下坠近似在 12-26m/s 速域误差 <2°，但飞镖只有
        // 5.5-10m/s，近似误差高达 4°-15°：吸附到错误的"理想方向"会把玩家
        // 本来正确的抛物线出手拉低到脱靶。这里解精确的无阻力弹道二次方程。
        var speed = velocity.magnitude;
        if (speed < 0.01f || maxCorrectionDegrees <= 0f) return velocity;

        var toTarget = targetCenter - origin;
        var flat = new Vector3(toTarget.x, 0f, toTarget.z);
        var flatDistance = flat.magnitude;
        if (flatDistance < 0.3f) return velocity;

        var g = Mathf.Max(0.01f, gravityMetersPerSecondSquared);
        var speedSquared = speed * speed;
        var discriminant = speedSquared * speedSquared -
                           g * (g * flatDistance * flatDistance + 2f * toTarget.y * speedSquared);
        // 该出手速度物理上够不到盘心：纠偏无意义，保持玩家原方向。
        if (discriminant < 0f) return velocity;

        var tanTheta = (speedSquared - Mathf.Sqrt(discriminant)) / (g * flatDistance);
        var idealDirection = (flat / flatDistance + Vector3.up * tanTheta).normalized;

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

    public static Vector3 SelectReleaseVelocity(
        Vector3 currentWindowVelocity,
        Vector3 peakWindowVelocity,
        float peakAgeSeconds,
        float forgivenessSeconds)
    {
        // 松手瞬间的窗口速度明显低于挥臂峰值、且峰值就发生在刚刚，
        // 说明玩家晚松手：用峰值窗口的速度（方向来自挥臂最快的一段）。
        var currentSpeed = currentWindowVelocity.magnitude;
        var peakSpeed = peakWindowVelocity.magnitude;
        if (peakAgeSeconds <= forgivenessSeconds && peakSpeed > currentSpeed * 1.15f)
        {
            return peakWindowVelocity;
        }

        return currentWindowVelocity;
    }

    public static float ComputeBoardCenterHeight(float headHeightMeters)
    {
        var height = headHeightMeters > 0.05f
            ? headHeightMeters
            : DartsGeometry.DefaultSeatedEyeHeightMeters;
        return Mathf.Clamp(height, DartsGeometry.MinBoardCenterHeightMeters, DartsGeometry.MaxBoardCenterHeightMeters);
    }
}
