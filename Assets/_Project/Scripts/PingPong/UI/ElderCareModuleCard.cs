using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ElderCareModuleCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public ElderCareHomeMenu menu;
    public string moduleId;
    public string moduleTitle;
    public RectTransform cardTransform;
    public CanvasGroup canvasGroup;
    public Graphic cardGraphic;
    public Graphic glowGraphic;
    public Graphic auraGraphic;
    public Graphic edgeGraphic;
    public Graphic scanLineGraphic;
    public float hoverScale = 1.05f;
    public float pressedScale = 0.96f;
    public float selectedScale = 1.03f;
    public float hoverLiftY = 9f;
    public float selectedLiftY = 5f;
    public bool playEntrance = true;
    public float entranceDelay;
    public float entranceOffsetY = -24f;
    public bool ambientMotion = true;
    public float ambientFloatY = 3.5f;
    public float ambientPulseSpeed = 1.25f;
    public float animationSpeed = 10f;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.white;
    public Color glowColor = new Color(1f, 1f, 1f, 0.35f);

    private Button _button;
    private Selectable _selectable;
    private Vector3 _basePosition;
    private float _enableTime;
    private bool _basePositionCaptured;
    private bool _hovered;
    private bool _pressed;

    private void Awake()
    {
        ResolveReferences();
        CaptureBasePosition();
        EnsureRuntimeDecor();

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
        }

        if (glowGraphic != null)
        {
            var glow = glowColor;
            glow.a = glowColor.a * 0.18f;
            glowGraphic.color = glow;
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureBasePosition();
        _enableTime = Time.unscaledTime;

        if (playEntrance && cardTransform != null)
        {
            cardTransform.localPosition = _basePosition + Vector3.up * entranceOffsetY;
        }

        if (playEntrance && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void Update()
    {
        var selected = IsSelected();
        var emphasized = _hovered || selected;
        var pulse = ambientMotion ? 0.74f + Mathf.Sin((Time.unscaledTime + entranceDelay) * ambientPulseSpeed) * 0.26f : 1f;

        if (cardTransform != null)
        {
            var targetScale = _pressed ? pressedScale : (emphasized ? (_hovered ? hoverScale : selectedScale) : 1f);
            cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * animationSpeed);

            var entranceProgress = playEntrance
                ? Mathf.Clamp01((Time.unscaledTime - _enableTime - entranceDelay) * 3.7f)
                : 1f;

            if (playEntrance && entranceProgress < 0.995f)
            {
                cardTransform.localPosition = Vector3.Lerp(cardTransform.localPosition, _basePosition, Time.unscaledDeltaTime * animationSpeed);
            }
            else
            {
                var ambient = ambientMotion ? Mathf.Sin((Time.unscaledTime + entranceDelay) * ambientPulseSpeed) * ambientFloatY : 0f;
                var lift = _pressed ? -2f : (_hovered ? hoverLiftY : (selected ? selectedLiftY : 0f));
                cardTransform.localPosition = Vector3.Lerp(
                    cardTransform.localPosition,
                    _basePosition + Vector3.up * (ambient + lift),
                    Time.unscaledDeltaTime * animationSpeed);
            }
        }

        if (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * animationSpeed);
        }

        if (cardGraphic != null)
        {
            var targetColor = _pressed ? Color.Lerp(normalColor, hoverColor, 0.55f) : (_hovered ? hoverColor : normalColor);
            if (!_hovered && selected)
            {
                targetColor = Color.Lerp(normalColor, hoverColor, 0.64f);
            }

            cardGraphic.color = Color.Lerp(cardGraphic.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
        }

        if (glowGraphic != null)
        {
            var target = glowColor;
            target.a = emphasized ? glowColor.a : glowColor.a * 0.16f * pulse;
            glowGraphic.color = Color.Lerp(glowGraphic.color, target, Time.unscaledDeltaTime * animationSpeed);
        }

        if (auraGraphic != null)
        {
            var target = glowColor;
            target.a = emphasized ? glowColor.a * 0.22f : glowColor.a * 0.07f * pulse;
            auraGraphic.color = Color.Lerp(auraGraphic.color, target, Time.unscaledDeltaTime * animationSpeed);
        }

        if (edgeGraphic != null)
        {
            var target = Color.Lerp(Color.white, glowColor, emphasized ? 0.45f : 0.2f);
            target.a = emphasized ? 0.34f : 0.16f * pulse;
            edgeGraphic.color = Color.Lerp(edgeGraphic.color, target, Time.unscaledDeltaTime * animationSpeed);
        }

        if (scanLineGraphic != null)
        {
            var target = glowColor;
            target.a = emphasized ? 0.72f : 0.28f * pulse;
            scanLineGraphic.color = Color.Lerp(scanLineGraphic.color, target, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    private void HandleClick()
    {
        if (menu != null)
        {
            menu.SelectModule(moduleId, moduleTitle);
        }
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

        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_selectable == null)
        {
            _selectable = _button != null ? _button : GetComponent<Selectable>();
        }
    }

    private void CaptureBasePosition()
    {
        if (_basePositionCaptured || cardTransform == null) return;

        _basePosition = cardTransform.localPosition;
        _basePositionCaptured = true;
    }

    private void EnsureRuntimeDecor()
    {
        if (cardTransform == null) return;

        if (auraGraphic == null)
        {
            auraGraphic = CreateRoundedGraphic(
                cardTransform,
                "Runtime_Aura",
                new Vector2(630f, 310f),
                Vector2.zero,
                54f,
                new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * 0.08f));
            if (auraGraphic != null)
            {
                auraGraphic.transform.SetAsFirstSibling();
            }
        }

        var panelRect = cardGraphic != null ? cardGraphic.transform as RectTransform : cardTransform;
        if (panelRect == null) return;

        if (edgeGraphic == null)
        {
            edgeGraphic = CreateRoundedGraphic(
                panelRect,
                "Runtime_EdgeLight",
                new Vector2(Mathf.Max(260f, panelRect.rect.width - 24f), 5f),
                new Vector2(0f, panelRect.rect.height * 0.5f - 16f),
                3f,
                new Color(1f, 1f, 1f, 0.16f));
            if (edgeGraphic != null)
            {
                edgeGraphic.transform.SetAsFirstSibling();
            }
        }

        if (scanLineGraphic == null)
        {
            scanLineGraphic = CreateRoundedGraphic(
                panelRect,
                "Runtime_ScanLine",
                new Vector2(Mathf.Max(220f, panelRect.rect.width - 96f), 3f),
                new Vector2(0f, -panelRect.rect.height * 0.5f + 24f),
                2f,
                new Color(glowColor.r, glowColor.g, glowColor.b, 0.26f));
            if (scanLineGraphic != null)
            {
                scanLineGraphic.transform.SetAsFirstSibling();
            }
        }
    }

    private Graphic CreateRoundedGraphic(RectTransform parent, string name, Vector2 size, Vector2 anchoredPosition, float radius, Color color)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(ElderCareRoundedPanel));
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        var panel = go.GetComponent<ElderCareRoundedPanel>();
        if (panel != null)
        {
            panel.cornerRadius = radius;
            panel.color = color;
            panel.raycastTarget = false;
        }

        return panel;
    }

    private bool IsSelected()
    {
        if (_selectable == null || !_selectable.IsInteractable() || EventSystem.current == null)
        {
            return false;
        }

        var selectedObject = EventSystem.current.currentSelectedGameObject;
        return selectedObject == gameObject ||
               (selectedObject != null && selectedObject.transform.IsChildOf(transform));
    }
}
