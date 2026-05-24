using UnityEngine;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusUiModeSwitch : MonoBehaviour
    {
        public bool useBPlusUi = true;
        public GameObject[] bPlusUiRoots;
        public GameObject[] legacyUiRoots;
        public bool applyOnStart = true;
        public bool applyOnValidate = true;

        private void Start()
        {
            if (applyOnStart)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            if (applyOnValidate)
            {
                Apply();
            }
        }

        [ContextMenu("Apply B+ UI Mode")]
        public void Apply()
        {
            SetRootsActive(bPlusUiRoots, useBPlusUi);
            SetRootsActive(legacyUiRoots, !useBPlusUi);
        }

        public void SetUseBPlusUi(bool value)
        {
            useBPlusUi = value;
            Apply();
        }

        private static void SetRootsActive(GameObject[] roots, bool active)
        {
            if (roots == null) return;
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    roots[i].SetActive(active);
                }
            }
        }
    }
}
