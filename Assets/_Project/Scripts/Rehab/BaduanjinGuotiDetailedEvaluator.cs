using System;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    /// <summary>
    /// Elder-friendly evaluation for the 30 Guoti video slices.
    /// Only HMD and wrist data are scored; unobservable lower-body details remain coaching cues.
    /// Transitional slices use a small state machine so an end pose alone cannot complete them.
    /// </summary>
    [Serializable]
    public sealed class BaduanjinGuotiDetailedEvaluator
    {
        private enum GuotiSlicePhase
        {
            WaitingForStart,
            Moving,
            TargetReached,
            Completed
        }

        private enum PunchSide
        {
            Left,
            Right
        }

        [Header("Elder-friendly thresholds")]
        [Min(0.1f)] public float maximumRelaxedHandHeightDifference = 0.30f;
        [Min(0.1f)] public float maximumMovingHandHeightDifference = 0.36f;
        [Min(0.05f)] public float minimumSideReachMeters = 0.32f;
        [Min(0.05f)] public float minimumSingleRaiseDifferenceMeters = 0.30f;
        [Min(1f)] public float comfortableLookYawDegrees = 15f;
        [Min(1f)] public float returnFacingToleranceDegrees = 12f;
        [Min(0.01f)] public float gentleUpperBodyShiftMeters = 0.06f;
        [Min(0.1f)] public float reachDownBelowHeadMeters = 0.58f;
        [Min(0.05f)] public float gentlePunchForwardMeters = 0.30f;

        [Header("Observable motion")]
        [Min(0.01f)] public float minimumObservableWristMotionMeters = 0.05f;
        [Min(1f)] public float minimumObservableYawDegrees = 4f;
        [Min(0.02f)] public float sequenceStartMarginMeters = 0.08f;
        [Min(0.05f)] public float punchDepthSeparationMeters = 0.16f;

        [Header("Gentle tempo scoring (not a pass condition)")]
        [Min(0.1f)] public float comfortableWristSpeedMetersPerSecond = 0.90f;
        [Min(0.2f)] public float excessiveWristSpeedMetersPerSecond = 2.40f;
        [Min(10f)] public float comfortableYawSpeedDegreesPerSecond = 70f;
        [Min(20f)] public float excessiveYawSpeedDegreesPerSecond = 180f;

        public TwoHandsLiftHeavenEvaluator liftHeaven = new TwoHandsLiftHeavenEvaluator
        {
            minimumWristRiseMeters = 0.26f,
            overheadAboveHeadMeters = 0.05f,
            startRiseMeters = 0.04f,
            minimumUpwardSpeed = 0.02f,
            maximumHandHeightDifferenceMeters = 0.26f,
            maximumVerticalSpeedDifference = 0.38f,
            maximumOverheadSpeed = 0.28f,
            overheadHoldSeconds = 0.5f,
            returnHeightToleranceMeters = 0.22f,
            verticalNoiseToleranceMeters = 0.025f
        };

        private RehabSessionFrame _sessionFrame;
        private RehabMovementId _activeMovementId;
        private GuotiSlicePhase _phase;
        private RehabPoseSample _movementStartSample;
        private RehabPoseSample _previousSample;
        private bool _hasPreviousSample;
        private float _maximumWristDisplacement;
        private float _maximumYawDisplacement;
        private float _smoothedWristSpeed;
        private float _smoothedYawSpeed;

        public void Reset(RehabMovementId movementId, RehabPoseSample neutralSample)
        {
            _activeMovementId = movementId;
            _sessionFrame = RehabSessionFrame.Capture(neutralSample, 0.05f, 0.26f);
            _phase = GuotiSlicePhase.WaitingForStart;
            _movementStartSample = neutralSample;
            _previousSample = neutralSample;
            _hasPreviousSample = neutralSample.IsValid;
            _maximumWristDisplacement = 0f;
            _maximumYawDisplacement = 0f;
            _smoothedWristSpeed = 0f;
            _smoothedYawSpeed = 0f;

            if (movementId == RehabMovementId.Baduanjin_Guoti_02_LiangshouTuotian)
            {
                EnsureLiftEvaluator();
                liftHeaven.Reset(neutralSample);
            }
        }

        public BaduanjinStepEvaluation Evaluate(
            RehabMovementId movementId,
            RehabPoseSample sample,
            float deltaTime)
        {
            if (!sample.IsValid)
            {
                return Invalid("等待头显与左右腕部追踪");
            }

            if (!_sessionFrame.IsValid || _activeMovementId != movementId)
            {
                Reset(movementId, sample);
                return Invalid("已记录自然站姿，请跟随视频缓慢开始");
            }

            UpdateMotionMetrics(sample, deltaTime);

            switch (movementId)
            {
                case RehabMovementId.Baduanjin_Guoti_00_WujiZhuang:
                    return EvaluateRelaxedPosture(sample, "双手自然下垂，保持上身放松稳定");
                case RehabMovementId.Baduanjin_Guoti_01_BaoqiuZhuang:
                    return EvaluateHoldingBall(sample);
                case RehabMovementId.Baduanjin_Guoti_02_LiangshouTuotian:
                    return EvaluateLiftHeaven(sample, deltaTime);
                case RehabMovementId.Baduanjin_Guoti_03_YouKaigong:
                    return EvaluateDrawBow(sample, false);
                case RehabMovementId.Baduanjin_Guoti_04_YouKaigongBingbu:
                    return EvaluateBowReturn(sample, false, "右开弓结束，双手缓慢收回身前");
                case RehabMovementId.Baduanjin_Guoti_05_ZuoKaigong:
                    return EvaluateDrawBow(sample, true);
                case RehabMovementId.Baduanjin_Guoti_06_ZuoKaigongBingbu:
                    return EvaluateBowReturn(sample, true, "左开弓结束，双手缓慢收回身前");
                case RehabMovementId.Baduanjin_Guoti_07_YouShangju:
                    return EvaluateSingleRaiseSequence(sample, false);
                case RehabMovementId.Baduanjin_Guoti_08_YouXialuo:
                    return EvaluateSingleLowerSequence(sample, false);
                case RehabMovementId.Baduanjin_Guoti_09_ZuoShangju:
                    return EvaluateSingleRaiseSequence(sample, true);
                case RehabMovementId.Baduanjin_Guoti_10_ZuoXialuo:
                    return EvaluateSingleLowerSequence(sample, true);
                case RehabMovementId.Baduanjin_Guoti_11_YouHouqiao:
                    return EvaluateLook(sample, false);
                case RehabMovementId.Baduanjin_Guoti_12_YouHouqiaoZhuanzheng:
                    return EvaluateFacingReturnSequence(sample, false, "从右后瞧缓慢转回正前方");
                case RehabMovementId.Baduanjin_Guoti_13_ZuoHouqiao:
                    return EvaluateLook(sample, true);
                case RehabMovementId.Baduanjin_Guoti_14_ZuoHouqiaoZhuanzheng:
                    return EvaluateFacingReturnSequence(sample, true, "从左后瞧缓慢转回正前方");
                case RehabMovementId.Baduanjin_Guoti_15_ShangtuoXiaan:
                    return EvaluateOpposingHands(sample);
                case RehabMovementId.Baduanjin_Guoti_16_YouxuanYaotouBaiwei:
                    return EvaluateUpperBodyTurn(sample, false);
                case RehabMovementId.Baduanjin_Guoti_17_ZuoxuanYaotouBaiwei:
                    return EvaluateUpperBodyTurn(sample, true);
                case RehabMovementId.Baduanjin_Guoti_18_LiangshouPanzu:
                    return EvaluateReachDownSequence(sample, "双腕向腿部方向舒适下探，不要求触碰脚面");
                case RehabMovementId.Baduanjin_Guoti_19_TaishouFanchuan:
                    return EvaluateBothHandsRaiseSequence(sample, -0.30f, "双腕从下方缓慢抬至胸前");
                case RehabMovementId.Baduanjin_Guoti_20_FanchuanPanzu:
                    return EvaluateReachDownSequence(sample, "双腕随反穿动作再次向下舒适伸展");
                case RehabMovementId.Baduanjin_Guoti_21_PanzuJushou:
                    return EvaluateBothHandsRaiseSequence(sample, -0.15f, "双腕由下向上举至肩部附近");
                case RehabMovementId.Baduanjin_Guoti_22_JushouXiaanFuwei:
                    return EvaluateBothHandsLowerSequence(sample, "双腕缓慢下按，回到腹部附近");
                case RehabMovementId.Baduanjin_Guoti_23_CuanquanMabu:
                    return EvaluateFistsAtWaist(sample);
                case RehabMovementId.Baduanjin_Guoti_24_ChuquanShouquan:
                    return EvaluatePunchAndReturnSequence(sample, PunchSide.Left, "左腕温和向前出拳后收回，右腕留在腰间");
                case RehabMovementId.Baduanjin_Guoti_25_HuanshouChuquanShouquan:
                    return EvaluatePunchAndReturnSequence(sample, PunchSide.Right, "换右腕温和出拳后收回，左腕留在腰间");
                case RehabMovementId.Baduanjin_Guoti_26_JieshuFuwei:
                    return EvaluatePunchRecoverySequence(sample);
                case RehabMovementId.Baduanjin_Guoti_27_Tizhong:
                    return EvaluateUpperBodyStableForHeelRaise(sample);
                case RehabMovementId.Baduanjin_Guoti_28_ShuangshouBaofu:
                    return EvaluateHandsAtAbdomen(sample, "双手放在腹部前方，保持舒适间距");
                case RehabMovementId.Baduanjin_Guoti_29_ShoushiTiaoxi:
                    return EvaluateClosingBreathSequence(sample);
                default:
                    return Invalid("当前切片尚未配置国体版动作判定");
            }
        }

        public static bool IsDetailedMovement(RehabMovementId movementId)
        {
            return movementId >= RehabMovementId.Baduanjin_Guoti_00_WujiZhuang &&
                   movementId <= RehabMovementId.Baduanjin_Guoti_29_ShoushiTiaoxi;
        }

        private BaduanjinStepEvaluation EvaluateRelaxedPosture(RehabPoseSample sample, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var handsLow = InRange(left.y, -0.82f, -0.20f) && InRange(right.y, -0.82f, -0.20f);
            var handsComfortable = Mathf.Abs(left.x) <= 0.58f && Mathf.Abs(right.x) <= 0.58f;
            var balanced = Mathf.Abs(left.y - right.y) <= maximumRelaxedHandHeightDifference;
            return StaticResult(handsLow && handsComfortable && balanced, message, HeightSymmetry(left, right), 0.35f);
        }

        private BaduanjinStepEvaluation EvaluateHoldingBall(RehabPoseSample sample)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var separation = right.x - left.x;
            var atTorso = InRange(left.y, -0.62f, -0.16f) && InRange(right.y, -0.62f, -0.16f);
            var forward = InRange(left.z, 0.02f, 0.72f) && InRange(right.z, 0.02f, 0.72f);
            var rounded = left.x <= 0.12f && right.x >= -0.12f && InRange(separation, 0.16f, 0.82f);
            var balanced = Mathf.Abs(left.y - right.y) <= maximumRelaxedHandHeightDifference;
            return StaticResult(atTorso && forward && rounded && balanced, "双腕在腹部至胸前形成舒适抱球姿势", HeightSymmetry(left, right), 0.45f);
        }

        private BaduanjinStepEvaluation EvaluateLiftHeaven(RehabPoseSample sample, float deltaTime)
        {
            EnsureLiftEvaluator();
            var evaluation = liftHeaven.Evaluate(sample, deltaTime);
            return new BaduanjinStepEvaluation
            {
                poseValid = evaluation.poseValid,
                sequenceCompleted = evaluation.sequenceCompleted,
                requiresSequenceCompletion = true,
                completion01 = evaluation.progress01,
                statusMessage = evaluation.statusMessage,
                symmetry = evaluation.symmetry,
                tempo = Mathf.Min(evaluation.tempo, CurrentTempoScore())
            };
        }

        private BaduanjinStepEvaluation EvaluateDrawBow(RehabPoseSample sample, bool leftSide)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var openHand = leftSide ? left : right;
            var nearHand = leftSide ? right : left;
            var signedReach = leftSide ? -openHand.x : openHand.x;
            var reachedSide = signedReach >= minimumSideReachMeters;
            var chestLevel = InRange(openHand.y, -0.62f, 0.14f) && InRange(nearHand.y, -0.65f, 0.16f);
            var nearCenter = Mathf.Abs(nearHand.x) <= 0.38f;
            var progress = Mathf.Clamp01(signedReach / Mathf.Max(0.01f, minimumSideReachMeters)) * 0.65f;
            return StaticResult(
                reachedSide && chestLevel && nearCenter,
                leftSide ? "左腕向左舒适展开，右腕留在胸前" : "右腕向右舒适展开，左腕留在胸前",
                1f - Mathf.Clamp01(Mathf.Abs(openHand.y - nearHand.y) / 0.45f),
                progress);
        }

        private BaduanjinStepEvaluation EvaluateBowReturn(RehabPoseSample sample, bool leftSide, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var openHand = leftSide ? left : right;
            var bowObserved = leftSide
                ? openHand.x <= -minimumSideReachMeters * 0.75f
                : openHand.x >= minimumSideReachMeters * 0.75f;
            ObserveSequenceStart(bowObserved);

            var returned = HandsReturnedToTorso(left, right);
            if (_phase == GuotiSlicePhase.Moving && returned)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(message, HeightSymmetry(left, right), bowObserved ? 0.5f : 0f);
        }

        private BaduanjinStepEvaluation EvaluateSingleRaiseSequence(RehabPoseSample sample, bool leftSide)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var high = leftSide ? left : right;
            var low = leftSide ? right : left;
            var separated = high.y - low.y;
            var target = high.y >= -0.06f && low.y <= -0.25f && separated >= minimumSingleRaiseDifferenceMeters;
            var startObserved = separated <= minimumSingleRaiseDifferenceMeters * 0.55f && high.y <= -0.12f;
            ObserveSequenceStart(startObserved || (target && HasObservableMotion()));
            if (_phase == GuotiSlicePhase.Moving && target)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            var progress = Mathf.Clamp01(separated / Mathf.Max(0.01f, minimumSingleRaiseDifferenceMeters));
            return SequenceResult(
                leftSide ? "左腕由低位缓慢上举、右腕自然下按" : "右腕由低位缓慢上举、左腕自然下按",
                progress,
                progress * 0.65f);
        }

        private BaduanjinStepEvaluation EvaluateSingleLowerSequence(RehabPoseSample sample, bool leftSide)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var loweringHand = leftSide ? left : right;
            var other = leftSide ? right : left;
            var raisedObserved = loweringHand.y - other.y >= minimumSingleRaiseDifferenceMeters * 0.70f || loweringHand.y >= -0.08f;
            ObserveSequenceStart(raisedObserved);

            var returned = loweringHand.y <= -0.22f && other.y <= -0.16f &&
                           loweringHand.y >= -0.85f && other.y >= -0.85f &&
                           Mathf.Abs(loweringHand.y - other.y) <= maximumMovingHandHeightDifference;
            if (_phase == GuotiSlicePhase.Moving && returned)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(
                leftSide ? "左腕从高位缓慢下落并回到身前" : "右腕从高位缓慢下落并回到身前",
                HeightSymmetry(left, right),
                raisedObserved ? 0.5f : 0f);
        }

        private BaduanjinStepEvaluation EvaluateLook(RehabPoseSample sample, bool leftSide)
        {
            var yaw = CurrentYawDelta(sample);
            var signedYaw = leftSide ? -yaw : yaw;
            var reached = signedYaw >= comfortableLookYawDegrees;
            var progress = Mathf.Clamp01(signedYaw / Mathf.Max(1f, comfortableLookYawDegrees)) * 0.7f;
            return StaticResult(
                reached,
                leftSide ? "头部缓慢向左转到舒适角度" : "头部缓慢向右转到舒适角度",
                1f,
                progress);
        }

        private BaduanjinStepEvaluation EvaluateFacingReturnSequence(RehabPoseSample sample, bool fromLeft, string message)
        {
            var yaw = CurrentYawDelta(sample);
            var turnedObserved = fromLeft
                ? yaw <= -comfortableLookYawDegrees * 0.65f
                : yaw >= comfortableLookYawDegrees * 0.65f;
            ObserveSequenceStart(turnedObserved);

            if (_phase == GuotiSlicePhase.Moving && Mathf.Abs(yaw) <= returnFacingToleranceDegrees)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(message, 1f, turnedObserved ? 0.5f : 0f);
        }

        private BaduanjinStepEvaluation EvaluateOpposingHands(RehabPoseSample sample)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var leftHigh = left.y >= -0.08f && right.y <= -0.32f;
            var rightHigh = right.y >= -0.08f && left.y <= -0.32f;
            var separation = Mathf.Abs(left.y - right.y);
            var valid = (leftHigh || rightHigh) && separation >= minimumSingleRaiseDifferenceMeters;
            return StaticResult(valid, "一腕舒适上托，另一腕自然下按", Mathf.Clamp01(separation / 0.75f), Mathf.Clamp01(separation / 0.75f) * 0.65f);
        }

        private BaduanjinStepEvaluation EvaluateUpperBodyTurn(RehabPoseSample sample, bool leftSide)
        {
            var headLocal = _sessionFrame.ToSessionLocal(sample.headPosition);
            var yaw = CurrentYawDelta(sample);
            var shifted = leftSide ? headLocal.x <= -gentleUpperBodyShiftMeters : headLocal.x >= gentleUpperBodyShiftMeters;
            var turned = leftSide ? yaw <= -10f : yaw >= 10f;
            var heightStable = Mathf.Abs(sample.headPosition.y - _sessionFrame.NeutralHeadHeight) <= 0.32f;
            var progress = Mathf.Max(
                Mathf.Abs(headLocal.x) / Mathf.Max(0.01f, gentleUpperBodyShiftMeters),
                Mathf.Abs(yaw) / 10f);
            return StaticResult(
                (shifted || turned) && heightStable,
                leftSide ? "上身向左舒适转移，避免大幅弯腰" : "上身向右舒适转移，避免大幅弯腰",
                1f,
                Mathf.Clamp01(progress) * 0.65f);
        }

        private BaduanjinStepEvaluation EvaluateReachDownSequence(RehabPoseSample sample, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var startObserved = left.y >= -reachDownBelowHeadMeters + sequenceStartMarginMeters &&
                                right.y >= -reachDownBelowHeadMeters + sequenceStartMarginMeters;
            ObserveSequenceStart(startObserved);

            var reached = left.y <= -reachDownBelowHeadMeters && right.y <= -reachDownBelowHeadMeters;
            var balanced = Mathf.Abs(left.y - right.y) <= maximumMovingHandHeightDifference;
            if (_phase == GuotiSlicePhase.Moving && reached && balanced)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            var depth = Mathf.Clamp01((Mathf.Min(-left.y, -right.y) - 0.30f) /
                                      Mathf.Max(0.1f, reachDownBelowHeadMeters - 0.30f));
            return SequenceResult(message, HeightSymmetry(left, right), depth * 0.65f);
        }

        private BaduanjinStepEvaluation EvaluateBothHandsRaiseSequence(RehabPoseSample sample, float minimumLocalHeight, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var startObserved = left.y <= minimumLocalHeight - sequenceStartMarginMeters &&
                                right.y <= minimumLocalHeight - sequenceStartMarginMeters;
            ObserveSequenceStart(startObserved);

            var balanced = Mathf.Abs(left.y - right.y) <= maximumMovingHandHeightDifference;
            if (_phase == GuotiSlicePhase.Moving && left.y >= minimumLocalHeight && right.y >= minimumLocalHeight && balanced)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            var heightProgress = Mathf.Clamp01((Mathf.Min(left.y, right.y) + 0.70f) /
                                               Mathf.Max(0.1f, minimumLocalHeight + 0.70f));
            return SequenceResult(message, HeightSymmetry(left, right), heightProgress * 0.65f);
        }

        private BaduanjinStepEvaluation EvaluateBothHandsLowerSequence(RehabPoseSample sample, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var raisedObserved = left.y >= -0.18f && right.y >= -0.18f;
            ObserveSequenceStart(raisedObserved);

            if (_phase == GuotiSlicePhase.Moving && HandsReturnedToTorso(left, right))
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(message, HeightSymmetry(left, right), raisedObserved ? 0.5f : 0f);
        }

        private BaduanjinStepEvaluation EvaluateFistsAtWaist(RehabPoseSample sample)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            return StaticResult(
                FistsAtWaist(left, right),
                "双腕收在腰部两侧；马步仅作引导，不作为腕部评分条件",
                HeightSymmetry(left, right),
                0.45f);
        }

        private BaduanjinStepEvaluation EvaluatePunchAndReturnSequence(RehabPoseSample sample, PunchSide expectedSide, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);

            if (_phase == GuotiSlicePhase.WaitingForStart && FistsAtWaist(left, right))
            {
                _phase = GuotiSlicePhase.Moving;
            }

            var expected = expectedSide == PunchSide.Left ? left : right;
            var supporting = expectedSide == PunchSide.Left ? right : left;
            var expectedExtended = expected.z >= gentlePunchForwardMeters &&
                                   expected.z - supporting.z >= punchDepthSeparationMeters;
            var supportingAtWaist = InRange(supporting.y, -0.72f, -0.20f) &&
                                    supporting.z <= gentlePunchForwardMeters + 0.10f;

            if (_phase == GuotiSlicePhase.Moving && expectedExtended && supportingAtWaist)
            {
                _phase = GuotiSlicePhase.TargetReached;
            }
            else if (_phase == GuotiSlicePhase.TargetReached && FistsAtWaist(left, right))
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(message, 1f - Mathf.Clamp01(Mathf.Abs(left.y - right.y) / 0.55f), expectedExtended ? 0.7f : 0f);
        }

        private BaduanjinStepEvaluation EvaluatePunchRecoverySequence(RehabPoseSample sample)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var punchObserved = Mathf.Abs(left.z - right.z) >= punchDepthSeparationMeters &&
                                Mathf.Max(left.z, right.z) >= gentlePunchForwardMeters;
            ObserveSequenceStart(punchObserved);

            if (_phase == GuotiSlicePhase.Moving && FistsAtWaist(left, right))
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult("出拳结束后双腕缓慢收回腰间", HeightSymmetry(left, right), punchObserved ? 0.5f : 0f);
        }

        private BaduanjinStepEvaluation EvaluateUpperBodyStableForHeelRaise(RehabPoseSample sample)
        {
            var headLocal = _sessionFrame.ToSessionLocal(sample.headPosition);
            var horizontallyStable = new Vector2(headLocal.x, headLocal.z).magnitude <= 0.20f;
            var verticallyComfortable = Mathf.Abs(sample.headPosition.y - _sessionFrame.NeutralHeadHeight) <= 0.18f;
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var handsRelaxed = left.y <= -0.18f && right.y <= -0.18f;
            return StaticResult(
                horizontallyStable && verticallyComfortable && handsRelaxed,
                "保持上身稳定并随视频轻柔提踵；腕部追踪不考核脚部幅度",
                HeightSymmetry(left, right),
                0.35f);
        }

        private BaduanjinStepEvaluation EvaluateHandsAtAbdomen(RehabPoseSample sample, string message)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            return StaticResult(
                HandsAtAbdomen(left, right),
                message,
                HeightSymmetry(left, right),
                0.45f);
        }

        private BaduanjinStepEvaluation EvaluateClosingBreathSequence(RehabPoseSample sample)
        {
            var left = ToBodyLocal(sample, sample.leftHandPosition);
            var right = ToBodyLocal(sample, sample.rightHandPosition);
            var handsRaisedOrOpen = left.y >= -0.18f || right.y >= -0.18f || Mathf.Abs(right.x - left.x) >= 0.68f;
            ObserveSequenceStart(handsRaisedOrOpen);

            var headLocal = _sessionFrame.ToSessionLocal(sample.headPosition);
            var headStable = new Vector2(headLocal.x, headLocal.z).magnitude <= 0.22f &&
                             Mathf.Abs(sample.headPosition.y - _sessionFrame.NeutralHeadHeight) <= 0.22f;
            if (_phase == GuotiSlicePhase.Moving && HandsAtAbdomen(left, right) && headStable)
            {
                _phase = GuotiSlicePhase.Completed;
            }

            return SequenceResult(
                headStable ? "双手从舒展位缓慢收回腹前，自然呼吸完成收势" : "上身回到稳定中立位置，双手放松于腹前",
                HeightSymmetry(left, right),
                handsRaisedOrOpen ? 0.5f : 0f);
        }

        private bool HandsReturnedToTorso(Vector3 left, Vector3 right)
        {
            return InRange(left.y, -0.78f, -0.12f) && InRange(right.y, -0.78f, -0.12f) &&
                   Mathf.Abs(left.x) <= 0.55f && Mathf.Abs(right.x) <= 0.55f &&
                   Mathf.Abs(left.y - right.y) <= maximumMovingHandHeightDifference;
        }

        private bool FistsAtWaist(Vector3 left, Vector3 right)
        {
            var waistLevel = InRange(left.y, -0.72f, -0.20f) && InRange(right.y, -0.72f, -0.20f);
            var atSides = left.x <= -0.08f && right.x >= 0.08f && right.x - left.x <= 1.15f;
            var similarDepth = Mathf.Abs(left.z - right.z) <= punchDepthSeparationMeters;
            var notOverextended = left.z <= gentlePunchForwardMeters + 0.12f &&
                                  right.z <= gentlePunchForwardMeters + 0.12f;
            return waistLevel && atSides && similarDepth && notOverextended;
        }

        private bool HandsAtAbdomen(Vector3 left, Vector3 right)
        {
            var separation = Mathf.Abs(right.x - left.x);
            var atAbdomen = InRange(left.y, -0.68f, -0.20f) && InRange(right.y, -0.68f, -0.20f);
            var close = InRange(separation, 0.08f, 0.62f);
            var forward = left.z >= -0.08f && right.z >= -0.08f;
            var balanced = Mathf.Abs(left.y - right.y) <= maximumRelaxedHandHeightDifference;
            return atAbdomen && close && forward && balanced;
        }

        private void ObserveSequenceStart(bool startObserved)
        {
            if (_phase == GuotiSlicePhase.WaitingForStart && startObserved)
            {
                _phase = GuotiSlicePhase.Moving;
            }
        }

        private void UpdateMotionMetrics(RehabPoseSample sample, float deltaTime)
        {
            if (_movementStartSample.IsValid)
            {
                _maximumWristDisplacement = Mathf.Max(
                    _maximumWristDisplacement,
                    Vector3.Distance(sample.leftHandPosition, _movementStartSample.leftHandPosition),
                    Vector3.Distance(sample.rightHandPosition, _movementStartSample.rightHandPosition));
                _maximumYawDisplacement = Mathf.Max(
                    _maximumYawDisplacement,
                    Mathf.Abs(YawDelta(_movementStartSample.headRotation, sample.headRotation)));
            }

            if (_hasPreviousSample)
            {
                var safeDeltaTime = Mathf.Max(0.01f, deltaTime);
                var wristSpeed = Mathf.Max(
                    Vector3.Distance(sample.leftHandPosition, _previousSample.leftHandPosition),
                    Vector3.Distance(sample.rightHandPosition, _previousSample.rightHandPosition)) / safeDeltaTime;
                var yawSpeed = Mathf.Abs(YawDelta(_previousSample.headRotation, sample.headRotation)) / safeDeltaTime;
                _smoothedWristSpeed = Mathf.Lerp(_smoothedWristSpeed, wristSpeed, 0.45f);
                _smoothedYawSpeed = Mathf.Lerp(_smoothedYawSpeed, yawSpeed, 0.45f);
            }

            _previousSample = sample;
            _hasPreviousSample = true;
        }

        private float CurrentTempoScore()
        {
            var score = 1f;
            if (_maximumWristDisplacement >= minimumObservableWristMotionMeters)
            {
                score = Mathf.Min(score, 1f - Mathf.InverseLerp(
                    comfortableWristSpeedMetersPerSecond,
                    Mathf.Max(comfortableWristSpeedMetersPerSecond + 0.01f, excessiveWristSpeedMetersPerSecond),
                    _smoothedWristSpeed));
            }

            if (_maximumYawDisplacement >= minimumObservableYawDegrees)
            {
                score = Mathf.Min(score, 1f - Mathf.InverseLerp(
                    comfortableYawSpeedDegreesPerSecond,
                    Mathf.Max(comfortableYawSpeedDegreesPerSecond + 1f, excessiveYawSpeedDegreesPerSecond),
                    _smoothedYawSpeed));
            }

            return Mathf.Clamp01(score);
        }

        private bool HasObservableMotion()
        {
            return _maximumWristDisplacement >= minimumObservableWristMotionMeters ||
                   _maximumYawDisplacement >= minimumObservableYawDegrees;
        }

        private Vector3 ToBodyLocal(RehabPoseSample sample, Vector3 worldPosition)
        {
            var yaw = Mathf.Atan2(
                _sessionFrame.InitialFacingDirection.x,
                _sessionFrame.InitialFacingDirection.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, -yaw, 0f) * (worldPosition - sample.headPosition);
        }

        private float CurrentYawDelta(RehabPoseSample sample)
        {
            var currentFacing = Vector3.ProjectOnPlane(sample.headRotation * Vector3.forward, Vector3.up);
            if (currentFacing.sqrMagnitude < 0.0001f) return 0f;
            return Vector3.SignedAngle(_sessionFrame.InitialFacingDirection, currentFacing.normalized, Vector3.up);
        }

        private float HeightSymmetry(Vector3 left, Vector3 right)
        {
            return 1f - Mathf.Clamp01(
                Mathf.Abs(left.y - right.y) / Mathf.Max(0.01f, maximumMovingHandHeightDifference));
        }

        private void EnsureLiftEvaluator()
        {
            if (liftHeaven == null) liftHeaven = new TwoHandsLiftHeavenEvaluator();
        }

        private static float YawDelta(Quaternion from, Quaternion to)
        {
            return Mathf.DeltaAngle(from.eulerAngles.y, to.eulerAngles.y);
        }

        private static bool InRange(float value, float minimum, float maximum)
        {
            return value >= minimum && value <= maximum;
        }

        private static BaduanjinStepEvaluation Invalid(string message)
        {
            return new BaduanjinStepEvaluation
            {
                poseValid = false,
                sequenceCompleted = false,
                requiresSequenceCompletion = false,
                completion01 = 0f,
                statusMessage = message,
                symmetry = 0f,
                tempo = 0f
            };
        }

        private BaduanjinStepEvaluation StaticResult(
            bool valid,
            string message,
            float symmetry,
            float rawPartialProgress)
        {
            return new BaduanjinStepEvaluation
            {
                poseValid = valid,
                sequenceCompleted = false,
                requiresSequenceCompletion = false,
                completion01 = valid
                    ? 0.75f
                    : HasObservableMotion() ? Mathf.Clamp(rawPartialProgress, 0f, 0.7f) : 0f,
                statusMessage = message,
                symmetry = Mathf.Clamp01(symmetry),
                tempo = CurrentTempoScore()
            };
        }

        private BaduanjinStepEvaluation SequenceResult(
            string message,
            float symmetry,
            float rawPartialProgress)
        {
            var completed = _phase == GuotiSlicePhase.Completed;
            float progress;
            switch (_phase)
            {
                case GuotiSlicePhase.Moving:
                    progress = Mathf.Max(0.15f, Mathf.Clamp(rawPartialProgress, 0f, 0.55f));
                    break;
                case GuotiSlicePhase.TargetReached:
                    progress = 0.72f;
                    break;
                case GuotiSlicePhase.Completed:
                    progress = 1f;
                    break;
                default:
                    progress = HasObservableMotion() ? Mathf.Clamp(rawPartialProgress, 0f, 0.12f) : 0f;
                    break;
            }

            return new BaduanjinStepEvaluation
            {
                poseValid = completed,
                sequenceCompleted = completed,
                requiresSequenceCompletion = true,
                completion01 = progress,
                statusMessage = message,
                symmetry = Mathf.Clamp01(symmetry),
                tempo = CurrentTempoScore()
            };
        }
    }
}
