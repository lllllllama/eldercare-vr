using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.XR.PXR;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    /// <summary>
    /// Allocation-free facade over the Object/Independent Tracking API shipped
    /// in PICO Integration SDK 3.4.0. The SDK's long tracker id is the stable
    /// identifier persisted by the application; list order is never semantic.
    /// </summary>
    public sealed class PicoObjectTrackingApi : IPicoObjectTrackingApi
    {
        private const int ApiSuccess = 0;
        private const int MaximumTrackerCount = 3;
        private const string PicoPlatformLibrary = "PxrPlatform";

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMotionTrackerLocation
        {
            public Posef pose;
            public Vector3f angularVelocity;
            public Vector3f linearVelocity;
            public Vector3f angularAcceleration;
            public Vector3f linearAcceleration;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport(
            PicoPlatformLibrary,
            EntryPoint = "Pxr_GetLocateMotionTracker",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMotionTrackerLocationNative(
            long trackerId,
            ref NativeMotionTrackerLocation location,
            ref bool isValidPose);
#endif

        private readonly long[] _nativeIds = new long[MaximumTrackerCount];
        private readonly string[] _stableIds = new string[MaximumTrackerCount];
        private readonly bool[] _connected = new bool[MaximumTrackerCount];
        private readonly PicoObjectTrackingDiagnostics _diagnostics = new PicoObjectTrackingDiagnostics();
        private int _connectedCount;
        private bool _running;
        private bool _disposed;

        public bool IsSupported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsRunning { get { return _running; } }
        public int ConnectedTrackerCount { get { return _connectedCount; } }
        public string LastError { get { return _diagnostics.lastError; } }
        public PicoObjectTrackingDiagnostics Diagnostics { get { return _diagnostics; } }

        public bool StartTracking()
        {
            if (_disposed)
            {
                SetError("Object Tracking API 已释放。");
                return false;
            }

            if (_running)
            {
                return true;
            }

            if (!IsSupported)
            {
                SetError("当前平台不支持 PICO Object Tracking。");
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
            // Object Tracking and Body Tracking share the Motion Tracker mode.
            // Stop the legacy body mode before requesting independent trackers.
            PXR_MotionTracking.StopBodyTracking();
            PXR_MotionTracking.RequestMotionTrackerCompleteAction += HandleTrackerRequestCompleted;
            PXR_MotionTracking.MotionTrackerConnectionAction += HandleTrackerConnectionChanged;
            _running = true;
            return RefreshTrackers();
#else
            return false;
#endif
        }

        public void StopTracking()
        {
            if (!_running)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
            PXR_MotionTracking.RequestMotionTrackerCompleteAction -= HandleTrackerRequestCompleted;
            PXR_MotionTracking.MotionTrackerConnectionAction -= HandleTrackerConnectionChanged;
#endif
            _running = false;
            ClearTrackers();
        }

        public bool RefreshTrackers()
        {
            if (!_running || !IsSupported)
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
            _diagnostics.refreshCount++;
            _diagnostics.discoveryResult = PXR_MotionTracking.CheckMotionTrackerNumber(MotionTrackerNum.TWO);
            if (_diagnostics.discoveryResult != ApiSuccess)
            {
                SetError("请求 PICO Motion Tracker 列表失败，错误码 " + _diagnostics.discoveryResult + "。");
                return false;
            }

            _diagnostics.lastError = string.Empty;
            return true;
#else
            return false;
#endif
        }

        public bool TryGetTrackerId(int index, out string trackerId)
        {
            if (index < 0 || index >= _connectedCount)
            {
                trackerId = string.Empty;
                return false;
            }

            trackerId = _stableIds[index];
            return !string.IsNullOrEmpty(trackerId) && _connected[index];
        }

        public bool TryGetTrackerPose(string trackerId, out PicoObjectTrackerPose pose)
        {
            pose = default(PicoObjectTrackerPose);
            var index = FindTrackerIndex(trackerId);
            if (!_running || index < 0 || !_connected[index])
            {
                return false;
            }

            pose.trackerId = trackerId;
            pose.connected = true;
            pose.timestamp = Time.realtimeSinceStartupAsDouble;

#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
            var location = default(NativeMotionTrackerLocation);
            var valid = false;
            _diagnostics.poseResult = GetMotionTrackerLocationNative(
                _nativeIds[index],
                ref location,
                ref valid);
            if (_diagnostics.poseResult != ApiSuccess)
            {
                _diagnostics.failedPoseCount++;
                SetError("读取腕部传感器 Pose 失败，错误码 " + _diagnostics.poseResult + "。");
                return false;
            }

            pose.poseValid = valid;
            pose.position = location.pose.Position.ToVector3();
            pose.rotation = location.pose.Orientation.ToQuat();
            pose.velocity = ConvertMotionVector(location.linearVelocity);
            pose.angularVelocity = ConvertMotionVector(location.angularVelocity);
            if (valid)
            {
                _diagnostics.successfulPoseCount++;
                _diagnostics.lastError = string.Empty;
            }
            else
            {
                _diagnostics.failedPoseCount++;
            }

            return valid;
#else
            return false;
#endif
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopTracking();
            _disposed = true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR && !PICO_OPENXR_SDK
        private void HandleTrackerRequestCompleted(RequestMotionTrackerCompleteEventData data)
        {
            ClearTrackers();
            if ((int)data.result != ApiSuccess || data.trackerIds == null)
            {
                SetError("PICO Motion Tracker 扫描未成功，结果 " + data.result + "。");
                return;
            }

            var count = Mathf.Min((int)data.trackerCount, Mathf.Min(data.trackerIds.Length, MaximumTrackerCount));
            for (var i = 0; i < count; i++)
            {
                _nativeIds[i] = data.trackerIds[i];
                _stableIds[i] = data.trackerIds[i].ToString(CultureInfo.InvariantCulture);
                _connected[i] = true;
            }

            _connectedCount = count;
            _diagnostics.connectedTrackerCount = count;
            _diagnostics.lastError = string.Empty;
        }

        private void HandleTrackerConnectionChanged(long trackerId, int state)
        {
            var index = FindNativeTrackerIndex(trackerId);
            if (state == 0)
            {
                if (index >= 0)
                {
                    _connected[index] = false;
                    CompactTrackers();
                }

                return;
            }

            if (index >= 0)
            {
                _connected[index] = true;
                return;
            }

            if (_connectedCount < MaximumTrackerCount)
            {
                _nativeIds[_connectedCount] = trackerId;
                _stableIds[_connectedCount] = trackerId.ToString(CultureInfo.InvariantCulture);
                _connected[_connectedCount] = true;
                _connectedCount++;
                _diagnostics.connectedTrackerCount = _connectedCount;
            }
        }
#endif

        private static Vector3 ConvertMotionVector(Vector3f value)
        {
            return new Vector3(value.x, value.y, -value.z);
        }

        private int FindTrackerIndex(string trackerId)
        {
            if (string.IsNullOrEmpty(trackerId))
            {
                return -1;
            }

            for (var i = 0; i < _connectedCount; i++)
            {
                if (_connected[i] && string.Equals(_stableIds[i], trackerId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindNativeTrackerIndex(long trackerId)
        {
            for (var i = 0; i < _connectedCount; i++)
            {
                if (_nativeIds[i] == trackerId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void CompactTrackers()
        {
            var write = 0;
            for (var read = 0; read < _connectedCount; read++)
            {
                if (!_connected[read])
                {
                    continue;
                }

                if (write != read)
                {
                    _nativeIds[write] = _nativeIds[read];
                    _stableIds[write] = _stableIds[read];
                    _connected[write] = true;
                }

                write++;
            }

            for (var i = write; i < MaximumTrackerCount; i++)
            {
                _nativeIds[i] = 0L;
                _stableIds[i] = null;
                _connected[i] = false;
            }

            _connectedCount = write;
            _diagnostics.connectedTrackerCount = write;
        }

        private void ClearTrackers()
        {
            for (var i = 0; i < MaximumTrackerCount; i++)
            {
                _nativeIds[i] = 0L;
                _stableIds[i] = null;
                _connected[i] = false;
            }

            _connectedCount = 0;
            _diagnostics.connectedTrackerCount = 0;
        }

        private void SetError(string message)
        {
            _diagnostics.lastError = message ?? string.Empty;
        }
    }
}
