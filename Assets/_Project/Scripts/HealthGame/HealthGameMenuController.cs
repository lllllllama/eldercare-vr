using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.HealthGame
{
    public class HealthGameMenuController : MonoBehaviour
    {
        [SerializeField]
        private string mainEntrySceneName = "00_MainEntry";

        [SerializeField]
        private string pingPongSceneName = "01_PingPongDemo";

        [SerializeField]
        private string archerySceneName = "03_ArcheryTraining";

        public void LoadPingPong()
        {
            LoadScene(pingPongSceneName);
        }

        public void LoadArchery()
        {
            LoadScene(archerySceneName);
        }

        public void ReturnToMainEntry()
        {
            LoadScene(mainEntrySceneName);
        }

        private bool _sceneLoadStarted;

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Scene name is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene is not available in Build Settings: {sceneName}");
                return;
            }

            // 异步加载避免 VR 里同步 LoadScene 的整帧冻结黑闪；
            // 同时挡掉老年用户容易出现的按钮连点导致的重复加载。
            if (_sceneLoadStarted) return;

            _sceneLoadStarted = true;
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
