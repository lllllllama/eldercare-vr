namespace PicoElderCare.Rehab
{
    public static class BaduanjinGuotiDetailedCatalog
    {
        private const float RequiredHoldSeconds = 0.8f;
        private const float TimeoutSeconds = 25f;

        public static MovementDefinition[] CreateMovements()
        {
            return new[]
            {
                Create(RehabMovementId.Baduanjin_Guoti_00_WujiZhuang, "\u65e0\u6781\u6869"),
                Create(RehabMovementId.Baduanjin_Guoti_01_BaoqiuZhuang, "\u62b1\u7403\u6869\uff08\u9884\u5907\u52bf\uff09"),
                Create(RehabMovementId.Baduanjin_Guoti_02_LiangshouTuotian, "\u4e24\u624b\u6258\u5929\u7406\u4e09\u7126\uff086 \u6b21\uff09"),
                Create(RehabMovementId.Baduanjin_Guoti_03_YouKaigong, "\u53f3\u5f00\u5f13"),
                Create(RehabMovementId.Baduanjin_Guoti_04_YouKaigongBingbu, "\u53f3\u5f00\u5de5\u5e76\u6b65"),
                Create(RehabMovementId.Baduanjin_Guoti_05_ZuoKaigong, "\u5de6\u5f00\u5de5"),
                Create(RehabMovementId.Baduanjin_Guoti_06_ZuoKaigongBingbu, "\u5de6\u5f00\u5f13\u5e76\u6b65"),
                Create(RehabMovementId.Baduanjin_Guoti_07_YouShangju, "\u53f3\u4e0a\u4e3e"),
                Create(RehabMovementId.Baduanjin_Guoti_08_YouXialuo, "\u53f3\u4e0b\u843d"),
                Create(RehabMovementId.Baduanjin_Guoti_09_ZuoShangju, "\u5de6\u4e0a\u4e3e"),
                Create(RehabMovementId.Baduanjin_Guoti_10_ZuoXialuo, "\u5de6\u4e0b\u843d"),
                Create(RehabMovementId.Baduanjin_Guoti_11_YouHouqiao, "\u53f3\u540e\u77a7"),
                Create(RehabMovementId.Baduanjin_Guoti_12_YouHouqiaoZhuanzheng, "\u53f3\u540e\u77a7\u8f6c\u6b63"),
                Create(RehabMovementId.Baduanjin_Guoti_13_ZuoHouqiao, "\u5de6\u540e\u77a7"),
                Create(RehabMovementId.Baduanjin_Guoti_14_ZuoHouqiaoZhuanzheng, "\u5de6\u540e\u77a7\u8f6c\u6b63"),
                Create(RehabMovementId.Baduanjin_Guoti_15_ShangtuoXiaan, "\u4e0a\u6258\u4e0b\u6309"),
                Create(RehabMovementId.Baduanjin_Guoti_16_YouxuanYaotouBaiwei, "\u53f3\u65cb\u6447\u5934\u6446\u5c3e"),
                Create(RehabMovementId.Baduanjin_Guoti_17_ZuoxuanYaotouBaiwei, "\u5de6\u65cb\u6447\u5934\u6446\u5c3e"),
                Create(RehabMovementId.Baduanjin_Guoti_18_LiangshouPanzu, "\u4e24\u624b\u6500\u8db3\u56fa\u80be\u8170"),
                Create(RehabMovementId.Baduanjin_Guoti_19_TaishouFanchuan, "\u62ac\u624b\u53cd\u7a7f"),
                Create(RehabMovementId.Baduanjin_Guoti_20_FanchuanPanzu, "\u53cd\u7a7f\u6500\u8db3"),
                Create(RehabMovementId.Baduanjin_Guoti_21_PanzuJushou, "\u6500\u8db3\u4e3e\u624b"),
                Create(RehabMovementId.Baduanjin_Guoti_22_JushouXiaanFuwei, "\u4e3e\u624b\u4e0b\u6309\u590d\u4f4d"),
                Create(RehabMovementId.Baduanjin_Guoti_23_CuanquanMabu, "\u6512\u62f3\u9a6c\u6b65"),
                Create(RehabMovementId.Baduanjin_Guoti_24_ChuquanShouquan, "\u51fa\u62f3\u6536\u62f3"),
                Create(RehabMovementId.Baduanjin_Guoti_25_HuanshouChuquanShouquan, "\u6362\u624b\u51fa\u62f3\u6536\u62f3"),
                Create(RehabMovementId.Baduanjin_Guoti_26_JieshuFuwei, "\u7ed3\u675f\u590d\u4f4d"),
                Create(RehabMovementId.Baduanjin_Guoti_27_Tizhong, "\u63d0\u8e35"),
                Create(RehabMovementId.Baduanjin_Guoti_28_ShuangshouBaofu, "\u53cc\u624b\u62b1\u8179"),
                Create(RehabMovementId.Baduanjin_Guoti_29_ShoushiTiaoxi, "\u6536\u52bf\u8c03\u606f")
            };
        }

        private static MovementDefinition Create(RehabMovementId movementId, string movementName)
        {
            var instruction = GetInstruction(movementId);
            return new MovementDefinition(
                movementId,
                movementName,
                instruction,
                new MovementStepDefinition(
                    movementName,
                    instruction,
                    RequiredHoldSeconds,
                    TimeoutSeconds));
        }

        private static string GetInstruction(RehabMovementId movementId)
        {
            switch (movementId)
            {
                case RehabMovementId.Baduanjin_Guoti_00_WujiZhuang:
                    return "双手自然下垂，上身放松稳定";
                case RehabMovementId.Baduanjin_Guoti_01_BaoqiuZhuang:
                    return "双腕在腹部至胸前形成舒适抱球姿势";
                case RehabMovementId.Baduanjin_Guoti_02_LiangshouTuotian:
                    return "双腕同步上举、头顶短暂停留，再缓慢回到起始位置";
                case RehabMovementId.Baduanjin_Guoti_03_YouKaigong:
                    return "右腕向右舒适展开，左腕留在胸前";
                case RehabMovementId.Baduanjin_Guoti_04_YouKaigongBingbu:
                    return "右开弓结束，双腕缓慢收回身前";
                case RehabMovementId.Baduanjin_Guoti_05_ZuoKaigong:
                    return "左腕向左舒适展开，右腕留在胸前";
                case RehabMovementId.Baduanjin_Guoti_06_ZuoKaigongBingbu:
                    return "左开弓结束，双腕缓慢收回身前";
                case RehabMovementId.Baduanjin_Guoti_07_YouShangju:
                    return "右腕上举、左腕自然下按";
                case RehabMovementId.Baduanjin_Guoti_08_YouXialuo:
                    return "右腕缓慢下落并回到身前";
                case RehabMovementId.Baduanjin_Guoti_09_ZuoShangju:
                    return "左腕上举、右腕自然下按";
                case RehabMovementId.Baduanjin_Guoti_10_ZuoXialuo:
                    return "左腕缓慢下落并回到身前";
                case RehabMovementId.Baduanjin_Guoti_11_YouHouqiao:
                    return "头部缓慢向右转到舒适角度";
                case RehabMovementId.Baduanjin_Guoti_12_YouHouqiaoZhuanzheng:
                    return "从右后瞧缓慢转回正前方";
                case RehabMovementId.Baduanjin_Guoti_13_ZuoHouqiao:
                    return "头部缓慢向左转到舒适角度";
                case RehabMovementId.Baduanjin_Guoti_14_ZuoHouqiaoZhuanzheng:
                    return "从左后瞧缓慢转回正前方";
                case RehabMovementId.Baduanjin_Guoti_15_ShangtuoXiaan:
                    return "一腕舒适上托，另一腕自然下按";
                case RehabMovementId.Baduanjin_Guoti_16_YouxuanYaotouBaiwei:
                    return "上身向右舒适转移，避免大幅弯腰";
                case RehabMovementId.Baduanjin_Guoti_17_ZuoxuanYaotouBaiwei:
                    return "上身向左舒适转移，避免大幅弯腰";
                case RehabMovementId.Baduanjin_Guoti_18_LiangshouPanzu:
                    return "双腕向腿部方向舒适下探，不要求触碰脚面";
                case RehabMovementId.Baduanjin_Guoti_19_TaishouFanchuan:
                    return "双腕从下方缓慢抬至胸前";
                case RehabMovementId.Baduanjin_Guoti_20_FanchuanPanzu:
                    return "双腕随反穿动作再次向下舒适伸展";
                case RehabMovementId.Baduanjin_Guoti_21_PanzuJushou:
                    return "双腕由下向上举至肩部附近";
                case RehabMovementId.Baduanjin_Guoti_22_JushouXiaanFuwei:
                    return "双腕缓慢下按，回到腹部附近";
                case RehabMovementId.Baduanjin_Guoti_23_CuanquanMabu:
                    return "双腕收在腰部两侧；马步只作安全引导";
                case RehabMovementId.Baduanjin_Guoti_24_ChuquanShouquan:
                    return "一侧腕部温和向前出拳，另一侧留在腰间";
                case RehabMovementId.Baduanjin_Guoti_25_HuanshouChuquanShouquan:
                    return "换侧温和出拳，肩臂保持放松";
                case RehabMovementId.Baduanjin_Guoti_26_JieshuFuwei:
                    return "双腕收回腰部两侧并放松";
                case RehabMovementId.Baduanjin_Guoti_27_Tizhong:
                    return "保持上身稳定并轻柔提踵；不考核脚部幅度";
                case RehabMovementId.Baduanjin_Guoti_28_ShuangshouBaofu:
                    return "双手放在腹部前方，保持舒适间距";
                case RehabMovementId.Baduanjin_Guoti_29_ShoushiTiaoxi:
                    return "上身稳定，双手放松于腹前并自然呼吸";
                default:
                    return "跟随视频在舒适范围内完成动作";
            }
        }
    }
}
