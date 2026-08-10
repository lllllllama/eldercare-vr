using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.HealthGame
{
    public static class HealthGameMenuVisualSkin
    {
        public static readonly Vector2 CanvasSize = new Vector2(900f, 560f);
        public static readonly Vector2 SportCardSize = new Vector2(250f, 312f);
        public const float CanvasWorldScale = 0.00165f;

        private static readonly Color Wood = new Color32(0xD9, 0xC7, 0xA3, 0xFF);
        private static readonly Color WoodDark = new Color32(0xC6, 0xB0, 0x85, 0xFF);
        private static readonly Color RiceLight = new Color32(0xFB, 0xF5, 0xE6, 0xFF);
        private static readonly Color RiceMid = new Color32(0xF1, 0xE6, 0xCC, 0xFF);
        private static readonly Color Card = new Color32(0xF7, 0xEF, 0xD9, 0xFF);
        private static readonly Color CardHighlight = new Color32(0xFF, 0xF8, 0xE4, 0xFF);
        private static readonly Color GoldStroke = new Color32(0xC9, 0xA9, 0x6A, 0xFF);
        private static readonly Color GoldDeep = new Color32(0xB5, 0x73, 0x27, 0xFF);
        private static readonly Color Amber = new Color32(0xD4, 0x8F, 0x3A, 0xFF);
        private static readonly Color Jade = new Color32(0x5F, 0x85, 0x60, 0xFF);
        private static readonly Color Coral = new Color32(0xB9, 0x68, 0x55, 0xFF);
        private static readonly Color TextPrimary = new Color32(0x3E, 0x2E, 0x1F, 0xFF);
        private static readonly Color TextSecondary = new Color32(0x6A, 0x55, 0x3E, 0xFF);
        private static readonly Color WarmShadow = new Color(0.25f, 0.16f, 0.06f, 0.23f);

        public struct MenuElements
        {
            public GameObject panel;
            public Button pingPongButton;
            public Button archeryButton;
            public Button dartsButton;
            public Button backButton;
        }

        public static MenuElements Build(
            Transform canvas,
            TMP_FontAsset font,
            Sprite pingPongIcon,
            Sprite archeryIcon,
            Sprite dartsIcon,
            Sprite backIcon,
            Sprite clockIcon,
            Sprite playIcon)
        {
            var panel = CreateRect("Panel", canvas, CanvasSize, Vector2.zero);
            ConfigureStretch(panel);

            BuildPanelFrame(panel);
            BuildHeader(panel, font);

            var cards = CreateRect("SportCards", panel, new Vector2(824f, 326f), new Vector2(0f, -25f));
            var pingPongButton = BuildSportCard(
                cards,
                "PingPongCard",
                new Vector2(-274f, 0f),
                "乒乓球",
                "灵活反应 · 活动身体",
                "约 8 分钟",
                "强度轻",
                pingPongIcon,
                clockIcon,
                playIcon,
                Jade,
                true);
            var archeryButton = BuildSportCard(
                cards,
                "ArcheryCard",
                Vector2.zero,
                "射箭",
                "稳定瞄准 · 锻炼协调",
                "约 10 分钟",
                "强度中",
                archeryIcon,
                clockIcon,
                playIcon,
                Amber,
                false);
            var dartsButton = BuildSportCard(
                cards,
                "DartsCard",
                new Vector2(274f, 0f),
                "飞镖",
                "专注投掷 · 轻松挑战",
                "约 8 分钟",
                "强度轻",
                dartsIcon,
                clockIcon,
                playIcon,
                Coral,
                false);

            ApplyFont(pingPongButton.transform, font);
            ApplyFont(archeryButton.transform, font);
            ApplyFont(dartsButton.transform, font);

            var backButton = BuildBottomDock(panel, font, backIcon);
            return new MenuElements
            {
                panel = panel.gameObject,
                pingPongButton = pingPongButton,
                archeryButton = archeryButton,
                dartsButton = dartsButton,
                backButton = backButton
            };
        }

        private static void BuildPanelFrame(RectTransform panel)
        {
            var root = CreateRect("VisualRoot", panel, CanvasSize, Vector2.zero);
            ConfigureStretch(root);
            CreateRounded(root, "Shadow", new Vector2(866f, 526f), new Vector2(0f, -8f), WarmShadow, 42f, Color.clear, Vector2.zero);
            CreateRounded(root, "WoodFrame", new Vector2(892f, 552f), Vector2.zero, Wood, 44f, WithAlpha(WoodDark, 0.48f), new Vector2(2f, -2f));
            CreateRounded(root, "WoodWarmLayer", new Vector2(876f, 536f), new Vector2(0f, 1f), WithAlpha(RiceMid, 0.26f), 38f, Color.clear, Vector2.zero);
            CreateRounded(root, "WoodStripeLeft", new Vector2(10f, 504f), new Vector2(-420f, 0f), WithAlpha(WoodDark, 0.34f), 5f, Color.clear, Vector2.zero);
            CreateRounded(root, "WoodStripeRight", new Vector2(9f, 500f), new Vector2(420f, 0f), WithAlpha(WoodDark, 0.26f), 4.5f, Color.clear, Vector2.zero);
            CreateRounded(root, "RicePaperPanel", new Vector2(852f, 512f), new Vector2(0f, 5f), RiceLight, 34f, WithAlpha(GoldStroke, 0.72f), new Vector2(1.5f, -1.5f));
            CreateRounded(root, "RiceWarmEdge", new Vector2(826f, 486f), new Vector2(0f, 4f), WithAlpha(RiceMid, 0.28f), 28f, WithAlpha(GoldStroke, 0.16f), new Vector2(0.8f, -0.8f));
            CreateRounded(root, "PaperGrainA", new Vector2(8f, 8f), new Vector2(-298f, 176f), WithAlpha(GoldStroke, 0.16f), 4f, Color.clear, Vector2.zero);
            CreateRounded(root, "PaperGrainB", new Vector2(6f, 6f), new Vector2(258f, 132f), WithAlpha(GoldStroke, 0.13f), 3f, Color.clear, Vector2.zero);
            CreateRounded(root, "PaperGrainC", new Vector2(7f, 7f), new Vector2(-92f, -148f), WithAlpha(GoldStroke, 0.12f), 3.5f, Color.clear, Vector2.zero);
        }

        private static void BuildHeader(RectTransform panel, TMP_FontAsset font)
        {
            var header = CreateRect("Header", panel, new Vector2(800f, 104f), new Vector2(0f, 193f));
            CreateText(header, "Title", "请选择健康运动类型", new Vector2(790f, 54f), new Vector2(0f, 20f), 42f, FontStyles.Bold, TextPrimary, font);
            CreateText(header, "Subtitle", "选择喜欢的运动，活动身体、轻松锻炼", new Vector2(760f, 32f), new Vector2(0f, -27f), 20f, FontStyles.Bold, TextSecondary, font);
            CreateRounded(header, "Divider", new Vector2(420f, 3f), new Vector2(0f, -51f), WithAlpha(GoldStroke, 0.58f), 1.5f, Color.clear, Vector2.zero);
        }

        private static Button BuildSportCard(
            RectTransform parent,
            string name,
            Vector2 position,
            string title,
            string subtitle,
            string duration,
            string intensity,
            Sprite sportIcon,
            Sprite clockIcon,
            Sprite playIcon,
            Color accent,
            bool recommended)
        {
            var card = CreateRect(name, parent, SportCardSize, position);
            var target = card.gameObject.AddComponent<ElderCareRoundedPanel>();
            target.cornerRadius = 28f;
            target.cornerSegments = 12;
            target.color = new Color(1f, 1f, 1f, 0.012f);
            target.raycastTarget = true;

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            var buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = Color.white;
            buttonColors.selectedColor = Color.white;
            buttonColors.pressedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            buttonColors.disabledColor = new Color(1f, 1f, 1f, 0.62f);
            buttonColors.fadeDuration = 0.12f;
            button.colors = buttonColors;

            var content = CreateRect("Content", card, SportCardSize, Vector2.zero);
            ConfigureStretch(content);
            var glow = CreateRounded(content, "HoverGlow", SportCardSize + new Vector2(20f, 18f), new Vector2(0f, -1f), WithAlpha(accent, 0.025f), 31f, Color.clear, Vector2.zero);
            CreateRounded(content, "Shadow", SportCardSize + new Vector2(14f, 14f), new Vector2(0f, -7f), WarmShadow, 30f, Color.clear, Vector2.zero);
            var surface = CreateRounded(content, "Background", SportCardSize - new Vector2(2f, 2f), Vector2.zero, recommended ? CardHighlight : Card, 27f, WithAlpha(recommended ? Amber : GoldStroke, 0.72f), new Vector2(1.5f, -1.5f));
            CreateRounded(content, "InnerRice", SportCardSize - new Vector2(18f, 18f), Vector2.zero, WithAlpha(RiceLight, 0.34f), 22f, WithAlpha(GoldStroke, 0.14f), new Vector2(0.7f, -0.7f));
            var edge = CreateRounded(content, "SideAccent", new Vector2(5f, 240f), new Vector2(-110f, -5f), WithAlpha(accent, 0.48f), 2.5f, Color.clear, Vector2.zero);

            var iconPosition = new Vector2(0f, 88f);
            CreateRounded(content, "IconHalo", new Vector2(98f, 98f), iconPosition, WithAlpha(accent, recommended ? 0.20f : 0.15f), 49f, Color.clear, Vector2.zero);
            var iconContainer = CreateRounded(content, "IconContainer", new Vector2(82f, 82f), iconPosition, WithAlpha(RiceLight, 0.97f), 41f, WithAlpha(accent, 0.46f), new Vector2(1f, -1f));
            CreateSpriteIcon(iconContainer.rectTransform, "SportIcon", sportIcon, new Vector2(72f, 72f), Vector2.zero, Color.white);

            var ribbon = CreateRounded(content, "RecommendationRibbon", new Vector2(94f, 26f), new Vector2(-70f, 136f), Amber, 13f, WithAlpha(GoldDeep, 0.50f), new Vector2(1f, -1f));
            CreateText(ribbon.rectTransform, "Label", "今日推荐", new Vector2(88f, 24f), Vector2.zero, 13f, FontStyles.Bold, RiceLight, null);
            ribbon.gameObject.SetActive(recommended);

            CreateText(content, "Title", title, new Vector2(220f, 38f), new Vector2(0f, 25f), 30f, FontStyles.Bold, TextPrimary, null);
            CreateText(content, "Subtitle", subtitle, new Vector2(224f, 28f), new Vector2(0f, -10f), 17f, FontStyles.Bold, TextSecondary, null);

            var metadata = CreateRect("Metadata", content, new Vector2(224f, 34f), new Vector2(0f, -51f));
            var durationPill = CreateRounded(metadata, "DurationPill", new Vector2(120f, 32f), new Vector2(-52f, 0f), WithAlpha(RiceMid, 0.94f), 16f, WithAlpha(GoldStroke, 0.42f), new Vector2(0.7f, -0.7f));
            CreateSpriteIcon(durationPill.rectTransform, "ClockIcon", clockIcon, new Vector2(18f, 18f), new Vector2(-43f, 0f), Color.white);
            CreateText(durationPill.rectTransform, "Label", duration, new Vector2(88f, 26f), new Vector2(10f, 0f), 15f, FontStyles.Bold, TextPrimary, null);
            var intensityPill = CreateRounded(metadata, "IntensityPill", new Vector2(94f, 32f), new Vector2(62f, 0f), WithAlpha(accent, 0.16f), 16f, WithAlpha(accent, 0.48f), new Vector2(0.7f, -0.7f));
            CreateText(intensityPill.rectTransform, "Label", intensity, new Vector2(88f, 26f), Vector2.zero, 15f, FontStyles.Bold, TextPrimary, null);

            var startFill = Color.Lerp(RiceLight, accent, 0.48f);
            var startButton = CreateRounded(content, "StartButtonVisual", new Vector2(210f, 68f), new Vector2(0f, -113f), startFill, 24f, WithAlpha(accent, 0.76f), new Vector2(1.2f, -1.2f));
            CreateSpriteIcon(startButton.rectTransform, "PlayIcon", playIcon, new Vector2(22f, 22f), new Vector2(-52f, 0f), Color.white);
            CreateText(startButton.rectTransform, "Label", "开始运动", new Vector2(120f, 34f), new Vector2(15f, 0f), 21f, FontStyles.Bold, TextPrimary, null);

            var group = card.gameObject.AddComponent<CanvasGroup>();
            var motion = card.gameObject.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = card;
            motion.canvasGroup = group;
            motion.cardGraphic = surface;
            motion.glowGraphic = glow;
            motion.edgeGraphic = edge;
            motion.interactable = true;
            motion.playEntrance = false;
            motion.hoverScale = PicoElderCare.UI.ElderCareUiTheme.HoverScale;
            motion.pressedScale = PicoElderCare.UI.ElderCareUiTheme.PressedScale;
            motion.selectedScale = 1.035f;
            motion.hoverLiftY = 6f;
            motion.selectedLiftY = 4f;
            motion.ambientMotion = false;
            motion.ambientFloatY = 0f;
            motion.animationSpeed = 10f;
            motion.normalColor = recommended ? CardHighlight : Card;
            motion.hoverColor = Color.Lerp(recommended ? CardHighlight : Card, accent, 0.08f);
            motion.pressedColor = Color.Lerp(recommended ? CardHighlight : Card, accent, 0.14f);
            motion.glowColor = WithAlpha(accent, 0.16f);
            motion.edgeColor = WithAlpha(accent, 0.84f);
            return button;
        }

        private static Button BuildBottomDock(RectTransform panel, TMP_FontAsset font, Sprite backIcon)
        {
            var dock = CreateRounded(panel, "BottomDock", new Vector2(824f, 72f), new Vector2(0f, -230f), WithAlpha(RiceMid, 0.90f), 28f, WithAlpha(GoldStroke, 0.46f), new Vector2(1f, -1f));
            CreateText(dock.rectTransform, "Hint", "将射线停留在运动卡片上即可选择", new Vector2(430f, 28f), new Vector2(118f, 0f), 16f, FontStyles.Bold, TextSecondary, font);

            var backRect = CreateRect("BackButton", dock.rectTransform, new Vector2(210f, 68f), new Vector2(-292f, 0f));
            var target = backRect.gameObject.AddComponent<ElderCareRoundedPanel>();
            target.cornerRadius = 26f;
            target.cornerSegments = 10;
            target.color = Card;
            target.raycastTarget = true;
            var outline = backRect.gameObject.AddComponent<Outline>();
            outline.effectColor = WithAlpha(GoldStroke, 0.66f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var button = backRect.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, RiceMid, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(Color.white, WoodDark, 0.16f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.62f);
            button.colors = colors;

            CreateSpriteIcon(backRect, "ArrowIcon", backIcon, new Vector2(25f, 25f), new Vector2(-70f, 0f), Color.white);
            CreateText(backRect, "Label", "返回上一页", new Vector2(140f, 34f), new Vector2(18f, 0f), 20f, FontStyles.Bold, TextPrimary, font);

            var group = backRect.gameObject.AddComponent<CanvasGroup>();
            var motion = backRect.gameObject.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = backRect;
            motion.canvasGroup = group;
            motion.cardGraphic = target;
            motion.interactable = true;
            motion.playEntrance = false;
            motion.hoverScale = 1.025f;
            motion.pressedScale = PicoElderCare.UI.ElderCareUiTheme.PressedScale;
            motion.hoverLiftY = 3f;
            motion.ambientMotion = false;
            motion.normalColor = Card;
            motion.hoverColor = CardHighlight;
            motion.pressedColor = RiceMid;
            ApplyFont(backRect, font);
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static ElderCareRoundedPanel CreateRounded(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 position,
            Color color,
            float radius,
            Color outlineColor,
            Vector2 outlineDistance)
        {
            var rect = CreateRect(name, parent, size, position);
            var panel = rect.gameObject.AddComponent<ElderCareRoundedPanel>();
            panel.cornerRadius = radius;
            panel.cornerSegments = 12;
            panel.color = color;
            panel.raycastTarget = false;
            if (outlineColor.a > 0.001f)
            {
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = outlineDistance;
                outline.useGraphicAlpha = true;
            }

            return panel;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string text,
            Vector2 size,
            Vector2 position,
            float fontSize,
            FontStyles style,
            Color color,
            TMP_FontAsset font)
        {
            var rect = CreateRect(name, parent, size, position);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            if (font != null) label.font = font;
            return label;
        }

        private static void CreateSpriteIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            var rect = CreateRect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void ApplyFont(Transform root, TMP_FontAsset font)
        {
            if (root == null || font == null) return;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
            }
        }

        private static void ConfigureStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
