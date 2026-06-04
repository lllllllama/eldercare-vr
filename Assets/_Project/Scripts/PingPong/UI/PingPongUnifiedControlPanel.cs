using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PingPongUnifiedControlPanel : MonoBehaviour
{
    private const string RuntimePanelName = "PingPongUnifiedControlPanel";
    private static readonly Vector2 PanelSize = new Vector2(820f, 760f);
    private static readonly Vector2 PanelPosition = new Vector2(-500f, 120f);
    private static readonly Vector2 ButtonSize = new Vector2(230f, 82f);

    private const string TitleLabel = "\u4e52\u4e53\u7403\u8bad\u7ec3";
    private const string StatusPrefix = "\u72b6\u6001\uff1a";
    private const string StatusTraining = "\u8bad\u7ec3\u4e2d";
    private const string StatusPaused = "\u5df2\u6682\u505c";
    private const string StatusWaiting = "\u7b49\u5f85\u5f00\u59cb";
    private const string AccuracyLabel = "\u547d\u4e2d\u7387";
    private const string HitLabel = "\u547d\u4e2d";
    private const string ServedLabel = "\u53d1\u7403";
    private const string MissedLabel = "\u6f0f\u7403";
    private const string HitSpeedLabel = "\u56de\u7403\u901f\u5ea6";
    private const string SpinSpeedLabel = "\u65cb\u8f6c\u901f\u5ea6";
    private const string DifficultyPrefix = "\u96be\u5ea6\uff1a";
    private const string ServeSpeedPrefix = "\u53d1\u7403\u901f\u5ea6\uff1a";
    private const string PauseServingLabel = "\u6682\u505c\u53d1\u7403";
    private const string ResumeServingLabel = "\u7ee7\u7eed\u53d1\u7403";
    private const string ResetScoreLabel = "\u91cd\u7f6e\u6570\u636e";
    private const string ReturnHomeLabel = "\u8fd4\u56de\u4e3b\u9875";
    private const string DifficultyEasyLabel = "\u8f7b\u677e";
    private const string DifficultyNormalLabel = "\u6807\u51c6";
    private const string DifficultyAdvancedLabel = "\u8fdb\u9636";
    private const string DifficultyChallengeLabel = "\u6311\u6218";
    private const string DifficultyCustomLabel = "\u81ea\u5b9a\u4e49";

    public ScoreManager scoreManager;
    public BallSpawner ballSpawner;
    public PingPongDifficultyController difficultyController;
    public ElderCareHomeMenu homeMenu;
    public TMP_FontAsset uiFont;

    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text accuracyText;
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text missedText;
    public TMP_Text speedText;
    public TMP_Text spinText;
    public TMP_Text difficultyText;
    public TMP_Text serveSpeedText;
    public Button servingToggleButton;
    public Button resetButton;
    public Button homeButton;
    public Graphic backgroundGraphic;

    private bool _buttonsWired;
    private bool _hasStartedServing;
    private float _nextRefreshTime;

    public static PingPongUnifiedControlPanel EnsureRuntimePanel(
        Transform canvasTransform,
        ScoreManager scoreManager,
        BallSpawner ballSpawner,
        PingPongDifficultyController difficultyController,
        ElderCareHomeMenu homeMenu,
        TMP_FontAsset fontAsset)
    {
        if (canvasTransform == null) return null;

        var root = FindChild(canvasTransform, RuntimePanelName);
        if (root == null)
        {
            root = new GameObject(RuntimePanelName, typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
        }

        var panel = root.GetComponent<PingPongUnifiedControlPanel>();
        if (panel == null)
        {
            panel = root.AddComponent<PingPongUnifiedControlPanel>();
        }

        panel.scoreManager = scoreManager;
        panel.ballSpawner = ballSpawner;
        panel.difficultyController = difficultyController;
        panel.homeMenu = homeMenu;
        panel.uiFont = fontAsset != null ? fontAsset : panel.uiFont;
        panel.ResolveReferences();
        panel.BuildOrRepairLayout();
        panel.RefreshDisplay();
        return panel;
    }

    private void Awake()
    {
        ResolveReferences();
        BuildOrRepairLayout();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BuildOrRepairLayout();
        _hasStartedServing = ballSpawner != null && ballSpawner.IsServing;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime) return;

        _nextRefreshTime = Time.unscaledTime + 0.15f;
        RefreshDisplay();
    }

    public void Bind(ScoreManager score, BallSpawner spawner, PingPongDifficultyController difficulty, ElderCareHomeMenu menu)
    {
        scoreManager = score;
        ballSpawner = spawner;
        difficultyController = difficulty;
        homeMenu = menu;
        RefreshDisplay();
    }

    public void ToggleServing()
    {
        ResolveReferences();
        if (ballSpawner == null) return;

        if (ballSpawner.IsServing)
        {
            ballSpawner.StopServing();
        }
        else
        {
            ballSpawner.StartServing();
            _hasStartedServing = true;
        }

        RefreshDisplay();
    }

    public void ResetScore()
    {
        ResolveReferences();
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        RefreshDisplay();
    }

    public void ReturnHome()
    {
        ResolveReferences();
        if (homeMenu != null)
        {
            homeMenu.ShowHome();
        }
    }

    public void RefreshDisplay()
    {
        ResolveReferences();

        var serving = ballSpawner != null && ballSpawner.IsServing;
        if (serving)
        {
            _hasStartedServing = true;
        }

        SetText(titleText, TitleLabel);
        SetText(statusText, StatusPrefix + ResolveStatusText(serving));
        SetText(accuracyText, $"{AccuracyLabel}\n<size=52><b>{ReadAccuracy():0.0}%</b></size>");
        SetText(hitText, $"{HitLabel}\n<size=42><b>{ReadHitCount()}</b></size>");
        SetText(servedText, $"{ServedLabel}\n<size=42><b>{ReadServedCount()}</b></size>");
        SetText(missedText, $"{MissedLabel}\n<size=42><b>{ReadMissedCount()}</b></size>");
        SetText(speedText, $"{HitSpeedLabel}\n<size=42><b>{ReadLastHitSpeed():0.0}</b></size> m/s");
        SetText(spinText, $"{SpinSpeedLabel}\n<size=42><b>{ReadLastSpinSpeed():0}</b></size> rad/s");
        SetText(difficultyText, DifficultyPrefix + ResolveDifficultyLabel());
        SetText(serveSpeedText, $"{ServeSpeedPrefix}{ResolveServeSpeed():0.0} m/s");
        SetButtonLabel(servingToggleButton, serving ? PauseServingLabel : ResumeServingLabel);
    }

    private string ResolveStatusText(bool serving)
    {
        if (serving) return StatusTraining;
        return _hasStartedServing || ReadServedCount() > 0 || ReadHitCount() > 0 || ReadMissedCount() > 0
            ? StatusPaused
            : StatusWaiting;
    }

    private int ReadServedCount() => scoreManager != null ? scoreManager.ServedCount : 0;
    private int ReadHitCount() => scoreManager != null ? scoreManager.HitCount : 0;
    private int ReadMissedCount() => scoreManager != null ? scoreManager.MissedCount : 0;
    private float ReadLastHitSpeed() => scoreManager != null ? scoreManager.LastHitSpeed : 0f;
    private float ReadLastSpinSpeed() => scoreManager != null ? scoreManager.LastSpinSpeed : 0f;
    private float ReadAccuracy() => scoreManager != null ? scoreManager.Accuracy : 0f;

    private string ResolveDifficultyLabel()
    {
        return difficultyController != null
            ? GetDifficultyLabel(difficultyController.CurrentDifficulty)
            : GetDifficultyLabel(PingPongDifficulty.Normal);
    }

    private static string GetDifficultyLabel(PingPongDifficulty difficulty)
    {
        switch (difficulty)
        {
            case PingPongDifficulty.Easy:
                return DifficultyEasyLabel;
            case PingPongDifficulty.Advanced:
                return DifficultyAdvancedLabel;
            case PingPongDifficulty.Challenge:
                return DifficultyChallengeLabel;
            case PingPongDifficulty.Custom:
                return DifficultyCustomLabel;
            default:
                return DifficultyNormalLabel;
        }
    }

    private float ResolveServeSpeed()
    {
        if (difficultyController != null) return difficultyController.CurrentSpeed;
        return ballSpawner != null ? ballSpawner.serveSpeed : PingPongDifficultyController.GetSpeed(PingPongDifficulty.Normal);
    }

    private void ResolveReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = FindSceneObject<ScoreManager>();
        }

        if (ballSpawner == null)
        {
            ballSpawner = FindSceneObject<BallSpawner>();
        }

        if (difficultyController == null)
        {
            difficultyController = FindSceneObject<PingPongDifficultyController>();
        }

        if (homeMenu == null)
        {
            homeMenu = FindSceneObject<ElderCareHomeMenu>();
        }

        if (uiFont == null)
        {
            uiFont = ResolveRuntimeFont();
        }
    }

    private void BuildOrRepairLayout()
    {
        UnwireButtons();

        var rootRect = ConfigureRect(gameObject, PanelSize, PanelPosition);
        var background = ConfigurePanel(
            GetOrCreateChild(transform, "Background"),
            PanelSize,
            Vector2.zero,
            WithAlpha(ElderCareUiTheme.PanelStrong, 0.99f),
            30f,
            true);
        backgroundGraphic = background;
        background.transform.SetAsFirstSibling();

        var outline = EnsureComponent<Outline>(background.gameObject);
        outline.effectColor = WithAlpha(ElderCareUiTheme.PanelStroke, 0.68f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        titleText = ConfigureText(GetOrCreateChild(transform, "Title"), TitleLabel, new Vector2(700f, 58f), new Vector2(0f, 326f), ElderCareUiTheme.Title, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        statusText = ConfigureText(GetOrCreateChild(transform, "StatusText"), StatusPrefix + StatusWaiting, new Vector2(700f, 46f), new Vector2(0f, 268f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Gold);

        accuracyText = ConfigureMetric("AccuracyMetric", AccuracyLabel, new Vector2(-225f, 178f), ElderCareUiTheme.Cyan, true);
        speedText = ConfigureMetric("SpeedMetric", HitSpeedLabel, new Vector2(225f, 178f), ElderCareUiTheme.Blue, true);
        hitText = ConfigureMetric("HitMetric", HitLabel, new Vector2(-260f, 58f), ElderCareUiTheme.Green, false);
        servedText = ConfigureMetric("ServedMetric", ServedLabel, new Vector2(0f, 58f), ElderCareUiTheme.Cyan, false);
        missedText = ConfigureMetric("MissedMetric", MissedLabel, new Vector2(260f, 58f), ElderCareUiTheme.Orange, false);
        spinText = ConfigureMetric("SpinMetric", SpinSpeedLabel, new Vector2(0f, -74f), ElderCareUiTheme.Violet, true);

        difficultyText = ConfigureText(GetOrCreateChild(transform, "DifficultyText"), DifficultyPrefix + GetDifficultyLabel(PingPongDifficulty.Normal), new Vector2(360f, 48f), new Vector2(-190f, -188f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Cyan);
        serveSpeedText = ConfigureText(GetOrCreateChild(transform, "ServeSpeedText"), ServeSpeedPrefix + "3.1 m/s", new Vector2(360f, 48f), new Vector2(200f, -188f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.TextPrimary);

        servingToggleButton = ConfigureButton(GetOrCreateChild(transform, "ServingToggleButton"), ResumeServingLabel, new Vector2(-260f, -306f), ButtonSize, ElderCareUiTheme.Cyan);
        resetButton = ConfigureButton(GetOrCreateChild(transform, "ResetButton"), ResetScoreLabel, new Vector2(0f, -306f), ButtonSize, ElderCareUiTheme.Green);
        homeButton = ConfigureButton(GetOrCreateChild(transform, "HomeButton"), ReturnHomeLabel, new Vector2(260f, -306f), ButtonSize, ElderCareUiTheme.Orange);

        ConfigureDrag(rootRect, background);
        servingToggleButton.transform.SetAsLastSibling();
        resetButton.transform.SetAsLastSibling();
        homeButton.transform.SetAsLastSibling();
        WireButtons();
    }

    private TMP_Text ConfigureMetric(string name, string label, Vector2 position, Color accent, bool wide)
    {
        var size = wide ? new Vector2(350f, 112f) : new Vector2(220f, 98f);
        var root = GetOrCreateChild(transform, name);
        ConfigureRect(root, size, position);

        var panel = ConfigurePanel(
            GetOrCreateChild(root.transform, "Panel"),
            size,
            Vector2.zero,
            WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.32f), 0.82f),
            wide ? 22f : 18f,
            false);
        var outline = EnsureComponent<Outline>(panel.gameObject);
        outline.effectColor = WithAlpha(accent, 0.42f);
        outline.effectDistance = new Vector2(2f, -2f);

        return ConfigureText(GetOrCreateChild(root.transform, "Text"), label, size, Vector2.zero, wide ? ElderCareUiTheme.Body : ElderCareUiTheme.BodySmall, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
    }

    private void ConfigureDrag(RectTransform rootRect, Graphic background)
    {
        var canvas = rootRect != null ? rootRect.GetComponentInParent<Canvas>(true) : null;
        var canvasTransform = canvas != null ? canvas.transform : rootRect != null ? rootRect.parent : null;
        var placer = canvasTransform != null ? canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>() : null;

        // Background 只负责显示，不参与拖动，避免按钮误触。
        if (background != null)
        {
            background.raycastTarget = false;

            var oldBackgroundDrag = background.GetComponent<WorldSpaceUiRayDragHandle>();
            if (oldBackgroundDrag != null)
            {
                DestroyComponent(oldBackgroundDrag);
            }
        }

        // 上半部分作为唯一拖动区域。
        var handle = GetOrCreateChild(rootRect, "DragHandle");
        var handleRect = ConfigureRect(
            handle,
            new Vector2(PanelSize.x, PanelSize.y * 0.5f),
            new Vector2(0f, PanelSize.y * 0.25f)
        );

        handleRect.SetAsLastSibling();

        var image = handle.GetComponent<Image>();
        if (image == null)
        {
            image = handle.AddComponent<Image>();
        }

        // 保持可射线命中，但视觉上接近隐藏。
        image.raycastTarget = true;
        image.color = new Color(0.35f, 0.75f, 0.9f, 0.05f);

        var drag = handle.GetComponent<WorldSpaceUiRayDragHandle>();
        if (drag == null)
        {
            drag = handle.AddComponent<WorldSpaceUiRayDragHandle>();
        }

        drag.placer = placer;
        drag.targetRoot = rootRect;
        drag.headTransform = placer != null ? placer.headTransform : Camera.main != null ? Camera.main.transform : null;
        drag.handleGraphic = image;
        drag.normalColor = image.color;
        drag.activeColor = new Color(0.68f, 1f, 1f, 0.18f);
        drag.minDistanceMeters = 0.9f;
        drag.maxDistanceMeters = 3.8f;
        drag.lockWorldHeight = true;
        drag.lockHeightToComfortOffset = true;
    }

    private TMP_Text ConfigureText(GameObject go, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, Color color)
    {
        ConfigureRect(go, size, position);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = go.AddComponent<TextMeshProUGUI>();
        }

        if (uiFont != null)
        {
            text.font = uiFont;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private Button ConfigureButton(GameObject go, string label, Vector2 position, Vector2 size, Color accent)
    {
        var rect = ConfigureRect(go, size, position);
        var graphic = ConfigurePanel(
            go,
            size,
            position,
            WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.52f), 0.98f),
            22f,
            true);
        var outline = EnsureComponent<Outline>(go);
        outline.effectColor = WithAlpha(accent, 0.52f);
        outline.effectDistance = new Vector2(2f, -2f);

        var button = go.GetComponent<Button>();
        if (button == null)
        {
            button = go.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;
        ConfigureText(GetOrCreateChild(rect, "Label"), label, new Vector2(size.x - 16f, size.y - 12f), Vector2.zero, ElderCareUiTheme.Button, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        return button;
    }

    private void WireButtons()
    {
        if (_buttonsWired) return;

        if (servingToggleButton != null) servingToggleButton.onClick.AddListener(ToggleServing);
        if (resetButton != null) resetButton.onClick.AddListener(ResetScore);
        if (homeButton != null) homeButton.onClick.AddListener(ReturnHome);
        _buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!_buttonsWired) return;

        if (servingToggleButton != null) servingToggleButton.onClick.RemoveListener(ToggleServing);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetScore);
        if (homeButton != null) homeButton.onClick.RemoveListener(ReturnHome);
        _buttonsWired = false;
    }

    private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = go.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Graphic ConfigurePanel(GameObject go, Vector2 size, Vector2 anchoredPosition, Color color, float radius, bool raycastTarget)
    {
        ConfigureRect(go, size, anchoredPosition);

        var image = go.GetComponent<Image>();
        if (image != null)
        {
            DestroyComponent(image);
        }

        var roundedPanel = go.GetComponent<ElderCareRoundedPanel>();
        if (roundedPanel == null)
        {
            roundedPanel = go.AddComponent<ElderCareRoundedPanel>();
        }

        roundedPanel.color = color;
        roundedPanel.cornerRadius = radius;
        roundedPanel.raycastTarget = raycastTarget;
        roundedPanel.SetAllDirty();
        return roundedPanel;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        var child = FindChild(parent, name);
        if (child != null) return child;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null) return null;

        var child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void DestroyComponent(Component component)
    {
        if (component == null) return;

        if (Application.isPlaying)
        {
            Destroy(component);
            return;
        }

        DestroyImmediate(component);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = value;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static TMP_FontAsset ResolveRuntimeFont()
    {
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font != null && font.name == "RehabChineseTMP")
            {
                return font;
            }
        }

        return null;
    }

    private static T FindSceneObject<T>() where T : Component
    {
        var objects = Resources.FindObjectsOfTypeAll<T>();
        foreach (var candidate in objects)
        {
            if (candidate != null && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }
}
