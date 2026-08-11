using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.UI
{
    public sealed class ElderCareChoiceCardSpec
    {
        public string Name;
        public Vector2 Position;
        public Vector2 Size;
        public string Title;
        public string Subtitle;
        public string Duration;
        public string Intensity;
        public string ActionText;
        public Sprite HeroIcon;
        public bool UseLineHero;
        public ElderCareIconType LineHeroType;
        public Sprite ClockIcon;
        public Sprite ActionIcon;
        public Color Accent;
        public bool Recommended;
        public bool Interactable = true;
    }

    public static class ElderCareChoiceCardBuilder
    {
        public static Button Build(RectTransform parent, TMP_FontAsset font, ElderCareChoiceCardSpec spec)
        {
            if (parent == null) throw new System.ArgumentNullException(nameof(parent));
            if (spec == null) throw new System.ArgumentNullException(nameof(spec));

            var size = spec.Size.sqrMagnitude > 0.001f ? spec.Size : ElderCareMenuDesignTokens.SecondaryThreeCardSize;
            var card = ElderCareMenuPanelBuilder.CreateRect(spec.Name, parent, size, spec.Position);
            var target = card.gameObject.AddComponent<ElderCareRoundedPanel>();
            target.cornerRadius = ElderCareMenuDesignTokens.CardRadius;
            target.cornerSegments = 12;
            target.color = new Color(1f, 1f, 1f, 0.012f);
            target.raycastTarget = true;

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            button.interactable = spec.Interactable;
            var buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = Color.white;
            buttonColors.selectedColor = Color.white;
            buttonColors.pressedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            buttonColors.disabledColor = new Color(1f, 1f, 1f, 0.62f);
            buttonColors.fadeDuration = 0.12f;
            button.colors = buttonColors;

            var content = ElderCareMenuPanelBuilder.CreateRect("Content", card, size, Vector2.zero);
            ElderCareMenuPanelBuilder.ConfigureStretch(content);
            var glow = ElderCareMenuPanelBuilder.CreateRounded(content, "HoverGlow", size + new Vector2(20f, 18f), new Vector2(0f, -1f), ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.025f), 31f, Color.clear, Vector2.zero);
            ElderCareMenuPanelBuilder.CreateRounded(content, "Shadow", size + new Vector2(14f, 14f), new Vector2(0f, -7f), ElderCareMenuDesignTokens.WarmShadow, 30f, Color.clear, Vector2.zero);
            var baseColor = spec.Recommended ? ElderCareMenuDesignTokens.CardHighlight : ElderCareMenuDesignTokens.Card;
            var surface = ElderCareMenuPanelBuilder.CreateRounded(content, "Background", size - new Vector2(2f, 2f), Vector2.zero, baseColor, 27f, ElderCareMenuDesignTokens.WithAlpha(spec.Recommended ? ElderCareMenuDesignTokens.Amber : ElderCareMenuDesignTokens.GoldStroke, 0.72f), new Vector2(1.5f, -1.5f));
            ElderCareMenuPanelBuilder.CreateRounded(content, "InnerRice", size - new Vector2(18f, 18f), Vector2.zero, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceLight, 0.34f), 22f, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.14f), new Vector2(0.7f, -0.7f));
            var edge = ElderCareMenuPanelBuilder.CreateRounded(content, "SideAccent", new Vector2(5f, size.y - 72f), new Vector2(-size.x * 0.5f + 15f, -5f), ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.48f), 2.5f, Color.clear, Vector2.zero);

            var iconPosition = new Vector2(0f, 88f);
            ElderCareMenuPanelBuilder.CreateRounded(content, "IconHalo", new Vector2(ElderCareMenuDesignTokens.IconHaloSize, ElderCareMenuDesignTokens.IconHaloSize), iconPosition, ElderCareMenuDesignTokens.WithAlpha(spec.Accent, spec.Recommended ? 0.20f : 0.15f), ElderCareMenuDesignTokens.IconHaloSize * 0.5f, Color.clear, Vector2.zero);
            var iconContainer = ElderCareMenuPanelBuilder.CreateRounded(content, "IconContainer", new Vector2(82f, 82f), iconPosition, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceLight, 0.97f), 41f, ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.46f), new Vector2(1f, -1f));
            BuildHeroIcon(iconContainer.rectTransform, spec);

            var ribbonX = -size.x * 0.5f + 55f;
            var ribbon = ElderCareMenuPanelBuilder.CreateRounded(content, "RecommendationRibbon", new Vector2(94f, 26f), new Vector2(ribbonX, 136f), ElderCareMenuDesignTokens.Amber, 13f, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldDeep, 0.50f), new Vector2(1f, -1f));
            ElderCareMenuPanelBuilder.CreateText(ribbon.rectTransform, "Label", "今日推荐", new Vector2(88f, 24f), Vector2.zero, 13f, FontStyles.Bold, ElderCareMenuDesignTokens.RiceLight, font);
            ribbon.gameObject.SetActive(spec.Recommended);

            ElderCareMenuPanelBuilder.CreateText(content, "Title", spec.Title, new Vector2(size.x - 30f, 38f), new Vector2(0f, 25f), 30f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);
            ElderCareMenuPanelBuilder.CreateText(content, "Subtitle", spec.Subtitle, new Vector2(size.x - 26f, 28f), new Vector2(0f, -10f), 17f, FontStyles.Bold, ElderCareMenuDesignTokens.TextSecondary, font);

            var metadataWidth = Mathf.Min(size.x - 26f, 280f);
            var metadata = ElderCareMenuPanelBuilder.CreateRect("Metadata", content, new Vector2(metadataWidth, 34f), new Vector2(0f, -51f));
            var durationPill = ElderCareMenuPanelBuilder.CreateRounded(metadata, "DurationPill", new Vector2(112f, ElderCareMenuDesignTokens.MetadataHeight), new Vector2(-49f, 0f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.94f), 16f, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.42f), new Vector2(0.7f, -0.7f));
            BuildFunctionalIcon(durationPill.rectTransform, "ClockIcon", spec.ClockIcon, ElderCareIconType.Target, new Vector2(18f, 18f), new Vector2(-39f, 0f));
            ElderCareMenuPanelBuilder.CreateText(durationPill.rectTransform, "Label", spec.Duration, new Vector2(80f, 26f), new Vector2(10f, 0f), 15f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);
            var intensityPill = ElderCareMenuPanelBuilder.CreateRounded(metadata, "IntensityPill", new Vector2(94f, ElderCareMenuDesignTokens.MetadataHeight), new Vector2(62f, 0f), ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.16f), 16f, ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.48f), new Vector2(0.7f, -0.7f));
            ElderCareMenuPanelBuilder.CreateText(intensityPill.rectTransform, "Label", spec.Intensity, new Vector2(88f, 26f), Vector2.zero, 15f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);

            var startWidth = Mathf.Min(size.x - 40f, 280f);
            var startFill = Color.Lerp(ElderCareMenuDesignTokens.RiceLight, spec.Accent, 0.48f);
            var startButton = ElderCareMenuPanelBuilder.CreateRounded(content, "StartButtonVisual", new Vector2(startWidth, ElderCareMenuDesignTokens.ChoiceCtaHeight), new Vector2(0f, -113f), startFill, 24f, ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.76f), new Vector2(1.2f, -1.2f));
            BuildFunctionalIcon(startButton.rectTransform, "ActionIcon", spec.ActionIcon, ElderCareIconType.Check, new Vector2(22f, 22f), new Vector2(-startWidth * 0.25f, 0f));
            ElderCareMenuPanelBuilder.CreateText(startButton.rectTransform, "Label", spec.ActionText, new Vector2(startWidth - 82f, 34f), new Vector2(18f, 0f), 21f, FontStyles.Bold, ElderCareMenuDesignTokens.TextPrimary, font);

            var group = card.gameObject.AddComponent<CanvasGroup>();
            var motion = card.gameObject.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = card;
            motion.canvasGroup = group;
            motion.cardGraphic = surface;
            motion.glowGraphic = glow;
            motion.edgeGraphic = edge;
            motion.interactable = spec.Interactable;
            motion.playEntrance = false;
            motion.hoverScale = spec.Interactable ? ElderCareMenuDesignTokens.HoverScale : 1f;
            motion.pressedScale = spec.Interactable ? ElderCareMenuDesignTokens.PressedScale : 1f;
            motion.selectedScale = 1.035f;
            motion.hoverLiftY = spec.Interactable ? ElderCareMenuDesignTokens.HoverLiftY : 0f;
            motion.selectedLiftY = 4f;
            motion.ambientMotion = false;
            motion.ambientFloatY = 0f;
            motion.animationSpeed = 10f;
            motion.normalColor = baseColor;
            motion.hoverColor = Color.Lerp(baseColor, spec.Accent, 0.08f);
            motion.pressedColor = Color.Lerp(baseColor, spec.Accent, 0.14f);
            motion.glowColor = ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.16f);
            motion.edgeColor = ElderCareMenuDesignTokens.WithAlpha(spec.Accent, 0.84f);
            group.interactable = spec.Interactable;
            group.blocksRaycasts = spec.Interactable;
            return button;
        }

        private static void BuildHeroIcon(RectTransform parent, ElderCareChoiceCardSpec spec)
        {
            var size = new Vector2(ElderCareMenuDesignTokens.HeroIconSize, ElderCareMenuDesignTokens.HeroIconSize);
            if (spec.HeroIcon != null)
            {
                ElderCareMenuPanelBuilder.CreateSpriteIcon(parent, "HeroIcon", spec.HeroIcon, size, Vector2.zero, Color.white);
                return;
            }

            if (spec.UseLineHero)
            {
                ElderCareMenuPanelBuilder.CreateLineIcon(parent, "HeroIcon", spec.LineHeroType, size, Vector2.zero, spec.Accent, 5f);
                return;
            }

            Debug.LogError("Missing UI Icon: " + spec.Name + "/HeroIcon");
            ElderCareMenuPanelBuilder.CreateLineIcon(parent, "HeroIcon", ElderCareIconType.User, size, Vector2.zero, ElderCareMenuDesignTokens.TextMuted, 5f);
        }

        private static void BuildFunctionalIcon(RectTransform parent, string name, Sprite sprite, ElderCareIconType fallbackType, Vector2 size, Vector2 position)
        {
            if (sprite != null)
            {
                ElderCareMenuPanelBuilder.CreateSpriteIcon(parent, name, sprite, size, position, Color.white);
                return;
            }

            Debug.LogError("Missing UI Icon: " + name);
            ElderCareMenuPanelBuilder.CreateLineIcon(parent, name, fallbackType, size, position, ElderCareMenuDesignTokens.TextPrimary, Mathf.Max(2f, size.x * 0.12f));
        }
    }
}
