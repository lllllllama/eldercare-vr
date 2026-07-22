using System.Collections.Generic;
using PicoElderCare.Rehab;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DefaultExecutionOrder(-190)]
public class MrBackgroundVisualSuppressor : MonoBehaviour
{
    public bool hideRenderers = true;
    public bool hideCanvasImages = true;
    public bool hideAllEnvironmentRenderers = true;
    public bool hideAllRoomSensingRenderers = true;
    public float scanIntervalSeconds = 0.15f;
    public float startupFullScanDurationSeconds = 6f;
    public float steadyStateScanIntervalSeconds = 1f;
    public float minimumLargeSurfaceMeters = 1.0f;
    public Transform[] protectedRoots;
    public Transform[] scanRoots;
    public bool scanWholeSceneWhenNoRoots = true;
    public bool logHiddenRenderers = false;

    private static readonly string[] ForcedRoomSensingKeywords =
    {
        "mrspacesensing",
        "mrdetectedplanetemplate",
        "mrspatialmeshtemplate",
        "pxr_plane",
        "pxrplane",
        "pxr_spatialmesh",
        "pxrspatialmesh",
        "spatial mesh",
        "spatialmesh"
    };

    private static readonly string[] BackgroundKeywords =
    {
        "background",
        "white",
        "whitewall",
        "back wall",
        "backwall",
        "wall",
        "backdrop",
        "debugwall",
        "environment",
        "boundary",
        "plane",
        "quad"
    };

    private static readonly string[] RendererProtectedKeywords =
    {
        "table",
        "net",
        "ball",
        "paddle",
        "racket",
        "controller",
        "hand",
        "text",
        "button",
        "prompt",
        "score",
        "home",
        "training",
        "circle",
        "draghandle",
        "light"
    };

    private static readonly string[] UiProtectedKeywords =
    {
        "rehabui",
        "trainingui",
        "instruction",
        "timer",
        "score",
        "button",
        "text",
        "tmp",
        "prompt",
        "home",
        "title",
        "status",
        "debug",
        "movement",
        "hit",
        "served",
        "missed",
        "accuracy",
        "speed",
        "spin"
    };

    private readonly List<Renderer> _knownRenderers = new List<Renderer>();
    private readonly List<Graphic> _knownGraphics = new List<Graphic>();
    private readonly HashSet<Renderer> _loggedHiddenRenderers = new HashSet<Renderer>();
    private float _nextScanTime;
    private float _enabledTime;
    private bool _keepVisibleTagAvailable = true;

    private void OnEnable()
    {
        _enabledTime = Time.realtimeSinceStartup;
        _nextScanTime = 0f;
        HideBackgroundVisuals();
    }

    private void Update()
    {
        if (Time.time < _nextScanTime) return;

        _nextScanTime = Time.time + GetCurrentScanInterval();
        HideBackgroundVisuals();
    }

    public void HideBackgroundVisuals()
    {
        RefreshCandidateCache();
        HideCachedVisuals();
    }

    private void RefreshCandidateCache()
    {
        if (hideRenderers)
        {
            foreach (var renderer in EnumerateRenderers())
            {
                AddRendererCandidate(renderer);
            }
        }

        if (!hideCanvasImages) return;

        foreach (var graphic in EnumerateGraphics())
        {
            AddGraphicCandidate(graphic);
        }
    }

    private void HideCachedVisuals()
    {
        for (var i = _knownRenderers.Count - 1; i >= 0; i--)
        {
            var renderer = _knownRenderers[i];
            if (renderer == null)
            {
                _knownRenderers.RemoveAt(i);
                continue;
            }

            if (!renderer.enabled || !TryGetRendererHideReason(renderer, out var reason))
            {
                continue;
            }

            // Protection can be added by another component after this renderer entered the cache.
            // Re-check immediately before mutating renderer state.
            if (IsRendererProtected(renderer.transform)) continue;

            renderer.enabled = false;
            if (logHiddenRenderers && _loggedHiddenRenderers.Add(renderer))
            {
                Debug.Log(
                    $"[MR Suppressor] Disabled renderer: path={GetLowerPath(renderer.transform)}, reason={reason}",
                    renderer);
            }
        }

        for (var i = _knownGraphics.Count - 1; i >= 0; i--)
        {
            var graphic = _knownGraphics[i];
            if (graphic == null)
            {
                _knownGraphics.RemoveAt(i);
                continue;
            }

            if (graphic.enabled && ShouldHideGraphic(graphic))
            {
                graphic.enabled = false;
            }
        }
    }

    private IEnumerable<Renderer> EnumerateRenderers()
    {
        if (scanRoots != null && scanRoots.Length > 0)
        {
            for (var i = 0; i < scanRoots.Length; i++)
            {
                var root = scanRoots[i];
                if (root == null) continue;

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    yield return renderer;
                }
            }

            yield break;
        }

        if (!scanWholeSceneWhenNoRoots) yield break;

        foreach (var renderer in FindObjectsOfType<Renderer>(true))
        {
            yield return renderer;
        }
    }

    private IEnumerable<Graphic> EnumerateGraphics()
    {
        if (scanRoots != null && scanRoots.Length > 0)
        {
            for (var i = 0; i < scanRoots.Length; i++)
            {
                var root = scanRoots[i];
                if (root == null) continue;

                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    yield return graphic;
                }
            }

            yield break;
        }

        if (!scanWholeSceneWhenNoRoots) yield break;

        foreach (var graphic in FindObjectsOfType<Graphic>(true))
        {
            yield return graphic;
        }
    }

    private void AddRendererCandidate(Renderer renderer)
    {
        if (renderer == null || _knownRenderers.Contains(renderer)) return;
        _knownRenderers.Add(renderer);
    }

    private void AddGraphicCandidate(Graphic graphic)
    {
        if (graphic == null || _knownGraphics.Contains(graphic)) return;
        _knownGraphics.Add(graphic);
    }

    private float GetCurrentScanInterval()
    {
        var startupActive = Time.realtimeSinceStartup - _enabledTime <= Mathf.Max(0f, startupFullScanDurationSeconds);
        return startupActive
            ? Mathf.Max(0.05f, scanIntervalSeconds)
            : Mathf.Max(Mathf.Max(0.25f, scanIntervalSeconds), steadyStateScanIntervalSeconds);
    }

    public void AddProtectedRoot(Transform root)
    {
        if (root == null) return;

        if (protectedRoots != null)
        {
            for (var i = 0; i < protectedRoots.Length; i++)
            {
                if (protectedRoots[i] == root) return;
            }
        }

        var previousLength = protectedRoots != null ? protectedRoots.Length : 0;
        var updatedRoots = new Transform[previousLength + 1];
        if (previousLength > 0)
        {
            protectedRoots.CopyTo(updatedRoots, 0);
        }

        updatedRoots[previousLength] = root;
        protectedRoots = updatedRoots;
    }

    private bool TryGetRendererHideReason(Renderer renderer, out string reason)
    {
        reason = null;
        if (renderer == null) return false;

        var target = renderer.transform;
        if (target == null || IsRendererProtected(target)) return false;

        var path = GetLowerPath(target);
        if (hideAllRoomSensingRenderers && ContainsAny(path, ForcedRoomSensingKeywords))
        {
            reason = "room sensing";
            return true;
        }

        if (hideAllEnvironmentRenderers && path.Contains("environment"))
        {
            reason = "environment root";
            return true;
        }

        if (!ContainsAny(path, BackgroundKeywords)) return false;
        if (!IsLargeRenderer(renderer)) return false;

        if (path.Contains("plane") || path.Contains("quad") || path.Contains("wall") || path.Contains("backdrop") || path.Contains("boundary"))
        {
            reason = "background keyword";
            return true;
        }

        if (IsWhiteOrDefaultRenderer(renderer))
        {
            reason = "large white renderer";
            return true;
        }

        return false;
    }

    private bool ShouldHideGraphic(Graphic graphic)
    {
        var target = graphic.transform;
        if (target == null || IsUiProtected(target)) return false;

        var path = GetLowerPath(target);
        var objectName = target.name.ToLowerInvariant();
        var color = graphic.color;
        if (!IsHighAlphaWhite(color)) return false;
        if (!IsUiBackgroundCandidate(graphic, objectName)) return false;

        Debug.Log("MR background suppressor hid UI graphic: " + path);
        return true;
    }

    private bool IsRendererProtected(Transform target)
    {
        if (target == null) return true;
        if (target.GetComponentInParent<MrKeepVisible>(true) != null) return true;
        if (target.GetComponentInParent<RehabVideoGuideController>(true) != null) return true;
        if (target.GetComponentInParent<RehabVideoPanelLayoutController>(true) != null) return true;
        if (target.GetComponentInParent<VideoPlayer>(true) != null) return true;

        if (protectedRoots != null)
        {
            for (var i = 0; i < protectedRoots.Length; i++)
            {
                var protectedRoot = protectedRoots[i];
                if (protectedRoot != null && (target == protectedRoot || target.IsChildOf(protectedRoot)))
                {
                    return true;
                }
            }
        }

        return IsProtectedByExistingRules(target);
    }

    private bool IsUiProtected(Transform target)
    {
        if (target == null) return true;
        if (IsExplicitlyKeptVisible(target)) return true;
        if (target.GetComponentInParent<RehabVideoGuideController>(true) != null) return true;
        if (target.GetComponentInParent<RehabVideoPanelLayoutController>(true) != null) return true;
        if (target.GetComponentInParent<VideoPlayer>(true) != null) return true;

        if (protectedRoots != null)
        {
            for (var i = 0; i < protectedRoots.Length; i++)
            {
                var protectedRoot = protectedRoots[i];
                if (protectedRoot != null && (target == protectedRoot || target.IsChildOf(protectedRoot)))
                {
                    return true;
                }
            }
        }

        var objectName = target.name.ToLowerInvariant();
        return ContainsAny(objectName, UiProtectedKeywords);
    }

    private bool IsProtectedByExistingRules(Transform target)
    {
        if (target == null) return true;

        if (_keepVisibleTagAvailable)
        {
            try
            {
                if (target.CompareTag("MrKeepVisible")) return true;
            }
            catch (UnityException)
            {
                // The optional tag may not exist in older project settings.
                _keepVisibleTagAvailable = false;
            }
        }

        var path = GetLowerPath(target);
        return ContainsAny(path, RendererProtectedKeywords) && !ContainsAny(path, ForcedRoomSensingKeywords);
    }

    private bool IsExplicitlyKeptVisible(Transform target)
    {
        if (target == null) return false;
        if (target.GetComponentInParent<MrKeepVisible>(true) != null) return true;

        if (!_keepVisibleTagAvailable) return false;

        try
        {
            if (target.CompareTag("MrKeepVisible")) return true;
        }
        catch (UnityException)
        {
            // The optional tag may not exist in older project settings.
            _keepVisibleTagAvailable = false;
        }

        return false;
    }

    private bool IsLargeRenderer(Renderer renderer)
    {
        var size = renderer.bounds.size;
        var largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        return largest >= Mathf.Max(0.1f, minimumLargeSurfaceMeters);
    }

    private bool IsLargeUiGraphic(Graphic graphic)
    {
        var rect = graphic.rectTransform;
        if (rect == null) return false;

        var size = rect.rect.size;
        return Mathf.Max(size.x, size.y) >= 450f;
    }

    private bool IsUiBackgroundCandidate(Graphic graphic, string objectName)
    {
        if (graphic is Image || graphic is RawImage)
        {
            return IsLargeUiGraphic(graphic) || ContainsAny(objectName, BackgroundKeywords) || objectName.Contains("panel");
        }

        return ContainsAny(objectName, BackgroundKeywords) || objectName.Contains("panel");
    }

    private static bool IsHighAlphaWhite(Color color)
    {
        return color.a > 0.2f && color.r > 0.85f && color.g > 0.85f && color.b > 0.85f;
    }

    private bool IsWhiteOrDefaultRenderer(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0) return true;

        foreach (var material in materials)
        {
            if (material == null) continue;
            if (TryGetMaterialColor(material, out var color))
            {
                if (color.a > 0.2f && color.r > 0.85f && color.g > 0.85f && color.b > 0.85f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetMaterialColor(Material material, out Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
            return true;
        }

        if (material.HasProperty("_Color"))
        {
            color = material.GetColor("_Color");
            return true;
        }

        color = Color.white;
        return false;
    }

    private static bool ContainsAny(string value, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (value.Contains(keyword)) return true;
        }

        return false;
    }

    private static string GetLowerPath(Transform target)
    {
        var path = target.name;
        var current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path.ToLowerInvariant();
    }
}
