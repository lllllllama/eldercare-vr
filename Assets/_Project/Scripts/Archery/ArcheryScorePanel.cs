using UnityEngine;
using UnityEngine.UI;

public class ArcheryScorePanel : MonoBehaviour
{
    [Header("绑定")]
    public ArcheryGameManager manager;
    public ElderCareHomeMenu homeMenu;
    public Font uiFont;

    [Header("文本")]
    public Text scoreValueText;
    public Text arrowsValueText;
    public Text lastHitValueText;
    public Text difficultyValueText;
    public Text statusText;

    [Header("按钮")]
    public Button restartButton;
    public Button homeButton;
    public Button difficultyNearButton;
    public Button difficultyMediumButton;
    public Button difficultyFarButton;

    private Font _runtimeFont;

    private void Awake()
    {
        ApplyReadableFont();
    }

    private void OnEnable()
    {
        BindButton(restartButton, HandleRestartClick);
        BindButton(homeButton, HandleHomeClick);
        BindButton(difficultyNearButton, HandleNearClick);
        BindButton(difficultyMediumButton, HandleMediumClick);
        BindButton(difficultyFarButton, HandleFarClick);
    }

    private void OnDisable()
    {
        UnbindButton(restartButton, HandleRestartClick);
        UnbindButton(homeButton, HandleHomeClick);
        UnbindButton(difficultyNearButton, HandleNearClick);
        UnbindButton(difficultyMediumButton, HandleMediumClick);
        UnbindButton(difficultyFarButton, HandleFarClick);
    }

    public void SetScore(int totalScore)
    {
        if (scoreValueText != null)
        {
            scoreValueText.text = $"{totalScore} 分";
        }
    }

    public void SetArrows(int releasedCount, int totalCount)
    {
        if (arrowsValueText != null)
        {
            arrowsValueText.text = $"{Mathf.Max(0, totalCount - releasedCount)} / {totalCount}";
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

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void HandleRestartClick()
    {
        if (manager != null)
        {
            manager.RestartRound();
        }
    }

    private void HandleHomeClick()
    {
        if (homeMenu != null)
        {
            homeMenu.ShowHome();
        }
    }

    private void HandleNearClick()
    {
        if (manager != null)
        {
            manager.SetDifficultyNear();
        }
    }

    private void HandleMediumClick()
    {
        if (manager != null)
        {
            manager.SetDifficultyMedium();
        }
    }

    private void HandleFarClick()
    {
        if (manager != null)
        {
            manager.SetDifficultyFar();
        }
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
