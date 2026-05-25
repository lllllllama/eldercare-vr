using UnityEngine;

[DefaultExecutionOrder(-60)]
public class PingPongSpatialSanitizer : MonoBehaviour
{
    public bool sanitizeOnStart = true;
    public float targetTableTopY = PingPongGeometry.TableTopHeight;
    public float uiPreferredY = 1.5f;
    public float uiMinY = 1.25f;
    public float uiMaxY = 1.75f;
    public bool fixTableHeight = true;
    public bool fixUiHeight = true;
    public bool ignoreCeilingPlanes = true;

    private void Start()
    {
        if (!sanitizeOnStart || !IsPingPongSceneContext()) return;

        RepairScene(
            targetTableTopY,
            uiPreferredY,
            uiMinY,
            uiMaxY,
            fixTableHeight,
            fixUiHeight);
    }

    public static SpatialRepairReport RepairScene(
        float targetTableTopY = PingPongGeometry.TableTopHeight,
        float uiPreferredY = 1.5f,
        float uiMinY = 1.25f,
        float uiMaxY = 1.75f,
        bool fixTableHeight = true,
        bool fixUiHeight = true)
    {
        var report = new SpatialRepairReport();
        var table = FindTable();

        if (fixTableHeight && table != null)
        {
            report.tableFound = true;
            report.tableTopY = ApplyTableHeightFromGround(table, targetTableTopY);
            SyncTableInteractionHelpers(table.transform, targetTableTopY);
        }

        if (fixUiHeight)
        {
            report.worldSpaceCanvasY = RepairUiRootHeight("WorldSpaceCanvas", uiPreferredY, uiMinY, uiMaxY);
            report.pingPongHomeCanvasY = RepairUiRootHeight("PingPongHomeCanvas", uiPreferredY, uiMinY, uiMaxY);
            report.pingPongHomeMenuY = RepairUiRootHeight("PingPongHomeMenu", uiPreferredY, uiMinY, uiMaxY);
            report.difficultyPanelY = RepairUiRootHeight("DifficultyPanel", uiPreferredY, uiMinY, uiMaxY);
            ConfigureComfortPlacers(uiPreferredY, uiMinY, uiMaxY);
        }

        return report;
    }

    public static float ApplyTableHeight(GameObject table, float targetTableTopY = PingPongGeometry.TableTopHeight)
    {
        return ApplyTableHeightFromGround(table, targetTableTopY);
    }

    public static float ApplyTableHeightFromGround(GameObject table, float targetTableTopY = PingPongGeometry.TableTopHeight)
    {
        if (table == null) return float.NaN;

        var tableCollider = table.GetComponent<BoxCollider>();
        if (tableCollider == null) return float.NaN;

        var deltaY = targetTableTopY - tableCollider.bounds.max.y;
        if (Mathf.Abs(deltaY) > 0.000001f)
        {
            table.transform.position += Vector3.up * deltaY;
            SyncStandaloneNetColliderHeight(table.transform, deltaY);
        }

        return tableCollider.bounds.max.y;
    }

    public static void SyncTableInteractionHelpers(Transform tableTransform, float targetTableTopY = PingPongGeometry.TableTopHeight)
    {
        if (tableTransform == null) return;

        foreach (var dragHandle in FindObjectsOfType<TableDragHandle>(true))
        {
            if (dragHandle == null) continue;

            dragHandle.tableRoot = tableTransform;
            dragHandle.standardTableTopHeight = targetTableTopY;
            dragHandle.tableBounceLocalZ = 1.35f - tableTransform.position.z;
            dragHandle.minimumNetClearanceAboveNet = 0.16f;
            dragHandle.SyncHeightDependentValues();
        }

        foreach (var spawner in FindObjectsOfType<BallSpawner>(true))
        {
            if (spawner == null) continue;

            spawner.tableBounceWorldY = targetTableTopY + PingPongGeometry.BallRadius;
            var minimumNetClearanceHeight = targetTableTopY + PingPongGeometry.NetHeight + 0.16f;
            spawner.minimumNetClearanceHeight = Mathf.Max(spawner.minimumNetClearanceHeight, minimumNetClearanceHeight);
            spawner.netWorldZ = tableTransform.position.z;
            spawner.tableBounceWorldZ = 1.35f;
        }

        foreach (var limiter in FindObjectsOfType<ControllerTableCollisionLimiter>(true))
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableTransform;
            limiter.tableTopY = targetTableTopY;
        }

        var passiveLock = tableTransform.GetComponent<TablePassiveMotionLock>();
        if (passiveLock != null)
        {
            passiveLock.standardTableTopHeight = targetTableTopY;
            passiveLock.AcceptCurrentTransform();
        }

        var tableCenterHeightAboveFloor = targetTableTopY - PingPongGeometry.TableThickness * 0.5f;
        foreach (var safety in FindObjectsOfType<PingPongPlayerTableSafety>(true))
        {
            if (safety == null) continue;

            safety.tableTransform = tableTransform;
            safety.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
        }

        foreach (var placer in FindObjectsOfType<PingPongOpenSpaceTablePlacer>(true))
        {
            if (placer == null) continue;

            placer.tableRoot = tableTransform;
            placer.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
            placer.fallbackFloorY = 0f;
            placer.ignoreCeilingPlanes = true;
        }

        foreach (var aligner in FindObjectsOfType<PingPongRoomPlaneAligner>(true))
        {
            if (aligner == null) continue;

            aligner.tableRoot = tableTransform;
            aligner.tableCenterHeightAboveFloor = tableCenterHeightAboveFloor;
            aligner.fallbackFloorY = 0f;
            aligner.ignoreCeilingPlanes = true;
        }
    }

    public static GameObject FindTable()
    {
        var pingPong = GameObject.Find("PingPong");
        var child = pingPong != null ? pingPong.transform.Find("Table") : null;
        if (child != null) return child.gameObject;

        return GameObject.Find("Table");
    }

    private static float RepairUiRootHeight(string objectName, float preferredY, float minY, float maxY)
    {
        var root = FindObjectByNameIncludingInactive(objectName);
        if (root == null) return float.NaN;

        var position = root.transform.position;
        position.y = Mathf.Clamp(preferredY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        root.transform.position = position;
        return position.y;
    }

    private static void ConfigureComfortPlacers(float preferredY, float minY, float maxY)
    {
        foreach (var placer in FindObjectsOfType<ComfortWorldSpaceUIPlacer>(true))
        {
            if (placer == null) continue;
            if (!IsPingPongUiRoot(placer.transform)) continue;

            placer.clampWorldHeight = true;
            placer.minWorldHeight = minY;
            placer.maxWorldHeight = maxY;
            placer.preferredWorldHeight = preferredY;
            placer.usePreferredHeightInsteadOfHeadHeight = true;
            placer.comfortFollowEnabled = false;
            placer.placeOnEnable = false;
            placer.startupRecenterSeconds = Mathf.Min(placer.startupRecenterSeconds, 0.35f);
            placer.startupRecenterFrames = Mathf.Min(placer.startupRecenterFrames, 4);
        }
    }

    private static bool IsPingPongUiRoot(Transform candidate)
    {
        if (candidate == null) return false;

        return candidate.name == "WorldSpaceCanvas" ||
               candidate.name == "PingPongHomeCanvas" ||
               candidate.GetComponentInParent<ScoreManager>(true) != null ||
               candidate.GetComponentInChildren<ScoreManager>(true) != null;
    }

    private static void SyncStandaloneNetColliderHeight(Transform tableTransform, float deltaY)
    {
        if (tableTransform == null || Mathf.Abs(deltaY) <= 0.000001f) return;

        var childNetCollider = tableTransform.Find("NetCollider");
        if (childNetCollider != null) return;

        var netCollider = GameObject.Find("NetCollider");
        if (netCollider == null || netCollider.transform.IsChildOf(tableTransform)) return;

        netCollider.transform.position += Vector3.up * deltaY;
    }

    private static GameObject FindObjectByNameIncludingInactive(string objectName)
    {
        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static bool IsPingPongSceneContext()
    {
        return GameObject.Find("PingPong") != null || FindTable() != null;
    }

    public struct SpatialRepairReport
    {
        public bool tableFound;
        public float tableTopY;
        public float worldSpaceCanvasY;
        public float difficultyPanelY;
        public float pingPongHomeCanvasY;
        public float pingPongHomeMenuY;
    }
}
