using System.Collections.Generic;
using PicoElderCare.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class HtmlStyleMainEntryPanel : MonoBehaviour
    {
        private const string IconResourceRoot = "HtmlSvgIcons/";
#if UNITY_EDITOR
        private const string RehabChineseFontAssetPath = "Assets/_Project/Materials/Rehab/RehabChineseTMP.asset";
#endif

        private static readonly Vector2 CanvasSize = new Vector2(1120f, 680f);
        private static readonly Vector2 PanelSize = new Vector2(1040f, 468f);
        private static readonly Vector2 CardSize = new Vector2(220f, 238f);
        private static readonly Vector2 SafeButtonSize = new Vector2(190f, 56f);

        private const string IconTableTennis = "table_tennis";
        private const string IconLotus = "lotus";
        private const string IconLuggage = "luggage";
        private const string IconCamera = "camera";
        private const string IconGear = "gear";
        private const string IconHeart = "heart";
        private const string IconTrophy = "trophy";
        private const string RequiredChineseGlyphs = "\u5f20\u5976\u4e0a\u5348\u597d\u667422\u00b0915\u5065\u5eb7\u6e38\u620f\u52a8\u8111\u53c8\u5f00\u5fc3\u5eb7\u590d\u8fd0\u4eca\u65e53\u4e2a\u4f5cVR\u65c5\u6e38\u8db3\u4e0d\u51fa\u6237\u770b\u4e16\u754c\u5f80\u65e5\u65f6\u5149\u56de\u5230\u4ece\u524d\u5f85\u63a5\u5165\u8bbe\u7f6e\u6211\u7684\u6392\u884c\u699c\u5df2\u63a5\u5165\u5176\u4ed6\u529f\u80fd\u6682\u65f6\u4fdd\u7559\u7a7a\u72b6\u6001\u5361\u7247\u60ac\u505c\u4f1a\u653e\u5927\u5e95\u90e8\u4e3a\u548c";

        private static readonly Color RoomBase = new Color32(0x2A, 0x25, 0x20, 0x8C);
        private static readonly Color RoomWarmGlow = new Color(1f, 0.86f, 0.62f, 0.17f);
        private static readonly Color PanelBrown = new Color32(0x26, 0x1E, 0x16, 0xE2);
        private static readonly Color PanelStrokeWarm = new Color(1f, 0.82f, 0.55f, 0.36f);
        private static readonly Color CardSurface = new Color(1f, 1f, 1f, 0.17f);
        private static readonly Color CardHover = new Color(1f, 0.72f, 0.36f, 0.22f);
        private static readonly Color TextWarm = new Color32(0xFF, 0xE7, 0xC2, 0xFF);
        private static readonly Color TextGold = new Color32(0xFF, 0xD9, 0xA8, 0xFF);
        private static readonly Color ButtonGold = new Color32(0xFF, 0xCE, 0x8A, 0xFF);
        private static readonly Color ButtonDark = new Color32(0x2A, 0x25, 0x20, 0xFF);

        public UnifiedEntryMenu menu;
        public TMP_FontAsset uiFont;
        public bool rebuildOnEnable = true;
        public bool normalizeWorldCanvasScale = true;
        public float targetWorldWidthMeters = 1.55f;

        private static readonly Dictionary<string, Sprite> IconSpriteCache = new Dictionary<string, Sprite>();
        private TMP_FontAsset _panelFontInstance;
        private TMP_FontAsset _panelFontSource;

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

        private void OnDestroy()
        {
            DestroyPanelFontInstance();
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
            ResolveReferences();
            ConfigureForVrDisplay();
            ClearChildren(transform);

            CreateRoomBackdrop(transform);

            CreatePanel(transform, "PanelShadow", PanelSize + new Vector2(44f, 36f), new Vector2(0f, 12f), new Color(0f, 0f, 0f, 0.32f), 46f, false);

            var panel = CreatePanel(transform, "Panel", PanelSize, new Vector2(0f, 34f), PanelBrown, 40f, false);
            AddOutline(panel.gameObject, PanelStrokeWarm, new Vector2(2f, -2f));
            CreatePanel(panel.transform, "PanelInnerGlow", new Vector2(986f, 410f), Vector2.zero, new Color(1f, 0.86f, 0.62f, 0.055f), 34f, false);

            CreateTopBar(panel.transform);

            CreateModuleCard(panel.transform, "Module_HealthGame", IconTableTennis, ElderCareIconType.TableTennis, "\u5065\u5eb7\u6e38\u620f", "\u52a8\u8111\u53c8\u5f00\u5fc3", new Vector2(-363f, 14f), true, LoadHealthGames, 0f);
            CreateModuleCard(panel.transform, "Module_Rehab", IconLotus, ElderCareIconType.Heart, "\u5eb7\u590d\u8fd0\u52a8", "\u4eca\u65e5 3 \u4e2a\u52a8\u4f5c", new Vector2(-121f, 14f), true, LoadRehab, 0.05f);
            CreateModuleCard(panel.transform, "Module_Travel", IconLuggage, ElderCareIconType.MapPin, "VR \u65c5\u6e38", "\u8db3\u4e0d\u51fa\u6237\u770b\u4e16\u754c", new Vector2(121f, 14f), false, null, 0.1f);
            CreateModuleCard(panel.transform, "Module_Memory", IconCamera, ElderCareIconType.Video, "\u5f80\u65e5\u65f6\u5149", "\u56de\u5230\u4ece\u524d", new Vector2(363f, 14f), false, null, 0.15f);

            CreateSafeBar(panel.transform);
            CreateWindowControls(transform);
            CreateHint(transform);
        }

        private void CreateRoomBackdrop(Transform parent)
        {
            CreatePanel(parent, "SceneBase", CanvasSize, Vector2.zero, RoomBase, 0f, false);
            CreatePanel(parent, "WarmRoomGlow", new Vector2(840f, 500f), new Vector2(-252f, 156f), RoomWarmGlow, 270f, false);
            CreatePanel(parent, "Floor", new Vector2(CanvasSize.x, 185f), new Vector2(0f, -238f), new Color(0.48f, 0.36f, 0.24f, 0.42f), 0f, false);
            CreatePanel(parent, "Window", new Vector2(228f, 172f), new Vector2(360f, 164f), new Color(0.9f, 0.86f, 0.72f, 0.24f), 10f, false);
            AddOutline(parent.Find("Window").gameObject, new Color(0.2f, 0.16f, 0.12f, 0.62f), new Vector2(8f, -8f));
            CreatePanel(parent, "Sofa", new Vector2(340f, 150f), new Vector2(-360f, -244f), new Color(0.48f, 0.42f, 0.33f, 0.62f), 44f, false);
            CreatePanel(parent, "Plant", new Vector2(80f, 170f), new Vector2(382f, -222f), new Color(0.18f, 0.35f, 0.15f, 0.66f), 34f, false);
        }

        private void CreateTopBar(Transform parent)
        {
            CreateText(parent, "Greeting", "\u5f20\u5976\u5976\uff0c\u4e0a\u5348\u597d", new Vector2(480f, 50f), new Vector2(-245f, 168f), 34f, FontStyles.Bold, TextAlignmentOptions.Left, TextWarm);
            CreateText(parent, "WeatherTime", "\u6674 22\u00b0  \u4e0a\u5348 9:15", new Vector2(330f, 42f), new Vector2(305f, 168f), 24f, FontStyles.Bold, TextAlignmentOptions.Right, TextGold);
        }

        private Button CreateModuleCard(
            Transform parent,
            string name,
            string iconResource,
            ElderCareIconType iconType,
            string title,
            string description,
            Vector2 position,
            bool enabled,
            UnityEngine.Events.UnityAction onClick,
            float entranceDelay)
        {
            var go = CreateUiObject(name, parent);
            var rect = ConfigureRect(go, CardSize, position);

            var glow = CreatePanel(rect, "Glow", new Vector2(242f, 260f), Vector2.zero, new Color(1f, 0.74f, 0.4f, enabled ? 0.105f : 0.045f), 32f, false);
            glow.transform.SetAsFirstSibling();

            var surfaceColor = enabled ? CardSurface : new Color(1f, 1f, 1f, 0.12f);
            var surface = CreatePanel(rect, "Surface", CardSize, Vector2.zero, surfaceColor, 28f, true);
            AddOutline(surface.gameObject, new Color(1f, 1f, 1f, enabled ? 0.22f : 0.16f), new Vector2(1.5f, -1.5f));

            CreatePanel(rect, "FocusRing", new Vector2(34f, 34f), new Vector2(83f, 82f), new Color(1f, 1f, 1f, enabled ? 0.05f : 0.035f), 17f, false);
            CreatePanel(rect, "IconHalo", new Vector2(104f, 104f), new Vector2(0f, 55f), new Color(1f, 0.8f, 0.48f, enabled ? 0.16f : 0.10f), 52f, false);
            CreateSvgIcon(rect, "Icon", iconResource, iconType, new Vector2(78f, 78f), new Vector2(0f, 55f), enabled ? 1f : 0.72f);
            CreateText(rect, "Title", title, new Vector2(190f, 42f), new Vector2(0f, -30f), 30f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.82f));
            CreateText(rect, "Description", description, new Vector2(190f, 34f), new Vector2(0f, -68f), 18f, FontStyles.Normal, TextAlignmentOptions.Center, enabled ? TextGold : WithAlpha(TextGold, 0.72f));

            if (!enabled)
            {
                CreatePanel(rect, "EmptyBadgePanel", new Vector2(84f, 26f), new Vector2(0f, -100f), new Color(0f, 0f, 0f, 0.30f), 13f, false);
                CreateText(rect, "EmptyBadge", "\u5f85\u63a5\u5165", new Vector2(84f, 24f), new Vector2(0f, -100f), 14f, FontStyles.Bold, TextAlignmentOptions.Center, WithAlpha(TextWarm, 0.86f));
            }

            var button = go.AddComponent<Button>();
            button.targetGraphic = surface;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            if (enabled && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var group = go.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = go.AddComponent<CanvasGroup>();
            }

            var motion = go.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = rect;
            motion.canvasGroup = group;
            motion.playEntrance = false;
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
            motion.ambientMotion = false;
            motion.ambientFloatY = 0f;
            motion.entranceDelay = entranceDelay;
            group.alpha = 1f;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            return button;
        }

        private void CreateSafeBar(Transform parent)
        {
            var bar = CreateUiObject("SafeBar", parent);
            ConfigureRect(bar, new Vector2(626f, 62f), new Vector2(0f, -172f));
            CreateSafeButton(bar.transform, "Settings", IconGear, ElderCareIconType.Gear, "\u8bbe\u7f6e", new Vector2(-218f, 0f));
            CreateSafeButton(bar.transform, "Health", IconHeart, ElderCareIconType.User, "\u6211\u7684\u5065\u5eb7", Vector2.zero);
            CreateSafeButton(bar.transform, "Rank", IconTrophy, ElderCareIconType.Trophy, "\u6392\u884c\u699c", new Vector2(218f, 0f));
        }

        private void CreateSafeButton(Transform parent, string name, string iconResource, ElderCareIconType iconType, string label, Vector2 position)
        {
            var button = CreateButton(parent, name, label, SafeButtonSize, position, false);
            CreateSvgIcon(button.transform, name + "Icon", iconResource, iconType, new Vector2(28f, 28f), new Vector2(-64f, 0f), 0.86f);
            button.interactable = false;
        }

        private void CreateWindowControls(Transform parent)
        {
            CreateRoundTextButton(parent, "Minimize", "-", new Vector2(-44f, -254f), ButtonGold, ButtonDark);
            CreateRoundTextButton(parent, "Close", "x", new Vector2(44f, -254f), new Color32(0xD9, 0x4F, 0x3D, 0xFF), Color.white);
        }

        private void CreateHint(Transform parent)
        {
            CreatePanel(parent, "HintPanel", new Vector2(620f, 30f), new Vector2(0f, -306f), new Color(0f, 0f, 0f, 0.38f), 15f, false);
            CreateText(parent, "Hint", "\u5361\u7247\u60ac\u505c\u4f1a\u653e\u5927\uff1b\u65c5\u6e38\u3001\u5f80\u65e5\u65f6\u5149\u548c\u5e95\u90e8\u529f\u80fd\u4e3a\u5f85\u63a5\u5165", new Vector2(596f, 24f), new Vector2(0f, -306f), 15f, FontStyles.Normal, TextAlignmentOptions.Center, WithAlpha(ElderCareUiTheme.TextPrimary, 0.72f));
        }

        private void LoadHealthGames()
        {
            ResolveReferences();
            if (menu != null) menu.LoadHealthGames();
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
            CreateText(go.transform, "Label", label, new Vector2(size.x - 58f, size.y - 6f), new Vector2(26f, 0f), 22f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? TextWarm : WithAlpha(TextWarm, 0.76f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private void CreateRoundTextButton(Transform parent, string name, string label, Vector2 position, Color background, Color textColor)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, new Vector2(62f, 62f), position);
            var surface = CreatePanel(go.transform, "Surface", new Vector2(62f, 62f), Vector2.zero, new Color(0.16f, 0.13f, 0.1f, 0.76f), 31f, true);
            AddOutline(surface.gameObject, new Color(1f, 0.82f, 0.54f, 0.28f), new Vector2(1.5f, -1.5f));
            CreateText(go.transform, "Label", label, new Vector2(52f, 52f), Vector2.zero, 32f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);

            var button = go.AddComponent<Button>();
            button.targetGraphic = surface;
            button.interactable = false;
            button.transition = Selectable.Transition.None;

            var group = go.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = go.AddComponent<CanvasGroup>();
            }

            var motion = go.AddComponent<TechModuleCardMotion>();
            motion.cardTransform = go.transform as RectTransform;
            motion.canvasGroup = group;
            motion.playEntrance = false;
            motion.cardGraphic = surface;
            motion.interactable = false;
            motion.normalColor = surface.color;
            motion.hoverColor = background;
            motion.pressedColor = background;
            motion.hoverScale = 1f;
            motion.pressedScale = 1f;
            motion.ambientMotion = false;
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            var rect = go.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = position;
                rect.localScale = Vector3.one;
            }
        }

        private void ResolveReferences()
        {
            if (menu == null)
            {
                menu = FindObjectOfType<UnifiedEntryMenu>(true);
            }

            var resolvedFont = RuntimeTmpFontAssetUtility.ResolveSourceFont(uiFont, _panelFontInstance, _panelFontSource);

            if (resolvedFont == null)
            {
#if UNITY_EDITOR
                resolvedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RehabChineseFontAssetPath);
#endif
            }

            if (resolvedFont == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                for (var i = 0; i < fonts.Length; i++)
                {
                    if (fonts[i] != null && fonts[i].name == "RehabChineseTMP")
                    {
                        resolvedFont = fonts[i];
                        break;
                    }
                }
            }

            uiFont = RuntimeTmpFontAssetUtility.PrepareDynamicFont(resolvedFont, RequiredChineseGlyphs, ref _panelFontInstance, ref _panelFontSource);
        }

        private void DestroyPanelFontInstance()
        {
            RuntimeTmpFontAssetUtility.DestroyRuntimeFont(ref _panelFontInstance, ref _panelFontSource);
        }

        private void ConfigureForVrDisplay()
        {
            if (GetComponent<MrKeepVisible>() == null)
            {
                gameObject.AddComponent<MrKeepVisible>();
            }

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = CanvasSize;
            }

            if (!normalizeWorldCanvasScale || targetWorldWidthMeters <= 0f || CanvasSize.x <= 0f) return;

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return;

            var scale = targetWorldWidthMeters / CanvasSize.x;
            transform.localScale = Vector3.one * scale;
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

        private Image CreateSvgIcon(Transform parent, string name, string resourceName, ElderCareIconType fallbackIconType, Vector2 size, Vector2 position, float alpha)
        {
            var sprite = LoadHtmlIconSprite(resourceName);
            if (sprite == null)
            {
                CreateLineIcon(parent, name, fallbackIconType, size, position, WithAlpha(ElderCareUiTheme.TextPrimary, alpha), Mathf.Max(4f, size.x * 0.08f));
                return null;
            }

            var go = CreateUiObject(name, parent);
            ConfigureRect(go, size, position);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            return image;
        }

        private static Sprite LoadHtmlIconSprite(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            if (IconSpriteCache.TryGetValue(resourceName, out var cached)) return cached;

            var importedSprite = Resources.Load<Sprite>(IconResourceRoot + resourceName);
#if UNITY_EDITOR
            if (importedSprite == null)
            {
                importedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/HtmlSvgIcons/" + resourceName + ".png");
            }
#endif
            if (importedSprite != null)
            {
                IconSpriteCache[resourceName] = importedSprite;
                return importedSprite;
            }

            var texture = Resources.Load<Texture2D>(IconResourceRoot + resourceName);
#if UNITY_EDITOR
            if (texture == null)
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/HtmlSvgIcons/" + resourceName + ".png");
            }
#endif
            if (texture == null)
            {
                IconSpriteCache[resourceName] = null;
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            IconSpriteCache[resourceName] = sprite;
            return sprite;
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
