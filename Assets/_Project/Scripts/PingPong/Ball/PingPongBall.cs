using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PingPongBall : MonoBehaviour
{
    public const float DefaultMaxAngularVelocity = 180f;
    private const float SurfaceCorrectionSkin = 0.006f;
    private const float MinimumGameplayTableBounceUpSpeed = 3.0f;
    private const float MinimumGameplayTableBounceRestitutionFloor = 0.98f;
    private const float MinimumGameplayTableBounceUpwardAssist = 0.52f;
    private const float MaximumGameplayTableBounceHorizontalDamping = 0.80f;
    private const float MaximumGameplayTableBounceHorizontalSpeed = 1.45f;

    public float paddleVelocityMultiplier = 1.05f;
    public float forwardBoost = 2.8f;
    public float upwardBoost = 0.32f;
    public float minimumPaddleHitSpeed = 3.0f;
    public float heldBallHitSpeed = 3.5f;
    public float paddleHitCooldown = 0.12f;
    public float surfaceHitCooldown = 0.035f;
    public float minimumClosingSpeed = 0.15f;
    public float heldBallMinimumSwingSpeed = 0.35f;
    public float maxSpeed = 10.5f;
    public float minimumTableBounceUpSpeed = MinimumGameplayTableBounceUpSpeed;
    public float tableBounceMaxUpSpeed = 0f;
    public float tableBounceRestitutionFloor = 0.98f;
    public float tableBounceUpwardAssist = MinimumGameplayTableBounceUpwardAssist;
    [Range(0f, 1f)] public float tableBounceHorizontalDamping = MaximumGameplayTableBounceHorizontalDamping;
    public float tableBounceMaxHorizontalSpeed = MaximumGameplayTableBounceHorizontalSpeed;
    public bool logPaddleHitDiagnostics = false;
    public bool logTableBounceDiagnostics = false;
    public bool logInitialTableBounceDiagnostics = true;
    public int initialTableBounceDiagnosticsLimit = 5;
    public bool enforceGameplayBounceTuningFloor = true;
    public bool enableSweptSurfaceFallback = true;
    public LayerMask sweptSurfaceLayers = ~0;
    public bool ignoreNonGameplayColliders = true;
    public float collisionFilterRefreshInterval = 0.2f;
    public string ballPhysicsLayerName = "Ball";
    public string ignoredRoomSensingLayerName = "RoomSensing";
    public bool useAerodynamics = true;
    public float airDensity = 1.27f;
    public float dragCoefficient = 0.5f;
    public float magnusLiftCoefficient = 0.28f;
    public float maximumAerodynamicAcceleration = 45f;
    public float maxAngularVelocity = DefaultMaxAngularVelocity;
    [Range(0f, 1f)] public float rawPaddleVelocityBlend = 0.28f;
    [Range(0f, 1f)] public float highAccelerationRawBlend = 0.34f;
    public bool enhanceBallVisibility = true;
    public Color ballCoreColor = new Color(1f, 0.96f, 0.76f, 1f);
    public Color trailStartColor = new Color(0.35f, 0.95f, 1f, 0.82f);
    public Color trailEndColor = new Color(0.35f, 0.95f, 1f, 0f);

    private readonly RaycastHit[] _sweepHits = new RaycastHit[16];
    private static Material _trackingTrailMaterial;
    private Rigidbody _rb;
    private SphereCollider _sphereCollider;
    private TrailRenderer _trackingTrail;
    private MaterialPropertyBlock _ballVisualBlock;
    private ControllerBallGrabber _activeGrabber;
    private Collider _lastSurfaceCollider;
    private Vector3 _lastSweepPosition;
    private Vector3 _lastPhysicsVelocity;
    private Vector3 _lastPhysicsAngularVelocity;
    private bool _hitRegistered;
    private float _ignoreGrabUntilTime;
    private float _lastPaddleHitTime = -1f;
    private float _lastSurfaceHitTime = -1f;
    private float _nextCollisionFilterRefreshTime;
    private int _initialTableBounceDiagnosticsCount;

    public bool IsGrabbed => _activeGrabber != null;
    public bool CanBeGrabbed => !IsGrabbed && Time.time >= _ignoreGrabUntilTime;
    public bool HasRegisteredHit => _hitRegistered;

    private bool IsHeld => IsGrabbed || (_rb != null && _rb.isKinematic && transform.parent != null);

    private void Awake()
    {
        ApplyRuntimeBounceTuningFloor();
        ConfigureRigidbody();
        ConfigureGameplayCollisionFilter(true);
        _sphereCollider = GetComponent<SphereCollider>();
        _lastSweepPosition = transform.position;
        ConfigureVisualTrackingAid();
    }

    private void OnEnable()
    {
        ApplyRuntimeBounceTuningFloor();
        _initialTableBounceDiagnosticsCount = 0;
        _lastSweepPosition = transform.position;
        _lastSurfaceCollider = null;
        _lastSurfaceHitTime = -1f;
        ConfigureGameplayCollisionFilter(true);
        ConfigureVisualTrackingAid();
        if (_trackingTrail != null)
        {
            _trackingTrail.Clear();
        }
    }

    private void ApplyRuntimeBounceTuningFloor()
    {
        if (!enforceGameplayBounceTuningFloor)
        {
            return;
        }

        minimumTableBounceUpSpeed = Mathf.Max(minimumTableBounceUpSpeed, MinimumGameplayTableBounceUpSpeed);
        if (tableBounceMaxUpSpeed > 0f && tableBounceMaxUpSpeed < minimumTableBounceUpSpeed)
        {
            tableBounceMaxUpSpeed = minimumTableBounceUpSpeed;
        }

        tableBounceRestitutionFloor = Mathf.Max(tableBounceRestitutionFloor, MinimumGameplayTableBounceRestitutionFloor);
        tableBounceUpwardAssist = Mathf.Max(tableBounceUpwardAssist, MinimumGameplayTableBounceUpwardAssist);
        tableBounceHorizontalDamping = Mathf.Min(Mathf.Clamp01(tableBounceHorizontalDamping), MaximumGameplayTableBounceHorizontalDamping);
        tableBounceMaxHorizontalSpeed = Mathf.Min(Mathf.Max(0f, tableBounceMaxHorizontalSpeed), MaximumGameplayTableBounceHorizontalSpeed);
    }

    public void ApplyGameplayBounceTuning(
        float minimumUpSpeed,
        float maximumUpSpeed,
        float upwardAssist,
        float horizontalDamping,
        float maximumHorizontalSpeed)
    {
        enforceGameplayBounceTuningFloor = false;
        minimumTableBounceUpSpeed = Mathf.Max(0f, minimumUpSpeed);
        tableBounceMaxUpSpeed = Mathf.Max(0f, maximumUpSpeed);
        if (tableBounceMaxUpSpeed > 0f && tableBounceMaxUpSpeed < minimumTableBounceUpSpeed)
        {
            minimumTableBounceUpSpeed = tableBounceMaxUpSpeed;
        }

        tableBounceUpwardAssist = Mathf.Max(0f, upwardAssist);
        tableBounceHorizontalDamping = Mathf.Clamp01(horizontalDamping);
        tableBounceMaxHorizontalSpeed = Mathf.Max(0f, maximumHorizontalSpeed);
    }

    public void ExcludeIgnoredRoomSensingLayerFromSweep()
    {
        if (string.IsNullOrEmpty(ignoredRoomSensingLayerName)) return;

        var ignoredLayer = LayerMask.NameToLayer(ignoredRoomSensingLayerName);
        if (ignoredLayer < 0) return;

        sweptSurfaceLayers = sweptSurfaceLayers & ~(1 << ignoredLayer);
    }

    public void ConfigureGameplayCollisionFilter(bool forceRefresh = false)
    {
        ConfigureBallLayer();
        ExcludeIgnoredRoomSensingLayerFromSweep();

        if (forceRefresh)
        {
            _nextCollisionFilterRefreshTime = 0f;
        }

        RefreshIgnoredNonGameplayColliders();
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic)
        {
            _lastSweepPosition = transform.position;
            return;
        }

        _lastPhysicsVelocity = _rb.velocity;
        _lastPhysicsAngularVelocity = _rb.angularVelocity;
        RefreshIgnoredNonGameplayColliders();
        ApplyAerodynamics();

        if (!enableSweptSurfaceFallback)
        {
            _lastSweepPosition = transform.position;
            return;
        }

        TryApplySweptSurfaceFallback(_lastSweepPosition, transform.position);
        _lastSweepPosition = transform.position;
    }

    private void LateUpdate()
    {
        UpdateVisualTrackingAidState();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (TryIgnoreNonGameplayCollision(collision))
        {
            return;
        }

        var tracker = collision.collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            var hitPoint = transform.position;
            var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, hitPoint);
            var normal = EstimatePaddleFaceNormal(tracker.transform, _rb.velocity, surfaceVelocity);
            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                hitPoint = contact.point;
                normal = contact.normal;
            }

            ApplyPaddleHit(tracker, normal, hitPoint, collision.collider);
            return;
        }

        var surface = PingPongSurface.Find(collision.collider);
        if (surface == null) return;

        var surfacePoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);
        var surfaceNormal = collision.contactCount > 0
            ? collision.GetContact(0).normal
            : PingPongSurface.EstimateNormal(collision.collider, transform.position, _rb.velocity);

        ApplySurfaceBounce(surface, collision.collider, surfaceNormal, surfacePoint, false);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryIgnoreNonGameplayCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    private void ConfigureBallLayer()
    {
        if (string.IsNullOrEmpty(ballPhysicsLayerName)) return;

        var ballLayer = LayerMask.NameToLayer(ballPhysicsLayerName);
        if (ballLayer < 0) return;

        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null)
            {
                child.gameObject.layer = ballLayer;
            }
        }
    }

    private void RefreshIgnoredNonGameplayColliders()
    {
        if (!ignoreNonGameplayColliders) return;
        if (!Application.isPlaying) return;
        if (Time.time < _nextCollisionFilterRefreshTime) return;

        _nextCollisionFilterRefreshTime = Time.time + Mathf.Max(0.02f, collisionFilterRefreshInterval);

        var ownColliders = GetComponentsInChildren<Collider>(true);
        if (ownColliders == null || ownColliders.Length == 0) return;

        foreach (var candidate in FindObjectsOfType<Collider>(false))
        {
            if (candidate == null || !candidate.enabled) continue;
            if (IsOwnCollider(candidate, ownColliders)) continue;

            var ignoreCollision = !IsGameplayCollider(candidate);

            foreach (var ownCollider in ownColliders)
            {
                if (ownCollider != null && ownCollider.enabled)
                {
                    Physics.IgnoreCollision(ownCollider, candidate, ignoreCollision);
                }
            }
        }
    }

    private bool TryIgnoreNonGameplayCollision(Collision collision)
    {
        if (!ignoreNonGameplayColliders || collision == null || collision.collider == null) return false;
        if (IsGameplayCollider(collision.collider)) return false;

        var ownColliders = GetComponentsInChildren<Collider>(true);
        foreach (var ownCollider in ownColliders)
        {
            if (ownCollider != null && ownCollider.enabled)
            {
                Physics.IgnoreCollision(ownCollider, collision.collider, true);
            }
        }

        if (_rb != null && !_rb.isKinematic)
        {
            _rb.velocity = _lastPhysicsVelocity;
            _rb.angularVelocity = _lastPhysicsAngularVelocity;
        }

        _lastSweepPosition = transform.position;
        return true;
    }

    private bool IsGameplayCollider(Collider candidate)
    {
        if (candidate == null) return false;
        if (candidate.GetComponentInParent<PingPongBall>() != null) return false;
        if (candidate.GetComponentInParent<PlayerTableBoundary>() != null) return false;
        if (candidate.GetComponentInParent<PaddleVelocityTracker>() != null) return true;

        var surface = candidate.GetComponent<PingPongSurface>() ?? candidate.GetComponentInParent<PingPongSurface>();
        if (surface != null)
        {
            return IsGameplaySurface(surface.surfaceType);
        }

        return HasGameplayName(candidate.transform) && HasAncestorNamed(candidate.transform, "PingPong");
    }

    private static bool IsGameplaySurface(PingPongSurfaceType surfaceType)
    {
        return surfaceType == PingPongSurfaceType.Table ||
               surfaceType == PingPongSurfaceType.Net ||
               surfaceType == PingPongSurfaceType.PaddleBody ||
               surfaceType == PingPongSurfaceType.PaddleHitZone;
    }

    private static bool HasGameplayName(Transform transform)
    {
        while (transform != null)
        {
            var lowerName = transform.name.ToLowerInvariant();
            if (lowerName.Contains("table") ||
                lowerName.Contains("net") ||
                lowerName.Contains("paddle") ||
                lowerName.Contains("racket"))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool HasAncestorNamed(Transform transform, string ancestorName)
    {
        while (transform != null)
        {
            if (transform.name == ancestorName)
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsOwnCollider(Collider candidate, Collider[] ownColliders)
    {
        foreach (var ownCollider in ownColliders)
        {
            if (candidate == ownCollider)
            {
                return true;
            }
        }

        return false;
    }

    public void SetGrabber(ControllerBallGrabber grabber)
    {
        _activeGrabber = grabber;
        if (grabber != null)
        {
            _hitRegistered = false;
            _ignoreGrabUntilTime = 0f;
        }
    }

    public void IgnoreGrabFor(float seconds)
    {
        _ignoreGrabUntilTime = Mathf.Max(_ignoreGrabUntilTime, Time.time + Mathf.Max(0f, seconds));
    }

    private void ConfigureRigidbody()
    {
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.mass = PingPongGeometry.BallMass;
        _rb.drag = useAerodynamics ? 0f : PingPongGeometry.BallDrag;
        _rb.angularDrag = PingPongGeometry.BallAngularDrag;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        ConfigureSpinLimit(_rb, maxAngularVelocity);
    }

    private void ConfigureVisualTrackingAid()
    {
        if (!enhanceBallVisibility) return;

        ConfigureBallRenderers();

        if (_trackingTrail == null)
        {
            _trackingTrail = GetComponent<TrailRenderer>();
            if (_trackingTrail == null)
            {
                _trackingTrail = gameObject.AddComponent<TrailRenderer>();
            }
        }

        var radius = Mathf.Max(0.01f, GetWorldRadius());
        _trackingTrail.time = 0.22f;
        _trackingTrail.startWidth = radius * 1.15f;
        _trackingTrail.endWidth = radius * 0.12f;
        _trackingTrail.minVertexDistance = radius * 0.35f;
        _trackingTrail.numCornerVertices = 2;
        _trackingTrail.numCapVertices = 2;
        _trackingTrail.alignment = LineAlignment.View;
        _trackingTrail.textureMode = LineTextureMode.Stretch;
        _trackingTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trackingTrail.receiveShadows = false;
        _trackingTrail.material = _trackingTrailMaterial ?? (_trackingTrailMaterial = CreateTrackingTrailMaterial());

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(trailStartColor, 0f),
                new GradientColorKey(trailEndColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(trailStartColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        _trackingTrail.colorGradient = gradient;
    }

    private void ConfigureBallRenderers()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var ballRenderer in renderers)
        {
            if (ballRenderer == null || ballRenderer is TrailRenderer) continue;

            var sharedMaterials = ballRenderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0) continue;

            if (_ballVisualBlock == null)
            {
                _ballVisualBlock = new MaterialPropertyBlock();
            }
            for (var i = 0; i < sharedMaterials.Length; i++)
            {
                var sharedMaterial = sharedMaterials[i];
                if (sharedMaterial == null) continue;

                ballRenderer.GetPropertyBlock(_ballVisualBlock, i);

                if (sharedMaterial.HasProperty("_BaseColor"))
                {
                    _ballVisualBlock.SetColor("_BaseColor", ballCoreColor);
                }

                if (sharedMaterial.HasProperty("_Color"))
                {
                    _ballVisualBlock.SetColor("_Color", ballCoreColor);
                }

                if (sharedMaterial.HasProperty("_EmissionColor"))
                {
                    _ballVisualBlock.SetColor("_EmissionColor", ballCoreColor * 0.35f);
                }

                ballRenderer.SetPropertyBlock(_ballVisualBlock, i);
            }
        }
    }

    private void UpdateVisualTrackingAidState()
    {
        if (_trackingTrail == null) return;

        var moving = _rb != null && !_rb.isKinematic && _rb.velocity.sqrMagnitude > 0.2f;
        _trackingTrail.emitting = enhanceBallVisibility && moving && !IsGrabbed;
    }

    private static Material CreateTrackingTrailMaterial()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        var material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        material.name = "PingPongBallTrackingTrail";
        return material;
    }

    public static void ConfigureSpinLimit(Rigidbody rb, float requiredAngularVelocity)
    {
        if (rb == null) return;

        rb.maxAngularVelocity = Mathf.Max(
            rb.maxAngularVelocity,
            Mathf.Max(DefaultMaxAngularVelocity, Mathf.Max(0f, requiredAngularVelocity)));
    }

    private void ApplyAerodynamics()
    {
        if (!useAerodynamics || _rb == null || _rb.isKinematic) return;

        var velocity = _rb.velocity;
        if (velocity.sqrMagnitude < 0.0001f) return;

        var acceleration = CalculateAerodynamicAcceleration(
            velocity,
            _rb.angularVelocity,
            GetWorldRadius(),
            _rb.mass,
            airDensity,
            dragCoefficient,
            magnusLiftCoefficient,
            maximumAerodynamicAcceleration);

        if (!IsFinite(acceleration) || acceleration.sqrMagnitude < 0.0001f) return;

        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    public static Vector3 CalculateAerodynamicAcceleration(
        Vector3 velocity,
        Vector3 angularVelocity,
        float radius,
        float mass,
        float airDensity,
        float dragCoefficient,
        float magnusLiftCoefficient,
        float maximumAcceleration)
    {
        if (velocity.sqrMagnitude < 0.0001f) return Vector3.zero;

        var safeRadius = Mathf.Max(radius, 0.001f);
        var area = Mathf.PI * safeRadius * safeRadius;
        var inverseMass = 1f / Mathf.Max(mass, 0.0001f);
        var dragAcceleration = -0.5f * Mathf.Max(0f, airDensity) * Mathf.Max(0f, dragCoefficient) * area * velocity.magnitude * velocity * inverseMass;
        var magnusAcceleration = 0.5f * Mathf.Max(0f, airDensity) * Mathf.Max(0f, magnusLiftCoefficient) * area * safeRadius * Vector3.Cross(angularVelocity, velocity) * inverseMass;
        var acceleration = dragAcceleration + magnusAcceleration;
        if (!IsFinite(acceleration)) return Vector3.zero;
        return Vector3.ClampMagnitude(acceleration, Mathf.Max(0f, maximumAcceleration));
    }

    private void TryApplyTriggerInteraction(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            var paddleSpeed = Mathf.Max(tracker.Speed, tracker.RawVelocity.magnitude);
            if (!IsHeld && paddleSpeed < 0.2f && _rb.velocity.sqrMagnitude > 0.02f) return;

            var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, transform.position);
            ApplyPaddleHit(
                tracker,
                EstimatePaddleFaceNormal(tracker.transform, _rb.velocity, surfaceVelocity),
                transform.position,
                other);
            return;
        }

        var surface = PingPongSurface.Find(other);
        if (surface == null || surface.surfaceType != PingPongSurfaceType.Net) return;

        var normal = PingPongSurface.EstimateNormal(other, transform.position, _rb.velocity);
        var point = other.ClosestPoint(transform.position);
        ApplySurfaceBounce(surface, other, normal, point, true);
    }

    private void ApplyPaddleHit(PaddleVelocityTracker tracker, Vector3 normal, Vector3 hitPoint, Collider hitCollider)
    {
        if (Time.time - _lastPaddleHitTime < paddleHitCooldown) return;
        if (tracker == null || _rb == null) return;

        var wasHeld = IsHeld;
        var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, hitPoint);
        var paddleSpeed = Mathf.Max(tracker.Speed, tracker.RawVelocity.magnitude);
        var preferredForward = ResolvePreferredPlayForward(tracker);
        if (wasHeld && paddleSpeed < heldBallMinimumSwingSpeed && Vector3.Dot(surfaceVelocity, preferredForward) < minimumClosingSpeed)
        {
            return;
        }

        var incomingVelocity = wasHeld ? Vector3.zero : _rb.velocity;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = EstimatePaddleFaceNormal(tracker.transform, incomingVelocity, surfaceVelocity);
        }

        normal = BlendTowardPaddleFace(normal, tracker.transform, incomingVelocity, surfaceVelocity);

        var input = PingPongHitSolver.CreateDefault(incomingVelocity, _rb.angularVelocity, normal, surfaceVelocity);
        input.normalRestitution = 0.88f;
        input.tangentialFriction = 0.52f;
        input.spinTransfer = 0.48f;
        input.minimumClosingSpeed = minimumClosingSpeed;
        input.minimumSpeed = wasHeld ? heldBallHitSpeed : minimumPaddleHitSpeed;
        input.maximumSpeed = maxSpeed;
        input.upwardBias = upwardBoost;
        input.preferredForward = preferredForward;
        input.minimumForwardDot = wasHeld ? 0.38f : 0.08f;
        input.forwardBlend = wasHeld ? 0.82f : 0.55f;
        input.biasTowardPreferredForward = true;

        var result = PingPongHitSolver.Solve(input);
        if (!result.accepted)
        {
            return;
        }

        _lastPaddleHitTime = Time.time;

        var velocity = PingPongHitSolver.ApplyPaddleContactPlacement(
            result.velocity,
            tracker.GetCenteredLocalHit(hitPoint, hitCollider),
            1.15f,
            0.35f);

        var forwardSpeed = Vector3.Dot(velocity, preferredForward);
        var desiredForwardSpeed = Mathf.Max(0.5f, forwardBoost);
        if (forwardSpeed < desiredForwardSpeed)
        {
            velocity += preferredForward * ((desiredForwardSpeed - forwardSpeed) * 0.45f);
        }

        velocity += preferredForward * Mathf.Max(0f, Vector3.Dot(surfaceVelocity, preferredForward)) * paddleVelocityMultiplier * 0.22f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        var finalAngularVelocity = Vector3.ClampMagnitude(result.angularVelocity, DefaultMaxAngularVelocity);
        if (logPaddleHitDiagnostics)
        {
            Debug.Log(
                "Paddle hit diagnostics:\n" +
                $"- incomingVelocity: {incomingVelocity}\n" +
                $"- surfaceVelocity: {surfaceVelocity}\n" +
                $"- preferredForward: {preferredForward}\n" +
                $"- final velocity: {velocity}\n" +
                $"- paddleSpeed: {paddleSpeed:0.###}\n" +
                $"- wasHeld: {wasHeld}");
        }

        if (_activeGrabber != null && _activeGrabber.ForceRelease(this, velocity))
        {
            _activeGrabber = null;
        }
        else
        {
            DetachFromGrabIfNeeded();
            _rb.velocity = velocity;
        }

        _rb.angularVelocity = finalAngularVelocity;
        _lastSweepPosition = transform.position;

        var firstHitForBall = !_hitRegistered;
        PingPongEvents.BallHit(
            new BallHitInfo(
                gameObject,
                hitCollider,
                wasHeld ? PingPongHitType.HeldBallPaddle : PingPongHitType.Paddle,
                hitPoint,
                normal,
                incomingVelocity,
                velocity,
                surfaceVelocity,
                finalAngularVelocity,
                result.closingSpeed,
                firstHitForBall),
            firstHitForBall);

        if (!_hitRegistered)
        {
            _hitRegistered = true;
        }
    }

    private void ApplySurfaceBounce(PingPongSurface surface, Collider collider, Vector3 normal, Vector3 contactPoint, bool forcePositionCorrection)
    {
        if (surface == null || collider == null || _rb == null || _rb.isKinematic) return;

        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = PingPongSurface.EstimateNormal(collider, transform.position, _rb.velocity);
        }

        normal.Normalize();
        var isTableBounce = surface.surfaceType == PingPongSurfaceType.Table;
        if (!(isTableBounce && normal.y > 0.5f) && Vector3.Dot(normal, _rb.velocity) > 0f)
        {
            normal = -normal;
        }

        if (_lastSurfaceCollider == collider && Time.time - _lastSurfaceHitTime < surfaceHitCooldown)
        {
            CorrectSurfacePenetrationIfNeeded(surface, collider, normal, contactPoint);
            return;
        }

        var incomingVelocity = ResolveSurfaceIncomingVelocity(normal, isTableBounce, out var usedPreviousPhysicsVelocity);
        var closingSpeed = -Vector3.Dot(incomingVelocity, normal);
        var minimumClosingSpeedForSurface = isTableBounce && normal.y > 0.5f ? 0.001f : 0.02f;
        if (closingSpeed < minimumClosingSpeedForSurface)
        {
            CorrectSurfacePenetrationIfNeeded(surface, collider, normal, contactPoint);
            return;
        }

        var input = PingPongHitSolver.CreateDefault(incomingVelocity, _rb.angularVelocity, normal, Vector3.zero);
        input.normalRestitution = isTableBounce
            ? Mathf.Max(surface.normalRestitution, tableBounceRestitutionFloor)
            : surface.normalRestitution;
        input.tangentialFriction = isTableBounce
            ? Mathf.Min(surface.tangentialFriction, 0.05f)
            : surface.tangentialFriction;
        input.spinTransfer = 0.35f;
        input.minimumClosingSpeed = 0.02f;
        input.minimumSpeed = 0f;
        input.maximumSpeed = maxSpeed;
        input.biasTowardPreferredForward = false;

        var result = PingPongHitSolver.Solve(input);
        if (!result.accepted) return;

        if (forcePositionCorrection || ShouldCorrectSurfacePosition(surface))
        {
            transform.position = CorrectedSurfacePosition(contactPoint, normal);
        }

        var finalAngularVelocity = Vector3.ClampMagnitude(result.angularVelocity, DefaultMaxAngularVelocity);
        _rb.velocity = isTableBounce && normal.y > 0.5f
            ? EnsureMinimumTableBounceVelocity(
                result.velocity,
                incomingVelocity,
                Mathf.Max(0f, minimumTableBounceUpSpeed),
                Mathf.Max(0f, tableBounceMaxUpSpeed),
                Mathf.Max(0f, tableBounceUpwardAssist),
                Mathf.Max(0f, maxSpeed),
                tableBounceHorizontalDamping,
                tableBounceMaxHorizontalSpeed)
            : result.velocity;
        _rb.angularVelocity = finalAngularVelocity;
        if (isTableBounce && ShouldLogTableBounceDiagnostics())
        {
            Debug.Log(
                "Table bounce diagnostics:\n" +
                $"- ball: {name}\n" +
                $"- collider: {collider.name}\n" +
                $"- surfaceType: {surface.surfaceType}\n" +
                $"- incomingVelocity: {incomingVelocity}\n" +
                $"- currentRbVelocityAtBounce: {_rb.velocity}\n" +
                $"- lastPhysicsVelocity: {_lastPhysicsVelocity}\n" +
                $"- usedPreviousPhysicsVelocity: {usedPreviousPhysicsVelocity}\n" +
                $"- closingSpeed: {closingSpeed:0.###}\n" +
                $"- result.velocity: {result.velocity}\n" +
                $"- final _rb.velocity: {_rb.velocity}\n" +
                $"- finalHorizontalSpeed: {new Vector3(_rb.velocity.x, 0f, _rb.velocity.z).magnitude:0.###}\n" +
                $"- normal: {normal}\n" +
                $"- surface.normalRestitution: {surface.normalRestitution:0.###}\n" +
                $"- minimumTableBounceUpSpeed: {minimumTableBounceUpSpeed:0.###}\n" +
                $"- tableBounceMaxUpSpeed: {tableBounceMaxUpSpeed:0.###}\n" +
                $"- tableBounceUpwardAssist: {tableBounceUpwardAssist:0.###}\n" +
                $"- tableBounceHorizontalDamping: {tableBounceHorizontalDamping:0.###}\n" +
                $"- tableBounceMaxHorizontalSpeed: {tableBounceMaxHorizontalSpeed:0.###}\n" +
                $"- tableBounceRestitutionFloor: {tableBounceRestitutionFloor:0.###}\n" +
                $"- maxSpeed: {maxSpeed:0.###}");
        }

        CorrectSurfacePenetrationIfNeeded(surface, collider, normal, contactPoint);
        _lastSurfaceCollider = collider;
        _lastSurfaceHitTime = Time.time;
        _lastSweepPosition = transform.position;

        PingPongEvents.SurfaceBounce(new SurfaceBounceInfo(
            gameObject,
            collider,
            surface.surfaceType,
            contactPoint,
            normal,
            incomingVelocity,
            result.velocity,
            finalAngularVelocity,
            result.closingSpeed,
            forcePositionCorrection));
    }

    private void TryApplySweptSurfaceFallback(Vector3 start, Vector3 end)
    {
        var delta = end - start;
        var distance = delta.magnitude;
        if (distance <= 0.0001f) return;

        var radius = GetWorldRadius();
        var direction = delta / distance;
        var count = Physics.SphereCastNonAlloc(
            start,
            radius,
            direction,
            _sweepHits,
            distance,
            sweptSurfaceLayers,
            QueryTriggerInteraction.Collide);

        RaycastHit bestHit = new RaycastHit();
        PingPongSurface bestSurface = null;
        var bestDistance = float.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var hit = _sweepHits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<PingPongBall>() == this) continue;
            if (!IsGameplayCollider(hit.collider)) continue;

            var surface = PingPongSurface.Find(hit.collider);
            if (surface == null || !surface.useSweptFallback) continue;
            if (hit.collider.isTrigger &&
                surface.surfaceType != PingPongSurfaceType.Net &&
                surface.surfaceType != PingPongSurfaceType.PaddleHitZone)
            {
                continue;
            }

            if (hit.distance >= bestDistance) continue;

            bestDistance = hit.distance;
            bestHit = hit;
            bestSurface = surface;
        }

        if (bestSurface == null) return;

        var normal = bestHit.normal.sqrMagnitude > 0.0001f
            ? bestHit.normal
            : PingPongSurface.EstimateNormal(bestHit.collider, transform.position, _rb.velocity);

        if (bestSurface.IsPaddleSurface)
        {
            var tracker = bestHit.collider.GetComponentInParent<PaddleVelocityTracker>();
            if (tracker != null)
            {
                ApplyPaddleHit(tracker, normal, bestHit.point, bestHit.collider);
                return;
            }
        }

        transform.position = CorrectedSurfacePosition(bestHit.point, normal);
        ApplySurfaceBounce(bestSurface, bestHit.collider, normal, bestHit.point, false);
    }

    private Vector3 CorrectedSurfacePosition(Vector3 surfacePoint, Vector3 normal)
    {
        if (surfacePoint == Vector3.zero)
        {
            surfacePoint = transform.position;
        }

        return surfacePoint + normal.normalized * (GetWorldRadius() + SurfaceCorrectionSkin);
    }

    private Vector3 ResolveSurfaceIncomingVelocity(Vector3 normal, bool isTableBounce, out bool usedPreviousPhysicsVelocity)
    {
        usedPreviousPhysicsVelocity = false;
        var currentVelocity = _rb != null ? _rb.velocity : Vector3.zero;
        if (!isTableBounce || normal.y <= 0.5f)
        {
            return currentVelocity;
        }

        var currentClosingSpeed = -Vector3.Dot(currentVelocity, normal);
        var previousClosingSpeed = -Vector3.Dot(_lastPhysicsVelocity, normal);
        if (previousClosingSpeed > Mathf.Max(0.005f, currentClosingSpeed))
        {
            usedPreviousPhysicsVelocity = true;
            return _lastPhysicsVelocity;
        }

        return currentVelocity;
    }

    private bool ShouldLogTableBounceDiagnostics()
    {
        if (logTableBounceDiagnostics)
        {
            return true;
        }

        if (!logInitialTableBounceDiagnostics)
        {
            return false;
        }

        var limit = Mathf.Max(0, initialTableBounceDiagnosticsLimit);
        if (_initialTableBounceDiagnosticsCount >= limit)
        {
            return false;
        }

        _initialTableBounceDiagnosticsCount++;
        return true;
    }

    public static Vector3 EnsureMinimumTableBounceVelocity(
        Vector3 outgoingVelocity,
        Vector3 incomingVelocity,
        float minimumUpSpeed,
        float upwardAssist,
        float maximumSpeed,
        float horizontalDamping = 1f,
        float maximumHorizontalSpeed = 0f)
    {
        return EnsureMinimumTableBounceVelocity(
            outgoingVelocity,
            incomingVelocity,
            minimumUpSpeed,
            0f,
            upwardAssist,
            maximumSpeed,
            horizontalDamping,
            maximumHorizontalSpeed);
    }

    public static Vector3 EnsureMinimumTableBounceVelocity(
        Vector3 outgoingVelocity,
        Vector3 incomingVelocity,
        float minimumUpSpeed,
        float maximumUpSpeed,
        float upwardAssist,
        float maximumSpeed,
        float horizontalDamping = 1f,
        float maximumHorizontalSpeed = 0f)
    {
        var velocity = outgoingVelocity;
        var damping = Mathf.Clamp01(horizontalDamping);
        velocity.x *= damping;
        velocity.z *= damping;
        var horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        if (maximumHorizontalSpeed > 0f && horizontalVelocity.magnitude > maximumHorizontalSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maximumHorizontalSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        velocity.y = Mathf.Max(velocity.y + Mathf.Max(0f, upwardAssist), Mathf.Max(0f, minimumUpSpeed));
        if (maximumUpSpeed > 0f)
        {
            velocity.y = Mathf.Min(velocity.y, Mathf.Max(minimumUpSpeed, maximumUpSpeed));
        }

        if (maximumSpeed > 0f && velocity.magnitude > maximumSpeed)
        {
            velocity = velocity.normalized * maximumSpeed;
            if (velocity.y < minimumUpSpeed)
            {
                velocity.y = minimumUpSpeed;
                var horizontal = new Vector3(velocity.x, 0f, velocity.z);
                var maxHorizontal = Mathf.Sqrt(Mathf.Max(0f, maximumSpeed * maximumSpeed - minimumUpSpeed * minimumUpSpeed));
                if (horizontal.magnitude > maxHorizontal && horizontal.sqrMagnitude > 0.0001f)
                {
                    horizontal = horizontal.normalized * maxHorizontal;
                    velocity.x = horizontal.x;
                    velocity.z = horizontal.z;
                }
            }
        }

        return velocity;
    }

    public bool CorrectSurfacePenetrationIfNeeded(PingPongSurface surface, Collider collider, Vector3 normal, Vector3 contactPoint)
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
        }

        if (surface == null || collider == null || _rb == null) return false;
        if (!ShouldCorrectSurfacePosition(surface)) return false;

        var radius = GetWorldRadius();
        var minCenterY = collider.bounds.max.y + radius + SurfaceCorrectionSkin;
        var position = transform.position;
        if (position.y >= minCenterY)
        {
            return false;
        }

        position.y = minCenterY;
        transform.position = position;

        var velocity = _rb.velocity;
        if (velocity.y < 0.05f)
        {
            velocity.y = Mathf.Max(0.25f, Mathf.Abs(velocity.y) * 0.45f);
            _rb.velocity = velocity;
        }

        _lastSweepPosition = transform.position;
        return true;
    }

    private static bool ShouldCorrectSurfacePosition(PingPongSurface surface)
    {
        return surface != null &&
               (surface.surfaceType == PingPongSurfaceType.Table ||
                surface.surfaceType == PingPongSurfaceType.Floor);
    }

    private void DetachFromGrabIfNeeded()
    {
        if (!_rb.isKinematic) return;

        transform.SetParent(null, true);
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    private Vector3 GetResponsiveSurfaceVelocity(PaddleVelocityTracker tracker, Vector3 worldPoint)
    {
        if (tracker == null) return Vector3.zero;

        var smoothed = tracker.GetSurfaceVelocity(worldPoint);
        var raw = tracker.GetRawSurfaceVelocity(worldPoint);
        var accelerationBlend = Mathf.InverseLerp(6f, 24f, tracker.RawAcceleration.magnitude) * highAccelerationRawBlend;
        return Vector3.Lerp(smoothed, raw, Mathf.Clamp01(rawPaddleVelocityBlend + accelerationBlend));
    }

    private float GetWorldRadius()
    {
        if (_sphereCollider == null)
        {
            _sphereCollider = GetComponent<SphereCollider>();
        }

        if (_sphereCollider == null) return PingPongGeometry.BallRadius;

        var scale = _sphereCollider.transform.lossyScale;
        var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return _sphereCollider.radius * maxScale;
    }

    private static Vector3 EstimatePaddleFaceNormal(Transform paddle, Vector3 incomingVelocity, Vector3 surfaceVelocity)
    {
        if (paddle == null) return Vector3.forward;

        var normal = paddle.up;
        var relativeVelocity = incomingVelocity - surfaceVelocity;
        if (relativeVelocity.sqrMagnitude > 0.0001f && Vector3.Dot(normal, relativeVelocity) > 0f)
        {
            normal = -normal;
        }
        else if (relativeVelocity.sqrMagnitude <= 0.0001f && Vector3.Dot(normal, Vector3.forward) < 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static Vector3 ResolvePreferredPlayForward(PaddleVelocityTracker tracker)
    {
        var table = PingPongTableRecenterOnEnter.FindTableRoot();
        Vector3 forward;

        if (table != null)
        {
            forward = table.forward;
        }
        else if (tracker != null)
        {
            forward = tracker.transform.forward;
        }
        else
        {
            forward = Vector3.forward;
        }

        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private static Vector3 BlendTowardPaddleFace(Vector3 collisionNormal, Transform paddle, Vector3 incomingVelocity, Vector3 surfaceVelocity)
    {
        if (paddle == null) return collisionNormal;

        var faceNormal = EstimatePaddleFaceNormal(paddle, incomingVelocity, surfaceVelocity);
        if (collisionNormal.sqrMagnitude < 0.0001f) return faceNormal;

        collisionNormal.Normalize();
        if (Vector3.Dot(collisionNormal, faceNormal) < 0.45f)
        {
            return faceNormal;
        }

        return Vector3.Slerp(collisionNormal, faceNormal, 0.35f);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
               !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }
}
