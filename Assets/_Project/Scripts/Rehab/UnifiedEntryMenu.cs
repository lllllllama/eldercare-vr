using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.Rehab
{
    public class UnifiedEntryMenu : MonoBehaviour
    {
        public string pingPongSceneName = "01_PingPongDemo";
        public string rehabSceneName = "MR_Rehab_Main";
        public bool applyHtmlStyleMainPanel = true;
        public string htmlStyleMainSceneName = "00_MainEntry";
        public string htmlStyleCanvasName = "MainEntryCanvas";
        public Transform htmlStyleMainCanvas;

        private void Start()
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

        public void LoadPingPong()
        {
            SceneManager.LoadScene(pingPongSceneName);
        }

        public void LoadRehab()
        {
            SceneManager.LoadScene(rehabSceneName);
        }

        private bool ShouldApplyHtmlStyleMainPanel()
        {
            if (!applyHtmlStyleMainPanel) return false;
            if (string.IsNullOrEmpty(htmlStyleMainSceneName)) return true;

            var activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && activeScene.name == htmlStyleMainSceneName;
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
