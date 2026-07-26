using UnityEngine;

public static class DartsGeometry
{
    // 镖体
    public const float DartLengthMeters = 0.16f;
    public const float DartRadiusMeters = 0.008f;
    public const float DartStickDepthMeters = 0.035f;
    public const float DartGravityMetersPerSecondSquared = 9.81f;
    public const float DartLinearDragPerSecond = 0.05f;
    public const float DartMaxFlightSeconds = 4f;

    // 投掷（面向老年用户：轻挥即可出镖，慢速松手视为放回不投出）
    public const float HandSpeedMultiplier = 2.2f;
    public const float MinThrowHandSpeedMetersPerSecond = 1.2f;
    public const float MinDartSpeedMetersPerSecond = 5.5f;
    public const float MaxDartSpeedMetersPerSecond = 10f;
    public const float VelocitySampleWindowSeconds = 0.15f;
    public const float HoldForwardOffsetMeters = 0.05f;

    // 镖盘（5 色环带，由外到内 2/4/6/8/10 环）
    public const float BoardFaceRadiusMeters = 0.3f;
    public const int BoardScoreBands = 5;
    public const int BoardMaxRingScore = 10;
    public const float BoardBackboardSizeMeters = 0.8f;
    public const float BoardBackboardThicknessMeters = 0.06f;

    // 投掷距离（难度：近/标准/远；真实飞镖标准距离 2.37 米）
    public const float NearBoardDistanceMeters = 1.8f;
    public const float StandardBoardDistanceMeters = 2.4f;
    public const float FarBoardDistanceMeters = 3f;

    // 坐姿适配：盘心高度跟随头部高度并限制在舒适区间
    public const float MinBoardCenterHeightMeters = 0.9f;
    public const float MaxBoardCenterHeightMeters = 1.75f;
    public const float DefaultSeatedEyeHeightMeters = 1.2f;

    // 适老辅助：投掷比拉弓更难控制方向，纠偏上限比射箭（4°）更宽
    public const float AimAssistDefaultDegrees = 8f;

    public static float BoardDistanceForDifficulty(DartsDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DartsDifficulty.Near:
                return NearBoardDistanceMeters;
            case DartsDifficulty.Far:
                return FarBoardDistanceMeters;
            default:
                return StandardBoardDistanceMeters;
        }
    }
}
