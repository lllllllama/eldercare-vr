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

        private static void LoadScene(string sceneName)
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

            SceneManager.LoadScene(sceneName);
        }
    }
}
