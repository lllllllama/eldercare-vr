using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.UI
{
    public static class ElderCareMenuPanelBuilder
    {
        public static void BuildPanelFrame(RectTransform panel, Vector2 canvasSize)
        {
            var root = CreateRect("VisualRoot", panel, canvasSize, Vector2.zero);
            ConfigureStretch(root);

            CreateRounded(root, "Shadow", canvasSize - new Vector2(34f, 34f), new Vector2(0f, -8f), ElderCareMenuDesignTokens.WarmShadow, 42f, Color.clear, 0f);
            CreateRounded(root, "WoodFrame", canvasSize - new Vector2(8f, 8f), Vector2.zero, ElderCareMenuDesignTokens.Wood, ElderCareMenuDesignTokens.PanelRadius, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.48f), 2f);
            CreateRounded(root, "WoodWarmLayer", canvasSize - new Vector2(24f, 24f), new Vector2(0f, 1f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.26f), 38f, Color.clear, 0f);
            CreateRounded(root, "WoodStripeLeft", new Vector2(10f, canvasSize.y - 56f), new Vector2(-canvasSize.x * 0.467f, 0f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.34f), 5f, Color.clear, 0f);
            CreateRounded(root, "WoodStripeRight", new Vector2(9f, canvasSize.y - 60f), new Vector2(canvasSize.x * 0.467f, 0f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.26f), 4.5f, Color.clear, 0f);
            CreateRounded(root, "RicePaperPanel", canvasSize - new Vector2(48f, 48f), new Vector2(0f, 5f), ElderCareMenuDesignTokens.RiceLight, 34f, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.72f), 1.5f);
            CreateRounded(root, "RiceWarmEdge", canvasSize - new Vector2(74f, 74f), new Vector2(0f, 4f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.28f), 28f, Color.clear, 0f);
            CreateRounded(root, "PaperGrainA", new Vector2(8f, 8f), new Vector2(-canvasSize.x * 0.33f, canvasSize.y * 0.315f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.16f), 4f, Color.clear, 0f);
            CreateRounded(root, "PaperGrainB", new Vector2(6f, 6f), new Vector2(canvasSize.x * 0.287f, canvasSize.y * 0.236f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.13f), 3f, Color.clear, 0f);
            CreateRounded(root, "PaperGrainC", new Vector2(7f, 7f), new Vector2(-canvasSize.x * 0.102f, -canvasSize.y * 0.264f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.12f), 3.5f, Color.clear, 0f);
        }

        public static RectTransform BuildHeader(RectTransform panel, Vector2 canvasSize, TMP_FontAsset font, string title, string subtitle)
        {
            var header = CreateRect("Header", panel, new Vector2(canvasSize.x - 100f, 104f), new Vector2(0f, canvasSize.y * 0.5f - 87f));
            CreateText(header, "Title", title, new Vector2(canvasSize.x - 110f, 54f), new Vector2(0f, 20f), 42f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);
            CreateText(header, "Subtitle", subtitle, new Vector2(canvasSize.x - 140f, 32f), new Vector2(0f, -27f), 20f, FontStyles.Bold, ElderCareMenuDesignTokens.TextSecondary, font);
            CreateRounded(header, "Divider", new Vector2(420f, 3f), new Vector2(0f, -51f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.58f), 1.5f, Color.clear, 0f);
            return header;
        }

        public static Button BuildBottomDock(RectTransform panel, Vector2 canvasSize, TMP_FontAsset font, Sprite backIcon, string hint, string backLabel)
        {
            var dockSize = new Vector2(canvasSize.x - 76f, ElderCareMenuDesignTokens.SecondaryDockSize.y);
            var dock = CreateRounded(panel, "BottomDock", dockSize, new Vector2(0f, -canvasSize.y * 0.5f + 50f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.90f), 28f, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.46f), 1f);
            CreateText(dock.rectTransform, "Hint", hint, new Vector2(430f, 28f), new Vector2(118f, 0f), 16f, FontStyles.Bold, ElderCareMenuDesignTokens.TextSecondary, font);

            var backPosition = new Vector2(-dockSize.x * 0.5f + ElderCareMenuDesignTokens.SecondaryBackButtonSize.x * 0.5f + 15f, 0f);
            var backRect = CreateRect("BackButton", dock.rectTransform, ElderCareMenuDesignTokens.SecondaryBackButtonSize, backPosition);
            var target = backRect.gameObject.AddComponent<ElderCareRoundedPanel>();
            target.cornerRadius = 26f;
            target.cornerSegments = 10;
            target.color = ElderCareMenuDesignTokens.Card;
            target.raycastTarget = true;
            ConfigureNativeStroke(
                target,
                ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.66f),
                1.2f);

            var button = backRect.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, ElderCareMenuDesignTokens.RiceMid, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(Color.white, ElderCareMenuDesignTokens.WoodDark, 0.16f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.62f);
            button.colors = colors;

            if (backIcon != null)
            {
                CreateSpriteIcon(backRect, "ArrowIcon", backIcon, new Vector2(25f, 25f), new Vector2(-70f, 0f), Color.white);
            }
            else
            {
                Debug.LogError("Missing UI Icon: arrow-left");
                CreateLineIcon(backRect, "ArrowIcon", ElderCareIconType.ArrowLeft, new Vector2(25f, 25f), new Vector2(-70f, 0f), ElderCareMenuDesignTokens.TextPrimary, 3f);
            }

            CreateText(backRect, "Label", backLabel, new Vector2(140f, 34f), new Vector2(18f, 0f), 20f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);

            var group = backRect.gameObject.AddComponent<CanvasGroup>();
            var motion = backRect.gameObject.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = backRect;
            motion.canvasGroup = group;
            motion.cardGraphic = target;
            motion.interactable = true;
            motion.playEntrance = false;
            motion.hoverScale = 1.025f;
            motion.pressedScale = ElderCareMenuDesignTokens.PressedScale;
            motion.hoverLiftY = 3f;
            motion.ambientMotion = false;
            motion.normalColor = ElderCareMenuDesignTokens.Card;
            motion.hoverColor = ElderCareMenuDesignTokens.CardHighlight;
            motion.pressedColor = ElderCareMenuDesignTokens.RiceMid;
            ApplyFont(backRect, font);
            return button;
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static ElderCareRoundedPanel CreateRounded(Transform parent, string name, Vector2 size, Vector2 position, Color color, float radius, Color strokeColor, float strokeWidth)
        {
            var rect = CreateRect(name, parent, size, position);
            var panel = rect.gameObject.AddComponent<ElderCareRoundedPanel>();
            panel.cornerRadius = radius;
            panel.cornerSegments = 12;
            panel.color = color;
            panel.raycastTarget = false;
            ConfigureNativeStroke(panel, strokeColor, strokeWidth);

            return panel;
        }

        public static void ConfigureNativeStroke(ElderCareRoundedPanel panel, Color strokeColor, float strokeWidth)
        {
            if (panel == null) return;

            var enabled = strokeWidth > 0f && strokeColor.a > 0f;
            panel.DrawStroke = enabled;
            panel.StrokeColor = enabled ? strokeColor : Color.clear;
            panel.StrokeWidth = enabled ? strokeWidth : 0f;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, Vector2 size, Vector2 position, float fontSize, FontStyles style, Color color, TMP_FontAsset font)
        {
            var rect = CreateRect(name, parent, size, position);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if (font != null) label.font = font;
            return label;
        }

        public static Image CreateSpriteIcon(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Color color)
        {
            var rect = CreateRect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static ElderCareLineIcon CreateLineIcon(Transform parent, string name, ElderCareIconType iconType, Vector2 size, Vector2 position, Color color, float strokeWidth)
        {
            var rect = CreateRect(name, parent, size, position);
            var icon = rect.gameObject.AddComponent<ElderCareLineIcon>();
            icon.iconType = iconType;
            icon.strokeWidth = strokeWidth;
            icon.color = color;
            icon.raycastTarget = false;
            return icon;
        }

        public static void ApplyFont(Transform root, TMP_FontAsset font)
        {
            if (root == null || font == null) return;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
            }
        }

        public static void ConfigureStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
