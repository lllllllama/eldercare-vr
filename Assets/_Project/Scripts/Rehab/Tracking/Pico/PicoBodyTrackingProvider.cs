using System;
using PicoElderCare.Rehab.Tracking.Pico.ObjectTracking;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class PicoBodyTrackingProvider : RehabPoseProviderBase
    {
        private const int ApiSuccess = 0;
        private const string SupportQueryFailedMessage = "查询 PICO Body Tracking 支持状态失败。";
        private const string StartFailedMessage = "启动 PICO Body Tracking 失败。";
        private const string StateQueryFailedMessage = "读取 PICO Body Tracking 状态失败。";
        private const string DataQueryFailedMessage = "读取 PICO Body Tracking 关节数据失败。请确认两个 Motion Tracker 均在线并已完成校准。";
        private const string MissingOriginMessage = "世界空间输出需要绑定 XR Origin。";
        private const string NoMappedJointsMessage = "PICO 返回的数据中没有可映射的身体关节。";

        [SerializeField] private bool autoStartOnEnable = true;
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private PicoBodyTrackingOutputSpace outputSpace = PicoBodyTrackingOutputSpace.XrOriginLocal;
        [SerializeField] private bool seatedMode;
        [SerializeField] private PicoBodyTrackingDiagnostics diagnostics = new PicoBodyTrackingDiagnostics();

        private readonly PicoBodyTrackingFrame _picoFrame = new PicoBodyTrackingFrame();
        private IPicoBodyTrackingApi _api;
        private bool _ownsApi;
        private bool _supportKnown;
        private bool _isSupported;
        private bool _isRunning;
        private RehabTrackingState _trackingState = RehabTrackingState.Unavailable;
        private string _statusMessage = PicoBodyTrackingStatusMapper.GetStatusMessage(RehabTrackingState.Unavailable);

        public bool AutoStartOnEnable
        {
            get { return autoStartOnEnable; }
            set { autoStartOnEnable = value; }
        }

        public Transform XrOrigin
        {
            get { return xrOrigin; }
            set { xrOrigin = value; }
        }

        public PicoBodyTrackingOutputSpace OutputSpace
        {
            get { return outputSpace; }
            set { outputSpace = value; }
        }

        public PicoBodyTrackingDiagnostics Diagnostics
        {
            get { return diagnostics; }
        }

        public override bool IsSupported
        {
            get { return !_supportKnown || _isSupported; }
        }

        public override bool IsRunning
        {
            get { return _isRunning; }
        }

        public override RehabTrackingState TrackingState
        {
            get { return _trackingState; }
        }

        public override string StatusMessage
        {
            get { return _statusMessage; }
        }

        private void OnEnable()
        {
            if (autoStartOnEnable)
            {
                StartTracking();
            }
        }

        private void OnDisable()
        {
            StopTracking();
        }

        private void OnDestroy()
        {
            StopTracking();
            DisposeOwnedApi();
        }

        public override void StartTracking()
        {
            if (_isRunning)
            {
                return;
            }

            // The application-level wrist runtime owns the mutually exclusive
            // Motion Tracker Object Tracking mode. Keep this legacy provider
            // available for explicit A/B tests, but never auto-start it behind
            // the new runtime.
            if (WristTrackingRuntime.Instance != null)
            {
                autoStartOnEnable = false;
                SetState(RehabTrackingState.Unavailable, "Object Tracking 模式已启用，Body Tracking 保持关闭。");
                return;
            }

            diagnostics.validJointCount = 0;
            SetState(RehabTrackingState.Starting, string.Empty);

            try
            {
                EnsureApi();
                diagnostics.supportResult = _api.GetBodyTrackingSupported(out _isSupported);
                _supportKnown = diagnostics.supportResult == ApiSuccess;
                if (diagnostics.supportResult != ApiSuccess)
                {
                    CaptureApiError(SupportQueryFailedMessage);
                    SetState(RehabTrackingState.Error, SupportQueryFailedMessage);
                    return;
                }

                if (!_isSupported)
                {
                    SetState(RehabTrackingState.Unsupported, string.Empty);
                    return;
                }

                diagnostics.startResult = _api.StartBodyTracking();
                if (diagnostics.startResult != ApiSuccess)
                {
                    CaptureApiError(StartFailedMessage);
                    SetState(RehabTrackingState.Error, StartFailedMessage);
                    return;
                }

                _isRunning = true;
                RefreshTrackingState();
            }
            catch (Exception exception)
            {
                diagnostics.lastError = exception.Message;
                SetState(RehabTrackingState.Error, StartFailedMessage);
            }
        }

        public override void StopTracking()
        {
            if (!_isRunning || _api == null)
            {
                return;
            }

            try
            {
                diagnostics.stopResult = _api.StopBodyTracking();
                if (diagnostics.stopResult != ApiSuccess)
                {
                    CaptureApiError("Failed to stop PICO Body Tracking.");
                }
            }
            catch (Exception exception)
            {
                diagnostics.lastError = exception.Message;
            }

            _isRunning = false;
            diagnostics.validJointCount = 0;
            SetState(RehabTrackingState.Unavailable, string.Empty);
        }

        public override bool TryGetSample(RehabBodySample target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            diagnostics.validJointCount = 0;
            target.timestamp = Time.realtimeSinceStartupAsDouble;
            target.isSeatedMode = seatedMode;
            if (!_isRunning || _api == null)
            {
                target.trackingState = _trackingState;
                return false;
            }

            if (!RefreshTrackingState())
            {
                target.trackingState = _trackingState;
                diagnostics.failedSampleCount++;
                return false;
            }

            if (_trackingState != RehabTrackingState.Valid &&
                _trackingState != RehabTrackingState.Limited)
            {
                target.trackingState = _trackingState;
                diagnostics.failedSampleCount++;
                return false;
            }

            if (outputSpace == PicoBodyTrackingOutputSpace.World && xrOrigin == null)
            {
                SetState(RehabTrackingState.Error, MissingOriginMessage);
                target.trackingState = _trackingState;
                diagnostics.failedSampleCount++;
                return false;
            }

            var dataExceptionCaught = false;
            try
            {
                diagnostics.dataResult = _api.GetBodyTrackingData(_picoFrame);
            }
            catch (Exception exception)
            {
                dataExceptionCaught = true;
                diagnostics.lastError = exception.Message;
                diagnostics.dataResult = -1;
            }

            if (diagnostics.dataResult != ApiSuccess)
            {
                if (!dataExceptionCaught)
                {
                    CaptureApiError(DataQueryFailedMessage);
                }
                SetState(RehabTrackingState.Error, DataQueryFailedMessage);
                target.trackingState = _trackingState;
                diagnostics.failedSampleCount++;
                return false;
            }

            for (var i = 0; i < _picoFrame.joints.Length; i++)
            {
                var source = _picoFrame.joints[i];
                RehabJoint joint;
                if (!source.valid || !PicoBodyJointMapper.TryMap(source.role, out joint))
                {
                    continue;
                }

                RehabJointPose pose;
                if (PicoBodyCoordinateConverter.TryConvert(
                        source,
                        outputSpace,
                        xrOrigin,
                        _trackingState,
                        out pose))
                {
                    target.SetJoint(joint, pose);
                }
            }

            target.trackingState = _trackingState;
            if (target.validJointCount == 0)
            {
                SetState(RehabTrackingState.Error, NoMappedJointsMessage);
                target.trackingState = _trackingState;
                diagnostics.failedSampleCount++;
                return false;
            }

            diagnostics.successfulSampleCount++;
            diagnostics.validJointCount = target.validJointCount;
            return true;
        }

        public bool RequestCalibration()
        {
            try
            {
                EnsureApi();
                if (_supportKnown && !_isSupported)
                {
                    return false;
                }

                diagnostics.calibrationResult = _api.StartMotionTrackerCalibApp();
                if (diagnostics.calibrationResult != ApiSuccess)
                {
                    CaptureApiError("Failed to open the Motion Tracker calibration app.");
                }
                return diagnostics.calibrationResult == ApiSuccess;
            }
            catch (Exception exception)
            {
                diagnostics.lastError = exception.Message;
                diagnostics.calibrationResult = -1;
                return false;
            }
        }

        public Vector3 ConvertSamplePositionToWorld(Vector3 samplePosition)
        {
            return outputSpace == PicoBodyTrackingOutputSpace.World || xrOrigin == null
                ? samplePosition
                : xrOrigin.TransformPoint(samplePosition);
        }

        public void SetApiForTesting(IPicoBodyTrackingApi api)
        {
            StopTracking();
            DisposeOwnedApi();
            _api = api;
            _ownsApi = false;
            _supportKnown = false;
            _isSupported = false;
            SetState(RehabTrackingState.Unavailable, string.Empty);
        }

        private bool RefreshTrackingState()
        {
            try
            {
                PicoBodyTrackingApiState apiState;
                diagnostics.stateResult = _api.GetBodyTrackingState(out apiState);
                if (diagnostics.stateResult != ApiSuccess)
                {
                    CaptureApiError(StateQueryFailedMessage);
                    SetState(RehabTrackingState.Error, StateQueryFailedMessage);
                    return false;
                }

                SetState(PicoBodyTrackingStatusMapper.Map(apiState), string.Empty);
                return true;
            }
            catch (Exception exception)
            {
                diagnostics.lastError = exception.Message;
                diagnostics.stateResult = -1;
                SetState(RehabTrackingState.Error, StateQueryFailedMessage);
                return false;
            }
        }

        private void EnsureApi()
        {
            if (_api != null)
            {
                return;
            }

            _api = new PicoBodyTrackingApi();
            _ownsApi = true;
        }

        private void DisposeOwnedApi()
        {
            if (_ownsApi && _api != null)
            {
                _api.Dispose();
            }

            if (_ownsApi)
            {
                _api = null;
            }

            _ownsApi = false;
        }

        private void SetState(RehabTrackingState state, string explicitMessage)
        {
            _trackingState = state;
            diagnostics.trackingState = state;
            _statusMessage = string.IsNullOrEmpty(explicitMessage)
                ? PicoBodyTrackingStatusMapper.GetStatusMessage(state)
                : explicitMessage;
            if (state != RehabTrackingState.Error)
            {
                diagnostics.lastError = string.Empty;
            }
        }

        private void CaptureApiError(string fallbackMessage)
        {
            diagnostics.lastError = _api != null && !string.IsNullOrEmpty(_api.LastError)
                ? _api.LastError
                : fallbackMessage;
        }
    }
}
