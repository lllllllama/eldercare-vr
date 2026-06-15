using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform spawnPoint;
    public Transform targetPoint;
    public Transform tableTransform;
    public bool autoResolveTableTransform = true;
    public Transform ballContainer;
    public bool autoStartOnPlay = false;

    public float serveInterval = 4.0f;
    public float serveSpeed = 3.1f;
    public PingPongServeProfile serveProfile = PingPongServeProfile.Basic;
    public float upwardArc = 0.55f;
    public float minimumNetClearanceHeight = PingPongGeometry.TableTopHeight + PingPongGeometry.NetHeight + 0.16f;
    public float netWorldZ = PingPongGeometry.TableCenter.z;
    public bool useTableRelativeServeTargets = true;
    public float netLocalZ = 0f;
    public bool bounceOnTableBeforePlayer = true;
    public float tableBounceWorldY = PingPongGeometry.TableTopHeight + PingPongGeometry.BallRadius;
    public float tableBounceWorldZ = 1.35f;
    public float tableBounceLocalZ = -0.65f;
    public float horizontalRandomRange = 0.08f;
    public float verticalRandomRange = 0.02f;

    [Header("Serve Path Variation")]
    public bool enableServePathVariation = true;
    public bool useDifficultyServeVariation = true;
    public float serveTargetLateralRandomRange = 0.18f;
    public float serveTargetDepthRandomRange = 0.05f;
    public float serveYawRandomDegrees = 2.5f;
    [Range(0f, 0.35f)] public float serveSpeedJitter = 0.05f;
    public float serveEdgeSafetyMargin = 0.18f;
    public bool drawServeVariationGizmos = true;

    public float topspinRadiansPerSecond = 95f;
    public float backspinRadiansPerSecond = 80f;
    public float sidespinRadiansPerSecond = 50f;
    [Range(0f, 1f)] public float serveSpinRandomness = 0.05f;
    public float maxServeSpin = 140f;
    public bool logServeDiagnostics = false;
    public float serveNetClearanceSafetyMargin = 0.03f;
    public float spawnedBallMass = PingPongGeometry.BallMass;
    public float spawnedBallDrag = 0.015f;
    public float spawnedBallAngularDrag = 0.04f;
    [Range(0f, 1f)] public float spawnedBallBounciness = 0.86f;
    [Range(0f, 1f)] public float spawnedBallDynamicFriction = 0.01f;
    [Range(0f, 1f)] public float spawnedBallStaticFriction = 0.01f;
    public float spawnedBallTableBounceMinUpSpeed = 3.0f;
    public float spawnedBallTableBounceMaxUpSpeed = 3.15f;
    public float spawnedBallTableBounceUpwardAssist = 0.52f;
    [Range(0f, 1f)] public float spawnedBallTableBounceHorizontalDamping = 0.80f;
    public float spawnedBallTableBounceMaxHorizontalSpeed = 1.45f;

    private PhysicMaterial _ballPhysicsMaterial;
    private Coroutine _serveRoutine;
    private bool _servingInEditModeForTests;
    private Vector3 _lastServeTarget;
    private Vector3 _lastServeLocalTarget;
    private float _lastServeSpeedMultiplier = 1f;
    private float _lastServeYawOffsetDegrees;
    private float _lastServeLateralOffset;
    private bool _hasLastServeTarget;
    private bool _hasLastServeLocalTarget;

    public bool IsServing => _serveRoutine != null || _servingInEditModeForTests;

    private void Start()
    {
        ResolveTableTransform();

        if (autoStartOnPlay)
        {
            StartServing();
        }
    }

    public void StartServing()
    {
        if (IsServing) return;

        if (!Application.isPlaying)
        {
            _servingInEditModeForTests = true;
            PingPongEvents.TrainingStarted();
            return;
        }

        if (_serveRoutine == null)
        {
            _serveRoutine = StartCoroutine(ServeLoop());
            PingPongEvents.TrainingStarted();
        }
    }

    public void StopServing()
    {
        if (_servingInEditModeForTests)
        {
            _servingInEditModeForTests = false;
            PingPongEvents.TrainingFinished();
        }

        if (_serveRoutine != null)
        {
            StopCoroutine(_serveRoutine);
            _serveRoutine = null;
            PingPongEvents.TrainingFinished();
        }
    }

    public void ClearBalls()
    {
        ClearBallsWithoutScoring();
    }

    public void ClearBallsWithoutScoring()
    {
        ClearBalls(suppressMissReports: true);
    }

    public void ApplyServeVariationProfile(PingPongDifficulty difficulty)
    {
        if (!useDifficultyServeVariation || difficulty == PingPongDifficulty.Custom)
        {
            return;
        }

        enableServePathVariation = true;

        switch (difficulty)
        {
            case PingPongDifficulty.Advanced:
                serveTargetLateralRandomRange = 0.28f;
                serveTargetDepthRandomRange = 0.12f;
                serveYawRandomDegrees = 4.5f;
                serveSpeedJitter = 0.10f;
                serveSpinRandomness = 0.10f;
                serveEdgeSafetyMargin = 0.18f;
                sidespinRadiansPerSecond = Mathf.Min(maxServeSpin, 56f);
                break;
            case PingPongDifficulty.Challenge:
                serveTargetLateralRandomRange = 0.38f;
                serveTargetDepthRandomRange = 0.18f;
                serveYawRandomDegrees = 6.0f;
                serveSpeedJitter = 0.14f;
                serveSpinRandomness = 0.16f;
                serveEdgeSafetyMargin = 0.16f;
                sidespinRadiansPerSecond = Mathf.Min(maxServeSpin, 62f);
                break;
            default:
                serveTargetLateralRandomRange = 0.18f;
                serveTargetDepthRandomRange = 0.05f;
                serveYawRandomDegrees = 2.5f;
                serveSpeedJitter = 0.05f;
                serveSpinRandomness = 0.05f;
                serveEdgeSafetyMargin = 0.20f;
                sidespinRadiansPerSecond = Mathf.Min(maxServeSpin, 50f);
                break;
        }
    }

    private void ClearBalls(bool suppressMissReports)
    {
        if (ballContainer == null) return;
        for (int i = ballContainer.childCount - 1; i >= 0; i--)
        {
            var ball = ballContainer.GetChild(i).gameObject;
            if (suppressMissReports)
            {
                var lifetime = ball.GetComponent<BallLifetime>();
                if (lifetime != null)
                {
                    lifetime.SuppressMissReport();
                }
            }

            if (Application.isPlaying)
            {
                Destroy(ball);
            }
            else
            {
                DestroyImmediate(ball);
            }
        }
    }

    private IEnumerator ServeLoop()
    {
        while (true)
        {
            SpawnBall();
            yield return new WaitForSeconds(serveInterval);
        }
    }

    private void SpawnBall()
    {
        if (ballPrefab == null || spawnPoint == null || targetPoint == null) return;

        ResolveTableTransform();

        var ballObj = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity, ballContainer);
        ballObj.transform.localScale = PingPongGeometry.BallPrefabScale;
        var rb = ConfigureSpawnedBall(ballObj);
        if (rb == null) return;

        var variationProfile = ResolveServeVariationProfile();
        var target = GetDifficultyAwareServeTarget();

        var trajectoryTarget = target;
        if (bounceOnTableBeforePlayer)
        {
            trajectoryTarget = GetTableBounceTarget(target);
        }

        var actualServeSpeed = ResolveServeSpeedForThisServe(variationProfile);
        var velocity = CalculateServeVelocity(spawnPoint.position, trajectoryTarget, actualServeSpeed);
        var actualProfile = SelectServeProfile();
        var spin = CalculateProfileSpin(actualProfile, velocity, topspinRadiansPerSecond, backspinRadiansPerSecond, sidespinRadiansPerSecond);
        spin = ApplySpinRandomness(spin);
        spin = DampSidespinForWideTargets(spin, actualProfile, variationProfile);

        rb.velocity = velocity;
        PingPongBall.ConfigureSpinLimit(rb, maxServeSpin);
        rb.angularVelocity = Vector3.ClampMagnitude(spin, maxServeSpin);

        if (logServeDiagnostics)
        {
            Debug.Log(
                $"Serve variation diagnostics: target={target}, localTarget={(_hasLastServeLocalTarget ? _lastServeLocalTarget.ToString() : "n/a")}, " +
                $"lateralRange={variationProfile.lateralRange:0.###}, depthRange={variationProfile.depthRange:0.###}, " +
                $"yawDegrees={variationProfile.yawDegrees:0.###}, yawOffset={_lastServeYawOffsetDegrees:0.###}, " +
                $"speedMultiplier={_lastServeSpeedMultiplier:0.###}, spinRandomness={variationProfile.spinRandomness:0.###}");
        }

        PingPongEvents.BallServed(new BallServedInfo(ballObj, ballObj.transform.position, rb.velocity, rb.angularVelocity, actualProfile));
    }

    private Vector3 CalculateServeVelocity(Vector3 start, Vector3 target, float speed)
    {
        speed = Mathf.Max(0.1f, speed);
        var horizontalDelta = new Vector3(target.x - start.x, 0f, target.z - start.z);
        var horizontalDistance = horizontalDelta.magnitude;
        if (horizontalDistance <= 0.001f)
        {
            return (target - start).normalized * speed;
        }

        var timeToTarget = horizontalDistance / speed;
        var arcFactor = Mathf.Lerp(0.92f, 1.18f, Mathf.Clamp01(upwardArc));
        timeToTarget = Mathf.Clamp(timeToTarget * arcFactor, 0.55f, 1.05f);

        var velocity = horizontalDelta / timeToTarget;
        velocity.y = (target.y - start.y - 0.5f * Physics.gravity.y * timeToTarget * timeToTarget) / timeToTarget;

        if (TryGetTimeToNet(start, velocity, timeToTarget, out var timeToNet))
        {
            var requiredNetY = minimumNetClearanceHeight + Mathf.Max(0f, serveNetClearanceSafetyMargin);
            var yAtNet = PredictProjectileY(start.y, velocity.y, timeToNet);
            if (yAtNet < requiredNetY)
            {
                velocity.y += (requiredNetY - yAtNet) / timeToNet;
                yAtNet = PredictProjectileY(start.y, velocity.y, timeToNet);
            }

            if (yAtNet < requiredNetY)
            {
                velocity.y += (requiredNetY - yAtNet) / timeToNet;
                yAtNet = PredictProjectileY(start.y, velocity.y, timeToNet);
            }

            if (logServeDiagnostics)
            {
                Debug.Log(
                    $"Serve diagnostics: spawn={start}, target={target}, timeToNet={timeToNet:0.###}, " +
                    $"yAtNet={yAtNet:0.###}, minimumNetClearanceHeight={minimumNetClearanceHeight:0.###}, " +
                    $"finalVelocity={velocity}");
            }
        }

        return velocity;
    }

    private ServeVariationProfile ResolveServeVariationProfile()
    {
        if (!enableServePathVariation)
        {
            return new ServeVariationProfile(
                0f,
                0f,
                0f,
                0f,
                Mathf.Clamp01(serveSpinRandomness),
                Mathf.Max(0f, serveEdgeSafetyMargin));
        }

        return new ServeVariationProfile(
            Mathf.Max(0f, serveTargetLateralRandomRange),
            Mathf.Max(0f, serveTargetDepthRandomRange),
            Mathf.Max(0f, serveYawRandomDegrees),
            Mathf.Clamp(serveSpeedJitter, 0f, 0.35f),
            Mathf.Clamp01(serveSpinRandomness),
            Mathf.Max(0f, serveEdgeSafetyMargin));
    }

    private Vector3 GetDifficultyAwareServeTarget()
    {
        if (!enableServePathVariation)
        {
            var fallbackTarget = GetRandomizedTargetPoint();
            RememberServeTarget(fallbackTarget, Vector3.zero, false, 0f);
            return fallbackTarget;
        }

        if (targetPoint == null) return Vector3.zero;

        var profile = ResolveServeVariationProfile();
        if (UseTableRelativeServeTargets())
        {
            var baseLocalTarget = tableTransform.InverseTransformPoint(targetPoint.position);
            var localTarget = baseLocalTarget;
            var balancedBaseLocalX = ResolveBalancedBaseLocalX(baseLocalTarget.x, profile);
            localTarget.x = balancedBaseLocalX + Random.Range(-profile.lateralRange, profile.lateralRange);
            localTarget.z += Random.Range(-profile.depthRange, profile.depthRange);
            localTarget = ApplyYawOffsetAsTargetShift(localTarget, profile);
            localTarget.x = ClampLocalXInsideTable(localTarget.x, profile.edgeMargin);
            localTarget.z = ClampLocalZInsideServeZone(localTarget.z, baseLocalTarget.z, profile.edgeMargin);

            var target = tableTransform.TransformPoint(localTarget);
            target.y += Random.Range(-verticalRandomRange, verticalRandomRange);
            RememberServeTarget(target, localTarget, true, Mathf.Abs(localTarget.x - balancedBaseLocalX));
            return target;
        }

        var worldTarget = GetRandomizedTargetPoint();
        RememberServeTarget(worldTarget, Vector3.zero, false, 0f);
        return worldTarget;
    }

    private float ResolveServeSpeedForThisServe(ServeVariationProfile profile)
    {
        var jitter = enableServePathVariation ? Mathf.Clamp(profile.speedJitter, 0f, 0.35f) : 0f;
        _lastServeSpeedMultiplier = jitter > 0f ? Random.Range(1f - jitter, 1f + jitter) : 1f;
        return Mathf.Max(0.1f, serveSpeed * _lastServeSpeedMultiplier);
    }

    private float ClampLocalXInsideTable(float localX, float margin)
    {
        var halfWidth = PingPongGeometry.TableWidth * 0.5f;
        var safeHalfWidth = Mathf.Max(0.05f, halfWidth - Mathf.Max(0f, margin));
        return Mathf.Clamp(localX, -safeHalfWidth, safeHalfWidth);
    }

    private float ResolveBalancedBaseLocalX(float baseLocalX, ServeVariationProfile profile)
    {
        var centerPull = Mathf.Max(0f, profile.lateralRange) * 0.5f;
        return Mathf.MoveTowards(baseLocalX, 0f, centerPull);
    }

    private float ClampLocalZInsideServeZone(float localZ, float baseLocalZ, float margin)
    {
        var halfLength = PingPongGeometry.TableLength * 0.5f;
        var safeHalfLength = Mathf.Max(0.05f, halfLength - Mathf.Max(0f, margin));
        var safeMin = -safeHalfLength;
        var safeMax = safeHalfLength;
        var profile = ResolveServeVariationProfile();
        var depthRange = Mathf.Max(0f, profile.depthRange);
        var zoneMin = Mathf.Max(safeMin, baseLocalZ - depthRange);
        var zoneMax = Mathf.Min(safeMax, baseLocalZ + depthRange);

        if (zoneMin > zoneMax)
        {
            return Mathf.Clamp(localZ, safeMin, safeMax);
        }

        return Mathf.Clamp(localZ, zoneMin, zoneMax);
    }

    private Vector3 ApplyYawOffsetAsTargetShift(Vector3 localTarget, ServeVariationProfile profile)
    {
        _lastServeYawOffsetDegrees = profile.yawDegrees > 0f
            ? Random.Range(-profile.yawDegrees, profile.yawDegrees)
            : 0f;

        if (Mathf.Abs(_lastServeYawOffsetDegrees) <= 0.001f)
        {
            return localTarget;
        }

        var localStartZ = localTarget.z + PingPongGeometry.TableLength * 0.5f;
        if (spawnPoint != null && tableTransform != null)
        {
            localStartZ = tableTransform.InverseTransformPoint(spawnPoint.position).z;
        }

        var travelDepth = Mathf.Clamp(Mathf.Abs(localTarget.z - localStartZ), 0.25f, PingPongGeometry.TableLength);
        localTarget.x += Mathf.Tan(_lastServeYawOffsetDegrees * Mathf.Deg2Rad) * travelDepth;
        return localTarget;
    }

    private Vector3 GetRandomizedTargetPoint()
    {
        if (targetPoint == null) return Vector3.zero;

        if (UseTableRelativeServeTargets())
        {
            var localTarget = tableTransform.InverseTransformPoint(targetPoint.position);
            localTarget.x += Random.Range(-horizontalRandomRange, horizontalRandomRange);
            var target = tableTransform.TransformPoint(localTarget);
            target.y += Random.Range(-verticalRandomRange, verticalRandomRange);
            return target;
        }

        var worldTarget = targetPoint.position;
        worldTarget.x += Random.Range(-horizontalRandomRange, horizontalRandomRange);
        worldTarget.y += Random.Range(-verticalRandomRange, verticalRandomRange);
        return worldTarget;
    }

    private Vector3 GetTableBounceTarget(Vector3 target)
    {
        if (UseTableRelativeServeTargets())
        {
            var localTarget = tableTransform.InverseTransformPoint(target);
            var localBounce = new Vector3(localTarget.x, 0f, tableBounceLocalZ);
            var worldBounce = tableTransform.TransformPoint(localBounce);
            return new Vector3(worldBounce.x, tableBounceWorldY, worldBounce.z);
        }

        return new Vector3(target.x, tableBounceWorldY, tableBounceWorldZ);
    }

    private bool TryGetTimeToNet(Vector3 start, Vector3 velocity, float timeToTarget, out float timeToNet)
    {
        timeToNet = 0f;
        if (UseTableRelativeServeTargets())
        {
            var localStart = tableTransform.InverseTransformPoint(start);
            var localVelocity = tableTransform.InverseTransformVector(velocity);
            if (Mathf.Abs(localVelocity.z) <= 0.001f) return false;

            timeToNet = (netLocalZ - localStart.z) / localVelocity.z;
        }
        else
        {
            if (Mathf.Abs(velocity.z) <= 0.001f) return false;

            timeToNet = (netWorldZ - start.z) / velocity.z;
        }

        return timeToNet > 0f && timeToNet < timeToTarget;
    }

    private bool UseTableRelativeServeTargets()
    {
        if (!useTableRelativeServeTargets) return false;

        ResolveTableTransform();
        return tableTransform != null;
    }

    private void ResolveTableTransform()
    {
        if (!autoResolveTableTransform || tableTransform != null) return;

        tableTransform = PingPongTableRecenterOnEnter.FindTableRoot();
    }

    private static float PredictProjectileY(float startY, float velocityY, float time)
    {
        return startY + velocityY * time + 0.5f * Physics.gravity.y * time * time;
    }

    public static Vector3 CalculateProfileSpin(
        PingPongServeProfile profile,
        Vector3 launchVelocity,
        float topspinRadiansPerSecond,
        float backspinRadiansPerSecond,
        float sidespinRadiansPerSecond)
    {
        var flatVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
        if (flatVelocity.sqrMagnitude < 0.0001f)
        {
            flatVelocity = Vector3.back;
        }

        var forwardRollAxis = Vector3.Cross(Vector3.up, flatVelocity.normalized);
        if (forwardRollAxis.sqrMagnitude < 0.0001f)
        {
            forwardRollAxis = Vector3.right;
        }

        forwardRollAxis.Normalize();

        switch (profile)
        {
            case PingPongServeProfile.Topspin:
                return forwardRollAxis * Mathf.Max(0f, topspinRadiansPerSecond);
            case PingPongServeProfile.Backspin:
                return -forwardRollAxis * Mathf.Max(0f, backspinRadiansPerSecond);
            case PingPongServeProfile.Sidespin:
                return Vector3.up * Mathf.Max(0f, sidespinRadiansPerSecond);
            default:
                return Vector3.zero;
        }
    }

    private PingPongServeProfile SelectServeProfile()
    {
        if (serveProfile != PingPongServeProfile.RandomMixed)
        {
            return serveProfile;
        }

        var roll = Random.value;
        if (roll < 0.25f) return PingPongServeProfile.Basic;
        if (roll < 0.58f) return PingPongServeProfile.Topspin;
        if (roll < 0.84f) return PingPongServeProfile.Backspin;
        return PingPongServeProfile.Sidespin;
    }

    private Vector3 ApplySpinRandomness(Vector3 spin)
    {
        var spinMagnitude = spin.magnitude;
        if (spinMagnitude <= 0.001f || serveSpinRandomness <= 0f)
        {
            return spin;
        }

        return spin + Random.insideUnitSphere * (spinMagnitude * serveSpinRandomness);
    }

    private Vector3 DampSidespinForWideTargets(Vector3 spin, PingPongServeProfile actualProfile, ServeVariationProfile profile)
    {
        if (actualProfile != PingPongServeProfile.Sidespin || profile.lateralRange <= 0.001f)
        {
            return spin;
        }

        var lateralRatio = Mathf.Clamp01(_lastServeLateralOffset / profile.lateralRange);
        var damping = Mathf.Lerp(1f, 0.82f, lateralRatio);
        return spin * damping;
    }

    private void RememberServeTarget(Vector3 target, Vector3 localTarget, bool hasLocalTarget, float lateralOffset)
    {
        _lastServeTarget = target;
        _lastServeLocalTarget = localTarget;
        _hasLastServeTarget = true;
        _hasLastServeLocalTarget = hasLocalTarget;
        _lastServeLateralOffset = lateralOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawServeVariationGizmos || targetPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPoint.position, 0.04f);

        if (_hasLastServeTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastServeTarget, 0.05f);
        }

        if (!UseTableRelativeServeTargets()) return;

        var profile = ResolveServeVariationProfile();
        var baseLocal = tableTransform.InverseTransformPoint(targetPoint.position);
        var halfWidth = PingPongGeometry.TableWidth * 0.5f;
        var halfLength = PingPongGeometry.TableLength * 0.5f;
        var safeHalfWidth = Mathf.Max(0.05f, halfWidth - profile.edgeMargin);
        var safeHalfLength = Mathf.Max(0.05f, halfLength - profile.edgeMargin);
        var safeMinX = -safeHalfWidth;
        var safeMaxX = safeHalfWidth;
        var safeMinZ = -safeHalfLength;
        var safeMaxZ = safeHalfLength;
        var minX = Mathf.Max(safeMinX, baseLocal.x - profile.lateralRange);
        var maxX = Mathf.Min(safeMaxX, baseLocal.x + profile.lateralRange);
        var minZ = Mathf.Max(safeMinZ, baseLocal.z - profile.depthRange);
        var maxZ = Mathf.Min(safeMaxZ, baseLocal.z + profile.depthRange);
        if (minX > maxX || minZ > maxZ) return;

        var y = baseLocal.y;
        var a = tableTransform.TransformPoint(new Vector3(minX, y, minZ));
        var b = tableTransform.TransformPoint(new Vector3(maxX, y, minZ));
        var c = tableTransform.TransformPoint(new Vector3(maxX, y, maxZ));
        var d = tableTransform.TransformPoint(new Vector3(minX, y, maxZ));

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.85f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    private Rigidbody ConfigureSpawnedBall(GameObject ballObj)
    {
        var rb = ballObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ballObj.AddComponent<Rigidbody>();
        }

        if (rb == null) return null;

        rb.mass = spawnedBallMass;
        var pingPongBall = ballObj.GetComponent<PingPongBall>();
        if (pingPongBall == null)
        {
            pingPongBall = ballObj.AddComponent<PingPongBall>();
        }

        var ballLayer = LayerMask.NameToLayer("Ball");
        if (ballLayer >= 0)
        {
            SetLayerRecursively(ballObj, ballLayer);
        }

        rb.drag = pingPongBall != null && pingPongBall.useAerodynamics ? 0f : spawnedBallDrag;
        rb.angularDrag = spawnedBallAngularDrag;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        PingPongBall.ConfigureSpinLimit(rb, pingPongBall != null ? pingPongBall.maxAngularVelocity : PingPongBall.DefaultMaxAngularVelocity);

        var collider = ballObj.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = ballObj.AddComponent<SphereCollider>();
        }

        collider.radius = 0.5f;
        collider.isTrigger = false;
        collider.sharedMaterial = GetBallPhysicsMaterial();

        if (pingPongBall != null)
        {
            pingPongBall.ApplyGameplayBounceTuning(
                spawnedBallTableBounceMinUpSpeed,
                spawnedBallTableBounceMaxUpSpeed,
                spawnedBallTableBounceUpwardAssist,
                spawnedBallTableBounceHorizontalDamping,
                spawnedBallTableBounceMaxHorizontalSpeed);
            pingPongBall.ConfigureGameplayCollisionFilter(true);
        }

        if (ballObj.GetComponent<BallLifetime>() == null) ballObj.AddComponent<BallLifetime>();

        return rb;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0) return;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null)
            {
                child.gameObject.layer = layer;
            }
        }
    }

    private PhysicMaterial GetBallPhysicsMaterial()
    {
        if (_ballPhysicsMaterial == null)
        {
            _ballPhysicsMaterial = new PhysicMaterial("PingPongBallPhysics");
        }

        _ballPhysicsMaterial.bounciness = spawnedBallBounciness;
        _ballPhysicsMaterial.dynamicFriction = spawnedBallDynamicFriction;
        _ballPhysicsMaterial.staticFriction = spawnedBallStaticFriction;
        _ballPhysicsMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
        _ballPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        return _ballPhysicsMaterial;
    }

    private readonly struct ServeVariationProfile
    {
        public readonly float lateralRange;
        public readonly float depthRange;
        public readonly float yawDegrees;
        public readonly float speedJitter;
        public readonly float spinRandomness;
        public readonly float edgeMargin;

        public ServeVariationProfile(
            float lateralRange,
            float depthRange,
            float yawDegrees,
            float speedJitter,
            float spinRandomness,
            float edgeMargin)
        {
            this.lateralRange = lateralRange;
            this.depthRange = depthRange;
            this.yawDegrees = yawDegrees;
            this.speedJitter = speedJitter;
            this.spinRandomness = spinRandomness;
            this.edgeMargin = edgeMargin;
        }
    }
}
