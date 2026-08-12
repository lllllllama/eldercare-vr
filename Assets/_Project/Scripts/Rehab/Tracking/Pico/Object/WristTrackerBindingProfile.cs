using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [Serializable]
    public sealed class WristTrackerBindingProfile
    {
        public string leftTrackerId = string.Empty;
        public string rightTrackerId = string.Empty;

        public bool HasBinding
        {
            get
            {
                return !string.IsNullOrEmpty(leftTrackerId) &&
                       !string.IsNullOrEmpty(rightTrackerId) &&
                       !string.Equals(leftTrackerId, rightTrackerId, StringComparison.Ordinal);
            }
        }

        public void Clear()
        {
            leftTrackerId = string.Empty;
            rightTrackerId = string.Empty;
        }
    }

    public interface IWristTrackerBindingStore
    {
        void Load(WristTrackerBindingProfile target);
        void Save(WristTrackerBindingProfile source);
        void Clear();
    }

    public sealed class PlayerPrefsWristTrackerBindingStore : IWristTrackerBindingStore
    {
        private const string LeftKey = "ElderCare.WristTracking.LeftTrackerId";
        private const string RightKey = "ElderCare.WristTracking.RightTrackerId";

        public void Load(WristTrackerBindingProfile target)
        {
            if (target == null) return;
            target.leftTrackerId = PlayerPrefs.GetString(LeftKey, string.Empty);
            target.rightTrackerId = PlayerPrefs.GetString(RightKey, string.Empty);
        }

        public void Save(WristTrackerBindingProfile source)
        {
            if (source == null) return;
            PlayerPrefs.SetString(LeftKey, source.leftTrackerId ?? string.Empty);
            PlayerPrefs.SetString(RightKey, source.rightTrackerId ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(LeftKey);
            PlayerPrefs.DeleteKey(RightKey);
            PlayerPrefs.Save();
        }
    }
}
