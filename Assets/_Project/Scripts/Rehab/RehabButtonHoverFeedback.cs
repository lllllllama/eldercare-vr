using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RehabButtonHoverFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Button _button;
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private Graphic _surface;
        [SerializeField] private Graphic _glow;
        [SerializeField] private Color _normalSurfaceColor;
        [SerializeField] private Color _hoverSurfaceColor;
        [SerializeField] private Color _normalGlowColor;
        [SerializeField] private Color _hoverGlowColor;
        [SerializeField] private Vector2 _basePosition;
        [SerializeField] private float _hoverScale = 1.04f;
        [SerializeField] private float _pressedScale = 0.975f;
        [SerializeField] private float _hoverLift = 5f;
        [SerializeField] private float _animationSpeed = 12f;
        [SerializeField] private bool _configured;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;

        public void Configure(
            RectTransform visualRoot,
            Graphic surface,
            Graphic glow,
            Color normalSurfaceColor,
            Color hoverSurfaceColor,
            Color normalGlowColor,
            Color hoverGlowColor,
            float hoverScale,
            float hoverLift)
        {
            _button = GetComponent<Button>();
            _visualRoot = visualRoot;
            _surface = surface;
            _glow = glow;
            _normalSurfaceColor = normalSurfaceColor;
            _hoverSurfaceColor = hoverSurfaceColor;
            _normalGlowColor = normalGlowColor;
            _hoverGlowColor = hoverGlowColor;
            _hoverScale = Mathf.Max(1f, hoverScale);
            _hoverLift = Mathf.Max(0f, hoverLift);
            _basePosition = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
            _configured = visualRoot != null;

            ResetFeedbackImmediate();
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            ResetFeedbackImmediate();
        }

        public void ResetFeedbackImmediate()
        {
            _hovered = false;
            _pressed = false;
            _selected = false;

            if (!_configured || _visualRoot == null) return;

            _visualRoot.localScale = Vector3.one;
            _visualRoot.anchoredPosition = _basePosition;
            if (_surface != null) _surface.color = _normalSurfaceColor;
            if (_glow != null) _glow.color = _normalGlowColor;
        }

        private void Update()
        {
            if (!_configured || _visualRoot == null) return;

            var interactable = _button != null && _button.IsInteractable();
            var emphasized = interactable && (_hovered || _selected);
            var targetScale = _pressed ? _pressedScale : (emphasized ? _hoverScale : 1f);
            var targetPosition = _basePosition + Vector2.up * (_pressed ? -1f : (emphasized ? _hoverLift : 0f));
            var speed = Time.unscaledDeltaTime * _animationSpeed;

            _visualRoot.localScale = Vector3.Lerp(_visualRoot.localScale, Vector3.one * targetScale, speed);
            _visualRoot.anchoredPosition = Vector2.Lerp(_visualRoot.anchoredPosition, targetPosition, speed);

            if (_surface != null)
            {
                var surfaceColor = emphasized || _pressed ? _hoverSurfaceColor : _normalSurfaceColor;
                _surface.color = Color.Lerp(_surface.color, surfaceColor, speed);
            }

            if (_glow != null)
            {
                var glowColor = emphasized || _pressed ? _hoverGlowColor : _normalGlowColor;
                _glow.color = Color.Lerp(_glow.color, glowColor, speed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && _button.IsInteractable()) _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && _button.IsInteractable()) _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_button != null && _button.IsInteractable()) _selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
        }
    }
}
