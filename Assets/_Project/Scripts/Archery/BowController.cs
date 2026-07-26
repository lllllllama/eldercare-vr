using UnityEngine;
using UnityEngine.XR;

public class BowController : MonoBehaviour
{
    [Header("双手柄绑定")]
    public Transform bowHandTransform;
    public Transform stringHandTransform;
    public XRNode bowHandNode = XRNode.LeftHand;
    public XRNode stringHandNode = XRNode.RightHand;
    public bool bowInLeftHand = true;
    public MonoBehaviour drawInputSourceBehaviour;
    public bool autoCreateDrawInputSource = true;

    [Header("持弓姿态")]
    public Vector3 bowHandPositionOffset = Vector3.zero;
    public Vector3 bowHandRotationOffsetEuler = Vector3.zero;

    [Header("弓体部件")]
    public Transform nockRest;
    public Transform stringTopAnchor;
    public Transform stringBottomAnchor;
    public Transform upperLimbTransform;
    public Transform lowerLimbTransform;
    public LineRenderer stringLine;
    public Transform nockedArrowVisual;
    public float limbBendDegrees = ArcheryGeometry.BowLimbBendDegrees;

    [Header("拉弓参数")]
    public float restSeparationMeters = ArcheryGeometry.DrawRestSeparationMeters;
    public float maxDrawLengthMeters = ArcheryGeometry.MaxDrawLengthMeters;
    public float minFireDraw01 = ArcheryGeometry.MinFireDraw01;
    public float nockCatchRadiusMeters = ArcheryGeometry.NockCatchRadiusMeters;
    public bool requireNockProximity = true;
    public float aimSmoothingSeconds = ArcheryGeometry.AimSmoothingSeconds;

    [Header("放箭参数")]
    public GameObject arrowTemplate;
    public Transform arrowContainer;
    public float minLaunchSpeed = ArcheryGeometry.MinLaunchSpeedMetersPerSecond;
    public float maxLaunchSpeed = ArcheryGeometry.MaxLaunchSpeedMetersPerSecond;
    public bool firingEnabled = true;

    [Header("适老辅助")]
    public Transform aimAssistTarget;
    public float aimAssistMaxDegrees;
    public ArcheryTrajectoryHint trajectoryHint;
    public bool showTrajectoryPreview = true;

    [Header("震动反馈")]
    public bool enableHaptics = true;
    public float drawHapticInterval01 = 0.08f;
    public float releaseHapticAmplitude = 0.85f;
    public float releaseHapticSeconds = 0.09f;

    private IGripInputSource _drawInput;
    private bool _isDrawing;
    private bool _gripWasPressed;
    private float _lastReportedDraw01;
    private float _lastHapticDraw01;
    private Vector3 _smoothedAimDirection = Vector3.forward;
    private Quaternion _upperLimbRestRotation;
    private Quaternion _lowerLimbRestRotation;
    private bool _limbRestRotationsCaptured;
    private ArcherySolver.DrawState _currentDraw;

    public bool IsDrawing => _isDrawing;
    public float CurrentDraw01 => _isDrawing ? _currentDraw.draw01 : 0f;

    private void Awake()
    {
        ResolveDrawInputSource();
        CaptureLimbRestRotations();
    }

    private void OnDisable()
    {
        CancelDraw();
        _gripWasPressed = false;
    }

    private void LateUpdate()
    {
        FollowBowHand();

        if (bowHandTransform == null || stringHandTransform == null)
        {
            UpdateRestVisuals();
            return;
        }

        var gripPressed = _drawInput != null && _drawInput.IsGripPressed;

        if (!_isDrawing)
        {
            if (gripPressed && !_gripWasPressed && firingEnabled && IsStringHandNearNock())
            {
                BeginDraw();
            }
        }

        if (_isDrawing)
        {
            _currentDraw = ArcherySolver.ComputeDraw(
                bowHandTransform.position,
                stringHandTransform.position,
                bowHandTransform.forward,
                restSeparationMeters,
                maxDrawLengthMeters,
                minFireDraw01);
            _currentDraw.aimDirection = SmoothAimDirection(_currentDraw.aimDirection);

            AimBowAlongDraw();
            UpdateDrawVisuals();
            UpdateTrajectoryPreview();
            ReportDrawProgress();

            if (!gripPressed)
            {
                if (firingEnabled && _currentDraw.canFire)
                {
                    Fire();
                }
                else
                {
                    // 拉弓不足就松手：给一个轻微“失败”提示震动，让玩家知道要再拉开一点。
                    SendHaptic(stringHandNode, 0.15f, 0.03f);
                    CancelDraw();
                }
            }
            else if (!firingEnabled)
            {
                CancelDraw();
            }
        }
        else
        {
            UpdateRestVisuals();
        }

        _gripWasPressed = gripPressed;
    }

    public void SetFiringEnabled(bool value)
    {
        firingEnabled = value;
        if (!value && _isDrawing)
        {
            CancelDraw();
        }
    }

    public void SetAimAssist(Transform target, float maxCorrectionDegrees, bool showPreview)
    {
        aimAssistTarget = target;
        aimAssistMaxDegrees = Mathf.Max(0f, maxCorrectionDegrees);
        showTrajectoryPreview = showPreview;
        if (!showPreview && trajectoryHint != null)
        {
            trajectoryHint.Hide();
        }
    }

    public void SetBowInLeftHand(bool leftHanded)
    {
        if (bowInLeftHand == leftHanded) return;

        SwapHands();
    }

    public void SwapHands()
    {
        CancelDraw();

        var handTransform = bowHandTransform;
        bowHandTransform = stringHandTransform;
        stringHandTransform = handTransform;

        var handNode = bowHandNode;
        bowHandNode = stringHandNode;
        stringHandNode = handNode;

        bowInLeftHand = !bowInLeftHand;
        ApplyStringHandNodeToInputSource();
    }

    public void CancelDraw()
    {
        _isDrawing = false;
        _lastReportedDraw01 = 0f;
        _lastHapticDraw01 = 0f;
        ArcheryEvents.DrawChanged(0f);
        UpdateRestVisuals();
    }

    private void BeginDraw()
    {
        _isDrawing = true;
        _lastReportedDraw01 = 0f;
        _lastHapticDraw01 = 0f;
        _smoothedAimDirection = ComputeRawAimDirection();
        ArcheryEvents.ArrowNocked();
        SendHaptic(stringHandNode, 0.3f, 0.045f);
        SendHaptic(bowHandNode, 0.18f, 0.035f);
    }

    private void Fire()
    {
        _isDrawing = false;

        var launchOrigin = ComputeNockPoint(_currentDraw);
        var velocity = ArcherySolver.ComputeReleaseVelocity(_currentDraw, minLaunchSpeed, maxLaunchSpeed);
        velocity = ApplyAimAssist(launchOrigin, velocity);

        var arrow = SpawnArrow();
        if (arrow != null && velocity.sqrMagnitude > 0.001f)
        {
            arrow.Launch(launchOrigin, velocity);
            ArcheryEvents.ArrowReleased(new ArrowReleasedInfo(arrow.gameObject, launchOrigin, velocity, _currentDraw.draw01));
            SendHaptic(stringHandNode, releaseHapticAmplitude, releaseHapticSeconds);
            SendHaptic(bowHandNode, releaseHapticAmplitude * 0.7f, releaseHapticSeconds);
        }

        _lastReportedDraw01 = 0f;
        _lastHapticDraw01 = 0f;
        ArcheryEvents.DrawChanged(0f);
        UpdateRestVisuals();
    }

    private Vector3 ApplyAimAssist(Vector3 origin, Vector3 velocity)
    {
        if (aimAssistTarget == null || aimAssistMaxDegrees <= 0f) return velocity;

        return ArcherySolver.ComputeAssistedVelocity(
            origin,
            velocity,
            aimAssistTarget.position,
            aimAssistMaxDegrees,
            ArcheryGeometry.ArrowGravityMetersPerSecondSquared);
    }

    private ArrowProjectile SpawnArrow()
    {
        if (arrowTemplate == null) return null;

        var parent = arrowContainer != null ? arrowContainer : transform.parent;
        var arrowObject = Instantiate(arrowTemplate, parent);
        arrowObject.name = "Arrow";
        arrowObject.SetActive(true);
        return arrowObject.GetComponent<ArrowProjectile>();
    }

    private void FollowBowHand()
    {
        if (bowHandTransform == null) return;

        transform.position = bowHandTransform.TransformPoint(bowHandPositionOffset);
        if (!_isDrawing)
        {
            transform.rotation = bowHandTransform.rotation * Quaternion.Euler(bowHandRotationOffsetEuler);
        }
    }

    private void AimBowAlongDraw()
    {
        if (bowHandTransform == null) return;

        var up = bowHandTransform.up;
        if (Mathf.Abs(Vector3.Dot(up.normalized, _currentDraw.aimDirection)) > 0.98f)
        {
            up = Vector3.up;
        }

        transform.rotation = Quaternion.LookRotation(_currentDraw.aimDirection, up);
    }

    private Vector3 ComputeRawAimDirection()
    {
        if (bowHandTransform == null || stringHandTransform == null) return transform.forward;

        var offset = bowHandTransform.position - stringHandTransform.position;
        return offset.sqrMagnitude > 0.000001f ? offset.normalized : transform.forward;
    }

    private Vector3 SmoothAimDirection(Vector3 rawAimDirection)
    {
        if (aimSmoothingSeconds <= 0.0001f) return rawAimDirection;

        var blend = Time.deltaTime / (aimSmoothingSeconds + Time.deltaTime);
        _smoothedAimDirection = Vector3.Slerp(_smoothedAimDirection, rawAimDirection, blend).normalized;
        return _smoothedAimDirection;
    }

    private bool IsStringHandNearNock()
    {
        if (!requireNockProximity) return true;
        if (stringHandTransform == null) return false;

        var anchor = nockRest != null ? nockRest.position : transform.position;
        return Vector3.Distance(stringHandTransform.position, anchor) <= nockCatchRadiusMeters;
    }

    private Vector3 ComputeNockPoint(ArcherySolver.DrawState state)
    {
        var rest = GetStringRestPoint();
        return rest - state.aimDirection * state.drawLengthMeters;
    }

    private Vector3 GetStringRestPoint()
    {
        if (nockRest != null) return nockRest.position;
        if (stringTopAnchor != null && stringBottomAnchor != null)
        {
            return (stringTopAnchor.position + stringBottomAnchor.position) * 0.5f;
        }

        return transform.position;
    }

    private void UpdateDrawVisuals()
    {
        var nockPoint = ComputeNockPoint(_currentDraw);
        UpdateStringLine(nockPoint);
        ApplyLimbBend(_currentDraw.draw01);

        if (nockedArrowVisual != null)
        {
            nockedArrowVisual.gameObject.SetActive(true);
            nockedArrowVisual.position = nockPoint;
            nockedArrowVisual.rotation = Quaternion.LookRotation(_currentDraw.aimDirection, transform.up);
        }
    }

    private void UpdateRestVisuals()
    {
        UpdateStringLine(GetStringRestPoint());
        ApplyLimbBend(0f);

        if (nockedArrowVisual != null && nockedArrowVisual.gameObject.activeSelf)
        {
            nockedArrowVisual.gameObject.SetActive(false);
        }

        if (trajectoryHint != null)
        {
            trajectoryHint.Hide();
        }
    }

    private void UpdateTrajectoryPreview()
    {
        if (trajectoryHint == null) return;

        // 只有当前松手真的会放箭时才显示弹道，避免“有弧线却射不出去”的矛盾反馈。
        if (!showTrajectoryPreview || !_currentDraw.canFire || !firingEnabled)
        {
            trajectoryHint.Hide();
            return;
        }

        var origin = ComputeNockPoint(_currentDraw);
        var velocity = _currentDraw.aimDirection *
                       ArcherySolver.ComputeLaunchSpeed(_currentDraw.draw01, minLaunchSpeed, maxLaunchSpeed);
        velocity = ApplyAimAssist(origin, velocity);
        trajectoryHint.ShowPreview(origin, velocity);
    }

    private void ApplyLimbBend(float draw01)
    {
        if (!_limbRestRotationsCaptured) return;

        var bend = limbBendDegrees * Mathf.Clamp01(draw01);
        if (upperLimbTransform != null)
        {
            upperLimbTransform.localRotation = _upperLimbRestRotation * Quaternion.Euler(-bend, 0f, 0f);
        }

        if (lowerLimbTransform != null)
        {
            lowerLimbTransform.localRotation = _lowerLimbRestRotation * Quaternion.Euler(bend, 0f, 0f);
        }
    }

    private void CaptureLimbRestRotations()
    {
        if (_limbRestRotationsCaptured) return;

        _upperLimbRestRotation = upperLimbTransform != null ? upperLimbTransform.localRotation : Quaternion.identity;
        _lowerLimbRestRotation = lowerLimbTransform != null ? lowerLimbTransform.localRotation : Quaternion.identity;
        _limbRestRotationsCaptured = true;
    }

    private void UpdateStringLine(Vector3 nockPoint)
    {
        if (stringLine == null || stringTopAnchor == null || stringBottomAnchor == null) return;

        stringLine.useWorldSpace = true;
        stringLine.positionCount = 3;
        stringLine.SetPosition(0, stringTopAnchor.position);
        stringLine.SetPosition(1, nockPoint);
        stringLine.SetPosition(2, stringBottomAnchor.position);
    }

    private void ReportDrawProgress()
    {
        if (Mathf.Abs(_currentDraw.draw01 - _lastReportedDraw01) > 0.01f)
        {
            _lastReportedDraw01 = _currentDraw.draw01;
            ArcheryEvents.DrawChanged(_currentDraw.draw01);
        }

        if (_currentDraw.draw01 - _lastHapticDraw01 >= drawHapticInterval01)
        {
            _lastHapticDraw01 = _currentDraw.draw01;
            SendHaptic(stringHandNode, 0.12f + 0.38f * _currentDraw.draw01, 0.03f);
            SendHaptic(bowHandNode, 0.06f + 0.2f * _currentDraw.draw01, 0.025f);
        }
    }

    private void ResolveDrawInputSource()
    {
        if (drawInputSourceBehaviour is IGripInputSource assigned)
        {
            _drawInput = assigned;
            ApplyStringHandNodeToInputSource();
            return;
        }

        if (!autoCreateDrawInputSource) return;

        var created = gameObject.GetComponent<PicoGripOrTriggerInputSource>();
        if (created == null)
        {
            created = gameObject.AddComponent<PicoGripOrTriggerInputSource>();
        }

        created.controllerNode = stringHandNode;
        drawInputSourceBehaviour = created;
        _drawInput = created;
    }

    private void ApplyStringHandNodeToInputSource()
    {
        if (drawInputSourceBehaviour is PicoGripOrTriggerInputSource gripOrTrigger)
        {
            gripOrTrigger.controllerNode = stringHandNode;
        }
        else if (drawInputSourceBehaviour is PicoGripInputSource grip)
        {
            grip.controllerNode = stringHandNode;
        }
    }

    private void SendHaptic(XRNode node, float amplitude, float seconds)
    {
        if (!enableHaptics) return;

        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid &&
            device.TryGetHapticCapabilities(out var capabilities) &&
            capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, seconds));
        }
    }
}
