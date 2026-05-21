using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public TMP_FontAsset uiFont;
    public BallSpawner ballSpawner;
    public bool autoCreateDifficultyControls = true;
    public bool enhanceHudReadability = true;
    public Vector2 hudPanelSize = new Vector2(680f, 432f);
    public Color hudPanelColor = new Color(0.015f, 0.03f, 0.045f, 0.78f);
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text missedText;
    public TMP_Text accuracyText;
    public TMP_Text lastSpeedText;
    public TMP_Text lastSpinText;

    private Image _hudBackdrop;
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
        EnsureReadableHud();
        RefreshUI();
    }

    private void Start()
    {
        EnsureDifficultyControls();
        EnsureReadableHud();
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

    private void EnsureReadableHud()
    {
        if (!enhanceHudReadability) return;

        var texts = new[] { hitText, servedText, missedText, accuracyText, lastSpeedText, lastSpinText };
        Transform canvasTransform = null;
        var firstSiblingIndex = int.MaxValue;
        var validTextCount = 0;
        var center = Vector2.zero;

        foreach (var text in texts)
        {
            if (text == null) continue;

            var rect = text.rectTransform;
            if (rect == null) continue;

            canvasTransform = canvasTransform != null
                ? canvasTransform
                : (text.canvas != null ? text.canvas.transform : rect.parent);
            firstSiblingIndex = Mathf.Min(firstSiblingIndex, rect.GetSiblingIndex());
            validTextCount++;
            center += rect.anchoredPosition;

            ConfigureHudText(text);
        }

        if (canvasTransform == null || validTextCount == 0) return;

        center /= validTextCount;

        if (_hudBackdrop == null)
        {
            var existing = canvasTransform.Find("ScoreHudBackdrop");
            var backdropObject = existing != null
                ? existing.gameObject
                : new GameObject("ScoreHudBackdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(canvasTransform, false);
            _hudBackdrop = backdropObject.GetComponent<Image>();
        }

        if (_hudBackdrop == null) return;

        var backdropRect = _hudBackdrop.rectTransform;
        backdropRect.anchorMin = new Vector2(0.5f, 0.5f);
        backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        backdropRect.sizeDelta = hudPanelSize;
        backdropRect.anchoredPosition = center;
        backdropRect.localRotation = Quaternion.identity;
        backdropRect.localScale = Vector3.one;
        _hudBackdrop.color = hudPanelColor;
        _hudBackdrop.raycastTarget = false;
        _hudBackdrop.transform.SetSiblingIndex(Mathf.Max(0, firstSiblingIndex - 1));
    }

    private static void ConfigureHudText(TMP_Text text)
    {
        if (text == null) return;

        var rect = text.rectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 620f), Mathf.Max(rect.sizeDelta.y, 64f));
        }

        text.fontSize = Mathf.Max(text.fontSize, 52f);
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(1f, 1f, 1f, 0.98f);
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.18f);
        text.enableWordWrapping = false;
        text.raycastTarget = false;
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
