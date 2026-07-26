using UnityEngine;

public class DartsGameManager : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "ElderCare.Darts.BestScore.";
    private const string AimAssistKey = "ElderCare.Darts.AimAssist";
    private const string ThrowHandKey = "ElderCare.Darts.ThrowWithRightHand";
    private const string SessionCountKey = "ElderCare.Darts.SessionCount";

    [Header("场景绑定")]
    public HandDartThrower thrower;
    public DartsBoard board;
    public Transform laneRoot;
    public Transform boardRig;
    public Transform boardHeightPivot;
    public Transform headTransform;
    public Transform dartContainer;
    public DartsScorePanel scorePanel;
    public ParticleSystem goldHitParticles;
    public ParticleSystem hitDustParticles;
    public Font popupFont;

    [Header("训练配置")]
    public int dartsPerRound = 10;
    public DartsDifficulty difficulty = DartsDifficulty.Standard;
    public bool autoStartSessionOnStart;
    public bool alignLaneToUserOnStart = true;
    public bool calibrateBoardHeightOnStart = true;
    public bool spawnScorePopups = true;

    [Header("适老辅助")]
    public bool enableAimAssist = true;
    public float aimAssistDegrees = DartsGeometry.AimAssistDefaultDegrees;

    [Header("启动重对准（独立场景）")]
    public float startupRealignSeconds = 0.5f;
    public int startupRealignFrames = 6;

    private int _totalScore;
    private int _dartsThrown;
    private int _dartsResolved;
    private int _bestSingleScore;
    private bool _sessionActive;
    private bool _roundFinished;

    public int TotalScore => _totalScore;
    public int DartsThrown => _dartsThrown;
    public int DartsRemaining => Mathf.Max(0, dartsPerRound - _dartsThrown);
    public bool SessionActive => _sessionActive;
    public bool RoundFinished => _roundFinished;

    private void Start()
    {
        LoadSettings();

        if (autoStartSessionOnStart)
        {
            StartSession();
            // 场景刚加载完的头显 pose 往往还没稳定跟踪，Start 里那次对齐
            // 可能对着默认原点。启动后的短暂窗口内持续重对齐，直到跟踪稳定。
            if (alignLaneToUserOnStart)
            {
                StartCoroutine(RealignLaneDuringStartup());
            }

            return;
        }

        ApplySettingsToThrower();
        UpdatePanel();
    }

    private System.Collections.IEnumerator RealignLaneDuringStartup()
    {
        var elapsedSeconds = 0f;
        var elapsedFrames = 0;
        while (elapsedSeconds < startupRealignSeconds || elapsedFrames < startupRealignFrames)
        {
            yield return null;
            elapsedSeconds += Time.unscaledDeltaTime;
            elapsedFrames++;

            // 玩家一旦握镖或已投出，说明已经进入游玩，立即停止自动重对准。
            if (_dartsThrown > 0 || (thrower != null && thrower.IsHolding)) yield break;

            AlignLaneToUser();
        }
    }

    private void OnEnable()
    {
        DartsEvents.OnDartThrown += HandleDartThrown;
        DartsEvents.OnDartHit += HandleDartHit;
        DartsEvents.OnDartMissed += HandleDartMissed;
    }

    private void OnDisable()
    {
        DartsEvents.OnDartThrown -= HandleDartThrown;
        DartsEvents.OnDartHit -= HandleDartHit;
        DartsEvents.OnDartMissed -= HandleDartMissed;
    }

    public void StartSession()
    {
        StartSessionInternal(true);
    }

    public void RestartRound()
    {
        // 再来一轮不重新对准投掷区：按按钮时玩家通常正看着侧面的计分板。
        StartSessionInternal(false);
    }

    private void StartSessionInternal(bool realignLane)
    {
        _totalScore = 0;
        _dartsThrown = 0;
        _dartsResolved = 0;
        _bestSingleScore = 0;
        _roundFinished = false;
        _sessionActive = true;

        ClearDarts();

        if (alignLaneToUserOnStart && realignLane)
        {
            AlignLaneToUser();
        }

        ApplyDifficulty();
        ApplySettingsToThrower();

        if (thrower != null)
        {
            thrower.CancelHold();
            thrower.SetThrowsEnabled(true);
        }

        var sessionCount = PlayerPrefs.GetInt(SessionCountKey, 0) + 1;
        PlayerPrefs.SetInt(SessionCountKey, sessionCount);
        PlayerPrefs.Save();

        UpdatePanel();
        var handLabel = thrower != null && !thrower.throwWithRightHand ? "左手" : "右手";
        SetStatus(sessionCount <= 2
            ? $"第一次玩？{handLabel}握紧手柄拿镖，朝镖盘挥臂时松手投出"
            : $"{handLabel}握紧手柄拿镖，挥臂松手投出");
        SetLastHitText("--");
        DartsEvents.SessionStarted();
    }

    public void StopSession()
    {
        if (!_sessionActive) return;

        _sessionActive = false;
        if (thrower != null)
        {
            thrower.CancelHold();
            thrower.SetThrowsEnabled(false);
        }

        DartsEvents.SessionFinished();
    }

    public void SetDifficulty(DartsDifficulty newDifficulty)
    {
        var changed = difficulty != newDifficulty;
        difficulty = newDifficulty;
        ApplyDifficulty();

        // 回合中途换盘距会导致分数混档、在飞的镖对着瞬移后的盘：
        // 直接以新盘距重开本轮，规则最好理解。
        if (_sessionActive && changed)
        {
            StartSessionInternal(false);
            SetStatus($"已切换到{DifficultyLabel(newDifficulty)}，本轮重新开始");
        }

        UpdatePanel();
    }

    public void SetDifficultyNear() => SetDifficulty(DartsDifficulty.Near);
    public void SetDifficultyStandard() => SetDifficulty(DartsDifficulty.Standard);
    public void SetDifficultyFar() => SetDifficulty(DartsDifficulty.Far);

    public void ToggleAimAssist()
    {
        enableAimAssist = !enableAimAssist;
        PlayerPrefs.SetInt(AimAssistKey, enableAimAssist ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettingsToThrower();
        UpdatePanel();
        SetStatus(enableAimAssist ? "辅助瞄准已开启：落点会向盘心自动微调" : "辅助瞄准已关闭：全凭真本事！");
    }

    public void ToggleThrowHand()
    {
        if (thrower == null) return;

        thrower.SetThrowWithRightHand(!thrower.throwWithRightHand);
        PlayerPrefs.SetInt(ThrowHandKey, thrower.throwWithRightHand ? 1 : 0);
        PlayerPrefs.Save();
        UpdatePanel();
        SetStatus(thrower.throwWithRightHand ? "已切换为右手投掷" : "已切换为左手投掷");
    }

    public void RecenterLane()
    {
        AlignLaneToUser();
        SetStatus("已重新对准：镖盘已转到你的正前方");
    }

    public void AlignLaneToUser()
    {
        var head = ResolveHeadTransform();
        if (laneRoot == null || head == null) return;

        laneRoot.rotation = ArcherySolver.ComputeLaneRotationFromHeadForward(head.forward, laneRoot.rotation);
        laneRoot.position = new Vector3(head.position.x, 0f, head.position.z);

        if (calibrateBoardHeightOnStart && boardHeightPivot != null)
        {
            var localHeight = boardHeightPivot.localPosition;
            localHeight.y = DartsSolver.ComputeBoardCenterHeight(head.position.y);
            boardHeightPivot.localPosition = localHeight;
        }
    }

    public void ApplyDifficulty()
    {
        if (boardRig == null) return;

        var local = boardRig.localPosition;
        local.z = DartsGeometry.BoardDistanceForDifficulty(difficulty);
        boardRig.localPosition = local;
    }

    public void ClearDarts()
    {
        ClearChildren(dartContainer);
        ClearChildren(board != null ? board.StickParent : null);
    }

    public int LoadBestScore(DartsDifficulty forDifficulty)
    {
        return PlayerPrefs.GetInt(BestScoreKeyPrefix + forDifficulty, 0);
    }

    private void LoadSettings()
    {
        enableAimAssist = PlayerPrefs.GetInt(AimAssistKey, 1) == 1;
        var rightHanded = PlayerPrefs.GetInt(ThrowHandKey, 1) == 1;
        if (thrower != null)
        {
            thrower.SetThrowWithRightHand(rightHanded);
        }
    }

    private void ApplySettingsToThrower()
    {
        if (thrower == null) return;

        var assistAnchor = board != null
            ? (board.faceCenter != null ? board.faceCenter : board.transform)
            : null;
        thrower.SetAimAssist(assistAnchor, enableAimAssist ? aimAssistDegrees : 0f);
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child == null) continue;

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

    private void HandleDartThrown(DartThrownInfo info)
    {
        if (!_sessionActive) return;

        _dartsThrown++;
        if (_dartsThrown >= dartsPerRound && thrower != null)
        {
            thrower.SetThrowsEnabled(false);
        }

        UpdatePanel();
    }

    private void HandleDartHit(DartHitInfo info)
    {
        if (!_sessionActive) return;

        _dartsResolved++;
        _totalScore += info.score;
        if (info.score > _bestSingleScore)
        {
            _bestSingleScore = info.score;
        }

        SetLastHitText(info.score > 0 ? $"{info.score} 环" : "上盘未中环");
        SpawnHitFeedback(info);
        UpdatePanel();
        CheckRoundEnd();
    }

    private void HandleDartMissed(DartMissedInfo info)
    {
        if (!_sessionActive) return;

        _dartsResolved++;
        SetLastHitText("脱靶");
        if (spawnScorePopups)
        {
            var position = info.position;
            position.y = Mathf.Max(0.4f, position.y);
            position += TowardHeadDirection(position) * 0.25f;
            ArcheryScorePopup.Spawn(position, "脱靶", new Color(1f, 1f, 1f, 0.72f), 0.024f, popupFont);
        }

        UpdatePanel();
        CheckRoundEnd();
    }

    private void SpawnHitFeedback(DartHitInfo info)
    {
        if (spawnScorePopups)
        {
            var isGold = info.score >= DartsGeometry.BoardMaxRingScore;
            var message = info.score > 0 ? (isGold ? "10 环！" : $"{info.score} 环") : "未中环";
            var color = isGold
                ? new Color(1f, 0.84f, 0.25f)
                : (info.score > 0 ? Color.white : new Color(1f, 1f, 1f, 0.72f));
            // 飘分往玩家方向推出，避免文字嵌进镖盘平面产生穿插。
            var popupPosition = info.hitPoint + Vector3.up * 0.1f + TowardHeadDirection(info.hitPoint) * 0.25f;
            ArcheryScorePopup.Spawn(popupPosition, message, color, isGold ? 0.038f : 0.028f, popupFont);
        }

        if (hitDustParticles != null)
        {
            hitDustParticles.transform.position = info.hitPoint;
            hitDustParticles.Emit(10);
        }

        if (info.score >= DartsGeometry.BoardMaxRingScore && goldHitParticles != null)
        {
            goldHitParticles.transform.position = info.hitPoint;
            goldHitParticles.Emit(36);
        }
    }

    private void CheckRoundEnd()
    {
        if (_roundFinished) return;
        if (_dartsThrown < dartsPerRound)
        {
            SetStatus($"打得不错，还剩 {DartsRemaining} 支镖");
            return;
        }
        if (_dartsResolved < _dartsThrown) return;

        _roundFinished = true;
        var stars = ArcherySolver.ComputeStarRating(_totalScore, dartsPerRound, DartsGeometry.BoardMaxRingScore);
        var previousBest = LoadBestScore(difficulty);
        var isNewBest = _totalScore > previousBest;
        if (isNewBest)
        {
            PlayerPrefs.SetInt(BestScoreKeyPrefix + difficulty, _totalScore);
            PlayerPrefs.Save();
        }

        UpdatePanel();
        SetStatus($"{ArcheryGameManager.StarsText(stars)} {ArcheryGameManager.EncouragementForStars(stars)}{(isNewBest ? " 刷新个人纪录！" : "")} 点击“再来一轮”继续");
        DartsEvents.RoundFinished(new DartsRoundResult(_totalScore, dartsPerRound, stars, isNewBest));
    }

    private Vector3 TowardHeadDirection(Vector3 fromPosition)
    {
        var head = ResolveHeadTransform();
        if (head == null) return Vector3.up * 0.2f;

        var toward = head.position - fromPosition;
        toward.y = 0f;
        return toward.sqrMagnitude > 0.0001f ? toward.normalized : Vector3.up * 0.2f;
    }

    private Transform ResolveHeadTransform()
    {
        if (headTransform != null) return headTransform;
        if (Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        return headTransform;
    }

    private void UpdatePanel()
    {
        if (scorePanel == null) return;

        scorePanel.SetScore(_totalScore);
        scorePanel.SetDarts(_dartsThrown, dartsPerRound);
        scorePanel.SetDifficultyLabel(DifficultyLabel(difficulty));
        scorePanel.SetDifficultySelection(difficulty);
        scorePanel.SetBestScore(Mathf.Max(_totalScore, LoadBestScore(difficulty)));
        scorePanel.SetAssistLabel(enableAimAssist ? "辅助瞄准：开" : "辅助瞄准：关");
        scorePanel.SetThrowHandLabel(thrower != null && !thrower.throwWithRightHand ? "投掷手：左手" : "投掷手：右手");
    }

    private void SetStatus(string message)
    {
        if (scorePanel != null)
        {
            scorePanel.SetStatus(message);
        }
    }

    private void SetLastHitText(string message)
    {
        if (scorePanel != null)
        {
            scorePanel.SetLastHit(message);
        }
    }

    public static string DifficultyLabel(DartsDifficulty value)
    {
        switch (value)
        {
            case DartsDifficulty.Near:
                return "近距 1.8 米";
            case DartsDifficulty.Far:
                return "远距 3 米";
            default:
                return "标准 2.4 米";
        }
    }
}
