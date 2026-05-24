using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusMainEntryController : MonoBehaviour
    {
        public Button pingPongButton;
        public Button rehabButton;
        public Button travelButton;
        public Button sceneVideoButton;
        public TMP_Text statusText;
        public GameObject mainEntryRoot;
        public GameObject pingPongPanel;
        public GameObject rehabSelectPanel;
        public GameObject sceneVideoPanel;
        public string pingPongSceneName = "01_PingPongDemo";
        public string rehabSceneName = "MR_Rehab_Main";
        public string sceneVideoSceneName = "SceneVideo_BPlusUI";
        public bool loadScenes = false;
        public bool sceneVideoAvailable = true;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            ShowMainEntry();
        }

        public void ShowMainEntry()
        {
            SetPanel(mainEntryRoot != null ? mainEntryRoot : gameObject, true);
            SetPanel(pingPongPanel, false);
            SetPanel(rehabSelectPanel, false);
            SetPanel(sceneVideoPanel, false);
            SetStatus("请选择服务");
        }

        public void OpenPingPong()
        {
            if (loadScenes)
            {
                LoadSceneIfConfigured(pingPongSceneName);
                return;
            }

            SetPanel(mainEntryRoot != null ? mainEntryRoot : gameObject, false);
            SetPanel(pingPongPanel, true);
            SetPanel(rehabSelectPanel, false);
            SetPanel(sceneVideoPanel, false);
            SetStatus("进入乒乓球训练");
        }

        public void OpenRehab()
        {
            if (loadScenes)
            {
                LoadSceneIfConfigured(rehabSceneName);
                return;
            }

            SetPanel(mainEntryRoot != null ? mainEntryRoot : gameObject, false);
            SetPanel(pingPongPanel, false);
            SetPanel(rehabSelectPanel, true);
            SetPanel(sceneVideoPanel, false);
            SetStatus("进入康复运动");
        }

        public void OpenSceneVideo()
        {
            if (!sceneVideoAvailable)
            {
                ShowPending("场景视频");
                return;
            }

            if (loadScenes)
            {
                LoadSceneIfConfigured(sceneVideoSceneName);
                return;
            }

            SetPanel(mainEntryRoot != null ? mainEntryRoot : gameObject, false);
            SetPanel(pingPongPanel, false);
            SetPanel(rehabSelectPanel, false);
            SetPanel(sceneVideoPanel, true);
            SetStatus("进入场景视频");
        }

        public void OpenTravel()
        {
            ShowPending("VR旅游");
        }

        public void ShowPending(string moduleName)
        {
            ShowMainEntry();
            SetStatus(moduleName + " 功能待接入");
            Debug.Log("B+ UI pending module: " + moduleName, this);
        }

        private void ResolveReferences()
        {
            var root = transform;
            if (pingPongButton == null) pingPongButton = BPlusUiRuntimeUtility.FindButton(root, "Button_PingPong");
            if (rehabButton == null) rehabButton = BPlusUiRuntimeUtility.FindButton(root, "Button_Rehab");
            if (travelButton == null) travelButton = BPlusUiRuntimeUtility.FindButton(root, "Button_Travel");
            if (sceneVideoButton == null) sceneVideoButton = BPlusUiRuntimeUtility.FindButton(root, "Button_SceneVideo");
            if (statusText == null) statusText = BPlusUiRuntimeUtility.FindText(root, "StatusText");
        }

        private void BindButtons()
        {
            BPlusUiRuntimeUtility.Bind(pingPongButton, OpenPingPong);
            BPlusUiRuntimeUtility.Bind(rehabButton, OpenRehab);
            BPlusUiRuntimeUtility.Bind(travelButton, OpenTravel);
            BPlusUiRuntimeUtility.Bind(sceneVideoButton, OpenSceneVideo);
        }

        private void LoadSceneIfConfigured(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                SetStatus("目标场景未配置");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void SetStatus(string value)
        {
            BPlusUiRuntimeUtility.SetText(statusText, value);
        }

        private static void SetPanel(GameObject target, bool active)
        {
            BPlusUiRuntimeUtility.SetActive(target, active);
        }
    }
}
