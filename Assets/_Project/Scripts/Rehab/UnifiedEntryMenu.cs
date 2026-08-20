using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class UnifiedEntryMenu : MonoBehaviour
    {
        public string healthGameMenuSceneName = "02_HealthGameMenu";
        public string pingPongSceneName = "01_PingPongDemo";
        public string rehabSceneName = "MR_Rehab_Main";
        public bool applyHtmlStyleMainPanel = true;
        public string htmlStyleMainSceneName = "00_MainEntry";
        public string htmlStyleCanvasName = "MainEntryCanvas";
        public Transform htmlStyleMainCanvas;
        public RehabPanelPlacementController panelPlacementController;
        public bool recenterPanelsOnEnable = true;
        public int recenterDelayFrames = 2;

        private Coroutine _recenterCoroutine;
        private PicoWristTrackingStatusPanel _trackerSettingsPanel;
        private bool _quitRequested;

        private void OnEnable()
        {
            if (recenterPanelsOnEnable)
            {
                ScheduleRecenterPanels();
            }
        }

        private void Start()
        {
            ApplyHtmlStyleMainPanelIfNeeded();
            BindTrackerSettingsButton();

            if (recenterPanelsOnEnable)
            {
                ScheduleRecenterPanels();
            }
        }

        public void LoadPingPong()
        {
            SceneManager.LoadScene(pingPongSceneName);
        }

        public void LoadHealthGames()
        {
            SceneManager.LoadScene(healthGameMenuSceneName);
        }

        public void LoadRehab()
        {
            SceneManager.LoadScene(rehabSceneName);
        }

        public void OpenTrackerSettings()
        {
            var canvas = htmlStyleMainCanvas != null
                ? htmlStyleMainCanvas
                : FindSceneTransform(htmlStyleCanvasName);
            if (canvas == null) return;

            _trackerSettingsPanel = PicoWristTrackingStatusPanel.Ensure(canvas, this);
            if (_trackerSettingsPanel != null) _trackerSettingsPanel.Open();
        }

        public void CloseTrackerSettings()
        {
            if (_trackerSettingsPanel != null) _trackerSettingsPanel.Close();
        }

        public void QuitApplication()
        {
            if (_quitRequested) return;
            _quitRequested = true;

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void RecenterPanels()
        {
            ResolveReferences();
            if (panelPlacementController != null)
            {
                panelPlacementController.RecenterPanels();
            }
        }

        private void ApplyHtmlStyleMainPanelIfNeeded()
        {
            if (!ShouldApplyHtmlStyleMainPanel()) return;

            var canvas = htmlStyleMainCanvas != null
                ? htmlStyleMainCanvas
                : FindSceneTransform(htmlStyleCanvasName);
            if (canvas != null)
            {
                HtmlStyleMainEntryPanel.Ensure(canvas, this, null);
            }
        }

        private bool ShouldApplyHtmlStyleMainPanel()
        {
            if (!applyHtmlStyleMainPanel) return false;
            if (string.IsNullOrEmpty(htmlStyleMainSceneName)) return true;

            var activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && activeScene.name == htmlStyleMainSceneName;
        }

        private void BindTrackerSettingsButton()
        {
            if (!IsMainEntryScene()) return;
            var canvas = htmlStyleMainCanvas != null
                ? htmlStyleMainCanvas
                : FindSceneTransform(htmlStyleCanvasName);
            if (canvas == null) return;

            _trackerSettingsPanel = PicoWristTrackingStatusPanel.Ensure(canvas, this);
            var settings = FindChild(canvas, "Settings");
            var button = settings != null ? settings.GetComponent<Button>() : null;
            if (button == null) return;
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenTrackerSettings);

            var group = settings.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            var motion = settings.GetComponent<TechModuleCardMotion>();
            if (motion != null) motion.interactable = true;
        }

        private bool IsMainEntryScene()
        {
            if (string.IsNullOrEmpty(htmlStyleMainSceneName)) return true;
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && activeScene.name == htmlStyleMainSceneName;
        }

        private void ScheduleRecenterPanels()
        {
            if (!isActiveAndEnabled) return;

            if (_recenterCoroutine != null)
            {
                StopCoroutine(_recenterCoroutine);
            }

            _recenterCoroutine = StartCoroutine(RecenterPanelsAfterDelay());
        }

        private IEnumerator RecenterPanelsAfterDelay()
        {
            var frames = Mathf.Max(0, recenterDelayFrames);
            for (var i = 0; i < frames; i++)
            {
                yield return null;
            }

            RecenterPanels();
            _recenterCoroutine = null;
        }

        private void ResolveReferences()
        {
            if (panelPlacementController == null)
            {
                panelPlacementController = FindObjectOfType<RehabPanelPlacementController>(true);
            }
        }

        private static Transform FindSceneTransform(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindChild(roots[i].transform, objectName);
                if (found != null) return found;
            }

            return null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name == objectName) return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), objectName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
