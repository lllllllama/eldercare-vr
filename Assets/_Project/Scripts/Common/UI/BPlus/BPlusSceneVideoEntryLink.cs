using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusSceneVideoEntryLink : MonoBehaviour
    {
        public string sceneName = "SceneVideo_BPlusUI";

        public void LoadSceneVideo()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("B+ scene video entry cannot load because sceneName is empty.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
