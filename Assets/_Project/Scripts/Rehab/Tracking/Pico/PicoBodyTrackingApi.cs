using System;
using System.Runtime.InteropServices;
using Unity.XR.PXR;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class PicoBodyTrackingApi : IPicoBodyTrackingApi
    {
        private readonly BodyTrackingRoleData[] _sdkRoleData;
        private BodyTrackingData _sdkData;
        private BodyTrackingGetDataInfo _getDataInfo;
        private GCHandle _roleDataHandle;
        private readonly int _roleDataStride;
        private readonly int _velocityOffset;
        private readonly int _accelerationOffset;
        private readonly int _angularVelocityOffset;
        private bool _disposed;

        public string LastError
        {
            get { return string.Empty; }
        }

        public PicoBodyTrackingApi()
        {
            _sdkRoleData = new BodyTrackingRoleData[(int)BodyTrackerRole.ROLE_NUM];
            _sdkData.roleDatas = _sdkRoleData;
            _roleDataStride = Marshal.SizeOf(typeof(BodyTrackingRoleData));
            _velocityOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "velo").ToInt32();
            _accelerationOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "acce").ToInt32();
            _angularVelocityOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "wvelo").ToInt32();
            _roleDataHandle = GCHandle.Alloc(_sdkRoleData, GCHandleType.Pinned);
            _getDataInfo.displayTime = 0L;
        }

        public int GetBodyTrackingSupported(out bool supported)
        {
            supported = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            return PXR_MotionTracking.GetBodyTrackingSupported(ref supported);
#else
            return 0;
#endif
        }

        public int StartBodyTracking()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return PXR_MotionTracking.StartBodyTracking(
                BodyJointSet.BODY_JOINT_SET_BODY_FULL_START,
                default(BodyTrackingBoneLength));
#else
            return -1;
#endif
        }

        public int StopBodyTracking()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return PXR_MotionTracking.StopBodyTracking();
#else
            return -1;
#endif
        }

        public int GetBodyTrackingState(out PicoBodyTrackingApiState state)
        {
            state = default(PicoBodyTrackingApiState);
#if UNITY_ANDROID && !UNITY_EDITOR
            var isTracking = false;
            var sdkStatus = default(BodyTrackingStatus);
            var result = PXR_MotionTracking.GetBodyTrackingState(ref isTracking, ref sdkStatus);
            state.isTracking = isTracking;
            state.statusCode = sdkStatus.stateCode;
            state.message = sdkStatus.message;
            return result;
#else
            return -1;
#endif
        }

        public int GetBodyTrackingData(PicoBodyTrackingFrame target)
        {
            if (target == null || _disposed)
            {
                return -1;
            }

            target.Clear();
#if UNITY_ANDROID && !UNITY_EDITOR
            var result = PXR_MotionTracking.GetBodyTrackingData(ref _getDataInfo, ref _sdkData);
            if (result != 0)
            {
                return result;
            }

            EnsurePinnedSdkArray();
            for (var i = 0; i < _sdkRoleData.Length; i++)
            {
                var source = _sdkRoleData[i];
                if (source.role == BodyTrackerRole.NONE_ROLE)
                {
                    continue;
                }

                // The installed PICO SDK has already converted localPose to Unity handedness.
                // localPose is relative to the tracking/XR origin; globalPose is intentionally ignored.
                target.SetJoint(new PicoBodyJointData
                {
                    valid = true,
                    role = source.role,
                    timestamp = source.localPose.TimeStamp,
                    position = new Vector3(
                        (float)source.localPose.PosX,
                        (float)source.localPose.PosY,
                        (float)source.localPose.PosZ),
                    rotation = new Quaternion(
                        (float)source.localPose.RotQx,
                        (float)source.localPose.RotQy,
                        (float)source.localPose.RotQz,
                        (float)source.localPose.RotQw),
                    velocity = ReadVector(i, _velocityOffset),
                    acceleration = ReadVector(i, _accelerationOffset),
                    angularVelocity = ReadVector(i, _angularVelocityOffset)
                });
            }

            return result;
#else
            return -1;
#endif
        }

        public int StartMotionTrackerCalibApp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return PXR_MotionTracking.StartMotionTrackerCalibApp();
#else
            return -1;
#endif
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_roleDataHandle.IsAllocated)
            {
                _roleDataHandle.Free();
            }

            _disposed = true;
        }

        private void EnsurePinnedSdkArray()
        {
            if (ReferenceEquals(_sdkData.roleDatas, _sdkRoleData))
            {
                return;
            }

            throw new InvalidOperationException("PICO SDK replaced the reusable BodyTrackingData joint array.");
        }

        private Vector3 ReadVector(int roleIndex, int fieldOffset)
        {
            var roleAddress = IntPtr.Add(
                _roleDataHandle.AddrOfPinnedObject(),
                roleIndex * _roleDataStride + fieldOffset);
            return new Vector3(
                ReadDouble(roleAddress, 0),
                ReadDouble(roleAddress, sizeof(long)),
                ReadDouble(roleAddress, sizeof(long) * 2));
        }

        private static float ReadDouble(IntPtr address, int offset)
        {
            var bits = Marshal.ReadInt64(address, offset);
            return (float)BitConverter.Int64BitsToDouble(bits);
        }
    }
}
