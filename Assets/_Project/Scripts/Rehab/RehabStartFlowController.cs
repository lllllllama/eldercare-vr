using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public enum RehabStartFlowState
    {
        Idle,
        WaitingForStart,
        StartPreparation,
        PreMovementCountdown,
        MovementRecovery
    }

    public class RehabStartFlowController : MonoBehaviour
    {
        public Button startButton;
        public TMP_Text startButtonLabel;
        public RehabUIController uiController;
        public Transform startButtonParent;
        public bool autoCreateStartButton = true;
        public float startPreparationDelaySeconds = 5f;
        public float preMovementCountdownSeconds = 3f;
        public float movementRecoveryDelaySeconds = 3f;

        private const string WaitingPrompt = "请站稳并准备开始";
        private const string PreparationPrompt = "请放好手柄，保持站稳";
        private const string InitialCountdownPromptFormat = "训练将在 {0} 秒后开始";
        private const string RecoveryPrompt = "本动作已完成，请自然呼吸，稍作休息";
        private const string NextMovementPrompt = "下一式即将开始";
        private const string PausedPrompt = "请回到训练圈内，训练已暂停";
        private const string ResumedPrompt = "已回到训练圈，准备继续";

        private RehabStartFlowState _state = RehabStartFlowState.Idle;
        private float _phaseRemainingSeconds;
        private bool _pausedOutOfArea;
        private bool _countdownForNextMovement;
        private float _resumeNoticeSecondsRemaining;
        private string _movementName;
        private string _nextMovementName;

        public event Action MovementReadyToStart;

        public RehabStartFlowState State
        {
            get { return _state; }
        }

        public bool IsPausedOutOfArea
        {
            get { return _pausedOutOfArea; }
        }

        public bool IsRunningTimedFlow
        {
            get
            {
                return _state == RehabStartFlowState.StartPreparation ||
                       _state == RehabStartFlowState.PreMovementCountdown ||
                       _state == RehabStartFlowState.MovementRecovery;
            }
        }

        public float RemainingSeconds
        {
            get { return Mathf.Max(0f, _phaseRemainingSeconds); }
        }

        public string DisplayMovementName
        {
            get
            {
                if (_state == RehabStartFlowState.PreMovementCountdown &&
                    _countdownForNextMovement &&
                    !string.IsNullOrEmpty(_nextMovementName))
                {
                    return _nextMovementName;
                }

                return _movementName;
            }
        }

        public string DisplayStatusMessage
        {
            get
            {
                if (_pausedOutOfArea) return PausedPrompt;
                if (_resumeNoticeSecondsRemaining > 0f) return ResumedPrompt;

                switch (_state)
                {
                    case RehabStartFlowState.WaitingForStart:
                        return WaitingPrompt;
                    case RehabStartFlowState.StartPreparation:
                        return PreparationPrompt;
                    case RehabStartFlowState.PreMovementCountdown:
                        return _countdownForNextMovement ? NextMovementPrompt : GetInitialCountdownPrompt();
                    case RehabStartFlowState.MovementRecovery:
                        return RecoveryPrompt;
                    default:
                        return string.Empty;
                }
            }
        }

        public string DisplaySafetyMessage
        {
            get { return DisplayStatusMessage; }
        }

        public string DisplayTimerText
        {
            get
            {
                if (_state == RehabStartFlowState.WaitingForStart)
                {
                    return "开始";
                }

                if (_state == RehabStartFlowState.PreMovementCountdown)
                {
                    return Mathf.Clamp(Mathf.CeilToInt(RemainingSeconds), 1, Mathf.CeilToInt(Mathf.Max(1f, preMovementCountdownSeconds))).ToString();
                }

                var remaining = RemainingSeconds;
                return string.Format("剩余 {0:00}:{1:00}", Mathf.FloorToInt(remaining / 60f), Mathf.FloorToInt(remaining % 60f));
            }
        }

        private string GetInitialCountdownPrompt()
        {
            var seconds = Mathf.CeilToInt(Mathf.Max(1f, preMovementCountdownSeconds));
            return string.Format(InitialCountdownPromptFormat, seconds);
        }

        private void Awake()
        {
            ResolveReferences();
            BindStartButton();
            SetStartButtonVisible(false);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            }
        }

        public void BeginWaitingForStart(string movementName)
        {
            ResolveReferences();
            BindStartButton();

            _movementName = movementName;
            _nextMovementName = string.Empty;
            _state = RehabStartFlowState.WaitingForStart;
            _phaseRemainingSeconds = 0f;
            _pausedOutOfArea = false;
            _countdownForNextMovement = false;
            _resumeNoticeSecondsRemaining = 0f;
            SetStartButtonVisible(true);
        }

        public void BeginMovementRecovery(string completedMovementName, string nextMovementName)
        {
            _movementName = completedMovementName;
            _nextMovementName = nextMovementName;
            _state = RehabStartFlowState.MovementRecovery;
            _phaseRemainingSeconds = Mathf.Max(0f, movementRecoveryDelaySeconds);
            _pausedOutOfArea = false;
            _countdownForNextMovement = true;
            _resumeNoticeSecondsRemaining = 0f;
            SetStartButtonVisible(false);

            if (_phaseRemainingSeconds <= 0f)
            {
                BeginCountdown(true);
            }
        }

        public void ResetFlow()
        {
            _state = RehabStartFlowState.Idle;
            _phaseRemainingSeconds = 0f;
            _pausedOutOfArea = false;
            _countdownForNextMovement = false;
            _resumeNoticeSecondsRemaining = 0f;
            _movementName = string.Empty;
            _nextMovementName = string.Empty;
            SetStartButtonVisible(false);
        }

        public void Tick(float deltaTime, bool userInsideTrainingArea)
        {
            if (_state == RehabStartFlowState.Idle ||
                _state == RehabStartFlowState.WaitingForStart)
            {
                return;
            }

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (!userInsideTrainingArea)
            {
                if (!_pausedOutOfArea && _state == RehabStartFlowState.PreMovementCountdown)
                {
                    _phaseRemainingSeconds = Mathf.Max(1f, preMovementCountdownSeconds);
                }

                _pausedOutOfArea = true;
                _resumeNoticeSecondsRemaining = 0f;
                return;
            }

            if (_pausedOutOfArea)
            {
                _pausedOutOfArea = false;
                _resumeNoticeSecondsRemaining = 1.1f;
            }
            else if (_resumeNoticeSecondsRemaining > 0f)
            {
                _resumeNoticeSecondsRemaining = Mathf.Max(0f, _resumeNoticeSecondsRemaining - safeDeltaTime);
            }

            _phaseRemainingSeconds = Mathf.Max(0f, _phaseRemainingSeconds - safeDeltaTime);

            if (_phaseRemainingSeconds > 0f)
            {
                return;
            }

            switch (_state)
            {
                case RehabStartFlowState.StartPreparation:
                    BeginCountdown(false);
                    break;
                case RehabStartFlowState.MovementRecovery:
                    BeginCountdown(true);
                    break;
                case RehabStartFlowState.PreMovementCountdown:
                    CompleteCountdown();
                    break;
            }
        }

        private void OnStartButtonClicked()
        {
            if (_state != RehabStartFlowState.WaitingForStart) return;

            _state = RehabStartFlowState.StartPreparation;
            _phaseRemainingSeconds = Mathf.Max(0f, startPreparationDelaySeconds);
            _pausedOutOfArea = false;
            _countdownForNextMovement = false;
            _resumeNoticeSecondsRemaining = 0f;
            SetStartButtonVisible(false);

            if (_phaseRemainingSeconds <= 0f)
            {
                BeginCountdown(false);
            }
        }

        private void BeginCountdown(bool nextMovement)
        {
            _state = RehabStartFlowState.PreMovementCountdown;
            _phaseRemainingSeconds = Mathf.Max(1f, preMovementCountdownSeconds);
            _pausedOutOfArea = false;
            _countdownForNextMovement = nextMovement;
            _resumeNoticeSecondsRemaining = 0f;
            SetStartButtonVisible(false);
        }

        private void CompleteCountdown()
        {
            _state = RehabStartFlowState.Idle;
            _phaseRemainingSeconds = 0f;
            _pausedOutOfArea = false;
            _resumeNoticeSecondsRemaining = 0f;
            SetStartButtonVisible(false);

            var handler = MovementReadyToStart;
            if (handler != null)
            {
                handler.Invoke();
            }
        }

        private void ResolveReferences()
        {
            if (uiController == null)
            {
                uiController = FindObjectOfType<RehabUIController>(true);
            }

            if (startButton == null && uiController != null)
            {
                startButton = uiController.startButton;
            }

            if (startButton == null)
            {
                startButton = FindStartButton();
            }

            if (startButtonParent == null && uiController != null)
            {
                if (uiController.safetyPromptText != null)
                {
                    startButtonParent = uiController.safetyPromptText.transform.parent;
                }
                else if (uiController.stepText != null)
                {
                    startButtonParent = uiController.stepText.transform.parent;
                }
            }

            if (startButton == null && autoCreateStartButton && startButtonParent != null)
            {
                startButton = CreateRuntimeStartButton(startButtonParent);
            }

            if (startButtonLabel == null && startButton != null)
            {
                startButtonLabel = startButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (uiController != null && uiController.startButton == null)
            {
                uiController.startButton = startButton;
            }
        }

        private void BindStartButton()
        {
            if (startButton == null) return;

            startButton.onClick.RemoveListener(OnStartButtonClicked);
            startButton.onClick.AddListener(OnStartButtonClicked);

            if (startButtonLabel == null)
            {
                startButtonLabel = startButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (startButtonLabel != null)
            {
                startButtonLabel.text = "开始";
            }
        }

        private Button FindStartButton()
        {
            var buttons = FindObjectsOfType<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "StartButton")
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private Button CreateRuntimeStartButton(Transform parent)
        {
            var go = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-232f, -174f);
            rect.sizeDelta = new Vector2(186f, 64f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.05f, 0.45f, 0.38f, 0.95f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(go.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            startButtonLabel = labelObject.GetComponent<TextMeshProUGUI>();
            startButtonLabel.text = "开始";
            startButtonLabel.fontSize = 34f;
            startButtonLabel.fontStyle = FontStyles.Bold;
            startButtonLabel.alignment = TextAlignmentOptions.Center;
            startButtonLabel.color = Color.white;
            startButtonLabel.enableAutoSizing = true;
            startButtonLabel.fontSizeMin = 22f;
            startButtonLabel.fontSizeMax = 30f;
            startButtonLabel.enableWordWrapping = false;
            startButtonLabel.overflowMode = TextOverflowModes.Ellipsis;
            startButtonLabel.margin = new Vector4(10f, 4f, 10f, 4f);
            startButtonLabel.raycastTarget = false;
            if (uiController != null)
            {
                var sourceText = uiController.stepText != null
                    ? uiController.stepText
                    : uiController.movementNameText;
                if (sourceText != null && sourceText.font != null)
                {
                    startButtonLabel.font = sourceText.font;
                }
            }

            return button;
        }

        private void SetStartButtonVisible(bool visible)
        {
            ResolveReferences();
            if (startButton != null && startButton.gameObject.activeSelf != visible)
            {
                startButton.gameObject.SetActive(visible);
            }
        }
    }
}
