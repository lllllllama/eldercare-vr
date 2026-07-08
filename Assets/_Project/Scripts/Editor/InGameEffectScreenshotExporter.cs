using System;
using System.IO;
using System.Reflection;
using System.Text;
using PicoElderCare.Rehab;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InGameEffectScreenshotExporter
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const string OutputDirectory = "outputs/ingame-ui-screenshots";
    private const string PingPongScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string MainEntryScenePath = "Assets/_Project/Scenes/00_MainEntry.unity";
    private const float UiPadding = 1.12f;

    private static readonly Vector3[] RectCorners = new Vector3[4];

    [MenuItem("Tools/PICO ElderCare/Export In-Game Effect Screenshots")]
    public static void ExportAll()
    {
        Directory.CreateDirectory(OutputDirectory);

        CaptureMainEntryPreview();
        CapturePingPongScenario("01_pingpong_overview_ready.png", "PingPong overview ready: full world-space panel placement", Scenario.ReadyOverview, true);
        CapturePingPongScenario("02_panel_ready_full.png", "Panel close-up: ready state with full controls", Scenario.ReadyFullPanel, false);
        CapturePingPongScenario("03_panel_training_compact.png", "Panel close-up: compact training state", Scenario.TrainingCompact, false);
        CapturePingPongScenario("04_panel_score_progress.png", "Panel close-up: score progress state", Scenario.ScoreProgress, false);
        CapturePingPongScenario("05_panel_difficulty_challenge.png", "Panel close-up: challenge difficulty state", Scenario.ChallengeDifficulty, false);
        CapturePingPongScenario("06_panel_table_drag_enabled.png", "Panel close-up: table drag enabled state", Scenario.TableDragEnabled, false);
        CapturePingPongScenario("07_panel_paused_after_training.png", "Panel close-up: paused after training state", Scenario.PausedAfterTraining, false);
        CapturePingPongScenario("08_pingpong_overview_active.png", "PingPong overview active: table and compact panel", Scenario.ActiveOverview, true);

        WriteIndex();
        AssetDatabase.Refresh();
        Debug.Log("INGAME_EFFECT_SCREENSHOTS_EXPORTED: " + Path.GetFullPath(OutputDirectory));
    }

    private static void CaptureMainEntryPreview()
    {
        EditorSceneManager.OpenScene(MainEntryScenePath);
        var canvas = FindSceneObject("MainEntryCanvas");
        if (canvas == null)
        {
            throw new InvalidOperationException("MainEntryCanvas was not found in the main entry scene.");
        }

        var menu = UnityEngine.Object.FindObjectOfType<UnifiedEntryMenu>(true);
        HtmlStyleMainEntryPanel.Ensure(canvas.transform, menu, null);
        Canvas.ForceUpdateCanvases();
        CaptureUiBounds("00_main_entry_panel.png", "Main entry panel: migrated four-module UI", new[] { "MainEntryCanvas" });
    }

    private static void CapturePingPongScenario(string fileName, string description, Scenario scenario, bool overview)
    {
        var context = overview ? ResolveScenePingPongContext() : CreateIsolatedPingPongContext();
        ApplyScenario(context, scenario);
        Canvas.ForceUpdateCanvases();

        if (overview)
        {
            CaptureOverview(fileName, description, context, scenario);
        }
        else
        {
            CaptureUiBounds(fileName, description, new[] { "PingPongUnifiedControlPanel" });
        }
    }

    private static PingPongContext ResolveScenePingPongContext()
    {
        EditorSceneManager.OpenScene(PingPongScenePath);
        var canvas = FindSceneObject("WorldSpaceCanvas") ?? FindSceneObject("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("WorldSpaceCanvas", typeof(RectTransform), typeof(Canvas));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvas.transform.position = new Vector3(0f, 1.45f, 2.1f);
            canvas.transform.localScale = Vector3.one * 0.001f;
        }

        var score = UnityEngine.Object.FindObjectOfType<ScoreManager>(true);
        if (score == null)
        {
            score = new GameObject("ScreenshotScoreManager").AddComponent<ScoreManager>();
        }

        var spawner = UnityEngine.Object.FindObjectOfType<BallSpawner>(true);
        if (spawner == null)
        {
            spawner = new GameObject("ScreenshotBallSpawner").AddComponent<BallSpawner>();
        }

        var difficulty = UnityEngine.Object.FindObjectOfType<PingPongDifficultyController>(true);
        if (difficulty == null)
        {
            difficulty = new GameObject("ScreenshotDifficulty").AddComponent<PingPongDifficultyController>();
        }

        difficulty.ballSpawner = spawner;
        difficulty.rememberDifficulty = false;
        difficulty.displayStandalonePanel = false;
        difficulty.ApplyLoadedDifficulty();

        var tableDrag = UnityEngine.Object.FindObjectOfType<RemoteTableDragController>(true);
        if (tableDrag == null)
        {
            tableDrag = new GameObject("ScreenshotRemoteTableDrag").AddComponent<RemoteTableDragController>();
        }

        var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvas.transform, score, spawner, difficulty, tableDrag, null, null);
        panel.gameObject.SetActive(true);
        panel.RefreshDisplay();

        return new PingPongContext
        {
            Canvas = canvas,
            Score = score,
            Spawner = spawner,
            Difficulty = difficulty,
            TableDrag = tableDrag,
            Panel = panel,
            TableRoot = ResolveTableRoot()
        };
    }

    private static PingPongContext CreateIsolatedPingPongContext()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvas = new GameObject("ScreenshotPanelCanvas", typeof(RectTransform), typeof(Canvas));
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000f, 900f);
        canvas.transform.localScale = Vector3.one * 0.001f;
        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;

        var score = new GameObject("ScreenshotScoreManager").AddComponent<ScoreManager>();
        var spawner = new GameObject("ScreenshotBallSpawner").AddComponent<BallSpawner>();
        var difficulty = new GameObject("ScreenshotDifficulty").AddComponent<PingPongDifficultyController>();
        difficulty.ballSpawner = spawner;
        difficulty.rememberDifficulty = false;
        difficulty.displayStandalonePanel = false;
        difficulty.ApplyLoadedDifficulty();

        var tableRoot = new GameObject("ScreenshotTableRoot").transform;
        var controller = new GameObject("ScreenshotController").transform;
        controller.position = new Vector3(0f, 0f, -0.4f);
        controller.rotation = Quaternion.identity;

        var tableDrag = new GameObject("ScreenshotRemoteTableDrag").AddComponent<RemoteTableDragController>();
        tableDrag.tableRoot = tableRoot;
        tableDrag.controllerTransform = controller;
        tableDrag.controlServing = false;
        tableDrag.clearBallsWhenDragging = false;

        var panel = PingPongUnifiedControlPanel.EnsureRuntimePanel(canvas.transform, score, spawner, difficulty, tableDrag, null, null);
        panel.gameObject.SetActive(true);
        panel.RefreshDisplay();

        return new PingPongContext
        {
            Canvas = canvas,
            Score = score,
            Spawner = spawner,
            Difficulty = difficulty,
            TableDrag = tableDrag,
            Panel = panel,
            TableRoot = tableRoot
        };
    }

    private static void ApplyScenario(PingPongContext context, Scenario scenario)
    {
        context.Spawner.StopServing();
        context.TableDrag.SetRemoteDragEnabled(false);
        SetScore(context.Score, 0, 0, 0, 0f, 0f);

        context.Difficulty.SetDifficulty(PingPongDifficulty.Normal);
        context.Panel.RefreshDisplay();

        switch (scenario)
        {
            case Scenario.TrainingCompact:
                SetScore(context.Score, 3, 2, 1, 2.8f, 42f);
                context.Panel.StartServingAndCompact();
                break;
            case Scenario.ScoreProgress:
                SetScore(context.Score, 18, 14, 4, 4.6f, 58f);
                context.Panel.RefreshDisplay();
                break;
            case Scenario.ChallengeDifficulty:
                SetScore(context.Score, 28, 22, 6, 5.2f, 63f);
                context.Difficulty.SetDifficulty(PingPongDifficulty.Challenge);
                context.Panel.RefreshDisplay();
                break;
            case Scenario.TableDragEnabled:
                SetScore(context.Score, 12, 9, 3, 3.9f, 51f);
                context.TableDrag.SetRemoteDragEnabled(true);
                context.Panel.RefreshDisplay();
                break;
            case Scenario.PausedAfterTraining:
                SetScore(context.Score, 36, 29, 7, 4.8f, 57f);
                context.Panel.StartServingAndCompact();
                context.Panel.ToggleServing();
                break;
            case Scenario.ActiveOverview:
                SetScore(context.Score, 8, 6, 2, 4.1f, 48f);
                context.Panel.StartServingAndCompact();
                CreatePreviewBall(context.TableRoot);
                break;
            case Scenario.ReadyOverview:
            case Scenario.ReadyFullPanel:
            default:
                context.Panel.RefreshDisplay();
                break;
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void CaptureOverview(string fileName, string description, PingPongContext context, Scenario scenario)
    {
        var cameraObject = new GameObject("InGameOverviewScreenshotCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.055f, 0.065f, 1f);
        camera.fieldOfView = 58f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 35f;
        camera.allowHDR = false;
        camera.allowMSAA = true;

        var target = context.TableRoot != null ? context.TableRoot.position + new Vector3(0f, 0.55f, 0.1f) : new Vector3(0f, 1.15f, 1.6f);
        if (scenario == Scenario.ReadyOverview)
        {
            target = Vector3.Lerp(target, context.Panel.transform.position, 0.35f);
        }

        camera.transform.position = new Vector3(0f, 1.62f, -1.45f);
        camera.transform.LookAt(target);
        RenderCamera(camera, Path.Combine(OutputDirectory, fileName));
        UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("INGAME_EFFECT_SCREENSHOT: " + fileName + " | " + description);
    }

    private static void CaptureUiBounds(string fileName, string description, string[] rootNames)
    {
        var bounds = CalculateBounds(rootNames);
        var cameraObject = new GameObject("InGameUiScreenshotCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.042f, 0.052f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.size.y * 0.5f, bounds.size.x * Height / (2f * Width)) * UiPadding;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 30f;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 3f);
        camera.transform.rotation = Quaternion.identity;

        RenderCamera(camera, Path.Combine(OutputDirectory, fileName));
        UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("INGAME_EFFECT_SCREENSHOT: " + fileName + " | " + description);
    }

    private static Bounds CalculateBounds(string[] rootNames)
    {
        var hasBounds = false;
        var bounds = new Bounds();
        for (var i = 0; i < rootNames.Length; i++)
        {
            var root = FindSceneObject(rootNames[i]);
            if (root == null) continue;
            var rects = root.GetComponentsInChildren<RectTransform>(false);
            for (var j = 0; j < rects.Length; j++)
            {
                var rect = rects[j];
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (!IsVisibleRect(rect)) continue;

                rect.GetWorldCorners(RectCorners);
                for (var k = 0; k < RectCorners.Length; k++)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(RectCorners[k], Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(RectCorners[k]);
                    }
                }
            }
        }

        if (!hasBounds) throw new InvalidOperationException("No active visible UI bounds were found.");
        return bounds;
    }

    private static bool IsVisibleRect(RectTransform rect)
    {
        if (!HasVisibleCanvasGroups(rect.transform)) return false;

        var graphics = rect.GetComponents<Graphic>();
        for (var i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null && graphics[i].enabled && graphics[i].color.a > 0.001f)
            {
                return true;
            }
        }

        var text = rect.GetComponent<TMP_Text>();
        return text != null && text.enabled && text.color.a > 0.001f && !string.IsNullOrEmpty(text.text);
    }

    private static bool HasVisibleCanvasGroups(Transform transform)
    {
        while (transform != null)
        {
            var group = transform.GetComponent<CanvasGroup>();
            if (group != null && group.alpha <= 0.001f)
            {
                return false;
            }

            transform = transform.parent;
        }

        return true;
    }

    private static void RenderCamera(Camera camera, string outputPath)
    {
        var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        var previousActive = RenderTexture.active;
        var previousTarget = camera.targetTexture;
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

    private static void SetScore(ScoreManager score, int served, int hit, int missed, float speed, float spin)
    {
        SetPrivateField(score, "_servedCount", served);
        SetPrivateField(score, "_hitCount", hit);
        SetPrivateField(score, "_missedCount", missed);
        SetPrivateField(score, "_lastHitSpeed", speed);
        SetPrivateField(score, "_lastSpinSpeed", spin);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(target, value);
    }

    private static Transform ResolveTableRoot()
    {
        var table = FindSceneObject("Table");
        if (table != null) return table.transform;

        var surface = UnityEngine.Object.FindObjectOfType<PingPongSurface>(true);
        return surface != null ? surface.transform.root : null;
    }

    private static void CreatePreviewBall(Transform tableRoot)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "ScreenshotPreviewBall";
        ball.transform.localScale = Vector3.one * 0.06f;
        ball.transform.position = tableRoot != null ? tableRoot.position + new Vector3(0.18f, 0.9f, -0.2f) : new Vector3(0.18f, 1.15f, 1.2f);

        var renderer = ball.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.72f, 0.22f, 1f);
            renderer.sharedMaterial = material;
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        var candidates = Resources.FindObjectsOfTypeAll<GameObject>();
        for (var i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || candidate.name != objectName) continue;
            if (!candidate.scene.IsValid() || !candidate.scene.isLoaded) continue;
            if (EditorUtility.IsPersistent(candidate)) continue;
            return candidate;
        }

        return null;
    }

    private static void WriteIndex()
    {
        WriteUtf8Index(Path.Combine(OutputDirectory, "index.html"));
    }

    private static void WriteUtf8Index(string indexPath)
    {
        var html =
@"<!doctype html>
<html lang=""zh-CN"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>PICO &#23616;&#20869; UI &#25928;&#26524;&#22270;</title>
  <style>
    body { margin: 0; background: #0b0f14; color: #edf6f7; font-family: ""Microsoft YaHei"", Arial, sans-serif; }
    main { max-width: 1180px; margin: 0 auto; padding: 32px 24px 56px; }
    h1 { margin: 0 0 8px; font-size: 28px; }
    p { color: #aebcc3; line-height: 1.7; }
    figure { margin: 24px 0 34px; padding: 0; }
    img { width: 100%; display: block; border: 1px solid rgba(255,255,255,.14); background: #111820; }
    figcaption { margin-top: 10px; color: #d7e3e6; font-size: 15px; }
  </style>
</head>
<body>
<main>
  <h1>PICO &#23616;&#20869; UI &#25928;&#26524;&#22270;</h1>
  <p>&#36825;&#20123;&#22270;&#29255;&#30001; Unity batchmode &#28210;&#26579;&#65292;&#35206;&#30422;&#20027;&#20837;&#21475;&#12289;&#20050;&#20051;&#29699;&#23616;&#20869;&#24453;&#24320;&#22987;&#12289;&#35757;&#32451;&#20013;&#12289;&#25104;&#32489;&#36827;&#23637;&#12289;&#38590;&#24230;&#20999;&#25442;&#12289;&#25302;&#26700;&#24320;&#21551;&#12289;&#26242;&#20572;&#31561;&#29366;&#24577;&#12290;&#30495;&#26426;&#20013;&#30340;&#36879;&#35270;&#32972;&#26223;&#12289;&#22836;&#26174;&#35270;&#22330;&#21644;&#25163;&#26564;&#23556;&#32447;&#20173;&#38656;&#22312; PICO &#19978;&#26368;&#32456;&#30830;&#35748;&#12290;</p>
  <figure><img src=""00_main_entry_panel.png""><figcaption>&#20027;&#20837;&#21475;&#38754;&#26495;&#65306;&#36801;&#31227;&#21518;&#30340;&#22235;&#27169;&#22359;&#20837;&#21475;&#25928;&#26524;</figcaption></figure>
  <figure><img src=""01_pingpong_overview_ready.png""><figcaption>&#23616;&#20869;&#24635;&#35272;&#65306;&#24453;&#24320;&#22987;&#65292;&#38754;&#26495;&#20301;&#20110;&#27491;&#21069;&#20559;&#24038;&#21487;&#35835;&#21306;</figcaption></figure>
  <figure><img src=""02_panel_ready_full.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#24453;&#24320;&#22987;&#65292;&#23436;&#25972;&#35757;&#32451;&#25511;&#21046;&#38754;&#26495;</figcaption></figure>
  <figure><img src=""03_panel_training_compact.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#35757;&#32451;&#20013;&#65292;&#33258;&#21160;&#25910;&#36215;&#20026;&#23567;&#22411;&#29366;&#24577;&#26465;</figcaption></figure>
  <figure><img src=""04_panel_score_progress.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#24050;&#26377;&#35757;&#32451;&#25968;&#25454;&#65292;&#21629;&#20013;&#29575;&#12289;&#36895;&#24230;&#12289;&#26059;&#36716;&#21516;&#27493;&#26174;&#31034;</figcaption></figure>
  <figure><img src=""05_panel_difficulty_challenge.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#25361;&#25112;&#38590;&#24230;&#65292;&#36895;&#24230;&#19982;&#38590;&#24230;&#26465;&#26356;&#26032;</figcaption></figure>
  <figure><img src=""06_panel_table_drag_enabled.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#25302;&#26700;&#27169;&#24335;&#24320;&#21551;&#65292;&#25353;&#38062;&#29366;&#24577;&#20999;&#25442;</figcaption></figure>
  <figure><img src=""07_panel_paused_after_training.png""><figcaption>&#38754;&#26495;&#36817;&#26223;&#65306;&#35757;&#32451;&#26242;&#20572;&#65292;&#20445;&#30041;&#38454;&#27573;&#25968;&#25454;&#24182;&#26174;&#31034;&#32487;&#32493;&#35757;&#32451;</figcaption></figure>
  <figure><img src=""08_pingpong_overview_active.png""><figcaption>&#23616;&#20869;&#24635;&#35272;&#65306;&#35757;&#32451;&#20013;&#65292;&#29699;&#26700;&#12289;&#29699;&#12289;&#23567;&#22411;&#38754;&#26495;&#25972;&#20307;&#20851;&#31995;</figcaption></figure>
</main>
</body>
</html>";
        File.WriteAllText(indexPath, html, new UTF8Encoding(false));
    }

    private enum Scenario
    {
        ReadyOverview,
        ReadyFullPanel,
        TrainingCompact,
        ScoreProgress,
        ChallengeDifficulty,
        TableDragEnabled,
        PausedAfterTraining,
        ActiveOverview
    }

    private struct PingPongContext
    {
        public GameObject Canvas;
        public ScoreManager Score;
        public BallSpawner Spawner;
        public PingPongDifficultyController Difficulty;
        public RemoteTableDragController TableDrag;
        public PingPongUnifiedControlPanel Panel;
        public Transform TableRoot;
    }
}
