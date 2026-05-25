using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-50)]
public class TableDragHandle : MonoBehaviour
{
    private const string SavedFlagSuffix = ".Saved";
    private const string PositionXSuffix = ".Position.X";
    private const string PositionYSuffix = ".Position.Y";
    private const string PositionZSuffix = ".Position.Z";

    public Transform tableRoot;
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public ControllerBallGrabber ballGrabber;
    public Transform[] syncedTransforms;
    public BallSpawner[] syncedSpawners;
    public ControllerTableCollisionLimiter[] syncedControllerLimiters;
    public float activationRadius = 0.18f;
    public float tableBounceLocalZ = -0.65f;
    public float minimumNetClearanceAboveNet = 0.16f;
    public bool lockTableHeight = true;
    public bool constrainToBounds = true;
    public Vector2 xBounds = new Vector2(-1.25f, 1.25f);
    public Vector2 zBounds = new Vector2(0.75f, 3.35f);
    public bool loadSavedPlacementOnEnable;
    public bool savePlacementOnRelease;
    public string placementSaveKey = "PingPong.MixedReality.Table";
    public Transform hmdTransform;
    public float positionSensitivity = 1.0f;
    public float rotationSensitivity = 0.35f;
    public float maxMoveSpeedMetersPerSecond = 3.0f;
    public float positionSmoothingSeconds = 0.025f;
    public float dragDeadZoneMeters = 0.005f;
    public float minUserTableDistanceMeters = 0.5f;
    public float maxUserTableDistanceMeters = 3f;
    public bool enableLocalHandleDrag = false;
    public bool hideLocalHandleVisuals = true;
    public bool enforceStandardTableHeightOnEnable = true;
    public float standardTableTopHeight = PingPongGeometry.TableTopHeight;

    private float _lockedTableY;
    private bool _dragging;
    private bool _loadedSavedPlacement;

    public bool IsDragging => _dragging;
    public bool HasSavedPlacement => PlayerPrefs.GetInt(placementSaveKey + SavedFlagSuffix, 0) == 1;

    private void OnEnable()
    {
        ResolveTableRoot();
        ApplyStandardTableHeightIfNeeded();
        _lockedTableY = tableRoot != null ? tableRoot.position.y : PingPongGeometry.TableCenter.y;
        _loadedSavedPlacement = false;
        SyncHeightDependentValues();
        ConfigureLocalHandleInteraction();

        if (loadSavedPlacementOnEnable)
        {
            LoadSavedPlacement();
        }
    }

    private void OnDisable()
    {
        _dragging = false;
    }

    private void Update()
    {
        ResolveTableRoot();
        _dragging = false;
    }

    public void SetTablePosition(Vector3 nextPosition)
    {
        ResolveTableRoot();
        if (tableRoot == null) return;

        var previousPosition = tableRoot.position;
        var delta = nextPosition - previousPosition;
        if (delta.sqrMagnitude <= 0.0000001f) return;

        tableRoot.position = nextPosition;
        SyncHeightDependentValues();
        AcceptTableTransform();

        if (syncedTransforms == null) return;
        foreach (var syncedTransform in syncedTransforms)
        {
            if (syncedTransform == null || syncedTransform == tableRoot || syncedTransform.IsChildOf(tableRoot)) continue;
            if (IsDetachedWorldUiTransform(syncedTransform)) continue;
            syncedTransform.position += delta;
        }
    }

    public bool LoadSavedPlacement()
    {
        ResolveTableRoot();
        if (tableRoot == null || _loadedSavedPlacement || !HasSavedPlacement) return false;

        var position = new Vector3(
            PlayerPrefs.GetFloat(placementSaveKey + PositionXSuffix, tableRoot.position.x),
            PlayerPrefs.GetFloat(placementSaveKey + PositionYSuffix, tableRoot.position.y),
            PlayerPrefs.GetFloat(placementSaveKey + PositionZSuffix, tableRoot.position.z));

        SetTablePosition(position);
        _lockedTableY = position.y;
        _loadedSavedPlacement = true;
        return true;
    }

    public void SavePlacement()
    {
        ResolveTableRoot();
        if (tableRoot == null || string.IsNullOrEmpty(placementSaveKey)) return;

        PlayerPrefs.SetInt(placementSaveKey + SavedFlagSuffix, 1);
        PlayerPrefs.SetFloat(placementSaveKey + PositionXSuffix, tableRoot.position.x);
        PlayerPrefs.SetFloat(placementSaveKey + PositionYSuffix, tableRoot.position.y);
        PlayerPrefs.SetFloat(placementSaveKey + PositionZSuffix, tableRoot.position.z);
        PlayerPrefs.Save();
    }

    public void ClearSavedPlacement()
    {
        PlayerPrefs.DeleteKey(placementSaveKey + SavedFlagSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionXSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionYSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionZSuffix);
        PlayerPrefs.Save();
    }

    public void SyncHeightDependentValues()
    {
        ResolveTableRoot();
        if (tableRoot == null) return;

        var tableTopY = GetTableTopY();
        SyncBallSpawners(tableTopY);
        SyncControllerLimiters(tableTopY);
    }

    private void SyncBallSpawners(float tableTopY)
    {
        if (syncedSpawners == null) return;

        foreach (var spawner in syncedSpawners)
        {
            if (spawner == null) continue;

            spawner.netWorldZ = tableRoot.position.z;
            var minimumNetClearanceHeight = tableTopY + PingPongGeometry.NetHeight + minimumNetClearanceAboveNet;
            spawner.minimumNetClearanceHeight = Mathf.Max(spawner.minimumNetClearanceHeight, minimumNetClearanceHeight);
            spawner.tableBounceWorldY = tableTopY + PingPongGeometry.BallRadius;
            spawner.tableBounceWorldZ = tableRoot.position.z + tableBounceLocalZ;
        }
    }

    private void SyncControllerLimiters(float tableTopY)
    {
        if (syncedControllerLimiters == null || syncedControllerLimiters.Length == 0)
        {
            syncedControllerLimiters = FindObjectsOfType<ControllerTableCollisionLimiter>(true);
        }

        if (syncedControllerLimiters == null) return;

        foreach (var limiter in syncedControllerLimiters)
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableRoot;
            limiter.tableTopY = tableTopY;
        }
    }

    private float GetTableTopY()
    {
        return tableRoot.position.y + PingPongGeometry.TableThickness * 0.5f;
    }

    public static bool IsDetachedWorldUiTransform(Transform candidate)
    {
        if (candidate == null) return false;
        if (candidate.GetComponentInParent<ComfortWorldSpaceUIPlacer>(true) != null) return true;
        if (candidate.GetComponentInParent<Canvas>(true) != null) return true;
        return candidate.name == "WorldSpaceCanvas" ||
               candidate.name == "ElderCareHomeCanvas" ||
               candidate.name.Contains("Canvas");
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

    private void ResolveTableRoot()
    {
        if (tableRoot != null) return;

        var table = GameObject.Find("Table");
        if (table != null)
        {
            tableRoot = table.transform;
        }
    }

    private void ApplyStandardTableHeightIfNeeded()
    {
        if (!enforceStandardTableHeightOnEnable || tableRoot == null) return;

        var targetY = Mathf.Max(0.1f, standardTableTopHeight) - PingPongGeometry.TableThickness * 0.5f;
        var position = tableRoot.position;
        if (Mathf.Abs(position.y - targetY) <= 0.0001f) return;

        tableRoot.position = new Vector3(position.x, targetY, position.z);
        AcceptTableTransform();
    }

    public void ConfigureLocalHandleInteraction()
    {
        var localColliders = GetComponentsInChildren<Collider>(true);
        foreach (var localCollider in localColliders)
        {
            if (localCollider != null)
            {
                localCollider.enabled = enableLocalHandleDrag;
            }
        }

        if (!hideLocalHandleVisuals) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }
}
