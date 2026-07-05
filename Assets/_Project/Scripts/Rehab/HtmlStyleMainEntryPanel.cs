using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class HtmlStyleMainEntryPanel : MonoBehaviour
    {
        private static readonly Vector2 CanvasSize = ElderCareUiTheme.MainEntryCanvasSize;

        private static readonly Color RoomBase = new Color32(0x37, 0x30, 0x29, 0xFF);
        private static readonly Color RoomWarmGlow = new Color(1f, 0.82f, 0.48f, 0.12f);
        private static readonly Color PanelBrown = new Color32(0x26, 0x1E, 0x16, 0xD8);
        private static readonly Color PanelStrokeWarm = new Color(1f, 0.82f, 0.55f, 0.34f);
        private static readonly Color CardSurface = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color CardHover = new Color(1f, 0.72f, 0.36f, 0.22f);
        private static readonly Color TextWarm = new Color32(0xFF, 0xE7, 0xC2, 0xFF);
        private static readonly Color TextGold = new Color32(0xFF, 0xD9, 0xA8, 0xFF);
        private static readonly Color ButtonGold = new Color32(0xFF, 0xCE, 0x8A, 0xFF);
        private static readonly Color ButtonDark = new Color32(0x2A, 0x25, 0x20, 0xFF);

        public UnifiedEntryMenu menu;
        public TMP_FontAsset uiFont;
        public bool rebuildOnEnable = true;

        private void Awake()
        {
            ResolveReferences();
            BuildOrRepair();
        }

        private void OnEnable()
        {
            if (!rebuildOnEnable) return;

            ResolveReferences();
            BuildOrRepair();
        }

        public static HtmlStyleMainEntryPanel Ensure(Transform canvasTransform, UnifiedEntryMenu entryMenu, TMP_FontAsset fontAsset)
        {
            if (canvasTransform == null) return null;

            var panel = canvasTransform.GetComponent<HtmlStyleMainEntryPanel>();
            if (panel == null)
            {
                panel = canvasTransform.gameObject.AddComponent<HtmlStyleMainEntryPanel>();
            }

            panel.menu = entryMenu != null ? entryMenu : panel.menu;
            panel.uiFont = fontAsset != null ? fontAsset : panel.uiFont;
            panel.BuildOrRepair();
            return panel;
        }

        public void BuildOrRepair()
        {
            ClearChildren(transform);

            CreateRoomBackdrop(transform);

            var panel = CreatePanel(transform, "Panel", new Vector2(628f, 304f), new Vector2(0f, 24f), PanelBrown, 40f, false);
            AddOutline(panel.gameObject, PanelStrokeWarm, new Vector2(2f, -2f));
            CreatePanel(panel.transform, "PanelInnerGlow", new Vector2(590f, 266f), Vector2.zero, new Color(1f, 0.86f, 0.62f, 0.04f), 34f, false);

            CreateTopBar(panel.transform);

            CreateModuleCard(panel.transform, "Module_HealthGame", ElderCareIconType.TableTennis, "\u5065\u5eb7\u6e38\u620f", "\u52a8\u8111\u53c8\u5f00\u5fc3", new Vector2(-226f, 8f), true, LoadPingPong, 0f);
            CreateModuleCard(panel.transform, "Module_Rehab", ElderCareIconType.Heart, "\u5eb7\u590d\u8fd0\u52a8", "\u4eca\u65e5 3 \u4e2a\u52a8\u4f5c", new Vector2(-75f, 8f), true, LoadRehab, 0.05f);
            CreateModuleCard(panel.transform, "Module_Travel", ElderCareIconType.MapPin, "VR \u65c5\u6e38", "\u8db3\u4e0d\u51fa\u6237\u770b\u4e16\u754c", new Vector2(75f, 8f), false, null, 0.1f);
            CreateModuleCard(panel.transform, "Module_Memory", ElderCareIconType.Video, "\u5f80\u65e5\u65f6\u5149", "\u56de\u5230\u4ece\u524d", new Vector2(226f, 8f), false, null, 0.15f);

            CreateSafeBar(panel.transform);
            CreateWindowControls(transform);
            CreateHint(transform);
        }

        private void CreateRoomBackdrop(Transform parent)
        {
            CreatePanel(parent, "SceneBase", CanvasSize, Vector2.zero, RoomBase, 0f, false);
            CreatePanel(parent, "WarmRoomGlow", new Vector2(530f, 360f), new Vector2(-150f, 122f), RoomWarmGlow, 220f, false);
            CreatePanel(parent, "Floor", new Vector2(CanvasSize.x, 138f), new Vector2(0f, -148f), new Color(0.48f, 0.36f, 0.24f, 0.28f), 0f, false);
            CreatePanel(parent, "Window", new Vector2(156f, 132f), new Vector2(222f, 122f), new Color(0.9f, 0.86f, 0.72f, 0.2f), 8f, false);
            AddOutline(parent.Find("Window").gameObject, new Color(0.2f, 0.16f, 0.12f, 0.58f), new Vector2(8f, -8f));
            CreatePanel(parent, "Sofa", new Vector2(220f, 92f), new Vector2(-235f, -166f), new Color(0.48f, 0.42f, 0.33f, 0.62f), 36f, false);
            CreatePanel(parent, "Plant", new Vector2(58f, 132f), new Vector2(244f, -142f), new Color(0.18f, 0.35f, 0.15f, 0.5f), 28f, false);
        }

        private void CreateTopBar(Transform parent)
        {
            CreateText(parent, "Greeting", "\u5f20\u5976\u5976\uff0c\u4e0a\u5348\u597d", new Vector2(330f, 42f), new Vector2(-126f, 116f), 30f, FontStyles.Bold, TextAlignmentOptions.Left, TextWarm);
            CreateText(parent, "WeatherTime", "\u6674 22\u00b0  \u4e0a\u5348 9:15", new Vector2(232f, 34f), new Vector2(178f, 116f), 21f, FontStyles.Bold, TextAlignmentOptions.Right, TextGold);
        }

        private Button CreateModuleCard(
            Transform parent,
            string name,
            ElderCareIconType iconType,
            string title,
            string description,
            Vector2 position,
            bool enabled,
            UnityEngine.Events.UnityAction onClick,
            float entranceDelay)
        {
            var go = CreateUiObject(name, parent);
            var rect = ConfigureRect(go, new Vector2(138f, 164f), position);

            var glow = CreatePanel(rect, "Glow", new Vector2(158f, 184f), Vector2.zero, new Color(1f, 0.74f, 0.4f, enabled ? 0.09f : 0.03f), 30f, false);
            glow.transform.SetAsFirstSibling();

            var surfaceColor = enabled ? CardSurface : new Color(1f, 1f, 1f, 0.045f);
            var surface = CreatePanel(rect, "Surface", new Vector2(138f, 164f), Vector2.zero, surfaceColor, 28f, true);
            AddOutline(surface.gameObject, new Color(1f, 1f, 1f, enabled ? 0.18f : 0.09f), new Vector2(1.5f, -1.5f));

            CreatePanel(rect, "FocusRing", new Vector2(30f, 30f), new Vector2(48f, 56f), new Color(1f, 1f, 1f, enabled ? 0.12f : 0.05f), 15f, false);
            CreatePanel(rect, "IconHalo", new Vector2(72f, 72f), new Vector2(0f, 40f), new Color(1f, 0.8f, 0.48f, enabled ? 0.13f : 0.05f), 36f, false);
            CreateLineIcon(rect, "Icon", iconType, new Vector2(66f, 66f), new Vector2(0f, 40f), enabled ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.5f), 6f);
            CreateText(rect, "Title", title, new Vector2(124f, 34f), new Vector2(0f, -22f), 23f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.62f));
            CreateText(rect, "Description", description, new Vector2(128f, 32f), new Vector2(0f, -54f), 14f, FontStyles.Normal, TextAlignmentOptions.Center, enabled ? TextGold : WithAlpha(TextGold, 0.52f));

            if (!enabled)
            {
                CreatePanel(rect, "EmptyBadgePanel", new Vector2(72f, 24f), new Vector2(0f, -76f), new Color(0f, 0f, 0f, 0.28f), 12f, false);
                CreateText(rect, "EmptyBadge", "\u5f85\u63a5\u5165", new Vector2(72f, 22f), new Vector2(0f, -76f), 13f, FontStyles.Bold, TextAlignmentOptions.Center, WithAlpha(TextWarm, 0.74f));
            }

            var button = go.AddComponent<Button>();
            button.targetGraphic = surface;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            if (enabled && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var motion = go.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = rect;
            motion.canvasGroup = go.AddComponent<CanvasGroup>();
            motion.cardGraphic = surface;
            motion.glowGraphic = glow;
            motion.interactable = enabled;
            motion.normalColor = surfaceColor;
            motion.hoverColor = CardHover;
            motion.pressedColor = new Color(0.4f, 0.27f, 0.14f, 0.82f);
            motion.glowColor = new Color(1f, 0.74f, 0.4f, 0.24f);
            motion.hoverScale = enabled ? 1.06f : 1f;
            motion.pressedScale = enabled ? ElderCareUiTheme.PressedScale : 1f;
            motion.hoverLiftY = 6f;
            motion.ambientFloatY = 1.2f;
            motion.entranceDelay = entranceDelay;
            return button;
        }

        private void CreateSafeBar(Transform parent)
        {
            var bar = CreateUiObject("SafeBar", parent);
            ConfigureRect(bar, new Vector2(480f, 48f), new Vector2(0f, -116f));
            CreateSafeButton(bar.transform, "Settings", ElderCareIconType.Gear, "\u8bbe\u7f6e", new Vector2(-164f, 0f));
            CreateSafeButton(bar.transform, "Health", ElderCareIconType.User, "\u6211\u7684\u5065\u5eb7", Vector2.zero);
            CreateSafeButton(bar.transform, "Rank", ElderCareIconType.Trophy, "\u6392\u884c\u699c", new Vector2(164f, 0f));
        }

        private void CreateSafeButton(Transform parent, string name, ElderCareIconType iconType, string label, Vector2 position)
        {
            var button = CreateButton(parent, name, label, new Vector2(142f, 42f), position, false);
            CreateLineIcon(button.transform, name + "Icon", iconType, new Vector2(22f, 22f), new Vector2(-48f, 0f), TextWarm, 3f);
            button.interactable = false;
        }

        private void CreateWindowControls(Transform parent)
        {
            CreateRoundTextButton(parent, "Minimize", "-", new Vector2(-40f, -182f), ButtonGold, ButtonDark);
            CreateRoundTextButton(parent, "Close", "x", new Vector2(40f, -182f), new Color32(0xD9, 0x4F, 0x3D, 0xFF), Color.white);
        }

        private void CreateHint(Transform parent)
        {
            CreatePanel(parent, "HintPanel", new Vector2(438f, 22f), new Vector2(0f, -209f), new Color(0f, 0f, 0f, 0.24f), 11f, false);
            CreateText(parent, "Hint", "\u5361\u7247\u60ac\u505c\u4f1a\u653e\u5927\uff0c\u5e95\u90e8\u529f\u80fd\u6682\u672a\u63a5\u5165", new Vector2(422f, 20f), new Vector2(0f, -209f), 11f, FontStyles.Normal, TextAlignmentOptions.Center, WithAlpha(ElderCareUiTheme.TextPrimary, 0.62f));
        }

        private void LoadPingPong()
        {
            ResolveReferences();
            if (menu != null) menu.LoadPingPong();
        }

        private void LoadRehab()
        {
            ResolveReferences();
            if (menu != null) menu.LoadRehab();
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 size, Vector2 position, bool enabled)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, size, position);
            var graphic = CreatePanel(go.transform, "Surface", size, Vector2.zero, new Color(1f, 1f, 1f, enabled ? 0.18f : 0.13f), size.y * 0.5f, true);
            AddOutline(graphic.gameObject, new Color(1f, 0.82f, 0.54f, enabled ? 0.44f : 0.18f), new Vector2(1.5f, -1.5f));
            CreateText(go.transform, "Label", label, new Vector2(size.x - 44f, size.y - 6f), new Vector2(17f, 0f), 16f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? TextWarm : WithAlpha(TextWarm, 0.76f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private void CreateRoundTextButton(Transform parent, string name, string label, Vector2 position, Color background, Color textColor)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, new Vector2(52f, 52f), position);
            var surface = CreatePanel(go.transform, "Surface", new Vector2(52f, 52f), Vector2.zero, new Color(0.16f, 0.13f, 0.1f, 0.76f), 26f, true);
            AddOutline(surface.gameObject, new Color(1f, 0.82f, 0.54f, 0.28f), new Vector2(1.5f, -1.5f));
            CreateText(go.transform, "Label", label, new Vector2(44f, 44f), Vector2.zero, 28f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);

            var button = go.AddComponent<Button>();
            button.targetGraphic = surface;
            button.interactable = false;
            button.transition = Selectable.Transition.None;

            var motion = go.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = go.transform as RectTransform;
            motion.canvasGroup = go.AddComponent<CanvasGroup>();
            motion.cardGraphic = surface;
            motion.interactable = false;
            motion.normalColor = surface.color;
            motion.hoverColor = background;
            motion.pressedColor = background;
            motion.hoverScale = 1f;
            motion.pressedScale = 1f;
            motion.ambientMotion = false;
        }

        private void ResolveReferences()
        {
            if (menu == null)
            {
                menu = FindObjectOfType<UnifiedEntryMenu>(true);
            }

            if (uiFont == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                for (var i = 0; i < fonts.Length; i++)
                {
                    if (fonts[i] != null && fonts[i].name == "RehabChineseTMP")
                    {
                        uiFont = fonts[i];
                        break;
                    }
                }
            }
        }

        private TMP_Text CreateText(Transform parent, string name, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, size, position);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private ElderCareLineIcon CreateLineIcon(Transform parent, string name, ElderCareIconType iconType, Vector2 size, Vector2 position, Color color, float strokeWidth)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, size, position);
            var icon = go.AddComponent<ElderCareLineIcon>();
            icon.iconType = iconType;
            icon.strokeWidth = strokeWidth;
            icon.color = color;
            icon.raycastTarget = false;
            return icon;
        }

        private static ElderCareRoundedPanel CreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Color color, float radius, bool raycastTarget)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, size, position);
            var panel = go.AddComponent<ElderCareRoundedPanel>();
            panel.cornerRadius = radius;
            panel.cornerSegments = 10;
            panel.color = color;
            panel.raycastTarget = raycastTarget;
            return panel;
        }

        private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 position)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
