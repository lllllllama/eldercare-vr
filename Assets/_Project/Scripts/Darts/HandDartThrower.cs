using UnityEngine;
using UnityEngine.XR;

public class HandDartThrower : MonoBehaviour
{
    [Header("投掷手绑定")]
    public Transform throwHandTransform;
    public Transform offHandTransform;
    public XRNode throwHandNode = XRNode.RightHand;
    public XRNode offHandNode = XRNode.LeftHand;
    public bool throwWithRightHand = true;
    public MonoBehaviour holdInputSourceBehaviour;
    public bool autoCreateHoldInputSource = true;

    [Header("握持姿态")]
    public Transform heldDartVisual;
    public float holdForwardOffsetMeters = DartsGeometry.HoldForwardOffsetMeters;

    [Header("投掷参数")]
    public GameObject dartTemplate;
    public Transform dartContainer;
    public float handSpeedMultiplier = DartsGeometry.HandSpeedMultiplier;
    public float minThrowHandSpeed = DartsGeometry.MinThrowHandSpeedMetersPerSecond;
    public float minDartSpeed = DartsGeometry.MinDartSpeedMetersPerSecond;
    public float maxDartSpeed = DartsGeometry.MaxDartSpeedMetersPerSecond;
    public float velocitySampleWindowSeconds = DartsGeometry.VelocitySampleWindowSeconds;
    public bool throwsEnabled = true;

    [Header("适老辅助")]
    public Transform aimAssistTarget;
    public float aimAssistMaxDegrees;
    [Tooltip("松手瞬间手速已衰减时，回溯挥臂峰值窗口速度的宽容时长（晚松手补偿）。")]
    public float lateReleaseForgivenessSeconds = 0.25f;
    [Tooltip("指针悬停在此 UI 上时不开始握镖，避免点按钮误抓/误投。")]
    public MonoBehaviour uiHoverGuardBehaviour;

    [Header("震动反馈")]
    public bool enableHaptics = true;
    public float grabHapticAmplitude = 0.3f;
    public float throwHapticAmplitude = 0.85f;

    private const int SampleCapacity = 16;

    private readonly Vector3[] _samplePositions = new Vector3[SampleCapacity];
    private readonly float[] _sampleTimes = new float[SampleCapacity];
    private int _sampleCount;
    private int _sampleHead;

    private IGripInputSource _holdInput;
    private bool _isHolding;
    private bool _gripWasPressed;
    private Vector3 _peakWindowVelocity;
    private float _peakWindowSpeed;
    private float _peakTime;

    public bool IsHolding => _isHolding;

    public float CurrentHandSpeed
    {
        get { return _isHolding ? ComputeTrackedVelocity().magnitude : 0f; }
    }

    private void Awake()
    {
        ResolveHoldInputSource();
    }

    private void OnDisable()
    {
        CancelHold();
        _gripWasPressed = false;
    }

    private void LateUpdate()
    {
        if (throwHandTransform == null)
        {
            UpdateHeldVisual(false);
            return;
        }

        var gripPressed = _holdInput != null && _holdInput.IsGripPressed;

        if (!_isHolding)
        {
            if (gripPressed && !_gripWasPressed && throwsEnabled && !IsPointerOverUi())
            {
                BeginHold();
            }
        }

        if (_isHolding)
        {
            RecordSample(throwHandTransform.position, Time.time);
            TrackPeakSwingVelocity();
            UpdateHeldVisual(true);

            if (!gripPressed)
            {
                ReleaseHold();
            }
            else if (!throwsEnabled)
            {
                CancelHold();
            }
        }
        else
        {
            UpdateHeldVisual(false);
        }

        _gripWasPressed = gripPressed;
    }

    public void SetThrowsEnabled(bool value)
    {
        throwsEnabled = value;
        if (!value && _isHolding)
        {
            CancelHold();
        }
    }

    public void SetAimAssist(Transform target, float maxCorrectionDegrees)
    {
        aimAssistTarget = target;
        aimAssistMaxDegrees = Mathf.Max(0f, maxCorrectionDegrees);
    }

    public void SetThrowWithRightHand(bool rightHanded)
    {
        if (throwWithRightHand == rightHanded) return;

        SwapHands();
    }

    public void SwapHands()
    {
        CancelHold();

        var handTransform = throwHandTransform;
        throwHandTransform = offHandTransform;
        offHandTransform = handTransform;

        var handNode = throwHandNode;
        throwHandNode = offHandNode;
        offHandNode = handNode;

        throwWithRightHand = !throwWithRightHand;
        ApplyThrowHandNodeToInputSource();
    }

    public void CancelHold()
    {
        if (_isHolding)
        {
            DartsEvents.DartHoldCancelled();
        }

        _isHolding = false;
        _sampleCount = 0;
        _sampleHead = 0;
        UpdateHeldVisual(false);
    }

    private void BeginHold()
    {
        _isHolding = true;
        _sampleCount = 0;
        _sampleHead = 0;
        _peakWindowVelocity = Vector3.zero;
        _peakWindowSpeed = 0f;
        _peakTime = Time.time;
        RecordSample(throwHandTransform.position, Time.time);
        DartsEvents.DartGrabbed();
        SendHaptic(throwHandNode, grabHapticAmplitude, 0.04f);
    }

    private void ReleaseHold()
    {
        // 晚松手宽容：老年玩家常在挥臂末段才松手，此刻手已减速甚至转向。
        // 回溯挥臂峰值窗口的速度作为出手速度，方向取自挥臂最快的那一段。
        var trackedVelocity = DartsSolver.SelectReleaseVelocity(
            ComputeTrackedVelocity(),
            _peakWindowVelocity,
            Time.time - _peakTime,
            lateReleaseForgivenessSeconds);
        _isHolding = false;
        _sampleCount = 0;
        _sampleHead = 0;

        var throwState = DartsSolver.ComputeThrow(
            trackedVelocity,
            minThrowHandSpeed,
            handSpeedMultiplier,
            minDartSpeed,
            maxDartSpeed);

        if (!throwsEnabled || !throwState.isThrow)
        {
            // 慢速松手＝把镖放回：轻微“未投出”提示震动，不出镖。
            DartsEvents.DartHoldCancelled();
            SendHaptic(throwHandNode, 0.15f, 0.03f);
            UpdateHeldVisual(false);
            return;
        }

        var direction = throwState.velocity.normalized;
        var origin = throwHandTransform.position + direction * holdForwardOffsetMeters;
        var velocity = ApplyAimAssist(origin, throwState.velocity);

        var dart = SpawnDart();
        if (dart != null)
        {
            dart.Launch(origin, velocity);
            DartsEvents.DartThrown(new DartThrownInfo(dart.gameObject, origin, velocity, throwState.handSpeed));
            SendHaptic(throwHandNode, throwHapticAmplitude, 0.08f);
        }

        UpdateHeldVisual(false);
    }

    private void TrackPeakSwingVelocity()
    {
        var windowVelocity = ComputeTrackedVelocity();
        var speed = windowVelocity.magnitude;
        if (speed >= _peakWindowSpeed)
        {
            _peakWindowSpeed = speed;
            _peakWindowVelocity = windowVelocity;
            _peakTime = Time.time;
        }
    }

    private bool IsPointerOverUi()
    {
        return uiHoverGuardBehaviour is IUiHoverGuard guard && guard.IsPointerOver;
    }

    private Vector3 ApplyAimAssist(Vector3 origin, Vector3 velocity)
    {
        if (aimAssistTarget == null || aimAssistMaxDegrees <= 0f) return velocity;

        // 飞镖速域必须用精确弹道解算理想方向（见 DartsSolver 内注释）。
        return DartsSolver.ComputeAssistedVelocityBallistic(
            origin,
            velocity,
            aimAssistTarget.position,
            aimAssistMaxDegrees,
            DartsGeometry.DartGravityMetersPerSecondSquared);
    }

    private DartProjectile SpawnDart()
    {
        if (dartTemplate == null) return null;

        var parent = dartContainer != null ? dartContainer : transform.parent;
        var dartObject = Instantiate(dartTemplate, parent);
        dartObject.name = "Dart";
        dartObject.SetActive(true);
        return dartObject.GetComponent<DartProjectile>();
    }

    private void RecordSample(Vector3 position, float time)
    {
        _samplePositions[_sampleHead] = position;
        _sampleTimes[_sampleHead] = time;
        _sampleHead = (_sampleHead + 1) % SampleCapacity;
        if (_sampleCount < SampleCapacity)
        {
            _sampleCount++;
        }
    }

    private Vector3 ComputeTrackedVelocity()
    {
        if (_sampleCount < 2 || throwHandTransform == null) return Vector3.zero;

        var newestIndex = (_sampleHead - 1 + SampleCapacity) % SampleCapacity;
        var newestTime = _sampleTimes[newestIndex];
        var newestPosition = _samplePositions[newestIndex];

        // 找采样窗口内最旧的一帧：用一小段时间的平均速度而不是单帧差分，
        // 老年用户手部微抖不会放大成投掷方向噪声。
        var oldestIndex = newestIndex;
        for (var i = 1; i < _sampleCount; i++)
        {
            var candidate = (newestIndex - i + SampleCapacity) % SampleCapacity;
            if (newestTime - _sampleTimes[candidate] > velocitySampleWindowSeconds) break;

            oldestIndex = candidate;
        }

        return DartsSolver.ComputeTrackedVelocity(
            _samplePositions[oldestIndex],
            newestPosition,
            newestTime - _sampleTimes[oldestIndex]);
    }

    private void UpdateHeldVisual(bool holding)
    {
        if (heldDartVisual == null) return;

        if (holding && throwHandTransform != null)
        {
            if (!heldDartVisual.gameObject.activeSelf)
            {
                heldDartVisual.gameObject.SetActive(true);
            }

            heldDartVisual.position = throwHandTransform.TransformPoint(new Vector3(0f, 0f, holdForwardOffsetMeters));
            heldDartVisual.rotation = throwHandTransform.rotation;
        }
        else if (heldDartVisual.gameObject.activeSelf)
        {
            heldDartVisual.gameObject.SetActive(false);
        }
    }

    private void ResolveHoldInputSource()
    {
        if (holdInputSourceBehaviour is IGripInputSource assigned)
        {
            _holdInput = assigned;
            ApplyThrowHandNodeToInputSource();
            return;
        }

        if (!autoCreateHoldInputSource) return;

        var created = gameObject.GetComponent<PicoGripOrTriggerInputSource>();
        if (created == null)
        {
            created = gameObject.AddComponent<PicoGripOrTriggerInputSource>();
        }

        created.controllerNode = throwHandNode;
        holdInputSourceBehaviour = created;
        _holdInput = created;
    }

    private void ApplyThrowHandNodeToInputSource()
    {
        if (holdInputSourceBehaviour is PicoGripOrTriggerInputSource gripOrTrigger)
        {
            gripOrTrigger.controllerNode = throwHandNode;
        }
        else if (holdInputSourceBehaviour is PicoGripInputSource grip)
        {
            grip.controllerNode = throwHandNode;
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
