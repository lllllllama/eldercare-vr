using System.Collections;
using System.Collections.Generic;
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
        public bool applyHtmlStylePanels = false;

        private Coroutine _startRecenterCoroutine;
        private Coroutine _routeRebindCoroutine;

        private void Awake()
        {
            RefreshHtmlUIAndButtonBindings("Awake");
        }

        private void OnEnable()
        {
            RefreshHtmlUIAndButtonBindings("OnEnable");
            ScheduleRouteRebind("OnEnableNextFrame");
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

            RefreshHtmlUIAndButtonBindings("Start");
            ScheduleRouteRebind("StartNextFrame");
        }

        private void OnDisable()
        {
            if (_startRecenterCoroutine != null)
            {
                StopCoroutine(_startRecenterCoroutine);
                _startRecenterCoroutine = null;
            }

            if (_routeRebindCoroutine != null)
            {
                StopCoroutine(_routeRebindCoroutine);
                _routeRebindCoroutine = null;
            }
        }

        public void ShowMainMenuPanel()
        {
            CancelCurrentTrainingAndHideVideo();

            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, false);

            RefreshHtmlUIAndButtonBindings("ShowMainMenuPanel");

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

            RefreshHtmlUIAndButtonBindings("ShowTrainingSelectPanel");

            if (placeUiOnTrainingSelectOpen)
            {
                RecenterNavigationPanels();
            }
        }

        public void StartBaduanjinTraining()
        {
            Debug.Log("[RehabModeSelectUI] StartBaduanjinTraining invoked.");
            StartTraining(RehabTrainingType.Baduanjin);
        }

        public void StartTaiChiTraining()
        {
            Debug.Log("[RehabModeSelectUI] StartTaiChiTraining invoked.");
            StartTraining(RehabTrainingType.TaiChi);
        }

        public void ShowTrainingResultPanel()
        {
            StopVideoGuideOnly();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, false);
            SetPanelActive(trainingResultPanel, true);

            RefreshHtmlUIAndButtonBindings("ShowTrainingResultPanel");
            RecenterNavigationPanels();
        }

        public void ReturnToMainEntry()
        {
            Debug.Log("[RehabModeSelectUI] ReturnToMainEntry invoked.");
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
            Debug.Log("[RehabModeSelectUI] StartTraining invoked. trainingType=" + trainingType);
            ResolveReferences();
            CancelCurrentTrainingAndHideVideo();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(rehabTrainingSelectPanel, false);
            SetPanelActive(rehabTrainingPanel, true);
            SetPanelActive(trainingResultPanel, false);

            RefreshHtmlUIAndButtonBindings("StartTraining.ShowTrainingPanel");

            if (sessionManager != null)
            {
                sessionManager.StartTraining(trainingType);
                RefreshHtmlUIAndButtonBindings("StartTraining.AfterBeginSession");
            }
            else
            {
                Debug.LogError("Cannot start rehab training because RehabSessionManager is not assigned.");
            }
        }

        private void RefreshHtmlUIAndButtonBindings(string reason)
        {
            ResolvePanelReferences();
            ResolveNonPanelReferences();

            Debug.Log($"[RehabModeSelectUI] RefreshHtmlUIAndButtonBindings reason={reason}, applyHtmlStylePanels={applyHtmlStylePanels}, object={name}, scene={gameObject.scene.name}");

            if (applyHtmlStylePanels)
            {
                try
                {
                    HtmlStyleRehabPanelSkin.Apply(this);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RehabModeSelectUI] HtmlStyleRehabPanelSkin.Apply failed during {reason}: {ex}");
                }
            }
            else
            {
                Debug.Log($"[RehabModeSelectUI] HTML style apply skipped during {reason} because applyHtmlStylePanels=false, object={name}, scene={gameObject.scene.name}");
            }

            ResolveNavigationButtons();
            NormalizeButtonRaycasts();
            BindButtonEvents();
            LogButtonBindingState(reason);
        }

        private void ResolveReferences()
        {
            ResolvePanelReferences();
            ResolveNonPanelReferences();
            ResolveNavigationButtons();
        }

        private void ResolveNonPanelReferences()
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

        private void BindButtonEvents()
        {
            ReplaceButtonRoute(rehabButton, ShowTrainingSelectPanel);
            ReplaceButtonRoute(baduanjinButton, StartBaduanjinTraining);
            ReplaceButtonRoute(taiChiButton, StartTaiChiTraining);
            ReplaceButtonRoute(backButton, ReturnToMainEntry, ShowMainMenuPanel);
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

        private void ScheduleRouteRebind(string reason)
        {
            if (!isActiveAndEnabled) return;

            if (_routeRebindCoroutine != null)
            {
                StopCoroutine(_routeRebindCoroutine);
            }

            _routeRebindCoroutine = StartCoroutine(RebindButtonEventsAfterActivation(reason));
        }

        private IEnumerator RebindButtonEventsAfterActivation(string reason)
        {
            yield return null;

            RefreshHtmlUIAndButtonBindings(reason);
            _routeRebindCoroutine = null;
        }

        private void NormalizeButtonRaycasts()
        {
            var forcedInteractableButtons = new HashSet<Button>();
            AddButton(forcedInteractableButtons, rehabButton);
            AddButton(forcedInteractableButtons, baduanjinButton);
            AddButton(forcedInteractableButtons, taiChiButton);
            AddButton(forcedInteractableButtons, backButton);
            AddButton(forcedInteractableButtons, trainingBackButton);
            AddButton(forcedInteractableButtons, resultBackButton);

            NormalizeButtonRaycasts(mainMenuPanel, forcedInteractableButtons);
            NormalizeButtonRaycasts(rehabTrainingSelectPanel, forcedInteractableButtons);
            NormalizeButtonRaycasts(rehabTrainingPanel, forcedInteractableButtons);
            NormalizeButtonRaycasts(trainingResultPanel, forcedInteractableButtons);
        }

        private static void NormalizeButtonRaycasts(GameObject panelRoot, HashSet<Button> forcedInteractableButtons)
        {
            if (panelRoot == null) return;

            var allowedGraphics = new HashSet<Graphic>();
            var buttons = panelRoot.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null) continue;

                button.enabled = true;
                if (forcedInteractableButtons != null && forcedInteractableButtons.Contains(button))
                {
                    button.interactable = true;
                }

                var targetGraphic = EnsureButtonTargetGraphic(button);
                if (targetGraphic != null)
                {
                    targetGraphic.raycastTarget = true;
                    allowedGraphics.Add(targetGraphic);
                }

                EnsureButtonDiagnostics(button);
            }

            var graphics = panelRoot.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                var graphic = graphics[i];
                if (graphic == null) continue;

                graphic.raycastTarget = allowedGraphics.Contains(graphic);
            }
        }

        private static void AddButton(HashSet<Button> buttons, Button button)
        {
            if (buttons != null && button != null)
            {
                buttons.Add(button);
            }
        }

        private static void ReplaceButtonRoute(Button button, UnityAction action, params UnityAction[] obsoleteActions)
        {
            if (button == null || action == null) return;

            button.enabled = true;
            button.interactable = true;

            var targetGraphic = EnsureButtonTargetGraphic(button);
            if (targetGraphic != null)
            {
                targetGraphic.raycastTarget = true;
            }
            EnsureButtonDiagnostics(button);

            if (obsoleteActions != null)
            {
                for (var i = 0; i < obsoleteActions.Length; i++)
                {
                    if (obsoleteActions[i] != null)
                    {
                        button.onClick.RemoveListener(obsoleteActions[i]);
                    }
                }
            }

            button.onClick.RemoveListener(action);
            if (!HasPersistentButtonListener(button, action))
            {
                button.onClick.AddListener(action);
            }
        }

        private static Graphic EnsureButtonTargetGraphic(Button button)
        {
            if (button == null) return null;
            if (button.targetGraphic != null) return button.targetGraphic;

            var graphic = button.GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = button.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
                graphic = image;
            }

            button.targetGraphic = graphic;
            return graphic;
        }

        private static void EnsureButtonDiagnostics(Button button)
        {
            if (button == null) return;

            var diagnostics = button.GetComponent<RehabUIButtonDiagnostics>();
            if (diagnostics == null)
            {
                button.gameObject.AddComponent<RehabUIButtonDiagnostics>();
            }
        }

        private static bool HasPersistentButtonListener(Button button, UnityAction action)
        {
            if (button == null || button.onClick == null || action == null) return false;

            var target = action.Target as Object;
            var methodName = action.Method != null ? action.Method.Name : string.Empty;
            if (target == null || string.IsNullOrEmpty(methodName)) return false;

            var persistentCount = button.onClick.GetPersistentEventCount();
            for (var i = 0; i < persistentCount; i++)
            {
                if (button.onClick.GetPersistentTarget(i) == target &&
                    button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private void LogButtonBindingState(string reason)
        {
            Debug.Log(
                "[RehabModeSelectUI] Button bindings refreshed: " + reason + "\n" +
                DescribeButton("rehabButton", rehabButton) + "\n" +
                DescribeButton("baduanjinButton", baduanjinButton) + "\n" +
                DescribeButton("taiChiButton", taiChiButton) + "\n" +
                DescribeButton("backButton", backButton) + "\n" +
                DescribeButton("trainingBackButton", trainingBackButton) + "\n" +
                DescribeButton("resultBackButton", resultBackButton));
        }

        private static string DescribeButton(string label, Button button)
        {
            if (button == null)
            {
                return label + ": null=True, path=<null>, label=, activeInHierarchy=False, enabled=False, interactable=False, targetGraphic=<null>, targetRaycast=False, persistentListeners=0";
            }

            var targetGraphic = button.targetGraphic;
            return string.Format(
                "{0}: null=False, path={1}, label={2}, activeInHierarchy={3}, enabled={4}, interactable={5}, targetGraphic={6}, targetRaycast={7}, persistentListeners={8}",
                label,
                GetTransformPath(button.transform),
                GetButtonLabel(button),
                button.gameObject.activeInHierarchy,
                button.enabled,
                button.interactable,
                targetGraphic != null ? targetGraphic.name : "<null>",
                targetGraphic != null && targetGraphic.raycastTarget,
                button.onClick != null ? button.onClick.GetPersistentEventCount() : 0);
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null) return "<null>";

            var path = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static string GetButtonLabel(Button button)
        {
            if (button == null) return string.Empty;

            var tmpText = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
            {
                return tmpText.text;
            }

            var legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null && !string.IsNullOrEmpty(legacyText.text))
            {
                return legacyText.text;
            }

            return string.Empty;
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

            var tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            var legacyLabel = button.GetComponentInChildren<Text>(true);

            for (var i = 0; i < namesOrLabels.Length; i++)
            {
                var value = namesOrLabels[i];
                if (!string.IsNullOrEmpty(value) &&
                    tmpLabel != null &&
                    !string.IsNullOrEmpty(tmpLabel.text) &&
                    tmpLabel.text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(value) &&
                    legacyLabel != null &&
                    !string.IsNullOrEmpty(legacyLabel.text) &&
                    legacyLabel.text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
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
