using UnityEngine;

namespace PicoElderCare.UI
{
    public static class ElderCareUiTheme
    {
        public static readonly Color Background = new Color(0.025f, 0.038f, 0.052f, 1f);
        public static readonly Color Panel = new Color(0.105f, 0.155f, 0.19f, 0.98f);
        public static readonly Color PanelStrong = new Color(0.075f, 0.13f, 0.17f, 0.99f);
        public static readonly Color PanelStroke = new Color(0.38f, 0.92f, 1f, 0.68f);
        public static readonly Color Cyan = new Color(0.38f, 0.92f, 1f, 1f);
        public static readonly Color Blue = new Color(0.18f, 0.46f, 0.91f, 1f);
        public static readonly Color Green = new Color(0.15f, 0.66f, 0.34f, 1f);
        public static readonly Color Violet = new Color(0.55f, 0.29f, 0.89f, 1f);
        public static readonly Color Orange = new Color(0.91f, 0.42f, 0.12f, 1f);
        public static readonly Color Red = new Color(1f, 0.42f, 0.36f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 1f);
        public static readonly Color TextPrimary = new Color(1f, 1f, 1f, 0.98f);
        public static readonly Color TextSecondary = new Color(1f, 1f, 1f, 0.78f);
        public static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.62f);
        public static readonly Color Disabled = new Color(1f, 1f, 1f, 0.46f);

        public const float TitleLarge = 74f;
        public const float Title = 44f;
        public const float Subtitle = 34f;
        public const float Body = 28f;
        public const float BodySmall = 22f;
        public const float Button = 30f;
        public const float HudPrimary = 52f;
        public const float HudSecondary = 28f;
        public const float Debug = 20f;

        public static readonly Vector2 MainEntryCanvasSize = new Vector2(680f, 432f);
        public static readonly Vector2 MainEntryCardSize = new Vector2(292f, 142f);
        public static readonly Vector2 RehabCanvasSize = new Vector2(680f, 432f);
        public static readonly Vector2 RehabButtonSize = new Vector2(360f, 76f);
        public static readonly Vector2 PingPongHudSize = new Vector2(680f, 432f);
        public const float MinButtonHeightForElderly = 68f;

        public const float MainEntryDistanceMeters = 2.2f;
        public const float RehabUiDistanceMeters = 2.45f;
        public const float HudDistanceMeters = 2.35f;
        public const float DefaultUiHeightOffsetMeters = -0.1f;

        public const float HoverScale = 1.05f;
        public const float PressedScale = 0.96f;
        public const float DisabledAlpha = 0.62f;
    }
}
