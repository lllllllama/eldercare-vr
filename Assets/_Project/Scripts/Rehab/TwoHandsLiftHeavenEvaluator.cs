using System;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    public enum TwoHandsLiftHeavenPhase
    {
        WaitingForRise,
        Rising,
        HoldingOverhead,
        Lowering,
        Completed
    }

    public struct TwoHandsLiftHeavenEvaluation
    {
        public bool poseValid;
        public bool sequenceCompleted;
        public float progress01;
        public float symmetry;
        public float tempo;
        public string statusMessage;
    }

    /// <summary>
    /// Three-point-only recognizer for 双手托天. It intentionally observes no
    /// inferred shoulder, elbow, chest, hip or lower-body joints.
    /// </summary>
    [Serializable]
    public sealed class TwoHandsLiftHeavenEvaluator
    {
        [Min(0.1f)] public float minimumWristRiseMeters = 0.35f;
        [Min(0f)] public float overheadAboveHeadMeters = 0.12f;
        [Min(0.01f)] public float startRiseMeters = 0.05f;
        [Min(0f)] public float minimumUpwardSpeed = 0.03f;
        [Min(0.01f)] public float maximumHandHeightDifferenceMeters = 0.18f;
        [Min(0.01f)] public float maximumVerticalSpeedDifference = 0.25f;
        [Min(0.01f)] public float maximumOverheadSpeed = 0.18f;
        [Min(0.1f)] public float overheadHoldSeconds = 0.8f;
        [Min(0.02f)] public float returnHeightToleranceMeters = 0.14f;
        [Min(0f)] public float verticalNoiseToleranceMeters = 0.018f;

        private RehabSessionFrame _sessionFrame;
        private TwoHandsLiftHeavenPhase _phase;
        private float _lastLeftHeight;
        private float _lastRightHeight;
        private float _holdSeconds;
        private bool _hasPreviousPose;

        public RehabSessionFrame SessionFrame { get { return _sessionFrame; } }
        public TwoHandsLiftHeavenPhase Phase { get { return _phase; } }

        public void Reset(RehabPoseSample neutralSample)
        {
            _sessionFrame = RehabSessionFrame.Capture(
                neutralSample,
                overheadAboveHeadMeters,
                minimumWristRiseMeters);
            ResetTransientState();
            if (_sessionFrame.IsValid)
            {
                _lastLeftHeight = neutralSample.leftHandPosition.y;
                _lastRightHeight = neutralSample.rightHandPosition.y;
                _hasPreviousPose = true;
            }
        }

        public void ResetTransientState()
        {
            _phase = TwoHandsLiftHeavenPhase.WaitingForRise;
            _holdSeconds = 0f;
            _hasPreviousPose = false;
        }

        public TwoHandsLiftHeavenEvaluation Evaluate(RehabPoseSample sample, float deltaTime)
        {
            if (!sample.IsValid)
            {
                return Result(false, false, 0f, 0f, 0f, "等待头显与左右腕部追踪");
            }

            if (!_sessionFrame.IsValid)
            {
                Reset(sample);
                return Result(false, false, 0f, 1f, 1f, "已记录中立姿势，请从自然垂手开始");
            }

            var safeDelta = Mathf.Max(0.0001f, deltaTime);
            var leftHeight = sample.leftHandPosition.y;
            var rightHeight = sample.rightHandPosition.y;
            var leftDelta = _hasPreviousPose ? leftHeight - _lastLeftHeight : 0f;
            var rightDelta = _hasPreviousPose ? rightHeight - _lastRightHeight : 0f;
            var leftSpeed = leftDelta / safeDelta;
            var rightSpeed = rightDelta / safeDelta;
            _lastLeftHeight = leftHeight;
            _lastRightHeight = rightHeight;
            _hasPreviousPose = true;

            var heightDifference = Mathf.Abs(leftHeight - rightHeight);
            var symmetry = 1f - Mathf.Clamp01(
                heightDifference / Mathf.Max(0.01f, maximumHandHeightDifferenceMeters));
            var speedDifference = Mathf.Abs(leftSpeed - rightSpeed);
            var tempo = 1f - Mathf.Clamp01(
                speedDifference / Mathf.Max(0.01f, maximumVerticalSpeedDifference));
            var leftRise = leftHeight - _sessionFrame.NeutralLeftWristHeight;
            var rightRise = rightHeight - _sessionFrame.NeutralRightWristHeight;

            switch (_phase)
            {
                case TwoHandsLiftHeavenPhase.WaitingForRise:
                    if (leftRise >= startRiseMeters &&
                        rightRise >= startRiseMeters &&
                        leftSpeed >= minimumUpwardSpeed &&
                        rightSpeed >= minimumUpwardSpeed &&
                        speedDifference <= maximumVerticalSpeedDifference)
                    {
                        _phase = TwoHandsLiftHeavenPhase.Rising;
                    }
                    return Result(false, false, 0.08f, symmetry, tempo, "双腕同时缓慢向上举起");

                case TwoHandsLiftHeavenPhase.Rising:
                    if (leftDelta < -verticalNoiseToleranceMeters || rightDelta < -verticalNoiseToleranceMeters)
                    {
                        _phase = TwoHandsLiftHeavenPhase.WaitingForRise;
                        return Result(false, false, 0f, symmetry, tempo, "上举中断，请双腕回到起始区后重试");
                    }

                    var targetHeight = _sessionFrame.ComfortableOverheadHeight;
                    if (leftHeight >= targetHeight &&
                        rightHeight >= targetHeight &&
                        heightDifference <= maximumHandHeightDifferenceMeters)
                    {
                        _phase = TwoHandsLiftHeavenPhase.HoldingOverhead;
                        _holdSeconds = 0f;
                    }

                    var riseProgress = Mathf.Min(
                        leftRise / Mathf.Max(0.1f, targetHeight - _sessionFrame.NeutralLeftWristHeight),
                        rightRise / Mathf.Max(0.1f, targetHeight - _sessionFrame.NeutralRightWristHeight));
                    return Result(
                        speedDifference <= maximumVerticalSpeedDifference,
                        false,
                        Mathf.Lerp(0.1f, 0.62f, Mathf.Clamp01(riseProgress)),
                        symmetry,
                        tempo,
                        speedDifference <= maximumVerticalSpeedDifference
                            ? "保持双腕同步，继续向头顶上方举起"
                            : "左右速度差较大，请放慢并同步上举");

                case TwoHandsLiftHeavenPhase.HoldingOverhead:
                    var stable = Mathf.Abs(leftSpeed) <= maximumOverheadSpeed &&
                                 Mathf.Abs(rightSpeed) <= maximumOverheadSpeed &&
                                 heightDifference <= maximumHandHeightDifferenceMeters;
                    if (leftHeight < _sessionFrame.ComfortableOverheadHeight - 0.08f ||
                        rightHeight < _sessionFrame.ComfortableOverheadHeight - 0.08f)
                    {
                        _phase = TwoHandsLiftHeavenPhase.Rising;
                        _holdSeconds = 0f;
                        return Result(false, false, 0.5f, symmetry, tempo, "请先在头顶上方稳定双腕");
                    }

                    _holdSeconds = stable ? _holdSeconds + Mathf.Max(0f, deltaTime) : 0f;
                    if (_holdSeconds >= overheadHoldSeconds) _phase = TwoHandsLiftHeavenPhase.Lowering;
                    return Result(
                        stable,
                        false,
                        Mathf.Lerp(0.62f, 0.8f, Mathf.Clamp01(_holdSeconds / Mathf.Max(0.1f, overheadHoldSeconds))),
                        symmetry,
                        stable ? 1f : tempo,
                        stable ? "头顶姿势稳定，请保持" : "请降低速度并保持左右齐平");

                case TwoHandsLiftHeavenPhase.Lowering:
                    var leftReturned = leftHeight <= _sessionFrame.NeutralLeftWristHeight + returnHeightToleranceMeters;
                    var rightReturned = rightHeight <= _sessionFrame.NeutralRightWristHeight + returnHeightToleranceMeters;
                    if (leftReturned && rightReturned && heightDifference <= maximumHandHeightDifferenceMeters)
                    {
                        _phase = TwoHandsLiftHeavenPhase.Completed;
                        return Result(true, true, 1f, symmetry, tempo, "双手托天动作完成");
                    }

                    var descentRange = Mathf.Max(
                        0.1f,
                        _sessionFrame.ComfortableOverheadHeight -
                        Mathf.Max(_sessionFrame.NeutralLeftWristHeight, _sessionFrame.NeutralRightWristHeight));
                    var descent = _sessionFrame.ComfortableOverheadHeight - Mathf.Max(leftHeight, rightHeight);
                    return Result(true, false, Mathf.Lerp(0.8f, 0.98f, Mathf.Clamp01(descent / descentRange)), symmetry, tempo, "双腕同步缓慢下降回起始位置");

                default:
                    return Result(true, true, 1f, symmetry, 1f, "双手托天动作完成");
            }
        }

        private static TwoHandsLiftHeavenEvaluation Result(
            bool valid,
            bool completed,
            float progress,
            float symmetry,
            float tempo,
            string message)
        {
            return new TwoHandsLiftHeavenEvaluation
            {
                poseValid = valid,
                sequenceCompleted = completed,
                progress01 = Mathf.Clamp01(progress),
                symmetry = Mathf.Clamp01(symmetry),
                tempo = Mathf.Clamp01(tempo),
                statusMessage = message
            };
        }
    }
}
