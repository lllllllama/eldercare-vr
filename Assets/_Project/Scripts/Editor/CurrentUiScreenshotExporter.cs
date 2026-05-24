using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CurrentUiScreenshotExporter
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const float Padding = 1.14f;
    private const string OutputDirectory = "output/screenshots/current-ui-20260524";

    private static readonly Vector3[] RectCorners = new Vector3[4];

    public static void ExportEntryAndRehab()
    {
        Directory.CreateDirectory(OutputDirectory);
        CaptureMainEntry();
        CaptureRehabPanel("rehab-select.png", "RehabTrainingSelectPanel");
        CaptureRehabPanel("rehab-training.png", "RehabTrainingPanel");
        CaptureRehabPanel("rehab-result.png", "TrainingResultPanel");
        AssetDatabase.Refresh();
        Debug.Log("UI_SCREENSHOTS_EXPORTED: " + Path.GetFullPath(OutputDirectory));
    }

    private static void CaptureMainEntry()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/00_MainEntry.unity");
        CaptureTargets("main-entry.png", "MainEntryCanvas");
    }

    private static void CaptureRehabPanel(string fileName, string activePanelName)
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MR_Rehab_Main.unity");
        SetActiveIfFound("TrainingArea", false);
        SetActiveIfFound("RehabTrainingSelectPanel", activePanelName == "RehabTrainingSelectPanel");
        SetActiveIfFound("RehabTrainingPanel", activePanelName == "RehabTrainingPanel");
        SetActiveIfFound("TrainingResultPanel", activePanelName == "TrainingResultPanel");
        SetActiveIfFound("MainMenuPanel", false);
        CaptureTargets(fileName, "RehabPromptCanvas");
    }

    private static void CaptureTargets(string fileName, params string[] rootNames)
    {
        Canvas.ForceUpdateCanvases();
        Bounds bounds = CalculateBounds(rootNames);
        Camera camera = CreatePreviewCamera(bounds);
        string outputPath = Path.Combine(OutputDirectory, fileName);
        RenderCamera(camera, outputPath);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        Debug.Log("UI_SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    private static Camera CreatePreviewCamera(Bounds bounds)
    {
        var cameraGo = new GameObject("CurrentUiScreenshotCamera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.073f, 0.095f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.size.y * 0.5f, bounds.size.x * Height / (2f * Width)) * Padding;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 20f;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 3f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static Bounds CalculateBounds(params string[] rootNames)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds();
        foreach (string rootName in rootNames)
        {
            GameObject root = FindObjectIncludingInactive(rootName);
            if (root == null) continue;
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(false))
            {
                if (!rect.gameObject.activeInHierarchy) continue;
                rect.GetWorldCorners(RectCorners);
                for (int i = 0; i < RectCorners.Length; i++)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(RectCorners[i], Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(RectCorners[i]);
                    }
                }
            }
        }

        if (!hasBounds) throw new InvalidOperationException("No active RectTransform bounds were found.");
        return bounds;
    }

    private static void RenderCamera(Camera camera, string outputPath)
    {
        var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static void SetActiveIfFound(string objectName, bool active)
    {
        GameObject target = FindObjectIncludingInactive(objectName);
        if (target != null) target.SetActive(active);
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name != objectName) continue;
            if (!candidate.scene.IsValid() || !candidate.scene.isLoaded) continue;
            if (EditorUtility.IsPersistent(candidate)) continue;
            return candidate;
        }

        return null;
    }
}
