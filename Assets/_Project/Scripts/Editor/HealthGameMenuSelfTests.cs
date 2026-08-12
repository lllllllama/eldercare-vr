using System;
using System.IO;
using PicoElderCare.HealthGame;
using PicoElderCare.Rehab;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class HealthGameMenuSelfTests
{
    private const string ScenePath = "Assets/_Project/Scenes/02_HealthGameMenu.unity";

    [MenuItem("Tools/PICO ElderCare/Run Health Game Menu Self Tests")]
    public static void RunAll()
    {
        // Validate the authored scene in place. Self-tests must never rebuild or overwrite it.
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateGeneratedMenu();
        Debug.Log("Health game menu self tests passed.");
    }

    private static void ValidateGeneratedMenu()
    {
        AssertTrue(File.Exists(ScenePath), "Health game menu scene should be generated.");

        var canvasObject = GameObject.Find("HealthGameMenuCanvas");
        AssertTrue(canvasObject != null, "HealthGameMenuCanvas should exist.");
        var canvasRect = canvasObject.GetComponent<RectTransform>();
        AssertVector2(canvasRect.sizeDelta, new Vector2(900f, 560f), "Health menu should use the three-card canvas size.");
        AssertApproximately(canvasObject.transform.localScale.x, 0.00165f, 0.00001f, "Health menu should preserve the established VR world-space footprint.");
        AssertTrue(canvasObject.GetComponent<GraphicRaycaster>() != null, "Health menu should keep the standard UI raycaster.");
        AssertTrue(canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>() != null, "Health menu should keep XR tracked-device raycasting.");

        var panel = RequireTransform(canvasObject.transform, "Panel");
        AssertNativeStroke(RequireTransform(panel, "VisualRoot/WoodFrame"), "WoodFrame");
        AssertNativeStroke(RequireTransform(panel, "VisualRoot/RicePaperPanel"), "RicePaperPanel");
        AssertNoStrokeOrOutline(RequireTransform(panel, "VisualRoot/RiceWarmEdge"), "RiceWarmEdge");
        var header = RequireTransform(panel, "Header");
        AssertText(header, "Title", "请选择健康运动类型");
        AssertText(header, "Subtitle", "选择喜欢的运动，活动身体、轻松锻炼");

        var cards = panel.Find("SportCards") ?? panel.Find("ChoiceCards");
        AssertTrue(cards != null, "Health menu should preserve its authored three-card container.");
        AssertSportCard(
            cards,
            "PingPongCard",
            "乒乓球",
            "灵活反应 · 活动身体",
            "约 8 分钟",
            "强度轻",
            "table_tennis",
            "LoadPingPong",
            true);
        AssertSportCard(
            cards,
            "ArcheryCard",
            "射箭",
            "稳定瞄准 · 锻炼协调",
            "约 10 分钟",
            "强度中",
            "bow_and_arrow",
            "LoadArchery",
            false);
        AssertSportCard(
            cards,
            "DartsCard",
            "飞镖",
            "专注投掷 · 轻松挑战",
            "约 8 分钟",
            "强度轻",
            "direct_hit",
            "LoadDarts",
            false);

        var dock = RequireTransform(panel, "BottomDock");
        AssertNativeStroke(dock, "BottomDock");
        var backButton = RequireTransform(dock, "BackButton").GetComponent<Button>();
        AssertTrue(backButton != null, "Bottom dock should contain a back Button.");
        AssertNativeStroke(backButton.transform, "BackButton");
        AssertPersistentListener(backButton, "ReturnToMainEntry");
        AssertText(backButton.transform, "Label", "返回上一页");
        AssertSpriteName(backButton.transform, "ArrowIcon", "arrow-left");

        var controller = UnityEngine.Object.FindObjectOfType<HealthGameMenuController>();
        AssertTrue(controller != null, "HealthGameMenuController should remain in the generated scene.");
        var placer = UnityEngine.Object.FindObjectOfType<ComfortWorldSpaceUIPlacer>();
        AssertTrue(placer != null, "Health menu should preserve ComfortWorldSpaceUIPlacer.");
        AssertApproximately(placer.distanceMeters, 2.2f, 0.001f, "Health menu world distance should remain 2.2 metres.");
        AssertTrue(placer.placeOnStart && placer.recenterDuringStartup, "Health menu should keep startup HMD placement.");
        AssertTrue(!placer.comfortFollowEnabled, "Health menu should freeze after the startup tracking window.");

        var eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        AssertTrue(eventSystem != null && eventSystem.GetComponent<XRUIInputModule>() != null, "Health menu should preserve XR UI input.");
        AssertTrue(panel.GetComponentsInChildren<Outline>(true).Length == 0, "Health warm-menu scope should not retain Unity Outline components.");
        AssertNoMissingScripts();
        AssertIconSourcesPresent();
    }

    private static void AssertSportCard(
        Transform cards,
        string cardName,
        string title,
        string subtitle,
        string duration,
        string intensity,
        string iconName,
        string methodName,
        bool recommended)
    {
        var card = RequireTransform(cards, cardName);
        var button = card.GetComponent<Button>();
        AssertTrue(button != null, cardName + " should be a large ray-friendly Button.");
        AssertTrue(button.targetGraphic != null && button.targetGraphic.raycastTarget, cardName + " should expose a raycast target.");
        AssertPersistentListener(button, methodName);
        AssertText(card, "Content/Title", title);
        AssertText(card, "Content/Subtitle", subtitle);
        AssertText(card, "Content/Metadata/DurationPill/Label", duration);
        AssertText(card, "Content/Metadata/IntensityPill/Label", intensity);
        AssertText(card, "Content/StartButtonVisual/Label", "开始运动");
        var sportIconPath = card.Find("Content/IconContainer/HeroIcon") != null
            ? "Content/IconContainer/HeroIcon"
            : "Content/IconContainer/SportIcon";
        var actionIconPath = card.Find("Content/StartButtonVisual/ActionIcon") != null
            ? "Content/StartButtonVisual/ActionIcon"
            : "Content/StartButtonVisual/PlayIcon";
        AssertSpriteName(card, sportIconPath, iconName);
        AssertSpriteName(card, "Content/Metadata/DurationPill/ClockIcon", "clock");
        AssertSpriteName(card, actionIconPath, "player-play");
        AssertNativeStroke(RequireTransform(card, "Content/Background"), cardName + "/Background");
        AssertNativeStroke(RequireTransform(card, "Content/IconContainer"), cardName + "/IconContainer");
        AssertNativeStroke(RequireTransform(card, "Content/StartButtonVisual"), cardName + "/StartButtonVisual");
        AssertNoStrokeOrOutline(RequireTransform(card, "Content/InnerRice"), cardName + "/InnerRice");
        AssertNoStrokeOrOutline(RequireTransform(card, "Content/Metadata/DurationPill"), cardName + "/DurationPill");
        AssertNoStrokeOrOutline(RequireTransform(card, "Content/Metadata/IntensityPill"), cardName + "/IntensityPill");
        AssertNoStrokeOrOutline(RequireTransform(card, "Content/RecommendationRibbon"), cardName + "/RecommendationRibbon");

        var ribbon = card.Find("Content/RecommendationRibbon");
        AssertTrue((ribbon != null && ribbon.gameObject.activeSelf) == recommended, cardName + " recommendation ribbon state should match the specification.");
        if (recommended) AssertText(card, "Content/RecommendationRibbon/Label", "今日推荐");

        var motion = card.GetComponent<TechModuleCardMotion>();
        AssertTrue(motion != null, cardName + " should reuse TechModuleCardMotion.");
        AssertTrue(!motion.ambientMotion && !motion.playEntrance, cardName + " should stay still when it is not focused.");
        AssertApproximately(motion.hoverScale, 1.05f, 0.001f, cardName + " hover scale should be elder-friendly and restrained.");
        AssertApproximately(motion.pressedScale, 0.96f, 0.001f, cardName + " pressed scale should follow ElderCareUiTheme.");
        AssertApproximately(motion.hoverLiftY, 6f, 0.001f, cardName + " hover lift should be six UI units.");

        foreach (var label in card.GetComponentsInChildren<TMP_Text>(true))
        {
            AssertTrue(label.font != null && label.font.name == "RehabChineseTMP", cardName + " should use the shared Chinese TMP font.");
            AssertTrue(!ContainsEmojiPlaceholder(label.text), cardName + " should not use emoji text as an icon.");
        }
    }

    private static void AssertIconSourcesPresent()
    {
        var requiredPaths = new[]
        {
            "Assets/Resources/HealthSportsIcons/CoreSports/table_tennis.svg",
            "Assets/Resources/HealthSportsIcons/CoreSports/bow_and_arrow.svg",
            "Assets/Resources/HealthSportsIcons/CoreSports/direct_hit.svg",
            "Assets/Resources/HealthSportsIcons/ATTRIBUTION.txt",
            "Assets/Resources/UiIcons/Tabler/UnityWarm/Navigation/arrow-left.svg",
            "Assets/Resources/UiIcons/Tabler/UnityWarm/Controls/clock.svg",
            "Assets/Resources/UiIcons/Tabler/UnityWarm/Controls/player-play.svg",
            "Assets/Resources/UiIcons/Tabler/LICENSE-TABLER"
        };

        foreach (var path in requiredPaths)
        {
            AssertTrue(File.Exists(path), "Required icon source or license is missing: " + path);
        }
    }

    private static void AssertPersistentListener(Button button, string methodName)
    {
        AssertTrue(button.onClick.GetPersistentEventCount() == 1, button.name + " should have exactly one persistent route.");
        AssertTrue(button.onClick.GetPersistentMethodName(0) == methodName, button.name + " should route through " + methodName + ".");
    }

    private static void AssertNativeStroke(Transform target, string label)
    {
        var rounded = target != null ? target.GetComponent<ElderCareRoundedPanel>() : null;
        AssertTrue(rounded != null && rounded.DrawStroke && rounded.StrokeWidth > 0f, label + " should use ElderCareRoundedPanel native stroke.");
        AssertTrue(target.GetComponent<Outline>() == null, label + " should not use Unity Outline.");
    }

    private static void AssertNoStrokeOrOutline(Transform target, string label)
    {
        var rounded = target != null ? target.GetComponent<ElderCareRoundedPanel>() : null;
        AssertTrue(rounded != null && !rounded.DrawStroke, label + " should remain a fill-only rounded layer.");
        AssertTrue(target.GetComponent<Outline>() == null, label + " should not use Unity Outline.");
    }

    private static void AssertText(Transform root, string path, string expected)
    {
        var target = RequireTransform(root, path);
        var label = target.GetComponent<TMP_Text>();
        AssertTrue(label != null && label.text == expected, path + " should read ‘" + expected + "’.");
    }

    private static void AssertSpriteName(Transform root, string path, string expectedName)
    {
        var target = RequireTransform(root, path);
        var image = target.GetComponent<Image>();
        AssertTrue(image != null && image.sprite != null, path + " should use an imported Sprite.");
        AssertTrue(image.sprite.name.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) >= 0, path + " should use the " + expectedName + " asset.");
    }

    private static Transform RequireTransform(Transform root, string path)
    {
        var target = root.Find(path);
        AssertTrue(target != null, "Missing expected UI object: " + root.name + "/" + path);
        return target;
    }

    private static void AssertNoMissingScripts()
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                AssertTrue(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script on " + transform.name + ".");
            }
        }
    }

    private static bool ContainsEmojiPlaceholder(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.IndexOf("🏓", StringComparison.Ordinal) >= 0 ||
               text.IndexOf("🏹", StringComparison.Ordinal) >= 0 ||
               text.IndexOf("🎯", StringComparison.Ordinal) >= 0;
    }

    private static void AssertVector2(Vector2 actual, Vector2 expected, string message)
    {
        AssertTrue(Vector2.Distance(actual, expected) < 0.01f, message + " Actual: " + actual + ".");
    }

    private static void AssertApproximately(float actual, float expected, float tolerance, string message)
    {
        AssertTrue(Mathf.Abs(actual - expected) <= tolerance, message + " Actual: " + actual + ".");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
