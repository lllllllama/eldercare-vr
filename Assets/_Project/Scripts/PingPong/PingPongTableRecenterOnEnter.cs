using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class PingPongTableRecenterOnEnter : MonoBehaviour
{
    public bool recenterOnStart = true;
    public Transform tableRoot;
    public Transform headTransform;
    public float tableDistanceInFront = 1.7f;
    public float targetTableTopY = PingPongGeometry.TableTopHeight;
    public bool preserveCurrentTableTopHeight = true;
    public bool rotateTableToFacePlayer = true;
    public float yawOffsetDegrees = 0f;
    public bool syncBallSpawner = true;
    public bool syncServeTransforms = true;
    public bool syncDifficultyPanel = true;
    public bool syncTableHelpers = true;
    public bool acceptPassiveLockAfterMove = true;
    public float startDelaySeconds = 0.15f;
    public float lateLinkedObjectSyncSeconds = 2f;
    public float lateLinkedObjectSyncIntervalSeconds = 0.1f;

    private bool _hasRecentered;
    private Coroutine _lateSyncRoutine;

    private void Start()
    {
        if (!recenterOnStart || _hasRecentered) return;
        StartCoroutine(RecenterAfterDelay());
    }

    [ContextMenu("Recenter Table In Front Of Player Once")]
    public void RecenterNowFromContext()
    {
        RecenterNow();
    }

    public bool RecenterNow()
    {
        ResolveReferences();
        var success = RecenterTableInFrontOfPlayer(
            tableRoot,
            headTransform,
            tableDistanceInFront,
            targetTableTopY,
            preserveCurrentTableTopHeight,
            rotateTableToFacePlayer,
            yawOffsetDegrees,
            syncBallSpawner,
            syncServeTransforms,
            syncDifficultyPanel,
            syncTableHelpers,
            acceptPassiveLockAfterMove,
            true,
            out var report);

        if (success)
        {
            _hasRecentered = true;
            StartLateLinkedObjectSync(report);
        }

        return success;
    }

    private IEnumerator RecenterAfterDelay()
    {
        var delay = Mathf.Max(0f, startDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        RecenterNow();
    }

    public static bool RecenterTableInFrontOfPlayer(
        Transform tableRoot,
        Transform headTransform,
        float tableDistanceInFront,
        float targetTableTopY,
        bool preserveCurrentTableTopHeight,
        bool rotateTableToFacePlayer,
        float yawOffsetDegrees,
        bool syncBallSpawner,
        bool syncTableHelpers,
        bool acceptPassiveLockAfterMove,
        bool logResult,
        out RecenterReport report)
    {
        return RecenterTableInFrontOfPlayer(
            tableRoot,
            headTransform,
            tableDistanceInFront,
            targetTableTopY,
            preserveCurrentTableTopHeight,
            rotateTableToFacePlayer,
            yawOffsetDegrees,
            syncBallSpawner,
            true,
            true,
            syncTableHelpers,
            acceptPassiveLockAfterMove,
            logResult,
            out report);
    }

    public static bool RecenterTableInFrontOfPlayer(
        Transform tableRoot,
        Transform headTransform,
        float tableDistanceInFront,
        float targetTableTopY,
        bool preserveCurrentTableTopHeight,
        bool rotateTableToFacePlayer,
        float yawOffsetDegrees,
        bool syncBallSpawner,
        bool syncServeTransforms,
        bool syncDifficultyPanel,
        bool syncTableHelpers,
        bool acceptPassiveLockAfterMove,
        bool logResult,
        out RecenterReport report)
    {
        report = new RecenterReport();
        if (tableRoot == null)
        {
            Debug.LogWarning("Cannot recenter ping pong table because Table was not found.");
            return false;
        }

        if (headTransform == null)
        {
            Debug.LogWarning("Cannot recenter table because Camera.main / HMD transform was not found.");
            return false;
        }

        var forward = GetHeadYawForward(headTransform);
        var oldPosition = tableRoot.position;
        var oldRotation = tableRoot.rotation;
        var tableCollider = tableRoot.GetComponent<BoxCollider>();
        var currentTopY = GetTableTopY(tableRoot, tableCollider);
        var linkedTransforms = CaptureLinkedTransforms(tableRoot, syncServeTransforms, syncDifficultyPanel);
        var targetPosition = headTransform.position + forward * Mathf.Max(0.1f, tableDistanceInFront);
        targetPosition.y = preserveCurrentTableTopHeight
            ? tableRoot.position.y
            : tableRoot.position.y + targetTableTopY - currentTopY;

        tableRoot.position = targetPosition;

        if (rotateTableToFacePlayer)
        {
            var yaw = Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y + yawOffsetDegrees;
            var euler = tableRoot.eulerAngles;
            tableRoot.rotation = Quaternion.Euler(euler.x, yaw, euler.z);
        }

        var deltaPosition = tableRoot.position - oldPosition;
        ApplyLinkedTransforms(tableRoot, linkedTransforms);
        SyncStandaloneNetCollider(tableRoot, deltaPosition);

        tableCollider = tableRoot.GetComponent<BoxCollider>();
        var tableTopY = GetTableTopY(tableRoot, tableCollider);
        if (syncBallSpawner)
        {
            SyncBallSpawners(tableRoot, tableTopY);
        }

        if (syncTableHelpers)
        {
            SyncTableHelpers(tableRoot, tableTopY);
        }

        if (acceptPassiveLockAfterMove)
        {
            AcceptPassiveLock(tableRoot);
        }

        report.headPosition = headTransform.position;
        report.forward = forward;
        report.oldTablePosition = oldPosition;
        report.newTablePosition = tableRoot.position;
        report.oldTableRotation = oldRotation;
        report.newTableRotation = tableRoot.rotation;
        report.tableTopY = tableTopY;
        report.yawOffsetDegrees = yawOffsetDegrees;
        report.syncedLinkedTransforms = ExtractSyncedTransforms(linkedTransforms);

        if (logResult)
        {
            Debug.Log(
                "PingPong table recentered in front of player:\n" +
                $"- head position: {FormatVector(report.headPosition)}\n" +
                $"- forward: {FormatVector(report.forward)}\n" +
                $"- old table position: {FormatVector(report.oldTablePosition)}\n" +
                $"- new table position: {FormatVector(report.newTablePosition)}\n" +
                $"- table top Y: {report.tableTopY:0.###}\n" +
                $"- yaw offset: {report.yawOffsetDegrees:0.###}");
        }

        return true;
    }

    public static Transform FindTableRoot()
    {
        var pingPong = GameObject.Find("PingPong");
        var child = pingPong != null ? pingPong.transform.Find("Table") : null;
        if (child != null) return child;

        var table = GameObject.Find("Table");
        return table != null ? table.transform : null;
    }

    public static Transform FindHeadTransform()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        var mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        var centerEye = GameObject.Find("CenterEye");
        if (centerEye != null)
        {
            return centerEye.transform;
        }

        foreach (var camera in FindObjectsOfType<Camera>(true))
        {
            if (camera != null && camera.name.Contains("Camera"))
            {
                return camera.transform;
            }
        }

        return null;
    }

    private void ResolveReferences()
    {
        if (tableRoot == null)
        {
            tableRoot = FindTableRoot();
        }

        if (headTransform == null)
        {
            headTransform = FindHeadTransform();
        }
    }

    private static Vector3 GetHeadYawForward(Transform head)
    {
        if (head == null)
        {
            return Vector3.forward;
        }

        var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private static float GetTableTopY(Transform tableRoot, BoxCollider tableCollider)
    {
        if (tableCollider != null)
        {
            return tableCollider.bounds.max.y;
        }

        return tableRoot != null
            ? tableRoot.position.y + PingPongGeometry.TableThickness * 0.5f
            : PingPongGeometry.TableTopHeight;
    }

    private static void SyncBallSpawners(Transform tableRoot, float tableTopY)
    {
        foreach (var spawner in FindObjectsOfType<BallSpawner>(true))
        {
            if (spawner == null) continue;

            spawner.useTableRelativeServeTargets = true;
            spawner.tableTransform = tableRoot;
            spawner.netLocalZ = 0f;
            spawner.tableBounceWorldY = tableTopY + PingPongGeometry.BallRadius;
            spawner.minimumNetClearanceHeight = tableTopY + PingPongGeometry.NetHeight + 0.16f;
            spawner.netWorldZ = tableRoot.TransformPoint(new Vector3(0f, 0f, spawner.netLocalZ)).z;
            spawner.tableBounceWorldZ = tableRoot.TransformPoint(new Vector3(0f, 0f, spawner.tableBounceLocalZ)).z;
        }
    }

    private static List<TrackedTransform> CaptureLinkedTransforms(Transform tableRoot, bool syncServeTransforms, bool syncDifficultyPanel)
    {
        var tracked = new List<TrackedTransform>();
        if (tableRoot == null) return tracked;

        if (syncServeTransforms)
        {
            foreach (var spawner in FindObjectsOfType<BallSpawner>(true))
            {
                if (spawner == null) continue;

                TrackTransform(tableRoot, spawner.spawnPoint, tracked);
                TrackTransform(tableRoot, spawner.targetPoint, tracked);
            }
        }

        if (syncDifficultyPanel)
        {
            TrackTransform(tableRoot, FindTransformByName("DifficultyPanel"), tracked);
        }

        return tracked;
    }

    private static void TrackTransform(Transform tableRoot, Transform linkedTransform, List<TrackedTransform> tracked)
    {
        if (tableRoot == null || linkedTransform == null || tracked == null) return;
        if (linkedTransform == tableRoot || linkedTransform.IsChildOf(tableRoot)) return;

        for (var i = 0; i < tracked.Count; i++)
        {
            if (tracked[i].transform == linkedTransform) return;
        }

        tracked.Add(new TrackedTransform
        {
            transform = linkedTransform,
            localPositionFromTable = tableRoot.InverseTransformPoint(linkedTransform.position),
            localRotationFromTable = Quaternion.Inverse(tableRoot.rotation) * linkedTransform.rotation
        });
    }

    private static void ApplyLinkedTransforms(Transform tableRoot, List<TrackedTransform> tracked)
    {
        if (tableRoot == null || tracked == null) return;

        for (var i = 0; i < tracked.Count; i++)
        {
            var item = tracked[i];
            if (item.transform == null) continue;

            item.transform.position = tableRoot.TransformPoint(item.localPositionFromTable);
            item.transform.rotation = tableRoot.rotation * item.localRotationFromTable;
        }
    }

    private void StartLateLinkedObjectSync(RecenterReport report)
    {
        if (lateLinkedObjectSyncSeconds <= 0f || tableRoot == null) return;

        if (_lateSyncRoutine != null)
        {
            StopCoroutine(_lateSyncRoutine);
        }

        _lateSyncRoutine = StartCoroutine(SyncLateLinkedObjects(report));
    }

    private IEnumerator SyncLateLinkedObjects(RecenterReport report)
    {
        var synced = new HashSet<Transform>();
        if (report.syncedLinkedTransforms != null)
        {
            for (var i = 0; i < report.syncedLinkedTransforms.Length; i++)
            {
                if (report.syncedLinkedTransforms[i] != null)
                {
                    synced.Add(report.syncedLinkedTransforms[i]);
                }
            }
        }

        var elapsed = 0f;
        var interval = Mathf.Max(0.02f, lateLinkedObjectSyncIntervalSeconds);
        while (elapsed < lateLinkedObjectSyncSeconds)
        {
            SyncNewLinkedTransformsFromPreviousTablePose(report, synced);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        _lateSyncRoutine = null;
    }

    private void SyncNewLinkedTransformsFromPreviousTablePose(RecenterReport report, HashSet<Transform> synced)
    {
        if (tableRoot == null || synced == null) return;

        var candidates = CaptureLinkedTransforms(tableRoot, syncServeTransforms, syncDifficultyPanel);
        for (var i = 0; i < candidates.Count; i++)
        {
            var linkedTransform = candidates[i].transform;
            if (linkedTransform == null || synced.Contains(linkedTransform)) continue;

            ApplyTransformFromTablePose(
                linkedTransform,
                report.oldTablePosition,
                report.oldTableRotation,
                report.newTablePosition,
                report.newTableRotation);
            synced.Add(linkedTransform);
        }
    }

    private static void ApplyTransformFromTablePose(
        Transform linkedTransform,
        Vector3 oldTablePosition,
        Quaternion oldTableRotation,
        Vector3 newTablePosition,
        Quaternion newTableRotation)
    {
        if (linkedTransform == null) return;

        var localPosition = Quaternion.Inverse(oldTableRotation) * (linkedTransform.position - oldTablePosition);
        var localRotation = Quaternion.Inverse(oldTableRotation) * linkedTransform.rotation;
        linkedTransform.position = newTablePosition + newTableRotation * localPosition;
        linkedTransform.rotation = newTableRotation * localRotation;
    }

    private static Transform[] ExtractSyncedTransforms(List<TrackedTransform> tracked)
    {
        if (tracked == null || tracked.Count == 0) return null;

        var transforms = new Transform[tracked.Count];
        for (var i = 0; i < tracked.Count; i++)
        {
            transforms[i] = tracked[i].transform;
        }

        return transforms;
    }

    private static Transform FindTransformByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform != null &&
                transform.name == objectName &&
                transform.GetComponentInParent<PingPongUnifiedControlPanel>(true) == null)
            {
                return transform;
            }
        }

        return null;
    }

    private static void SyncTableHelpers(Transform tableRoot, float tableTopY)
    {
        foreach (var dragHandle in FindObjectsOfType<TableDragHandle>(true))
        {
            if (dragHandle == null) continue;

            dragHandle.tableRoot = tableRoot;
            dragHandle.SyncHeightDependentValues();
        }

        foreach (var limiter in FindObjectsOfType<ControllerTableCollisionLimiter>(true))
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableRoot;
            limiter.tableTopY = tableTopY;
        }

        var tableCenterHeightAboveFloor = tableTopY - PingPongGeometry.TableThickness * 0.5f;
        foreach (var safety in FindObjectsOfType<PingPongPlayerTableSafety>(true))
        {
            if (safety == null) continue;

            safety.tableTransform = tableRoot;
            safety.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
        }
    }

    private static void AcceptPassiveLock(Transform tableRoot)
    {
        if (tableRoot == null) return;

        var passiveLock = tableRoot.GetComponent<TablePassiveMotionLock>();
        if (passiveLock != null)
        {
            passiveLock.AcceptCurrentTransform();
        }
    }

    private static void SyncStandaloneNetCollider(Transform tableRoot, Vector3 deltaPosition)
    {
        if (tableRoot == null || deltaPosition.sqrMagnitude <= 0.0000001f) return;

        var childNet = tableRoot.Find("NetCollider");
        if (childNet != null) return;

        var net = GameObject.Find("NetCollider");
        if (net == null || net.transform.IsChildOf(tableRoot)) return;

        net.transform.position += deltaPosition;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    public struct RecenterReport
    {
        public Vector3 headPosition;
        public Vector3 forward;
        public Vector3 oldTablePosition;
        public Vector3 newTablePosition;
        public Quaternion oldTableRotation;
        public Quaternion newTableRotation;
        public float tableTopY;
        public float yawOffsetDegrees;
        public Transform[] syncedLinkedTransforms;
    }

    private struct TrackedTransform
    {
        public Transform transform;
        public Vector3 localPositionFromTable;
        public Quaternion localRotationFromTable;
    }
}
