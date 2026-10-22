using UnityEngine;
using UnityEngine.InputSystem;

using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.Math;

using InteractionSystem;

using ColbyO.SJ26.Input;

namespace ColbyO.SJ26
{
    public class SitController : MonoBehaviour
    {
        private enum DetachedHeadState
        {
            Attached,
            Entering,
            Looking,
            Returning,
        }

        [SerializeField] private MovementSettings _settings;
        [SerializeField] private Transform _head;
        [SerializeField] private AnimationController _animation;
        [SerializeField] private InteractorController _interactor;
        [SerializeField] private InputAction _sitUpKey;

        [Header("Debug")]
        [SerializeField, ReadOnly] private DetachedHeadState _detachedHeadState = DetachedHeadState.Attached;
        [SerializeField, ReadOnly] private Transform _detachedHeadTargetObject;
        [SerializeField, ReadOnly] private MathExt.Transform _detachedHeadTarget;
        [SerializeField, ReadOnly] private MathExt.Transform _detachedHeadFromTrans;
        [SerializeField, ReadOnly] private Vector2 _detachedLookLimits;
        [SerializeField, ReadOnly] private float _detachedHeadTransition = 0;
        [SerializeField, ReadOnly] private bool _lockHead = false;
        [SerializeField, ReadOnly] private MathExt.Transform _savedLookTransform;
        [SerializeField, ReadOnly] private Vector2 _savedLookLimits;
        [SerializeField, ReadOnly] private bool _isOverridingLook = false;


        private Promise _sitPromise;
        private IInputMonoSystem _input;

        public bool HasDetachedHead() => _detachedHeadState != DetachedHeadState.Attached;

        public bool LockHead { get => _lockHead; set => _lockHead = value; }

        private void Awake()
        {
            _input = GameManager.GetMonoSystem<IInputMonoSystem>();
            if (!_animation) _animation = GetComponent<AnimationController>();
            if (!_interactor) _interactor = GetComponent<InteractorController>();
        }

        private void OnEnable()
        {
            _sitUpKey.Enable();
        }

        private void OnDisable()
        {
            _sitUpKey.Disable();
        }

        private void Update()
        {
            if (!LockHead
                && !_interactor.IsInspecting()
                && !_animation.IsChangingState()
                && _sitUpKey.WasPerformedThisFrame()
            )
            {
                AttachHead();
            }

            if (_detachedHeadState != DetachedHeadState.Attached)
            {
                HandleHeadDeattachment();
                return;
            }
        }

        public Promise DetachHead(Transform target)
        {
            if (_sitPromise == null)
            {
                Promise.CreateExisting(ref _sitPromise);
            }
            _animation.Sit(target);
            DetachHead(target, target);

            return _sitPromise;
        }

        private void CalcualteYawLookLimits(Transform obj)
        {
            Vector3 forward = obj.forward;

            Vector3 minDir = Quaternion.Euler(0, -80f, 0) * forward;
            Vector3 maxDir = Quaternion.Euler(0, 80f, 0) * forward;

            Vector3 localMin = _head.parent.InverseTransformDirection(minDir);
            Vector3 localMax = _head.parent.InverseTransformDirection(maxDir);

            float minYaw = Mathf.Atan2(localMin.x, localMin.z) * Mathf.Rad2Deg;
            float maxYaw = Mathf.Atan2(localMax.x, localMax.z) * Mathf.Rad2Deg;

            _detachedLookLimits = new Vector2(minYaw, maxYaw);
        }

        public void DetachHead(MathExt.Transform target, Transform obj)
        {
            if (_detachedHeadState != DetachedHeadState.Attached) return;
            _detachedHeadTargetObject = obj;
            _detachedHeadTarget = target;
            _detachedHeadTransition = 0;
            _detachedHeadState = DetachedHeadState.Entering;
            _detachedHeadFromTrans = _head;

            if (obj) CalcualteYawLookLimits(obj);
        }

        public void DetachHeadImmediate(Transform target)
        {
            if (_detachedHeadState != DetachedHeadState.Attached) return;

            _animation.Sit(target, immediate: true);

            _detachedHeadTargetObject = target;
            _detachedHeadTarget = target;
            _detachedHeadTransition = 1f;
            _detachedHeadState = DetachedHeadState.Looking;
            _detachedHeadFromTrans = _head;

            MathExt.Transform.Slerp(_detachedHeadFromTrans, _detachedHeadTarget, 1f)
                .ApplyTo(_head);

            CalcualteYawLookLimits(target);
        }

        public void DetachHeadLookAt(Vector3 targetPos)
        {
            MathExt.Transform pos = _head;
            pos.rotation = Quaternion.LookRotation(targetPos - pos.position);
            DetachHead(pos, null);
        }

        public void AttachHead()
        {
            if (_detachedHeadState != DetachedHeadState.Looking) return;
            _detachedHeadTarget = _head;
            _detachedHeadState = DetachedHeadState.Returning;
            _detachedHeadTransition = 0;
            _detachedHeadTargetObject = null;
            _animation.Unsit();
        }

        public void AttachHeadImmediately()
        {
            Promise.ResolveExisting(ref _sitPromise);
            _detachedHeadState = DetachedHeadState.Attached;
            _detachedHeadFromTrans.ApplyTo(_head);
            _detachedHeadTargetObject = null;
            _animation.Unsit();
        }

        public void RetargetHeadLookAt(Vector3 targetPos)
        {
            if (_detachedHeadState == DetachedHeadState.Attached) return;

            if (!_isOverridingLook)
            {
                _savedLookTransform = _head;
                _savedLookLimits = _detachedLookLimits;
                _isOverridingLook = true;
            }

            _detachedHeadFromTrans = _head;

            MathExt.Transform target = _head;
            target.rotation = Quaternion.LookRotation(targetPos - _head.position);
            _detachedHeadTarget = target;

            Vector3 targetDir = (targetPos - _head.position).normalized;
            Vector3 localDir = _head.parent.InverseTransformDirection(targetDir);
            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            _detachedLookLimits = new Vector2(targetYaw, targetYaw);

            _detachedHeadTransition = 0f;
            _detachedHeadState = DetachedHeadState.Entering;
        }

        public void RestoreHeadLook()
        {
            if (!_isOverridingLook || _detachedHeadState == DetachedHeadState.Attached) return;

            _detachedHeadFromTrans = _head;
            _detachedHeadTarget = _savedLookTransform;
            _detachedLookLimits = _savedLookLimits;

            _detachedHeadTransition = 0f;
            _detachedHeadState = DetachedHeadState.Entering;
            _isOverridingLook = false;
        }

        private void ProcessDetachedHeadLook()
        {
            if (SJ26GameManager.LockMovement) return;
            Vector3 rot = _head.localEulerAngles.AngleAs180();

            rot.x -= (_settings.InvertLookY ? -1 : 1) * _settings.Sensitivity * _input.RawLook.y;
            rot.x = Mathf.Clamp(rot.x, _settings.YLookLimit.x, _settings.YLookLimit.y);

            rot.y += (_settings.InvertLookX ? -1 : 1) * _settings.Sensitivity * _input.RawLook.x;
            if (!(_detachedLookLimits.x > _detachedLookLimits.y))
            {
                rot.y = Mathf.Clamp(rot.y, _detachedLookLimits.x, _detachedLookLimits.y);
            }
            else
            {
                if (rot.y < _detachedLookLimits.x && rot.y > _detachedLookLimits.y)
                {
                    float distToMin = Mathf.Abs(rot.y - _detachedLookLimits.x);
                    float distToMax = Mathf.Abs(rot.y - _detachedLookLimits.y);
                    rot.y = (distToMin < distToMax) ? _detachedLookLimits.x : _detachedLookLimits.y;
                }
            }

            _head.localEulerAngles = rot;
        }

        private void HandleHeadDeattachment()
        {
            if (_detachedHeadState == DetachedHeadState.Looking)
            {
                ProcessDetachedHeadLook();
            }
            else if (_detachedHeadState == DetachedHeadState.Entering)
            {
                _detachedHeadTransition = MathExt.Clamp01(
                    _detachedHeadTransition +
                    Time.deltaTime * SJ26GameManager.Preferences.DetachedHeadTransitionTime);
                MathExt.Transform.Slerp(_detachedHeadFromTrans, _detachedHeadTarget, _detachedHeadTransition)
                    .ApplyTo(_head);

                if (_detachedHeadTransition >= 1) _detachedHeadState = DetachedHeadState.Looking;
            }
            else if (_detachedHeadState == DetachedHeadState.Returning)
            {
                _detachedHeadTransition = MathExt.Clamp01(
                    _detachedHeadTransition +
                    Time.deltaTime * SJ26GameManager.Preferences.DetachedHeadTransitionTime);
                MathExt.Transform.Slerp(_detachedHeadTarget, _detachedHeadFromTrans, _detachedHeadTransition)
                    .ApplyTo(_head);

                if (_detachedHeadTransition >= 1)
                {
                    Promise.ResolveExisting(ref _sitPromise);
                    _detachedHeadState = DetachedHeadState.Attached;
                }
            }
        }
    }
}
