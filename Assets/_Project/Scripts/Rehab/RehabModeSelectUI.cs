using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        public bool applyHtmlStylePanels = true;

        private Coroutine _startRecenterCoroutine;

        private void Awake()
        {
            ResolveReferences();
            ApplyHtmlStylePanelsIfNeeded();
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
            ApplyHtmlStylePanelsIfNeeded();

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
            ApplyHtmlStylePanelsIfNeeded();

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
            ApplyHtmlStylePanelsIfNeeded();

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
                ApplyHtmlStyleSkinOnly();
            }
            else
            {
                Debug.LogError("Cannot start rehab training because RehabSessionManager is not assigned.");
            }
        }

        private void ResolveReferences()
        {
            ResolvePanelReferences();

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

            ResolveNavigationButtons();
        }

        private void ResolvePanelReferences()
        {
            if (mainMenuPanel == null)
            {
                mainMenuPanel = FindChildGameObject(transform, "MainMenuPanel");
            }

            if (rehabTrainingSelectPanel == null)
            {
                rehabTrainingSelectPanel = FindChildGameObject(transform, "RehabTrainingSelectPanel");
            }

            if (rehabTrainingPanel == null)
            {
                rehabTrainingPanel = FindChildGameObject(transform, "RehabTrainingPanel");
            }

            if (trainingResultPanel == null)
            {
                trainingResultPanel = FindChildGameObject(transform, "TrainingResultPanel");
            }
        }

        private void ResolveNavigationButtons()
        {
            rehabButton = ResolveButtonReference(rehabButton, mainMenuPanel, "RehabButton", "\u5eb7\u590d\u8fd0\u52a8");
            baduanjinButton = ResolveButtonReference(baduanjinButton, rehabTrainingSelectPanel, "BaduanjinButton", "\u516b\u6bb5\u9526");
            taiChiButton = ResolveButtonReference(taiChiButton, rehabTrainingSelectPanel, "TaiChiButton", "TaijiButton", "\u592a\u6781");
            backButton = ResolveButtonReference(backButton, rehabTrainingSelectPanel, "BackButton", "HomeButton", "\u8fd4\u56de");

            if (rehabTrainingPanel != null)
            {
                trainingBackButton = ResolveButtonReference(trainingBackButton, rehabTrainingPanel, "HomeButton", "BackButton", "\u8fd4\u56de");
            }

            if (trainingResultPanel != null)
            {
                resultBackButton = ResolveButtonReference(resultBackButton, trainingResultPanel, "BackButton", "HomeButton", "\u8fd4\u56de");
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
            if (uiPlacer.usePreferredHeightInsteadOfHeadHeight && uiPlacer.headTransform != null)
            {
                var desiredHeight = uiPlacer.headTransform.position.y + trainingSelectHeightOffsetMeters;
                uiPlacer.preferredWorldHeight = Mathf.Max(uiPlacer.preferredWorldHeight, desiredHeight);
            }

            uiPlacer.enableRayDrag = true;
            uiPlacer.enableThumbstickNavigation = true;
        }

        private void ApplyHtmlStylePanelsIfNeeded()
        {
            if (!applyHtmlStylePanels) return;

            ApplyHtmlStyleSkinOnly();
            BindButtonEvents();
        }

        private void ApplyHtmlStyleSkinOnly()
        {
            if (!applyHtmlStylePanels) return;

            ResolveReferences();
            HtmlStyleRehabPanelSkin.Apply(this);
            ResolveNavigationButtons();
        }

        private void BindButtonEvents()
        {
            ReplaceButtonRoute(rehabButton, ShowTrainingSelectPanel);
            ReplaceButtonRoute(baduanjinButton, StartBaduanjinTraining);
            ReplaceButtonRoute(taiChiButton, StartTaiChiTraining);
            ReplaceButtonRoute(backButton, ReturnToMainEntry);
            ReplaceButtonRoute(trainingBackButton, ShowTrainingSelectPanel);
            ReplaceButtonRoute(resultBackButton, ShowTrainingSelectPanel);
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

        private static void ReplaceButtonRoute(Button button, UnityAction action)
        {
            if (button == null || action == null) return;

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        private static Button ResolveButtonReference(Button current, GameObject panelRoot, params string[] namesOrLabels)
        {
            if (IsButtonUnderPanel(current, panelRoot) && ButtonMatches(current, namesOrLabels, false))
            {
                return current;
            }

            if (panelRoot == null) return current;

            var buttons = panelRoot.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (ButtonMatches(buttons[i], namesOrLabels, true))
                {
                    return buttons[i];
                }
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                if (ButtonMatches(buttons[i], namesOrLabels, false))
                {
                    return buttons[i];
                }
            }

            return current;
        }

        private static bool IsButtonUnderPanel(Button button, GameObject panelRoot)
        {
            if (button == null) return false;
            if (panelRoot == null) return true;
            return button.transform == panelRoot.transform || button.transform.IsChildOf(panelRoot.transform);
        }

        private static bool ButtonMatches(Button button, string[] namesOrLabels, bool exactNameOnly)
        {
            if (button == null || namesOrLabels == null) return false;

            for (var i = 0; i < namesOrLabels.Length; i++)
            {
                var value = namesOrLabels[i];
                if (string.IsNullOrEmpty(value)) continue;

                if (string.Equals(button.name, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!exactNameOnly && button.name.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (exactNameOnly) return false;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null || string.IsNullOrEmpty(label.text)) return false;

            for (var i = 0; i < namesOrLabels.Length; i++)
            {
                var value = namesOrLabels[i];
                if (!string.IsNullOrEmpty(value) &&
                    label.text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject FindChildGameObject(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;

            var child = root.Find(childName);
            if (child != null) return child.gameObject;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildGameObject(root.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
