using System.Collections.Generic;
using PicoElderCare.Rehab;
using PicoElderCare.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PingPongUnifiedControlPanel : MonoBehaviour
{
    private const string RuntimePanelName = "PingPongUnifiedControlPanel";
    private static readonly Vector2 PanelSize = new Vector2(620f, 560f);
    private static readonly Vector2 PanelPosition = new Vector2(-160f, 40f);
    private static readonly Vector2 ContentSize = new Vector2(572f, 0f);
    private static readonly Vector2 MiniPanelSize = new Vector2(340f, 180f);
    private static readonly Vector2 PrimaryButtonSize = new Vector2(300f, 72f);
    private static readonly Vector2 SecondaryButtonSize = new Vector2(220f, 64f);
    private static readonly Vector2 StandardButtonSize = new Vector2(180f, 64f);
    private static readonly Vector2 SmallButtonSize = new Vector2(48f, 48f);

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
    private const string ExpandPanelLabel = "\u5c55\u5f00";
    private const string CollapsePanelLabel = "\u6536";
    private static readonly string RequiredChineseGlyphs =
        TitleLabel + StatusWaiting + StatusTraining + StatusPaused +
        AssessmentLabel + DifficultyLabel + ControlLabel + AccuracyLabel +
        HitLabel + ServedLabel + MissedLabel + HitSpeedLabel + SpinSpeedLabel +
        ServeSpeedPrefix + PauseServingLabel + ResumeServingLabel + ContinueServingLabel +
        ResetScoreLabel + ReturnHomeLabel + CoachEmptyLabel + DifficultyDownLabel +
        DifficultyUpLabel + DifficultyNormalLabel + DifficultyAdvancedLabel +
        DifficultyChallengeLabel + DifficultyCustomLabel + TableDragTurnOnLabel +
        TableDragTurnOffLabel + TableDragUnavailableLabel + ExpandPanelLabel + CollapsePanelLabel +
        "0123456789.%m/srad\u00d7";

    private const string IconTableTennis = "table_tennis";
    private const string IconAssessment = "bar_chart";
    private const string IconTarget = "direct_hit";
    private const string IconHourglass = "hourglass";
    private const string IconPause = "pause";
    private const string IconPlay = "play";
    private const string IconDifficulty = "level_slider";
    private const string IconCoach = "robot_face";
    private const string IconHome = "home";
    private const string IconCheck = "check";
    private const string IconCross = "cross";
    private const string IconRunning = "green_circle";
    private const string IconSpeed = "high_voltage";
    private const string IconSpin = "cyclone";
    private const string IconResourceRoot = "HtmlSvgIcons/";

    public ScoreManager scoreManager;
    public BallSpawner ballSpawner;
    public PingPongDifficultyController difficultyController;
    public RemoteTableDragController tableDragController;
    public ElderCareHomeMenu homeMenu;
    public ModuleHomeMenu moduleHomeMenu;
    public TMP_FontAsset uiFont;
    public string mainEntrySceneName = "00_MainEntry";
    public bool loadMainEntryWhenHomeMenuMissing = true;

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
    public Button miniResetButton;
    public Button miniHomeButton;
    public TMP_Text miniHitText;
    public TMP_Text miniAccuracyText;

    private Button _minimizeButton;
    private Button _closeButton;
    private Button _miniExpandButton;
    private CanvasGroup _fullPanelGroup;
    private CanvasGroup _miniPanelGroup;
    private Image _statusIcon;
    private Graphic _accuracyFill;
    private Graphic[] _difficultyGears;
    private bool _buttonsWired;
    private bool _hasStartedServing;
    private bool _forceFullPanel;
    private bool _layoutBuilt;
    private bool _standaloneDifficultyPanelsSuppressed;
    private float _nextRefreshTime;
    private TMP_FontAsset _runtimeFont;
    private TMP_FontAsset _runtimeFontSource;

    private static readonly Dictionary<string, Sprite> IconSpriteCache = new Dictionary<string, Sprite>();

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

    private void OnDestroy()
    {
        RuntimeTmpFontAssetUtility.DestroyRuntimeFont(ref _runtimeFont, ref _runtimeFontSource);
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
            _forceFullPanel = true;
        }
        else
        {
            ballSpawner.StartServing();
            _hasStartedServing = true;
            _forceFullPanel = false;
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

        _forceFullPanel = false;
        RefreshDisplay();
    }

    public void ExpandFullPanel()
    {
        _forceFullPanel = true;
        SetPanelVisibility(_hasStartedServing);
    }

    public void CollapseToMiniPanel()
    {
        _forceFullPanel = false;
        SetPanelVisibility(true);
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

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            ballSpawner.ClearBallsWithoutScoring();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (moduleHomeMenu != null)
        {
            moduleHomeMenu.LoadMainEntry();
            return;
        }

        if (homeMenu != null)
        {
            homeMenu.ShowHome();
            return;
        }

        if (loadMainEntryWhenHomeMenuMissing && Application.isPlaying && !string.IsNullOrEmpty(mainEntrySceneName))
        {
            SceneManager.LoadScene(mainEntrySceneName);
            return;
        }

        Debug.LogWarning("PingPongUnifiedControlPanel could not return home because no ElderCareHomeMenu was found.");
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
        SetText(hitText, $"<size=160%><b>{ReadHitCount()}</b></size>\n{HitLabel}");
        SetText(servedText, $"<size=160%><b>{ReadServedCount()}</b></size>\n{ServedLabel}");
        SetText(missedText, $"<size=160%><b>{ReadMissedCount()}</b></size>\n{MissedLabel}");
        SetText(accuracyText, $"<size=160%><b>{ReadAccuracy():0}%</b></size>\n{AccuracyLabel}");
        SetText(speedText, $"{HitSpeedLabel}  <b>{ReadLastHitSpeed():0.0} m/s</b>");
        SetText(difficultyText, ResolveDifficultyLabel());
        SetText(serveSpeedText, $"{ServeSpeedPrefix}\n<b>{ResolveServeSpeed():0.0} m/s</b>");
        SetText(spinText, $"{SpinSpeedLabel}\n<b>{ResolveServeSpin():0} rad/s</b>");
        SetText(miniHitText, $"<size=160%><b>{ReadHitCount()}</b></size>\n{HitLabel}");
        SetText(miniAccuracyText, $"<size=160%><b>{ReadAccuracy():0}%</b></size>\n{AccuracyLabel}");
        SetButtonLabel(servingToggleButton, serving ? PauseServingLabel : (_hasStartedServing ? ContinueServingLabel : ResumeServingLabel));
        SetButtonLabel(miniPauseButton, serving ? PauseServingLabel : ContinueServingLabel);
        SetButtonLabel(coachButton, CoachEmptyLabel);
        SetButtonIcon(servingToggleButton, serving ? IconPause : IconPlay);
        SetButtonIcon(miniPauseButton, serving ? IconPause : IconPlay);
        RefreshStatusIcon(serving);

        RefreshDifficultyGears();
        RefreshTableDragButton();
        SetPanelVisibility(_hasStartedServing);
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
                return 3;
            case PingPongDifficulty.Challenge:
            case PingPongDifficulty.Custom:
                return 4;
            default:
                return 2;
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
        compact = compact && !_forceFullPanel;

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
        if (moduleHomeMenu == null) moduleHomeMenu = FindSceneObject<ModuleHomeMenu>();
        SuppressStandaloneDifficultyPanels();
        var resolvedFont = RuntimeTmpFontAssetUtility.ResolveSourceFont(uiFont, _runtimeFont, _runtimeFontSource);
        if (resolvedFont == null) resolvedFont = ResolveRuntimeFont();
        uiFont = RuntimeTmpFontAssetUtility.PrepareDynamicFont(resolvedFont, RequiredChineseGlyphs, ref _runtimeFont, ref _runtimeFontSource);
    }

    private void BuildOrRepairLayout()
    {
        if (GetComponent<MrKeepVisible>() == null)
        {
            gameObject.AddComponent<MrKeepVisible>();
        }

        if (_layoutBuilt && FindChild(transform, "FullPanel") != null)
        {
            RepairEmbeddedDifficultyControlsIfNeeded();
            EnsureMiniPanelOnTop();
            WireButtons();
            return;
        }

        UnwireButtons();
        ClearChildren(transform);
        _difficultyGears = new Graphic[5];

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
        ConfigurePanel(GetOrCreateChild(fullRect, "AmbientGlow"), new Vector2(580f, 520f), Vector2.zero, new Color(0.38f, 0.92f, 1f, 0.045f), 34f, false).transform.SetAsFirstSibling();

        titleText = ConfigureText(GetOrCreateChild(fullRect, "Title"), TitleLabel, new Vector2(336f, 52f), new Vector2(-62f, 232f), 34f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Left);
        ConfigureSvgIcon(GetOrCreateChild(fullRect, "TitleIcon"), IconTableTennis, new Vector2(44f, 44f), new Vector2(-270f, 232f), 0.98f);
        statusText = ConfigureBadge(fullRect, "StatusBadge", StatusWaiting, new Vector2(174f, 44f), new Vector2(198f, 232f), ElderCareUiTheme.Gold, IconHourglass);

        var assessment = ConfigurePanel(GetOrCreateChild(fullRect, "AssessmentPanel"), new Vector2(ContentSize.x, 120f), new Vector2(0f, 136f), new Color(1f, 1f, 1f, 0.055f), 20f, false);
        AddOutline(assessment.gameObject, new Color(1f, 1f, 1f, 0.12f), new Vector2(1.5f, -1.5f));
        hitText = CreateMetric(assessment.rectTransform, "HitMetric", HitLabel, IconCheck, new Vector2(-207f, 0f), ElderCareUiTheme.Green);
        servedText = CreateMetric(assessment.rectTransform, "ServedMetric", ServedLabel, IconTableTennis, new Vector2(-69f, 0f), ElderCareUiTheme.Cyan);
        missedText = CreateMetric(assessment.rectTransform, "MissedMetric", MissedLabel, IconCross, new Vector2(69f, 0f), ElderCareUiTheme.Orange);
        accuracyText = CreateMetric(assessment.rectTransform, "AccuracyMetric", AccuracyLabel, IconTarget, new Vector2(207f, 0f), ElderCareUiTheme.Gold);
        _accuracyFill = null;

        BuildDifficultyPanel(fullRect);
        BuildControlPanel(fullRect);
    }

    private void BuildControlPanel(RectTransform parent)
    {
        var panel = ConfigurePanel(GetOrCreateChild(parent, "ControlPanel"), new Vector2(ContentSize.x, 172f), new Vector2(0f, -166f), new Color(1f, 1f, 1f, 0.055f), 20f, false);
        AddOutline(panel.gameObject, new Color(0.38f, 0.66f, 0.94f, 0.25f), new Vector2(1.5f, -1.5f));

        servingToggleButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "ServingToggleButton"), ResumeServingLabel, IconPlay, new Vector2(0f, 42f), PrimaryButtonSize, ElderCareUiTheme.Green, true);
        resetButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "ResetButton"), ResetScoreLabel, IconAssessment, new Vector2(-108f, -48f), StandardButtonSize, ElderCareUiTheme.Cyan, true);
        homeButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "HomeButton"), ReturnHomeLabel, IconHome, new Vector2(102f, -48f), SecondaryButtonSize, ElderCareUiTheme.Orange, true);

        coachButton = null;
        _minimizeButton = ConfigureButton(GetOrCreateChild(panel.rectTransform, "MinimizeButton"), CollapsePanelLabel, new Vector2(258f, -48f), SmallButtonSize, ElderCareUiTheme.Cyan, true);
        _closeButton = null;
    }

    private void BuildDifficultyPanel(RectTransform parent)
    {
        RemoveChildIfExists(parent, "EmbeddedDifficultyControls");
        RemoveChildIfExists(parent, "DifficultyPanel");

        var panelGraphic = ConfigurePanel(GetOrCreateChild(parent, "DifficultyPanel"), new Vector2(ContentSize.x, 108f), new Vector2(0f, 10f), new Color(1f, 1f, 1f, 0.055f), 20f, false);
        AddOutline(panelGraphic.gameObject, new Color(1f, 0.82f, 0.35f, 0.18f), new Vector2(1.5f, -1.5f));
        var panel = panelGraphic.rectTransform;
        ConfigureSvgIcon(GetOrCreateChild(panel, "DifficultyIcon"), IconDifficulty, new Vector2(22f, 22f), new Vector2(-250f, 28f), 0.98f);
        ConfigureText(GetOrCreateChild(panel, "DifficultyLabel"), DifficultyLabel, new Vector2(170f, 28f), new Vector2(-152f, 28f), 20f, FontStyles.Bold, ElderCareUiTheme.Gold, TextAlignmentOptions.Left);
        difficultyDownButton = ConfigureButton(GetOrCreateChild(panel, "DifficultyDownButton"), DifficultyDownLabel, new Vector2(-214f, -20f), SmallButtonSize, ElderCareUiTheme.SoftButton, true);
        difficultyUpButton = ConfigureButton(GetOrCreateChild(panel, "DifficultyUpButton"), DifficultyUpLabel, new Vector2(10f, -20f), SmallButtonSize, ElderCareUiTheme.SoftButton, true);
        difficultyText = ConfigureText(GetOrCreateChild(panel, "DifficultyText"), DifficultyNormalLabel, new Vector2(152f, 40f), new Vector2(-102f, -14f), 28f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);

        for (var i = 0; i < _difficultyGears.Length; i++)
        {
            _difficultyGears[i] = ConfigurePanel(GetOrCreateChild(panel, "Gear" + i), new Vector2(22f, 7f), new Vector2(-102f + (i - 2) * 28f, -40f), new Color(1f, 1f, 1f, 0.18f), 4f, false);
        }

        serveSpeedText = CreateInlineInfoText(panel, "ServeSpeed", ServeSpeedPrefix, IconSpeed, new Vector2(160f, 20f), ElderCareUiTheme.Gold);
        spinText = CreateInlineInfoText(panel, "SpinSpeed", SpinSpeedLabel, IconSpin, new Vector2(160f, -24f), ElderCareUiTheme.Gold);
        speedText = null;
    }

    private void RepairEmbeddedDifficultyControlsIfNeeded()
    {
        if (!HasLegacyEmbeddedDifficultyPanel()) return;

        var controlPanel = transform.Find("FullPanel/ControlPanel") as RectTransform;
        if (controlPanel == null) return;

        UnwireButtons();
        if (_difficultyGears == null || _difficultyGears.Length != 5)
        {
            _difficultyGears = new Graphic[5];
        }

        BuildDifficultyPanel(controlPanel);
        WireButtons();
        RefreshDisplay();
    }

    private void BuildMiniPanel(RectTransform rootRect)
    {
        var mini = GetOrCreateChild(transform, "MiniPanel");
        var miniRect = ConfigureRect(mini, MiniPanelSize, new Vector2(-130f, 185f));
        _miniPanelGroup = EnsureComponent<CanvasGroup>(mini);
        var bg = ConfigurePanel(mini, MiniPanelSize, Vector2.zero, new Color32(0x12, 0x1A, 0x26, 0xE8), 24f, true);
        AddOutline(bg.gameObject, new Color(0.38f, 0.92f, 1f, 0.54f), new Vector2(2f, -2f));
        miniHitText = CreateMetric(miniRect, "MiniHitMetric", HitLabel, IconCheck, new Vector2(-76f, 38f), ElderCareUiTheme.Green);
        miniAccuracyText = CreateMetric(miniRect, "MiniAccuracyMetric", AccuracyLabel, IconTarget, new Vector2(76f, 38f), ElderCareUiTheme.Gold);
        miniPauseButton = ConfigureButton(GetOrCreateChild(miniRect, "MiniPauseButton"), PauseServingLabel, IconPause, new Vector2(-78f, -55f), new Vector2(170f, 64f), ElderCareUiTheme.Gold, true);
        _miniExpandButton = ConfigureButton(GetOrCreateChild(miniRect, "MiniExpandButton"), ExpandPanelLabel, new Vector2(35f, -55f), SmallButtonSize, ElderCareUiTheme.Cyan, true);
        miniResetButton = ConfigureButton(GetOrCreateChild(miniRect, "MiniResetButton"), "\u91cd", new Vector2(95f, -55f), SmallButtonSize, ElderCareUiTheme.Cyan, true);
        miniHomeButton = ConfigureButton(GetOrCreateChild(miniRect, "MiniHomeButton"), ReturnHomeLabel, IconHome, new Vector2(145f, -55f), SmallButtonSize, ElderCareUiTheme.Orange, true);
    }

    private void BuildHiddenCompatibilityControls(RectTransform rootRect)
    {
        tableDragToggleButton = ConfigureButton(GetOrCreateChild(rootRect, "LegacyTableDragToggleButton"), TableDragUnavailableLabel, new Vector2(9999f, 9999f), new Vector2(4f, 4f), ElderCareUiTheme.Violet, true);
        var canvasGroup = tableDragToggleButton.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void CreateSectionLabel(RectTransform parent, string name, string label, string iconResource, Vector2 position, Color accent)
    {
        ConfigureSvgIcon(GetOrCreateChild(parent, name + "Icon"), iconResource, new Vector2(14f, 14f), new Vector2(position.x - 64f, position.y), 0.98f);
        ConfigureText(GetOrCreateChild(parent, name), label, new Vector2(190f, 30f), new Vector2(position.x + 48f, position.y), 18f, FontStyles.Bold, accent, TextAlignmentOptions.Left);
        ConfigurePanel(GetOrCreateChild(parent, name + "Divider"), new Vector2(318f, 2f), new Vector2(114f, position.y), new Color(1f, 1f, 1f, 0.12f), 1f, false);
    }

    private TMP_Text CreateMetric(RectTransform parent, string name, string label, string iconResource, Vector2 position, Color accent)
    {
        var root = GetOrCreateChild(parent, name);
        var rect = ConfigureRect(root, new Vector2(132f, 86f), position);
        var bg = ConfigurePanel(GetOrCreateChild(rect, "Panel"), new Vector2(132f, 86f), Vector2.zero, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.26f), 0.72f), 14f, false);
        AddOutline(bg.gameObject, WithAlpha(accent, 0.34f), new Vector2(1.5f, -1.5f));
        ConfigureSvgIcon(GetOrCreateChild(rect, "Icon"), iconResource, new Vector2(20f, 20f), new Vector2(-47f, 0f), 0.98f);
        return ConfigureText(GetOrCreateChild(rect, "Text"), label, new Vector2(96f, 76f), new Vector2(14f, 0f), 22f, FontStyles.Bold, ElderCareUiTheme.TextPrimary, TextAlignmentOptions.Center);
    }

    private TMP_Text CreateInlineInfoText(RectTransform parent, string name, string label, string iconResource, Vector2 position, Color accent)
    {
        ConfigureSvgIcon(GetOrCreateChild(parent, name + "Icon"), iconResource, new Vector2(18f, 18f), new Vector2(position.x - 88f, position.y), 0.94f);
        return ConfigureText(GetOrCreateChild(parent, name + "Text"), label, new Vector2(170f, 32f), new Vector2(position.x + 10f, position.y), 17f, FontStyles.Bold, accent, TextAlignmentOptions.Left);
    }

    private TMP_Text ConfigureBadge(RectTransform parent, string name, string label, Vector2 size, Vector2 position, Color accent, string iconResource)
    {
        var panel = ConfigurePanel(GetOrCreateChild(parent, name + "Panel"), size, position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.28f), 0.92f), size.y * 0.5f, false);
        _statusIcon = ConfigureSvgIcon(GetOrCreateChild(panel.rectTransform, name + "Icon"), iconResource, new Vector2(24f, 24f), new Vector2(-56f, 0f), 0.98f);
        return ConfigureText(GetOrCreateChild(panel.rectTransform, name), label, size - new Vector2(50f, 6f), new Vector2(18f, 0f), 20f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
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

        var fullPanel = FindChild(rootRect, "FullPanel");
        var handleParent = fullPanel != null ? fullPanel.transform : rootRect;
        var handle = GetOrCreateChild(handleParent, "DragHandle");
        var handleRect = ConfigureRect(handle, new Vector2(PanelSize.x, 148f), new Vector2(0f, PanelSize.y * 0.5f - 74f));
        handleRect.SetAsLastSibling();

        var image = handle.GetComponent<Image>();
        if (image == null) image = handle.AddComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(0.35f, 0.75f, 0.9f, 0f);

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
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(14f, fontSize - 6f);
        text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = new Vector4(4f, 2f, 4f, 2f);
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private Button ConfigureButton(GameObject go, string label, Vector2 position, Vector2 size, Color accent, bool interactable)
    {
        return ConfigureButton(go, label, null, position, size, accent, interactable);
    }

    private Button ConfigureButton(GameObject go, string label, string iconResource, Vector2 position, Vector2 size, Color accent, bool interactable)
    {
        var rect = ConfigureRect(go, size, position);
        var graphic = ConfigurePanel(go, size, position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, interactable ? 0.58f : 0.26f), interactable ? 0.98f : 0.58f), Mathf.Min(22f, size.y * 0.48f), true);
        AddOutline(go, WithAlpha(accent, interactable ? 0.5f : 0.16f), new Vector2(1.5f, -1.5f));

        var button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;
        button.interactable = interactable;

        RectTransform iconRect = null;
        if (iconResource != null)
        {
            var icon = ConfigureSvgIcon(GetOrCreateChild(rect, "Icon"), iconResource, Vector2.zero, Vector2.zero, interactable ? 0.98f : 0.52f);
            iconRect = icon.rectTransform;
        }

        var maxFontSize = size.y >= 70f ? 34f : size.x <= SmallButtonSize.x ? 24f : 30f;
        var minFontSize = size.y >= 70f ? 26f : 20f;
        var buttonLabel = ConfigureText(GetOrCreateChild(rect, "Label"), label, size, Vector2.zero, maxFontSize, FontStyles.Bold, interactable ? ElderCareUiTheme.TextPrimary : WithAlpha(ElderCareUiTheme.TextPrimary, 0.62f), TextAlignmentOptions.Center);
        ElderCareButtonTextFitter.Configure(go, iconRect, buttonLabel, minFontSize, maxFontSize, size.x <= 72f && iconRect != null);
        return button;
    }

    private Image ConfigureSvgIcon(GameObject go, string resourceName, Vector2 size, Vector2 position, float alpha)
    {
        ConfigureRect(go, size, position);
        var image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.sprite = LoadHtmlIconSprite(resourceName);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, image.sprite != null ? alpha : 0f);
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

    private void SuppressStandaloneDifficultyPanels()
    {
        if (_standaloneDifficultyPanelsSuppressed) return;

        var controllers = Resources.FindObjectsOfTypeAll<PingPongDifficultyController>();
        var foundSceneController = false;
        for (var i = 0; i < controllers.Length; i++)
        {
            var controller = controllers[i];
            if (controller == null || !controller.gameObject.scene.IsValid()) continue;
            foundSceneController = true;

            controller.displayStandalonePanel = false;
            controller.showScreenButtons = false;
            controller.enableControllerSpeedButtons = false;

            if (controller.transform == transform || controller.transform.IsChildOf(transform)) continue;
            if (controller.gameObject.name != "DifficultyPanel") continue;

            var group = EnsureComponent<CanvasGroup>(controller.gameObject);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            controller.gameObject.SetActive(false);

            if (difficultyController == null)
            {
                difficultyController = controller;
            }
        }

        _standaloneDifficultyPanelsSuppressed = foundSceneController;
    }

    private void RefreshStatusIcon(bool serving)
    {
        if (_statusIcon == null) return;

        var icon = serving ? IconRunning : _hasStartedServing || ReadServedCount() > 0 || ReadHitCount() > 0 || ReadMissedCount() > 0
            ? IconPause
            : IconHourglass;
        _statusIcon.sprite = LoadHtmlIconSprite(icon);
        _statusIcon.color = new Color(1f, 1f, 1f, _statusIcon.sprite != null ? 0.98f : 0f);
    }

    private void WireButtons()
    {
        if (_buttonsWired) return;

        if (difficultyDownButton != null) difficultyDownButton.onClick.AddListener(DecreaseDifficulty);
        if (difficultyUpButton != null) difficultyUpButton.onClick.AddListener(IncreaseDifficulty);
        if (tableDragToggleButton != null) tableDragToggleButton.onClick.AddListener(ToggleTableDrag);
        if (servingToggleButton != null) servingToggleButton.onClick.AddListener(ToggleServing);
        if (miniPauseButton != null) miniPauseButton.onClick.AddListener(ToggleServing);
        if (_miniExpandButton != null) _miniExpandButton.onClick.AddListener(ExpandFullPanel);
        if (miniResetButton != null) miniResetButton.onClick.AddListener(ResetScore);
        if (miniHomeButton != null) miniHomeButton.onClick.AddListener(ReturnHome);
        if (resetButton != null) resetButton.onClick.AddListener(ResetScore);
        if (homeButton != null) homeButton.onClick.AddListener(ReturnHome);
        if (_minimizeButton != null) _minimizeButton.onClick.AddListener(CollapseToMiniPanel);
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
        if (_miniExpandButton != null) _miniExpandButton.onClick.RemoveListener(ExpandFullPanel);
        if (miniResetButton != null) miniResetButton.onClick.RemoveListener(ResetScore);
        if (miniHomeButton != null) miniHomeButton.onClick.RemoveListener(ReturnHome);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetScore);
        if (homeButton != null) homeButton.onClick.RemoveListener(ReturnHome);
        if (_minimizeButton != null) _minimizeButton.onClick.RemoveListener(CollapseToMiniPanel);
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

    private static RectTransform ConfigureEmbeddedGroup(GameObject go, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = ConfigureRect(go, size, anchoredPosition);

        var graphic = go.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = Color.clear;
            graphic.raycastTarget = false;
        }

        var outline = go.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = Color.clear;
        }

        return rect;
    }

    private bool HasLegacyEmbeddedDifficultyPanel()
    {
        return transform.Find("FullPanel/ControlPanel/DifficultyPanel") != null;
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

    private static void RemoveChildIfExists(Transform parent, string name)
    {
        var child = FindChild(parent, name);
        if (child == null) return;

        DestroyGameObject(child);
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

    private static void DestroyGameObject(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        if (Application.isPlaying)
        {
            Destroy(go);
            return;
        }

        DestroyImmediate(go);
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

    private static void SetButtonIcon(Button button, string resourceName)
    {
        if (button == null) return;

        var icon = FindChild(button.transform, "Icon");
        if (icon == null)
        {
            icon = FindChild(button.transform, "IconSlot/Icon");
        }
        if (icon == null) return;

        var image = icon.GetComponent<Image>();
        if (image == null) return;

        image.sprite = LoadHtmlIconSprite(resourceName);
        image.color = new Color(1f, 1f, 1f, image.sprite != null && button.interactable ? 0.98f : 0f);
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
