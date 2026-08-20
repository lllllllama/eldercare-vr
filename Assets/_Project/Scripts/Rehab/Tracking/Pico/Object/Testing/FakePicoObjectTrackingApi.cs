using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking.Testing
{
    public sealed class FakePicoObjectTrackingApi : IPicoObjectTrackingApi
    {
        private const int Capacity = 3;
        private readonly PicoObjectTrackerPose[] _poses = new PicoObjectTrackerPose[Capacity];
        private readonly bool[] _occupied = new bool[Capacity];
        private readonly PicoObjectTrackingDiagnostics _diagnostics = new PicoObjectTrackingDiagnostics();
        private readonly TrackerSetupRequestGate _setupRequestGate = new TrackerSetupRequestGate();
        private bool _running;

        public bool Supported = true;
        public bool StartSucceeds = true;
        public bool SetupRequestSucceeds = true;
        public int StartCount { get; private set; }

        public bool IsSupported { get { return Supported; } }
        public bool IsRunning { get { return _running; } }
        public int ConnectedTrackerCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Capacity; i++) if (_occupied[i] && _poses[i].connected) count++;
                return count;
            }
        }
        public string LastError { get; set; }
        public PicoObjectTrackingDiagnostics Diagnostics { get { return _diagnostics; } }

        public bool StartTracking()
        {
            StartCount++;
            _running = Supported && StartSucceeds;
            return _running;
        }

        public void StopTracking()
        {
            _running = false;
            CompleteSetupRequest();
        }

        public bool RequestTrackerSetup()
        {
            if (!_running || !Supported || !_setupRequestGate.TryBegin()) return false;
            _diagnostics.setupRequestInFlight = true;
            _diagnostics.setupRequestCount++;
            _diagnostics.setupRequestResult = SetupRequestSucceeds ? 0 : -1;
            if (SetupRequestSucceeds) return true;
            CompleteSetupRequest();
            return false;
        }

        public void ReconcileAfterApplicationResume()
        {
            CompleteSetupRequest();
        }

        public void CompleteSetupRequest()
        {
            _setupRequestGate.Complete();
            _diagnostics.setupRequestInFlight = false;
        }

        public bool TryGetTrackerId(int index, out string trackerId)
        {
            var seen = 0;
            for (var i = 0; i < Capacity; i++)
            {
                if (!_occupied[i] || !_poses[i].connected) continue;
                if (seen++ != index) continue;
                trackerId = _poses[i].trackerId;
                return true;
            }

            trackerId = string.Empty;
            return false;
        }

        public bool TryGetTrackerPose(string trackerId, out PicoObjectTrackerPose pose)
        {
            for (var i = 0; i < Capacity; i++)
            {
                if (_occupied[i] && string.Equals(_poses[i].trackerId, trackerId, StringComparison.Ordinal))
                {
                    pose = _poses[i];
                    pose.timestamp = Time.realtimeSinceStartupAsDouble;
                    return _running && pose.connected && pose.poseValid;
                }
            }

            pose = default(PicoObjectTrackerPose);
            return false;
        }

        public void SetTracker(
            int slot,
            string trackerId,
            Vector3 position,
            Quaternion rotation,
            bool connected = true,
            bool poseValid = true)
        {
            if (slot < 0 || slot >= Capacity) throw new ArgumentOutOfRangeException("slot");
            _occupied[slot] = true;
            _poses[slot] = new PicoObjectTrackerPose
            {
                trackerId = trackerId,
                connected = connected,
                poseValid = poseValid,
                position = position,
                rotation = rotation,
                timestamp = Time.realtimeSinceStartupAsDouble
            };
        }

        public void SetPosition(int slot, Vector3 position)
        {
            if (slot < 0 || slot >= Capacity || !_occupied[slot]) return;
            var pose = _poses[slot];
            pose.position = position;
            _poses[slot] = pose;
        }

        public void SetPoseValid(int slot, bool valid)
        {
            if (slot < 0 || slot >= Capacity || !_occupied[slot]) return;
            var pose = _poses[slot];
            pose.poseValid = valid;
            _poses[slot] = pose;
        }

        public void SetConnected(int slot, bool connected)
        {
            if (slot < 0 || slot >= Capacity || !_occupied[slot]) return;
            var pose = _poses[slot];
            pose.connected = connected;
            _poses[slot] = pose;
        }

        public void ClearTrackers()
        {
            for (var i = 0; i < Capacity; i++)
            {
                _occupied[i] = false;
                _poses[i] = default(PicoObjectTrackerPose);
            }
        }

        public void Dispose() { StopTracking(); }
    }

    public sealed class MemoryWristTrackerBindingStore : IWristTrackerBindingStore
    {
        public string left = string.Empty;
        public string right = string.Empty;
        public void Load(WristTrackerBindingProfile target) { target.leftTrackerId = left; target.rightTrackerId = right; }
        public void Save(WristTrackerBindingProfile source) { left = source.leftTrackerId; right = source.rightTrackerId; }
        public void Clear() { left = string.Empty; right = string.Empty; }
    }

    public sealed class MemoryWristTrackerCalibrationStore : IWristTrackerCalibrationStore
    {
        public readonly WristTrackerCalibrationProfile saved = new WristTrackerCalibrationProfile();
        public void Load(WristTrackerCalibrationProfile target) { Copy(saved, target); }
        public void Save(WristTrackerCalibrationProfile source) { Copy(source, saved); }
        public void Clear() { Copy(new WristTrackerCalibrationProfile(), saved); }
        private static void Copy(WristTrackerCalibrationProfile source, WristTrackerCalibrationProfile target)
        {
            target.leftPositionOffset = source.leftPositionOffset;
            target.leftRotationOffset = source.leftRotationOffset;
            target.rightPositionOffset = source.rightPositionOffset;
            target.rightRotationOffset = source.rightRotationOffset;
            target.leftReady = source.leftReady;
            target.rightReady = source.rightReady;
            target.identityCalibrationExplicitlyAccepted = source.identityCalibrationExplicitlyAccepted;
        }
    }
}
