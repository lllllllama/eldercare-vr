using UnityEngine;
using UnityEngine.UI;

public class ComfortWorldSpaceUIPlacer : MonoBehaviour
{
    public Transform headTransform;
    public Transform uiRoot;
    public float distanceMeters = 2f;
    public float hmdHeightOffsetMeters = -0.1f;
    public bool placeOnStart = true;
    public bool placeOnEnable = false;
    public bool recenterDuringStartup = true;
    public float startupRecenterSeconds = 1.25f;
    public int startupRecenterFrames = 18;
    public bool enableRayDrag = true;
    public bool enableThumbstickNavigation = true;
    public bool invertThumbstickHorizontal = false;
    public bool comfortFollowEnabled;
    public float followYawThresholdDegrees = 35f;
    public float followPositionThresholdMeters = 0.8f;
    public float followSmoothTime = 0.35f;
    public float followRotationSlerpSpeed = 4f;
    public float maxFollowSpeedMetersPerSecond = 1.25f;

    private Vector3 _followVelocity;
    private Vector3 _followTargetPosition;
    private Quaternion _followTargetRotation;
    private bool _hasFollowTarget;
    private bool _started;
    private bool _startupRecenterActive;
    private float _startupRecenterUntilTime;
    private int _startupRecenterFramesRemaining;

    private Transform TargetRoot => uiRoot != null ? uiRoot : transform;

    private void Awake()
    {
        ResolveReferences();
        EnsureWorldSpaceInteractionHelpers();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureWorldSpaceInteractionHelpers();
        _hasFollowTarget = false;

        if (_started && placeOnEnable)
        {
            PlaceInFrontOfUser();
            BeginStartupRecenterWindow();
        }
    }

    private void Start()
    {
        _started = true;

        if (placeOnStart)
        {
            PlaceInFrontOfUser();
            BeginStartupRecenterWindow();
        }

        EnsureWorldSpaceInteractionHelpers();
    }

    private void LateUpdate()
    {
        RefreshStartupPlacementIfNeeded();

        if (comfortFollowEnabled)
        {
            UpdateComfortFollow();
        }
    }

    public void PlaceInFrontOfUser()
    {
        if (!TryGetComfortPose(out var position, out var rotation)) return;

        var target = TargetRoot;
        target.position = position;
        target.rotation = rotation;
        _followTargetPosition = position;
        _followTargetRotation = rotation;
        _followVelocity = Vector3.zero;
        _hasFollowTarget = false;
    }

    public void ResetUiPosition()
    {
        PlaceInFrontOfUser();
    }

    public void PlaceOnOpen()
    {
        PlaceInFrontOfUser();
    }

    public void NotifyUserMovedUi()
    {
        _startupRecenterActive = false;
        _hasFollowTarget = false;
        _followVelocity = Vector3.zero;
    }

    public void BeginStartupRecenterWindow()
    {
        if (!recenterDuringStartup)
        {
            _startupRecenterActive = false;
            return;
        }

        _startupRecenterActive = true;
        _startupRecenterUntilTime = Time.unscaledTime + Mathf.Max(0f, startupRecenterSeconds);
        _startupRecenterFramesRemaining = Mathf.Max(0, startupRecenterFrames);
    }

    public void RefreshStartupPlacementIfNeeded()
    {
        if (!_startupRecenterActive) return;

        var stillWithinTime = Time.unscaledTime <= _startupRecenterUntilTime;
        var stillWithinFrames = _startupRecenterFramesRemaining > 0;
        if (!stillWithinTime && !stillWithinFrames)
        {
            _startupRecenterActive = false;
            return;
        }

        PlaceInFrontOfUser();
        _startupRecenterFramesRemaining--;
    }

    private void UpdateComfortFollow()
    {
        var target = TargetRoot;
        if (!TryGetComfortPose(out var desiredPosition, out var desiredRotation)) return;

        if (!_hasFollowTarget && ShouldRefreshComfortTarget(target, desiredPosition))
        {
            _followTargetPosition = desiredPosition;
            _followTargetRotation = desiredRotation;
            _hasFollowTarget = true;
        }

        if (!_hasFollowTarget) return;

        target.position = Vector3.SmoothDamp(
            target.position,
            _followTargetPosition,
            ref _followVelocity,
            Mathf.Max(0.01f, followSmoothTime),
            Mathf.Max(0.01f, maxFollowSpeedMetersPerSecond));

        target.rotation = Quaternion.Slerp(
            target.rotation,
            _followTargetRotation,
            Mathf.Max(0.01f, followRotationSlerpSpeed) * Time.deltaTime);

        if (Vector3.Distance(target.position, _followTargetPosition) < 0.02f &&
            Quaternion.Angle(target.rotation, _followTargetRotation) < 1f)
        {
            _hasFollowTarget = false;
            _followVelocity = Vector3.zero;
        }
    }

    private bool ShouldRefreshComfortTarget(Transform target, Vector3 desiredPosition)
    {
        if (headTransform == null || target == null) return false;

        var desiredDirection = GetHeadYawForward();
        var currentDirection = Vector3.ProjectOnPlane(target.position - headTransform.position, Vector3.up);
        if (currentDirection.sqrMagnitude < 0.0001f)
        {
            currentDirection = desiredDirection;
        }

        currentDirection.Normalize();
        var yawDelta = Vector3.Angle(currentDirection, desiredDirection);
        var positionDelta = Vector3.Distance(target.position, desiredPosition);
        return yawDelta > Mathf.Max(0f, followYawThresholdDegrees) ||
               positionDelta > Mathf.Max(0f, followPositionThresholdMeters);
    }

    private bool TryGetComfortPose(out Vector3 position, out Quaternion rotation)
    {
        ResolveReferences();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (headTransform == null)
        {
            return false;
        }

        var forward = GetHeadYawForward();
        position = headTransform.position + forward * Mathf.Max(0.01f, distanceMeters);
        position.y = headTransform.position.y + hmdHeightOffsetMeters;
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
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
            forward = TargetRoot != null
                ? Vector3.ProjectOnPlane(TargetRoot.forward, Vector3.up)
                : Vector3.forward;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private void ResolveReferences()
    {
        if (uiRoot == null)
        {
            uiRoot = transform;
        }

        if (headTransform != null) return;

        var camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
        if (camera != null)
        {
            headTransform = camera.transform;
        }
    }

    public void EnsureWorldSpaceInteractionHelpers()
    {
        var root = TargetRoot as RectTransform;
        if (root == null) return;

        if (enableRayDrag)
        {
            EnsureRayDragHandle(root);
        }

        if (enableThumbstickNavigation)
        {
            var navigator = root.GetComponent<WorldSpaceUiThumbstickNavigator>();
            if (navigator == null)
            {
                navigator = root.gameObject.AddComponent<WorldSpaceUiThumbstickNavigator>();
            }

            navigator.selectableRoot = root;
            navigator.invertHorizontalInput = invertThumbstickHorizontal;
            navigator.disableBuiltInSelectableNavigation = true;
            navigator.ConfigureSelectableNavigation();
        }
    }

    private void EnsureRayDragHandle(RectTransform root)
    {
        var existing = root.Find("RayDragHandle");
        if (existing != null)
        {
            ConfigureRayDragHandle(existing.gameObject, root);
            return;
        }

        var handle = new GameObject("RayDragHandle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(WorldSpaceUiRayDragHandle));
        handle.transform.SetParent(root, false);
        ConfigureRayDragHandle(handle, root);
    }

    private void ConfigureRayDragHandle(GameObject handle, RectTransform root)
    {
        if (handle == null || root == null) return;

        var rect = handle.GetComponent<RectTransform>();
        if (rect != null)
        {
            var rootHeight = Mathf.Max(300f, root.rect.height);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 18f);
            rect.anchoredPosition = new Vector2(0f, rootHeight * 0.5f - 34f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        var image = handle.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            image.color = new Color(0.35f, 0.95f, 1f, 0.34f);
        }

        var outline = handle.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0.68f, 1f, 1f, 0.28f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        var drag = handle.GetComponent<WorldSpaceUiRayDragHandle>();
        if (drag != null)
        {
            drag.placer = this;
            drag.targetRoot = root;
            drag.headTransform = headTransform;
            drag.handleGraphic = image;
        }
    }
}
