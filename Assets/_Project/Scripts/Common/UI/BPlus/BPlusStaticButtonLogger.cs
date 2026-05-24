using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.UI.BPlus
{
    [RequireComponent(typeof(Button))]
    public class BPlusStaticButtonLogger : MonoBehaviour
    {
        public string panelName;
        public string buttonLabel;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(LogClick);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(LogClick);
            }
        }

        public void LogClick()
        {
            Debug.Log($"B+ UI static button clicked: {panelName}/{buttonLabel}", this);
        }
    }
}
