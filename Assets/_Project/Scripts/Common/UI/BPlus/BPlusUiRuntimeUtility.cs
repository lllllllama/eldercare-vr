using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    internal static class BPlusUiRuntimeUtility
    {
        public static T FindComponent<T>(Transform root, string objectName) where T : Component
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;
            if (objectName.IndexOf('/') >= 0)
            {
                var target = root.Find(objectName);
                if (target != null)
                {
                    return target.GetComponent<T>();
                }
            }

            var components = root.GetComponentsInChildren<T>(true);
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].name == objectName)
                {
                    return components[i];
                }
            }

            return null;
        }

        public static Transform FindTransform(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;
            if (objectName.IndexOf('/') >= 0)
            {
                return root.Find(objectName);
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        public static Button FindButton(Transform root, string objectName)
        {
            return FindComponent<Button>(root, objectName);
        }

        public static TMP_Text FindText(Transform root, string objectName)
        {
            return FindComponent<TMP_Text>(root, objectName);
        }

        public static RectTransform FindRect(Transform root, string objectName)
        {
            return FindComponent<RectTransform>(root, objectName);
        }

        public static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        public static void Bind(Button button, UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        public static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        public static string FormatClock(float seconds)
        {
            var safeSeconds = Mathf.Max(0f, seconds);
            return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(safeSeconds / 60f), Mathf.FloorToInt(safeSeconds % 60f));
        }
    }
}
