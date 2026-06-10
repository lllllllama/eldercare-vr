using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.PXR;

[DefaultExecutionOrder(-40)]
public class PingPongRoomPlaneAligner : MonoBehaviour
{
    public Transform tableRoot;
    public TableDragHandle tableDragHandle;
    public bool autoStartPlaneDetection = true;
    public bool autoAlignTableHeightToFloor = true;
    public bool alignOnlyOnce = true;
    public bool savePlacementAfterFloorAlignment = true;
    public bool upgradeToOpenSpacePlacement = true;
    public bool disableSpatialTablePlacementForNow = true;
    public float tableCenterHeightAboveFloor = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness * 0.5f;
    public float fallbackFloorY = 0f;
    public bool ignoreCeilingPlanes = true;
    public float maxAcceptedFloorY = 0.4f;
    public float minimumFloorBelowHeadMeters = 0.6f;
    public float minimumFloorNormalDot = 0.85f;
    public float minimumHeightChange = 0.01f;
    public float maximumFloorDistance = 3f;

    private bool _alignedToFloor;

    private void Start()
    {
        ResolveReferences();
        if (disableSpatialTablePlacementForNow)
        {
            return;
        }

        if (upgradeToOpenSpacePlacement)
        {
            EnsureOpenSpacePlacement();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (autoStartPlaneDetection)
        {
            StartPlaneDetectionAsync();
        }
#else
        if (autoStartPlaneDetection)
        {
            Debug.Log("PingPong MR plane alignment is armed. Plane detection starts on a supported PICO Android runtime.");
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private async void StartPlaneDetectionAsync()
    {
        try
        {
            await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.PlaneDetection);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PingPong MR plane detection failed to start. {ex.GetType().Name}: {ex.Message}");
        }
    }
#endif

    private void OnEnable()
    {
        if (disableSpatialTablePlacementForNow) return;
        if (upgradeToOpenSpacePlacement) return;
        PXR_Manager.PlaneDetectionDataUpdated += HandlePlaneDetectionDataUpdated;
    }

    private void OnDisable()
    {
        PXR_Manager.PlaneDetectionDataUpdated -= HandlePlaneDetectionDataUpdated;
    }

    private void HandlePlaneDetectionDataUpdated(List<PxrPlaneData> planeDatas)
    {
        if (upgradeToOpenSpacePlacement) return;
        if (!autoAlignTableHeightToFloor || planeDatas == null || planeDatas.Count == 0) return;
        if (alignOnlyOnce && _alignedToFloor) return;
        if (tableDragHandle != null && tableDragHandle.IsDragging) return;

        ResolveReferences();
        if (tableRoot == null) return;
        if (!TryFindBestFloorY(planeDatas, tableRoot.position, out var floorY))
        {
            floorY = fallbackFloorY;
        }

        var targetPosition = tableRoot.position;
        targetPosition.y = floorY + tableCenterHeightAboveFloor;
        if (Mathf.Abs(targetPosition.y - tableRoot.position.y) < minimumHeightChange) return;
        var delta = targetPosition - tableRoot.position;

        if (tableDragHandle != null)
        {
            tableDragHandle.SetTablePosition(targetPosition);
            tableDragHandle.SyncHeightDependentValues();
            if (savePlacementAfterFloorAlignment)
            {
                tableDragHandle.SavePlacement();
            }
        }
        else
        {
            tableRoot.position = targetPosition;
            AcceptTableTransform();
            SyncDetachedSceneTransforms(delta);
            SyncHeightDependentSceneValues();
        }

        _alignedToFloor = true;
    }

    private void AcceptTableTransform()
    {
        if (tableRoot == null) return;

        var motionLock = tableRoot.GetComponent<TablePassiveMotionLock>();
        if (motionLock != null)
        {
            motionLock.AcceptCurrentTransform();
        }
    }

    private void SyncDetachedSceneTransforms(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.0000001f) return;

        var moved = new HashSet<Transform>();
        foreach (var spawner in FindObjectsOfType<BallSpawner>(true))
        {
            if (spawner == null) continue;

            MoveIfDetached(spawner.spawnPoint, delta, moved);
            MoveIfDetached(spawner.targetPoint, delta, moved);
        }

        foreach (var boundary in FindObjectsOfType<PlayerTableBoundary>(true))
        {
            if (boundary == null) continue;

            MoveIfDetached(boundary.transform, delta, moved);
            boundary.tableTransform = tableRoot;
            boundary.tableCenter = tableRoot.position;
        }

        MoveIfDetached(FindObjectByNameIncludingInactive("Net"), delta, moved);
    }

    private void MoveIfDetached(Transform target, Vector3 delta, HashSet<Transform> moved)
    {
        if (target == null || target == tableRoot || target.IsChildOf(tableRoot) || moved.Contains(target)) return;
        if (TableDragHandle.IsDetachedWorldUiTransform(target)) return;

        target.position += delta;
        moved.Add(target);
    }

    private static Transform FindObjectByNameIncludingInactive(string objectName)
    {
        foreach (var transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name == objectName)
            {
                return transform;
            }
        }

        return null;
    }

    private void SyncHeightDependentSceneValues()
    {
        if (tableRoot == null) return;

        var tableTopY = tableRoot.position.y + PingPongGeometry.TableThickness * 0.5f;
        foreach (var spawner in FindObjectsOfType<BallSpawner>(true))
        {
            if (spawner == null) continue;

            spawner.netWorldZ = tableRoot.position.z;
            var minimumNetClearanceHeight = tableTopY + PingPongGeometry.NetHeight + 0.08f;
            spawner.minimumNetClearanceHeight = Mathf.Max(spawner.minimumNetClearanceHeight, minimumNetClearanceHeight);
            spawner.tableBounceWorldY = tableTopY + PingPongGeometry.BallRadius;
        }

        foreach (var limiter in FindObjectsOfType<ControllerTableCollisionLimiter>(true))
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableRoot;
            limiter.tableTopY = tableTopY;
        }
    }

    private bool TryFindBestFloorY(List<PxrPlaneData> planeDatas, Vector3 referencePosition, out float floorY)
    {
        var bestDistance = float.MaxValue;
        floorY = 0f;

        foreach (var plane in planeDatas)
        {
            var planeY = GetPlaneAverageY(plane);
            if (!IsUsableFloorPlane(plane, planeY)) continue;

            var horizontalDelta = new Vector2(referencePosition.x - plane.position.x, referencePosition.z - plane.position.z);
            var distance = horizontalDelta.magnitude;
            if (distance > maximumFloorDistance || distance >= bestDistance) continue;

            bestDistance = distance;
            floorY = planeY;
        }

        return bestDistance < float.MaxValue;
    }

    private bool IsUsableFloorPlane(PxrPlaneData plane, float planeY)
    {
        if (plane.state == MeshChangeState.Removed) return false;
        if (ignoreCeilingPlanes && !IsReliableFloorPlaneHeight(planeY)) return false;
        if (ignoreCeilingPlanes && Vector3.Dot(GetPlaneNormal(plane), Vector3.up) < Mathf.Clamp01(minimumFloorNormalDot)) return false;
        if (plane.label == PxrSemanticLabel.Floor) return true;

        return plane.label == PxrSemanticLabel.Unknown &&
               plane.orientationMode == PxrPlaneOrientation.HorizontalUpward;
    }

    private bool IsReliableFloorPlaneHeight(float planeY)
    {
        var camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
        var headY = camera != null ? camera.transform.position.y : 1.6f;
        if (planeY > maxAcceptedFloorY) return false;
        if (planeY > headY - Mathf.Max(0.1f, minimumFloorBelowHeadMeters)) return false;
        return true;
    }

    private static Vector3 GetPlaneNormal(PxrPlaneData plane)
    {
        var normal = plane.rotation * Vector3.up;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector3.up;
        }

        return normal.normalized;
    }

    private static float GetPlaneAverageY(PxrPlaneData plane)
    {
        if (plane.vertices == null || plane.vertices.Length == 0)
        {
            return plane.position.y;
        }

        var sum = 0f;
        for (var i = 0; i < plane.vertices.Length; i++)
        {
            sum += (plane.position + plane.rotation * plane.vertices[i]).y;
        }

        return sum / plane.vertices.Length;
    }

    private void ResolveReferences()
    {
        if (tableRoot == null)
        {
            var table = GameObject.Find("Table");
            if (table != null)
            {
                tableRoot = table.transform;
            }
        }

        if (tableDragHandle == null)
        {
            tableDragHandle = FindObjectOfType<TableDragHandle>();
        }
    }

    private void EnsureOpenSpacePlacement()
    {
        ResolveReferences();

        var placer = FindObjectOfType<PingPongOpenSpaceTablePlacer>(true);
        if (placer == null)
        {
            placer = gameObject.AddComponent<PingPongOpenSpaceTablePlacer>();
        }

        placer.tableRoot = tableRoot;
        placer.tableDragHandle = tableDragHandle;
        placer.hmdTransform = Camera.main != null ? Camera.main.transform : null;
        placer.remoteDragControllerTransform = tableDragHandle != null ? tableDragHandle.controllerTransform : null;
        placer.ballGrabber = tableDragHandle != null ? tableDragHandle.ballGrabber : FindObjectOfType<ControllerBallGrabber>(true);
        placer.interactionState = SimpleGripInteractionState.EnsureInstance();
        placer.ballSpawners = FindObjectsOfType<BallSpawner>(true);
        placer.autoPlaceOnStart = false;
        placer.clearSavedPlacementOnStart = true;
        placer.controlServing = true;
        placer.allowAutomaticResumeServing = false;
        placer.clearBallsWhenTableMoves = true;
        placer.startServingAfterClearPlacement = false;
        placer.startServingAfterManualPlacement = false;
        placer.startServingAfterConfirmedPlacementOnly = false;
        placer.disableSpatialTablePlacementForNow = true;
        placer.requireRoomSensingColliderForAutoPlacement = true;
        placer.minimumRoomSensingColliderCount = 1;
        placer.desiredDistanceMeters = 2.05f;
        placer.minDistanceMeters = 1.35f;
        placer.maxDistanceMeters = 3.8f;
        placer.clearanceRadiusMeters = 1.65f;
        placer.clearanceHeightMeters = 1.15f;
        placer.fallbackFloorY = 0f;
        placer.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
        placer.ignoreCeilingPlanes = ignoreCeilingPlanes;
        placer.maxAcceptedFloorY = maxAcceptedFloorY;
        placer.minimumFloorBelowHeadMeters = minimumFloorBelowHeadMeters;
        placer.searchDurationSeconds = 8f;
        placer.searchIntervalSeconds = 0.5f;
        placer.enableRemoteDrag = true;
        placer.remoteDragControllerNode = XRNode.LeftHand;
        placer.remoteGrabSelectableRadiusMeters = 2.35f;
        placer.remoteGrabMaxDistanceMeters = 8f;
        placer.remoteDragMaxRayDistanceMeters = 8f;
        placer.remoteDragActivationRadiusMeters = 2.35f;
        placer.positionSensitivity = 1.0f;
        placer.rotationSensitivity = 0.35f;
        placer.maxMoveSpeedMetersPerSecond = 3.0f;
        placer.positionSmoothingSeconds = 0.025f;
        placer.dragDeadZoneMeters = 0.005f;
        placer.minUserTableDistanceMeters = 0.5f;
        placer.maxUserTableDistanceMeters = 3f;
        placer.enabled = true;

        var suppressor = FindObjectOfType<MrBackgroundVisualSuppressor>(true);
        if (suppressor == null)
        {
            suppressor = gameObject.AddComponent<MrBackgroundVisualSuppressor>();
        }

        suppressor.hideAllEnvironmentRenderers = true;
        suppressor.hideAllRoomSensingRenderers = true;
    }
}
