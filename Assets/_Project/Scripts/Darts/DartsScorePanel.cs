using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DartsScorePanel : MonoBehaviour, IUiHoverGuard, IPointerEnterHandler, IPointerExitHandler
{
    public const string DefaultHealthGameMenuSceneName = "02_HealthGameMenu";

    [Header("绑定")]
    public DartsGameManager manager;
    public HandDartThrower thrower;
    public Font uiFont;

    [Header("场景导航")]
    [SerializeField]
    private string healthGameMenuSceneName = DefaultHealthGameMenuSceneName;

    [Header("文本")]
    public Text scoreValueText;
    public Text dartsValueText;
    public Text lastHitValueText;
    public Text bestScoreValueText;
    public Text difficultyValueText;
    public Text statusText;
    public Text assistButtonLabel;
    public Text throwHandButtonLabel;

    [Header("按钮")]
    public Button restartButton;
    public Button homeButton;
    public Button difficultyNearButton;
    public Button difficultyStandardButton;
    public Button difficultyFarButton;
    public Button assistToggleButton;
    public Button throwHandButton;
    public Button recenterButton;

    [Header("难度选中态配色")]
    public Color difficultyNormalFill = new Color(0.12f, 0.27f, 0.48f, 1f);
    public Color difficultyNormalOutline = new Color(0.18f, 0.46f, 0.91f, 0.66f);
    public Color difficultySelectedFill = new Color(0.53f, 0.47f, 0.26f, 1f);
    public Color difficultySelectedOutline = new Color(1f, 0.82f, 0.35f, 0.9f);

    private Font _runtimeFont;
    private BaseRaycaster[] _raycasters;
    private bool _raycastBlocked;
    private bool _sceneLoadStarted;
    private int _pointerOverCount;

    // uGUI 的 pointer enter/exit 会沿父链传播，挂在画布根上即可覆盖所有子按钮。
    public bool IsPointerOver => _pointerOverCount > 0;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOverCount++;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOverCount = Mathf.Max(0, _pointerOverCount - 1);
    }

    public string HealthGameMenuSceneName
    {
        get => healthGameMenuSceneName;
        set => healthGameMenuSceneName = value;
    }

    private void Awake()
    {
        // 必须禁用 Canvas 上所有 BaseRaycaster：实机 XR 点击走的是 XRI 的
        // TrackedDeviceGraphicRaycaster，只禁 GraphicRaycaster 等于没防护。
        _raycasters = GetComponents<BaseRaycaster>();
        ApplyReadableFont();
    }

    private void Update()
    {
        // 挥臂投掷过程中屏蔽面板射线点击：出手瞬间扳机释放若恰好扫过面板，
        // 会误触按钮。阈值取 1.2 m/s 手速——静止点按钮不受影响。
        var shouldBlock = thrower != null && thrower.IsHolding &&
                          thrower.CurrentHandSpeed > DartsGeometry.MinThrowHandSpeedMetersPerSecond;
        if (shouldBlock == _raycastBlocked) return;

        _raycastBlocked = shouldBlock;
        if (_raycasters == null) return;

        foreach (var raycaster in _raycasters)
        {
            if (raycaster != null)
            {
                raycaster.enabled = !shouldBlock;
            }
        }
    }

    private void OnEnable()
    {
        BindButton(restartButton, HandleRestartClick);
        BindButton(homeButton, HandleHomeClick);
        BindButton(difficultyNearButton, HandleNearClick);
        BindButton(difficultyStandardButton, HandleStandardClick);
        BindButton(difficultyFarButton, HandleFarClick);
        BindButton(assistToggleButton, HandleAssistClick);
        BindButton(throwHandButton, HandleThrowHandClick);
        BindButton(recenterButton, HandleRecenterClick);
    }

    private void OnDisable()
    {
        // 面板被禁用时 pointer exit 不一定送达，悬停计数必须归零。
        _pointerOverCount = 0;
        UnbindButton(restartButton, HandleRestartClick);
        UnbindButton(homeButton, HandleHomeClick);
        UnbindButton(difficultyNearButton, HandleNearClick);
        UnbindButton(difficultyStandardButton, HandleStandardClick);
        UnbindButton(difficultyFarButton, HandleFarClick);
        UnbindButton(assistToggleButton, HandleAssistClick);
        UnbindButton(throwHandButton, HandleThrowHandClick);
        UnbindButton(recenterButton, HandleRecenterClick);
    }

    public void SetScore(int totalScore)
    {
        if (scoreValueText != null)
        {
            scoreValueText.text = $"{totalScore} 分";
        }
    }

    public void SetDarts(int thrownCount, int totalCount)
    {
        if (dartsValueText != null)
        {
            dartsValueText.text = $"{Mathf.Max(0, totalCount - thrownCount)} / {totalCount}";
        }
    }

    public void SetLastHit(string message)
    {
        if (lastHitValueText != null)
        {
            lastHitValueText.text = message;
        }
    }

    public void SetDifficultyLabel(string message)
    {
        if (difficultyValueText != null)
        {
            difficultyValueText.text = message;
        }
    }

    public void SetBestScore(int bestScore)
    {
        if (bestScoreValueText != null)
        {
            bestScoreValueText.text = bestScore > 0 ? $"{bestScore} 分" : "--";
        }
    }

    public void SetAssistLabel(string message)
    {
        if (assistButtonLabel != null)
        {
            assistButtonLabel.text = message;
        }
    }

    public void SetThrowHandLabel(string message)
    {
        if (throwHandButtonLabel != null)
        {
            throwHandButtonLabel.text = message;
        }
    }

    public void SetDifficultySelection(DartsDifficulty difficulty)
    {
        ApplyDifficultyButtonState(difficultyNearButton, difficulty == DartsDifficulty.Near);
        ApplyDifficultyButtonState(difficultyStandardButton, difficulty == DartsDifficulty.Standard);
        ApplyDifficultyButtonState(difficultyFarButton, difficulty == DartsDifficulty.Far);
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void ReturnToHealthGameMenu()
    {
        CancelHoldBeforeUiAction();
        LoadScene(healthGameMenuSceneName);
    }

    private void ApplyDifficultyButtonState(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;

        var fill = selected ? difficultySelectedFill : difficultyNormalFill;
        button.targetGraphic.color = fill;

        // 悬停动效以 normalColor 为基准回落，必须同步，否则移开指针后高亮被冲掉。
        var motion = button.GetComponent<TechModuleCardMotion>();
        if (motion != null)
        {
            motion.normalColor = fill;
            motion.hoverColor = Color.Lerp(fill, Color.white, 0.12f);
            motion.pressedColor = Color.Lerp(fill, Color.black, 0.18f);
        }

        var outline = button.targetGraphic.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = selected ? difficultySelectedOutline : difficultyNormalOutline;
        }
    }

    private void HandleRestartClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.RestartRound();
        }
    }

    private void HandleHomeClick()
    {
        ReturnToHealthGameMenu();
    }

    private void HandleNearClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.SetDifficultyNear();
        }
    }

    private void HandleStandardClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.SetDifficultyStandard();
        }
    }

    private void HandleFarClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.SetDifficultyFar();
        }
    }

    private void HandleAssistClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.ToggleAimAssist();
        }
    }

    private void HandleThrowHandClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.ToggleThrowHand();
        }
    }

    private void HandleRecenterClick()
    {
        CancelHoldBeforeUiAction();
        if (manager != null)
        {
            manager.RecenterLane();
        }
    }

    private void CancelHoldBeforeUiAction()
    {
        // 扳机既是 UI 点击键又是拿镖键：按钮点击发生在 Update 阶段，早于
        // HandDartThrower.LateUpdate 的出手判定。这里先取消握持，保证
        // “点按钮”永远不会在松开扳机的同一帧误投一支镖。
        if (thrower != null && thrower.IsHolding)
        {
            thrower.CancelHold();
        }
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("DartsScorePanel cannot load the health game menu because the scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"DartsScorePanel cannot load a scene that is not available in Build Settings: {sceneName}");
            return;
        }

        // 异步加载避免 VR 里同步 LoadScene 的整帧冻结黑闪，并防按钮连点重复加载。
        if (_sceneLoadStarted) return;

        _sceneLoadStarted = true;
        SetStatus("正在返回选择页…");
        SceneManager.LoadSceneAsync(sceneName);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
    }

    private void ApplyReadableFont()
    {
        var texts = GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            text.font = ResolveUiFont();
        }
    }

    private Font ResolveUiFont()
    {
        if (uiFont != null) return uiFont;
        if (_runtimeFont != null) return _runtimeFont;

        _runtimeFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" },
            64);

        if (_runtimeFont == null)
        {
            _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return _runtimeFont;
    }
}
