using UnityEngine;
using PlazmaGames.Attribute;

namespace ColbyO.SJ26.Entities
{
    [System.Serializable]
    public class ActorAnimator
    {
        [Header("IK Bones")]
        [SerializeField] private Transform _lHand;
        [SerializeField] private Transform _rHand;
        [SerializeField] private Transform _lFoot;
        [SerializeField] private Transform _rFoot;
        [SerializeField] private Transform _pelvis;

        [Header("Walking Settings")]
        [SerializeField] private LayerMask _walkableLayer;
        [SerializeField] private float _stepLength = 0.22f;
        [SerializeField] private float _stepHeight = 0.12f;
        [SerializeField] private float _stepWidthSway = 0.025f;
        [SerializeField] private float _footPitchAmount = 18f;
        [SerializeField] private float _armSwingAmount = 0.16f;
        [SerializeField] private float _armSwingHeight = 0.04f;
        [SerializeField] private float _armCrossBody = 0.03f;
        [SerializeField] private float _stepSpeed = 5f;
        [SerializeField] private float _smoothing = 14f;
        [SerializeField] private float _footRaycastHeight = 1f;
        [SerializeField] private float _footGroundOffset = 0.02f;

        [Header("Pelvis Motion")]
        [SerializeField] private float _hipBounce = 0.025f;
        [SerializeField] private float _hipSway = 0.03f;

        [Header("Chase Mode")]
        [SerializeField] private bool _isChasing = false;
        [SerializeField] private bool _outstretchArmsOnChase = true;
        [SerializeField] private float _chaseSpeedMultiplier = 2.6f;
        [SerializeField] private float _chaseStrideMultiplier = 1.85f;
        [SerializeField] private float _chaseStepHeightMultiplier = 2.0f;
        [SerializeField] private float _chaseSwayMultiplier = 2.2f;
        [SerializeField] private float _chaseHipCrouchOffset = -0.05f;
        [SerializeField] private Vector3 _chaseArmReachOffset = new Vector3(0f, 0.28f, 0.35f);
        [SerializeField] private float _chaseClawIntensity = 0.12f;

        [Header("Debug")]
        [SerializeField, ReadOnly] private float _timer;

        private Vector3 _lHandStartPos;
        private Vector3 _rHandStartPos;
        private Vector3 _lFootStartPos;
        private Vector3 _rFootStartPos;
        private Vector3 _pelvisStartPos;

        private Quaternion _lFootStartRot;
        private Quaternion _rFootStartRot;

        private bool _initialized;

        public bool IsChasing
        {
            get => _isChasing;
            set => _isChasing = value;
        }

        public void Init()
        {
            CacheRestPositions();
            _initialized = true;
        }

        public void CacheRestPositions()
        {
            if (_lHand) _lHandStartPos = _lHand.localPosition;
            if (_rHand) _rHandStartPos = _rHand.localPosition;
            if (_lFoot)
            {
                _lFootStartPos = _lFoot.localPosition;
                _lFootStartRot = _lFoot.localRotation;
            }
            if (_rFoot)
            {
                _rFootStartPos = _rFoot.localPosition;
                _rFootStartRot = _rFoot.localRotation;
            }
            if (_pelvis) _pelvisStartPos = _pelvis.localPosition;
        }

        public void UpdateAnimator(MoveState state, float deltaTime)
        {
            if (!_initialized) Init();

            switch (state)
            {
                case MoveState.Moving:
                case MoveState.HoldingLightMoving:
                    Walking(deltaTime, state);
                    break;
                case MoveState.Idle:
                case MoveState.HoldingLightIdle:
                    ReturnToRest(deltaTime, state);
                    break;
                case MoveState.ChangingState:
                    _timer = 0f;
                    break;
            }
        }

        private void Walking(float deltaTime, MoveState state = MoveState.Moving)
        {
            float activeStepSpeed = _isChasing ? _stepSpeed * _chaseSpeedMultiplier : _stepSpeed;
            float activeStepLength = _isChasing ? _stepLength * _chaseStrideMultiplier : _stepLength;
            float activeStepHeight = _isChasing ? _stepHeight * _chaseStepHeightMultiplier : _stepHeight;
            float activeWidthSway = _isChasing ? _stepWidthSway * _chaseSwayMultiplier : _stepWidthSway;
            float activeFootPitch = _isChasing ? _footPitchAmount * 1.6f : _footPitchAmount;
            float activeHipBounce = _isChasing ? _hipBounce * 2.4f : _hipBounce;
            float activeHipSway = _isChasing ? _hipSway * 2.0f : _hipSway;
            float activeSmoothing = _isChasing ? _smoothing * 1.6f : _smoothing;

            _timer += deltaTime * activeStepSpeed;

            float lPhase = _timer;
            float rPhase = _timer + Mathf.PI;

            float lLiftRaw = Mathf.Max(0f, Mathf.Sin(lPhase));
            float rLiftRaw = Mathf.Max(0f, Mathf.Sin(rPhase));

            float liftPow = _isChasing ? 1.1f : 1.3f;
            float lFootLift = Mathf.Pow(lLiftRaw, liftPow) * activeStepHeight;
            float rFootLift = Mathf.Pow(rLiftRaw, liftPow) * activeStepHeight;

            float lStride = -Mathf.Cos(lPhase) * activeStepLength;
            float rStride = -Mathf.Cos(rPhase) * activeStepLength;

            float lateralSway = Mathf.Sin(lPhase) * activeWidthSway;

            Vector3 leftFootTarget = _lFootStartPos + new Vector3(lateralSway, 0f, lStride);
            Vector3 rightFootTarget = _rFootStartPos + new Vector3(lateralSway, 0f, rStride);

            float lPitch = Mathf.Sin(lPhase) * activeFootPitch * lLiftRaw;
            float rPitch = Mathf.Sin(rPhase) * activeFootPitch * rLiftRaw;

            SetFootPosition(_lFoot, leftFootTarget, _lFootStartRot, lFootLift, lPitch, activeStepHeight, activeSmoothing, deltaTime);
            SetFootPosition(_rFoot, rightFootTarget, _rFootStartRot, rFootLift, rPitch, activeStepHeight, activeSmoothing, deltaTime);

            Vector3 leftHandTarget;
            Vector3 rightHandTarget;

            if (_isChasing && _outstretchArmsOnChase)
            {
                leftHandTarget = _lHandStartPos + _chaseArmReachOffset + new Vector3(
                    Mathf.Sin(lPhase * 1.3f) * (_chaseClawIntensity * 0.5f),
                    Mathf.Sin(lPhase) * _chaseClawIntensity,
                    Mathf.Cos(lPhase) * _chaseClawIntensity
                );

                rightHandTarget = _rHandStartPos + _chaseArmReachOffset + new Vector3(
                    -Mathf.Sin(rPhase * 1.3f) * (_chaseClawIntensity * 0.5f),
                    Mathf.Sin(rPhase) * _chaseClawIntensity,
                    Mathf.Cos(rPhase) * _chaseClawIntensity
                );
            }
            else
            {
                float armSwingMul = _isChasing ? 1.8f : 1f;
                float armHeightMul = _isChasing ? 2.5f : 1f;

                float lArmForward = Mathf.Cos(lPhase) * (_armSwingAmount * armSwingMul);
                float rArmForward = Mathf.Cos(rPhase) * (_armSwingAmount * armSwingMul);

                float lArmLift = (1f - Mathf.Sin(lPhase)) * 0.5f * (_armSwingHeight * armHeightMul);
                float rArmLift = (1f - Mathf.Sin(rPhase)) * 0.5f * (_armSwingHeight * armHeightMul);

                leftHandTarget = _lHandStartPos + new Vector3(
                    Mathf.Max(0f, lArmForward) * _armCrossBody,
                    lArmLift,
                    lArmForward
                );
                rightHandTarget = _rHandStartPos + new Vector3(
                    -Mathf.Max(0f, rArmForward) * _armCrossBody,
                    rArmLift,
                    rArmForward
                );
            }

            float lerpFactor = 1f - Mathf.Exp(-activeSmoothing * deltaTime);

            if (_lHand) _lHand.localPosition = Vector3.Lerp(_lHand.localPosition, leftHandTarget, lerpFactor);
            if (_rHand && state != MoveState.HoldingLightMoving) _rHand.localPosition = Vector3.Lerp(_rHand.localPosition, rightHandTarget, lerpFactor);

            if (_pelvis)
            {
                float crouchOffset = _isChasing ? _chaseHipCrouchOffset : 0f;
                Vector3 pelvisOffset = new Vector3(
                    -Mathf.Cos(lPhase) * activeHipSway,
                    crouchOffset + (Mathf.Abs(Mathf.Sin(lPhase)) * activeHipBounce),
                    0f
                );
                _pelvis.localPosition = Vector3.Lerp(_pelvis.localPosition, _pelvisStartPos + pelvisOffset, lerpFactor);
            }
        }

        private void ReturnToRest(float deltaTime, MoveState state = MoveState.Moving)
        {
            _timer = 0f;
            float lerpFactor = 1f - Mathf.Exp(-_smoothing * deltaTime);

            SetFootPosition(_lFoot, _lFootStartPos, _lFootStartRot, 0f, 0f, _stepHeight, _smoothing, deltaTime);
            SetFootPosition(_rFoot, _rFootStartPos, _rFootStartRot, 0f, 0f, _stepHeight, _smoothing, deltaTime);

            if (_lHand) _lHand.localPosition = Vector3.Lerp(_lHand.localPosition, _lHandStartPos, lerpFactor);
            if (_rHand && state != MoveState.HoldingLightIdle) _rHand.localPosition = Vector3.Lerp(_rHand.localPosition, _rHandStartPos, lerpFactor);
            if (_pelvis) _pelvis.localPosition = Vector3.Lerp(_pelvis.localPosition, _pelvisStartPos, lerpFactor);
        }

        private void SetFootPosition(Transform footTransform, Vector3 localTargetPos, Quaternion startLocalRot, float stepLift, float pitchAngle, float currentMaxStepHeight, float currentSmoothing, float deltaTime)
        {
            if (!footTransform) return;

            float lerpFactor = 1f - Mathf.Exp(-currentSmoothing * deltaTime);

            Vector3 worldTarget = footTransform.parent.TransformPoint(localTargetPos);
            Vector3 rayStart = worldTarget + Vector3.up * _footRaycastHeight;
            Quaternion targetLocalRot = startLocalRot * Quaternion.Euler(pitchAngle, 0f, 0f);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _footRaycastHeight * 2f, _walkableLayer))
            {
                worldTarget.y = hit.point.y + _footGroundOffset + stepLift;

                Quaternion slopeRot = Quaternion.FromToRotation(footTransform.parent.up, hit.normal) * startLocalRot;
                float groundWeight = Mathf.Clamp01(1f - (stepLift / Mathf.Max(0.001f, currentMaxStepHeight)));
                targetLocalRot = Quaternion.Slerp(targetLocalRot, slopeRot * Quaternion.Euler(pitchAngle, 0f, 0f), groundWeight);
            }
            else
            {
                worldTarget.y += stepLift;
            }

            Vector3 finalLocalPos = footTransform.parent.InverseTransformPoint(worldTarget);
            footTransform.localPosition = Vector3.Lerp(footTransform.localPosition, finalLocalPos, lerpFactor);
            footTransform.localRotation = Quaternion.Slerp(footTransform.localRotation, targetLocalRot, lerpFactor);
        }
    }
}