using PicoElderCare.Rehab;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusRehabSelectController : MonoBehaviour
    {
        public RehabModeSelectUI modeSelectUI;
        public RehabSessionManager sessionManager;
        public Button baduanjinButton;
        public Button taiChiButton;
        public Button returnButton;
        public GameObject mainEntryRoot;
        public GameObject rehabTrainingPanel;
        public string mainEntrySceneName = "00_MainEntry_BPlus";
        public bool returnBySceneLoad;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
        }

        public void StartBaduanjin()
        {
            StartTraining(RehabTrainingType.Baduanjin);
        }

        public void StartTaiChi()
        {
            StartTraining(RehabTrainingType.TaiChi);
        }

        public void ReturnToMainEntry()
        {
            if (modeSelectUI != null)
            {
                modeSelectUI.ReturnToMainEntry();
                return;
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

        private void StartTraining(RehabTrainingType trainingType)
        {
            ResolveReferences();

            if (modeSelectUI != null)
            {
                if (trainingType == RehabTrainingType.TaiChi)
                {
                    modeSelectUI.StartTaiChiTraining();
                }
                else
                {
                    modeSelectUI.StartBaduanjinTraining();
                }
            }
            else if (sessionManager != null)
            {
                sessionManager.StartTraining(trainingType);
            }
            else
            {
                Debug.LogWarning("B+ Rehab UI cannot start training because RehabModeSelectUI/RehabSessionManager is missing.", this);
            }

            if (rehabTrainingPanel != null)
            {
                rehabTrainingPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (modeSelectUI == null) modeSelectUI = FindObjectOfType<RehabModeSelectUI>(true);
            if (sessionManager == null) sessionManager = FindObjectOfType<RehabSessionManager>(true);
            if (baduanjinButton == null) baduanjinButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Baduanjin");
            if (taiChiButton == null) taiChiButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_TaiChi");
            if (returnButton == null) returnButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Return");
        }

        private void BindButtons()
        {
            BPlusUiRuntimeUtility.Bind(baduanjinButton, StartBaduanjin);
            BPlusUiRuntimeUtility.Bind(taiChiButton, StartTaiChi);
            BPlusUiRuntimeUtility.Bind(returnButton, ReturnToMainEntry);
        }
    }
}
