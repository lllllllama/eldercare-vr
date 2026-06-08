namespace PicoElderCare.Rehab
{
    public static class BaduanjinGuotiDetailedCatalog
    {
        private const float RequiredHoldSeconds = 1.5f;
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
            return new MovementDefinition(
                movementId,
                movementName,
                movementName,
                new MovementStepDefinition(
                    movementName,
                    movementName,
                    RequiredHoldSeconds,
                    TimeoutSeconds));
        }
    }
}
