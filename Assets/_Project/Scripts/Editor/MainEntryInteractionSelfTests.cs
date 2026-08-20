using System;
using PicoElderCare.Rehab;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainEntryInteractionSelfTests
{
    public static void RunAll()
    {
        FunctionalButtonsHaveFeedbackAndDisabledButtonsDoNot();
        CloseOpensBlockingModalAndContinueReturnsToMainEntry();
        RebuildIsIdempotentAndPreservesInteractionContracts();
        Debug.Log("Main entry interaction self tests passed.");
    }

    private static void RebuildIsIdempotentAndPreservesInteractionContracts()
    {
        var canvasObject = new GameObject("MainEntryRebuildTest", typeof(RectTransform), typeof(Canvas));
        var menu = canvasObject.AddComponent<UnifiedEntryMenu>();
        try
        {
            var panel = HtmlStyleMainEntryPanel.Ensure(canvasObject.transform, menu, null);
            panel.BuildOrRepair();

            AssertTrue(panel.GeneratedUiVersion == HtmlStyleMainEntryPanel.CurrentGeneratedUiVersion, "BuildOrRepair should stamp the current generated UI version for authored-scene migration.");
            AssertTrue(CountNamedChildren(canvasObject.transform, "ExitConfirmationOverlay") == 1, "Repeated BuildOrRepair should create exactly one exit overlay.");
            var overlay = canvasObject.transform.Find("ExitConfirmationOverlay");
            AssertTrue(overlay != null && !overlay.gameObject.activeSelf, "Rebuilt exit confirmation should return to its hidden default state.");

            var close = FindButton(canvasObject.transform, "Close");
            var continueButton = FindButton(overlay, "ExitConfirmationDialog/ExitContinueButton");
            var quitButton = FindButton(overlay, "ExitConfirmationDialog/ExitQuitButton");
            AssertPersistentAction(close, "RequestQuit", "Close");
            AssertPersistentAction(continueButton, "CancelQuit", "Continue");
            AssertPersistentAction(quitButton, "ConfirmQuit", "Quit");

            var healthGame = FindButton(canvasObject.transform, "Panel/Module_HealthGame");
            var rehab = FindButton(canvasObject.transform, "Panel/Module_Rehab");
            var travel = FindButton(canvasObject.transform, "Panel/Module_Travel");
            var memory = FindButton(canvasObject.transform, "Panel/Module_Memory");
            AssertTrue(healthGame.GetComponent<TechModuleCardMotion>() != null && rehab.GetComponent<TechModuleCardMotion>() != null, "Functional module cards should keep TechModuleCardMotion.");
            AssertTrue(travel != null && !travel.interactable && memory != null && !memory.interactable, "Travel and Memory should remain disabled.");
            AssertTrue(travel.GetComponent<RehabButtonHoverFeedback>() == null && memory.GetComponent<RehabButtonHoverFeedback>() == null, "Disabled module cards must not gain ordinary button hover feedback.");

            var settings = FindButton(canvasObject.transform, "Panel/SafeBar/Settings");
            AssertPersistentAction(settings, "OpenTrackerSettings", "Settings");
            var hint = canvasObject.transform.Find("Hint")?.GetComponent<TMPro.TMP_Text>();
            AssertTrue(hint != null && hint.text == "对准可用按钮即可操作；部分功能正在完善中", "Main-entry hint should describe functional controls without development terminology.");

            AssertTrue(HtmlStyleMainEntryPanel.CoversRequiredChineseGlyphs("确定要退出应用吗？"), "RequiredChineseGlyphs should cover the exit title.");
            AssertTrue(HtmlStyleMainEntryPanel.CoversRequiredChineseGlyphs("退出后将返回PICO系统界面"), "RequiredChineseGlyphs should cover the exit description.");
            AssertTrue(HtmlStyleMainEntryPanel.CoversRequiredChineseGlyphs("继续使用退出应用"), "RequiredChineseGlyphs should cover both exit actions.");
            AssertTrue(HtmlStyleMainEntryPanel.CoversRequiredChineseGlyphs("对准可用按钮即可操作；部分功能正在完善中"), "RequiredChineseGlyphs should cover the updated hint.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    private static void CloseOpensBlockingModalAndContinueReturnsToMainEntry()
    {
        var previousEventSystem = EventSystem.current;
        var eventSystemObject = previousEventSystem == null ? new GameObject("MainEntryModalEventSystem", typeof(EventSystem)) : null;
        var canvasObject = new GameObject("MainEntryModalTest", typeof(RectTransform), typeof(Canvas));
        try
        {
            HtmlStyleMainEntryPanel.Ensure(canvasObject.transform, null, null);
            var overlay = canvasObject.transform.Find("ExitConfirmationOverlay");
            var close = FindButton(canvasObject.transform, "Close");

            AssertTrue(overlay != null, "BuildOrRepair should create the exit confirmation overlay.");
            AssertTrue(!overlay.gameObject.activeSelf, "Exit confirmation should be hidden by default.");

            close.transform.localScale = Vector3.one * 1.06f;
            close.onClick.Invoke();
            AssertTrue(overlay.gameObject.activeSelf, "Close should show confirmation instead of quitting immediately.");
            AssertTrue(overlay.GetSiblingIndex() == canvasObject.transform.childCount - 1, "Exit confirmation should remain above every main-entry control.");
            AssertTrue(Vector3.Distance(close.transform.localScale, Vector3.one) < 0.001f, "Opening confirmation should reset the Close hover/press visual state.");

            var blocker = overlay.Find("ExitConfirmationBlocker")?.GetComponent<Graphic>();
            var continueButton = FindButton(overlay, "ExitConfirmationDialog/ExitContinueButton");
            var quitButton = FindButton(overlay, "ExitConfirmationDialog/ExitQuitButton");
            var title = overlay.Find("ExitConfirmationDialog/ExitConfirmationTitle")?.GetComponent<TMPro.TMP_Text>();
            var description = overlay.Find("ExitConfirmationDialog/ExitConfirmationDescription")?.GetComponent<TMPro.TMP_Text>();

            AssertTrue(blocker != null && blocker.raycastTarget, "Exit confirmation blocker should consume background raycasts.");
            AssertInteractiveWithFeedback(continueButton, "Continue");
            AssertInteractiveWithFeedback(quitButton, "Quit");
            AssertTrue(title != null && title.text == "确定要退出应用吗？", "Exit confirmation should use the explicit Chinese title.");
            AssertTrue(description != null && description.text == "退出后将返回 PICO 系统界面", "Exit confirmation should explain the PICO destination.");
            AssertTrue(EventSystem.current == null || EventSystem.current.currentSelectedGameObject == continueButton.gameObject, "Continue should be the safe default selection.");

            continueButton.onClick.Invoke();
            AssertTrue(!overlay.gameObject.activeSelf, "Continue should close the exit confirmation.");

            close.onClick.Invoke();
            AssertTrue(overlay.gameObject.activeSelf, "Exit confirmation should open again after Continue.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
            if (eventSystemObject != null) UnityEngine.Object.DestroyImmediate(eventSystemObject);
        }
    }

    private static void FunctionalButtonsHaveFeedbackAndDisabledButtonsDoNot()
    {
        var canvasObject = new GameObject("MainEntryInteractionTest", typeof(RectTransform), typeof(Canvas));
        try
        {
            var panel = HtmlStyleMainEntryPanel.Ensure(canvasObject.transform, null, null);
            var settings = FindButton(canvasObject.transform, "Panel/SafeBar/Settings");
            var close = FindButton(canvasObject.transform, "Close");
            var minimize = FindButton(canvasObject.transform, "Minimize");
            var health = FindButton(canvasObject.transform, "Panel/SafeBar/Health");
            var rank = FindButton(canvasObject.transform, "Panel/SafeBar/Rank");

            AssertTrue(panel != null, "Main entry panel should be created.");
            AssertInteractiveWithFeedback(settings, "Settings");
            AssertInteractiveWithFeedback(close, "Close");
            AssertDisabledWithoutFeedback(minimize, "Minimize");
            AssertDisabledWithoutFeedback(health, "Health");
            AssertDisabledWithoutFeedback(rank, "Rank");
            AssertTrue(close.targetGraphic != null && close.targetGraphic.raycastTarget, "Close should receive UI raycasts.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    private static Button FindButton(Transform root, string path)
    {
        var target = root.Find(path);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static void AssertInteractiveWithFeedback(Button button, string name)
    {
        AssertTrue(button != null, name + " button should exist.");
        AssertTrue(button.interactable, name + " button should be interactable.");
        AssertTrue(button.GetComponent<RehabButtonHoverFeedback>() != null, name + " should use RehabButtonHoverFeedback.");
    }

    private static void AssertDisabledWithoutFeedback(Button button, string name)
    {
        AssertTrue(button != null, name + " button should exist.");
        AssertTrue(!button.interactable, name + " button should remain disabled.");
        AssertTrue(button.GetComponent<RehabButtonHoverFeedback>() == null, name + " must not imply functionality through hover feedback.");
    }

    private static void AssertPersistentAction(Button button, string methodName, string name)
    {
        AssertTrue(button != null, name + " button should exist.");
        AssertTrue(button.onClick.GetPersistentEventCount() == 1, name + " should have exactly one authored listener after rebuilding.");
        AssertTrue(button.onClick.GetPersistentMethodName(0) == methodName, name + " should invoke " + methodName + ".");
    }

    private static int CountNamedChildren(Transform root, string name)
    {
        if (root == null) return 0;
        var count = root.name == name ? 1 : 0;
        for (var i = 0; i < root.childCount; i++) count += CountNamedChildren(root.GetChild(i), name);
        return count;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
