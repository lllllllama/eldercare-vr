using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [Serializable]
    public sealed class WristTrackerCalibrationProfile
    {
        public Vector3 leftPositionOffset;
        public Quaternion leftRotationOffset = Quaternion.identity;
        public Vector3 rightPositionOffset;
        public Quaternion rightRotationOffset = Quaternion.identity;
        public bool leftReady;
        public bool rightReady;
        public bool identityCalibrationExplicitlyAccepted;

        public bool IsReady
        {
            get
            {
                return identityCalibrationExplicitlyAccepted ||
                       (leftReady && rightReady);
            }
        }
    }

    public interface IWristTrackerCalibrationStore
    {
        void Load(WristTrackerCalibrationProfile target);
        void Save(WristTrackerCalibrationProfile source);
        void Clear();
    }

    public sealed class PlayerPrefsWristTrackerCalibrationStore : IWristTrackerCalibrationStore
    {
        private const string Prefix = "ElderCare.WristTracking.Calibration.";

        public void Load(WristTrackerCalibrationProfile target)
        {
            if (target == null) return;
            target.leftPositionOffset = LoadVector("LeftPosition");
            target.leftRotationOffset = LoadQuaternion("LeftRotation");
            target.rightPositionOffset = LoadVector("RightPosition");
            target.rightRotationOffset = LoadQuaternion("RightRotation");
            target.leftReady = PlayerPrefs.GetInt(Prefix + "LeftReady", 0) != 0;
            target.rightReady = PlayerPrefs.GetInt(Prefix + "RightReady", 0) != 0;
            target.identityCalibrationExplicitlyAccepted = PlayerPrefs.GetInt(Prefix + "IdentityAccepted", 0) != 0;
        }

        public void Save(WristTrackerCalibrationProfile source)
        {
            if (source == null) return;
            SaveVector("LeftPosition", source.leftPositionOffset);
            SaveQuaternion("LeftRotation", source.leftRotationOffset);
            SaveVector("RightPosition", source.rightPositionOffset);
            SaveQuaternion("RightRotation", source.rightRotationOffset);
            PlayerPrefs.SetInt(Prefix + "LeftReady", source.leftReady ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "RightReady", source.rightReady ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "IdentityAccepted", source.identityCalibrationExplicitlyAccepted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            var keys = new[]
            {
                "LeftPosition.x", "LeftPosition.y", "LeftPosition.z",
                "LeftRotation.x", "LeftRotation.y", "LeftRotation.z", "LeftRotation.w",
                "RightPosition.x", "RightPosition.y", "RightPosition.z",
                "RightRotation.x", "RightRotation.y", "RightRotation.z", "RightRotation.w",
                "LeftReady", "RightReady", "IdentityAccepted"
            };
            for (var i = 0; i < keys.Length; i++) PlayerPrefs.DeleteKey(Prefix + keys[i]);
            PlayerPrefs.Save();
        }

        private static Vector3 LoadVector(string key)
        {
            return new Vector3(
                PlayerPrefs.GetFloat(Prefix + key + ".x", 0f),
                PlayerPrefs.GetFloat(Prefix + key + ".y", 0f),
                PlayerPrefs.GetFloat(Prefix + key + ".z", 0f));
        }

        private static Quaternion LoadQuaternion(string key)
        {
            var value = new Quaternion(
                PlayerPrefs.GetFloat(Prefix + key + ".x", 0f),
                PlayerPrefs.GetFloat(Prefix + key + ".y", 0f),
                PlayerPrefs.GetFloat(Prefix + key + ".z", 0f),
                PlayerPrefs.GetFloat(Prefix + key + ".w", 1f));
            var magnitudeSquared = value.x * value.x + value.y * value.y +
                                   value.z * value.z + value.w * value.w;
            return magnitudeSquared > 0.0001f ? Quaternion.Normalize(value) : Quaternion.identity;
        }

        private static void SaveVector(string key, Vector3 value)
        {
            PlayerPrefs.SetFloat(Prefix + key + ".x", value.x);
            PlayerPrefs.SetFloat(Prefix + key + ".y", value.y);
            PlayerPrefs.SetFloat(Prefix + key + ".z", value.z);
        }

        private static void SaveQuaternion(string key, Quaternion value)
        {
            PlayerPrefs.SetFloat(Prefix + key + ".x", value.x);
            PlayerPrefs.SetFloat(Prefix + key + ".y", value.y);
            PlayerPrefs.SetFloat(Prefix + key + ".z", value.z);
            PlayerPrefs.SetFloat(Prefix + key + ".w", value.w);
        }
    }
}
