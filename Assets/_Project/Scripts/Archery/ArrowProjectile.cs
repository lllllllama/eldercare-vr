using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float gravityMetersPerSecondSquared = ArcheryGeometry.ArrowGravityMetersPerSecondSquared;
    public float linearDragPerSecond = ArcheryGeometry.ArrowLinearDragPerSecond;
    public float arrowLengthMeters = ArcheryGeometry.ArrowLengthMeters;
    public float castRadiusMeters = ArcheryGeometry.ArrowRadiusMeters;
    public float stickDepthMeters = ArcheryGeometry.ArrowStickDepthMeters;
    public float maxFlightSeconds = ArcheryGeometry.ArrowMaxFlightSeconds;
    public float missFloorY = -0.5f;
    public LayerMask hitLayers = ~0;
    public float stuckLifetimeSeconds = 20f;

    private Vector3 _tailPosition;
    private Vector3 _velocity;
    private bool _inFlight;
    private bool _firstSweep;
    private float _flightSeconds;
    private float _stuckSeconds;
    private TrailRenderer _trail;

    public bool InFlight => _inFlight;
    public Vector3 Velocity => _velocity;

    public void Launch(Vector3 tailOrigin, Vector3 velocity)
    {
        // 弹道用内部世界坐标积分，不回读 transform：父节点（箭道）在“重新对准”时移动
        // 不会把飞行中的箭一起瞬移带偏。
        _tailPosition = tailOrigin;
        transform.position = tailOrigin;
        _velocity = velocity;
        _inFlight = true;
        _firstSweep = true;
        _flightSeconds = 0f;
        _stuckSeconds = 0f;
        AlignToVelocity();
        gameObject.SetActive(true);

        if (_trail == null)
        {
            _trail = GetComponentInChildren<TrailRenderer>(true);
        }

        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }
    }

    private void FixedUpdate()
    {
        if (!_inFlight)
        {
            _stuckSeconds += Time.fixedDeltaTime;
            if (_stuckSeconds > stuckLifetimeSeconds)
            {
                Destroy(gameObject);
            }

            return;
        }

        _flightSeconds += Time.fixedDeltaTime;
        if (_flightSeconds > maxFlightSeconds)
        {
            FinishAsMiss(ArcheryMissReason.TimedOut);
            return;
        }

        var previousTail = _tailPosition;
        var direction = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : transform.forward;
        // 首帧从搭弦点起扫，覆盖箭尾到箭尖之间的 0.6m 盲区：贴近障碍物（如 MR 中的
        // 真实桌面）发射时，箭尖起始点可能已越过障碍表面，SphereCast 不报告初始重叠。
        var previousTip = _firstSweep ? previousTail : previousTail + direction * arrowLengthMeters;
        _firstSweep = false;

        var position = previousTail;
        ArcherySolver.SimulateArrowStep(ref position, ref _velocity, gravityMetersPerSecondSquared, linearDragPerSecond, Time.fixedDeltaTime);

        var newDirection = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : direction;
        var newTip = position + newDirection * arrowLengthMeters;
        var travel = newTip - previousTip;
        var travelDistance = travel.magnitude;

        if (travelDistance > 0.0001f &&
            Physics.SphereCast(previousTip, castRadiusMeters, travel / travelDistance, out var hit, travelDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            HandleImpact(hit, newDirection);
            return;
        }

        _tailPosition = position;
        transform.position = position;
        AlignToVelocity();

        if (position.y < missFloorY)
        {
            FinishAsMiss(ArcheryMissReason.FellShort);
        }
    }

    private void HandleImpact(RaycastHit hit, Vector3 flightDirection)
    {
        _inFlight = false;
        StopTrail();
        // 绕箭轴随机滚转一下，插靶后的尾羽姿态更自然。
        transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), flightDirection) *
                             Quaternion.LookRotation(flightDirection, Vector3.up);
        transform.position = hit.point + flightDirection * stickDepthMeters - flightDirection * arrowLengthMeters;

        var target = hit.collider != null ? hit.collider.GetComponentInParent<ArcheryTarget>() : null;
        if (target != null)
        {
            transform.SetParent(target.StickParent, true);
            target.RegisterHit(hit.point, gameObject);
            return;
        }

        ArcheryEvents.ArrowMissed(new ArrowMissedInfo(gameObject, hit.point, ArcheryMissReason.HitEnvironment));
    }

    private void FinishAsMiss(ArcheryMissReason reason)
    {
        _inFlight = false;
        StopTrail();
        ArcheryEvents.ArrowMissed(new ArrowMissedInfo(gameObject, transform.position, reason));
    }

    private void StopTrail()
    {
        if (_trail != null)
        {
            _trail.emitting = false;
        }
    }

    private void AlignToVelocity()
    {
        if (_velocity.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
    }
}
