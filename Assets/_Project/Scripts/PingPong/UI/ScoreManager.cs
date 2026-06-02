using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    private static readonly Vector2 HudPanelPosition = new Vector2(-570f, 215f);
    private static readonly Vector2 PrimaryMetricSize = new Vector2(292f, 142f);
    private static readonly Vector2 AccuracyPosition = new Vector2(-722f, 293f);
    private static readonly Vector2 SpeedPosition = new Vector2(-418f, 293f);
    private static readonly Vector2 SecondaryMetricSize = new Vector2(186f, 82f);
    private static readonly Vector2 HitPosition = new Vector2(-780f, 145f);
    private static readonly Vector2 ServedPosition = new Vector2(-570f, 145f);
    private static readonly Vector2 MissedPosition = new Vector2(-360f, 145f);
    private static readonly Vector2 SpinSize = new Vector2(606f, 58f);
    private static readonly Vector2 SpinPosition = new Vector2(-570f, 51f);

    public TMP_FontAsset uiFont;
    public BallSpawner ballSpawner;
    public bool autoCreateDifficultyControls = false;
    public bool useUnifiedControlPanel = true;
    public bool enhanceHudReadability = true;
    public Vector2 hudPanelSize = ElderCareUiTheme.PingPongHudSize;
    public Color hudPanelColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.96f);
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text missedText;
    public TMP_Text accuracyText;
    public TMP_Text lastSpeedText;
    public TMP_Text lastSpinText;

    private Graphic _hudBackdrop;
    private int _servedCount;
    private int _hitCount;
    private int _missedCount;
    private float _lastHitSpeed;
    private float _lastSpinSpeed;

    public int ServedCount => _servedCount;
    public int HitCount => _hitCount;
    public int MissedCount => _missedCount;
    public float LastHitSpeed => _lastHitSpeed;
    public float LastSpinSpeed => _lastSpinSpeed;
    public float Accuracy => _servedCount > 0 ? (float)_hitCount / _servedCount * 100f : 0f;

    private void OnEnable()
    {
        PingPongEvents.OnBallServed += HandleBallServed;
        PingPongEvents.OnBallHit += HandleBallHit;
        PingPongEvents.OnBallHitDetailed += HandleBallHitDetailed;
        PingPongEvents.OnBallMissed += HandleBallMissed;
        ResolveFontIfNeeded();
        if (!useUnifiedControlPanel)
        {
            ApplyFont();
            EnsureReadableHud();
            EnsureDisplayCanvasInteraction();
        }
        RefreshUI();
    }

    private void Start()
    {
        if (!useUnifiedControlPanel)
        {
            EnsureDifficultyControls();
            EnsureReadableHud();
            EnsureDisplayCanvasInteraction();
        }
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
        var accuracy = Accuracy;

        if (accuracyText != null) accuracyText.text = FormatPrimaryMetric("命中率", accuracy.ToString("0.0"), "%");
        if (lastSpeedText != null) lastSpeedText.text = FormatPrimaryMetric("回球速度", _lastHitSpeed.ToString("0.0"), "m/s");
        if (hitText != null) hitText.text = FormatSecondaryMetric("命中", _hitCount);
        if (servedText != null) servedText.text = FormatSecondaryMetric("发球", _servedCount);
        if (missedText != null) missedText.text = FormatSecondaryMetric("漏球", _missedCount);
        if (lastSpinText != null) lastSpinText.text = FormatAuxiliaryMetric("旋转", _lastSpinSpeed.ToString("0"), "rad/s");
    }

    private static string FormatPrimaryMetric(string label, string value, string unit)
    {
        var separator = unit == "%" ? string.Empty : " ";
        return $"<size=28>{label}</size>\n<size=72><b>{value}</b></size>{separator}<size=28>{unit}</size>";
    }

    private static string FormatSecondaryMetric(string label, int value)
    {
        return $"<size=22>{label}</size>\n<size=40><b>{value}</b></size>";
    }

    private static string FormatAuxiliaryMetric(string label, string value, string unit)
    {
        return $"<size=22>{label}</size>  <size=32><b>{value}</b></size> <size=20>{unit}</size>";
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

        var canvasTransform = ResolveCanvasTransform();
        if (canvasTransform == null) return;

        EnsureHudBackdrop(canvasTransform);
        EnsureMetricCard(canvasTransform, "AccuracyMetricCard", PrimaryMetricSize, AccuracyPosition, ElderCareUiTheme.Cyan, 0.16f, 24f);
        EnsureMetricCard(canvasTransform, "SpeedMetricCard", PrimaryMetricSize, SpeedPosition, ElderCareUiTheme.Blue, 0.14f, 24f);
        EnsureMetricCard(canvasTransform, "HitMetricCard", SecondaryMetricSize, HitPosition, ElderCareUiTheme.Green, 0.09f, 18f);
        EnsureMetricCard(canvasTransform, "ServedMetricCard", SecondaryMetricSize, ServedPosition, ElderCareUiTheme.Cyan, 0.08f, 18f);
        EnsureMetricCard(canvasTransform, "MissedMetricCard", SecondaryMetricSize, MissedPosition, ElderCareUiTheme.Orange, 0.08f, 18f);
        EnsureMetricCard(canvasTransform, "SpinMetricCard", SpinSize, SpinPosition, ElderCareUiTheme.Violet, 0.08f, 18f);

        ConfigureHudText(accuracyText, PrimaryMetricSize, AccuracyPosition, ElderCareUiTheme.HudPrimary + 18f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.Cyan, 0.16f);
        ConfigureHudText(lastSpeedText, PrimaryMetricSize, SpeedPosition, ElderCareUiTheme.HudPrimary + 10f, FontStyles.Bold, TextAlignmentOptions.Center, Color.Lerp(ElderCareUiTheme.TextPrimary, ElderCareUiTheme.Blue, 0.24f), 0.14f);
        ConfigureHudText(hitText, SecondaryMetricSize, HitPosition, ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextPrimary, 0.1f);
        ConfigureHudText(servedText, SecondaryMetricSize, ServedPosition, ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextSecondary, 0.1f);
        ConfigureHudText(missedText, SecondaryMetricSize, MissedPosition, ElderCareUiTheme.HudSecondary + 8f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareUiTheme.TextSecondary, 0.1f);
        ConfigureHudText(lastSpinText, SpinSize, SpinPosition, ElderCareUiTheme.HudSecondary, FontStyles.Normal, TextAlignmentOptions.Center, ElderCareUiTheme.TextMuted, 0.08f);
    }

    private void EnsureHudBackdrop(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("ScoreHudBackdrop");
        var backdropObject = existing != null
            ? existing.gameObject
            : new GameObject("ScoreHudBackdrop", typeof(RectTransform), typeof(ElderCareRoundedPanel));
        backdropObject.transform.SetParent(canvasTransform, false);

        _hudBackdrop = ConfigureRoundedPanel(backdropObject, hudPanelSize, HudPanelPosition, hudPanelColor, 30f, true);
        if (_hudBackdrop == null) return;

        var outline = EnsureComponent<Outline>(backdropObject);
        if (outline != null)
        {
            outline.effectColor = WithAlpha(ElderCareUiTheme.PanelStroke, 0.58f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        _hudBackdrop.transform.SetAsFirstSibling();
    }

    private static void EnsureMetricCard(Transform parent, string name, Vector2 size, Vector2 position, Color accent, float alpha, float radius)
    {
        var child = parent.Find(name);
        var go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform), typeof(ElderCareRoundedPanel));
        go.transform.SetParent(parent, false);

        var panel = ConfigureRoundedPanel(go, size, position, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, accent, 0.28f), alpha + 0.56f), radius, false);
        var outline = EnsureComponent<Outline>(go);
        if (outline != null)
        {
            outline.effectColor = WithAlpha(accent, alpha + 0.32f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        const string traceName = "TopTrace";
        var activeTrace = go.transform.Find(traceName);
        if (activeTrace != null)
        {
            Object.Destroy(activeTrace.gameObject);
        }

        var legacyTrace = go.transform.Find(name + "_TopTrace");
        if (legacyTrace != null)
        {
            Object.Destroy(legacyTrace.gameObject);
        }

        if (panel != null)
        {
            panel.transform.SetSiblingIndex(Mathf.Min(panel.transform.GetSiblingIndex(), 1));
        }
    }

    private static void ConfigureHudText(TMP_Text text, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color, float outlineWidth)
    {
        if (text == null) return;

        ConfigureRect(text.gameObject, size, anchoredPosition);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, outlineWidth);
        text.enableWordWrapping = false;
        text.richText = true;
        text.raycastTarget = false;
        text.transform.SetAsLastSibling();
    }

    private void EnsureDifficultyControls()
    {
        if (useUnifiedControlPanel) return;
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

    public void EnsureDisplayCanvasInteraction()
    {
        var canvasTransform = ResolveCanvasTransform();
        if (canvasTransform == null) return;

        var canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
        }

        var placer = canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>();
        if (placer == null)
        {
            placer = canvasTransform.gameObject.AddComponent<ComfortWorldSpaceUIPlacer>();
        }

        placer.uiRoot = canvasTransform;
        if (placer.headTransform == null && Camera.main != null)
        {
            placer.headTransform = Camera.main.transform;
        }

        placer.distanceMeters = Mathf.Max(placer.distanceMeters, ElderCareUiTheme.HudDistanceMeters);
        placer.hmdHeightOffsetMeters = Mathf.Max(placer.hmdHeightOffsetMeters, 0.12f);
        placer.placeOnStart = false;
        placer.placeOnEnable = false;
        placer.recenterDuringStartup = false;
        placer.enableRayDrag = true;
        placer.enableThumbstickNavigation = true;
        placer.comfortFollowEnabled = false;
        placer.EnsureWorldSpaceInteractionHelpers();
        WorldSpaceUiRayDragHandle.EnsureOnSurface(_hudBackdrop, canvasTransform, placer);
    }

    private Transform ResolveCanvasTransform()
    {
        if (hitText != null && hitText.canvas != null)
        {
            return hitText.canvas.transform;
        }

        if (accuracyText != null && accuracyText.canvas != null)
        {
            return accuracyText.canvas.transform;
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

    private static RectTransform ConfigureRect(GameObject go, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = go.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Graphic ConfigureRoundedPanel(GameObject go, Vector2 size, Vector2 anchoredPosition, Color color, float radius, bool raycastTarget)
    {
        ConfigureRect(go, size, anchoredPosition);

        var image = go.GetComponent<Image>();
        if (image != null)
        {
            DestroyComponent(image);
        }

        var roundedPanel = go.GetComponent<ElderCareRoundedPanel>();
        if (roundedPanel == null)
        {
            roundedPanel = go.AddComponent<ElderCareRoundedPanel>();
        }

        roundedPanel.color = color;
        roundedPanel.cornerRadius = radius;
        roundedPanel.raycastTarget = raycastTarget;
        roundedPanel.SetAllDirty();
        return roundedPanel;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void DestroyComponent(Component component)
    {
        if (component == null) return;

        if (Application.isPlaying)
        {
            Object.Destroy(component);
            return;
        }

        Object.DestroyImmediate(component);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
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
