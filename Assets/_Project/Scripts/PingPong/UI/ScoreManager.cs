using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TMP_FontAsset uiFont;
    public BallSpawner ballSpawner;
    public bool autoCreateDifficultyControls = true;
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text missedText;
    public TMP_Text accuracyText;
    public TMP_Text lastSpeedText;
    public TMP_Text lastSpinText;

    private int _servedCount;
    private int _hitCount;
    private int _missedCount;
    private float _lastHitSpeed;
    private float _lastSpinSpeed;

    private void OnEnable()
    {
        PingPongEvents.OnBallServed += HandleBallServed;
        PingPongEvents.OnBallHit += HandleBallHit;
        PingPongEvents.OnBallHitDetailed += HandleBallHitDetailed;
        PingPongEvents.OnBallMissed += HandleBallMissed;
        ResolveFontIfNeeded();
        ApplyFont();
        RefreshUI();
    }

    private void Start()
    {
        EnsureDifficultyControls();
    }

    private void OnDisable()
    {
        PingPongEvents.OnBallServed -= HandleBallServed;
        PingPongEvents.OnBallHit -= HandleBallHit;
        PingPongEvents.OnBallHitDetailed -= HandleBallHitDetailed;
        PingPongEvents.OnBallMissed -= HandleBallMissed;
    }

    public void ResetScore()
    {
        _servedCount = 0;
        _hitCount = 0;
        _missedCount = 0;
        _lastHitSpeed = 0f;
        _lastSpinSpeed = 0f;
        RefreshUI();
    }

    private void HandleBallServed()
    {
        _servedCount++;
        RefreshUI();
    }

    private void HandleBallHit()
    {
        _hitCount++;
        RefreshUI();
    }

    private void HandleBallHitDetailed(BallHitInfo info)
    {
        _lastHitSpeed = info.OutgoingSpeed;
        _lastSpinSpeed = info.SpinSpeed;
        RefreshUI();
    }

    private void HandleBallMissed()
    {
        _missedCount++;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var acc = _servedCount > 0 ? (float)_hitCount / _servedCount * 100f : 0f;

        if (hitText != null) hitText.text = $"命中：{_hitCount}";
        if (servedText != null) servedText.text = $"发球：{_servedCount}";
        if (missedText != null) missedText.text = $"漏球：{_missedCount}";
        if (accuracyText != null) accuracyText.text = $"命中率：{acc:0.0}%";
        if (lastSpeedText != null) lastSpeedText.text = $"回球速度：{_lastHitSpeed:0.0} m/s";
        if (lastSpinText != null) lastSpinText.text = $"旋转：{_lastSpinSpeed:0} rad/s";
    }

    private void ApplyFont()
    {
        if (uiFont == null) return;

        if (hitText != null) hitText.font = uiFont;
        if (servedText != null) servedText.font = uiFont;
        if (missedText != null) missedText.font = uiFont;
        if (accuracyText != null) accuracyText.font = uiFont;
        if (lastSpeedText != null) lastSpeedText.font = uiFont;
        if (lastSpinText != null) lastSpinText.font = uiFont;
    }

    private void EnsureDifficultyControls()
    {
        if (!autoCreateDifficultyControls) return;

        var canvasTransform = ResolveCanvasTransform();
        if (canvasTransform == null) return;

        if (ballSpawner == null)
        {
            ballSpawner = FindSceneObject<BallSpawner>();
        }

        if (ballSpawner == null) return;

        PingPongDifficultyController.EnsureRuntimePanel(canvasTransform, ballSpawner, uiFont);
    }

    private Transform ResolveCanvasTransform()
    {
        if (hitText != null && hitText.canvas != null)
        {
            return hitText.canvas.transform;
        }

        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            return canvas.transform;
        }

        var worldCanvas = GameObject.Find("WorldSpaceCanvas");
        return worldCanvas != null ? worldCanvas.transform : null;
    }

    private void ResolveFontIfNeeded()
    {
        if (uiFont != null) return;

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font != null && font.name == "RehabChineseTMP")
            {
                uiFont = font;
                return;
            }
        }
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
