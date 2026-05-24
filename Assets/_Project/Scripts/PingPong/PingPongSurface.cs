using UnityEngine;

public enum PingPongSurfaceType
{
    Unknown,
    Table,
    Net,
    PaddleBody,
    PaddleHitZone,
    Floor
}

[DisallowMultipleComponent]
public class PingPongSurface : MonoBehaviour
{
    public PingPongSurfaceType surfaceType = PingPongSurfaceType.Unknown;
    [Range(0f, 1f)] public float normalRestitution = 0.78f;
    [Range(0f, 1f)] public float tangentialFriction = 0.08f;
    public bool useSweptFallback = true;
    public bool useTypeDefaults = true;
    public bool tuneNetVisualsForMr = true;
    [Range(0.15f, 1f)] public float netVisualAlpha = 0.88f;
    public Color netVisualColor = new Color(0.9f, 1f, 0.98f, 0.88f);

    private static PhysicMaterial _tableMaterial;
    private static PhysicMaterial _paddleMaterial;
    private static PhysicMaterial _floorMaterial;

    public bool IsPaddleSurface => surfaceType == PingPongSurfaceType.PaddleBody || surfaceType == PingPongSurfaceType.PaddleHitZone;

    private void Reset()
    {
        ApplyTypeDefaults();
    }

    private void Awake()
    {
        if (useTypeDefaults)
        {
            ApplyTypeDefaults();
        }

        ApplyRuntimePhysicMaterial();
        ApplyNetVisualReadability();
    }

    private void OnEnable()
    {
        ApplyNetVisualReadability();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (useTypeDefaults)
        {
            ApplyTypeDefaults();
        }
    }
#endif

    public void Configure(PingPongSurfaceType type)
    {
        surfaceType = type;
        ApplyTypeDefaults();
        if (Application.isPlaying)
        {
            ApplyRuntimePhysicMaterial();
            ApplyNetVisualReadability();
        }
    }

    public void ApplyTypeDefaults()
    {
        switch (surfaceType)
        {
            case PingPongSurfaceType.Table:
                normalRestitution = 0.93f;
                tangentialFriction = 0.05f;
                useSweptFallback = true;
                break;
            case PingPongSurfaceType.Net:
                normalRestitution = 0.28f;
                tangentialFriction = 0.45f;
                useSweptFallback = true;
                netVisualAlpha = 0.88f;
                netVisualColor = new Color(0.9f, 1f, 0.98f, 0.88f);
                break;
            case PingPongSurfaceType.PaddleBody:
                normalRestitution = 0.72f;
                tangentialFriction = 0.45f;
                useSweptFallback = true;
                break;
            case PingPongSurfaceType.PaddleHitZone:
                normalRestitution = 0.78f;
                tangentialFriction = 0.58f;
                useSweptFallback = true;
                break;
            case PingPongSurfaceType.Floor:
                normalRestitution = 0.45f;
                tangentialFriction = 0.35f;
                useSweptFallback = false;
                break;
            default:
                normalRestitution = 0.72f;
                tangentialFriction = 0.12f;
                useSweptFallback = false;
                break;
        }
    }

    public static PingPongSurface Find(Collider collider)
    {
        if (collider == null) return null;

        var surface = collider.GetComponent<PingPongSurface>() ?? collider.GetComponentInParent<PingPongSurface>();
        if (surface != null) return surface;

        var inferredType = InferType(collider);
        if (inferredType == PingPongSurfaceType.Unknown) return null;

        var inferred = collider.gameObject.AddComponent<PingPongSurface>();
        inferred.Configure(inferredType);
        return inferred;
    }

    public static PingPongSurfaceType InferType(Collider collider)
    {
        if (collider == null) return PingPongSurfaceType.Unknown;
        if (collider.GetComponentInParent<PlayerTableBoundary>() != null) return PingPongSurfaceType.Unknown;

        var tracker = collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            return collider.isTrigger || collider.name.ToLowerInvariant().Contains("hitzone")
                ? PingPongSurfaceType.PaddleHitZone
                : PingPongSurfaceType.PaddleBody;
        }

        var lowerName = collider.name.ToLowerInvariant();
        var lowerParentName = collider.transform.parent != null ? collider.transform.parent.name.ToLowerInvariant() : string.Empty;
        if (lowerName.Contains("net")) return PingPongSurfaceType.Net;
        if (lowerName.Contains("table") || lowerParentName.Contains("table")) return PingPongSurfaceType.Table;
        if (lowerName.Contains("floor") || lowerName.Contains("ground") || lowerName.Contains("plane")) return PingPongSurfaceType.Floor;

        return PingPongSurfaceType.Unknown;
    }

    public static Vector3 EstimateNormal(Collider collider, Vector3 ballCenter, Vector3 travelDirection)
    {
        var surface = Find(collider);
        var type = surface != null ? surface.surfaceType : InferType(collider);

        if (type == PingPongSurfaceType.Table || type == PingPongSurfaceType.Floor)
        {
            return OrientAgainstTravel(collider != null ? collider.transform.up : Vector3.up, travelDirection);
        }

        if (type == PingPongSurfaceType.Net)
        {
            var normal = collider != null ? collider.transform.forward : Vector3.forward;
            if (travelDirection.sqrMagnitude < 0.0001f && collider != null)
            {
                normal = Vector3.Dot(ballCenter - collider.bounds.center, normal) >= 0f ? normal : -normal;
            }

            return OrientAgainstTravel(normal, travelDirection);
        }

        if (type == PingPongSurfaceType.PaddleBody || type == PingPongSurfaceType.PaddleHitZone)
        {
            var tracker = collider != null ? collider.GetComponentInParent<PaddleVelocityTracker>() : null;
            if (tracker != null)
            {
                return OrientAgainstTravel(tracker.transform.up, travelDirection);
            }
        }

        if (collider is BoxCollider box)
        {
            return EstimateBoxNormal(box, ballCenter, travelDirection);
        }

        if (collider != null)
        {
            var closest = collider.ClosestPoint(ballCenter);
            var normal = ballCenter - closest;
            if (normal.sqrMagnitude > 0.0001f)
            {
                return OrientAgainstTravel(normal.normalized, travelDirection);
            }
        }

        return OrientAgainstTravel(Vector3.up, travelDirection);
    }

    private static Vector3 EstimateBoxNormal(BoxCollider box, Vector3 ballCenter, Vector3 travelDirection)
    {
        var local = box.transform.InverseTransformPoint(ballCenter) - box.center;
        var half = box.size * 0.5f;
        var dx = half.x > 0f ? Mathf.Abs(local.x) / half.x : 0f;
        var dy = half.y > 0f ? Mathf.Abs(local.y) / half.y : 0f;
        var dz = half.z > 0f ? Mathf.Abs(local.z) / half.z : 0f;

        Vector3 localNormal;
        if (dy >= dx && dy >= dz)
        {
            localNormal = new Vector3(0f, Mathf.Sign(local.y == 0f ? 1f : local.y), 0f);
        }
        else if (dx >= dz)
        {
            localNormal = new Vector3(Mathf.Sign(local.x == 0f ? 1f : local.x), 0f, 0f);
        }
        else
        {
            localNormal = new Vector3(0f, 0f, Mathf.Sign(local.z == 0f ? 1f : local.z));
        }

        return OrientAgainstTravel(box.transform.TransformDirection(localNormal), travelDirection);
    }

    private static Vector3 OrientAgainstTravel(Vector3 normal, Vector3 travelDirection)
    {
        if (normal.sqrMagnitude < 0.0001f) return Vector3.up;

        normal.Normalize();
        if (travelDirection.sqrMagnitude > 0.0001f && Vector3.Dot(normal, travelDirection) > 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private void ApplyRuntimePhysicMaterial()
    {
        var collider = GetComponent<Collider>();
        if (collider == null || collider.isTrigger) return;

        switch (surfaceType)
        {
            case PingPongSurfaceType.Table:
                collider.sharedMaterial = _tableMaterial ?? (_tableMaterial = CreateMaterial("PingPongTableRuntime", 0.05f, 0.93f));
                break;
            case PingPongSurfaceType.PaddleBody:
            case PingPongSurfaceType.PaddleHitZone:
                collider.sharedMaterial = _paddleMaterial ?? (_paddleMaterial = CreateMaterial("PingPongPaddleRuntime", 0.28f, 0.78f));
                break;
            case PingPongSurfaceType.Floor:
                collider.sharedMaterial = _floorMaterial ?? (_floorMaterial = CreateMaterial("PingPongFloorRuntime", 0.4f, 0.45f));
                break;
        }
    }

    private static PhysicMaterial CreateMaterial(string materialName, float friction, float bounciness)
    {
        return new PhysicMaterial(materialName)
        {
            dynamicFriction = friction,
            staticFriction = friction,
            bounciness = bounciness,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Maximum
        };
    }

    private void ApplyNetVisualReadability()
    {
        if (!tuneNetVisualsForMr || surfaceType != PingPongSurfaceType.Net) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var netRenderer in renderers)
        {
            if (netRenderer == null) continue;

            var materials = netRenderer.materials;
            for (var i = 0; i < materials.Length; i++)
            {
                ConfigureTransparentNetMaterial(materials[i], netVisualColor, netVisualAlpha);
            }
        }
    }

    private static void ConfigureTransparentNetMaterial(Material material, Color baseColor, float alpha)
    {
        if (material == null) return;

        var color = baseColor;
        color.a = Mathf.Clamp(alpha, 0.55f, 1f);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
