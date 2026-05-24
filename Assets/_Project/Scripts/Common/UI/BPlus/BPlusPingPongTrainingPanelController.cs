using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusPingPongTrainingPanelController : MonoBehaviour
    {
        public BallSpawner ballSpawner;
        public PingPongDifficultyController difficultyController;
        public Button startButton;
        public Button returnButton;
        public Button increaseDifficultyButton;
        public Button decreaseDifficultyButton;
        public TMP_Text statusText;
        public TMP_Text accuracyText;
        public TMP_Text difficultyText;
        public TMP_Text hitsText;
        public TMP_Text durationText;
        public GameObject mainEntryRoot;
        public string mainEntrySceneName = "00_MainEntry_BPlus";
        public bool returnBySceneLoad;
        public bool clearBallsOnReturn = true;

        private int _servedCount;
        private int _hitCount;
        private bool _trainingActive;
        private float _startedAt;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        public void StartTraining()
        {
            ResolveReferences();
            _servedCount = 0;
            _hitCount = 0;
            _startedAt = Time.time;
            _trainingActive = true;

            if (ballSpawner != null)
            {
                ballSpawner.autoStartOnPlay = false;
                ballSpawner.ClearBalls();
                ballSpawner.StartServing();
            }
            else
            {
                Debug.LogWarning("B+ PingPong UI cannot start serving because BallSpawner is missing.", this);
            }

            RefreshAll();
        }

        public void StopTraining()
        {
            ResolveReferences();
            _trainingActive = false;
            if (ballSpawner != null)
            {
                ballSpawner.StopServing();
            }

            RefreshAll();
        }

        public void IncreaseDifficulty()
        {
            ResolveReferences();
            if (difficultyController != null)
            {
                difficultyController.IncreaseDifficulty();
            }

            RefreshAll();
        }

        public void DecreaseDifficulty()
        {
            ResolveReferences();
            if (difficultyController != null)
            {
                difficultyController.DecreaseDifficulty();
            }

            RefreshAll();
        }

        public void ReturnToMainEntry()
        {
            StopTraining();
            if (ballSpawner != null && clearBallsOnReturn)
            {
                ballSpawner.ClearBalls();
            }

            if (returnBySceneLoad)
            {
                SceneManager.LoadScene(mainEntrySceneName);
                return;
            }

            if (mainEntryRoot != null)
            {
                mainEntryRoot.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_trainingActive || (ballSpawner != null && ballSpawner.IsServing))
            {
                RefreshAll();
            }
        }

        private void ResolveReferences()
        {
            if (ballSpawner == null) ballSpawner = FindObjectOfType<BallSpawner>(true);
            if (difficultyController == null) difficultyController = FindObjectOfType<PingPongDifficultyController>(true);
            if (startButton == null) startButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Start");
            if (returnButton == null) returnButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Return");
            if (increaseDifficultyButton == null) increaseDifficultyButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_IncreaseDifficulty");
            if (decreaseDifficultyButton == null) decreaseDifficultyButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_DecreaseDifficulty");
            if (statusText == null) statusText = BPlusUiRuntimeUtility.FindText(transform, "StatusText");
            if (accuracyText == null) accuracyText = BPlusUiRuntimeUtility.FindText(transform, "Metric_Accuracy/Value");
            if (difficultyText == null) difficultyText = BPlusUiRuntimeUtility.FindText(BPlusUiRuntimeUtility.FindTransform(transform, "Metric_Difficulty"), "Value");
            if (hitsText == null) hitsText = BPlusUiRuntimeUtility.FindText(BPlusUiRuntimeUtility.FindTransform(transform, "Metric_Hits"), "Value");
            if (durationText == null) durationText = BPlusUiRuntimeUtility.FindText(BPlusUiRuntimeUtility.FindTransform(transform, "Metric_Time"), "Value");
        }

        private void BindButtons()
        {
            BPlusUiRuntimeUtility.Bind(startButton, StartTraining);
            BPlusUiRuntimeUtility.Bind(returnButton, ReturnToMainEntry);
            BPlusUiRuntimeUtility.Bind(increaseDifficultyButton, IncreaseDifficulty);
            BPlusUiRuntimeUtility.Bind(decreaseDifficultyButton, DecreaseDifficulty);
        }

        private void SubscribeEvents()
        {
            PingPongEvents.OnBallServed -= OnBallServed;
            PingPongEvents.OnBallHit -= OnBallHit;
            PingPongEvents.OnTrainingStarted -= OnTrainingStarted;
            PingPongEvents.OnTrainingFinished -= OnTrainingFinished;
            PingPongEvents.OnBallServed += OnBallServed;
            PingPongEvents.OnBallHit += OnBallHit;
            PingPongEvents.OnTrainingStarted += OnTrainingStarted;
            PingPongEvents.OnTrainingFinished += OnTrainingFinished;
        }

        private void UnsubscribeEvents()
        {
            PingPongEvents.OnBallServed -= OnBallServed;
            PingPongEvents.OnBallHit -= OnBallHit;
            PingPongEvents.OnTrainingStarted -= OnTrainingStarted;
            PingPongEvents.OnTrainingFinished -= OnTrainingFinished;
        }

        private void OnBallServed()
        {
            _servedCount++;
            RefreshAll();
        }

        private void OnBallHit()
        {
            _hitCount++;
            RefreshAll();
        }

        private void OnTrainingStarted()
        {
            _trainingActive = true;
            _startedAt = Time.time;
            RefreshAll();
        }

        private void OnTrainingFinished()
        {
            _trainingActive = false;
            RefreshAll();
        }

        private void RefreshAll()
        {
            var serving = _trainingActive || (ballSpawner != null && ballSpawner.IsServing);
            BPlusUiRuntimeUtility.SetText(statusText, serving ? "训练中" : "待开始");
            BPlusUiRuntimeUtility.SetText(accuracyText, FormatAccuracy());
            BPlusUiRuntimeUtility.SetText(difficultyText, difficultyController != null ? PingPongDifficultyController.GetLabel(difficultyController.CurrentDifficulty) : "标准");
            BPlusUiRuntimeUtility.SetText(hitsText, _hitCount.ToString());
            BPlusUiRuntimeUtility.SetText(durationText, serving ? BPlusUiRuntimeUtility.FormatClock(Time.time - _startedAt) : "00:00");
        }

        private string FormatAccuracy()
        {
            if (_servedCount <= 0) return "0%";
            return Mathf.RoundToInt((_hitCount / Mathf.Max(1f, _servedCount)) * 100f) + "%";
        }
    }
}
