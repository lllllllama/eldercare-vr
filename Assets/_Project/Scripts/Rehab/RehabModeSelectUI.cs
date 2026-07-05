using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class RehabModeSelectUI : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject rehabTrainingSelectPanel;
        public GameObject rehabTrainingPanel;
        public GameObject trainingResultPanel;

        public Button rehabButton;
        public Button baduanjinButton;
        public Button taiChiButton;
        public Button backButton;
        public Button trainingBackButton;
        public Button resultBackButton;
        public ModuleHomeMenu homeMenu;
        public ComfortWorldSpaceUIPlacer uiPlacer;
        public RehabPanelPlacementController panelPlacementController;

        public RehabSessionManager sessionManager;
        public RehabVideoGuideController videoGuideController;
        public bool showTrainingSelectOnStart = true;
        public bool placeUiOnStart = true;
        public bool placeUiOnMainMenuOpen = true;
        public bool placeUiOnTrainingSelectOpen = true;
        public int startRecenterDelayFrames = 2;
        public float startRecenterSeconds = 1.25f;
        public int startRecenterFrames = 18;
        public float trainingSelectDistanceMeters = 2.45f;
        public float trainingSelectHeightOffsetMeters = 0.08f;

        private Coroutine _startRecenterCoroutine;

        private void Awake()
        {
            ResolveReferences();
            BindButtonEvents();
        }

        private void Start()
        {
            if (showTrainingSelectOnStart)
            {
                ShowTrainingSelectPanel();
            }
            else
            {
                ShowMainMenuPanel();
            }

            if (placeUiOnStart)
            {
                RecenterNavigationPanels();
                ScheduleStartRecenterNavigationPanels();
            }
        }

        private void OnDisable()
        {
            if (_startRecenterCoroutine != null)
            {
                StopCoroutine(_startRecenterCoroutine);
                _startRecenterCoroutine = null;
            }
        }

        public void ShowMainMenuPanel()
        {
            CancelCurrentTrainingAndHideVideo();

            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, false);

            if (placeUiOnMainMenuOpen)
            {
                RecenterNavigationPanels();
            }
        }

        public void ShowTrainingSelectPanel()
        {
            CancelCurrentTrainingAndHideVideo();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, true);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, false);

            if (placeUiOnTrainingSelectOpen)
            {
                RecenterNavigationPanels();
            }
        }

        public void StartBaduanjinTraining()
        {
            StartTraining(RehabTrainingType.Baduanjin);
        }

        public void StartTaiChiTraining()
        {
            StartTraining(RehabTrainingType.TaiChi);
        }

        public void ShowTrainingResultPanel()
        {
            StopVideoGuideOnly();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, true);

            RecenterNavigationPanels();
        }

        public void ReturnToMainEntry()
        {
            ResolveReferences();
            CancelCurrentTrainingAndHideVideo();

            if (homeMenu != null)
            {
                homeMenu.LoadMainEntry();
                return;
            }

            ShowMainMenuPanel();
        }

        public void ResetUiPosition()
        {
            RecenterNavigationPanels();
        }

        private void StartTraining(RehabTrainingType trainingType)
        {
            ResolveReferences();
            CancelCurrentTrainingAndHideVideo();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, true);
            SetPanelActive(trainingResultPanel, false);

            if (sessionManager != null)
            {
                sessionManager.StartTraining(trainingType);
            }
            else
            {
                Debug.LogError("Cannot start rehab training because RehabSessionManager is not assigned.");
            }
        }

        private void ResolveReferences()
        {
            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }

            if (videoGuideController == null)
            {
                videoGuideController = FindObjectOfType<RehabVideoGuideController>(true);
            }

            if (homeMenu == null)
            {
                homeMenu = FindObjectOfType<ModuleHomeMenu>(true);
            }

            if (panelPlacementController == null && sessionManager != null)
            {
                panelPlacementController = sessionManager.panelPlacementController;
            }

            if (panelPlacementController == null)
            {
                panelPlacementController = FindObjectOfType<RehabPanelPlacementController>(true);
            }

            if (panelPlacementController != null && panelPlacementController.promptPanelRoot == null)
            {
                panelPlacementController.promptPanelRoot = transform;
            }

            if (uiPlacer == null)
            {
                uiPlacer = GetComponentInParent<ComfortWorldSpaceUIPlacer>();
            }

            if (trainingBackButton == null && rehabTrainingPanel != null)
            {
                var buttons = rehabTrainingPanel.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++)
                {
                    var buttonName = buttons[i].name;
                    if (buttonName == "HomeButton" ||
                        buttonName.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        trainingBackButton = buttons[i];
                        break;
                    }
                }
            }

            if (resultBackButton == null && trainingResultPanel != null)
            {
                var buttons = trainingResultPanel.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++)
                {
                    var buttonName = buttons[i].name;
                    if (buttonName == "BackButton" ||
                        buttonName.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        resultBackButton = buttons[i];
                        break;
                    }
                }
            }
        }

        private void RecenterNavigationPanels()
        {
            ResolveReferences();

            if (panelPlacementController != null)
            {
                panelPlacementController.RecenterPanels();
                return;
            }

            if (uiPlacer != null)
            {
                ApplyTrainingSelectPlacementDefaults();
                uiPlacer.EnsureWorldSpaceInteractionHelpers();
                uiPlacer.PlaceInFrontOfUser();
            }
        }

        private void ScheduleStartRecenterNavigationPanels()
        {
            if (!isActiveAndEnabled) return;

            if (_startRecenterCoroutine != null)
            {
                StopCoroutine(_startRecenterCoroutine);
            }

            _startRecenterCoroutine = StartCoroutine(RecenterNavigationPanelsAfterStartDelay());
        }

        private IEnumerator RecenterNavigationPanelsAfterStartDelay()
        {
            var delayFrames = Mathf.Max(0, startRecenterDelayFrames);
            for (var i = 0; i < delayFrames; i++)
            {
                yield return null;
            }

            var recenterUntilTime = Time.unscaledTime + Mathf.Max(0f, startRecenterSeconds);
            var recenterFramesRemaining = Mathf.Max(1, startRecenterFrames);
            while (isActiveAndEnabled)
            {
                RecenterNavigationPanels();
                recenterFramesRemaining--;

                var stillWithinTime = Time.unscaledTime <= recenterUntilTime;
                var stillWithinFrames = recenterFramesRemaining > 0;
                if (!stillWithinTime && !stillWithinFrames)
                {
                    break;
                }

                yield return null;
            }

            _startRecenterCoroutine = null;
        }

        private void ApplyTrainingSelectPlacementDefaults()
        {
            if (uiPlacer == null) return;

            uiPlacer.distanceMeters = Mathf.Max(uiPlacer.distanceMeters, trainingSelectDistanceMeters);
            uiPlacer.hmdHeightOffsetMeters = Mathf.Max(uiPlacer.hmdHeightOffsetMeters, trainingSelectHeightOffsetMeters);
            uiPlacer.enableRayDrag = true;
            uiPlacer.enableThumbstickNavigation = true;
        }

        private void BindButtonEvents()
        {
            if (rehabButton != null)
            {
                rehabButton.onClick.RemoveListener(ShowTrainingSelectPanel);
                rehabButton.onClick.AddListener(ShowTrainingSelectPanel);
            }

            if (baduanjinButton != null)
            {
                baduanjinButton.onClick.RemoveListener(StartBaduanjinTraining);
                baduanjinButton.onClick.AddListener(StartBaduanjinTraining);
            }

            if (taiChiButton != null)
            {
                taiChiButton.onClick.RemoveListener(StartTaiChiTraining);
                taiChiButton.onClick.AddListener(StartTaiChiTraining);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenuPanel);
                backButton.onClick.RemoveListener(ReturnToMainEntry);
                backButton.onClick.AddListener(ReturnToMainEntry);
            }

            if (trainingBackButton != null)
            {
                trainingBackButton.onClick.RemoveListener(ShowTrainingSelectPanel);
                trainingBackButton.onClick.AddListener(ShowTrainingSelectPanel);
            }

            if (resultBackButton != null)
            {
                resultBackButton.onClick = new Button.ButtonClickedEvent();
                resultBackButton.onClick.AddListener(ShowTrainingSelectPanel);
            }
        }

        private void CancelCurrentTrainingAndHideVideo()
        {
            ResolveReferences();

            if (sessionManager != null)
            {
                sessionManager.CancelCurrentTraining();
                return;
            }

            StopVideoGuideOnly();
        }

        private void StopVideoGuideOnly()
        {
            ResolveReferences();

            if (videoGuideController != null)
            {
                videoGuideController.StopAndHide();
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}
