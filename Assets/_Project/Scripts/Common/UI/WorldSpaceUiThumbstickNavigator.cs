using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public class WorldSpaceUiThumbstickNavigator : MonoBehaviour
{
    public RectTransform selectableRoot;
    public Selectable[] selectables;
    public bool invertHorizontalInput = false;
    public bool invertVerticalInput = false;
    public bool disableBuiltInSelectableNavigation = true;
    public float deadZone = 0.55f;
    public float repeatDelaySeconds = 0.28f;
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private float _nextNavigateTime;

    private void Awake()
    {
        ResolveReferences();
        ConfigureSelectableNavigation();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureSelectableNavigation();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextNavigateTime) return;
        if (!TryReadAxis(out var axis)) return;

        axis.x = invertHorizontalInput ? -axis.x : axis.x;
        axis.y = invertVerticalInput ? -axis.y : axis.y;
        if (axis.sqrMagnitude < deadZone * deadZone) return;

        if (NavigateForInput(axis))
        {
            _nextNavigateTime = Time.unscaledTime + Mathf.Max(0.05f, repeatDelaySeconds);
        }
    }

    public bool NavigateForInput(Vector2 axis)
    {
        ResolveReferences();
        if (selectables == null || selectables.Length == 0) return false;

        var current = GetCurrentSelectable();
        if (current == null)
        {
            var first = FindFirstInteractable();
            if (first == null) return false;
            Select(first);
            return true;
        }

        var target = FindBestCandidate(current, axis);
        if (target == null || target == current) return false;

        Select(target);
        return true;
    }

    public void ConfigureSelectableNavigation()
    {
        if (!disableBuiltInSelectableNavigation) return;

        ResolveReferences();
        if (selectables == null) return;

        for (var i = 0; i < selectables.Length; i++)
        {
            var selectable = selectables[i];
            if (selectable == null) continue;

            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }
    }

    private Selectable FindBestCandidate(Selectable current, Vector2 axis)
    {
        var currentRect = current.transform as RectTransform;
        if (currentRect == null) return null;

        var currentPosition = currentRect.anchoredPosition;
        var horizontal = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y);
        var direction = horizontal
            ? new Vector2(Mathf.Sign(axis.x), 0f)
            : new Vector2(0f, Mathf.Sign(axis.y));

        Selectable best = null;
        var bestScore = float.PositiveInfinity;

        for (var i = 0; i < selectables.Length; i++)
        {
            var candidate = selectables[i];
            if (candidate == null || candidate == current || !candidate.IsInteractable()) continue;

            var candidateRect = candidate.transform as RectTransform;
            if (candidateRect == null) continue;

            var delta = candidateRect.anchoredPosition - currentPosition;
            if (Vector2.Dot(delta, direction) <= 0.01f) continue;

            var primaryDistance = Mathf.Abs(Vector2.Dot(delta, direction));
            var crossDistance = Mathf.Abs(horizontal ? delta.y : delta.x);
            var score = primaryDistance + crossDistance * 2.2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private bool TryReadAxis(out Vector2 axis)
    {
        if (TryReadAxis(leftControllerNode, out axis)) return true;
        if (TryReadAxis(rightControllerNode, out axis)) return true;
        axis = Vector2.zero;
        return false;
    }

    private bool TryReadAxis(XRNode node, out Vector2 axis)
    {
        axis = Vector2.zero;
        InputDevices.GetDevicesAtXRNode(node, _devices);
        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis) &&
                axis.sqrMagnitude >= deadZone * deadZone)
            {
                return true;
            }

            if (device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out axis) &&
                axis.sqrMagnitude >= deadZone * deadZone)
            {
                return true;
            }
        }

        return false;
    }

    private Selectable GetCurrentSelectable()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return null;
        }

        var selected = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
        return selected != null && selected.IsInteractable() && ContainsSelectable(selected) ? selected : null;
    }

    private Selectable FindFirstInteractable()
    {
        for (var i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null && selectables[i].IsInteractable())
            {
                return selectables[i];
            }
        }

        return null;
    }

    private void Select(Selectable selectable)
    {
        if (EventSystem.current != null && selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private bool ContainsSelectable(Selectable selectable)
    {
        if (selectables == null || selectable == null) return false;

        for (var i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == selectable)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (selectableRoot == null)
        {
            selectableRoot = transform as RectTransform;
        }

        if ((selectables == null || selectables.Length == 0) && selectableRoot != null)
        {
            selectables = selectableRoot.GetComponentsInChildren<Selectable>(true);
        }
    }
}
