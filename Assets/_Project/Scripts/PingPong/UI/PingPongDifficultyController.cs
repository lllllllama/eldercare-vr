using System.Collections.Generic;
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
    private static readonly Vector2 ReadablePanelSize = new Vector2(560f, 280f);
    private static readonly Vector2 ReadablePanelPosition = new Vector2(630f, 148f);

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

    [Range(1.5f, 5.0f)] public float customSpeed = 3.0f;
    public bool controlServeInterval = true;
    public bool enhancePanelReadability = true;
    public bool showScreenButtons = false;
    public bool enableControllerSpeedButtons = true;
    public XRNode controllerButtonNode = XRNode.RightHand;

    private readonly List<InputDevice> _buttonDevices = new List<InputDevice>();
    private PingPongDifficulty _difficulty;
    private bool _buttonsWired;
    private bool _wasAcceleratePressed;
    private bool _wasDeceleratePressed;

    public PingPongDifficulty CurrentDifficulty => _difficulty;
    public float CurrentSpeed => ballSpawner != null ? ballSpawner.serveSpeed : GetPreset(startingDifficulty).speed;

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
        var background = ConfigureImage(GetOrCreateChild(root.transform, "Background"), ReadablePanelSize, Vector2.zero, new Color(0.015f, 0.04f, 0.07f, 0.94f));
        var glow = ConfigureImage(GetOrCreateChild(root.transform, "Glow"), new Vector2(590f, 310f), Vector2.zero, new Color(0.2f, 0.82f, 1f, 0.1f));
        if (glow != null) glow.transform.SetAsFirstSibling();
        ConfigureImage(GetOrCreateChild(root.transform, "TopScanLine"), new Vector2(486f, 4f), new Vector2(0f, 112f), new Color(0.42f, 0.92f, 1f, 0.72f));

        var title = ConfigureText(GetOrCreateChild(root.transform, "Title"), "发球速度", fontAsset, new Vector2(480f, 54f), new Vector2(0f, 116f), 34f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.98f));
        var difficulty = ConfigureText(GetOrCreateChild(root.transform, "DifficultyText"), "难度：标准", fontAsset, new Vector2(480f, 48f), new Vector2(0f, 66f), 28f, FontStyles.Bold, new Color(0.62f, 0.96f, 1f, 0.98f));
        var speed = ConfigureText(GetOrCreateChild(root.transform, "SpeedText"), "速度 3.0 m/s", fontAsset, new Vector2(480f, 46f), new Vector2(0f, 20f), 26f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.94f));
        var hint = ConfigureText(GetOrCreateChild(root.transform, "HintText"), "使用 +/- 调节下一次发球速度", fontAsset, new Vector2(500f, 44f), new Vector2(0f, -126f), 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.78f));

        var decrease = ConfigureButton(GetOrCreateChild(root.transform, "DecreaseButton"), "-", fontAsset, new Vector2(104f, 68f), new Vector2(-166f, -66f));
        var reset = ConfigureButton(GetOrCreateChild(root.transform, "ResetButton"), "标准", fontAsset, new Vector2(150f, 68f), new Vector2(0f, -66f));
        var increase = ConfigureButton(GetOrCreateChild(root.transform, "IncreaseButton"), "+", fontAsset, new Vector2(104f, 68f), new Vector2(166f, -66f));
        ConfigureText(title.gameObject, "发球速度", fontAsset, new Vector2(480f, 54f), new Vector2(0f, 86f), 34f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.98f));
        ConfigureText(difficulty.gameObject, "难度：标准", fontAsset, new Vector2(480f, 48f), new Vector2(0f, 36f), 28f, FontStyles.Bold, new Color(0.62f, 0.96f, 1f, 0.98f));
        ConfigureText(speed.gameObject, "速度 3.0 m/s", fontAsset, new Vector2(480f, 46f), new Vector2(0f, -8f), 26f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.94f));
        ConfigureText(hint.gameObject, "A 加速 / B 减速", fontAsset, new Vector2(500f, 44f), new Vector2(0f, -86f), 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.78f));
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
        motion.normalColor = new Color(0.025f, 0.055f, 0.095f, 0.88f);
        motion.hoverColor = new Color(0.035f, 0.085f, 0.14f, 0.94f);
        motion.pressedColor = new Color(0.02f, 0.045f, 0.08f, 0.94f);
        motion.glowColor = new Color(0.25f, 0.9f, 1f, 0.18f);
        motion.hoverScale = 1.015f;
        motion.pressedScale = 0.99f;
        motion.entranceDelay = 0.22f;

        if (title != null)
        {
            title.raycastTarget = false;
        }

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
        if (!enhancePanelReadability) return;

        var rootRect = transform as RectTransform;
        if (rootRect == null) return;

        ConfigureRect(gameObject, ReadablePanelSize, ReadablePanelPosition);
        var background = ConfigureImage(GetOrCreateChild(transform, "Background"), ReadablePanelSize, Vector2.zero, new Color(0.015f, 0.04f, 0.07f, 0.94f));
        var glow = ConfigureImage(GetOrCreateChild(transform, "Glow"), new Vector2(590f, 310f), Vector2.zero, new Color(0.2f, 0.82f, 1f, 0.1f));
        if (glow != null)
        {
            glow.transform.SetAsFirstSibling();
        }

        ConfigureImage(GetOrCreateChild(transform, "TopScanLine"), new Vector2(486f, 4f), new Vector2(0f, 112f), new Color(0.42f, 0.92f, 1f, 0.72f));

        ConfigureExistingText(FindPanelText("Title"), new Vector2(480f, 54f), new Vector2(0f, 86f), 34f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.98f));
        ConfigureExistingText(ResolveText(ref difficultyText, "DifficultyText"), new Vector2(480f, 48f), new Vector2(0f, 36f), 28f, FontStyles.Bold, new Color(0.62f, 0.96f, 1f, 0.98f));
        ConfigureExistingText(ResolveText(ref speedText, "SpeedText"), new Vector2(480f, 46f), new Vector2(0f, -8f), 26f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.94f));
        ConfigureExistingText(ResolveText(ref hintText, "HintText"), new Vector2(500f, 44f), new Vector2(0f, -86f), 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.78f));

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
            motion.normalColor = new Color(0.015f, 0.04f, 0.07f, 0.94f);
            motion.hoverColor = new Color(0.03f, 0.08f, 0.13f, 0.98f);
            motion.pressedColor = new Color(0.01f, 0.035f, 0.06f, 0.98f);
            motion.glowColor = new Color(0.25f, 0.9f, 1f, 0.2f);
        }
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

        RefreshText(preset, persist);
        RefreshControllerHint(preset, persist);
    }

    private void RefreshText(DifficultyPreset preset, bool changed)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"难度：{preset.label}";
        }

        if (speedText != null)
        {
            speedText.text = $"速度 {preset.speed:0.0} m/s";
        }

        if (hintText != null)
        {
            hintText.text = changed ? $"当前发球速度已切换到 {preset.label}" : "使用 +/- 调节下一次发球速度";
        }
    }

    private void RefreshControllerHint(DifficultyPreset preset, bool changed)
    {
        if (hintText != null)
        {
            hintText.text = changed ? $"当前发球速度：{preset.label}" : "A 加速 / B 减速";
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
                return new DifficultyPreset("标准", 3.0f, 4.2f);
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

    private static Image ConfigureImage(GameObject go, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        ConfigureRect(go, size, anchoredPosition);
        var image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = false;
        return image;
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

    private static void ConfigureExistingText(TMP_Text text, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style, Color color)
    {
        if (text == null) return;

        ConfigureRect(text.gameObject, size, anchoredPosition);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
        text.outlineWidth = Mathf.Max(text.outlineWidth, 0.12f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    private static void ConfigureExistingButton(Button button, Vector2 size, Vector2 anchoredPosition)
    {
        if (button == null) return;

        ConfigureRect(button.gameObject, size, anchoredPosition);
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.05f, 0.18f, 0.3f, 0.98f);
            image.raycastTarget = true;
        }
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
        var image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.color = new Color(0.06f, 0.18f, 0.28f, 0.96f);

        var outline = go.GetComponent<Outline>();
        if (outline == null)
        {
            outline = go.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.48f, 0.92f, 1f, 0.38f);
        outline.effectDistance = new Vector2(2f, -2f);

        var button = go.GetComponent<Button>();
        if (button == null)
        {
            button = go.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;

        ConfigureText(GetOrCreateChild(rect, "Label"), label, fontAsset, size, Vector2.zero, label.Length > 1 ? 24f : 36f, FontStyles.Bold, Color.white);

        var motion = go.GetComponent<TechModuleCardMotion>();
        if (motion == null)
        {
            motion = go.AddComponent<TechModuleCardMotion>();
        }

        motion.cardTransform = rect;
        motion.canvasGroup = EnsureCanvasGroup(go);
        motion.cardGraphic = image;
        motion.normalColor = new Color(0.06f, 0.18f, 0.28f, 0.96f);
        motion.hoverColor = new Color(0.11f, 0.31f, 0.45f, 0.98f);
        motion.pressedColor = new Color(0.04f, 0.13f, 0.22f, 0.98f);
        motion.hoverScale = 1.055f;
        motion.pressedScale = 0.94f;
        motion.playEntrance = false;
        return button;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var canvasGroup = go.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : go.AddComponent<CanvasGroup>();
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
