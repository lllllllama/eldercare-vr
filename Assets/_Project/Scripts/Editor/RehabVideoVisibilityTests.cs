#if UNITY_INCLUDE_TESTS
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PicoElderCare.Rehab;
using UnityEngine;
using UnityEngine.TestTools;

public class RehabVideoVisibilityTests
{
    [Test]
    public void RehabVideoQuadIsProtectedBySemanticComponent()
    {
        var root = new GameObject("TestRoot");
        root.SetActive(false);
        try
        {
            var panel = CreateChild(root.transform, "RehabVideoPanel");
            panel.AddComponent<RehabVideoGuideController>();
            var fallbackMarker = panel.GetComponent<MrKeepVisible>();
            if (fallbackMarker != null)
            {
                Object.DestroyImmediate(fallbackMarker);
            }
            var quad = CreateLargeQuad(panel.transform, "VideoQuad");
            var suppressor = ConfigureSuppressor(root, root.transform);

            root.SetActive(true);
            suppressor.HideBackgroundVisuals();

            Assert.IsTrue(quad.GetComponent<Renderer>().enabled,
                "A large renderer under RehabVideoGuideController must be protected even when its name contains quad.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OrdinaryLargeEnvironmentQuadIsStillHidden()
    {
        var root = new GameObject("TestRoot");
        root.SetActive(false);
        try
        {
            var quad = CreateLargeQuad(root.transform, "OrdinaryLargeQuad");
            var suppressor = ConfigureSuppressor(root, root.transform);

            root.SetActive(true);
            suppressor.HideBackgroundVisuals();

            Assert.IsFalse(quad.GetComponent<Renderer>().enabled,
                "An ordinary large environment quad should still be suppressed.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MrKeepVisibleProtectsRenderer()
    {
        var root = new GameObject("TestRoot");
        root.SetActive(false);
        try
        {
            var protectedRoot = CreateChild(root.transform, "ProtectedVideo");
            protectedRoot.AddComponent<MrKeepVisible>();
            var quad = CreateLargeQuad(protectedRoot.transform, "LargeQuad");
            var suppressor = ConfigureSuppressor(root, root.transform);

            root.SetActive(true);
            suppressor.HideBackgroundVisuals();

            Assert.IsTrue(quad.GetComponent<Renderer>().enabled);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ProtectedRootsProtectRenderer()
    {
        var root = new GameObject("TestRoot");
        root.SetActive(false);
        try
        {
            var protectedRoot = CreateChild(root.transform, "ProtectedVideo");
            var quad = CreateLargeQuad(protectedRoot.transform, "LargeQuad");
            var suppressor = ConfigureSuppressor(root, root.transform);
            suppressor.protectedRoots = new[] { protectedRoot.transform };

            root.SetActive(true);
            suppressor.HideBackgroundVisuals();

            Assert.IsTrue(quad.GetComponent<Renderer>().enabled);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void QuadMaterialVisibilityEnablesQuadAndRenderer()
    {
        var panel = new GameObject("RehabVideoPanel");
        try
        {
            var displayRoot = CreateChild(panel.transform, "VideoCanvas");
            var quad = CreateLargeQuad(panel.transform, "VideoQuad");
            var renderer = quad.GetComponent<Renderer>();
            var guide = panel.AddComponent<RehabVideoGuideController>();
            guide.videoPanel = panel;
            guide.displayRoot = displayRoot;
            guide.videoQuad = quad;
            guide.videoQuadRenderer = renderer;
            guide.displayMode = RehabVideoDisplayMode.QuadMaterial;

            quad.SetActive(false);
            renderer.enabled = false;
            InvokeApplyDisplayVisible(guide, true);

            Assert.IsTrue(quad.activeSelf);
            Assert.IsTrue(renderer.enabled);
            Assert.IsFalse(displayRoot.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void QuadUnderDisplayRootKeepsParentActiveAndReportsError()
    {
        var panel = new GameObject("RehabVideoPanel");
        try
        {
            var displayRoot = CreateChild(panel.transform, "VideoCanvas");
            var quad = CreateLargeQuad(displayRoot.transform, "VideoQuad");
            var renderer = quad.GetComponent<Renderer>();
            var guide = panel.AddComponent<RehabVideoGuideController>();
            guide.videoPanel = panel;
            guide.displayRoot = displayRoot;
            guide.videoQuad = quad;
            guide.videoQuadRenderer = renderer;
            guide.displayMode = RehabVideoDisplayMode.QuadMaterial;

            displayRoot.SetActive(false);
            LogAssert.Expect(
                LogType.Error,
                new Regex("VideoQuad must not be a child of displayRoot", RegexOptions.CultureInvariant));
            InvokeApplyDisplayVisible(guide, true);

            Assert.IsTrue(displayRoot.activeSelf);
            Assert.IsTrue(quad.activeSelf);
            Assert.IsTrue(quad.activeInHierarchy);
            Assert.IsTrue(renderer.enabled);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    private static MrBackgroundVisualSuppressor ConfigureSuppressor(GameObject owner, Transform scanRoot)
    {
        var suppressor = owner.AddComponent<MrBackgroundVisualSuppressor>();
        suppressor.hideRenderers = true;
        suppressor.hideCanvasImages = false;
        suppressor.hideAllEnvironmentRenderers = true;
        suppressor.hideAllRoomSensingRenderers = true;
        suppressor.minimumLargeSurfaceMeters = 1f;
        suppressor.scanRoots = new[] { scanRoot };
        suppressor.scanWholeSceneWhenNoRoots = false;
        return suppressor;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject CreateLargeQuad(Transform parent, string name)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localScale = new Vector3(2f, 2f, 1f);
        return quad;
    }

    private static void InvokeApplyDisplayVisible(RehabVideoGuideController guide, bool visible)
    {
        var method = typeof(RehabVideoGuideController).GetMethod(
            "ApplyDisplayVisible",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "ApplyDisplayVisible should remain available as the display visibility gate.");
        method.Invoke(guide, new object[] { visible });
    }
}
#endif
