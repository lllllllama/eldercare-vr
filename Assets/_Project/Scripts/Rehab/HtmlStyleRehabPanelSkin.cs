using System.Collections.Generic;
using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public static class HtmlStyleRehabPanelSkin
    {
        private const string IconResourceRoot = "HtmlSvgIcons/";
        private const string IconLotus = "lotus";
        private const string IconCyclone = "cyclone";
        private const string IconPlay = "play";
        private const string IconHome = "home";
        private const string IconHourglass = "hourglass";
        private const string IconCheck = "check";

        private static readonly Vector2 PanelSize = ElderCareUiTheme.RehabCanvasSize;
        private static readonly Color PanelBrown = new Color32(0x1B, 0x14, 0x0E, 0xF0);
        private static readonly Color PanelStrokeWarm = new Color(0.76f, 0.56f, 0.34f, 0.42f);
        private static readonly Color CardSurface = new Color(0.20f, 0.15f, 0.10f, 0.88f);
        private static readonly Color TextWarm = new Color32(0xF0, 0xD1, 0xA6, 0xF4);
        private static readonly Color TextGold = new Color32(0xC9, 0xA2, 0x6B, 0xEA);
        private static readonly Dictionary<string, Sprite> IconSpriteCache = new Dictionary<string, Sprite>();

        public static void Apply(RehabModeSelectUI ui)
        {
            if (ui == null) return;

            ApplyMainMenuPanel(ui);
            ApplyTrainingSelectPanel(ui);
            ApplyTrainingPanel(ui);
            ApplyResultPanel(ui);
        }

        private static void ApplyMainMenuPanel(RehabModeSelectUI ui)
        {
            if (ui.mainMenuPanel == null) return;

            StylePanelRoot(ui.mainMenuPanel);
            ConfigureText(FindText(ui.mainMenuPanel.transform, "Title"), "\u5eb7\u590d\u8fd0\u52a8", new Vector2(560f, 70f), new Vector2(0f, 122f), 44f, FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);
            StyleButton(ui.rehabButton, "\u5eb7\u590d\u8fd0\u52a8", IconLotus, null, new Vector2(420f, 96f), new Vector2(0f, -22f), ElderCareUiTheme.Green, true);
        }

        private static void ApplyTrainingSelectPanel(RehabModeSelectUI ui)
        {
            if (ui.rehabTrainingSelectPanel == null) return;

            StylePanelRoot(ui.rehabTrainingSelectPanel);
            ConfigureText(FindText(ui.rehabTrainingSelectPanel.transform, "Title"), "\u9009\u62e9\u5eb7\u590d\u8bad\u7ec3", new Vector2(560f, 58f), new Vector2(0f, 156f), 42f, FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);
            ConfigureText(GetOrCreateText(ui.rehabTrainingSelectPanel.transform, "HtmlSubtitle"), "\u8bf7\u6839\u636e\u4eca\u5929\u7684\u72b6\u6001\u9009\u62e9\u8bad\u7ec3\u65b9\u5f0f", new Vector2(560f, 32f), new Vector2(0f, 108f), 22f, FontStyles.Normal, TextGold, TextAlignmentOptions.Center);

            StyleButton(ui.baduanjinButton, "\u516b\u6bb5\u9526\u8bad\u7ec3", IconLotus, "\u8212\u5c55\u4e0a\u80a2\uff0c\u7a33\u5b9a\u547c\u5438", new Vector2(270f, 168f), new Vector2(-148f, 6f), ElderCareUiTheme.Green, true);
            StyleButton(ui.taiChiButton, "\u592a\u6781\u8bad\u7ec3", IconCyclone, "\u7f13\u6162\u8f6c\u79fb\uff0c\u5e73\u8861\u534f\u8c03", new Vector2(270f, 168f), new Vector2(148f, 6f), ElderCareUiTheme.Cyan, true);
            StyleButton(ui.backButton, "\u8fd4\u56de\u4e3b\u9875", IconHome, null, new Vector2(540f, 58f), new Vector2(0f, -160f), ElderCareUiTheme.Orange, true);
        }

        private static void ApplyTrainingPanel(RehabModeSelectUI ui)
        {
            if (ui.rehabTrainingPanel == null) return;

            var root = ui.rehabTrainingPanel.transform;
            StylePanelRoot(ui.rehabTrainingPanel);
            ConfigureText(FindText(root, "MovementTitle"), null, new Vector2(610f, 54f), new Vector2(0f, 158f), 38f, FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);
            ConfigureText(FindText(root, "StatusText"), null, new Vector2(610f, 44f), new Vector2(0f, 108f), 24f, FontStyles.Normal, TextGold, TextAlignmentOptions.Center);

            StyleDataBlock(FindText(root, "Value", "TimerBlock"), "\u5012\u8ba1\u65f6", IconHourglass, new Vector2(-150f, 18f), ElderCareUiTheme.Cyan);
            StyleDataBlock(FindText(root, "Value", "CompletionBlock"), "\u5b8c\u6210\u5ea6", IconCheck, new Vector2(150f, 18f), ElderCareUiTheme.Green);

            var safetyPanel = GetOrCreatePanel(root, "HtmlSafetyPanel", new Vector2(610f, 58f), new Vector2(0f, -110f), new Color(0f, 0f, 0f, 0.30f), 20f);
            safetyPanel.transform.SetAsFirstSibling();
            ConfigureText(FindText(root, "SafetyPromptText"), null, new Vector2(586f, 52f), new Vector2(0f, -110f), 22f, FontStyles.Bold, TextGold, TextAlignmentOptions.Center);
            ConfigureText(FindText(root, "DebugText"), null, new Vector2(360f, 32f), new Vector2(0f, -154f), 16f, FontStyles.Normal, WithAlpha(ElderCareUiTheme.TextPrimary, 0.52f), TextAlignmentOptions.Center);

            StyleButton(FindButton(root, "StartButton"), "\u5f00\u59cb", IconPlay, null, new Vector2(206f, 68f), new Vector2(-226f, -184f), ElderCareUiTheme.Green, true);
            StyleButton(ui.trainingBackButton != null ? ui.trainingBackButton : FindButton(root, "HomeButton"), "\u8fd4\u56de\u9009\u62e9", IconHome, null, new Vector2(206f, 68f), new Vector2(226f, -184f), ElderCareUiTheme.Orange, true);
        }

        private static void ApplyResultPanel(RehabModeSelectUI ui)
        {
            if (ui.trainingResultPanel == null) return;

            var root = ui.trainingResultPanel.transform;
            StylePanelRoot(ui.trainingResultPanel);
            ConfigureText(FindText(root, "Title"), "\u8bad\u7ec3\u7ed3\u679c", new Vector2(560f, 64f), new Vector2(0f, 128f), 42f, FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);
            ConfigureText(FindText(root, "Summary"), "\u8bad\u7ec3\u7ed3\u675f\u540e\uff0c\u7ed3\u679c\u5df2\u4fdd\u5b58\u5230\u672c\u673a", new Vector2(560f, 92f), new Vector2(0f, 36f), 26f, FontStyles.Normal, TextGold, TextAlignmentOptions.Center);
            StyleButton(ui.resultBackButton, "\u8fd4\u56de\u9009\u62e9", IconHome, null, new Vector2(300f, 72f), new Vector2(0f, -118f), ElderCareUiTheme.Green, true);
        }

        private static void StylePanelRoot(GameObject panelObject)
        {
            if (panelObject == null) return;

            var rect = panelObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            var panel = panelObject.GetComponent<ElderCareRoundedPanel>();
            if (panel == null)
            {
                panel = panelObject.AddComponent<ElderCareRoundedPanel>();
            }

            panel.color = PanelBrown;
            panel.cornerRadius = 34f;
            panel.cornerSegments = 12;
            panel.raycastTarget = false;
            panel.SetAllDirty();

            var outline = panelObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = panelObject.AddComponent<Outline>();
            }

            outline.effectColor = PanelStrokeWarm;
            outline.effectDistance = new Vector2(2f, -2f);

            var glow = GetOrCreatePanel(panelObject.transform, "HtmlInnerGlow", PanelSize - new Vector2(42f, 42f), Vector2.zero, new Color(0.70f, 0.50f, 0.30f, 0.035f), 28f);
            glow.transform.SetAsFirstSibling();
        }

        private static void StyleDataBlock(TMP_Text valueText, string label, string iconResource, Vector2 position, Color accent)
        {
            if (valueText == null) return;

            var block = valueText.transform.parent;
            if (block == null) return;

            var panel = ConfigurePanel(block.gameObject, new Vector2(286f, 128f), position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.28f), 0.94f), 22f, false);
            AddOutline(panel.gameObject, WithAlpha(accent, 0.34f), new Vector2(1.5f, -1.5f));
            ConfigureSvgIcon(GetOrCreateChild(block, "HtmlIcon"), iconResource, new Vector2(24f, 24f), new Vector2(-98f, 32f), 0.96f);
            ConfigureText(GetOrCreateText(block, "Label"), label, new Vector2(180f, 30f), new Vector2(22f, 32f), 20f, FontStyles.Normal, TextGold, TextAlignmentOptions.Left);
            ConfigureText(valueText, null, new Vector2(240f, 56f), new Vector2(0f, -18f), 34f, FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);
        }

        private static void StyleButton(Button button, string label, string iconResource, string description, Vector2 size, Vector2 position, Color accent, bool interactable)
        {
            if (button == null) return;

            var rect = ConfigureRect(button.gameObject, size, position);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.clear;
                image.raycastTarget = false;
            }

            var normalColor = WithAlpha(Color.Lerp(CardSurface, accent, interactable ? 0.16f : 0.08f), interactable ? 0.96f : 0.58f);
            var panel = ConfigurePanel(button.gameObject, size, position, normalColor, Mathf.Min(28f, size.y * 0.22f), true);
            AddOutline(button.gameObject, WithAlpha(accent, interactable ? 0.44f : 0.18f), new Vector2(1.5f, -1.5f));
            button.targetGraphic = panel;
            button.interactable = interactable;
            ConfigureButtonFeedback(button, panel, normalColor, accent, interactable);
            button.transform.SetAsLastSibling();

            var hasDescription = !string.IsNullOrEmpty(description);
            var iconSize = hasDescription ? 58f : Mathf.Clamp(size.y * 0.42f, 22f, 32f);
            var iconPosition = hasDescription ? new Vector2(0f, 38f) : new Vector2(-size.x * 0.34f, 0f);
            ConfigureSvgIcon(GetOrCreateChild(rect, "HtmlIcon"), iconResource, new Vector2(iconSize, iconSize), iconPosition, interactable ? 0.98f : 0.55f);

            var labelText = FindLabelText(button);
            var labelPosition = hasDescription ? new Vector2(0f, -28f) : new Vector2(24f, 0f);
            var labelSize = hasDescription ? new Vector2(size.x - 28f, 38f) : new Vector2(size.x - 82f, size.y - 8f);
            ConfigureText(labelText, label, labelSize, labelPosition, hasDescription ? 28f : Mathf.Clamp(size.y * 0.38f, 20f, 28f), FontStyles.Bold, TextWarm, TextAlignmentOptions.Center);

            if (hasDescription)
            {
                ConfigureText(GetOrCreateText(rect, "HtmlDescription"), description, new Vector2(size.x - 30f, 28f), new Vector2(0f, -64f), 17f, FontStyles.Normal, TextGold, TextAlignmentOptions.Center);
            }
        }

        private static void ConfigureButtonFeedback(Button button, Graphic targetGraphic, Color normalColor, Color accent, bool interactable)
        {
            if (button == null || targetGraphic == null) return;

            var highlightedColor = interactable
                ? WithAlpha(Color.Lerp(normalColor, accent, 0.26f), normalColor.a)
                : WithAlpha(normalColor, 0.50f);
            var pressedColor = interactable
                ? WithAlpha(Color.Lerp(normalColor, Color.black, 0.34f), normalColor.a)
                : WithAlpha(normalColor, 0.42f);
            var selectedColor = interactable
                ? WithAlpha(Color.Lerp(normalColor, accent, 0.18f), normalColor.a)
                : WithAlpha(normalColor, 0.50f);
            var disabledColor = WithAlpha(normalColor, 0.40f);

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = selectedColor;
            colors.disabledColor = disabledColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;

            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            targetGraphic.color = interactable ? normalColor : disabledColor;
        }

        private static TMP_Text FindLabelText(Button button)
        {
            var labelTransform = button.transform.Find("Label");
            var label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            if (label != null) return label;

            label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) return label;

            return GetOrCreateText(button.transform, "Label");
        }

        private static TMP_Text ConfigureText(TMP_Text text, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            if (text == null) return null;

            ConfigureRect(text.gameObject, size, position);
            if (value != null)
            {
                text.text = value;
            }

            if (text.font == null)
            {
                text.font = ResolveFont(text.transform.parent);
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.outlineColor = new Color(0f, 0f, 0f, 0.74f);
            text.outlineWidth = Mathf.Max(text.outlineWidth, 0.035f);
            return text;
        }

        private static Image ConfigureSvgIcon(GameObject go, string resourceName, Vector2 size, Vector2 position, float alpha)
        {
            if (go == null) return null;

            ConfigureRect(go, size, position);
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = LoadHtmlIconSprite(resourceName);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, image.sprite != null ? alpha : 0f);
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text FindText(Transform root, string name, string parentName = null)
        {
            var searchRoot = root;
            if (!string.IsNullOrEmpty(parentName))
            {
                var parent = FindChildRecursive(root, parentName);
                searchRoot = parent != null ? parent : root;
            }

            var found = FindChildRecursive(searchRoot, name);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Button FindButton(Transform root, string name)
        {
            var found = FindChildRecursive(root, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static TMP_Text GetOrCreateText(Transform parent, string name)
        {
            var go = GetOrCreateChild(parent, name);
            var text = go.GetComponent<TMP_Text>();
            var created = false;
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
                created = true;
            }

            if (created || text.font == null)
            {
                text.font = ResolveFont(parent);
            }

            return text;
        }

        private static ElderCareRoundedPanel GetOrCreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Color color, float radius)
        {
            return ConfigurePanel(GetOrCreateChild(parent, name), size, position, color, radius, false);
        }

        private static ElderCareRoundedPanel ConfigurePanel(GameObject go, Vector2 size, Vector2 position, Color color, float radius, bool raycastTarget)
        {
            ConfigureRect(go, size, position);
            var panel = go.GetComponent<ElderCareRoundedPanel>();
            if (panel == null)
            {
                panel = go.AddComponent<ElderCareRoundedPanel>();
            }

            panel.color = color;
            panel.cornerRadius = radius;
            panel.cornerSegments = 10;
            panel.raycastTarget = raycastTarget;
            panel.SetAllDirty();
            return panel;
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

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            if (parent == null) return null;

            var child = parent.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null) return;

            var outline = go.GetComponent<Outline>();
            if (outline == null)
            {
                outline = go.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Sprite LoadHtmlIconSprite(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            if (IconSpriteCache.TryGetValue(resourceName, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(IconResourceRoot + resourceName);
            if (sprite != null)
            {
                IconSpriteCache[resourceName] = sprite;
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(IconResourceRoot + resourceName);
            if (texture == null)
            {
                IconSpriteCache[resourceName] = null;
                return null;
            }

            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            IconSpriteCache[resourceName] = sprite;
            return sprite;
        }

        private static TMP_FontAsset ResolveFont(Transform context)
        {
            if (context != null)
            {
                var texts = context.GetComponentsInParent<TMP_Text>(true);
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i].font != null)
                    {
                        return texts[i].font;
                    }
                }

                var childTexts = context.root != null ? context.root.GetComponentsInChildren<TMP_Text>(true) : null;
                if (childTexts != null)
                {
                    for (var i = 0; i < childTexts.Length; i++)
                    {
                        if (childTexts[i] != null && childTexts[i].font != null)
                        {
                            return childTexts[i].font;
                        }
                    }
                }
            }

            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (var i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] != null && fonts[i].name == "RehabChineseTMP")
                {
                    return fonts[i];
                }
            }

            return null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
