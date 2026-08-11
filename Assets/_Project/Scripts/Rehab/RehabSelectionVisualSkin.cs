using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab
{
    public static class RehabSelectionVisualSkin
    {
        public static readonly Vector2 CanvasSize = ElderCareMenuDesignTokens.SecondaryCanvasSize;
        public static readonly Vector2 CardSize = ElderCareMenuDesignTokens.SecondaryTwoCardSize;
        public const float SelectionWorldScaleCompensation =
            ElderCareMenuDesignTokens.RehabSelectionWorldScaleCompensation;

        public struct MenuElements
        {
            public Button baduanjinButton;
            public Button taiChiButton;
            public Button backButton;
        }

        public static bool IsBuilt(GameObject panel)
        {
            return panel != null && panel.transform.Find("ChoiceCards") != null;
        }

        public static MenuElements Build(Transform panelTransform, TMP_FontAsset font, Sprite backIcon, Sprite clockIcon, Sprite playIcon)
        {
            if (panelTransform == null) throw new System.ArgumentNullException(nameof(panelTransform));
            DisableLegacyRootSurface(panelTransform);
            ClearChildren(panelTransform);

            var panel = panelTransform as RectTransform;
            if (panel == null) throw new System.InvalidOperationException("Rehab selection panel requires a RectTransform.");
            ElderCareMenuPanelBuilder.ConfigureStretch(panel);
            ElderCareMenuPanelBuilder.BuildPanelFrame(panel, CanvasSize);
            ElderCareMenuPanelBuilder.BuildHeader(
                panel,
                CanvasSize,
                font,
                "请选择康复训练类型",
                "跟着虚拟教练慢慢来，随时可以暂停休息");

            var cards = ElderCareMenuPanelBuilder.CreateRect("ChoiceCards", panel, new Vector2(716f, 326f), new Vector2(0f, -25f));
            var baduanjinButton = ElderCareChoiceCardBuilder.Build(cards, font, new ElderCareChoiceCardSpec
            {
                Name = "BaduanjinButton",
                Position = new Vector2(-188f, 0f),
                Size = CardSize,
                Title = "八段锦",
                Subtitle = "舒展筋骨 · 经典养生",
                Duration = "约 10 分钟",
                Intensity = "强度轻",
                ActionText = "开始训练",
                UseLineHero = true,
                LineHeroType = ElderCareIconType.BaduanjinStretch,
                ClockIcon = clockIcon,
                ActionIcon = playIcon,
                Accent = ElderCareMenuDesignTokens.Amber,
                Recommended = true,
                Interactable = true
            });
            var taiChiButton = ElderCareChoiceCardBuilder.Build(cards, font, new ElderCareChoiceCardSpec
            {
                Name = "TaiChiButton",
                Position = new Vector2(188f, 0f),
                Size = CardSize,
                Title = "太极",
                Subtitle = "柔和缓慢 · 平衡身心",
                Duration = "约 12 分钟",
                Intensity = "强度中",
                ActionText = "开始训练",
                UseLineHero = true,
                LineHeroType = ElderCareIconType.TaiChi,
                ClockIcon = clockIcon,
                ActionIcon = playIcon,
                Accent = ElderCareMenuDesignTokens.Jade,
                Recommended = false,
                Interactable = true
            });
            var backButton = ElderCareMenuPanelBuilder.BuildBottomDock(
                panel,
                CanvasSize,
                font,
                backIcon,
                "将射线停留在训练卡片上即可选择",
                "返回上一页");

            return new MenuElements
            {
                baduanjinButton = baduanjinButton,
                taiChiButton = taiChiButton,
                backButton = backButton
            };
        }

        private static void DisableLegacyRootSurface(Transform panelTransform)
        {
            // Older authored rehab scenes used the page root itself as a dark teal
            // background. The shared warm frame now owns the complete visible
            // surface, so leaving that Graphic enabled exposes a colored rim around
            // the new frame. Keep the page/root Transform intact and suppress only
            // the obsolete visual component.
            var legacySurface = panelTransform.GetComponent<ElderCareRoundedPanel>();
            if (legacySurface == null) return;

            legacySurface.enabled = false;
            legacySurface.raycastTarget = false;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
