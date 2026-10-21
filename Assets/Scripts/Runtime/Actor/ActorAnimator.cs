using UnityEngine;
using PlazmaGames.Attribute;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class ActorAnimator
    {
        [Header("IK Bones")]
        [SerializeField] private Transform _lHand;
        [SerializeField] private Transform _rHand;
        [SerializeField] private Transform _lFoot;
        [SerializeField] private Transform _rFoot;
        [Tooltip("Optional: Assign your character's hips/pelvis or mesh root for subtle weight shift and bobbing.")]
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

        [Header("Pelvis Motion (If Assigned)")]
        [SerializeField] private float _hipBounce = 0.025f;
        [SerializeField] private float _hipSway = 0.03f;

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
                    Walking(deltaTime);
                    break;
                case MoveState.Idle:
                    ReturnToRest(deltaTime);
                    break;
                case MoveState.ChangingState:
                    _timer = 0f;
                    break;
            }
        }

        private void Walking(float deltaTime)
        {
            _timer += deltaTime * _stepSpeed;

            float lPhase = _timer;
            float rPhase = _timer + Mathf.PI;

            float lLiftRaw = Mathf.Max(0f, Mathf.Sin(lPhase));
            float rLiftRaw = Mathf.Max(0f, Mathf.Sin(rPhase));

            float lFootLift = Mathf.Pow(lLiftRaw, 1.3f) * _stepHeight;
            float rFootLift = Mathf.Pow(rLiftRaw, 1.3f) * _stepHeight;

            float lStride = -Mathf.Cos(lPhase) * _stepLength;
            float rStride = -Mathf.Cos(rPhase) * _stepLength;

            float lateralSway = Mathf.Sin(lPhase) * _stepWidthSway;

            Vector3 leftFootTarget = _lFootStartPos + new Vector3(lateralSway, 0f, lStride);
            Vector3 rightFootTarget = _rFootStartPos + new Vector3(lateralSway, 0f, rStride);

            float lPitch = Mathf.Sin(lPhase) * _footPitchAmount * lLiftRaw;
            float rPitch = Mathf.Sin(rPhase) * _footPitchAmount * rLiftRaw;

            SetFootPosition(_lFoot, leftFootTarget, _lFootStartRot, lFootLift, lPitch, deltaTime);
            SetFootPosition(_rFoot, rightFootTarget, _rFootStartRot, rFootLift, rPitch, deltaTime);

            float lArmForward = Mathf.Cos(lPhase) * _armSwingAmount;
            float rArmForward = Mathf.Cos(rPhase) * _armSwingAmount;

            float lArmLift = (1f - Mathf.Sin(lPhase)) * 0.5f * _armSwingHeight;
            float rArmLift = (1f - Mathf.Sin(rPhase)) * 0.5f * _armSwingHeight;

            Vector3 leftHandTarget = _lHandStartPos + new Vector3(
                Mathf.Max(0f, lArmForward) * _armCrossBody,
                lArmLift,
                lArmForward
            );
            Vector3 rightHandTarget = _rHandStartPos + new Vector3(
                -Mathf.Max(0f, rArmForward) * _armCrossBody,
                rArmLift,
                rArmForward
            );

            float lerpFactor = 1f - Mathf.Exp(-_smoothing * deltaTime);

            if (_lHand) _lHand.localPosition = Vector3.Lerp(_lHand.localPosition, leftHandTarget, lerpFactor);
            if (_rHand) _rHand.localPosition = Vector3.Lerp(_rHand.localPosition, rightHandTarget, lerpFactor);

            if (_pelvis)
            {
                Vector3 pelvisOffset = new Vector3(
                    -Mathf.Cos(lPhase) * _hipSway,
                    Mathf.Abs(Mathf.Sin(lPhase)) * _hipBounce,
                    0f
                );
                _pelvis.localPosition = Vector3.Lerp(_pelvis.localPosition, _pelvisStartPos + pelvisOffset, lerpFactor);
            }
        }

        private void ReturnToRest(float deltaTime)
        {
            _timer = 0f;
            float lerpFactor = 1f - Mathf.Exp(-_smoothing * deltaTime);

            SetFootPosition(_lFoot, _lFootStartPos, _lFootStartRot, 0f, 0f, deltaTime);
            SetFootPosition(_rFoot, _rFootStartPos, _rFootStartRot, 0f, 0f, deltaTime);

            if (_lHand) _lHand.localPosition = Vector3.Lerp(_lHand.localPosition, _lHandStartPos, lerpFactor);
            if (_rHand) _rHand.localPosition = Vector3.Lerp(_rHand.localPosition, _rHandStartPos, lerpFactor);
            if (_pelvis) _pelvis.localPosition = Vector3.Lerp(_pelvis.localPosition, _pelvisStartPos, lerpFactor);
        }

        private void SetFootPosition(Transform footTransform, Vector3 localTargetPos, Quaternion startLocalRot, float stepLift, float pitchAngle, float deltaTime)
        {
            if (!footTransform) return;

            float lerpFactor = 1f - Mathf.Exp(-_smoothing * deltaTime);

            Vector3 worldTarget = footTransform.parent.TransformPoint(localTargetPos);
            Vector3 rayStart = worldTarget + Vector3.up * _footRaycastHeight;
            Quaternion targetLocalRot = startLocalRot * Quaternion.Euler(pitchAngle, 0f, 0f);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _footRaycastHeight * 2f, _walkableLayer))
            {
                worldTarget.y = hit.point.y + _footGroundOffset + stepLift;

                Quaternion slopeRot = Quaternion.FromToRotation(footTransform.parent.up, hit.normal) * startLocalRot;
                float groundWeight = Mathf.Clamp01(1f - (stepLift / Mathf.Max(0.001f, _stepHeight)));
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