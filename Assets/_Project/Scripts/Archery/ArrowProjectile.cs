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

    private Vector3 _velocity;
    private bool _inFlight;
    private float _flightSeconds;
    private float _stuckSeconds;

    public bool InFlight => _inFlight;
    public Vector3 Velocity => _velocity;

    public void Launch(Vector3 tailOrigin, Vector3 velocity)
    {
        transform.position = tailOrigin;
        _velocity = velocity;
        _inFlight = true;
        _flightSeconds = 0f;
        _stuckSeconds = 0f;
        AlignToVelocity();
        gameObject.SetActive(true);
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

        var previousTail = transform.position;
        var direction = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : transform.forward;
        var previousTip = previousTail + direction * arrowLengthMeters;

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

        transform.position = position;
        AlignToVelocity();

        if (transform.position.y < missFloorY)
        {
            FinishAsMiss(ArcheryMissReason.FellShort);
        }
    }

    private void HandleImpact(RaycastHit hit, Vector3 flightDirection)
    {
        _inFlight = false;
        transform.rotation = Quaternion.LookRotation(flightDirection, Vector3.up);
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
        ArcheryEvents.ArrowMissed(new ArrowMissedInfo(gameObject, transform.position, reason));
    }

    private void AlignToVelocity()
    {
        if (_velocity.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
    }
}
