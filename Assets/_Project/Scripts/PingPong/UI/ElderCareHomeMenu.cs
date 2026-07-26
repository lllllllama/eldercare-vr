using UnityEngine;
using UnityEngine.UI;

public class ElderCareHomeMenu : MonoBehaviour
{
    public GameObject homeRoot;
    public GameObject[] pingPongGameplayRoots;
    public GameObject[] archeryGameplayRoots;
    public ArcheryGameManager archeryGameManager;
    public BallSpawner ballSpawner;
    public ScoreManager scoreManager;
    public VrInitialViewAligner initialViewAligner;
    public ComfortWorldSpaceUIPlacer uiPlacer;
    public Text statusText;
    public ElderCareModuleCard[] moduleCards;
    public Font uiFont;
    public bool showHomeOnStart = true;
    public bool clearBallsWhenLeavingPingPong = true;
    public bool placeHomeUiOnShow = true;
    public bool startServingWhenOpeningPingPong = false;

    private Font _runtimeFont;

    private void Awake()
    {
        if (homeRoot == null)
        {
            homeRoot = gameObject;
        }

        if (uiPlacer == null)
        {
            uiPlacer = homeRoot.GetComponentInParent<ComfortWorldSpaceUIPlacer>();
        }

        ApplyReadableFont(homeRoot);
    }

    private void Start()
    {
        if (showHomeOnStart)
        {
            ShowHome();
        }
        else
        {
            StartPingPongModule();
        }
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    // 项目 activeInputHandler 为 Input System-only 时旧版 Input API 会抛异常，
    // 这个编辑器快捷键只在旧输入管线可用时编译。
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHome();
        }
    }
#endif

    public void SelectModule(string moduleId, string moduleTitle)
    {
        if (moduleId == "pingpong")
        {
            StartPingPongModule();
            return;
        }

        if (moduleId == "archery")
        {
            StartArcheryModule();
            return;
        }

        ShowFutureModule(moduleTitle);
    }

    public void ShowHome()
    {
        SetHomeActive(true);
        SetPingPongGameplayActive(false);
        StopArcheryGameplay();
        PlaceHomeUiIfNeeded();

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            if (clearBallsWhenLeavingPingPong)
            {
                ballSpawner.ClearBalls();
            }
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        SetStatus("使用手柄或手势选择功能");
    }

    public void ResetHomeUiPosition()
    {
        if (uiPlacer != null)
        {
            uiPlacer.PlaceInFrontOfUser();
        }
    }

    public void StartPingPongModule()
    {
        SetHomeActive(false);
        StopArcheryGameplay();
        SetPingPongGameplayActive(true);

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (ballSpawner != null)
        {
            ballSpawner.ClearBalls();
            if (startServingWhenOpeningPingPong)
            {
                ballSpawner.StartServing();
            }
        }

        if (initialViewAligner != null)
        {
            initialViewAligner.AlignNow();
        }
    }

    public void StartArcheryModule()
    {
        SetHomeActive(false);
        SetPingPongGameplayActive(false);

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            if (clearBallsWhenLeavingPingPong)
            {
                ballSpawner.ClearBalls();
            }
        }

        SetArcheryGameplayActive(true);

        if (archeryGameManager != null)
        {
            archeryGameManager.StartSession();
        }
    }

    private void StopArcheryGameplay()
    {
        if (archeryGameManager != null)
        {
            archeryGameManager.StopSession();
        }

        SetArcheryGameplayActive(false);
    }

    private void SetArcheryGameplayActive(bool active)
    {
        if (archeryGameplayRoots == null) return;

        foreach (var gameplayRoot in archeryGameplayRoots)
        {
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(active);
            }
        }
    }

    private void ShowFutureModule(string moduleTitle)
    {
        SetHomeActive(true);
        SetPingPongGameplayActive(false);
        StopArcheryGameplay();

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            ballSpawner.ClearBalls();
        }

        SetStatus($"{moduleTitle} 功能正在接入");
    }

    private void SetHomeActive(bool active)
    {
        if (homeRoot != null)
        {
            homeRoot.SetActive(active);
        }
    }

    private void SetPingPongGameplayActive(bool active)
    {
        if (pingPongGameplayRoots == null) return;

        foreach (var gameplayRoot in pingPongGameplayRoots)
        {
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(active);
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void PlaceHomeUiIfNeeded()
    {
        if (!placeHomeUiOnShow || uiPlacer == null) return;

        uiPlacer.PlaceInFrontOfUser();
    }

    private void ApplyReadableFont(GameObject root)
    {
        if (root == null) return;

        var texts = root.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            text.font = ResolveUiFont();
        }
    }

    private Font ResolveUiFont()
    {
        return uiFont != null ? uiFont : GetRuntimeFont();
    }

    private Font GetRuntimeFont()
    {
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
