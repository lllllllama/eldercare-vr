using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldSpaceUiRayDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public ComfortWorldSpaceUIPlacer placer;
    public Transform targetRoot;
    public Transform headTransform;
    public Graphic handleGraphic;
    public Color normalColor = new Color(0.35f, 0.95f, 1f, 0.34f);
    public Color activeColor = new Color(0.68f, 1f, 1f, 0.76f);
    public float minDistanceMeters = 0.9f;
    public float maxDistanceMeters = 3.4f;
    public float minHeightFromHead = -0.45f;
    public float maxHeightFromHead = 0.55f;
    public bool lockWorldHeight = true;
    public bool lockHeightToComfortOffset = true;
    public float lockedHeightToleranceMeters = 0.08f;

    private Plane _dragPlane;
    private Vector3 _dragOffset;
    private Transform _activeRayTransform;
    private bool _dragging;
    private float _dragStartHeight;
    private float _lockedWorldY;
    private bool _hasDragStartHeight;

    private void Awake()
    {
        ResolveReferences();
        ApplyVisual(false);
    }

    private void Update()
    {
        if (!_dragging || _activeRayTransform == null) return;

        var ray = new Ray(_activeRayTransform.position, _activeRayTransform.forward);
        MoveFromRay(ray);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyVisual(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_dragging)
        {
            ApplyVisual(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();
        if (targetRoot == null) return;

        var hitPoint = ResolvePointerWorldPoint(eventData, targetRoot.position);
        _dragPlane = new Plane(GetDragPlaneNormal(), targetRoot.position);
        _activeRayTransform = FindBestControllerRay(hitPoint);
        _dragOffset = targetRoot.position - hitPoint;
        _dragStartHeight = targetRoot.position.y;
        _lockedWorldY = targetRoot.position.y;
        _hasDragStartHeight = true;
        _dragging = true;
        if (placer != null)
        {
            placer.NotifyUserMovedUi();
        }

        ApplyVisual(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging) return;

        if (_activeRayTransform != null)
        {
            MoveFromRay(new Ray(_activeRayTransform.position, _activeRayTransform.forward));
            return;
        }

        if (TryGetEventRay(eventData, out var ray))
        {
            MoveFromRay(ray);
            return;
        }

        var hitPoint = ResolvePointerWorldPoint(eventData, targetRoot != null ? targetRoot.position : transform.position);
        MoveTargetToWorldPoint(hitPoint + _dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        _activeRayTransform = null;
        _hasDragStartHeight = false;
        ApplyVisual(false);
    }

    private void MoveFromRay(Ray ray)
    {
        if (targetRoot == null) return;
        if (!_dragPlane.Raycast(ray, out var distance)) return;
        if (distance < 0f || distance > 8f) return;

        MoveTargetToWorldPoint(ray.GetPoint(distance) + _dragOffset);
    }

    public void MoveTargetToWorldPoint(Vector3 position)
    {
        if (targetRoot == null) return;

        var headPosition = headTransform != null ? headTransform.position : position - targetRoot.forward;
        var horizontal = position - headPosition;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 0.0001f)
        {
            horizontal = GetHeadYawForward();
        }

        var distance = Mathf.Clamp(horizontal.magnitude, Mathf.Max(0.1f, minDistanceMeters), Mathf.Max(minDistanceMeters, maxDistanceMeters));
        horizontal = horizontal.normalized * distance;

        position.x = headPosition.x + horizontal.x;
        position.z = headPosition.z + horizontal.z;
        position.y = ConstrainHeight(position.y, headPosition);
        if (lockWorldHeight)
        {
            position.y = _hasDragStartHeight ? _lockedWorldY : targetRoot.position.y;
        }

        targetRoot.position = position;
        var toPanel = targetRoot.position - headPosition;
        toPanel.y = 0f;
        if (toPanel.sqrMagnitude < 0.0001f)
        {
            toPanel = GetHeadYawForward();
        }

        targetRoot.rotation = Quaternion.LookRotation(toPanel.normalized, Vector3.up);
    }

    public static WorldSpaceUiRayDragHandle EnsureOnSurface(Graphic surface, Transform targetRoot, ComfortWorldSpaceUIPlacer placer = null)
    {
        if (surface == null || targetRoot == null) return null;

        var baseColor = surface.color;
        surface.raycastTarget = true;

        var handle = surface.GetComponent<WorldSpaceUiRayDragHandle>();
        if (handle == null)
        {
            handle = surface.gameObject.AddComponent<WorldSpaceUiRayDragHandle>();
        }

        handle.placer = placer;
        handle.targetRoot = targetRoot;
        handle.headTransform = placer != null ? placer.headTransform : handle.headTransform;
        if (handle.headTransform == null && Camera.main != null)
        {
            handle.headTransform = Camera.main.transform;
        }

        handle.handleGraphic = surface;
        handle.normalColor = baseColor;
        handle.activeColor = new Color(
            Mathf.Min(1f, baseColor.r + 0.12f),
            Mathf.Min(1f, baseColor.g + 0.16f),
            Mathf.Min(1f, baseColor.b + 0.18f),
            Mathf.Min(1f, Mathf.Max(baseColor.a, 0.72f)));
        handle.minDistanceMeters = 0.9f;
        handle.maxDistanceMeters = 3.8f;
        handle.lockWorldHeight = true;
        handle.lockHeightToComfortOffset = true;
        handle.lockedHeightToleranceMeters = 0.08f;
        surface.color = baseColor;
        return handle;
    }

    private float ConstrainHeight(float requestedY, Vector3 headPosition)
    {
        var absoluteMin = headPosition.y + minHeightFromHead;
        var absoluteMax = headPosition.y + maxHeightFromHead;
        if (!lockHeightToComfortOffset)
        {
            return Mathf.Clamp(requestedY, absoluteMin, absoluteMax);
        }

        var preferredY = _hasDragStartHeight
            ? _dragStartHeight
            : headPosition.y + (placer != null ? placer.hmdHeightOffsetMeters : 0f);
        preferredY = Mathf.Clamp(preferredY, absoluteMin, absoluteMax);

        var tolerance = Mathf.Max(0f, lockedHeightToleranceMeters);
        var minY = Mathf.Max(absoluteMin, preferredY - tolerance);
        var maxY = Mathf.Min(absoluteMax, preferredY + tolerance);
        if (minY > maxY)
        {
            return preferredY;
        }

        return Mathf.Clamp(requestedY, minY, maxY);
    }

    private Vector3 ResolvePointerWorldPoint(PointerEventData eventData, Vector3 fallback)
    {
        if (eventData != null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            var worldPosition = eventData.pointerCurrentRaycast.worldPosition;
            if (worldPosition.sqrMagnitude > 0.0001f)
            {
                return worldPosition;
            }
        }

        return fallback;
    }

    private bool TryGetEventRay(PointerEventData eventData, out Ray ray)
    {
        ray = new Ray(Vector3.zero, Vector3.forward);
        if (eventData == null) return false;

        var eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
        if (eventCamera == null &&
            eventData.pointerCurrentRaycast.module != null)
        {
            eventCamera = eventData.pointerCurrentRaycast.module.eventCamera;
        }

        if (eventCamera == null) return false;

        ray = eventCamera.ScreenPointToRay(eventData.position);
        return true;
    }

    private Transform FindBestControllerRay(Vector3 hitPoint)
    {
        var left = FindTransformByName("Left Controller");
        var right = FindTransformByName("Right Controller");
        var leftDistance = DistancePointToRay(left, hitPoint);
        var rightDistance = DistancePointToRay(right, hitPoint);

        if (left == null && right == null) return null;
        if (left == null) return right;
        if (right == null) return left;
        return leftDistance <= rightDistance ? left : right;
    }

    private float DistancePointToRay(Transform rayTransform, Vector3 point)
    {
        if (rayTransform == null) return float.PositiveInfinity;

        var ray = new Ray(rayTransform.position, rayTransform.forward);
        var projected = ray.origin + ray.direction.normalized * Mathf.Max(0f, Vector3.Dot(point - ray.origin, ray.direction.normalized));
        return Vector3.Distance(point, projected);
    }

    private Vector3 GetDragPlaneNormal()
    {
        if (targetRoot == null) return -GetHeadYawForward();

        var normal = Vector3.ProjectOnPlane(targetRoot.forward, Vector3.up);
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = -GetHeadYawForward();
        }

        return normal.normalized;
    }

    private Vector3 GetHeadYawForward()
    {
        var forward = Vector3.forward;
        if (headTransform != null)
        {
            forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private void ResolveReferences()
    {
        if (placer == null)
        {
            placer = GetComponentInParent<ComfortWorldSpaceUIPlacer>();
        }

        if (targetRoot == null && placer != null)
        {
            targetRoot = placer.uiRoot != null ? placer.uiRoot : placer.transform;
        }

        if (targetRoot == null)
        {
            targetRoot = transform.root;
        }

        if (headTransform == null && placer != null)
        {
            headTransform = placer.headTransform;
        }

        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        if (handleGraphic == null)
        {
            handleGraphic = GetComponent<Graphic>();
        }
    }

    private void ApplyVisual(bool active)
    {
        if (handleGraphic != null)
        {
            handleGraphic.color = active ? activeColor : normalColor;
        }
    }

    private static Transform FindTransformByName(string objectName)
    {
        var transforms = FindObjectsOfType<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }
}
