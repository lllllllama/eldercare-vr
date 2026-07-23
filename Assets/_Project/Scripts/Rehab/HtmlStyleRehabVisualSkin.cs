using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PicoElderCare.Rehab
{
    public static class HtmlStyleRehabVisualSkin
    {
        private const string VisualRootName = "HtmlVisual_Root";
        private const string PanelVisualRootName = "HtmlVisual_PanelRoot";
        private const string VideoVisualRootName = "HtmlVisual_VideoShell";
        private const string LegacyVideoVisualRootName = "HtmlVisual_VideoPanelRoot";
        private const string IconResourceRoot = "HtmlSvgIcons/";

        private static readonly Color WoodMid = new Color32(0xD9, 0xC7, 0xA3, 0xFF);
        private static readonly Color WoodDark = new Color32(0xC6, 0xB0, 0x85, 0xFF);
        private static readonly Color RiceLight = new Color32(0xFB, 0xF5, 0xE6, 0xFF);
        private static readonly Color RiceMid = new Color32(0xF1, 0xE6, 0xCC, 0xFF);
        private static readonly Color CardHighlight = new Color32(0xFF, 0xF8, 0xE4, 0xFF);
        private static readonly Color CardNormal = new Color32(0xF7, 0xEF, 0xD9, 0xFF);
        private static readonly Color GoldStroke = new Color32(0xC9, 0xA9, 0x6A, 0xFF);
        private static readonly Color GoldDeep = new Color32(0xB5, 0x73, 0x27, 0xFF);
        private static readonly Color Amber = new Color32(0xD4, 0x8F, 0x3A, 0xFF);
        private static readonly Color AmberLight = new Color32(0xE8, 0xB2, 0x69, 0xFF);
        private static readonly Color Jade = new Color32(0x5F, 0x85, 0x60, 0xFF);
        private static readonly Color TextPrimary = new Color32(0x3E, 0x2E, 0x1F, 0xFF);
        private static readonly Color TextSecondary = new Color32(0x7A, 0x69, 0x52, 0xFF);
        private static readonly Color TrainingTextSecondary = new Color32(0x5A, 0x46, 0x30, 0xFF);
        private static readonly Color TextMuted = new Color32(0x9A, 0x85, 0x60, 0xFF);
        private static readonly Color WarmShadow = new Color(0.25f, 0.16f, 0.06f, 0.24f);
        private static readonly Dictionary<string, Material> VideoMaterialCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Sprite> IconSpriteCache = new Dictionary<string, Sprite>();
        private static Mesh _unitQuadMesh;
        private static TMP_FontAsset _activeUiFont;
        private static bool _iconResourceAvailabilityLogged;

        public static void Apply(RehabModeSelectUI ui)
        {
            if (ui == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] RehabModeSelectUI is null, skip styling");
                return;
            }

            Debug.Log(
                string.Format(
                    "[HtmlStyleRehabVisualSkin] Apply started scene={0} ui={1}",
                    SceneManager.GetActiveScene().name,
                    ui.name));

            LogIconResourceAvailability();

            var previousFont = _activeUiFont;
            _activeUiFont = ResolveUiFont(ui);
            try
            {
                StyleMainMenuPanel(ui);
                StyleTrainingSelectPanel(ui);
                StyleTrainingPanel(ui);
                StyleResultPanel(ui);
                StyleVideoPanel(ui);
            }
            finally
            {
                _activeUiFont = previousFont;
            }

            Debug.Log("[HtmlStyleRehabVisualSkin] Apply finished");
        }

        private static void StyleMainMenuPanel(RehabModeSelectUI ui)
        {
            StylePanelFrame(ui.mainMenuPanel, "MainMenu", "\u5eb7\u590d\u8fd0\u52a8", "\u67d4\u548c\u8bad\u7ec3 - \u7a33\u6b65\u6062\u590d");
            StyleSoftButton(ui.rehabButton, "RehabButton", "\u5eb7\u590d\u8fd0\u52a8", "\u516b\u6bb5\u9526\u4e0e\u592a\u6781\u8bad\u7ec3", Jade);
        }

        private static void StyleTrainingSelectPanel(RehabModeSelectUI ui)
        {
            StylePanelFrame(
                ui.rehabTrainingSelectPanel,
                "TrainingSelection",
                "\u8bf7\u9009\u62e9\u5eb7\u590d\u8bad\u7ec3\u7c7b\u578b",
                "\u8ddf\u7740\u865a\u62df\u6559\u7ec3\u6162\u6162\u6765\uff0c\u968f\u65f6\u53ef\u4ee5\u6682\u505c\u4f11\u606f");

            StyleSelectionCardButton(
                ui.baduanjinButton,
                "BaduanjinButton",
                "\u516b\u6bb5\u9526",
                "\u8212\u5c55\u7b4b\u9aa8 - \u7ecf\u5178\u517b\u751f",
                "\u4eca\u65e5\u63a8\u8350",
                "\u7ea6 10 \u5206\u949f",
                "\u5f3a\u5ea6\u8f7b",
                "\u7f13\u6162\u547c\u5438 - \u5faa\u5e8f\u6e10\u8fdb",
                "lotus",
                Amber,
                true);

            StyleSelectionCardButton(
                ui.taiChiButton,
                "TaiChiButton",
                "\u592a\u6781",
                "\u67d4\u548c\u7f13\u6162 - \u5e73\u8861\u8eab\u5fc3",
                null,
                "\u7ea6 12 \u5206\u949f",
                "\u5f3a\u5ea6\u4e2d",
                "\u4ee5\u67d4\u514b\u521a - \u5185\u5916\u517c\u4fee",
                "cyclone",
                Jade,
                false);

            StyleBackButton(ui.backButton, "BackButton", "\u8fd4\u56de\u4e0a\u4e00\u9875");
            StyleBottomDock(ui.rehabTrainingSelectPanel);
            Debug.Log("[HtmlStyleRehabVisualSkin] Styled training select panel");
        }

        private static void StyleTrainingPanel(RehabModeSelectUI ui)
        {
            StylePanelFrame(ui.rehabTrainingPanel, "Training", null, null);
            StyleTrainingHeader(ui.rehabTrainingPanel);
            StyleDataBlock(ui.rehabTrainingPanel, "TimerBlock", "\u5012\u8ba1\u65f6", Amber, CardHighlight);
            StyleDataBlock(ui.rehabTrainingPanel, "CompletionBlock", "\u5b8c\u6210\u5ea6", Jade, new Color32(0xEA, 0xF3, 0xE4, 0xFF));
            StyleSafetyBanner(ui.rehabTrainingPanel);
            StyleTrainingStats(ui.rehabTrainingPanel);
            StyleTrainingDebugText(ui.rehabTrainingPanel);
            var startButton = FindButton(ui.rehabTrainingPanel, "StartButton");
            ConfigureTrainingFooterButton(startButton, new Vector2(-232f, -174f));
            ConfigureTrainingFooterButton(ui.trainingBackButton, new Vector2(232f, -174f));
            StyleTrainingActionButton(startButton, "StartButton", Amber, true);
            StyleTrainingActionButton(ui.trainingBackButton, "TrainingBackButton", GoldStroke, false);
            HideBottomHint(ui.rehabTrainingPanel);
            StyleBottomDock(ui.rehabTrainingPanel);
            Debug.Log("[HtmlStyleRehabVisualSkin] Styled training panel");
        }

        private static void StyleResultPanel(RehabModeSelectUI ui)
        {
            StylePanelFrame(ui.trainingResultPanel, "TrainingResult", "\u8bad\u7ec3\u7ed3\u679c", "\u7ed3\u679c\u5df2\u4fdd\u5b58\uff0c\u8bf7\u7a33\u6b65\u8fd4\u56de\u9009\u62e9\u9875");
            var panelRect = GetRect(ui.trainingResultPanel, "TrainingResultPanel");
            var panelRoot = GetPanelRoot(ui.trainingResultPanel);
            var summary = FindChildText(ui.trainingResultPanel != null ? ui.trainingResultPanel.transform : null, "Summary");
            if (panelRect != null && summary != null)
            {
                var size = GetUsableSize(panelRect);
                StyleExistingText(summary, 22f, FontStyles.Normal, TextSecondary, TextAlignmentOptions.Center);
                ConfigureTextLayout(summary, new Vector2(size.x - 120f, 68f), new Vector2(0f, 30f));
                ConfigureTextFitting(summary, 18f, 22f, true, new Vector4(10f, 4f, 10f, 4f));
                ClearVisualText(panelRoot, "HtmlVisual_Subtitle");
            }
            StyleBackButton(ui.resultBackButton, "ResultBackButton", "\u8fd4\u56de\u9009\u62e9");
        }

        private static void StyleVideoPanel(RehabModeSelectUI ui)
        {
            var guide = ui != null ? ui.videoGuideController : null;
            if (guide == null)
            {
                guide = UnityEngine.Object.FindObjectOfType<RehabVideoGuideController>(true);
            }

            if (guide == null ||
                (guide.videoPanel == null && guide.videoQuad == null && guide.displayRoot == null && guide.rawImage == null))
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] Video panel root not found, skip video styling");
                return;
            }

            guide.videoFrameColor = WithAlpha(RiceLight, 0.98f);
            guide.videoFrameAccentColor = WithAlpha(Jade, 0.86f);

            if (guide.videoQuad != null)
            {
                StyleVideoQuadPanel(guide);
            }

            if (guide.rawImage != null)
            {
                StyleVideoRawImageFrame(guide.rawImage);
            }

            if (guide.videoPanel != null)
            {
                StyleVideoControlButtons(guide.videoPanel);
            }

            StyleSpatialVideoControls();

            Debug.Log("[HtmlStyleRehabVisualSkin] Styled video panel");
        }

        private static void StylePanelFrame(GameObject panel, string logName, string title, string subtitle)
        {
            var rect = GetRect(panel, logName + "Panel");
            if (rect == null) return;

            var root = EnsurePanelVisualRoot(panel.transform);
            ConfigureStretch(root);
            MoveToBack(root);

            var panelSize = GetUsableSize(rect);
            HideLegacyGraphic(panel.transform, "PanelTopTrace");
            HideLegacyGraphic(panel.transform, "PanelBottomTrace");
            HideLegacyGraphic(panel.transform, "TitleTrace");
            EnsureRoundedDecor(root, "HtmlVisual_WoodWash", panelSize, Vector2.zero, WithAlpha(WoodMid, 0.98f), 34f, WithAlpha(WoodDark, 0.34f), new Vector2(1.8f, -1.8f));
            EnsureRoundedDecor(root, "HtmlVisual_WoodWarmLayer", panelSize - new Vector2(14f, 14f), new Vector2(0f, 1f), WithAlpha(new Color32(0xE9, 0xDC, 0xC0, 0xFF), 0.18f), 30f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_WoodStripeLeft", new Vector2(10f, panelSize.y - 18f), new Vector2(-panelSize.x * 0.39f, 0f), WithAlpha(WoodDark, 0.28f), 4f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_WoodStripeRight", new Vector2(9f, panelSize.y - 20f), new Vector2(panelSize.x * 0.39f, 0f), WithAlpha(WoodDark, 0.20f), 4f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_Shadow", panelSize - new Vector2(40f, 36f), new Vector2(0f, -8f), WarmShadow, 34f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_RiceCard", panelSize - new Vector2(54f, 48f), new Vector2(0f, 6f), WithAlpha(RiceLight, 0.99f), 30f, WithAlpha(GoldStroke, 0.70f), new Vector2(1.4f, -1.4f));
            EnsureRoundedDecor(root, "HtmlVisual_RiceWarmEdge", panelSize - new Vector2(80f, 74f), new Vector2(0f, 4f), WithAlpha(RiceMid, 0.26f), 25f, WithAlpha(GoldStroke, 0.16f), new Vector2(0.8f, -0.8f));
            EnsureRoundedDecor(root, "HtmlVisual_PaperGrainA", new Vector2(8f, 8f), new Vector2(-panelSize.x * 0.28f, panelSize.y * 0.25f), WithAlpha(GoldStroke, 0.16f), 4f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_PaperGrainB", new Vector2(6f, 6f), new Vector2(panelSize.x * 0.22f, panelSize.y * 0.13f), WithAlpha(GoldStroke, 0.13f), 3f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_PaperGrainC", new Vector2(7f, 7f), new Vector2(-panelSize.x * 0.08f, -panelSize.y * 0.16f), WithAlpha(GoldStroke, 0.12f), 3.5f, Color.clear, Vector2.zero);
            var hasHeaderCopy = !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(subtitle);
            if (hasHeaderCopy)
            {
                var isTrainingSelection = string.Equals(logName, "TrainingSelection", System.StringComparison.Ordinal);
                if (isTrainingSelection)
                {
                    SetVisualDecorTransparent(root, "HtmlVisual_TitleDivider");
                }
                else
                {
                    var dividerWidth = Mathf.Min(panelSize.x - 220f, 360f);
                    var dividerY = panelSize.y * 0.215f;
                    EnsureRoundedDecor(root, "HtmlVisual_TitleDivider", new Vector2(dividerWidth, 3f), new Vector2(0f, dividerY), WithAlpha(GoldStroke, 0.58f), 1.5f, Color.clear, Vector2.zero);
                }
            }
            else
            {
                SetVisualDecorTransparent(root, "HtmlVisual_TitleDivider");
            }
            EnsureLeafDecor(root, panelSize);

            if (!string.IsNullOrEmpty(title))
            {
                var isTrainingSelection = string.Equals(logName, "TrainingSelection", System.StringComparison.Ordinal);
                var existingTitle = FindChildText(panel.transform, "Title");
                if (existingTitle != null)
                {
                    StyleExistingText(existingTitle, 42f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
                    var titleY = isTrainingSelection ? panelSize.y * 0.39f : panelSize.y * 0.30f;
                    ConfigureTextLayout(existingTitle, new Vector2(panelSize.x - 100f, 56f), new Vector2(0f, titleY));
                    ClearVisualText(root, "HtmlVisual_Title");
                }
                else
                {
                    var titleY = isTrainingSelection ? panelSize.y * 0.39f : panelSize.y * 0.31f;
                    EnsureText(root, "HtmlVisual_Title", title, new Vector2(panelSize.x - 110f, 56f), new Vector2(0f, titleY), 42f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
                }
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                var isTrainingSelection = string.Equals(logName, "TrainingSelection", System.StringComparison.Ordinal);
                var subtitleY = isTrainingSelection ? panelSize.y * 0.283f : panelSize.y * 0.205f;
                var subtitleColor = isTrainingSelection ? TrainingTextSecondary : TextSecondary;
                EnsureText(root, "HtmlVisual_Subtitle", subtitle, new Vector2(panelSize.x - 120f, isTrainingSelection ? 24f : 28f), new Vector2(0f, subtitleY), isTrainingSelection ? 18f : 18f, isTrainingSelection ? FontStyles.Bold : FontStyles.Normal, subtitleColor, TextAlignmentOptions.Center);
            }

            DisableRaycastForVisualTree(root.gameObject);
        }

        private static void StyleSelectionCardButton(
            Button button,
            string logName,
            string title,
            string subtitle,
            string ribbon,
            string duration,
            string intensity,
            string footer,
            string iconResourceName,
            Color accent,
            bool highlighted)
        {
            var rect = GetButtonRect(button, logName);
            if (rect == null) return;

            EnsureButtonTargetGraphicPresent(button);
            FadeButtonTargetGraphic(button, 0.055f);

            var root = EnsureVisualRoot(button.transform);
            ConfigureStretch(root);
            MoveToBack(root);

            var size = GetUsableSize(rect);
            var background = highlighted ? CardHighlight : CardNormal;
            var surfaceColor = WithAlpha(background, 0.99f);
            var hoverGlow = EnsureRoundedDecor(root, "HtmlVisual_HoverGlow", size + new Vector2(24f, 22f), new Vector2(0f, -1f), WithAlpha(accent, 0.06f), 28f, Color.clear, Vector2.zero);
            if (hoverGlow != null) hoverGlow.transform.SetSiblingIndex(0);
            EnsureRoundedDecor(root, "HtmlVisual_Shadow", size + new Vector2(20f, 18f), new Vector2(0f, -7f), new Color(0.28f, 0.18f, 0.06f, 0.25f), 25f, Color.clear, Vector2.zero);
            var cardSurface = EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, surfaceColor, 24f, WithAlpha(highlighted ? Amber : GoldStroke, 0.76f), new Vector2(1.8f, -1.8f));
            EnsureRoundedDecor(root, "HtmlVisual_InnerRice", size - new Vector2(18f, 18f), Vector2.zero, WithAlpha(RiceLight, highlighted ? 0.35f : 0.25f), 19f, WithAlpha(GoldStroke, 0.14f), new Vector2(0.8f, -0.8f));
            SetVisualDecorTransparent(root, "HtmlVisual_TopGlow");
            EnsureRoundedDecor(root, "HtmlVisual_SideAccent", new Vector2(5f, size.y - 48f), new Vector2(-size.x * 0.44f, 0f), WithAlpha(accent, 0.62f), 2.5f, Color.clear, Vector2.zero);
            var iconPosition = new Vector2(-size.x * 0.35f, 2f);
            EnsureRoundedDecor(root, "HtmlVisual_IconHaloOuter", new Vector2(70f, 70f), iconPosition, WithAlpha(accent, highlighted ? 0.22f : 0.18f), 35f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_IconHalo", new Vector2(54f, 54f), iconPosition, WithAlpha(RiceLight, 0.94f), 27f, WithAlpha(accent, 0.48f), new Vector2(1f, -1f));
            EnsureIconOrFallback(root, "HtmlVisual_Icon_" + logName, iconResourceName, ElderCareIconType.User, new Vector2(38f, 38f), iconPosition, highlighted ? GoldDeep : Jade);
            ClearVisualText(root, "HtmlVisual_Icon");
            EnsureText(root, "HtmlVisual_Subtitle", subtitle, new Vector2(180f, 24f), new Vector2(38f, -7f), 16f, FontStyles.Bold, TrainingTextSecondary, TextAlignmentOptions.Center);
            EnsurePill(root, "HtmlVisual_DurationPill", duration, new Vector2(108f, 28f), new Vector2(-29f, -43f), highlighted ? CardHighlight : CardNormal, GoldStroke, TextPrimary);
            EnsurePill(root, "HtmlVisual_IntensityPill", intensity, new Vector2(96f, 28f), new Vector2(82f, -43f), highlighted ? CardHighlight : CardNormal, highlighted ? Jade : Amber, TextPrimary);
            ClearVisualText(root, "HtmlVisual_Footer");
            SetVisualDecorTransparent(root, "HtmlVisual_AccentLine");

            if (!string.IsNullOrEmpty(ribbon))
            {
                EnsureRoundedDecor(root, "HtmlVisual_Ribbon", new Vector2(90f, 24f), new Vector2(-95f, 55f), WithAlpha(Amber, 0.98f), 12f, WithAlpha(GoldDeep, 0.50f), new Vector2(1.1f, -1.1f));
                SetVisualDecorTransparent(root, "HtmlVisual_RibbonDot");
                EnsureText(root, "HtmlVisual_RibbonText", ribbon, new Vector2(82f, 22f), new Vector2(-95f, 55f), 13f, FontStyles.Bold, new Color32(0xFF, 0xF8, 0xE4, 0xFF), TextAlignmentOptions.Center);
            }
            else
            {
                ClearVisualText(root, "HtmlVisual_RibbonText");
                SetVisualDecorTransparent(root, "HtmlVisual_Ribbon");
                SetVisualDecorTransparent(root, "HtmlVisual_RibbonDot");
            }

            StyleButtonLabel(button, title, 30f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, Vector4.zero);
            ConfigureButtonLabelLayout(button, new Vector2(172f, 36f), new Vector2(43f, 30f));
            ConfigureHoverFeedback(button, root, cardSurface, hoverGlow, surfaceColor, accent, 1.045f, 5f);
            DisableRaycastForVisualTree(root.gameObject);
            LogStyledButton(logName, button, root);
        }

        private static void StyleSoftButton(Button button, string logName, string title, string subtitle, Color accent)
        {
            var rect = GetButtonRect(button, logName);
            if (rect == null) return;

            EnsureButtonTargetGraphicPresent(button);
            FadeButtonTargetGraphic(button, 0.10f);

            var root = EnsureVisualRoot(button.transform);
            ConfigureStretch(root);
            MoveToBack(root);

            var size = GetUsableSize(rect);
            var surfaceColor = WithAlpha(CardHighlight, 0.96f);
            var hoverGlow = EnsureRoundedDecor(root, "HtmlVisual_HoverGlow", size + new Vector2(14f, 12f), Vector2.zero, WithAlpha(accent, 0.05f), 29f, Color.clear, Vector2.zero);
            if (hoverGlow != null) hoverGlow.transform.SetSiblingIndex(0);
            var cardSurface = EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, surfaceColor, 26f, WithAlpha(GoldStroke, 0.58f), new Vector2(1.5f, -1.5f));
            EnsureRoundedDecor(root, "HtmlVisual_Accent", new Vector2(5f, size.y - 30f), new Vector2(-size.x * 0.42f, 0f), WithAlpha(accent, 0.72f), 2.5f, Color.clear, Vector2.zero);
            EnsureText(root, "HtmlVisual_Subtitle", subtitle, new Vector2(size.x - 70f, 26f), new Vector2(20f, -20f), 15f, FontStyles.Normal, TextSecondary, TextAlignmentOptions.Center);
            StyleButtonLabel(button, title, 30f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, new Vector4(20f, 10f, 20f, 20f));
            ConfigureHoverFeedback(button, root, cardSurface, hoverGlow, surfaceColor, accent, 1.035f, 4f);
            DisableRaycastForVisualTree(root.gameObject);
            LogStyledButton(logName, button, root);
        }

        private static void StyleBackButton(Button button, string logName, string label)
        {
            var rect = GetButtonRect(button, logName);
            if (rect == null) return;

            EnsureButtonTargetGraphicPresent(button);
            FadeButtonTargetGraphic(button, 0.08f);

            var root = EnsureVisualRoot(button.transform);
            ConfigureStretch(root);
            MoveToBack(root);

            var size = GetUsableSize(rect);
            var surfaceColor = WithAlpha(CardNormal, 0.96f);
            var hoverGlow = EnsureRoundedDecor(root, "HtmlVisual_HoverGlow", size + new Vector2(12f, 10f), Vector2.zero, WithAlpha(GoldStroke, 0.05f), size.y * 0.55f, Color.clear, Vector2.zero);
            if (hoverGlow != null) hoverGlow.transform.SetSiblingIndex(0);
            var cardSurface = EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, surfaceColor, size.y * 0.5f, WithAlpha(GoldStroke, 0.62f), new Vector2(1.2f, -1.2f));
            EnsureRoundedDecor(root, "HtmlVisual_Wash", size - new Vector2(24f, 18f), Vector2.zero, WithAlpha(RiceLight, 0.26f), size.y * 0.42f, Color.clear, Vector2.zero);
            StyleButtonLabel(button, label, 20f, new Color32(0x5A, 0x4A, 0x32, 0xFF), FontStyles.Bold, TextAlignmentOptions.Center, Vector4.zero);
            ConfigureHoverFeedback(button, root, cardSurface, hoverGlow, surfaceColor, GoldStroke, 1.025f, 3f);
            DisableRaycastForVisualTree(root.gameObject);
            LogStyledButton(logName, button, root);
        }

        private static void StyleTrainingActionButton(Button button, string logName, Color accent, bool primary)
        {
            var rect = GetButtonRect(button, logName);
            if (rect == null) return;

            EnsureButtonTargetGraphicPresent(button);
            FadeButtonTargetGraphic(button, primary ? 0.10f : 0.08f);

            var root = EnsureVisualRoot(button.transform);
            ConfigureStretch(root);
            MoveToBack(root);

            var size = GetUsableSize(rect);
            var fill = primary ? Amber : WoodMid;
            var textColor = primary ? new Color32(0xFF, 0xF8, 0xE4, 0xFF) : new Color32(0x4A, 0x38, 0x28, 0xFF);
            var surfaceColor = WithAlpha(fill, 0.98f);
            var hoverGlow = EnsureRoundedDecor(root, "HtmlVisual_HoverGlow", size + new Vector2(8f, 4f), Vector2.zero, WithAlpha(accent, 0.05f), size.y * 0.54f, Color.clear, Vector2.zero);
            if (hoverGlow != null) hoverGlow.transform.SetSiblingIndex(0);
            var cardSurface = EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, surfaceColor, size.y * 0.48f, WithAlpha(GoldDeep, primary ? 0.58f : 0.72f), new Vector2(1.2f, -1.2f));
            EnsureRoundedDecor(root, "HtmlVisual_AccentLine", new Vector2(size.x - 70f, 3f), new Vector2(0f, -size.y * 0.28f), WithAlpha(primary ? new Color32(0xFF, 0xF8, 0xE4, 0xFF) : GoldStroke, 0.45f), 1.5f, Color.clear, Vector2.zero);
            StyleExistingButtonLabelOnly(button, primary ? 24f : 22f, textColor, FontStyles.Bold);
            ConfigureHoverFeedback(button, root, cardSurface, hoverGlow, surfaceColor, accent, 1.03f, 3f);
            DisableRaycastForVisualTree(root.gameObject);
            LogStyledButton(logName, button, root);
        }

        private static void StyleTrainingHeader(GameObject panel)
        {
            var root = GetPanelRoot(panel);
            var rect = GetRect(panel, "TrainingPanel");
            if (root == null || rect == null) return;

            var size = GetUsableSize(rect);
            var title = FindChildText(panel.transform, "MovementTitle");
            if (title != null)
            {
                StyleExistingText(title, 40f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
                ConfigureTextLayout(title, new Vector2(size.x - 100f, 52f), new Vector2(0f, size.y * 0.365f));
                ClearVisualText(root, "HtmlVisual_DefaultTrainingTitle");
            }
            else
            {
                EnsureText(root, "HtmlVisual_DefaultTrainingTitle", "\u65e0\u6781\u6869", new Vector2(size.x - 130f, 52f), new Vector2(0f, size.y * 0.31f), 40f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
            }

            var status = FindChildText(panel.transform, "StatusText");
            if (status != null)
            {
                StyleExistingText(status, 21f, FontStyles.Bold, TrainingTextSecondary, TextAlignmentOptions.Center);
                ConfigureTextLayout(status, new Vector2(size.x - 120f, 34f), new Vector2(0f, 108f));
                ClearVisualText(root, "HtmlVisual_DefaultTrainingSubtitle");
            }
            else
            {
                EnsureText(root, "HtmlVisual_DefaultTrainingSubtitle", "\u8bf7\u7ad9\u7a33\u5e76\u51c6\u5907\u5f00\u59cb", new Vector2(size.x - 140f, 34f), new Vector2(0f, size.y * 0.21f), 20f, FontStyles.Bold, TrainingTextSecondary, TextAlignmentOptions.Center);
            }
        }

        private static void StyleDataBlock(GameObject panel, string blockName, string labelFallback, Color accent, Color fill)
        {
            var block = FindChildRecursive(panel != null ? panel.transform : null, blockName);
            if (block == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + blockName + " is null, skip styling");
                return;
            }

            var rect = GetRect(block.gameObject, blockName);
            if (rect == null) return;

            var root = EnsureVisualRoot(block);
            ConfigureStretch(root);
            MoveToBack(root);

            var size = GetUsableSize(rect);
            HideLegacyGraphic(block, "TopTrace");
            var originalPanel = block.GetComponent<ElderCareRoundedPanel>();
            if (originalPanel != null)
            {
                originalPanel.color = WithAlpha(fill, 0.99f);
                originalPanel.cornerRadius = 20f;
                originalPanel.raycastTarget = false;
                originalPanel.SetAllDirty();
            }

            var originalOutline = block.GetComponent<Outline>();
            if (originalOutline != null)
            {
                originalOutline.effectColor = Color.clear;
                originalOutline.effectDistance = Vector2.zero;
            }

            SetVisualDecorTransparent(root, "HtmlVisual_Shadow");
            EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, WithAlpha(fill, 0.99f), 20f, WithAlpha(accent, 0.34f), new Vector2(0.8f, -0.8f));
            EnsureRoundedDecor(root, "HtmlVisual_InnerWash", size - new Vector2(22f, 22f), new Vector2(0f, -2f), WithAlpha(RiceLight, 0.28f), 15f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_TopWash", new Vector2(size.x - 44f, 4f), new Vector2(0f, size.y * 0.38f), WithAlpha(accent, 0.48f), 2f, Color.clear, Vector2.zero);
            var existingLabel = FindChildText(block, "Label");
            if (existingLabel != null)
            {
                StyleExistingText(existingLabel, 18f, FontStyles.Bold, Color.Lerp(accent, TextPrimary, 0.34f), TextAlignmentOptions.Center);
                ConfigureTextLayout(existingLabel, new Vector2(182f, 26f), new Vector2(18f, 34f));
                ClearVisualText(root, "HtmlVisual_LabelHint");
            }
            else
            {
                EnsureText(root, "HtmlVisual_LabelHint", labelFallback, new Vector2(182f, 26f), new Vector2(18f, 34f), 18f, FontStyles.Bold, Color.Lerp(accent, TextPrimary, 0.34f), TextAlignmentOptions.Center);
            }

            EnsureIconOrFallback(
                root,
                "HtmlVisual_Icon_" + blockName,
                blockName == "TimerBlock" ? "hourglass" : "check",
                blockName == "TimerBlock" ? ElderCareIconType.Video : ElderCareIconType.Check,
                new Vector2(20f, 20f),
                new Vector2(-105f, 34f),
                accent);

            var value = FindChildText(block, "Value");
            if (blockName == "CompletionBlock")
            {
                var ringPosition = new Vector2(-82f, -20f);
                EnsureRoundedDecor(root, "HtmlVisual_RingOuter", new Vector2(68f, 68f), ringPosition, WithAlpha(new Color32(0xD0, 0xE4, 0xC6, 0xFF), 0.62f), 34f, Color.clear, Vector2.zero);
                EnsureRoundedDecor(root, "HtmlVisual_RingAccent", new Vector2(54f, 54f), ringPosition, WithAlpha(Jade, 0.36f), 27f, Color.clear, Vector2.zero);
                EnsureRoundedDecor(root, "HtmlVisual_RingInner", new Vector2(40f, 40f), ringPosition, WithAlpha(fill, 0.98f), 20f, Color.clear, Vector2.zero);
                StyleExistingText(value, 30f, FontStyles.Bold, Jade, TextAlignmentOptions.Center);
                ConfigureTextLayout(value, new Vector2(150f, 48f), new Vector2(40f, -20f));
            }
            else
            {
                StyleExistingText(value, 38f, FontStyles.Bold, Amber, TextAlignmentOptions.Center);
                ConfigureTextLayout(value, new Vector2(240f, 48f), new Vector2(0f, -20f));
                SetVisualDecorTransparent(root, "HtmlVisual_RingOuter");
                SetVisualDecorTransparent(root, "HtmlVisual_RingAccent");
                SetVisualDecorTransparent(root, "HtmlVisual_RingInner");
            }
            DisableRaycastForVisualTree(root.gameObject);
        }

        private static void StyleSafetyBanner(GameObject panel)
        {
            var safetyPanel = FindChildRecursive(panel != null ? panel.transform : null, "SafetyPanel");
            if (safetyPanel != null)
            {
                ConfigureRect(safetyPanel.gameObject, new Vector2(606f, 48f), new Vector2(0f, -110f));
                var image = safetyPanel.GetComponent<ElderCareRoundedPanel>();
                if (image != null)
                {
                    image.color = WithAlpha(Jade, 0.96f);
                    image.cornerRadius = 28f;
                    image.raycastTarget = false;
                    image.SetAllDirty();
                }
            }

            var safetyText = FindChildText(panel != null ? panel.transform : null, "SafetyPromptText");
            StyleExistingText(safetyText, 20f, FontStyles.Bold, new Color32(0xFF, 0xF8, 0xE4, 0xFF), TextAlignmentOptions.Center);
            ConfigureTextLayout(safetyText, new Vector2(560f, 36f), new Vector2(0f, -110f));
            ConfigureTextFitting(safetyText, 16f, 20f, false, new Vector4(10f, 2f, 10f, 2f));
        }

        private static void StyleTrainingStats(GameObject panel)
        {
            var root = GetPanelRoot(panel);
            if (root == null) return;

            SetVisualTreeTransparent(root.Find("HtmlVisual_StatMovement"));
            SetVisualTreeTransparent(root.Find("HtmlVisual_StatHold"));
            SetVisualTreeTransparent(root.Find("HtmlVisual_StatDistance"));
        }

        private static void StyleTrainingDebugText(GameObject panel)
        {
            var debugText = FindChildText(panel != null ? panel.transform : null, "DebugText");
            if (debugText == null) return;

            StyleExistingText(debugText, 13f, FontStyles.Bold, TrainingTextSecondary, TextAlignmentOptions.Center);
            ConfigureTextLayout(debugText, new Vector2(258f, 24f), new Vector2(0f, -174f));
            ConfigureTextFitting(debugText, 10f, 13f, false, new Vector4(6f, 2f, 6f, 2f));
        }

        private static void EnsureStatCard(RectTransform root, string name, string label, string value, Vector2 position)
        {
            var group = EnsureChildRect(root, name);
            ConfigureRect(group.gameObject, new Vector2(132f, 56f), position);
            EnsureRoundedDecor(group, "HtmlVisual_Background", new Vector2(132f, 56f), Vector2.zero, WithAlpha(RiceLight, 0.82f), 14f, WithAlpha(GoldStroke, 0.30f), new Vector2(0.8f, -0.8f));
            EnsureText(group, "HtmlVisual_Value", value, new Vector2(124f, 30f), new Vector2(0f, 8f), 22f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
            EnsureText(group, "HtmlVisual_Label", label, new Vector2(124f, 24f), new Vector2(0f, -16f), 14f, FontStyles.Bold, TrainingTextSecondary, TextAlignmentOptions.Center);
            DisableRaycastForVisualTree(group.gameObject);
        }

        private static void HideBottomHint(GameObject panel)
        {
            var root = GetPanelRoot(panel);
            ClearVisualText(root, "HtmlVisual_BottomHint");
        }

        private static void ConfigureTrainingFooterButton(Button button, Vector2 position)
        {
            if (button == null) return;
            ConfigureRect(button.gameObject, new Vector2(186f, 64f), position);
        }

        private static void StyleBottomDock(GameObject panel)
        {
            var root = GetPanelRoot(panel);
            if (root == null) return;

            SetVisualDecorTransparent(root, "HtmlVisual_BottomDock");
            ClearVisualText(root, "HtmlVisual_DockMinimize");
            ClearVisualText(root, "HtmlVisual_DockClose");
            ClearVisualText(root, "HtmlVisual_DockSettings");
        }

        private static void StyleVideoQuadPanel(RehabVideoGuideController guide)
        {
            if (guide == null || guide.videoQuad == null) return;

            var root = EnsureVideoPanelVisualRoot(guide.videoQuad.transform);
            if (root == null) return;

            // The video quad remains the only full-size surface. Decorations are edge-only so
            // their depth can never cover the RenderTexture on device.
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            DisableLegacyVideoCoverSurfaces(root);
            var hasClosedFrame = HasClosedVideoFrame(guide.videoQuad.transform);
            if (hasClosedFrame)
            {
                SetQuadDecorVisible(root, "HtmlVisual_VideoTopLine", false);
                SetQuadDecorVisible(root, "HtmlVisual_VideoBottomLine", false);
                SetQuadDecorVisible(root, "HtmlVisual_VideoLeftLine", false);
                SetQuadDecorVisible(root, "HtmlVisual_VideoRightLine", false);
            }
            else
            {
                EnsureQuadDecor(root, "HtmlVisual_VideoTopLine", new Vector2(1.10f, 0.020f), new Vector3(0f, 0.548f, -0.018f), WithAlpha(Jade, 0.92f));
                EnsureQuadDecor(root, "HtmlVisual_VideoBottomLine", new Vector2(1.10f, 0.016f), new Vector3(0f, -0.548f, -0.018f), WithAlpha(GoldStroke, 0.86f));
                EnsureQuadDecor(root, "HtmlVisual_VideoLeftLine", new Vector2(0.018f, 1.08f), new Vector3(-0.548f, 0f, -0.018f), WithAlpha(GoldStroke, 0.86f));
                EnsureQuadDecor(root, "HtmlVisual_VideoRightLine", new Vector2(0.018f, 1.08f), new Vector3(0.548f, 0f, -0.018f), WithAlpha(GoldStroke, 0.86f));
            }
            EnsureQuadDecor(root, "HtmlVisual_VideoTopPill", new Vector2(0.48f, 0.062f), new Vector3(0f, 0.605f, -0.018f), WithAlpha(CardNormal, 0.98f));
            EnsureQuadDecor(root, "HtmlVisual_VideoBottomPill", new Vector2(0.66f, 0.054f), new Vector3(0f, -0.605f, -0.018f), WithAlpha(RiceMid, 0.98f));
            EnsureWorldText(root, "HtmlVisual_VideoTitle", "\u52a8\u4f5c\u793a\u8303", new Vector3(0f, 0.605f, -0.022f), new Vector2(0.46f, 0.07f), 0.07f, FontStyles.Bold, TextPrimary);
            EnsureWorldText(root, "HtmlVisual_VideoNote", "\u653e\u6162\u547c\u5438  \u8ddf\u968f\u793a\u8303", new Vector3(0f, -0.605f, -0.022f), new Vector2(0.62f, 0.06f), 0.040f, FontStyles.Normal, TextSecondary);

            StyleExistingVideoFrameBars(guide.videoQuad.transform);
        }

        private static void StyleVideoRawImageFrame(RawImage rawImage)
        {
            if (rawImage == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] VideoRawImage not found, skip video frame styling");
                return;
            }

            var root = EnsureVideoCanvasShell(rawImage);
            if (root == null) return;

            var size = GetUsableSize(rawImage.rectTransform);
            var shellSize = size + new Vector2(38f, 94f);
            EnsureRoundedDecor(root, "HtmlVisual_VideoPanelBackground", shellSize, new Vector2(0f, 3f), WithAlpha(RiceLight, 0.98f), 24f, WithAlpha(GoldStroke, 0.82f), new Vector2(1.5f, -1.5f));
            EnsureRoundedDecor(root, "HtmlVisual_VideoShadow", shellSize + new Vector2(12f, 12f), new Vector2(0f, -5f), WithAlpha(WarmShadow, 0.82f), 28f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_VideoTopBorder", new Vector2(size.x + 26f, 7f), new Vector2(0f, size.y * 0.5f + 11f), WithAlpha(Jade, 0.94f), 3.5f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_VideoBottomBorder", new Vector2(size.x + 26f, 6f), new Vector2(0f, -size.y * 0.5f - 11f), WithAlpha(GoldStroke, 0.88f), 3f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_VideoLeftBorder", new Vector2(7f, size.y + 26f), new Vector2(-size.x * 0.5f - 11f, 0f), WithAlpha(GoldStroke, 0.88f), 3.5f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_VideoRightBorder", new Vector2(7f, size.y + 26f), new Vector2(size.x * 0.5f + 11f, 0f), WithAlpha(GoldStroke, 0.88f), 3.5f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_VideoTitleBar", new Vector2(176f, 36f), new Vector2(0f, size.y * 0.5f + 35f), WithAlpha(CardNormal, 0.98f), 18f, WithAlpha(GoldStroke, 0.62f), new Vector2(0.8f, -0.8f));
            EnsureIconOrFallback(root, "HtmlVisual_Icon_Video", "camera", ElderCareIconType.Video, new Vector2(18f, 18f), new Vector2(-58f, size.y * 0.5f + 35f), Jade);
            EnsureText(root, "HtmlVisual_VideoTitle", "\u52a8\u4f5c\u793a\u8303", new Vector2(106f, 28f), new Vector2(18f, size.y * 0.5f + 35f), 16f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
            EnsureRoundedDecor(root, "HtmlVisual_VideoNote", new Vector2(246f, 28f), new Vector2(0f, -size.y * 0.5f - 35f), WithAlpha(RiceMid, 0.96f), 14f, WithAlpha(GoldStroke, 0.34f), new Vector2(0.6f, -0.6f));
            EnsureText(root, "HtmlVisual_VideoNoteText", "\u653e\u6162\u547c\u5438  \u8ddf\u968f\u793a\u8303", new Vector2(228f, 22f), new Vector2(0f, -size.y * 0.5f - 35f), 13f, FontStyles.Normal, TextSecondary, TextAlignmentOptions.Center);
            DisableRaycastForVisualTree(root.gameObject);

            Debug.Log(
                string.Format(
                    "[HtmlStyleRehabVisualSkin] Styled video frame, rawImage={0}, texture={1}",
                    rawImage.name,
                    rawImage.texture != null ? rawImage.texture.name : "<none>"));
        }

        private static void StyleVideoControlButtons(GameObject videoPanel)
        {
            if (videoPanel == null) return;

            var buttons = videoPanel.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null) continue;

                var rect = GetButtonRect(button, button.name);
                if (rect == null) continue;

                EnsureButtonTargetGraphicPresent(button);
                FadeButtonTargetGraphic(button, 0.08f);

                var root = EnsureVisualRoot(button.transform);
                ConfigureStretch(root);
                MoveToBack(root);

                var size = GetUsableSize(rect);
                var primary = i == 0;
                EnsureRoundedDecor(root, "HtmlVisual_Background", size - new Vector2(2f, 2f), Vector2.zero, WithAlpha(primary ? AmberLight : CardNormal, 0.96f), size.y * 0.48f, WithAlpha(primary ? GoldDeep : GoldStroke, 0.62f), new Vector2(1f, -1f));
                StyleExistingButtonLabelOnly(button, Mathf.Clamp(size.y * 0.32f, 16f, 22f), primary ? new Color32(0xFF, 0xF8, 0xE4, 0xFF) : TextPrimary, FontStyles.Bold);
                DisableRaycastForVisualTree(root.gameObject);
            }

            Debug.Log("[HtmlStyleRehabVisualSkin] Styled video controls");
        }

        private static void StyleSpatialVideoControls()
        {
            var spatialControls = UnityEngine.Object.FindObjectOfType<RehabSpatialRayControl>(true);
            var root = spatialControls != null ? spatialControls.controlCanvasRoot : null;
            if (root == null) return;

            var background = root.GetComponent<Image>();
            if (background != null)
            {
                background.color = WithAlpha(RiceMid, 0.98f);
                background.raycastTarget = false;
            }

            var outline = root.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = WithAlpha(GoldStroke, 0.74f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);
            }

            StyleVisualDivider(root.transform, "TopTrace", Jade, 0.74f);
            StyleVisualDivider(root.transform, "BottomTrace", GoldStroke, 0.64f);
            StyleExistingText(spatialControls.statusText, 18f, FontStyles.Normal, TextSecondary, TextAlignmentOptions.Center);
            StyleVideoControlButtons(root);
        }

        private static void StyleVisualDivider(Transform root, string name, Color color, float alpha)
        {
            var divider = FindChildRecursive(root, name);
            var image = divider != null ? divider.GetComponent<Image>() : null;
            if (image == null) return;

            image.color = WithAlpha(color, alpha);
            image.raycastTarget = false;
        }

        private static void StyleExistingVideoFrameBars(Transform videoQuad)
        {
            var frameRoot = videoQuad != null ? videoQuad.Find("VideoFrameRoot") : null;
            if (frameRoot == null) return;

            for (var i = 0; i < frameRoot.childCount; i++)
            {
                var child = frameRoot.GetChild(i);
                var renderer = child != null ? child.GetComponent<Renderer>() : null;
                if (renderer == null) continue;

                var accent = child.name.IndexOf("Top", System.StringComparison.OrdinalIgnoreCase) >= 0;
                renderer.sharedMaterial = GetVideoDecorMaterial(accent ? WithAlpha(Jade, 0.90f) : WithAlpha(GoldStroke, 0.92f));
            }
        }

        private static bool HasClosedVideoFrame(Transform videoQuad)
        {
            if (videoQuad == null || videoQuad.parent == null) return false;

            var frame = videoQuad.parent.Find("VideoClosedFrame");
            return frame != null && frame.gameObject.activeSelf;
        }

        private static void SetQuadDecorVisible(Transform root, string name, bool visible)
        {
            var child = root != null ? root.Find(name) : null;
            var renderer = child != null ? child.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        private static void DisableLegacyVideoCoverSurfaces(Transform root)
        {
            if (root == null) return;

            var coverNames = new[]
            {
                "HtmlVisual_VideoShadow",
                "HtmlVisual_VideoRiceCard",
                "HtmlVisual_VideoInnerWash",
                "HtmlVisual_VideoTopBand",
                "HtmlVisual_VideoNotePill"
            };

            for (var i = 0; i < coverNames.Length; i++)
            {
                var child = root.Find(coverNames[i]);
                var renderer = child != null ? child.GetComponent<Renderer>() : null;
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        private static void EnsureLeafDecor(RectTransform root, Vector2 panelSize)
        {
            EnsureRoundedDecor(root, "HtmlVisual_BambooLeafA", new Vector2(18f, 64f), new Vector2(panelSize.x * 0.36f, panelSize.y * 0.28f), WithAlpha(Jade, 0.12f), 9f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_BambooLeafB", new Vector2(16f, 56f), new Vector2(panelSize.x * 0.41f, panelSize.y * 0.22f), WithAlpha(Jade, 0.10f), 8f, Color.clear, Vector2.zero);
            EnsureRoundedDecor(root, "HtmlVisual_BambooStem", new Vector2(3f, 86f), new Vector2(panelSize.x * 0.34f, panelSize.y * 0.14f), WithAlpha(new Color32(0x6B, 0x5A, 0x3E, 0xFF), 0.14f), 1.5f, Color.clear, Vector2.zero);
        }

        private static void EnsurePill(RectTransform root, string name, string text, Vector2 size, Vector2 position, Color fill, Color accent, Color textColor)
        {
            var group = EnsureChildRect(root, name);
            ConfigureRect(group.gameObject, size, position);
            EnsureRoundedDecor(group, "HtmlVisual_Background", size, Vector2.zero, WithAlpha(fill, 0.86f), size.y * 0.5f, WithAlpha(accent, 0.38f), new Vector2(0.7f, -0.7f));
            var hasDurationMarker = name.IndexOf("Duration", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var hasIntensityMarker = name.IndexOf("Intensity", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (hasDurationMarker)
            {
                EnsureIconOrFallback(group, "HtmlVisual_Icon_Hourglass", "hourglass", ElderCareIconType.Video, new Vector2(14f, 14f), new Vector2(-size.x * 0.34f, 0f), accent);
            }

            if (hasIntensityMarker)
            {
                var dotCount = text != null && text.IndexOf("\u4e2d", System.StringComparison.Ordinal) >= 0 ? 2 : 1;
                for (var i = 0; i < 3; i++)
                {
                    var dotColor = i < dotCount ? WithAlpha(accent, 0.92f) : WithAlpha(accent, 0.22f);
                    EnsureRoundedDecor(group, "HtmlVisual_IntensityDot" + i, new Vector2(6f, 6f), new Vector2(-size.x * 0.33f + i * 9f, 0f), dotColor, 3f, Color.clear, Vector2.zero);
                }
            }

            var textOffset = hasDurationMarker || hasIntensityMarker ? 6f : 0f;
            EnsureText(group, "HtmlVisual_Text", text, size - new Vector2(24f, 0f), new Vector2(textOffset, 0f), 14f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            DisableRaycastForVisualTree(group.gameObject);
        }

        private static RectTransform EnsurePanelVisualRoot(Transform parent)
        {
            return EnsureNamedRect(parent, PanelVisualRootName);
        }

        private static Transform EnsureVideoPanelVisualRoot(Transform parent)
        {
            if (parent == null) return null;

            var root = parent.Find(VideoVisualRootName);
            if (root == null)
            {
                var legacyRoot = parent.Find(LegacyVideoVisualRootName);
                if (legacyRoot != null)
                {
                    legacyRoot.name = VideoVisualRootName;
                    root = legacyRoot;
                }
            }

            return root != null ? root : EnsureTransformChild(parent, VideoVisualRootName);
        }

        private static RectTransform EnsureVideoCanvasShell(RawImage rawImage)
        {
            if (rawImage == null || rawImage.transform.parent == null) return null;

            var parent = rawImage.transform.parent;
            var root = parent.Find(VideoVisualRootName) as RectTransform;
            if (root == null)
            {
                var legacyRoot = rawImage.transform.Find(LegacyVideoVisualRootName) as RectTransform;
                if (legacyRoot != null)
                {
                    legacyRoot.SetParent(parent, false);
                    legacyRoot.name = VideoVisualRootName;
                    root = legacyRoot;
                }
            }

            if (root == null)
            {
                root = EnsureNamedRect(parent, VideoVisualRootName);
            }

            if (root == null) return null;

            var rawRect = rawImage.rectTransform;
            root.anchorMin = rawRect.anchorMin;
            root.anchorMax = rawRect.anchorMax;
            root.pivot = rawRect.pivot;
            root.anchoredPosition = rawRect.anchoredPosition;
            root.sizeDelta = rawRect.sizeDelta;
            root.offsetMin = rawRect.offsetMin;
            root.offsetMax = rawRect.offsetMax;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.SetSiblingIndex(0);
            DisableRaycastForVisualTree(root.gameObject);
            return root;
        }

        private static RectTransform EnsureVisualRoot(Transform parent)
        {
            return EnsureNamedRect(parent, VisualRootName);
        }

        private static Transform EnsureTransformChild(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name)) return null;

            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static RectTransform EnsureNamedRect(Transform parent, string name)
        {
            if (parent == null) return null;

            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }

            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            DisableRaycastForVisualTree(go);
            return rect;
        }

        private static RectTransform EnsureChildRect(RectTransform parent, string name)
        {
            if (parent == null) return null;

            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }

            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            DisableRaycastForVisualTree(go);
            return rect;
        }

        private static ElderCareRoundedPanel EnsureRoundedDecor(
            RectTransform parent,
            string name,
            Vector2 size,
            Vector2 position,
            Color color,
            float radius,
            Color outlineColor,
            Vector2 outlineDistance)
        {
            var rect = EnsureChildRect(parent, name);
            if (rect == null) return null;

            ConfigureRect(rect.gameObject, size, position);

            var panel = rect.GetComponent<ElderCareRoundedPanel>();
            if (panel == null)
            {
                panel = rect.gameObject.AddComponent<ElderCareRoundedPanel>();
            }

            panel.color = color;
            panel.cornerRadius = Mathf.Max(0f, radius);
            panel.cornerSegments = 10;
            panel.raycastTarget = false;
            panel.SetAllDirty();

            var outline = rect.GetComponent<Outline>();
            if (outlineColor.a > 0.001f)
            {
                if (outline == null)
                {
                    outline = rect.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = outlineColor;
                outline.effectDistance = outlineDistance;
            }
            else if (outline != null)
            {
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
            }

            DisableRaycastForVisualTree(rect.gameObject);
            return panel;
        }

        private static Renderer EnsureQuadDecor(Transform parent, string name, Vector2 size, Vector3 localPosition, Color color)
        {
            var transform = EnsureTransformChild(parent, name);
            if (transform == null) return null;

            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y), 1f);

            var filter = transform.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = transform.gameObject.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = GetUnitQuadMesh();

            var renderer = transform.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = transform.gameObject.AddComponent<MeshRenderer>();
            }

            renderer.enabled = true;
            renderer.sharedMaterial = GetVideoDecorMaterial(color);
            return renderer;
        }

        private static TMP_Text EnsureText(
            RectTransform parent,
            string name,
            string text,
            Vector2 size,
            Vector2 position,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = EnsureChildRect(parent, name);
            if (rect == null) return null;

            ConfigureRect(rect.gameObject, size, position);

            var label = rect.GetComponent<TMP_Text>();
            if (label == null)
            {
                label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            label.text = SanitizeAndLog(text, rect.name);
            if (_activeUiFont != null)
            {
                label.font = _activeUiFont;
            }
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            ConfigureTextFitting(label, Mathf.Max(11f, fontSize * 0.72f), fontSize, true, new Vector4(4f, 2f, 4f, 2f));
            return label;
        }

        private static TextMeshPro EnsureWorldText(
            Transform parent,
            string name,
            string text,
            Vector3 localPosition,
            Vector2 size,
            float fontSize,
            FontStyles fontStyle,
            Color color)
        {
            var transform = EnsureTransformChild(parent, name);
            if (transform == null) return null;

            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var label = transform.GetComponent<TextMeshPro>();
            if (label == null)
            {
                label = transform.gameObject.AddComponent<TextMeshPro>();
            }

            label.text = SanitizeAndLog(text, transform.name);
            if (_activeUiFont != null)
            {
                label.font = _activeUiFont;
            }
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.rectTransform.sizeDelta = size;
            ConfigureTextFitting(label, Mathf.Max(0.025f, fontSize * 0.72f), fontSize, false, Vector4.zero);
            return label;
        }

        public static string SanitizeLabelText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var builder = new System.Text.StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];
                if (char.IsHighSurrogate(character) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    // Emoji are not guaranteed by the Chinese TMP font. Their visual role is
                    // supplied by Image or line-icon decoration instead.
                    i++;
                    continue;
                }

                switch (character)
                {
                    case '\u2014':
                    case '\u2013':
                        builder.Append('-');
                        continue;
                    case '\u00b7':
                    case '\u3000':
                        builder.Append(' ');
                        continue;
                    case '\uFF0C':
                        builder.Append(',');
                        continue;
                    case '\uFF1A':
                        builder.Append(':');
                        continue;
                    case '\uFF01':
                        builder.Append('!');
                        continue;
                    case '\uFF1B':
                        builder.Append(';');
                        continue;
                    case '\u3002':
                        builder.Append('.');
                        continue;
                    case '\uFF08':
                        builder.Append('(');
                        continue;
                    case '\uFF09':
                        builder.Append(')');
                        continue;
                    case '\uFF05':
                        builder.Append('%');
                        continue;
                    case '\uFF0F':
                        builder.Append('/');
                        continue;
                    case '\u2190':
                    case '\u2192':
                    case '\u2713':
                    case '\u2714':
                    case '\u2726':
                    case '\u2600':
                    case '\u23F3':
                    case '\u23F8':
                    case '\u25B6':
                        continue;
                }

                if ((character >= '\u4E00' && character <= '\u9FFF') ||
                    (character >= '\u3400' && character <= '\u4DBF') ||
                    character <= '\u007F')
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static string SanitizeAndLog(string text, string objectName)
        {
            var safeText = SanitizeLabelText(text);
            if (!string.Equals(text ?? string.Empty, safeText, System.StringComparison.Ordinal))
            {
                Debug.Log("[HtmlStyleRehabVisualSkin] Sanitized unsupported glyphs in " + objectName);
            }

            return safeText;
        }

        private static void EnsureIconOrFallback(
            RectTransform parent,
            string name,
            string iconName,
            ElderCareIconType fallbackType,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            var rect = EnsureChildRect(parent, name);
            if (rect == null) return;

            ConfigureRect(rect.gameObject, size, position);
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            var fallbackRect = EnsureChildRect(rect, "HtmlVisual_Fallback");
            if (fallbackRect == null) return;
            ConfigureStretch(fallbackRect);
            var fallback = fallbackRect.GetComponent<ElderCareLineIcon>();
            var sprite = ResolveIconSprite(iconName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = color;
                if (fallback != null)
                {
                    fallback.color = Color.clear;
                    fallback.raycastTarget = false;
                }
            }
            else
            {
                image.sprite = null;
                image.color = Color.clear;
                if (fallback == null)
                {
                    fallback = fallbackRect.gameObject.AddComponent<ElderCareLineIcon>();
                }

                fallback.iconType = fallbackType;
                fallback.strokeWidth = Mathf.Max(2f, size.x * 0.09f);
                fallback.color = color;
                fallback.raycastTarget = false;
            }

            image.raycastTarget = false;
            DisableRaycastForVisualTree(rect.gameObject);
        }

        private static Sprite ResolveIconSprite(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            if (IconSpriteCache.TryGetValue(iconName, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(IconResourceRoot + iconName);
#if UNITY_EDITOR
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/HtmlSvgIcons/" + iconName + ".png");
            }
#endif

            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(IconResourceRoot + iconName);
#if UNITY_EDITOR
                if (texture == null)
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/HtmlSvgIcons/" + iconName + ".png");
                }
#endif
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            IconSpriteCache[iconName] = sprite;
            if (sprite != null)
            {
                Debug.Log("[HtmlStyleRehabVisualSkin] Icon loaded: " + iconName);
            }
            else
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] Icon missing: " + iconName + ", use fallback");
            }

            return sprite;
        }

        private static TMP_FontAsset ResolveUiFont(RehabModeSelectUI ui)
        {
            if (ui == null) return null;

            var labels = ui.GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label != null && label.font != null && !IsVisualTransform(label.transform))
                {
                    return label.font;
                }
            }

            return null;
        }

        private static void LogIconResourceAvailability()
        {
            if (_iconResourceAvailabilityLogged) return;

            _iconResourceAvailabilityLogged = true;
            var probe = Resources.Load<UnityEngine.Object>(IconResourceRoot + "lotus");
            if (probe == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] svg_icons folder not found, use fallback icons");
            }
        }

        private static void ClearVisualText(RectTransform parent, string name)
        {
            var child = parent != null ? parent.Find(name) : null;
            var text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                text.text = string.Empty;
                text.raycastTarget = false;
            }
        }

        private static void SetVisualDecorTransparent(RectTransform parent, string name)
        {
            var child = parent != null ? parent.Find(name) : null;
            var graphic = child != null ? child.GetComponent<Graphic>() : null;
            if (graphic != null)
            {
                graphic.color = Color.clear;
                graphic.raycastTarget = false;
            }
        }

        private static void HideLegacyGraphic(Transform root, string name)
        {
            var target = FindChildRecursive(root, name);
            var graphic = target != null ? target.GetComponent<Graphic>() : null;
            if (graphic == null) return;

            graphic.color = Color.clear;
            graphic.raycastTarget = false;
        }

        private static void StyleButtonLabel(Button button, string text, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment, Vector4 margin)
        {
            var label = FindPrimaryButtonLabel(button);
            if (label == null) return;

            label.text = SanitizeAndLog(text, label.name);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.margin = margin;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            ConfigureTextFitting(label, Mathf.Max(12f, fontSize * 0.72f), fontSize, false, margin);
        }

        private static void StyleExistingButtonLabelOnly(Button button, float fontSize, Color color, FontStyles style)
        {
            var label = FindPrimaryButtonLabel(button);
            if (label == null) return;

            label.text = SanitizeAndLog(label.text, label.name);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.margin = Vector4.zero;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            ConfigureTextFitting(label, Mathf.Max(12f, fontSize * 0.72f), fontSize, false, new Vector4(6f, 2f, 6f, 2f));
        }

        private static void StyleExistingText(TMP_Text label, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            if (label == null) return;

            label.text = SanitizeAndLog(label.text, label.name);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            ConfigureTextFitting(label, Mathf.Max(12f, fontSize * 0.72f), fontSize, false, new Vector4(6f, 2f, 6f, 2f));
        }

        private static void ConfigureTextFitting(
            TMP_Text label,
            float minimumFontSize,
            float maximumFontSize,
            bool wordWrapping,
            Vector4 margin)
        {
            if (label == null) return;

            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(0.001f, minimumFontSize);
            label.fontSizeMax = Mathf.Max(label.fontSizeMin, maximumFontSize);
            label.enableWordWrapping = wordWrapping;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = margin;
        }

        private static void ConfigureButtonLabelLayout(Button button, Vector2 size, Vector2 position)
        {
            var label = FindPrimaryButtonLabel(button);
            if (label == null) return;

            ConfigureTextLayout(label, size, position);
        }

        private static void ConfigureHoverFeedback(
            Button button,
            RectTransform visualRoot,
            Graphic surface,
            Graphic glow,
            Color normalSurfaceColor,
            Color accentColor,
            float hoverScale,
            float hoverLift)
        {
            if (button == null || visualRoot == null || surface == null) return;

            var feedback = button.GetComponent<RehabButtonHoverFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<RehabButtonHoverFeedback>();
            }

            var opaqueAccent = accentColor;
            opaqueAccent.a = normalSurfaceColor.a;
            var hoverSurfaceColor = Color.Lerp(normalSurfaceColor, opaqueAccent, 0.16f);
            feedback.Configure(
                visualRoot,
                surface,
                glow,
                normalSurfaceColor,
                hoverSurfaceColor,
                WithAlpha(accentColor, 0.06f),
                WithAlpha(accentColor, 0.34f),
                hoverScale,
                hoverLift);
        }

        private static void ConfigureTextLayout(TMP_Text label, Vector2 size, Vector2 position)
        {
            if (label == null) return;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetVisualTreeTransparent(Transform root)
        {
            if (root == null) return;

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                var graphic = graphics[i];
                if (graphic == null) continue;

                var color = graphic.color;
                color.a = 0f;
                graphic.color = color;
                graphic.raycastTarget = false;
            }
        }

        private static TMP_Text FindPrimaryButtonLabel(Button button)
        {
            if (button == null) return null;

            var labels = button.GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label != null && !IsVisualTransform(label.transform))
                {
                    return label;
                }
            }

            return null;
        }

        private static bool IsVisualTransform(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.name.StartsWith("HtmlVisual_", System.StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private static void EnsureButtonTargetGraphicPresent(Button button)
        {
            if (button == null) return;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
                return;
            }

            var graphic = button.GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = button.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.05f);
                image.raycastTarget = true;
                graphic = image;
            }

            button.targetGraphic = graphic;
            button.targetGraphic.raycastTarget = true;
        }

        private static void FadeButtonTargetGraphic(Button button, float alpha)
        {
            if (button == null || button.targetGraphic == null) return;

            var color = button.targetGraphic.color;
            color.a = Mathf.Clamp(alpha, 0.02f, 0.15f);
            button.targetGraphic.color = color;
            button.targetGraphic.raycastTarget = true;
        }

        private static RectTransform GetPanelRoot(GameObject panel)
        {
            return panel != null ? panel.transform.Find(PanelVisualRootName) as RectTransform : null;
        }

        private static RectTransform GetRect(GameObject go, string logName)
        {
            if (go == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + logName + " is null, skip styling");
                return null;
            }

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + logName + " has no RectTransform, skip styling");
            }

            return rect;
        }

        private static RectTransform GetButtonRect(Button button, string logName)
        {
            if (button == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + logName + " is null, skip styling");
                return null;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + logName + " has no RectTransform, skip styling");
            }

            return rect;
        }

        private static Vector2 GetUsableSize(RectTransform rect)
        {
            if (rect == null) return Vector2.zero;

            var size = rect.rect.size;
            if (size.x <= 1f || size.y <= 1f)
            {
                size = rect.sizeDelta;
            }

            size.x = Mathf.Max(1f, size.x);
            size.y = Mathf.Max(1f, size.y);
            return size;
        }

        private static void ConfigureStretch(RectTransform rect)
        {
            if (rect == null) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 position)
        {
            if (go == null) return null;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void MoveToBack(RectTransform rect)
        {
            if (rect == null) return;

            rect.SetSiblingIndex(0);
        }

        private static Button FindButton(GameObject root, string name)
        {
            var found = FindChildRecursive(root != null ? root.transform : null, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static TMP_Text FindChildText(Transform root, string name)
        {
            var found = FindChildRecursive(root, name);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static void DisableRaycastForVisualTree(GameObject go)
        {
            if (go == null) return;

            var graphics = go.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].raycastTarget = false;
                }
            }
        }

        private static void LogStyledButton(string label, Button button, RectTransform visualRoot)
        {
            if (button == null)
            {
                Debug.LogWarning("[HtmlStyleRehabVisualSkin] " + label + " is null, skip styling");
                return;
            }

            var targetGraphic = button.targetGraphic;
            Debug.Log(
                string.Format(
                    "[HtmlStyleRehabVisualSkin] Styled {0} visualRoot={1} targetGraphic={2} targetRaycast={3}",
                    label,
                    visualRoot != null ? visualRoot.name : "<null>",
                    targetGraphic != null ? targetGraphic.name : "<null>",
                    targetGraphic != null && targetGraphic.raycastTarget));
        }

        private static Mesh GetUnitQuadMesh()
        {
            if (_unitQuadMesh != null) return _unitQuadMesh;

            _unitQuadMesh = new Mesh
            {
                name = "HtmlVisualUnitQuad"
            };
            _unitQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f)
            };
            _unitQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            _unitQuadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _unitQuadMesh.RecalculateNormals();
            _unitQuadMesh.RecalculateBounds();
            return _unitQuadMesh;
        }

        private static Material GetVideoDecorMaterial(Color color)
        {
            var key = ColorUtility.ToHtmlStringRGBA(color);
            if (VideoMaterialCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "HtmlVisualVideo_" + key,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.renderQueue = 3000;
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            VideoMaterialCache[key] = material;
            return material;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
