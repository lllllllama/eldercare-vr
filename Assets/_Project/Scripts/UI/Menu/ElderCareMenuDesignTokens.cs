using UnityEngine;

namespace PicoElderCare.UI
{
    public static class ElderCareMenuDesignTokens
    {
        public static readonly Color Wood = new Color32(0xD9, 0xC7, 0xA3, 0xFF);
        public static readonly Color WoodDark = new Color32(0xC6, 0xB0, 0x85, 0xFF);
        public static readonly Color RiceLight = new Color32(0xFB, 0xF5, 0xE6, 0xFF);
        public static readonly Color RiceMid = new Color32(0xF1, 0xE6, 0xCC, 0xFF);
        public static readonly Color Card = new Color32(0xF7, 0xEF, 0xD9, 0xFF);
        public static readonly Color CardHighlight = new Color32(0xFF, 0xF8, 0xE4, 0xFF);
        public static readonly Color GoldStroke = new Color32(0xC9, 0xA9, 0x6A, 0xFF);
        public static readonly Color GoldDeep = new Color32(0xB5, 0x73, 0x27, 0xFF);
        public static readonly Color Amber = new Color32(0xD4, 0x8F, 0x3A, 0xFF);
        public static readonly Color AmberLight = new Color32(0xE8, 0xB2, 0x69, 0xFF);
        public static readonly Color Jade = new Color32(0x5F, 0x85, 0x60, 0xFF);
        public static readonly Color Coral = new Color32(0xB9, 0x68, 0x55, 0xFF);
        public static readonly Color TextPrimary = new Color32(0x3E, 0x2E, 0x1F, 0xFF);
        public static readonly Color TextSecondary = new Color32(0x6A, 0x55, 0x3E, 0xFF);
        public static readonly Color TextMuted = new Color32(0x9A, 0x85, 0x60, 0xFF);
        public static readonly Color WarmShadow = new Color(0.25f, 0.16f, 0.06f, 0.23f);

        public static readonly Vector2 SecondaryCanvasSize = new Vector2(900f, 560f);
        public static readonly Vector2 SecondaryThreeCardSize = new Vector2(250f, 312f);
        public static readonly Vector2 SecondaryTwoCardSize = new Vector2(340f, 312f);
        public static readonly Vector2 SecondaryDockSize = new Vector2(824f, 72f);
        public static readonly Vector2 SecondaryBackButtonSize = new Vector2(210f, 68f);

        public static readonly Vector2 MainEntryCanvasSize = new Vector2(1120f, 680f);
        public static readonly Vector2 MainEntryPanelSize = new Vector2(1040f, 468f);
        public static readonly Vector2 MainEntryCardSize = new Vector2(220f, 238f);
        public static readonly Vector2 MainEntrySafeButtonSize = new Vector2(190f, 56f);

        public const float SecondaryCanvasWorldScale = 0.00165f;
        public const float RehabSelectionRootScale = 0.825f;
        public const float PanelRadius = 44f;
        public const float CardRadius = 28f;
        public const float IconHaloSize = 98f;
        public const float HeroIconSize = 72f;
        public const float MetadataHeight = 32f;
        public const float ChoiceCtaHeight = 68f;
        public const float HoverScale = 1.05f;
        public const float PressedScale = 0.96f;
        public const float HoverLiftY = 6f;

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
