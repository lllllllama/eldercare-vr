using System.Collections.Generic;
using PicoElderCare.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
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

        public static readonly Vector2 CanvasSize = ElderCareMenuDesignTokens.MainEntryCanvasSize;
        public static readonly Vector2 PanelSize = ElderCareMenuDesignTokens.MainEntryPanelSize;
        public static readonly Vector2 CardSize = ElderCareMenuDesignTokens.MainEntryCardSize;
        public static readonly Vector2 SafeButtonSize = ElderCareMenuDesignTokens.MainEntrySafeButtonSize;

        private const string IconTableTennis = "table_tennis";
        private const string IconLotus = "lotus";
        private const string IconLuggage = "luggage";
        private const string IconCamera = "camera";
        private const string RequiredChineseGlyphs = "\u5f20\u5976\u4e0a\u5348\u597d\u667422\u00b0915\u5065\u5eb7\u6e38\u620f\u52a8\u8111\u53c8\u5f00\u5fc3\u5eb7\u590d\u8fd0\u4eca\u65e53\u4e2a\u4f5cVR\u65c5\u6e38\u8db3\u4e0d\u51fa\u6237\u770b\u4e16\u754c\u5f80\u65e5\u65f6\u5149\u56de\u5230\u4ece\u524d\u5f85\u63a5\u5165\u8bbe\u7f6e\u6211\u7684\u6392\u884c\u699c\u5df2\u63a5\u5165\u5176\u4ed6\u529f\u80fd\u6682\u65f6\u4fdd\u7559\u7a7a\u72b6\u6001\u5361\u7247\u60ac\u505c\u4f1a\u653e\u5927\u5e95\u90e8\u4e3a\u548c\u8f7b\u677e\u953b\u70bc\u8212\u7f13\u8bad\u7ec3\u517b\u751f\u91cd\u6e29\u719f\u6089\u8bb0\u5fc6\u5f00\u59cb\u4f53\u9a8c";

        private static readonly Color RoomBase = new Color(0.20f, 0.15f, 0.10f, 0.08f);
        private static readonly Color RoomWarmGlow = new Color(1f, 0.86f, 0.62f, 0.09f);

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
            if (!rebuildOnEnable) return;
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

            CreatePanel(transform, "PanelShadow", PanelSize + new Vector2(44f, 36f), new Vector2(0f, 12f), ElderCareMenuDesignTokens.WarmShadow, 46f, false);

            var panel = CreatePanel(transform, "Panel", PanelSize, new Vector2(0f, 34f), ElderCareMenuDesignTokens.Wood, 42f, false);
            ConfigureNativeStroke(panel, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.WoodDark, 0.52f), 2f);
            CreatePanel(panel.transform, "WoodWarmLayer", PanelSize - new Vector2(16f, 16f), new Vector2(0f, 1f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.26f), 36f, false);
            var ricePaperPanel = CreatePanel(panel.transform, "RicePaperPanel", PanelSize - new Vector2(36f, 36f), new Vector2(0f, 3f), ElderCareMenuDesignTokens.RiceLight, 32f, false);
            ConfigureNativeStroke(ricePaperPanel, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.68f), 1.4f);
            CreatePanel(panel.transform, "RiceWarmEdge", PanelSize - new Vector2(58f, 58f), new Vector2(0f, 3f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.24f), 27f, false);

            CreateTopBar(panel.transform);

            UnityEngine.Events.UnityAction loadHealthAction = LoadHealthGames;
            UnityEngine.Events.UnityAction loadRehabAction = LoadRehab;
            if (menu != null)
            {
                loadHealthAction = menu.LoadHealthGames;
                loadRehabAction = menu.LoadRehab;
            }
            CreateModuleCard(panel.transform, "Module_HealthGame", IconTableTennis, ElderCareIconType.TableTennis, "健康运动", "活动身体 · 轻松锻炼", "开始体验", new Vector2(-363f, 14f), ElderCareMenuDesignTokens.Jade, true, loadHealthAction, 0f);
            CreateModuleCard(panel.transform, "Module_Rehab", IconLotus, ElderCareIconType.Heart, "康复运动", "舒缓训练 · 健康养生", "开始训练", new Vector2(-121f, 14f), ElderCareMenuDesignTokens.Amber, true, loadRehabAction, 0.05f);
            CreateModuleCard(panel.transform, "Module_Travel", IconLuggage, ElderCareIconType.MapPin, "VR 旅游", "足不出户看世界", "待接入", new Vector2(121f, 14f), ElderCareMenuDesignTokens.GoldDeep, false, null, 0.1f);
            CreateModuleCard(panel.transform, "Module_Memory", IconCamera, ElderCareIconType.Video, "往日时光", "重温熟悉的记忆", "待接入", new Vector2(363f, 14f), ElderCareMenuDesignTokens.Coral, false, null, 0.15f);

            CreateSafeBar(panel.transform);
            CreateWindowControls(transform);
            CreateHint(transform);
        }

        private void CreateRoomBackdrop(Transform parent)
        {
            CreatePanel(parent, "SceneBase", CanvasSize, Vector2.zero, RoomBase, 0f, false);
            CreatePanel(parent, "WarmRoomGlow", new Vector2(840f, 500f), new Vector2(-252f, 156f), RoomWarmGlow, 270f, false);
            CreatePanel(parent, "Floor", new Vector2(CanvasSize.x, 185f), new Vector2(0f, -238f), new Color(0.48f, 0.36f, 0.24f, 0.08f), 0f, false);
            CreatePanel(parent, "Window", new Vector2(228f, 172f), new Vector2(360f, 164f), new Color(0.9f, 0.86f, 0.72f, 0.08f), 10f, false);
            AddLegacyDecorShadow(parent.Find("Window").gameObject, new Color(0.2f, 0.16f, 0.12f, 0.10f), new Vector2(8f, -8f));
            CreatePanel(parent, "Sofa", new Vector2(340f, 150f), new Vector2(-360f, -244f), new Color(0.48f, 0.42f, 0.33f, 0.07f), 44f, false);
            CreatePanel(parent, "Plant", new Vector2(80f, 170f), new Vector2(382f, -222f), new Color(0.18f, 0.35f, 0.15f, 0.09f), 34f, false);
        }

        private void CreateTopBar(Transform parent)
        {
            CreateText(parent, "Greeting", "张奶奶，上午好", new Vector2(480f, 50f), new Vector2(-245f, 168f), 34f, FontStyles.Bold, TextAlignmentOptions.Left, ElderCareMenuDesignTokens.TextPrimary);
            CreateText(parent, "WeatherTime", "晴 22°  上午 9:15", new Vector2(330f, 42f), new Vector2(305f, 168f), 24f, FontStyles.Bold, TextAlignmentOptions.Right, ElderCareMenuDesignTokens.GoldDeep);
            CreatePanel(parent, "TopDivider", new Vector2(920f, 3f), new Vector2(0f, 138f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.46f), 1.5f, false);
        }

        private Button CreateModuleCard(
            Transform parent,
            string name,
            string iconResource,
            ElderCareIconType iconType,
            string title,
            string description,
            string status,
            Vector2 position,
            Color accent,
            bool enabled,
            UnityEngine.Events.UnityAction onClick,
            float entranceDelay)
        {
            var go = CreateUiObject(name, parent);
            var rect = ConfigureRect(go, CardSize, position);

            var glow = CreatePanel(rect, "Glow", CardSize + new Vector2(20f, 18f), new Vector2(0f, -1f), ElderCareMenuDesignTokens.WithAlpha(accent, enabled ? 0.04f : 0.02f), 32f, false);
            glow.transform.SetAsFirstSibling();

            var surfaceColor = enabled ? ElderCareMenuDesignTokens.Card : Color.Lerp(ElderCareMenuDesignTokens.Card, ElderCareMenuDesignTokens.RiceMid, 0.36f);
            var surface = CreatePanel(rect, "Surface", CardSize, Vector2.zero, surfaceColor, 28f, true);
            ConfigureNativeStroke(surface, ElderCareMenuDesignTokens.WithAlpha(enabled ? accent : ElderCareMenuDesignTokens.GoldStroke, enabled ? 0.52f : 0.28f), 1.5f);
            CreatePanel(rect, "InnerRice", CardSize - new Vector2(16f, 16f), Vector2.zero, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceLight, 0.38f), 22f, false);

            CreatePanel(rect, "IconHalo", new Vector2(96f, 96f), new Vector2(0f, 66f), ElderCareMenuDesignTokens.WithAlpha(accent, enabled ? 0.18f : 0.10f), 48f, false);
            var iconContainer = CreatePanel(rect, "IconContainer", new Vector2(82f, 82f), new Vector2(0f, 66f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceLight, 0.96f), 41f, false);
            ConfigureNativeStroke(iconContainer, ElderCareMenuDesignTokens.WithAlpha(accent, 0.42f), 1f);
            CreateSvgIcon(rect, "HeroIcon", iconResource, iconType, new Vector2(72f, 72f), new Vector2(0f, 66f), enabled ? 1f : 0.72f);
            CreateText(rect, "Title", title, new Vector2(194f, 38f), new Vector2(0f, 4f), 30f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? ElderCareMenuDesignTokens.TextPrimary : ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.TextPrimary, 0.72f));
            CreateText(rect, "Description", description, new Vector2(194f, 30f), new Vector2(0f, -34f), 18f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? ElderCareMenuDesignTokens.TextSecondary : ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.TextSecondary, 0.68f));

            var ctaFill = enabled
                ? Color.Lerp(ElderCareMenuDesignTokens.RiceLight, accent, 0.42f)
                : ElderCareMenuDesignTokens.RiceMid;
            var statusPanel = CreatePanel(rect, "StatusPanel", new Vector2(178f, 42f), new Vector2(0f, -91f), ctaFill, 20f, false);
            ConfigureNativeStroke(statusPanel, ElderCareMenuDesignTokens.WithAlpha(enabled ? accent : ElderCareMenuDesignTokens.GoldStroke, 0.52f), 1f);
            CreateText(rect, "Status", status, new Vector2(170f, 36f), new Vector2(0f, -91f), 18f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextPrimary);

            var button = go.AddComponent<Button>();
            button.targetGraphic = surface;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            if (enabled && onClick != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEventTools.AddPersistentListener(button.onClick, onClick);
                }
                else
#endif
                {
                button.onClick.AddListener(onClick);
                }
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
            motion.hoverColor = Color.Lerp(surfaceColor, accent, 0.08f);
            motion.pressedColor = Color.Lerp(surfaceColor, accent, 0.14f);
            motion.glowColor = ElderCareMenuDesignTokens.WithAlpha(accent, 0.16f);
            motion.hoverScale = enabled ? ElderCareMenuDesignTokens.HoverScale : 1f;
            motion.pressedScale = enabled ? ElderCareMenuDesignTokens.PressedScale : 1f;
            motion.hoverLiftY = ElderCareMenuDesignTokens.HoverLiftY;
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
            var background = CreatePanel(bar.transform, "Background", new Vector2(626f, 62f), Vector2.zero, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.92f), 28f, false);
            ConfigureNativeStroke(background, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.46f), 1f);
            CreateSafeButton(bar.transform, "Settings", ElderCareIconType.Gear, "设置", new Vector2(-218f, 0f));
            CreateSafeButton(bar.transform, "Health", ElderCareIconType.User, "我的健康", Vector2.zero);
            CreateSafeButton(bar.transform, "Rank", ElderCareIconType.Trophy, "排行榜", new Vector2(218f, 0f));
        }

        private void CreateSafeButton(Transform parent, string name, ElderCareIconType iconType, string label, Vector2 position)
        {
            var button = CreateButton(parent, name, label, SafeButtonSize, position, false);
            CreateLineIcon(button.transform, name + "Icon", iconType, new Vector2(28f, 28f), new Vector2(-64f, 0f), ElderCareMenuDesignTokens.TextPrimary, 3f);
            button.interactable = false;
        }

        private void CreateWindowControls(Transform parent)
        {
            CreateRoundIconButton(parent, "Minimize", ElderCareIconType.Minus, new Vector2(-44f, -254f), ElderCareMenuDesignTokens.GoldDeep);
            CreateRoundIconButton(parent, "Close", ElderCareIconType.Close, new Vector2(44f, -254f), ElderCareMenuDesignTokens.Coral);
        }

        private void CreateHint(Transform parent)
        {
            CreatePanel(parent, "HintPanel", new Vector2(620f, 30f), new Vector2(0f, -306f), ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.74f), 15f, false);
            CreateText(parent, "Hint", "卡片悬停会放大；旅游、往日时光和底部功能为待接入", new Vector2(596f, 24f), new Vector2(0f, -306f), 15f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextSecondary);
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
            var graphic = CreatePanel(go.transform, "Surface", size, Vector2.zero, enabled ? ElderCareMenuDesignTokens.CardHighlight : ElderCareMenuDesignTokens.Card, size.y * 0.5f, true);
            ConfigureNativeStroke(graphic, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, enabled ? 0.64f : 0.34f), 1.5f);
            CreateText(go.transform, "Label", label, new Vector2(size.x - 58f, size.y - 6f), new Vector2(26f, 0f), 22f, FontStyles.Bold, TextAlignmentOptions.Center, enabled ? ElderCareMenuDesignTokens.TextPrimary : ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.TextPrimary, 0.72f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private void CreateRoundIconButton(Transform parent, string name, ElderCareIconType iconType, Vector2 position, Color accent)
        {
            var go = CreateUiObject(name, parent);
            ConfigureRect(go, new Vector2(62f, 62f), position);
            var surface = CreatePanel(go.transform, "Surface", new Vector2(62f, 62f), Vector2.zero, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.RiceMid, 0.86f), 31f, true);
            ConfigureNativeStroke(surface, ElderCareMenuDesignTokens.WithAlpha(accent, 0.34f), 1.5f);
            CreateLineIcon(go.transform, "Icon", iconType, new Vector2(26f, 26f), Vector2.zero, ElderCareMenuDesignTokens.TextPrimary, 3f);

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
            motion.hoverColor = surface.color;
            motion.pressedColor = surface.color;
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyPanelFontInstance();
                uiFont = resolvedFont;
                return;
            }
#endif

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
                Debug.LogError("Missing UI Icon: HtmlSvgIcons/" + resourceName);
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

        private static void ConfigureNativeStroke(ElderCareRoundedPanel panel, Color color, float width)
        {
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(panel, color, width);
        }

        // This is an environmental drop shadow, not a rounded surface border. It is
        // intentionally the only Outline retained by the current MainEntry skin.
        private static void AddLegacyDecorShadow(GameObject go, Color color, Vector2 distance)
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
