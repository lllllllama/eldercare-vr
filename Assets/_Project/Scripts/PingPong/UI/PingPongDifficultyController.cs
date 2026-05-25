using System.Collections.Generic;
using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public enum PingPongDifficulty
{
    Easy,
    Normal,
    Advanced,
    Challenge,
    Custom
}

public class PingPongDifficultyController : MonoBehaviour
{
    private const string DefaultPrefsKey = "PicoElderCare.PingPong.Difficulty";
    private const string RuntimePanelName = "DifficultyPanel";
    private const string ControllerHint = "A 加速 / B 减速 · 下次发球生效";
    private static readonly Vector2 ReadablePanelSize = new Vector2(560f, 260f);
    private static readonly Vector2 ReadablePanelPosition = new Vector2(520f, 174f);

    public BallSpawner ballSpawner;
    public TMP_Text difficultyText;
    public TMP_Text speedText;
    public TMP_Text hintText;
    public Button decreaseButton;
    public Button increaseButton;
    public Button resetButton;
    public PingPongDifficulty startingDifficulty = PingPongDifficulty.Normal;
    public bool rememberDifficulty = true;
    public string playerPrefsKey = DefaultPrefsKey;

    [Range(1.5f, 5.0f)] public float customSpeed = 3.1f;
    public bool controlServeInterval = true;
    public bool enhancePanelReadability = true;
    public bool showScreenButtons = false;
    public bool enableControllerSpeedButtons = true;
    public XRNode controllerButtonNode = XRNode.RightHand;
    public bool createVisiblePanel = true;

    private readonly List<InputDevice> _buttonDevices = new List<InputDevice>();
    private PingPongDifficulty _difficulty;
    private bool _buttonsWired;
    private bool _wasAcceleratePressed;
    private bool _wasDeceleratePressed;

    public PingPongDifficulty CurrentDifficulty => _difficulty;
    public float CurrentSpeed => ballSpawner != null ? ballSpawner.serveSpeed : GetPreset(startingDifficulty).speed;
    public string CurrentLabel => GetLabel(_difficulty);

    public static PingPongDifficultyController EnsureRuntimeController(GameObject host, BallSpawner spawner)
    {
        if (host == null) return null;

        var controller = host.GetComponent<PingPongDifficultyController>();
        if (controller == null)
        {
            controller = host.AddComponent<PingPongDifficultyController>();
        }

        controller.ballSpawner = spawner;
        controller.difficultyText = null;
        controller.speedText = null;
        controller.hintText = null;
        controller.decreaseButton = null;
        controller.resetButton = null;
        controller.increaseButton = null;
        controller.startingDifficulty = PingPongDifficulty.Normal;
        controller.controlServeInterval = true;
        controller.showScreenButtons = false;
        controller.enableControllerSpeedButtons = true;
        controller.createVisiblePanel = false;
        controller.enhancePanelReadability = false;
        controller.RebindButtons();
        controller.ApplyLoadedDifficulty();
        return controller;
    }

    public static PingPongDifficultyController EnsureRuntimePanel(Transform canvasTransform, BallSpawner spawner, TMP_FontAsset fontAsset)
    {
        if (canvasTransform == null) return null;

        var root = FindChild(canvasTransform, RuntimePanelName);
        if (root == null)
        {
            root = new GameObject(RuntimePanelName, typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
        }
        else
        {
            var existingController = root.GetComponent<PingPongDifficultyController>();
            if (existingController != null && existingController.difficultyText != null)
            {
                existingController.ballSpawner = spawner;
                existingController.RebindButtons();
                existingController.ApplyLoadedDifficulty();
                existingController.ApplyReadabilityLayout();
                return existingController;
            }
        }

        var rootRect = ConfigureRect(root, ReadablePanelSize, ReadablePanelPosition);
        var background = ConfigurePanel(GetOrCreateChild(root.transform, "Background"), ReadablePanelSize, Vector2.zero, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.22f), 1f), 26f, false);
        var glow = ConfigurePanel(GetOrCreateChild(root.transform, "Glow"), new Vector2(584f, 286f), Vector2.zero, WithAlpha(ElderCareUiTheme.Cyan, 0.05f), 30f, false);
        if (glow != null) glow.transform.SetAsFirstSibling();
        ConfigurePanel(GetOrCreateChild(root.transform, "TopTrace"), new Vector2(420f, 3f), new Vector2(0f, 104f), WithAlpha(ElderCareUiTheme.Cyan, 0.22f), 2f, false);
        ConfigurePanel(GetOrCreateChild(root.transform, "BottomTrace"), new Vector2(320f, 2f), new Vector2(0f, -106f), WithAlpha(ElderCareUiTheme.Blue, 0.16f), 2f, false);

        var title = ConfigureText(GetOrCreateChild(root.transform, "Title"), "发球速度", fontAsset, new Vector2(480f, 44f), new Vector2(0f, 72f), ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        var difficulty = ConfigureText(GetOrCreateChild(root.transform, "DifficultyText"), "当前难度：标准", fontAsset, new Vector2(480f, 42f), new Vector2(0f, 24f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Cyan);
        var speed = ConfigureText(GetOrCreateChild(root.transform, "SpeedText"), "发球速度 3.0 m/s", fontAsset, new Vector2(480f, 44f), new Vector2(0f, -20f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        var hint = ConfigureText(GetOrCreateChild(root.transform, "HintText"), ControllerHint, fontAsset, new Vector2(500f, 40f), new Vector2(0f, -82f), ElderCareUiTheme.BodySmall, FontStyles.Normal, ElderCareUiTheme.TextSecondary);

        var decrease = ConfigureButton(GetOrCreateChild(root.transform, "DecreaseButton"), "-", fontAsset, new Vector2(104f, 68f), new Vector2(-166f, -66f));
        var reset = ConfigureButton(GetOrCreateChild(root.transform, "ResetButton"), "标准", fontAsset, new Vector2(150f, 68f), new Vector2(0f, -66f));
        var increase = ConfigureButton(GetOrCreateChild(root.transform, "IncreaseButton"), "+", fontAsset, new Vector2(104f, 68f), new Vector2(166f, -66f));
        SetButtonVisible(decrease, false, new Vector2(104f, 68f), new Vector2(-166f, -66f));
        SetButtonVisible(reset, false, new Vector2(150f, 68f), new Vector2(0f, -66f));
        SetButtonVisible(increase, false, new Vector2(104f, 68f), new Vector2(166f, -66f));

        var controller = root.GetComponent<PingPongDifficultyController>();
        if (controller == null)
        {
            controller = root.AddComponent<PingPongDifficultyController>();
        }

        controller.ballSpawner = spawner;
        controller.difficultyText = difficulty;
        controller.speedText = speed;
        controller.hintText = hint;
        controller.decreaseButton = null;
        controller.resetButton = null;
        controller.increaseButton = null;
        controller.startingDifficulty = PingPongDifficulty.Normal;
        controller.controlServeInterval = true;
        controller.showScreenButtons = false;
        controller.enableControllerSpeedButtons = true;
        controller.createVisiblePanel = true;
        controller.enhancePanelReadability = true;
        controller.RebindButtons();
        controller.ApplyLoadedDifficulty();
        controller.ApplyReadabilityLayout();

        var motion = root.GetComponent<TechModuleCardMotion>();
        if (motion == null)
        {
            motion = root.AddComponent<TechModuleCardMotion>();
        }

        motion.cardTransform = rootRect;
        motion.canvasGroup = EnsureCanvasGroup(root);
        motion.cardGraphic = background;
        motion.glowGraphic = glow;
        motion.normalColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.22f), 1f);
        motion.hoverColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 1f);
        motion.pressedColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.94f);
        motion.glowColor = WithAlpha(ElderCareUiTheme.Cyan, 0.1f);
        motion.hoverScale = 1.012f;
        motion.pressedScale = 0.99f;
        motion.entranceDelay = 0.18f;

        if (title != null)
        {
            title.raycastTarget = false;
        }

        EnsureDifficultyPanelDragHandle(rootRect, background);
        return controller;
    }

    private void Awake()
    {
        if (ballSpawner == null)
        {
            ballSpawner = FindObjectOfType<BallSpawner>();
        }

        WireButtons();
        ApplyReadabilityLayout();
    }

    private void Start()
    {
        _difficulty = LoadDifficulty();
        ApplyDifficulty(_difficulty, false);
        ApplyReadabilityLayout();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void Update()
    {
        HandleControllerSpeedButtons();
    }

    public void IncreaseDifficulty()
    {
        SetDifficulty((PingPongDifficulty)Mathf.Min((int)_difficulty + 1, (int)PingPongDifficulty.Challenge));
    }

    public void DecreaseDifficulty()
    {
        SetDifficulty((PingPongDifficulty)Mathf.Max((int)_difficulty - 1, (int)PingPongDifficulty.Easy));
    }

    public void ResetDifficulty()
    {
        SetDifficulty(PingPongDifficulty.Normal);
    }

    public void SetDifficulty(PingPongDifficulty difficulty)
    {
        ApplyDifficulty(difficulty, true);
    }

    public void SetCustomSpeed(float speed)
    {
        customSpeed = Mathf.Clamp(speed, 1.5f, 5.0f);
        ApplyDifficulty(PingPongDifficulty.Custom, true);
    }

    public void RebindButtons()
    {
        WireButtons();
    }

    public void ApplyLoadedDifficulty()
    {
        _difficulty = LoadDifficulty();
        ApplyDifficulty(_difficulty, false);
        ApplyReadabilityLayout();
    }

    public void ApplyReadabilityLayout()
    {
        if (!createVisiblePanel) return;
        if (!enhancePanelReadability) return;

        var rootRect = transform as RectTransform;
        if (rootRect == null) return;

        ConfigureRect(gameObject, ReadablePanelSize, ReadablePanelPosition);
        var background = ConfigurePanel(GetOrCreateChild(transform, "Background"), ReadablePanelSize, Vector2.zero, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.22f), 1f), 26f, false);
        var glow = ConfigurePanel(GetOrCreateChild(transform, "Glow"), new Vector2(584f, 286f), Vector2.zero, WithAlpha(ElderCareUiTheme.Cyan, 0.05f), 30f, false);
        if (glow != null)
        {
            glow.transform.SetAsFirstSibling();
        }

        ConfigurePanel(GetOrCreateChild(transform, "TopTrace"), new Vector2(420f, 3f), new Vector2(0f, 104f), WithAlpha(ElderCareUiTheme.Cyan, 0.22f), 2f, false);
        ConfigurePanel(GetOrCreateChild(transform, "BottomTrace"), new Vector2(320f, 2f), new Vector2(0f, -106f), WithAlpha(ElderCareUiTheme.Blue, 0.16f), 2f, false);

        ConfigureExistingText(FindPanelText("Title"), "发球速度", new Vector2(480f, 44f), new Vector2(0f, 72f), ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        ConfigureExistingText(ResolveText(ref difficultyText, "DifficultyText"), null, new Vector2(480f, 42f), new Vector2(0f, 24f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.Cyan);
        ConfigureExistingText(ResolveText(ref speedText, "SpeedText"), null, new Vector2(480f, 44f), new Vector2(0f, -20f), ElderCareUiTheme.Body, FontStyles.Bold, ElderCareUiTheme.TextPrimary);
        ConfigureExistingText(ResolveText(ref hintText, "HintText"), null, new Vector2(500f, 40f), new Vector2(0f, -82f), ElderCareUiTheme.BodySmall, FontStyles.Normal, ElderCareUiTheme.TextSecondary);

        SetButtonVisible(ResolveButton(ref decreaseButton, "DecreaseButton"), showScreenButtons, new Vector2(104f, 68f), new Vector2(-166f, -66f));
        SetButtonVisible(ResolveButton(ref resetButton, "ResetButton"), showScreenButtons, new Vector2(150f, 68f), new Vector2(0f, -66f));
        SetButtonVisible(ResolveButton(ref increaseButton, "IncreaseButton"), showScreenButtons, new Vector2(104f, 68f), new Vector2(166f, -66f));

        var motion = GetComponent<TechModuleCardMotion>();
        if (motion != null)
        {
            motion.cardTransform = rootRect;
            motion.canvasGroup = EnsureCanvasGroup(gameObject);
            motion.cardGraphic = background;
            motion.glowGraphic = glow;
            motion.normalColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Blue, 0.22f), 1f);
            motion.hoverColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 1f);
            motion.pressedColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.94f);
            motion.glowColor = WithAlpha(ElderCareUiTheme.Cyan, 0.1f);
            motion.hoverScale = 1.012f;
            motion.pressedScale = 0.99f;
        }

        EnsureDifficultyPanelDragHandle(rootRect, background);
    }

    public static bool RepairPanelDrag(GameObject panel)
    {
        if (panel == null) return false;

        var rootRect = panel.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = panel.AddComponent<RectTransform>();
        }

        var backgroundTransform = panel.transform.Find("Background");
        var background = backgroundTransform != null ? backgroundTransform.GetComponent<Graphic>() : null;
        EnsureDifficultyPanelDragHandle(rootRect, background);
        return true;
    }

    public static string GetLabel(PingPongDifficulty difficulty)
    {
        switch (difficulty)
        {
            case PingPongDifficulty.Easy:
                return "轻松";
            case PingPongDifficulty.Advanced:
                return "进阶";
            case PingPongDifficulty.Challenge:
                return "挑战";
            case PingPongDifficulty.Custom:
                return "自定义";
            default:
                return "标准";
        }
    }

    public static float GetSpeed(PingPongDifficulty difficulty)
    {
        return GetPreset(difficulty).speed;
    }

    public static float GetServeInterval(PingPongDifficulty difficulty)
    {
        return GetPreset(difficulty).interval;
    }

    private void ApplyDifficulty(PingPongDifficulty difficulty, bool persist)
    {
        _difficulty = difficulty;
        var preset = difficulty == PingPongDifficulty.Custom
            ? new DifficultyPreset(GetLabel(difficulty), customSpeed, 4.0f)
            : GetPreset(difficulty);

        if (ballSpawner != null)
        {
            ballSpawner.serveSpeed = preset.speed;
            if (controlServeInterval)
            {
                ballSpawner.serveInterval = preset.interval;
            }
        }

        if (persist && rememberDifficulty && difficulty != PingPongDifficulty.Custom)
        {
            PlayerPrefs.SetInt(ResolvePrefsKey(), (int)difficulty);
            PlayerPrefs.Save();
        }

        RefreshText(preset);
        RefreshControllerHint(preset, persist);
    }

    private void RefreshText(DifficultyPreset preset)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"当前难度：{preset.label}";
        }

        if (speedText != null)
        {
            speedText.text = $"发球速度 {preset.speed:0.0} m/s";
        }
    }

    private void RefreshControllerHint(DifficultyPreset preset, bool changed)
    {
        if (hintText != null)
        {
            hintText.text = changed ? $"已切换到 {preset.label} · 下次发球生效" : ControllerHint;
        }
    }

    private PingPongDifficulty LoadDifficulty()
    {
        if (!rememberDifficulty) return startingDifficulty;

        var saved = PlayerPrefs.GetInt(ResolvePrefsKey(), (int)startingDifficulty);
        return (PingPongDifficulty)Mathf.Clamp(saved, (int)PingPongDifficulty.Easy, (int)PingPongDifficulty.Challenge);
    }

    private string ResolvePrefsKey()
    {
        return string.IsNullOrEmpty(playerPrefsKey) ? DefaultPrefsKey : playerPrefsKey;
    }

    private void WireButtons()
    {
        UnwireButtons();
        if (showScreenButtons && decreaseButton != null) decreaseButton.onClick.AddListener(DecreaseDifficulty);
        if (showScreenButtons && increaseButton != null) increaseButton.onClick.AddListener(IncreaseDifficulty);
        if (showScreenButtons && resetButton != null) resetButton.onClick.AddListener(ResetDifficulty);
        _buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!_buttonsWired) return;

        if (decreaseButton != null) decreaseButton.onClick.RemoveListener(DecreaseDifficulty);
        if (increaseButton != null) increaseButton.onClick.RemoveListener(IncreaseDifficulty);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetDifficulty);
        _buttonsWired = false;
    }

    private static DifficultyPreset GetPreset(PingPongDifficulty difficulty)
    {
        switch (difficulty)
        {
            case PingPongDifficulty.Easy:
                return new DifficultyPreset("轻松", 2.2f, 4.8f);
            case PingPongDifficulty.Advanced:
                return new DifficultyPreset("进阶", 3.6f, 3.8f);
            case PingPongDifficulty.Challenge:
                return new DifficultyPreset("挑战", 4.2f, 3.4f);
            default:
                return new DifficultyPreset("标准", 3.1f, 4.2f);
        }
    }

    private void HandleControllerSpeedButtons()
    {
        if (!enableControllerSpeedButtons) return;

        var acceleratePressed = ReadButton(controllerButtonNode, CommonUsages.primaryButton);
        var deceleratePressed = ReadButton(controllerButtonNode, CommonUsages.secondaryButton);

        if (acceleratePressed && !_wasAcceleratePressed)
        {
            IncreaseDifficulty();
        }

        if (deceleratePressed && !_wasDeceleratePressed)
        {
            DecreaseDifficulty();
        }

        _wasAcceleratePressed = acceleratePressed;
        _wasDeceleratePressed = deceleratePressed;
    }

    private bool ReadButton(XRNode node, InputFeatureUsage<bool> usage)
    {
        InputDevices.GetDevicesAtXRNode(node, _buttonDevices);
        for (var i = 0; i < _buttonDevices.Count; i++)
        {
            var device = _buttonDevices[i];
            if (device.isValid && device.TryGetFeatureValue(usage, out var pressed) && pressed)
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct DifficultyPreset
    {
        public readonly string label;
        public readonly float speed;
        public readonly float interval;

        public DifficultyPreset(string label, float speed, float interval)
        {
            this.label = label;
            this.speed = speed;
            this.interval = interval;
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

    private static Graphic ConfigurePanel(GameObject go, Vector2 size, Vector2 anchoredPosition, Color color, float radius, bool raycastTarget)
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

    private static void EnsureDifficultyPanelDragHandle(RectTransform rootRect, Graphic background)
    {
        if (rootRect == null) return;

        var canvas = rootRect.GetComponentInParent<Canvas>(true);
        var canvasTransform = canvas != null ? canvas.transform : rootRect.parent;
        var placer = canvasTransform != null ? canvasTransform.GetComponent<ComfortWorldSpaceUIPlacer>() : null;
        if (background != null)
        {
            var surfaceDrag = WorldSpaceUiRayDragHandle.EnsureOnSurface(background, rootRect, placer);
            if (surfaceDrag != null)
            {
                surfaceDrag.targetRoot = rootRect;
                surfaceDrag.lockWorldHeight = true;
            }
        }

        var handleObject = GetOrCreateChild(rootRect, "DragHandle");
        var handleRect = ConfigureRect(handleObject, new Vector2(420f, 34f), new Vector2(0f, 108f));
        handleRect.SetAsLastSibling();

        var roundedPanel = handleObject.GetComponent<ElderCareRoundedPanel>();
        if (roundedPanel != null)
        {
            DestroyComponent(roundedPanel);
        }

        var image = handleObject.GetComponent<Image>();
        if (image == null)
        {
            image = handleObject.AddComponent<Image>();
        }

        image.raycastTarget = true;
        image.color = new Color(0.35f, 0.95f, 1f, 0.35f);

        var outline = handleObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = handleObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.68f, 1f, 1f, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);

        var drag = handleObject.GetComponent<WorldSpaceUiRayDragHandle>();
        if (drag == null)
        {
            drag = handleObject.AddComponent<WorldSpaceUiRayDragHandle>();
        }

        drag.targetRoot = rootRect;
        drag.placer = placer;
        drag.headTransform = placer != null ? placer.headTransform : (Camera.main != null ? Camera.main.transform : null);
        drag.handleGraphic = image;
        drag.normalColor = image.color;
        drag.activeColor = new Color(0.68f, 1f, 1f, 0.76f);
        drag.minDistanceMeters = 0.9f;
        drag.maxDistanceMeters = 3.8f;
        drag.lockWorldHeight = true;
        drag.lockHeightToComfortOffset = true;
    }

    private static TMP_Text ConfigureText(GameObject go, string value, TMP_FontAsset fontAsset, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style, Color color)
    {
        ConfigureRect(go, size, anchoredPosition);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = go.AddComponent<TextMeshProUGUI>();
        }

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text ResolveText(ref TMP_Text field, string childName)
    {
        if (field == null)
        {
            field = FindPanelText(childName);
        }

        return field;
    }

    private Button ResolveButton(ref Button field, string childName)
    {
        if (field == null)
        {
            var child = FindChild(transform, childName);
            field = child != null ? child.GetComponent<Button>() : null;
        }

        return field;
    }

    private TMP_Text FindPanelText(string childName)
    {
        var child = FindChild(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void ConfigureExistingText(TMP_Text text, string value, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style, Color color)
    {
        if (text == null) return;

        ConfigureRect(text.gameObject, size, anchoredPosition);
        if (!string.IsNullOrEmpty(value))
        {
            text.text = value;
        }

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    private static void ConfigureExistingButton(Button button, Vector2 size, Vector2 anchoredPosition)
    {
        if (button == null) return;

        ConfigureRect(button.gameObject, size, anchoredPosition);
        var graphic = ConfigurePanel(button.gameObject, size, anchoredPosition, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 0.9f), 20f, true);
        button.targetGraphic = graphic;
    }

    private static void SetButtonVisible(Button button, bool visible, Vector2 size, Vector2 anchoredPosition)
    {
        if (button == null) return;

        ConfigureExistingButton(button, size, anchoredPosition);
        button.interactable = visible;
        button.gameObject.SetActive(visible);
    }

    private static Button ConfigureButton(GameObject go, string label, TMP_FontAsset fontAsset, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = ConfigureRect(go, size, anchoredPosition);
        var graphic = ConfigurePanel(go, size, anchoredPosition, WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 0.9f), 20f, true);

        var outline = go.GetComponent<Outline>();
        if (outline == null)
        {
            outline = go.AddComponent<Outline>();
        }

        outline.effectColor = WithAlpha(ElderCareUiTheme.Cyan, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);

        var button = go.GetComponent<Button>();
        if (button == null)
        {
            button = go.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;

        ConfigureText(GetOrCreateChild(rect, "Label"), label, fontAsset, size, Vector2.zero, label.Length > 1 ? ElderCareUiTheme.BodySmall : ElderCareUiTheme.Subtitle, FontStyles.Bold, ElderCareUiTheme.TextPrimary);

        var motion = go.GetComponent<TechModuleCardMotion>();
        if (motion == null)
        {
            motion = go.AddComponent<TechModuleCardMotion>();
        }

        motion.cardTransform = rect;
        motion.canvasGroup = EnsureCanvasGroup(go);
        motion.cardGraphic = graphic;
        motion.normalColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.22f), 0.9f);
        motion.hoverColor = WithAlpha(Color.Lerp(ElderCareUiTheme.PanelStrong, ElderCareUiTheme.Cyan, 0.34f), 0.94f);
        motion.pressedColor = WithAlpha(ElderCareUiTheme.PanelStrong, 0.96f);
        motion.glowColor = WithAlpha(ElderCareUiTheme.Cyan, 0.12f);
        motion.hoverScale = ElderCareUiTheme.HoverScale;
        motion.pressedScale = ElderCareUiTheme.PressedScale;
        motion.playEntrance = false;
        return button;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var canvasGroup = go.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : go.AddComponent<CanvasGroup>();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
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

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        var child = FindChild(parent, name);
        if (child != null) return child;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null) return null;

        var child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }
}
