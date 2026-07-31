using Unity.XR.PXR;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public static class PicoBodyTrackingStatusMapper
    {
        public static RehabTrackingState Map(PicoBodyTrackingApiState state)
        {
            if (state.message == BodyTrackingMessage.BT_MESSAGE_TRACKER_NOT_CALIBRATED ||
                (state.message == BodyTrackingMessage.BT_MESSAGE_UNKNOWN && !state.isTracking) ||
                state.message == BodyTrackingMessage.BT_MESSAGE_USER_CHANGE)
            {
                return RehabTrackingState.WaitingForCalibration;
            }

            if (state.message == BodyTrackingMessage.BT_MESSAGE_TRACKER_NUM_NOT_ENOUGH)
            {
                return RehabTrackingState.WaitingForDevice;
            }

            if (state.statusCode == BodyTrackingStatusCode.BT_LIMITED && state.isTracking)
            {
                return RehabTrackingState.Limited;
            }

            if (state.statusCode == BodyTrackingStatusCode.BT_VALID && state.isTracking)
            {
                return RehabTrackingState.Valid;
            }

            if (state.message == BodyTrackingMessage.BT_MESSAGE_TRACKER_PERSISTENT_INVISIBILITY ||
                state.message == BodyTrackingMessage.BT_MESSAGE_TRACKER_STATE_NOT_SATISFIED)
            {
                return RehabTrackingState.Lost;
            }

            if (state.message == BodyTrackingMessage.BT_MESSAGE_TRACKER_DATA_ERROR ||
                state.message == BodyTrackingMessage.BT_MESSAGE_TRACKING_POSE_ERROR)
            {
                return RehabTrackingState.Error;
            }

            return RehabTrackingState.Lost;
        }

        public static string GetStatusMessage(RehabTrackingState state)
        {
            switch (state)
            {
                case RehabTrackingState.Unsupported: return "当前设备不支持 PICO Body Tracking。";
                case RehabTrackingState.Starting: return "正在启动 PICO Body Tracking。";
                case RehabTrackingState.WaitingForDevice: return "等待两个 PICO Motion Tracker 连接。";
                case RehabTrackingState.WaitingForCalibration: return "Motion Tracker 尚未完成身体追踪校准。";
                case RehabTrackingState.Valid: return "PICO 全身追踪有效。";
                case RehabTrackingState.Limited: return "PICO 全身追踪受限，训练计时应暂停。";
                case RehabTrackingState.Lost: return "PICO 全身追踪已丢失。";
                case RehabTrackingState.Error: return "PICO Body Tracking API 返回错误。";
                default: return "PICO Body Tracking 未启动。";
            }
        }
    }
}
