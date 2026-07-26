using UnityEngine;

public static class ArcheryGeometry
{
    // 拉弓（面向老年用户：拉距短、起射门槛低）
    public const float DrawRestSeparationMeters = 0.1f;
    public const float MaxDrawLengthMeters = 0.45f;
    public const float MinFireDraw01 = 0.16f;
    public const float NockCatchRadiusMeters = 0.28f;

    // 箭
    public const float ArrowLengthMeters = 0.6f;
    public const float ArrowRadiusMeters = 0.012f;
    public const float ArrowStickDepthMeters = 0.09f;
    public const float MinLaunchSpeedMetersPerSecond = 12f;
    public const float MaxLaunchSpeedMetersPerSecond = 26f;
    public const float ArrowGravityMetersPerSecondSquared = 9.81f;
    public const float ArrowLinearDragPerSecond = 0.06f;
    public const float ArrowMaxFlightSeconds = 8f;

    // 靶面（5 色环带，由外到内 2/4/6/8/10 环）
    public const float TargetFaceRadiusMeters = 0.4f;
    public const int TargetScoreBands = 5;
    public const int TargetMaxRingScore = 10;
    public const float TargetBackboardSizeMeters = 1f;
    public const float TargetBackboardThicknessMeters = 0.06f;

    // 箭道距离（难度：近/中/远）
    public const float NearTargetDistanceMeters = 4f;
    public const float MediumTargetDistanceMeters = 6f;
    public const float FarTargetDistanceMeters = 8.5f;

    // 坐姿适配：靶心高度跟随头部高度并限制在舒适区间
    public const float MinTargetCenterHeightMeters = 0.9f;
    public const float MaxTargetCenterHeightMeters = 1.75f;
    public const float DefaultSeatedEyeHeightMeters = 1.2f;

    // 适老辅助：防手抖平滑、限角度辅助瞄准、弹道预览
    public const float AimSmoothingSeconds = 0.06f;
    public const float AimAssistDefaultDegrees = 4f;
    public const float AimAssistMaxAngleFromIdealDegrees = 30f;
    public const float TrajectoryPreviewStepSeconds = 0.033f;
    public const float TrajectoryPreviewMaxSeconds = 2.5f;
    public const int TrajectoryPreviewPointCapacity = 60;

    // 手感：弓臂弯曲与震动
    public const float BowLimbBendDegrees = 14f;

    public static float TargetDistanceForDifficulty(ArcheryDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ArcheryDifficulty.Near:
                return NearTargetDistanceMeters;
            case ArcheryDifficulty.Far:
                return FarTargetDistanceMeters;
            default:
                return MediumTargetDistanceMeters;
        }
    }
}
