using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.Rehab
{
    public class UnifiedEntryMenu : MonoBehaviour
    {
        public string pingPongSceneName = "01_PingPongDemo";
        public string rehabSceneName = "MR_Rehab_Main";
        public RehabPanelPlacementController panelPlacementController;
        public bool recenterPanelsOnEnable = true;
        public int recenterDelayFrames = 2;

        private Coroutine _recenterCoroutine;

        private void OnEnable()
        {
            if (recenterPanelsOnEnable)
            {
                ScheduleRecenterPanels();
            }
        }

        private void Start()
        {
            if (recenterPanelsOnEnable)
            {
                ScheduleRecenterPanels();
            }
        }

        public void LoadPingPong()
        {
            SceneManager.LoadScene(pingPongSceneName);
        }

        public void LoadRehab()
        {
            SceneManager.LoadScene(rehabSceneName);
        }

        public void RecenterPanels()
        {
            ResolveReferences();
            if (panelPlacementController != null)
            {
                panelPlacementController.RecenterPanels();
            }
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
    }
}
