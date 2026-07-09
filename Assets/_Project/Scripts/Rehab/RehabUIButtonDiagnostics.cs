using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public class RehabUIButtonDiagnostics : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        private Button _button;

        private void Awake()
        {
            ResolveButton();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            LogPointerEvent("ENTER", eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            LogPointerEvent("EXIT", eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            LogPointerEvent("DOWN", eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            LogPointerEvent("UP", eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            LogPointerEvent("CLICK", eventData);
        }

        private void LogPointerEvent(string eventName, PointerEventData eventData)
        {
            ResolveButton();

            var targetGraphic = _button != null ? _button.targetGraphic : null;
            Debug.Log(
                string.Format(
                    "RehabUIButtonDiagnostics {0} {1}, label={2}, active={3}, enabled={4}, interactable={5}, targetGraphic={6}, targetRaycast={7}, pointerId={8}",
                    eventName,
                    gameObject.name,
                    GetLabel(),
                    gameObject.activeInHierarchy,
                    _button != null && _button.enabled,
                    _button != null && _button.interactable,
                    targetGraphic != null ? targetGraphic.name : "<null>",
                    targetGraphic != null && targetGraphic.raycastTarget,
                    eventData != null ? eventData.pointerId : 0));
        }

        private void ResolveButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private string GetLabel()
        {
            var tmpText = GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
            {
                return tmpText.text;
            }

            var legacyText = GetComponentInChildren<Text>(true);
            if (legacyText != null && !string.IsNullOrEmpty(legacyText.text))
            {
                return legacyText.text;
            }

            return string.Empty;
        }
    }
}
