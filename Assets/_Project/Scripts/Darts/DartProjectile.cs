using UnityEngine;

public class DartProjectile : MonoBehaviour
{
    public float gravityMetersPerSecondSquared = DartsGeometry.DartGravityMetersPerSecondSquared;
    public float linearDragPerSecond = DartsGeometry.DartLinearDragPerSecond;
    public float dartLengthMeters = DartsGeometry.DartLengthMeters;
    public float castRadiusMeters = DartsGeometry.DartRadiusMeters;
    public float stickDepthMeters = DartsGeometry.DartStickDepthMeters;
    public float maxFlightSeconds = DartsGeometry.DartMaxFlightSeconds;
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
        // 弹道用内部世界坐标积分，不回读 transform：父节点（投掷区）被“重新对准”
        // 移动时不会把飞行中的镖一起瞬移带偏。
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
            FinishAsMiss(DartsMissReason.TimedOut);
            return;
        }

        var previousTail = _tailPosition;
        var direction = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : transform.forward;
        // 首帧从出手点起扫，覆盖镖尾到镖尖之间的盲区（贴近障碍物出手时
        // 镖尖起始点可能已越过障碍表面，SphereCast 不报告初始重叠）。
        var previousTip = _firstSweep ? previousTail : previousTail + direction * dartLengthMeters;
        _firstSweep = false;

        var position = previousTail;
        ArcherySolver.SimulateArrowStep(ref position, ref _velocity, gravityMetersPerSecondSquared, linearDragPerSecond, Time.fixedDeltaTime);

        var newDirection = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : direction;
        var newTip = position + newDirection * dartLengthMeters;
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
            FinishAsMiss(DartsMissReason.FellShort);
        }
    }

    private void HandleImpact(RaycastHit hit, Vector3 flightDirection)
    {
        _inFlight = false;
        StopTrail();
        // 绕镖轴随机滚转，插盘后的尾翼姿态更自然。
        transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), flightDirection) *
                             Quaternion.LookRotation(flightDirection, Vector3.up);
        transform.position = hit.point + flightDirection * stickDepthMeters - flightDirection * dartLengthMeters;

        var board = hit.collider != null ? hit.collider.GetComponentInParent<DartsBoard>() : null;
        if (board != null)
        {
            transform.SetParent(board.StickParent, true);
            board.RegisterHit(hit.point, gameObject);
            return;
        }

        DartsEvents.DartMissed(new DartMissedInfo(gameObject, hit.point, DartsMissReason.HitEnvironment));
    }

    private void FinishAsMiss(DartsMissReason reason)
    {
        _inFlight = false;
        StopTrail();
        DartsEvents.DartMissed(new DartMissedInfo(gameObject, transform.position, reason));
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
