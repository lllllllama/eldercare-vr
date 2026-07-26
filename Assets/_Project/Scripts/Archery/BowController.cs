using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BowController : MonoBehaviour
{
    [Header("双手柄绑定")]
    public Transform bowHandTransform;
    public Transform stringHandTransform;
    public XRNode stringHandNode = XRNode.RightHand;
    public MonoBehaviour drawInputSourceBehaviour;
    public bool autoCreateDrawInputSource = true;

    [Header("持弓姿态")]
    public Vector3 bowHandPositionOffset = Vector3.zero;
    public Vector3 bowHandRotationOffsetEuler = Vector3.zero;

    [Header("弓体部件")]
    public Transform nockRest;
    public Transform stringTopAnchor;
    public Transform stringBottomAnchor;
    public LineRenderer stringLine;
    public Transform nockedArrowVisual;

    [Header("拉弓参数")]
    public float restSeparationMeters = ArcheryGeometry.DrawRestSeparationMeters;
    public float maxDrawLengthMeters = ArcheryGeometry.MaxDrawLengthMeters;
    public float minFireDraw01 = ArcheryGeometry.MinFireDraw01;
    public float nockCatchRadiusMeters = ArcheryGeometry.NockCatchRadiusMeters;
    public bool requireNockProximity = true;

    [Header("放箭参数")]
    public GameObject arrowTemplate;
    public Transform arrowContainer;
    public float minLaunchSpeed = ArcheryGeometry.MinLaunchSpeedMetersPerSecond;
    public float maxLaunchSpeed = ArcheryGeometry.MaxLaunchSpeedMetersPerSecond;
    public bool firingEnabled = true;

    [Header("震动反馈")]
    public bool enableHaptics = true;
    public float drawHapticInterval01 = 0.08f;
    public float releaseHapticAmplitude = 0.8f;
    public float releaseHapticSeconds = 0.08f;

    private readonly List<InputDevice> _hapticDevices = new List<InputDevice>();
    private IGripInputSource _drawInput;
    private bool _isDrawing;
    private bool _gripWasPressed;
    private float _lastReportedDraw01;
    private float _lastHapticDraw01;
    private ArcherySolver.DrawState _currentDraw;

    public bool IsDrawing => _isDrawing;
    public float CurrentDraw01 => _isDrawing ? _currentDraw.draw01 : 0f;

    private void Awake()
    {
        ResolveDrawInputSource();
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

            AimBowAlongDraw();
            UpdateDrawVisuals();
            ReportDrawProgress();

            if (!gripPressed)
            {
                if (firingEnabled && _currentDraw.canFire)
                {
                    Fire();
                }
                else
                {
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
        SendHaptic(0.25f, 0.04f);
    }

    private void Fire()
    {
        _isDrawing = false;

        var launchOrigin = ComputeNockPoint(_currentDraw);
        var velocity = ArcherySolver.ComputeReleaseVelocity(_currentDraw, minLaunchSpeed, maxLaunchSpeed);
        var arrow = SpawnArrow();
        if (arrow != null && velocity.sqrMagnitude > 0.001f)
        {
            arrow.Launch(launchOrigin, velocity);
            ArcheryEvents.ArrowReleased(new ArrowReleasedInfo(arrow.gameObject, launchOrigin, velocity, _currentDraw.draw01));
            SendHaptic(releaseHapticAmplitude, releaseHapticSeconds);
        }

        _lastReportedDraw01 = 0f;
        _lastHapticDraw01 = 0f;
        ArcheryEvents.DrawChanged(0f);
        UpdateRestVisuals();
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

        if (nockedArrowVisual != null && nockedArrowVisual.gameObject.activeSelf)
        {
            nockedArrowVisual.gameObject.SetActive(false);
        }
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
            SendHaptic(0.12f + 0.35f * _currentDraw.draw01, 0.03f);
        }
    }

    private void ResolveDrawInputSource()
    {
        if (drawInputSourceBehaviour is IGripInputSource assigned)
        {
            _drawInput = assigned;
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

    private void SendHaptic(float amplitude, float seconds)
    {
        if (!enableHaptics) return;

        InputDevices.GetDevicesAtXRNode(stringHandNode, _hapticDevices);
        foreach (var device in _hapticDevices)
        {
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, seconds));
            }
        }
    }
}
