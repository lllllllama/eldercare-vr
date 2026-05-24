using PicoElderCare.Rehab;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusRehabTrainingPanelController : MonoBehaviour
    {
        public RehabSessionManager sessionManager;
        public MovementEvaluator movementEvaluator;
        public RehabUIController uiControllerAdapter;
        public Button returnButton;
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public TMP_Text countdownText;
        public TMP_Text progressText;
        public TMP_Text safetyText;
        public TMP_Text devInfoText;
        public Image progressFillImage;
        public GameObject rehabSelectPanel;
        public string mainEntrySceneName = "00_MainEntry_BPlus";
        public bool returnBySceneLoad;
        public float fallbackDurationSeconds = 258f;

        private float _fallbackRemainingSeconds;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
            _fallbackRemainingSeconds = fallbackDurationSeconds;
        }

        private void OnEnable()
        {
            ResolveReferences();
            Refresh();
        }

        private void Update()
        {
            if (sessionManager == null || !sessionManager.IsSessionActive)
            {
                _fallbackRemainingSeconds = Mathf.Max(0f, _fallbackRemainingSeconds - Time.deltaTime);
            }

            Refresh();
        }

        public void ReturnToRehabSelect()
        {
            ResolveReferences();
            if (sessionManager != null)
            {
                sessionManager.CancelCurrentTraining();
            }

            if (returnBySceneLoad)
            {
                SceneManager.LoadScene(mainEntrySceneName);
                return;
            }

            if (rehabSelectPanel != null)
            {
                rehabSelectPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        public void Refresh()
        {
            ResolveReferences();
            var movement = movementEvaluator != null ? movementEvaluator.CurrentMovement : null;
            var movementName = movement != null ? movement.movementName : movementEvaluator != null ? movementEvaluator.movementName : "八段锦：双手托天理三焦";
            var progress = movementEvaluator != null ? Mathf.Clamp01(movementEvaluator.CurrentCompletion) : 0.38f;
            var remainingText = ResolveRemainingText();
            var safety = ResolveSafetyText();

            BPlusUiRuntimeUtility.SetText(titleText, movementName);
            BPlusUiRuntimeUtility.SetText(descriptionText, "请保持舒适幅度，跟随提示完成动作。");
            BPlusUiRuntimeUtility.SetText(countdownText, remainingText);
            BPlusUiRuntimeUtility.SetText(progressText, Mathf.RoundToInt(progress * 100f) + "%");
            BPlusUiRuntimeUtility.SetText(safetyText, safety);
            BPlusUiRuntimeUtility.SetText(devInfoText, sessionManager != null && sessionManager.IsTrainingActive ? "开发信息：正在读取现有康复训练状态" : "开发信息：等待康复训练数据");

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = progress;
                progressFillImage.color = ResolveProgressColor(progress);
            }
        }

        private void ResolveReferences()
        {
            if (sessionManager == null) sessionManager = FindObjectOfType<RehabSessionManager>(true);
            if (movementEvaluator == null) movementEvaluator = FindObjectOfType<MovementEvaluator>(true);
            if (uiControllerAdapter == null) uiControllerAdapter = FindObjectOfType<RehabUIController>(true);
            if (returnButton == null) returnButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Return");
            if (titleText == null) titleText = BPlusUiRuntimeUtility.FindText(transform, "Title");
            if (descriptionText == null) descriptionText = BPlusUiRuntimeUtility.FindText(transform, "Description");
            if (countdownText == null) countdownText = BPlusUiRuntimeUtility.FindText(BPlusUiRuntimeUtility.FindTransform(transform, "CountdownCard"), "Value");
            if (progressText == null) progressText = BPlusUiRuntimeUtility.FindText(transform, "Percent");
            if (safetyText == null) safetyText = BPlusUiRuntimeUtility.FindText(transform, "SafetyHint/Text");
            if (devInfoText == null) devInfoText = BPlusUiRuntimeUtility.FindText(transform, "DevInfo");
            if (progressFillImage == null)
            {
                var fill = BPlusUiRuntimeUtility.FindTransform(transform, "RingFill");
                if (fill != null) progressFillImage = fill.GetComponent<Image>();
            }
        }

        private void BindButtons()
        {
            BPlusUiRuntimeUtility.Bind(returnButton, ReturnToRehabSelect);
        }

        private string ResolveRemainingText()
        {
            if (sessionManager != null && sessionManager.timerText != null && !string.IsNullOrWhiteSpace(sessionManager.timerText.text))
            {
                var text = sessionManager.timerText.text;
                var lastSpace = text.LastIndexOf(' ');
                return lastSpace >= 0 && lastSpace < text.Length - 1 ? text.Substring(lastSpace + 1) : text;
            }

            if (uiControllerAdapter != null && uiControllerAdapter.remainingTimeText != null && !string.IsNullOrWhiteSpace(uiControllerAdapter.remainingTimeText.text))
            {
                var text = uiControllerAdapter.remainingTimeText.text.Replace("剩余", string.Empty).Trim();
                return string.IsNullOrEmpty(text) ? BPlusUiRuntimeUtility.FormatClock(_fallbackRemainingSeconds) : text;
            }

            return BPlusUiRuntimeUtility.FormatClock(_fallbackRemainingSeconds);
        }

        private string ResolveSafetyText()
        {
            if (uiControllerAdapter != null && uiControllerAdapter.safetyPromptText != null && !string.IsNullOrWhiteSpace(uiControllerAdapter.safetyPromptText.text))
            {
                return uiControllerAdapter.safetyPromptText.text;
            }

            return "保持呼吸平稳，肩部不要用力过猛";
        }

        private static Color ResolveProgressColor(float progress)
        {
            if (progress >= 0.7f) return ElderCareUiTheme.StatusWarn;
            if (progress >= 0.3f) return ElderCareUiTheme.RehabButton;
            return ElderCareUiTheme.PingPongButton;
        }
    }
}
