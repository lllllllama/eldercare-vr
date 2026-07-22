using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PicoElderCare.Rehab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class RehabVideoVisibilityValidator
{
    private const string ScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string ValidateMenuPath = "Tools/ElderCare/Rehab/Validate Video Visibility";
    private const string RepairMenuPath = "Tools/ElderCare/Rehab/Repair Video Visibility Protection";

    [MenuItem(ValidateMenuPath)]
    public static void ValidateVideoVisibility()
    {
        var scene = OpenTargetScene();
        if (!scene.IsValid() || !scene.isLoaded) return;

        ValidateScene(scene, true);
    }

    [MenuItem(RepairMenuPath)]
    public static void RepairVideoVisibilityProtection()
    {
        var scene = OpenTargetScene();
        if (!scene.IsValid() || !scene.isLoaded) return;

        var guides = FindSceneComponents<RehabVideoGuideController>(scene);
        var suppressors = FindSceneComponents<MrBackgroundVisualSuppressor>(scene);
        if (guides.Count != 1)
        {
            Debug.LogError("[Rehab Video Visibility] Repair requires exactly one RehabVideoGuideController; found " + guides.Count + ".");
            return;
        }

        var guide = guides[0];
        if (guide.videoPanel == null || guide.videoQuadRenderer == null)
        {
            Debug.LogError("[Rehab Video Visibility] Repair stopped because videoPanel or videoQuadRenderer is missing.", guide);
            return;
        }

        if (suppressors.Count == 0)
        {
            Debug.LogError("[Rehab Video Visibility] Repair stopped because no MrBackgroundVisualSuppressor exists in the scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair Rehab Video Visibility Protection");
        var changed = false;

        try
        {
            if (guide.videoPanel.GetComponent<MrKeepVisible>() == null)
            {
                Undo.AddComponent<MrKeepVisible>(guide.videoPanel);
                changed = true;
            }

            for (var i = 0; i < suppressors.Count; i++)
            {
                var suppressor = suppressors[i];
                if (ContainsRoot(suppressor.protectedRoots, guide.videoPanel.transform)) continue;

                Undo.RecordObject(suppressor, "Protect RehabVideoPanel from MR suppression");
                suppressor.AddProtectedRoot(guide.videoPanel.transform);
                EditorUtility.SetDirty(suppressor);
                changed = true;
            }

            if (!guide.videoQuadRenderer.enabled)
            {
                Undo.RecordObject(guide.videoQuadRenderer, "Enable rehab VideoQuad renderer");
                guide.videoQuadRenderer.enabled = true;
                EditorUtility.SetDirty(guide.videoQuadRenderer);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(guide.videoPanel);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Unity could not save " + ScenePath + ".");
                }
            }
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        Debug.Log(changed
            ? "[Rehab Video Visibility] Protection repair completed and the scene was saved."
            : "[Rehab Video Visibility] Protection was already complete; no scene changes were needed.");
        ValidateScene(scene, true);
    }

    public static bool ValidateScene(Scene scene, bool logResult)
    {
        var issues = new List<string>();
        var guides = FindSceneComponents<RehabVideoGuideController>(scene);
        var videoPlayers = FindSceneComponents<VideoPlayer>(scene);
        var suppressors = FindSceneComponents<MrBackgroundVisualSuppressor>(scene);

        if (guides.Count != 1)
        {
            issues.Add("Expected exactly one RehabVideoGuideController; found " + guides.Count + ".");
        }

        if (videoPlayers.Count != 1)
        {
            issues.Add("Expected exactly one VideoPlayer; found " + videoPlayers.Count + ".");
        }

        var guide = guides.Count == 1 ? guides[0] : null;
        if (guide != null)
        {
            ValidateGuide(guide, videoPlayers, suppressors, issues);
        }

        var namedVideoQuads = FindSceneComponents<Transform>(scene)
            .Where(transform => string.Equals(transform.name, "VideoQuad", StringComparison.Ordinal))
            .ToList();
        if (namedVideoQuads.Count != 1)
        {
            issues.Add("Expected exactly one playback object named VideoQuad; found " + namedVideoQuads.Count + ".");
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target.gameObject);
                if (missingCount > 0)
                {
                    issues.Add(GetHierarchyPath(target) + " has " + missingCount + " missing script(s).");
                }
            }
        }

        if (issues.Count == 0)
        {
            if (logResult)
            {
                Debug.Log("<color=#49D17D>[Rehab Video Visibility] Validation passed.</color>");
            }
            return true;
        }

        if (logResult)
        {
            Debug.LogError("[Rehab Video Visibility] Validation found " + issues.Count + " issue(s):\n- " + string.Join("\n- ", issues));
        }
        return false;
    }

    private static void ValidateGuide(
        RehabVideoGuideController guide,
        List<VideoPlayer> videoPlayers,
        List<MrBackgroundVisualSuppressor> suppressors,
        List<string> issues)
    {
        if (guide.videoPanel == null) issues.Add("RehabVideoGuideController.videoPanel is null.");
        if (guide.videoQuad == null) issues.Add("RehabVideoGuideController.videoQuad is null.");
        if (guide.videoQuadRenderer == null) issues.Add("RehabVideoGuideController.videoQuadRenderer is null.");
        if (guide.videoPlayer == null) issues.Add("RehabVideoGuideController.videoPlayer is null.");

        if (guide.videoPlayer != null && (videoPlayers.Count != 1 || videoPlayers[0] != guide.videoPlayer))
        {
            issues.Add("The unique scene VideoPlayer does not match RehabVideoGuideController.videoPlayer.");
        }

        if (guide.videoPanel != null && guide.videoQuad != null &&
            !guide.videoQuad.transform.IsChildOf(guide.videoPanel.transform))
        {
            issues.Add("VideoQuad is not under RehabVideoPanel.");
        }

        if (guide.videoQuad != null && guide.displayRoot != null &&
            (guide.videoQuad.transform == guide.displayRoot.transform ||
             guide.videoQuad.transform.IsChildOf(guide.displayRoot.transform)))
        {
            issues.Add("VideoQuad must not be displayRoot/VideoCanvas or one of its children.");
        }

        if (guide.videoPanel != null && guide.displayRoot != null &&
            guide.displayRoot.transform.parent != guide.videoPanel.transform)
        {
            issues.Add("displayRoot/VideoCanvas should be a direct child of RehabVideoPanel and a sibling of VideoQuad.");
        }

        if (guide.videoQuadRenderer != null && !guide.videoQuadRenderer.enabled)
        {
            issues.Add("VideoQuad MeshRenderer is disabled.");
        }

        if (guide.videoPanel != null && guide.videoPanel.GetComponent<MrKeepVisible>() == null)
        {
            issues.Add("RehabVideoPanel does not have MrKeepVisible.");
        }

        if (suppressors.Count == 0)
        {
            issues.Add("No MrBackgroundVisualSuppressor exists in the scene.");
        }
        else if (guide.videoPanel != null)
        {
            for (var i = 0; i < suppressors.Count; i++)
            {
                if (!ContainsRoot(suppressors[i].protectedRoots, guide.videoPanel.transform))
                {
                    issues.Add("Suppressor " + GetHierarchyPath(suppressors[i].transform) +
                               " does not include RehabVideoPanel in protectedRoots.");
                }
            }
        }

        if (guide.videoPlayer != null && guide.videoPlayer.targetTexture == null)
        {
            issues.Add("VideoPlayer.targetTexture is null.");
        }
        if (guide.renderTexture == null)
        {
            issues.Add("RehabVideoGuideController.renderTexture is null.");
        }
        if (guide.videoPlayer != null && guide.renderTexture != null &&
            guide.videoPlayer.targetTexture != guide.renderTexture)
        {
            issues.Add("VideoPlayer.targetTexture and guide.renderTexture are different assets.");
        }

        if (guide.renderTexture != null)
        {
            var material = guide.videoQuadRenderer != null
                ? guide.videoQuadRenderer.sharedMaterial
                : guide.videoMaterial;
            var mainTextureMatches = material != null && material.mainTexture == guide.renderTexture;
            var baseMapMatches = material != null && material.HasProperty("_BaseMap") &&
                                 material.GetTexture("_BaseMap") == guide.renderTexture;
            if (!mainTextureMatches && !baseMapMatches)
            {
                issues.Add("VideoQuad material mainTexture/_BaseMap is not bound to guide.renderTexture.");
            }
        }

        if (guide.displayMode != RehabVideoDisplayMode.QuadMaterial)
        {
            issues.Add("RehabVideoGuideController.displayMode is not QuadMaterial.");
        }
    }

    private static Scene OpenTargetScene()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[Rehab Video Visibility] Scene not found: " + ScenePath);
            return default;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded && activeScene.path == ScenePath)
        {
            return activeScene;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[Rehab Video Visibility] Operation cancelled before opening the rehab scene.");
            return default;
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static bool ContainsRoot(Transform[] roots, Transform expected)
    {
        if (roots == null || expected == null) return false;
        for (var i = 0; i < roots.Length; i++)
        {
            if (roots[i] == expected) return true;
        }
        return false;
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        var results = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return results;
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null) return "<null>";
        var path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }
}
