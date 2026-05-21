using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TechModuleCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform cardTransform;
    public CanvasGroup canvasGroup;
    public Graphic cardGraphic;
    public Graphic glowGraphic;
    public Graphic edgeGraphic;
    public bool interactable = true;
    public bool playEntrance = true;
    public float entranceDelay;
    public float entranceOffsetY = -24f;
    public float hoverScale = 1.035f;
    public float pressedScale = 0.97f;
    public float selectedScale = 1.028f;
    public float hoverLiftY = 8f;
    public float selectedLiftY = 5f;
    public bool ambientMotion = true;
    public float ambientFloatY = 3.5f;
    public float ambientPulseSpeed = 1.35f;
    public float animationSpeed = 10f;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.white;
    public Color pressedColor = Color.white;
    public Color glowColor = new Color(0.4f, 0.9f, 1f, 0.28f);
    public Color edgeColor = new Color(0.65f, 0.95f, 1f, 0.36f);

    private Vector3 _basePosition;
    private bool _hovered;
    private bool _pressed;
    private float _enableTime;
    private Selectable _selectable;

    private void Awake()
    {
        ResolveReferences();
        if (cardTransform == null) return;
        _basePosition = cardTransform.localPosition;
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (cardTransform == null) return;
        _basePosition = cardTransform.localPosition;
        _enableTime = Time.unscaledTime;

        if (playEntrance && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            cardTransform.localPosition = _basePosition + Vector3.up * entranceOffsetY;
        }
    }

    private void Update()
    {
        if (cardTransform == null) return;

        var selected = IsSelected();
        var emphasized = _hovered || selected;
        var targetScale = _pressed ? pressedScale : (emphasized ? (_hovered ? hoverScale : selectedScale) : 1f);
        cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * animationSpeed);

        var entranceProgress = playEntrance
            ? Mathf.Clamp01((Time.unscaledTime - _enableTime - entranceDelay) * 3.6f)
            : 1f;

        if (playEntrance && entranceProgress < 0.995f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, entranceProgress, Time.unscaledDeltaTime * animationSpeed);
            }

            cardTransform.localPosition = Vector3.Lerp(cardTransform.localPosition, _basePosition, Time.unscaledDeltaTime * animationSpeed);
        }
        else
        {
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * animationSpeed);
            }

            var ambient = ambientMotion ? Mathf.Sin((Time.unscaledTime + entranceDelay) * ambientPulseSpeed) * ambientFloatY : 0f;
            var lift = _pressed ? -2f : (_hovered ? hoverLiftY : (selected ? selectedLiftY : 0f));
            var targetPosition = _basePosition + Vector3.up * (ambient + lift);
            cardTransform.localPosition = Vector3.Lerp(cardTransform.localPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
        }

        var targetColor = _pressed ? pressedColor : (_hovered ? hoverColor : normalColor);
        if (!_hovered && selected)
        {
            targetColor = Color.Lerp(normalColor, hoverColor, 0.68f);
        }

        if (cardGraphic != null)
        {
            cardGraphic.color = Color.Lerp(cardGraphic.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
        }

        if (glowGraphic != null)
        {
            var glow = glowColor;
            var pulse = ambientMotion ? 0.76f + Mathf.Sin((Time.unscaledTime + entranceDelay) * ambientPulseSpeed) * 0.24f : 1f;
            glow.a = emphasized && interactable ? glowColor.a : glowColor.a * 0.34f * pulse;
            glowGraphic.color = Color.Lerp(glowGraphic.color, glow, Time.unscaledDeltaTime * animationSpeed);
        }

        if (edgeGraphic != null)
        {
            var edge = edgeColor;
            var pulse = ambientMotion ? 0.72f + Mathf.Sin((Time.unscaledTime + entranceDelay + 0.4f) * ambientPulseSpeed) * 0.28f : 1f;
            edge.a = emphasized && interactable ? edgeColor.a : edgeColor.a * 0.52f * pulse;
            edgeGraphic.color = Color.Lerp(edgeGraphic.color, edge, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (interactable) _hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable) _pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    private void ResolveReferences()
    {
        if (cardTransform == null)
        {
            cardTransform = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_selectable == null)
        {
            _selectable = GetComponent<Selectable>();
        }
    }

    private bool IsSelected()
    {
        return interactable &&
               _selectable != null &&
               _selectable.IsInteractable() &&
               EventSystem.current != null &&
               EventSystem.current.currentSelectedGameObject == gameObject;
    }
}
