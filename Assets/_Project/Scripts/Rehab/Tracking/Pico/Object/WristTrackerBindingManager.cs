using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    public sealed class WristTrackerBindingManager
    {
        private const int MaximumTrackerCount = 3;

        private readonly IPicoObjectTrackingApi _api;
        private readonly IWristTrackerBindingStore _store;
        private readonly WristTrackerBindingProfile _profile;
        private readonly string[] _sampleIds = new string[MaximumTrackerCount];
        private readonly Vector3[] _lastPositions = new Vector3[MaximumTrackerCount];
        private readonly float[] _travelMeters = new float[MaximumTrackerCount];
        private readonly int[] _validSamples = new int[MaximumTrackerCount];
        private readonly bool[] _hasLastPosition = new bool[MaximumTrackerCount];

        private string _pendingLeftId = string.Empty;
        private float _sampleElapsed;
        private int _sampleTrackerCount;
        private WristTrackerSetupState _state;
        private string _statusMessage = string.Empty;
        private string _lastPendingLeftId = string.Empty;
        private string _lastResultMessage = string.Empty;
        private bool _lastResultFailed;
        private bool _isPreparingSample;
        private float _preparationElapsed;
        private string _activeSamplePrompt = string.Empty;

        // Give a VR user time to read the new left/right instruction before the
        // measurement starts, then leave a generous movement window. Binding still
        // requires one clearly dominant stable ID; these values only improve tolerance.
        public float preparationSeconds = 0.75f;
        public float sampleWindowSeconds = 2.5f;
        public float minimumTravelMeters = 0.06f;
        public float winnerRatio = 1.35f;
        public int minimumValidSamples = 8;

        public WristTrackerBindingProfile Profile { get { return _profile; } }
        public WristTrackerSetupState State { get { return _state; } }
        public string StatusMessage { get { return _statusMessage; } }
        public string PendingLeftTrackerId { get { return _pendingLeftId; } }
        public string LastPendingLeftTrackerId { get { return _lastPendingLeftId; } }
        public string LastResultMessage { get { return _lastResultMessage; } }
        public bool LastResultFailed { get { return _lastResultFailed; } }
        public float SampleElapsedSeconds { get { return _sampleElapsed; } }
        public float SampleDurationSeconds { get { return Mathf.Max(0.25f, sampleWindowSeconds); } }
        public bool IsPreparingSample { get { return _isPreparingSample; } }
        public float PreparationRemainingSeconds
        {
            get { return Mathf.Max(0f, Mathf.Max(0f, preparationSeconds) - _preparationElapsed); }
        }
        public int SampleTrackerCount { get { return _sampleTrackerCount; } }
        public string CurrentCandidateTrackerId
        {
            get
            {
                int winner;
                float best;
                float second;
                GetMovementRanking(out winner, out best, out second);
                return winner >= 0 && _validSamples[winner] > 0 ? _sampleIds[winner] : string.Empty;
            }
        }
        public float CurrentBestTravelMeters
        {
            get
            {
                int winner;
                float best;
                float second;
                GetMovementRanking(out winner, out best, out second);
                return best;
            }
        }
        public float CurrentSecondTravelMeters
        {
            get
            {
                int winner;
                float best;
                float second;
                GetMovementRanking(out winner, out best, out second);
                return second;
            }
        }
        public bool HasSavedBinding { get { return _profile.HasBinding; } }
        public bool IsBindingInProgress
        {
            get
            {
                return _state == WristTrackerSetupState.BindingLeft ||
                       _state == WristTrackerSetupState.BindingRight;
            }
        }

        public bool IsVerificationInProgress
        {
            get { return _state == WristTrackerSetupState.VerifyingLeft; }
        }

        public bool IsBindingReady
        {
            get
            {
                return _profile.HasBinding &&
                       IsTrackerPresent(_profile.leftTrackerId) &&
                       IsTrackerPresent(_profile.rightTrackerId);
            }
        }

        public WristTrackerBindingManager(
            IPicoObjectTrackingApi api,
            IWristTrackerBindingStore store = null,
            WristTrackerBindingProfile profile = null)
        {
            _api = api ?? throw new ArgumentNullException("api");
            _store = store ?? new PlayerPrefsWristTrackerBindingStore();
            _profile = profile ?? new WristTrackerBindingProfile();
            _store.Load(_profile);
            RefreshIdleState();
        }

        public bool BeginBinding()
        {
            if (_api.ConnectedTrackerCount < 2)
            {
                RefreshIdleState();
                return false;
            }

            _pendingLeftId = string.Empty;
            _lastPendingLeftId = string.Empty;
            ClearLastResult();
            BeginSample(WristTrackerSetupState.BindingLeft, "请轻轻移动左手腕");
            return true;
        }

        public void CancelBinding()
        {
            _pendingLeftId = string.Empty;
            _lastPendingLeftId = string.Empty;
            ClearLastResult();
            RefreshIdleState();
        }

        public void ClearBinding()
        {
            _pendingLeftId = string.Empty;
            _lastPendingLeftId = string.Empty;
            ClearLastResult();
            _profile.Clear();
            _store.Clear();
            RefreshIdleState();
        }

        public bool BeginQuickVerification()
        {
            if (!IsBindingReady)
            {
                _state = WristTrackerSetupState.BindingRequired;
                _statusMessage = "请先完成左右腕匹配。";
                return false;
            }

            ClearLastResult();
            BeginSample(WristTrackerSetupState.VerifyingLeft, "请轻轻抬一下左手");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsBindingInProgress && !IsVerificationInProgress)
            {
                RefreshIdleState();
                return;
            }

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (_isPreparingSample)
            {
                _preparationElapsed += safeDeltaTime;
                if (_preparationElapsed < Mathf.Max(0f, preparationSeconds)) return;

                _isPreparingSample = false;
                _sampleElapsed = 0f;
                _statusMessage = _activeSamplePrompt;
            }

            _sampleElapsed += safeDeltaTime;
            AccumulateMotion();
            if (_sampleElapsed < Mathf.Max(0.25f, sampleWindowSeconds))
            {
                return;
            }

            if (_state == WristTrackerSetupState.BindingLeft)
            {
                CompleteLeftBindingSample();
            }
            else if (_state == WristTrackerSetupState.BindingRight)
            {
                CompleteRightBindingSample();
            }
            else
            {
                CompleteVerificationSample();
            }
        }

        public void RefreshIdleState()
        {
            if (IsBindingInProgress || IsVerificationInProgress)
            {
                return;
            }

            if (!_api.IsSupported)
            {
                _state = WristTrackerSetupState.Unsupported;
                _statusMessage = "当前设备不支持腕部传感器。";
            }
            else if (_api.ConnectedTrackerCount <= 0)
            {
                _state = WristTrackerSetupState.NoTracker;
                _statusMessage = "未检测到腕部传感器。";
            }
            else if (_api.ConnectedTrackerCount == 1)
            {
                _state = WristTrackerSetupState.OneTrackerOnly;
                _statusMessage = "仅检测到一个腕部传感器。";
            }
            // Keep a concrete binding failure visible until the user starts a new
            // operation. Device loss still takes priority over an old failure.
            else if (_lastResultFailed && !IsBindingReady && !string.IsNullOrEmpty(_lastResultMessage))
            {
                _state = WristTrackerSetupState.BindingRequired;
                _statusMessage = _lastResultMessage;
            }
            else if (!IsBindingReady)
            {
                _state = WristTrackerSetupState.BindingRequired;
                _statusMessage = _profile.HasBinding
                    ? "已保存的腕部传感器未全部连接。"
                    : "需要完成左右腕匹配。";
            }
            else
            {
                _state = WristTrackerSetupState.CalibrationRequired;
                _statusMessage = "左右腕匹配已就绪。";
            }
        }

        private void BeginSample(WristTrackerSetupState state, string message)
        {
            _state = state;
            _activeSamplePrompt = message;
            _preparationElapsed = 0f;
            _isPreparingSample = preparationSeconds > 0f;
            _statusMessage = _isPreparingSample ? BuildPreparationMessage(state) : message;
            _sampleElapsed = 0f;
            _sampleTrackerCount = 0;
            for (var i = 0; i < MaximumTrackerCount; i++)
            {
                _sampleIds[i] = null;
                _lastPositions[i] = Vector3.zero;
                _travelMeters[i] = 0f;
                _validSamples[i] = 0;
                _hasLastPosition[i] = false;
            }

            if (state == WristTrackerSetupState.VerifyingLeft)
            {
                AddSampleId(_profile.leftTrackerId);
                AddSampleId(_profile.rightTrackerId);
                return;
            }

            for (var i = 0; i < _api.ConnectedTrackerCount && i < MaximumTrackerCount; i++)
            {
                string id;
                if (_api.TryGetTrackerId(i, out id))
                {
                    AddSampleId(id);
                }
            }
        }

        private void AddSampleId(string id)
        {
            if (string.IsNullOrEmpty(id) || _sampleTrackerCount >= MaximumTrackerCount)
            {
                return;
            }

            for (var i = 0; i < _sampleTrackerCount; i++)
            {
                if (string.Equals(_sampleIds[i], id, StringComparison.Ordinal))
                {
                    return;
                }
            }

            _sampleIds[_sampleTrackerCount++] = id;
        }

        private void AccumulateMotion()
        {
            for (var i = 0; i < _sampleTrackerCount; i++)
            {
                PicoObjectTrackerPose pose;
                if (!_api.TryGetTrackerPose(_sampleIds[i], out pose) || !pose.connected || !pose.poseValid)
                {
                    continue;
                }

                if (_hasLastPosition[i])
                {
                    _travelMeters[i] += Vector3.Distance(_lastPositions[i], pose.position);
                }

                _lastPositions[i] = pose.position;
                _hasLastPosition[i] = true;
                _validSamples[i]++;
            }
        }

        private void CompleteLeftBindingSample()
        {
            int winner;
            if (!TryGetConfidentWinner(out winner))
            {
                FailBinding(BuildMovementFailureMessage("左腕"));
                return;
            }

            _pendingLeftId = _sampleIds[winner];
            _lastPendingLeftId = _pendingLeftId;
            BeginSample(WristTrackerSetupState.BindingRight, "请轻轻移动右手腕");
        }

        private void CompleteRightBindingSample()
        {
            int winner;
            if (!TryGetConfidentWinner(out winner))
            {
                FailBinding(BuildMovementFailureMessage("右腕"));
                return;
            }

            var rightId = _sampleIds[winner];
            if (string.Equals(rightId, _pendingLeftId, StringComparison.Ordinal))
            {
                FailBinding("检测到的仍是左腕传感器，匹配未保存。请重新开始。");
                return;
            }

            _profile.leftTrackerId = _pendingLeftId;
            _profile.rightTrackerId = rightId;
            _store.Save(_profile);
            _pendingLeftId = string.Empty;
            _state = WristTrackerSetupState.CalibrationRequired;
            _statusMessage = "左右腕匹配完成，请进行腕部校准。";
            SetLastResult(_statusMessage, false);
        }

        private void CompleteVerificationSample()
        {
            int winner;
            if (!TryGetConfidentWinner(out winner))
            {
                _state = WristTrackerSetupState.CalibrationRequired;
                _statusMessage = "未检测到明确移动，请重新进行佩戴测试。";
                SetLastResult(_statusMessage, true);
                return;
            }

            var movedId = _sampleIds[winner];
            _state = WristTrackerSetupState.CalibrationRequired;
            _statusMessage = string.Equals(movedId, _profile.leftTrackerId, StringComparison.Ordinal)
                ? "左腕佩戴正常。"
                : "检测到左右腕传感器可能佩戴反了，请检查腕带。";
            SetLastResult(_statusMessage, false);
        }

        private bool TryGetConfidentWinner(out int winner)
        {
            float best;
            float second;
            GetMovementRanking(out winner, out best, out second);
            if (winner < 0 || _validSamples[winner] < Mathf.Max(2, minimumValidSamples))
            {
                return false;
            }

            return best >= Mathf.Max(0.01f, minimumTravelMeters) &&
                   best >= Mathf.Max(0.001f, second) * Mathf.Max(1.05f, winnerRatio);
        }

        private void GetMovementRanking(out int winner, out float best, out float second)
        {
            winner = -1;
            best = 0f;
            second = 0f;
            for (var i = 0; i < _sampleTrackerCount; i++)
            {
                var distance = _travelMeters[i];
                if (winner < 0 || distance > best)
                {
                    second = best;
                    best = distance;
                    winner = i;
                }
                else if (distance > second)
                {
                    second = distance;
                }
            }
        }

        private void FailBinding(string message)
        {
            if (!string.IsNullOrEmpty(_pendingLeftId)) _lastPendingLeftId = _pendingLeftId;
            _pendingLeftId = string.Empty;
            _state = WristTrackerSetupState.BindingRequired;
            _statusMessage = message;
            SetLastResult(message, true);
        }

        public bool TryGetSampleDiagnostics(int index, out WristBindingSampleDiagnostics diagnostics)
        {
            diagnostics = default(WristBindingSampleDiagnostics);
            if (index < 0 || index >= _sampleTrackerCount) return false;

            diagnostics.trackerId = _sampleIds[index];
            diagnostics.position = _lastPositions[index];
            diagnostics.poseValid = _hasLastPosition[index];
            diagnostics.validSamples = _validSamples[index];
            diagnostics.travelMeters = _travelMeters[index];
            return !string.IsNullOrEmpty(diagnostics.trackerId);
        }

        private string BuildMovementFailureMessage(string wristLabel)
        {
            int winner;
            float best;
            float second;
            GetMovementRanking(out winner, out best, out second);
            var requiredSamples = Mathf.Max(2, minimumValidSamples);
            var winnerSamples = winner >= 0 ? _validSamples[winner] : 0;
            if (winner < 0 || winnerSamples < requiredSamples)
            {
                return wristLabel + "匹配失败：有效 Pose 样本不足（" + winnerSamples + "/" + requiredSamples + " 帧）。";
            }

            var requiredTravel = Mathf.Max(0.01f, minimumTravelMeters);
            if (best < requiredTravel)
            {
                return wristLabel + "匹配失败：移动距离不足（" + best.ToString("F3") + "m / " + requiredTravel.ToString("F3") + "m）。";
            }

            return wristLabel + "匹配失败：两只传感器同时移动，无法明确区分（" +
                   best.ToString("F3") + "m / " + second.ToString("F3") + "m）。";
        }

        private void SetLastResult(string message, bool failed)
        {
            _lastResultMessage = message ?? string.Empty;
            _lastResultFailed = failed;
        }

        private void ClearLastResult()
        {
            _lastResultMessage = string.Empty;
            _lastResultFailed = false;
        }

        private static string BuildPreparationMessage(WristTrackerSetupState state)
        {
            if (state == WristTrackerSetupState.BindingRight)
                return "已识别左腕，准备识别右腕，请先保持双手静止";
            if (state == WristTrackerSetupState.VerifyingLeft)
                return "准备进行佩戴测试，请先保持双手静止";
            return "准备识别左腕，请先保持双手静止";
        }

        private bool IsTrackerPresent(string trackerId)
        {
            if (string.IsNullOrEmpty(trackerId))
            {
                return false;
            }

            for (var i = 0; i < _api.ConnectedTrackerCount; i++)
            {
                string connectedId;
                if (_api.TryGetTrackerId(i, out connectedId) &&
                    string.Equals(connectedId, trackerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
