using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ElderCareHomeButton : MonoBehaviour
{
    public ElderCareHomeMenu menu;
    public bool applySafeGameplayLayout = true;
    public Vector2 safeAnchoredPosition = new Vector2(-760f, 500f);
    public Vector2 safeSize = new Vector2(300f, 78f);

    private Button _button;

    private void Awake()
    {
        ApplySafeGameplayLayout();
        _button = GetComponent<Button>();
        _button.onClick.RemoveListener(HandleClick);
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (menu != null)
        {
            menu.ShowHome();
        }
    }

    private void ApplySafeGameplayLayout()
    {
        if (!applySafeGameplayLayout) return;

        var rect = transform as RectTransform;
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = safeSize;
        rect.anchoredPosition = safeAnchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        transform.SetAsLastSibling();

        var panel = transform.Find("Panel") as RectTransform;
        if (panel != null)
        {
            panel.sizeDelta = safeSize;
            panel.anchoredPosition = Vector2.zero;
        }
    }
}
