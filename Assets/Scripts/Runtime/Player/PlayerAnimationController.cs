using UnityEngine;

using System;

using PlazmaGames.Core;
using PlazmaGames.Math;
using PlazmaGames.Animation;
using PlazmaGames.Attribute;

using ColbyO.SJ26.Rig;
using ColbyO.SJ26.Utilities;
using ColbyO.SJ26.Entities;


namespace ColbyO.SJ26
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField] private ActorAnimator _animation;

        [SerializeField] private Transform _headIK;
        [SerializeField] private Transform _head;

        [SerializeField] private VelocityTracker _vel;
        [SerializeField] private Transform _mesh;
        [SerializeField] private SitController _sitController;
        [SerializeField] private float _sitHeightOffset = 0.2f;
        [SerializeField] private float _sitForwardOffset = 0.25f;


        [SerializeField, ReadOnly] private MoveState _state;
        [SerializeField, ReadOnly] private Vector3 _startPos;
        [SerializeField, ReadOnly] private Vector3 _meshSavedPosition;
        [SerializeField, ReadOnly] private Quaternion _meshSavedRotation;
        [SerializeField, ReadOnly] private Quaternion _defautHeadRot;

        private IPoseMonoSystem _poseSystem;
        private bool _wasInited = false;

        public  bool HoldingLight { get; set; }

        private void Awake()
        {
            _defautHeadRot = _headIK.localRotation;

            if (!_vel) _vel = GetComponent<VelocityTracker>();
            if (!_sitController) _sitController = GetComponent<SitController>();

            _startPos = _mesh.localPosition;
        }

        private void Start()
        {
            _poseSystem = GameManager.GetMonoSystem<IPoseMonoSystem>();
        }

        private void Update()
        {
            if (SJ26GameManager.IsPaused || SJ26GameManager.LockMovement || _sitController.LockHead) return;

            RotateHead();

            if (_state == MoveState.Sitting || _state == MoveState.SittingIntermediate || _state == MoveState.ChangingState || _sitController.HasDetachedHead()) return;

            _mesh.localPosition = _startPos;

            if (_vel.SpeedInPlane > 0.01f)
            {
                LerpPose(HoldingLight ? MoveState.HoldingLightMoving : MoveState.Moving);
            }
            else
            {
                LerpPose(HoldingLight ? MoveState.HoldingLightIdle: MoveState.Idle);
            }

            _animation.UpdateAnimator(_state, Time.deltaTime);
        }

        public bool CanSit() => _state != MoveState.Sitting && _state != MoveState.SittingIntermediate && !IsChangingState();

        public bool IsChangingState() => _state == MoveState.ChangingState;
        public MoveState GetState() => _state;

        private void RotateHead()
        {
            if (_sitController.HasDetachedHead()) _headIK.forward = _head.forward;
            else _headIK.localRotation = _defautHeadRot;

            _headIK.localRotation = Quaternion.Euler(
                _headIK.localEulerAngles.SetX(_head.localEulerAngles.x)
            );
        }

        private void MoveMeshStep(float t, Vector3 startPos, Vector3 targetPos, Quaternion startRot, Quaternion targetRot)
        {
            _mesh.position = Vector3.Lerp(startPos, targetPos, t);
            _mesh.rotation = Quaternion.Slerp(startRot, targetRot, t);
        }

        public void Sit(Transform target, bool immediate = false)
        {
            _meshSavedPosition = _mesh.position;
            _meshSavedRotation = _mesh.rotation;

            Vector3 targetPos = target.position.SetY(_meshSavedPosition.y - _sitHeightOffset) + _sitForwardOffset * target.forward;
            Quaternion targetRot = Quaternion.LookRotation(target.forward);

            if (immediate)
            {
                LoadPoseImmediate(MoveState.Sitting);
            }
            else
            {
                LerpPose(MoveState.SittingIntermediate)
                .Then(_ =>
                {
                    LerpPose(MoveState.Sitting);
                });
            }

            GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, immediate ? 0f : 1f, (float t) => MoveMeshStep(t, _meshSavedPosition, targetPos, _meshSavedRotation, targetRot));
        }

        public void Unsit()
        {
            if (_state != MoveState.Sitting) return;

            Vector3 startPos = _mesh.position.SetY(_mesh.position.y + 0.05f);
            Quaternion startRot = _mesh.rotation;

            LerpPose(MoveState.SittingIntermediate)
                .Then(_ =>
                {
                    LerpPose(MoveState.Idle);
                });
            GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, 1f, (float t) => MoveMeshStep(t, startPos, _meshSavedPosition, startRot, _meshSavedRotation));
        }

        private Promise LerpPose(MoveState state)
        {
            if (_state == state) return null;

            _state = MoveState.ChangingState;

            return _poseSystem.LerpPose("Player", Enum.GetName(typeof(MoveState), state))
            .Then(_ =>
            {
                _state = state;
                if (!_wasInited && (state == MoveState.Moving || state == MoveState.Idle))
                {
                    _animation.Init();
                    _wasInited = true;
                }
            });
        }

        private void LoadPoseImmediate(MoveState state)
        {
            if (_state == state) return;

            _state = state;

            if (_poseSystem == null) _poseSystem = GameManager.GetMonoSystem<IPoseMonoSystem>();

            _poseSystem.LoadPose("Player", Enum.GetName(typeof(MoveState), state));
        }
    }
}
