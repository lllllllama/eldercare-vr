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
    public bool autoStartOnPlay = true;

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
    public float topspinRadiansPerSecond = 95f;
    public float backspinRadiansPerSecond = 80f;
    public float sidespinRadiansPerSecond = 50f;
    [Range(0f, 1f)] public float serveSpinRandomness = 0.08f;
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
        if (ballContainer == null) return;
        for (int i = ballContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(ballContainer.GetChild(i).gameObject);
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

        Vector3 target = GetRandomizedTargetPoint();

        var trajectoryTarget = target;
        if (bounceOnTableBeforePlayer)
        {
            trajectoryTarget = GetTableBounceTarget(target);
        }

        var velocity = CalculateServeVelocity(spawnPoint.position, trajectoryTarget);
        var actualProfile = SelectServeProfile();
        var spin = CalculateProfileSpin(actualProfile, velocity, topspinRadiansPerSecond, backspinRadiansPerSecond, sidespinRadiansPerSecond);
        spin = ApplySpinRandomness(spin);

        rb.velocity = velocity;
        PingPongBall.ConfigureSpinLimit(rb, maxServeSpin);
        rb.angularVelocity = Vector3.ClampMagnitude(spin, maxServeSpin);

        PingPongEvents.BallServed(new BallServedInfo(ballObj, ballObj.transform.position, rb.velocity, rb.angularVelocity, actualProfile));
    }

    private Vector3 CalculateServeVelocity(Vector3 start, Vector3 target)
    {
        var horizontalDelta = new Vector3(target.x - start.x, 0f, target.z - start.z);
        var horizontalDistance = horizontalDelta.magnitude;
        if (horizontalDistance <= 0.001f)
        {
            return (target - start).normalized * serveSpeed;
        }

        var timeToTarget = horizontalDistance / Mathf.Max(serveSpeed, 0.1f);
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
}
