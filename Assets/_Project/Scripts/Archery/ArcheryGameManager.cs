using UnityEngine;

public class ArcheryGameManager : MonoBehaviour
{
    private const string BestScoreKeyPrefix = "ElderCare.Archery.BestScore.";
    private const string AimAssistKey = "ElderCare.Archery.AimAssist";
    private const string BowHandKey = "ElderCare.Archery.BowInLeftHand";
    private const string SessionCountKey = "ElderCare.Archery.SessionCount";

    [Header("场景绑定")]
    public BowController bow;
    public ArcheryTarget target;
    public Transform laneRoot;
    public Transform targetRig;
    public Transform targetHeightPivot;
    public Transform headTransform;
    public Transform arrowContainer;
    public ArcheryScorePanel scorePanel;
    public ParticleSystem goldHitParticles;
    public ParticleSystem hitDustParticles;

    [Header("训练配置")]
    public int arrowsPerRound = 10;
    public ArcheryDifficulty difficulty = ArcheryDifficulty.Medium;
    public bool alignLaneToUserOnStart = true;
    public bool calibrateTargetHeightOnStart = true;
    public bool spawnScorePopups = true;

    [Header("适老辅助")]
    public bool enableAimAssist = true;
    public float aimAssistDegrees = ArcheryGeometry.AimAssistDefaultDegrees;

    private int _totalScore;
    private int _arrowsReleased;
    private int _arrowsResolved;
    private int _bestSingleScore;
    private bool _sessionActive;
    private bool _roundFinished;

    public int TotalScore => _totalScore;
    public int ArrowsReleased => _arrowsReleased;
    public int ArrowsRemaining => Mathf.Max(0, arrowsPerRound - _arrowsReleased);
    public bool SessionActive => _sessionActive;
    public bool RoundFinished => _roundFinished;

    private void Start()
    {
        LoadSettings();
        ApplySettingsToBow();
        UpdatePanel();
    }

    private void OnEnable()
    {
        ArcheryEvents.OnArrowReleased += HandleArrowReleased;
        ArcheryEvents.OnArrowHit += HandleArrowHit;
        ArcheryEvents.OnArrowMissed += HandleArrowMissed;
    }

    private void OnDisable()
    {
        ArcheryEvents.OnArrowReleased -= HandleArrowReleased;
        ArcheryEvents.OnArrowHit -= HandleArrowHit;
        ArcheryEvents.OnArrowMissed -= HandleArrowMissed;
    }

    public void StartSession()
    {
        _totalScore = 0;
        _arrowsReleased = 0;
        _arrowsResolved = 0;
        _bestSingleScore = 0;
        _roundFinished = false;
        _sessionActive = true;

        ClearArrows();

        if (alignLaneToUserOnStart)
        {
            AlignLaneToUser();
        }

        ApplyDifficulty();
        ApplySettingsToBow();

        if (bow != null)
        {
            bow.CancelDraw();
            bow.SetFiringEnabled(true);
        }

        var sessionCount = PlayerPrefs.GetInt(SessionCountKey, 0) + 1;
        PlayerPrefs.SetInt(SessionCountKey, sessionCount);
        PlayerPrefs.Save();

        UpdatePanel();
        SetStatus(sessionCount <= 2
            ? "第一次玩？右手靠近弓身握紧手柄搭弦，慢慢向后拉，松手放箭"
            : "握紧右手手柄搭弦，向后拉再松开放箭");
        SetLastHitText("--");
        ArcheryEvents.SessionStarted();
    }

    public void StopSession()
    {
        if (!_sessionActive) return;

        _sessionActive = false;
        if (bow != null)
        {
            bow.CancelDraw();
            bow.SetFiringEnabled(false);
        }

        ArcheryEvents.SessionFinished();
    }

    public void RestartRound()
    {
        StartSession();
    }

    public void SetDifficulty(ArcheryDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficulty();
        UpdatePanel();
    }

    public void SetDifficultyNear() => SetDifficulty(ArcheryDifficulty.Near);
    public void SetDifficultyMedium() => SetDifficulty(ArcheryDifficulty.Medium);
    public void SetDifficultyFar() => SetDifficulty(ArcheryDifficulty.Far);

    public void ToggleAimAssist()
    {
        enableAimAssist = !enableAimAssist;
        PlayerPrefs.SetInt(AimAssistKey, enableAimAssist ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettingsToBow();
        UpdatePanel();
        SetStatus(enableAimAssist ? "辅助瞄准已开启：拉弓时会显示弹道，落点自动微调" : "辅助瞄准已关闭：全凭真本事！");
    }

    public void ToggleBowHand()
    {
        if (bow == null) return;

        bow.SetBowInLeftHand(!bow.bowInLeftHand);
        PlayerPrefs.SetInt(BowHandKey, bow.bowInLeftHand ? 1 : 0);
        PlayerPrefs.Save();
        UpdatePanel();
        SetStatus(bow.bowInLeftHand ? "已切换为左手持弓、右手拉弦" : "已切换为右手持弓、左手拉弦");
    }

    public void RecenterLane()
    {
        AlignLaneToUser();
        SetStatus("已重新对准：箭道已转到你的正前方");
    }

    public void AlignLaneToUser()
    {
        var head = ResolveHeadTransform();
        if (laneRoot == null || head == null) return;

        laneRoot.rotation = ArcherySolver.ComputeLaneRotationFromHeadForward(head.forward, laneRoot.rotation);
        laneRoot.position = new Vector3(head.position.x, 0f, head.position.z);

        if (calibrateTargetHeightOnStart && targetHeightPivot != null)
        {
            var localHeight = targetHeightPivot.localPosition;
            localHeight.y = ArcherySolver.ComputeTargetCenterHeight(head.position.y);
            targetHeightPivot.localPosition = localHeight;
        }
    }

    public void ApplyDifficulty()
    {
        if (targetRig == null) return;

        var local = targetRig.localPosition;
        local.z = ArcheryGeometry.TargetDistanceForDifficulty(difficulty);
        targetRig.localPosition = local;
    }

    public void ClearArrows()
    {
        ClearChildren(arrowContainer);
        ClearChildren(target != null ? target.StickParent : null);
    }

    public int LoadBestScore(ArcheryDifficulty forDifficulty)
    {
        return PlayerPrefs.GetInt(BestScoreKeyPrefix + forDifficulty, 0);
    }

    private void LoadSettings()
    {
        enableAimAssist = PlayerPrefs.GetInt(AimAssistKey, 1) == 1;
        var bowInLeftHand = PlayerPrefs.GetInt(BowHandKey, 1) == 1;
        if (bow != null)
        {
            bow.SetBowInLeftHand(bowInLeftHand);
        }
    }

    private void ApplySettingsToBow()
    {
        if (bow == null) return;

        var assistAnchor = target != null
            ? (target.faceCenter != null ? target.faceCenter : target.transform)
            : null;
        bow.SetAimAssist(assistAnchor, enableAimAssist ? aimAssistDegrees : 0f, enableAimAssist);
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

    private void HandleArrowReleased(ArrowReleasedInfo info)
    {
        if (!_sessionActive) return;

        _arrowsReleased++;
        if (_arrowsReleased >= arrowsPerRound && bow != null)
        {
            bow.SetFiringEnabled(false);
        }

        UpdatePanel();
    }

    private void HandleArrowHit(ArrowHitInfo info)
    {
        if (!_sessionActive) return;

        _arrowsResolved++;
        _totalScore += info.score;
        if (info.score > _bestSingleScore)
        {
            _bestSingleScore = info.score;
        }

        SetLastHitText(info.score > 0 ? $"{info.score} 环" : "上靶未中环");
        SpawnHitFeedback(info);
        UpdatePanel();
        CheckRoundEnd();
    }

    private void HandleArrowMissed(ArrowMissedInfo info)
    {
        if (!_sessionActive) return;

        _arrowsResolved++;
        SetLastHitText("脱靶");
        if (spawnScorePopups)
        {
            var position = info.position;
            position.y = Mathf.Max(0.4f, position.y);
            ArcheryScorePopup.Spawn(position, "脱靶", new Color(1f, 1f, 1f, 0.72f), 0.026f);
        }

        UpdatePanel();
        CheckRoundEnd();
    }

    private void SpawnHitFeedback(ArrowHitInfo info)
    {
        if (spawnScorePopups)
        {
            var isGold = info.score >= ArcheryGeometry.TargetMaxRingScore;
            var message = info.score > 0 ? (isGold ? "10 环！" : $"{info.score} 环") : "未中环";
            var color = isGold
                ? new Color(1f, 0.84f, 0.25f)
                : (info.score > 0 ? Color.white : new Color(1f, 1f, 1f, 0.72f));
            ArcheryScorePopup.Spawn(info.hitPoint + Vector3.up * 0.12f, message, color, isGold ? 0.042f : 0.032f);
        }

        if (hitDustParticles != null)
        {
            hitDustParticles.transform.position = info.hitPoint;
            hitDustParticles.Emit(12);
        }

        if (info.score >= ArcheryGeometry.TargetMaxRingScore && goldHitParticles != null)
        {
            goldHitParticles.transform.position = info.hitPoint;
            goldHitParticles.Emit(40);
        }
    }

    private void CheckRoundEnd()
    {
        if (_roundFinished) return;
        if (_arrowsReleased < arrowsPerRound)
        {
            SetStatus($"打得不错，还剩 {ArrowsRemaining} 支箭");
            return;
        }
        if (_arrowsResolved < _arrowsReleased) return;

        _roundFinished = true;
        var stars = ArcherySolver.ComputeStarRating(_totalScore, arrowsPerRound, ArcheryGeometry.TargetMaxRingScore);
        var previousBest = LoadBestScore(difficulty);
        var isNewBest = _totalScore > previousBest;
        if (isNewBest)
        {
            PlayerPrefs.SetInt(BestScoreKeyPrefix + difficulty, _totalScore);
            PlayerPrefs.Save();
        }

        UpdatePanel();
        SetStatus($"{StarsText(stars)} {EncouragementForStars(stars)}{(isNewBest ? " 刷新个人纪录！" : "")} 点击“再来一轮”继续");
        ArcheryEvents.RoundFinished(new ArcheryRoundResult(_totalScore, arrowsPerRound, stars, isNewBest));
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
        scorePanel.SetArrows(_arrowsReleased, arrowsPerRound);
        scorePanel.SetDifficultyLabel(DifficultyLabel(difficulty));
        scorePanel.SetBestScore(Mathf.Max(_totalScore, LoadBestScore(difficulty)));
        scorePanel.SetAssistLabel(enableAimAssist ? "辅助瞄准：开" : "辅助瞄准：关");
        scorePanel.SetHandednessLabel(bow != null && !bow.bowInLeftHand ? "持弓手：右手" : "持弓手：左手");
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

    public static string StarsText(int stars)
    {
        var clamped = Mathf.Clamp(stars, 0, 5);
        return new string('★', clamped) + new string('☆', 5 - clamped);
    }

    public static string EncouragementForStars(int stars)
    {
        switch (Mathf.Clamp(stars, 0, 5))
        {
            case 5:
                return "百步穿杨，太厉害了！";
            case 4:
                return "宝刀未老，好眼力！";
            case 3:
                return "稳中有进，继续加油！";
            case 2:
                return "越来越顺手了！";
            case 1:
                return "热身完毕，再来一轮！";
            default:
                return "慢慢来，可以先切换到近距靶试试。";
        }
    }

    public static string DifficultyLabel(ArcheryDifficulty value)
    {
        switch (value)
        {
            case ArcheryDifficulty.Near:
                return "近距 4 米";
            case ArcheryDifficulty.Far:
                return "远距 8.5 米";
            default:
                return "中距 6 米";
        }
    }
}
