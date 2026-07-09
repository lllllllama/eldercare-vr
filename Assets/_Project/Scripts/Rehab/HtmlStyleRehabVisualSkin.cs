using System.Collections.Generic;
using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public static class HtmlStyleRehabVisualSkin
    {
        private const string IconResourceRoot = "HtmlSvgIcons/";
        private const string IconLotus = "lotus";
        private const string IconCyclone = "cyclone";
        private const string IconPlay = "play";
        private const string IconHome = "home";
        private const string IconHourglass = "hourglass";
        private const string IconCheck = "check";

        private static readonly Color PanelTint = new Color32(0x1B, 0x14, 0x0E, 0x55);
        private static readonly Color GlowTint = new Color(0.70f, 0.50f, 0.30f, 0.08f);
        private static readonly Color ButtonGlow = new Color(1f, 0.78f, 0.42f, 0.10f);
        private static readonly Color DescriptionColor = new Color32(0xC9, 0xA2, 0x6B, 0xE6);
        private static readonly Dictionary<string, Sprite> IconSpriteCache = new Dictionary<string, Sprite>();

        public static void Apply(RehabModeSelectUI ui)
        {
            if (ui == null) return;

            StylePanel(ui.mainMenuPanel);
            StylePanel(ui.rehabTrainingSelectPanel);
            StylePanel(ui.rehabTrainingPanel);
            StylePanel(ui.trainingResultPanel);

            StyleButtonVisual(ui.rehabButton, IconLotus, null, ElderCareUiTheme.Green);
            StyleButtonVisual(ui.baduanjinButton, IconLotus, "\u8212\u5c55\u4e0a\u80a2\uff0c\u7a33\u5b9a\u547c\u5438", ElderCareUiTheme.Green);
            StyleButtonVisual(ui.taiChiButton, IconCyclone, "\u7f13\u6162\u8f6c\u79fb\uff0c\u5e73\u8861\u534f\u8c03", ElderCareUiTheme.Cyan);
            StyleButtonVisual(ui.backButton, IconHome, null, ElderCareUiTheme.Orange);
            StyleButtonVisual(ui.trainingBackButton, IconHome, null, ElderCareUiTheme.Orange);
            StyleButtonVisual(ui.resultBackButton, IconHome, null, ElderCareUiTheme.Green);

            StyleButtonVisual(FindButton(ui.rehabTrainingPanel != null ? ui.rehabTrainingPanel.transform : null, "StartButton"), IconPlay, null, ElderCareUiTheme.Green);
            StyleDataIcon(ui.rehabTrainingPanel != null ? ui.rehabTrainingPanel.transform : null, "TimerBlock", IconHourglass, ElderCareUiTheme.Cyan);
            StyleDataIcon(ui.rehabTrainingPanel != null ? ui.rehabTrainingPanel.transform : null, "CompletionBlock", IconCheck, ElderCareUiTheme.Green);
        }

        private static void StylePanel(GameObject panel)
        {
            if (panel == null) return;

            var rect = panel.GetComponent<RectTransform>();
            if (rect == null) return;

            var visualRoot = EnsureVisualRoot(panel.transform, "HtmlPanelVisualRoot", false);
            ConfigureStretch(visualRoot);
            visualRoot.SetSiblingIndex(0);

            var decor = EnsureDecor(visualRoot, "HtmlInnerGlow", rect.rect.size - new Vector2(42f, 42f), Vector2.zero, GlowTint, 28f);
            decor.transform.SetSiblingIndex(0);

            var wash = EnsureDecor(visualRoot, "HtmlPanelWash", rect.rect.size, Vector2.zero, PanelTint, 34f);
            wash.transform.SetSiblingIndex(0);
        }

        private static void StyleButtonVisual(Button button, string iconResource, string description, Color accent)
        {
            if (button == null) return;

            EnsureButtonTargetGraphicOnlyIfMissing(button);

            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            var visualRoot = EnsureVisualRoot(button.transform, "HtmlVisualRoot", true);
            ConfigureStretch(visualRoot);
            visualRoot.SetSiblingIndex(0);

            EnsureDecor(visualRoot, "HtmlGlow", rect.rect.size, Vector2.zero, Color.Lerp(ButtonGlow, accent, 0.22f), Mathf.Min(28f, rect.rect.height * 0.22f));
            EnsureDecor(visualRoot, "HtmlAccentLine", new Vector2(Mathf.Max(32f, rect.rect.width * 0.58f), 4f), new Vector2(0f, -rect.rect.height * 0.34f), WithAlpha(accent, 0.52f), 2f);

            var hasDescription = !string.IsNullOrEmpty(description);
            var iconSize = hasDescription ? 46f : Mathf.Clamp(rect.rect.height * 0.34f, 22f, 34f);
            var iconPosition = hasDescription ? new Vector2(0f, rect.rect.height * 0.18f) : new Vector2(-rect.rect.width * 0.34f, 0f);
            EnsureIcon(visualRoot, iconResource, new Vector2(iconSize, iconSize), iconPosition);
            EnsureDescription(visualRoot, description, new Vector2(rect.rect.width - 28f, 28f), new Vector2(0f, -rect.rect.height * 0.34f));
        }

        private static void StyleDataIcon(Transform root, string blockName, string iconResource, Color accent)
        {
            var block = FindChildRecursive(root, blockName);
            if (block == null) return;

            var rect = block.GetComponent<RectTransform>();
            if (rect == null) return;

            var visualRoot = EnsureVisualRoot(block, "HtmlVisualRoot", true);
            ConfigureStretch(visualRoot);
            visualRoot.SetSiblingIndex(0);
            EnsureDecor(visualRoot, "HtmlGlow", rect.rect.size, Vector2.zero, WithAlpha(accent, 0.12f), 18f);
            EnsureIcon(visualRoot, iconResource, new Vector2(22f, 22f), new Vector2(-rect.rect.width * 0.34f, rect.rect.height * 0.22f));
        }

        private static RectTransform EnsureVisualRoot(Transform parent, string name, bool localToParent)
        {
            var existing = parent != null ? parent.Find(name) : null;
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null && parent != null)
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
            if (localToParent)
            {
                rect.anchoredPosition3D = Vector3.zero;
            }

            DisableRaycastTargets(go);
            return rect;
        }

        private static ElderCareRoundedPanel EnsureDecor(RectTransform parent, string name, Vector2 size, Vector2 position, Color color, float radius)
        {
            var go = GetOrCreateChild(parent, name);
            var rect = ConfigureRect(go, size, position);
            var panel = go.GetComponent<ElderCareRoundedPanel>();
            if (panel == null)
            {
                panel = go.AddComponent<ElderCareRoundedPanel>();
            }

            panel.color = color;
            panel.cornerRadius = radius;
            panel.cornerSegments = 10;
            panel.raycastTarget = false;
            panel.SetAllDirty();
            DisableRaycastTargets(go);
            return panel;
        }

        private static TMP_Text EnsureDescription(RectTransform parent, string text, Vector2 size, Vector2 position)
        {
            var go = GetOrCreateChild(parent, "HtmlDescription");
            var rect = ConfigureRect(go, size, position);
            var label = go.GetComponent<TMP_Text>();
            if (label == null)
            {
                label = go.AddComponent<TextMeshProUGUI>();
            }

            label.text = text ?? string.Empty;
            label.fontSize = 17f;
            label.fontStyle = FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.color = !string.IsNullOrEmpty(text) ? DescriptionColor : Color.clear;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            rect.localRotation = Quaternion.identity;
            DisableRaycastTargets(go);
            return label;
        }

        private static Image EnsureIcon(RectTransform parent, string resourceName, Vector2 size, Vector2 position)
        {
            var go = GetOrCreateChild(parent, "HtmlIcon");
            ConfigureRect(go, size, position);

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = LoadHtmlIconSprite(resourceName);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, image.sprite != null ? 0.90f : 0f);
            image.raycastTarget = false;
            DisableRaycastTargets(go);
            return image;
        }

        private static void EnsureButtonTargetGraphicOnlyIfMissing(Button button)
        {
            if (button == null || button.targetGraphic != null) return;

            var graphic = button.GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = button.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
                image.raycastTarget = true;
                graphic = image;
            }

            button.targetGraphic = graphic;
            button.targetGraphic.raycastTarget = true;
        }

        private static void ConfigureStretch(RectTransform rect)
        {
            if (rect == null) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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

        private static void DisableRaycastTargets(GameObject go)
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

        private static Button FindButton(Transform root, string name)
        {
            var found = FindChildRecursive(root, name);
            return found != null ? found.GetComponent<Button>() : null;
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
