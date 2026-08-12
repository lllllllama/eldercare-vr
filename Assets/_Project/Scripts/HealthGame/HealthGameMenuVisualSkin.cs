using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.HealthGame
{
    public static class HealthGameMenuVisualSkin
    {
        public static readonly Vector2 CanvasSize = ElderCareMenuDesignTokens.SecondaryCanvasSize;
        public static readonly Vector2 SportCardSize = ElderCareMenuDesignTokens.SecondaryThreeCardSize;
        public const float CanvasWorldScale = ElderCareMenuDesignTokens.SecondaryCanvasWorldScale;

        public struct MenuElements
        {
            public GameObject panel;
            public Button pingPongButton;
            public Button archeryButton;
            public Button dartsButton;
            public Button backButton;
        }

        public static MenuElements Build(
            Transform canvas,
            TMP_FontAsset font,
            Sprite pingPongIcon,
            Sprite archeryIcon,
            Sprite dartsIcon,
            Sprite backIcon,
            Sprite clockIcon,
            Sprite playIcon)
        {
            var panel = ElderCareMenuPanelBuilder.CreateRect("Panel", canvas, CanvasSize, Vector2.zero);
            ElderCareMenuPanelBuilder.ConfigureStretch(panel);
            ElderCareMenuPanelBuilder.BuildPanelFrame(panel, CanvasSize);
            ElderCareMenuPanelBuilder.BuildHeader(
                panel,
                CanvasSize,
                font,
                "请选择健康运动类型",
                "选择喜欢的运动，活动身体、轻松锻炼");

            var cards = ElderCareMenuPanelBuilder.CreateRect("ChoiceCards", panel, new Vector2(824f, 326f), new Vector2(0f, -25f));
            var pingPongButton = ElderCareChoiceCardBuilder.Build(cards, font, CreateSpec(
                "PingPongCard",
                new Vector2(-274f, 0f),
                "乒乓球",
                "灵活反应 · 活动身体",
                "约 8 分钟",
                "强度轻",
                pingPongIcon,
                clockIcon,
                playIcon,
                ElderCareMenuDesignTokens.Jade,
                true));
            var archeryButton = ElderCareChoiceCardBuilder.Build(cards, font, CreateSpec(
                "ArcheryCard",
                Vector2.zero,
                "射箭",
                "稳定瞄准 · 锻炼协调",
                "约 10 分钟",
                "强度中",
                archeryIcon,
                clockIcon,
                playIcon,
                ElderCareMenuDesignTokens.Amber,
                false));
            var dartsButton = ElderCareChoiceCardBuilder.Build(cards, font, CreateSpec(
                "DartsCard",
                new Vector2(274f, 0f),
                "飞镖",
                "专注投掷 · 轻松挑战",
                "约 8 分钟",
                "强度轻",
                dartsIcon,
                clockIcon,
                playIcon,
                ElderCareMenuDesignTokens.Coral,
                false));

            var backButton = ElderCareMenuPanelBuilder.BuildBottomDock(
                panel,
                CanvasSize,
                font,
                backIcon,
                "将射线停留在运动卡片上即可选择",
                "返回上一页");

            return new MenuElements
            {
                panel = panel.gameObject,
                pingPongButton = pingPongButton,
                archeryButton = archeryButton,
                dartsButton = dartsButton,
                backButton = backButton
            };
        }

        private static ElderCareChoiceCardSpec CreateSpec(
            string name,
            Vector2 position,
            string title,
            string subtitle,
            string duration,
            string intensity,
            Sprite heroIcon,
            Sprite clockIcon,
            Sprite playIcon,
            Color accent,
            bool recommended)
        {
            return new ElderCareChoiceCardSpec
            {
                Name = name,
                Position = position,
                Size = SportCardSize,
                Title = title,
                Subtitle = subtitle,
                Duration = duration,
                Intensity = intensity,
                ActionText = "开始运动",
                HeroIcon = heroIcon,
                ClockIcon = clockIcon,
                ActionIcon = playIcon,
                Accent = accent,
                Recommended = recommended,
                Interactable = true
            };
        }
    }
}
