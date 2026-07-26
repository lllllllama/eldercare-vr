using UnityEngine;

public class ArcheryGameManager : MonoBehaviour
{
    [Header("场景绑定")]
    public BowController bow;
    public ArcheryTarget target;
    public Transform laneRoot;
    public Transform targetRig;
    public Transform targetHeightPivot;
    public Transform headTransform;
    public Transform arrowContainer;
    public ArcheryScorePanel scorePanel;

    [Header("训练配置")]
    public int arrowsPerRound = 10;
    public ArcheryDifficulty difficulty = ArcheryDifficulty.Medium;
    public bool alignLaneToUserOnStart = true;
    public bool calibrateTargetHeightOnStart = true;

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

        if (bow != null)
        {
            bow.CancelDraw();
            bow.SetFiringEnabled(true);
        }

        UpdatePanel();
        SetStatus("握紧右手手柄搭弦，向后拉再松开放箭");
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
        SetStatus("箭飞出去了，看看落点");
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
        UpdatePanel();
        CheckRoundEnd();
    }

    private void HandleArrowMissed(ArrowMissedInfo info)
    {
        if (!_sessionActive) return;

        _arrowsResolved++;
        SetLastHitText("脱靶");
        UpdatePanel();
        CheckRoundEnd();
    }

    private void CheckRoundEnd()
    {
        if (_roundFinished) return;
        if (_arrowsReleased < arrowsPerRound)
        {
            SetStatus("继续拉弓，瞄准靶心");
            return;
        }
        if (_arrowsResolved < _arrowsReleased) return;

        _roundFinished = true;
        SetStatus($"本轮结束，总分 {_totalScore} 分，点击“再来一轮”继续");
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
