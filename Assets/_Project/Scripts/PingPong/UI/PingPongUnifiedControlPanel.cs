using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PingPongUnifiedControlPanel : MonoBehaviour
{
    private const string RuntimePanelName = "PingPongUnifiedControlPanel";
    private static readonly Vector2 PanelSize = new Vector2(600f, 760f);
    private static readonly Vector2 PanelPosition = new Vector2(-430f, 100f);
    private static readonly Vector2 ContentSize = new Vector2(548f, 0f);
    private static readonly Vector2 SecondaryButtonSize = new Vector2(266f, 56f);
    private const float AccuracyBarWidth = 300f;

    private const string TitleLabel = "\u4e52\u4e53\u7403\u8bad\u7ec3";
    private const string StatusWaiting = "\u7b49\u5f85\u5f00\u59cb";
    private const string StatusTraining = "\u8bad\u7ec3\u4e2d";
    private const string StatusPaused = "\u5df2\u6682\u505c";
    private const string AssessmentLabel = "\u8bad\u7ec3\u8bc4\u4f30";
    private const string DifficultyLabel = "\u96be\u5ea6\u8c03\u8282";
    private const string ControlLabel = "\u8bad\u7ec3\u63a7\u5236";
    private const string AccuracyLabel = "\u547d\u4e2d\u7387";
    private const string HitLabel = "\u547d\u4e2d";
    private const string ServedLabel = "\u53d1\u7403";
    private const string MissedLabel = "\u6f0f\u63a5";
    private const string HitSpeedLabel = "\u5e73\u5747\u56de\u7403\u901f\u5ea6";
    private const string SpinSpeedLabel = "\u65cb\u8f6c\u901f\u5ea6";
    private const string ServeSpeedPrefix = "\u53d1\u7403\u901f\u5ea6";
    private const string PauseServingLabel = "\u6682\u505c\u8bad\u7ec3";
    private const string ResumeServingLabel = "\u5f00\u59cb\u8bad\u7ec3";
    private const string ContinueServingLabel = "\u7ee7\u7eed\u8bad\u7ec3";
    private const string ResetScoreLabel = "\u91cd\u7f6e\u6570\u636e";
    private const string ReturnHomeLabel = "\u9000\u51fa\u8fd4\u56de";
    private const string CoachEmptyLabel = "\u966a\u7ec3\u5f85\u63a5\u5165";
    private const string DifficultyDownLabel = "\u2212";
    private const string DifficultyUpLabel = "\uff0b";
    private const string DifficultyNormalLabel = "\u9002\u4e2d";
    private const string DifficultyAdvancedLabel = "\u8fdb\u9636";
    private const string DifficultyChallengeLabel = "\u6311\u6218";
    private const string DifficultyCustomLabel = "\u81ea\u5b9a";
    private const string TableDragTurnOnLabel = "\u5f00\u542f\u62d6\u684c";
    private const string TableDragTurnOffLabel = "\u5173\u95ed\u62d6\u684c";
    private const string TableDragUnavailableLabel = "\u62d6\u684c\u4e0d\u53ef\u7528";

    public ScoreManager scoreManager;
    public BallSpawner ballSpawner;
    public PingPongDifficultyController difficultyController;
    public RemoteTableDragController tableDragController;
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
    public Button difficultyDownButton;
    public Button difficultyUpButton;
    public Button tableDragToggleButton;
    public Button servingToggleButton;
    public Button resetButton;
    public Button homeButton;
    public Graphic backgroundGraphic;

    public Button coachButton;
    public Button miniPauseButton;
    public TMP_Text miniHitText;
    public TMP_Text miniAccuracyText;

    private Button _minimizeButton;
    private Button _closeButton;
    private CanvasGroup _fullPanelGroup;
    private CanvasGroup _miniPanelGroup;
    private Graphic _accuracyFill;
    private Graphic[] _difficultyGears;
    private bool _buttonsWired;
    private bool _hasStartedServing;
    private bool _layoutBuilt;
    private float _nextRefreshTime;

    public static PingPongUnifiedControlPanel EnsureRuntimePanel(
        Transform canvasTransform,
        ScoreManager scoreManager,
        BallSpawner ballSpawner,
        PingPongDifficultyController difficultyController,
        ElderCareHomeMenu homeMenu,
        TMP_FontAsset fontAsset)
    {
        return EnsureRuntimePanel(canvasTransform, scoreManager, ballSpawner, difficultyController, null, homeMenu, fontAsset);
    }

    public static PingPongUnifiedControlPanel EnsureRuntimePanel(
        Transform canvasTransform,
        ScoreManager scoreManager,
        BallSpawner ballSpawner,
        PingPongDifficultyController difficultyController,
        RemoteTableDragController tableDragController,
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
        panel.tableDragController = tableDragController;
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
        Bind(score, spawner, difficulty, null, menu);
    }

    public void Bind(ScoreManager score, BallSpawner spawner, PingPongDifficultyController difficulty, RemoteTableDragController tableDrag, ElderCareHomeMenu menu)
    {
        scoreManager = score;
        ballSpawner = spawner;
        difficultyController = difficulty;
        tableDragController = tableDrag;
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
            ballSpawner.ClearBallsWithoutScoring();
        }
        else
        {
            ballSpawner.StartServing();
            _hasStartedServing = true;
        }

        RefreshDisplay();
    }

    public void StartServingAndCompact()
    {
        ResolveReferences();
        if (ballSpawner != null && !ballSpawner.IsServing)
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

    public void ToggleTableDrag()
    {
        ResolveReferences();
        if (tableDragController == null) return;

        tableDragController.ToggleRemoteDragEnabled();
        RefreshDisplay();
    }

    public void IncreaseDifficulty()
    {
        ResolveReferences();
        if (difficultyController != null)
        {
            difficultyController.IncreaseDifficulty();
        }

        RefreshDisplay();
    }

    public void DecreaseDifficulty()
    {
        ResolveReferences();
        if (difficultyController != null)
        {
            difficultyController.DecreaseDifficulty();
        }

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        ResolveReferences();

        var serving = ballSpawner != null && ballSpawner.IsServing;
        if (serving)
        {
            _hasStartedServing = true;
        }

        var status = ResolveStatusText(serving);
        SetText(titleText, TitleLabel);
        SetText(statusText, status);
        SetText(hitText, $"<size=42><b>{ReadHitCount()}</b></size>\n{HitLabel}");
        SetText(servedText, $"<size=42><b>{ReadServedCount()}</b></size>\n{ServedLabel}");
        SetText(missedText, $"<size=42><b>{ReadMissedCount()}</b></size>\n{MissedLabel}");
        SetText(accuracyText, $"{AccuracyLabel}\n<size=38><b>{ReadAccuracy():0}%</b></size>");
        SetText(speedText, $"{HitSpeedLabel}  <b>{ReadLastHitSpeed():0.0} m/s</b>");
        SetText(difficultyText, ResolveDifficultyLabel());
        SetText(serveSpeedText, $"{ServeSpeedPrefix}\n<b>{ResolveServeSpeed():0.0} m/s</b>");
        SetText(spinText, $"{SpinSpeedLabel}\n<b>{ResolveServeSpin():0} rad/s</b>");
        SetText(miniHitText, $"<size=38><b>{ReadHitCount()}</b></size>\n{HitLabel}");
        SetText(miniAccuracyText, $"<size=38><b>{ReadAccuracy():0}%</b></size>\n{AccuracyLabel}");
        SetButtonLabel(servingToggleButton, serving ? PauseServingLabel : (_hasStartedServing ? ContinueServingLabel : ResumeServingLabel));
        SetButtonLabel(miniPauseButton, PauseServingLabel);
        SetButtonLabel(coachButton, CoachEmptyLabel);

        RefreshAccuracyBar();
        RefreshDifficultyGears();
        RefreshTableDragButton();
        SetPanelVisibility(serving);
    }

    private string ResolveStatusText(bool serving)
    {
        if (serving) return "\u25cf " + StatusTraining;
        return _hasStartedServing || ReadServedCount() > 0 || ReadHitCount() > 0 || ReadMissedCount() > 0
            ? "\u23f8 " + StatusPaused
            : "\u23f3 " + StatusWaiting;
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
                return DifficultyNormalLabel;
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

    private int ResolveDifficultyIndex()
    {
        var difficulty = difficultyController != null ? difficultyController.CurrentDifficulty : PingPongDifficulty.Normal;
        switch (difficulty)
        {
            case PingPongDifficulty.Advanced:
                return 1;
            case PingPongDifficulty.Challenge:
            case PingPongDifficulty.Custom:
                return 2;
            default:
                return 0;
        }
    }

    private float ResolveServeSpeed()
    {
        if (difficultyController != null) return difficultyController.CurrentSpeed;
        return ballSpawner != null ? ballSpawner.serveSpeed : PingPongDifficultyController.GetSpeed(PingPongDifficulty.Normal);
    }

    private float ResolveServeSpin()
    {
        var lastSpin = ReadLastSpinSpeed();
        if (lastSpin > 0.01f) return lastSpin;
        return ballSpawner != null ? ballSpawner.sidespinRadiansPerSecond : 0f;
    }

    private void RefreshAccuracyBar()
    {
        if (_accuracyFill == null) return;

        var ratio = Mathf.Clamp01(ReadAccuracy() / 100f);
        var width = Mathf.Lerp(8f, AccuracyBarWidth, ratio);
        var rect = _accuracyFill.rectTransform;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(-AccuracyBarWidth * 0.5f + width * 0.5f, 0f);
    }

    private void RefreshDifficultyGears()
    {
        if (_difficultyGears == null) return;

        var index = ResolveDifficultyIndex();
        for (var i = 0; i < _difficultyGears.Length; i++)
        {
            if (_difficultyGears[i] == null) continue;
            _difficultyGears[i].color = i <= index ? ElderCareUiTheme.Gold : new Color(1f, 1f, 1f, 0.18f);
        }
    }

    private void RefreshTableDragButton()
    {
        if (tableDragToggleButton == null) return;

        if (tableDragController == null)
        {
            tableDragToggleButton.interactable = false;
            SetButtonLabel(tableDragToggleButton, TableDragUnavailableLabel);
            return;
        }

        tableDragToggleButton.interactable = true;
        SetButtonLabel(tableDragToggleButton, tableDragController.IsRemoteDragEnabled ? TableDragTurnOffLabel : TableDragTurnOnLabel);
    }

    private void SetPanelVisibility(bool compact)
    {
        if (_fullPanelGroup != null)
        {
            _fullPanelGroup.alpha = compact ? 0f : 1f;
            _fullPanelGroup.interactable = !compact;
            _fullPanelGroup.blocksRaycasts = !compact;
        }

        if (_miniPanelGroup != null)
        {
            _miniPanelGroup.alpha = compact ? 1f : 0f;
            _miniPanelGroup.interactable = compact;
            _miniPanelGroup.blocksRaycasts = compact;
        }
    }

    private void ResolveReferences()
    {
        if (scoreManager == null) scoreManager = FindSceneObject<ScoreManager>();
        if (ballSpawner == null) ballSpawner = FindSceneObject<BallSpawner>();
        if (difficultyController == null) difficultyController = FindSceneObject<PingPongDifficultyController>();
        if (tableDragController == null) tableDragController = FindSceneObject<RemoteTableDragController>();
        if (homeMenu == null) homeMenu = FindSceneObject<ElderCareHomeMenu>();
        if (uiFont == null) uiFont = ResolveRuntimeFont();
    }

    private void BuildOrRepairLayout()
    {
        if (_layoutBuilt && FindChild(transform, "FullPanel") != null)
        {
            EnsureMiniPanelOnTop();
            WireButtons();
            return;
        }

        UnwireButtons();
        ClearChildren(transform);
        _difficultyGears = new Graphic[3];

        var rootRect = ConfigureRect(gameObject, PanelSize, PanelPosition);
        BuildFullPanel(rootRect);
        BuildMiniPanel(rootRect);
        BuildHiddenCompatibilityControls(rootRect);
        ConfigureDrag(rootRect, backgroundGraphic);
        EnsureMiniPanelOnTop();
        WireButtons();
        _layoutBuilt = true;
    }

    private void BuildFullPanel(RectTransform rootRect)
    {
        var full = GetOrCreateChild(transform, "FullPanel");
        var fullRect = ConfigureRect(full, PanelSize, Vector2.zero);
        _fullPanelGroup = EnsureComponent<CanvasGroup>(full);

        backgroundGraphic = ConfigurePanel(full, PanelSize, Vector2.zero, new Color32(0x12, 0x1A, 0x26, 0xF2), 30f, true);
        AddOutline(full, new Color(0.38f, 0.92f, 1f, 0.58f), new Vector2(3f, -3f));
        ConfigurePanel(GetOrCreateChild(fullRect, "AmbientGlow"), new Vector2(548f, 638f), Vector2.zero, new Color(0.38f, 0.92f, 1f, 0.045f), 36f, false).transform.SetAsFirstSibling();

        titleText = ConfigureText(GetOrCreateChild(fullRect, "Title"), TitleLabel, new Vector2(330f, 48f), new Vector2(-84f, 318f), 34f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Left);
        ConfigureText(GetOrCreateChild(fullRect, "TitleIcon"), "\u4e52", new Vector2(54f, 52f), new Vector2(-246f, 318f), 36f, FontStyles.Bold, ElderCareUiTheme.Gold, TextAlignmentOptions.Center);
        statusText = ConfigureBadge(fullRect, "StatusBadge", StatusWaiting, new Vector2(166f, 42f), new Vector2(190f, 318f), ElderCareUiTheme.Gold);

        CreateSectionLabel(fullRect, "AssessmentHeader", AssessmentLabel, new Vector2(-206f, 258f), ElderCareUiTheme.Cyan);
        var assessment = ConfigurePanel(GetOrCreateChild(fullRect, "AssessmentPanel"), new Vector2(ContentSize.x, 182f), new Vector2(0f, 151f), new Color(1f, 1f, 1f, 0.055f), 20f, false);
        AddOutline(assessment.gameObject, new Color(1f, 1f, 1f, 0.12f), new Vector2(1.5f, -1.5f));
        hitText = CreateMetric(assessment.rectTransform, "HitMetric", HitLabel, new Vector2(-180f, 48f), ElderCareUiTheme.Green);
        servedText = CreateMetric(assessment.rectTransform, "ServedMetric", ServedLabel, new Vector2(0f, 48f), ElderCareUiTheme.Cyan);
        missedText = CreateMetric(assessment.rectTransform, "MissedMetric", MissedLabel, new Vector2(180f, 48f), ElderCareUiTheme.Orange);

        accuracyText = ConfigureText(GetOrCreateChild(assessment.rectTransform, "AccuracyText"), AccuracyLabel, new Vector2(138f, 66f), new Vector2(-188f, -40f), 20f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
        var bar = ConfigurePanel(GetOrCreateChild(assessment.rectTransform, "AccuracyBar"), new Vector2(AccuracyBarWidth, 14f), new Vector2(96f, -38f), new Color(1f, 1f, 1f, 0.14f), 7f, false);
        _accuracyFill = ConfigurePanel(GetOrCreateChild(bar.rectTransform, "Fill"), new Vector2(8f, 14f), new Vector2(-AccuracyBarWidth * 0.5f, 0f), ElderCareUiTheme.Cyan, 7f, false);
        speedText = ConfigureText(GetOrCreateChild(assessment.rectTransform, "SpeedRow"), HitSpeedLabel, new Vector2(492f, 28f), new Vector2(0f, -75f), 18f, FontStyles.Bold, ElderCareUiTheme.TextSecondary, TextAlignmentOptions.Left);

        CreateSectionLabel(fullRect, "DifficultyHeader", DifficultyLabel, new Vector2(-206f, 36f), ElderCareUiTheme.Gold);
        BuildDifficultyPanel(fullRect);

        CreateSectionLabel(fullRect, "ControlHeader", ControlLabel, new Vector2(-206f, -192f), ElderCareUiTheme.Cyan);
        servingToggleButton = ConfigureButton(GetOrCreateChild(fullRect, "ServingToggleButton"), ResumeServingLabel, new Vector2(0f, -252f), new Vector2(ContentSize.x, 64f), ElderCareUiTheme.Green, true);
        coachButton = ConfigureButton(GetOrCreateChild(fullRect, "CoachButton"), CoachEmptyLabel, new Vector2(-141f, -326f), SecondaryButtonSize, ElderCareUiTheme.Violet, false);
        homeButton = ConfigureButton(GetOrCreateChild(fullRect, "HomeButton"), ReturnHomeLabel, new Vector2(141f, -326f), SecondaryButtonSize, ElderCareUiTheme.Orange, true);

        resetButton = ConfigureButton(GetOrCreateChild(fullRect, "ResetButton"), "\u21bb", new Vector2(-68f, -366f), new Vector2(48f, 48f), ElderCareUiTheme.Green, true);
        _minimizeButton = ConfigureButton(GetOrCreateChild(fullRect, "MinimizeButton"), DifficultyDownLabel, new Vector2(0f, -366f), new Vector2(48f, 48f), ElderCareUiTheme.Cyan, true);
        _closeButton = ConfigureButton(GetOrCreateChild(fullRect, "CloseButton"), "\u00d7", new Vector2(68f, -366f), new Vector2(48f, 48f), ElderCareUiTheme.Orange, true);
    }

    private void BuildDifficultyPanel(RectTransform parent)
    {
        var panel = ConfigurePanel(GetOrCreateChild(parent, "DifficultyPanel"), new Vector2(ContentSize.x, 166f), new Vector2(0f, -74f), new Color(1f, 1f, 1f, 0.055f), 20f, false);
        AddOutline(panel.gameObject, new Color(0.38f, 0.66f, 0.94f, 0.25f), new Vector2(1.5f, -1.5f));
        difficultyDownButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "DifficultyDownButton"), DifficultyDownLabel, new Vector2(-210f, 34f), new Vector2(60f, 60f), ElderCareUiTheme.SoftButton, true);
        difficultyUpButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "DifficultyUpButton"), DifficultyUpLabel, new Vector2(210f, 34f), new Vector2(60f, 60f), ElderCareUiTheme.SoftButton, true);
        difficultyText = ConfigureText(GetOrCreateChild(panel.rectTransform, "DifficultyText"), DifficultyNormalLabel, new Vector2(190f, 40f), new Vector2(0f, 46f), 30f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);

        for (var i = 0; i < _difficultyGears.Length; i++)
        {
            _difficultyGears[i] = ConfigurePanel(GetOrCreateChild(panel.rectTransform, "Gear" + i), new Vector2(30f, 10f), new Vector2((i - 1) * 40f, 10f), new Color(1f, 1f, 1f, 0.18f), 5f, false);
        }

        serveSpeedText = CreateInfoTile(panel.rectTransform, "ServeSpeed", ServeSpeedPrefix, new Vector2(-130f, -52f), ElderCareUiTheme.Gold);
        spinText = CreateInfoTile(panel.rectTransform, "SpinSpeed", SpinSpeedLabel, new Vector2(130f, -52f), ElderCareUiTheme.Violet);
    }

    private void BuildMiniPanel(RectTransform rootRect)
    {
        var mini = GetOrCreateChild(transform, "MiniPanel");
        var miniRect = ConfigureRect(mini, new Vector2(252f, 156f), new Vector2(174f, 282f));
        _miniPanelGroup = EnsureComponent<CanvasGroup>(mini);
        var bg = ConfigurePanel(mini, new Vector2(252f, 156f), Vector2.zero, new Color32(0x12, 0x1A, 0x26, 0xE8), 24f, true);
        AddOutline(bg.gameObject, new Color(0.38f, 0.92f, 1f, 0.54f), new Vector2(2f, -2f));
        miniHitText = ConfigureText(GetOrCreateChild(miniRect, "MiniHit"), HitLabel, new Vector2(98f, 62f), new Vector2(-56f, 38f), 18f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
        miniAccuracyText = ConfigureText(GetOrCreateChild(miniRect, "MiniAccuracy"), AccuracyLabel, new Vector2(98f, 62f), new Vector2(56f, 38f), 18f, FontStyles.Bold, ElderCareUiTheme.Gold, TextAlignmentOptions.Center);
        ConfigurePanel(GetOrCreateChild(miniRect, "Divider"), new Vector2(2f, 62f), new Vector2(0f, 38f), new Color(1f, 1f, 1f, 0.18f), 1f, false);
        miniPauseButton = ConfigureButton(GetOrCreateChild(miniRect, "MiniPauseButton"), PauseServingLabel, new Vector2(0f, -46f), new Vector2(206f, 50f), ElderCareUiTheme.Gold, true);
    }

    private void BuildHiddenCompatibilityControls(RectTransform rootRect)
    {
        tableDragToggleButton = ConfigureButton(GetOrCreateChild(rootRect, "LegacyTableDragToggleButton"), TableDragUnavailableLabel, new Vector2(9999f, 9999f), new Vector2(4f, 4f), ElderCareUiTheme.Violet, true);
        var canvasGroup = tableDragToggleButton.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void CreateSectionLabel(RectTransform parent, string name, string label, Vector2 position, Color accent)
    {
        ConfigureText(GetOrCreateChild(parent, name), label, new Vector2(190f, 30f), position, 18f, FontStyles.Bold, accent, TextAlignmentOptions.Left);
        ConfigurePanel(GetOrCreateChild(parent, name + "Divider"), new Vector2(318f, 2f), new Vector2(114f, position.y), new Color(1f, 1f, 1f, 0.12f), 1f, false);
    }

    private TMP_Text CreateMetric(RectTransform parent, string name, string label, Vector2 position, Color accent)
    {
        var root = GetOrCreateChild(parent, name);
        var rect = ConfigureRect(root, new Vector2(160f, 72f), position);
        var bg = ConfigurePanel(GetOrCreateChild(rect, "Panel"), new Vector2(160f, 72f), Vector2.zero, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.26f), 0.72f), 14f, false);
        AddOutline(bg.gameObject, WithAlpha(accent, 0.34f), new Vector2(1.5f, -1.5f));
        return ConfigureText(GetOrCreateChild(rect, "Text"), label, new Vector2(140f, 62f), Vector2.zero, 18f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
    }

    private TMP_Text CreateInfoTile(RectTransform parent, string name, string label, Vector2 position, Color accent)
    {
        var panel = ConfigurePanel(GetOrCreateChild(parent, name + "Panel"), new Vector2(238f, 58f), position, new Color(1f, 1f, 1f, 0.06f), 14f, false);
        AddOutline(panel.gameObject, WithAlpha(accent, 0.24f), new Vector2(1f, -1f));
        return ConfigureText(GetOrCreateChild(panel.rectTransform, name + "Text"), label, new Vector2(214f, 50f), Vector2.zero, 18f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
    }

    private TMP_Text ConfigureBadge(RectTransform parent, string name, string label, Vector2 size, Vector2 position, Color accent)
    {
        var panel = ConfigurePanel(GetOrCreateChild(parent, name + "Panel"), size, position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.28f), 0.92f), size.y * 0.5f, false);
        return ConfigureText(GetOrCreateChild(panel.rectTransform, name), label, size - new Vector2(14f, 6f), Vector2.zero, 20f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
    }

    private void ConfigureDrag(RectTransform rootRect, Graphic background)
    {
        var canvas = rootRect != null ? rootRect.GetComponentInParent<Canvas>(true) : null;
        var canvasTransform = canvas != null ? canvas.transform : rootRect != null ? rootRect.parent : null;
        var placer = canvasTransform != null ? canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>() : null;

        if (background != null)
        {
            background.raycastTarget = false;
        }

        var handle = GetOrCreateChild(rootRect, "DragHandle");
        var handleRect = ConfigureRect(handle, new Vector2(PanelSize.x, 148f), new Vector2(0f, PanelSize.y * 0.5f - 74f));
        handleRect.SetAsLastSibling();

        var image = handle.GetComponent<Image>();
        if (image == null) image = handle.AddComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(0.35f, 0.75f, 0.9f, 0.035f);

        var drag = handle.GetComponent<WorldSpaceUiRayDragHandle>();
        if (drag == null) drag = handle.AddComponent<WorldSpaceUiRayDragHandle>();
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

    private void EnsureMiniPanelOnTop()
    {
        var miniPanel = FindChild(transform, "MiniPanel");
        if (miniPanel == null) return;

        miniPanel.transform.SetAsLastSibling();
    }

    private TMP_Text ConfigureText(GameObject go, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        ConfigureRect(go, size, position);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (text == null) text = go.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.82f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.06f);
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private Button ConfigureButton(GameObject go, string label, Vector2 position, Vector2 size, Color accent, bool interactable)
    {
        var rect = ConfigureRect(go, size, position);
        var graphic = ConfigurePanel(go, size, position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, interactable ? 0.58f : 0.26f), interactable ? 0.98f : 0.58f), Mathf.Min(22f, size.y * 0.48f), true);
        AddOutline(go, WithAlpha(accent, interactable ? 0.5f : 0.16f), new Vector2(1.5f, -1.5f));

        var button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;
        button.interactable = interactable;
        ConfigureText(GetOrCreateChild(rect, "Label"), label, new Vector2(size.x - 10f, size.y - 8f), Vector2.zero, Mathf.Clamp(size.y * 0.42f, 18f, 30f), FontStyles.Bold, interactable ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.62f), TextAlignmentOptions.Center);
        return button;
    }

    private void WireButtons()
    {
        if (_buttonsWired) return;

        if (difficultyDownButton != null) difficultyDownButton.onClick.AddListener(DecreaseDifficulty);
        if (difficultyUpButton != null) difficultyUpButton.onClick.AddListener(IncreaseDifficulty);
        if (tableDragToggleButton != null) tableDragToggleButton.onClick.AddListener(ToggleTableDrag);
        if (servingToggleButton != null) servingToggleButton.onClick.AddListener(ToggleServing);
        if (miniPauseButton != null) miniPauseButton.onClick.AddListener(ToggleServing);
        if (resetButton != null) resetButton.onClick.AddListener(ResetScore);
        if (homeButton != null) homeButton.onClick.AddListener(ReturnHome);
        if (_minimizeButton != null) _minimizeButton.onClick.AddListener(StartServingAndCompact);
        if (_closeButton != null) _closeButton.onClick.AddListener(ReturnHome);
        _buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!_buttonsWired) return;

        if (difficultyDownButton != null) difficultyDownButton.onClick.RemoveListener(DecreaseDifficulty);
        if (difficultyUpButton != null) difficultyUpButton.onClick.RemoveListener(IncreaseDifficulty);
        if (tableDragToggleButton != null) tableDragToggleButton.onClick.RemoveListener(ToggleTableDrag);
        if (servingToggleButton != null) servingToggleButton.onClick.RemoveListener(ToggleServing);
        if (miniPauseButton != null) miniPauseButton.onClick.RemoveListener(ToggleServing);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetScore);
        if (homeButton != null) homeButton.onClick.RemoveListener(ReturnHome);
        if (_minimizeButton != null) _minimizeButton.onClick.RemoveListener(StartServingAndCompact);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(ReturnHome);
        _buttonsWired = false;
    }

    private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
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
        if (image != null) DestroyComponent(image);
        var roundedPanel = go.GetComponent<ElderCareRoundedPanel>();
        if (roundedPanel == null) roundedPanel = go.AddComponent<ElderCareRoundedPanel>();
        roundedPanel.color = color;
        roundedPanel.cornerRadius = radius;
        roundedPanel.cornerSegments = 10;
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

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
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
        if (text != null) text.text = value;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = value;
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
