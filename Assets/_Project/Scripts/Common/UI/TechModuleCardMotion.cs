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

        var targetScale = _pressed ? pressedScale : (_hovered ? hoverScale : 1f);
        cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * animationSpeed);

        if (playEntrance)
        {
            var progress = Mathf.Clamp01((Time.unscaledTime - _enableTime - entranceDelay) * 3.6f);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, progress, Time.unscaledDeltaTime * animationSpeed);
            }

            cardTransform.localPosition = Vector3.Lerp(cardTransform.localPosition, _basePosition, Time.unscaledDeltaTime * animationSpeed);
        }

        var targetColor = _pressed ? pressedColor : (_hovered ? hoverColor : normalColor);
        if (cardGraphic != null)
        {
            cardGraphic.color = Color.Lerp(cardGraphic.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
        }

        if (glowGraphic != null)
        {
            var glow = glowColor;
            glow.a = _hovered && interactable ? glowColor.a : glowColor.a * 0.28f;
            glowGraphic.color = Color.Lerp(glowGraphic.color, glow, Time.unscaledDeltaTime * animationSpeed);
        }

        if (edgeGraphic != null)
        {
            var edge = edgeColor;
            edge.a = _hovered && interactable ? edgeColor.a : edgeColor.a * 0.45f;
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
    }
}
