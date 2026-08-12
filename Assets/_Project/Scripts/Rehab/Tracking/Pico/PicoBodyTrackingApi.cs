using System;
using System.Runtime.InteropServices;
using Unity.XR.PXR;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class PicoBodyTrackingApi : IPicoBodyTrackingApi
    {
        private const string PicoPlatformLibrary = "PxrPlatform";

#if UNITY_ANDROID && !UNITY_EDITOR
        private BodyTrackingGetDataInfo _getDataInfo;
#endif
        private readonly IntPtr _nativeRoleData;
        private readonly int _roleDataCount;
        private readonly int _roleDataStride;
        private readonly int _roleOffset;
        private readonly int _localPoseOffset;
        private readonly int _timestampOffset;
        private readonly int _positionXOffset;
        private readonly int _positionYOffset;
        private readonly int _positionZOffset;
        private readonly int _rotationXOffset;
        private readonly int _rotationYOffset;
        private readonly int _rotationZOffset;
        private readonly int _rotationWOffset;
        private readonly int _velocityOffset;
        private readonly int _accelerationOffset;
        private readonly int _angularVelocityOffset;
        private bool _disposed;

#if UNITY_ANDROID && !UNITY_EDITOR
        // PICO Integration SDK 3.4.0's managed wrapper writes index 3 of fixed
        // three-element vectors, and IL2CPP allocates a new ByValArray while
        // marshaling BodyTrackingData back. Call the native entry point with a
        // reusable unmanaged inline-array buffer to avoid both defects.
        [DllImport(
            PicoPlatformLibrary,
            EntryPoint = "Pxr_GetBodyTrackingData",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBodyTrackingDataNative(
            ref BodyTrackingGetDataInfo getInfo,
            IntPtr data);
#endif

        public string LastError
        {
            get { return string.Empty; }
        }

        public PicoBodyTrackingApi()
        {
            _roleDataCount = (int)BodyTrackerRole.ROLE_NUM;
            _roleDataStride = Marshal.SizeOf(typeof(BodyTrackingRoleData));
            _roleOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "role").ToInt32();
            _localPoseOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "localPose").ToInt32();
            _timestampOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "TimeStamp").ToInt32();
            _positionXOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "PosX").ToInt32();
            _positionYOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "PosY").ToInt32();
            _positionZOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "PosZ").ToInt32();
            _rotationXOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "RotQx").ToInt32();
            _rotationYOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "RotQy").ToInt32();
            _rotationZOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "RotQz").ToInt32();
            _rotationWOffset = Marshal.OffsetOf(typeof(BodyTrackerTransPose), "RotQw").ToInt32();
            _velocityOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "velo").ToInt32();
            _accelerationOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "acce").ToInt32();
            _angularVelocityOffset = Marshal.OffsetOf(typeof(BodyTrackingRoleData), "wvelo").ToInt32();
            ValidateNativeLayout();
            _nativeRoleData = Marshal.AllocHGlobal(checked(_roleDataStride * _roleDataCount));
            ResetNativeRoles();
#if UNITY_ANDROID && !UNITY_EDITOR
            _getDataInfo.displayTime = 0L;
#endif
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
            // BodyTrackingData contains a ByValArray. IL2CPP's automatic
            // marshal-back allocates and substitutes a new 24-element array on
            // every call. Passing a reusable unmanaged buffer avoids both that
            // per-frame allocation and the stale pinned-array failure.
            ResetNativeRoles();
            var result = GetBodyTrackingDataNative(ref _getDataInfo, _nativeRoleData);
            if (result != 0)
            {
                return result;
            }

            for (var i = 0; i < _roleDataCount; i++)
            {
                var roleAddress = GetRoleAddress(i);
                PicoBodyJointData joint;
                if (!TryReadNativeJoint(roleAddress, out joint))
                {
                    continue;
                }

                target.SetJoint(joint);
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

            if (_nativeRoleData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_nativeRoleData);
            }

            _disposed = true;
        }

        private void ResetNativeRoles()
        {
            for (var i = 0; i < _roleDataCount; i++)
            {
                Marshal.WriteInt32(
                    GetRoleAddress(i),
                    _roleOffset,
                    (int)BodyTrackerRole.NONE_ROLE);
            }
        }

        private void ValidateNativeLayout()
        {
            if (_roleDataCount != (int)BodyTrackerRole.NONE_ROLE)
            {
                throw new InvalidOperationException("PICO body role count does not match the native inline-array length.");
            }

            ValidateField(_roleOffset, sizeof(int), "role");
            ValidateField(
                _localPoseOffset,
                Marshal.SizeOf(typeof(BodyTrackerTransPose)),
                "localPose");
            ValidateField(_velocityOffset, sizeof(long) * 3, "velo");
            ValidateField(_accelerationOffset, sizeof(long) * 3, "acce");
            ValidateField(_angularVelocityOffset, sizeof(long) * 3, "wvelo");
        }

        private void ValidateField(int offset, int size, string fieldName)
        {
            if (offset < 0 || size < 0 || offset > _roleDataStride - size)
            {
                throw new InvalidOperationException(
                    "PICO native body role layout is invalid for field " + fieldName + ".");
            }
        }

        private IntPtr GetRoleAddress(int roleIndex)
        {
            return IntPtr.Add(_nativeRoleData, roleIndex * _roleDataStride);
        }

        internal bool TryReadNativeJoint(IntPtr roleAddress, out PicoBodyJointData joint)
        {
            joint = default(PicoBodyJointData);
            if (roleAddress == IntPtr.Zero)
            {
                return false;
            }

            var role = (BodyTrackerRole)Marshal.ReadInt32(roleAddress, _roleOffset);
            var roleIndex = (int)role;
            if (role == BodyTrackerRole.NONE_ROLE || roleIndex < 0 || roleIndex >= _roleDataCount)
            {
                return false;
            }

            // Native localPose is relative to the tracking/XR origin. Convert
            // all fields to Unity handedness here; globalPose is intentionally ignored.
            var nativePose = ReadLocalPose(roleAddress);
            joint = new PicoBodyJointData
            {
                valid = true,
                role = role,
                timestamp = nativePose.TimeStamp,
                position = ConvertPositionToUnity(nativePose),
                rotation = ConvertRotationToUnity(nativePose),
                velocity = ConvertMotionVectorToUnity(ReadVector(roleAddress, _velocityOffset)),
                acceleration = ConvertMotionVectorToUnity(ReadVector(roleAddress, _accelerationOffset)),
                angularVelocity = ConvertMotionVectorToUnity(ReadVector(roleAddress, _angularVelocityOffset))
            };
            return true;
        }

        private BodyTrackerTransPose ReadLocalPose(IntPtr roleAddress)
        {
            var poseAddress = IntPtr.Add(roleAddress, _localPoseOffset);
            return new BodyTrackerTransPose
            {
                TimeStamp = Marshal.ReadInt64(poseAddress, _timestampOffset),
                PosX = ReadDouble(poseAddress, _positionXOffset),
                PosY = ReadDouble(poseAddress, _positionYOffset),
                PosZ = ReadDouble(poseAddress, _positionZOffset),
                RotQx = ReadDouble(poseAddress, _rotationXOffset),
                RotQy = ReadDouble(poseAddress, _rotationYOffset),
                RotQz = ReadDouble(poseAddress, _rotationZOffset),
                RotQw = ReadDouble(poseAddress, _rotationWOffset)
            };
        }

        private static Vector3 ReadVector(IntPtr roleAddress, int fieldOffset)
        {
            var vectorAddress = IntPtr.Add(roleAddress, fieldOffset);
            return new Vector3(
                ReadDouble(vectorAddress, 0),
                ReadDouble(vectorAddress, sizeof(long)),
                ReadDouble(vectorAddress, sizeof(long) * 2));
        }

        internal static Vector3 ConvertPositionToUnity(BodyTrackerTransPose pose)
        {
            return new Vector3(
                (float)pose.PosX,
                (float)pose.PosY,
                -(float)pose.PosZ);
        }

        internal static Quaternion ConvertRotationToUnity(BodyTrackerTransPose pose)
        {
            return new Quaternion(
                (float)pose.RotQx,
                (float)pose.RotQy,
                -(float)pose.RotQz,
                -(float)pose.RotQw);
        }

        internal static Vector3 ConvertMotionVectorToUnity(Vector3 vector)
        {
            return new Vector3(vector.x, vector.y, -vector.z);
        }

        private static float ReadDouble(IntPtr address, int offset)
        {
            var bits = Marshal.ReadInt64(address, offset);
            return (float)BitConverter.Int64BitsToDouble(bits);
        }
    }
}
