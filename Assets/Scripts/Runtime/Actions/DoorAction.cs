using UnityEngine;

using PlazmaGames.Animation;
using PlazmaGames.Attribute;
using PlazmaGames.Core;

using InteractionSystem;
using InteractionSystem.Actions;

using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.Utilities;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class DoorAction : InteractionAction
    {
        [Header("References")]
        [SerializeField] private Transform _door;
        [SerializeField] private Transform _hinge;
        [SerializeField] private GameObject _blockage;
        [SerializeField] private AudioCyclePlayer _audioPlayer;

        [Header("Audio")]
        [SerializeField] private AudioSource _as;
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private AudioClip _closeClip;
        [SerializeField] private AudioClip _lockedClip;

        [Header("Dialogue")]
        [SerializeField] private string OnLockedDialogueName;

        [Header("Settings")]
        [SerializeField] private float _openAngle;
        [SerializeField] private float _speed;

        [Header("Flags")]
        [SerializeField] private bool _isLocked;
        [SerializeField, ReadOnly] private bool _isOpen;

        [Header("Debug")]
        [SerializeField, ReadOnly] private float _curAngle;

        [SerializeField, ReadOnly] private float _lastAppliedAngle = 0f;

        private Promise _promise;

        private IAnimationMonoSystem _animationSystem;

        public bool IsLocked { get => _isLocked; set => _isLocked = value; }
        public bool IsOpen { get => _isOpen; }

        public override string ActionName => _isOpen ? "Close" : "Open";

        public override bool CanExecute() => !_animationSystem.HasAnimationRunning(_owner);

        public override void Initialize(Interactable owner)
        {
            base.Initialize(owner);
            _animationSystem = GameManager.GetMonoSystem<IAnimationMonoSystem>();
        }

        public override void Execute(InteractorController interactor)
        {
            if (_isLocked)
            {
                if (_as && _lockedClip)
                {
                    _as.PlayOneShot(_lockedClip);
                }

                if (!string.IsNullOrEmpty(OnLockedDialogueName))
                {
                    GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(OnLockedDialogueName, passive: true);
                }

                return;
            }

            if (_audioPlayer) _audioPlayer.Stop();

            if (_isOpen)
            {
                RequestClose();
            }
            else
            {
                Promise.ResolveExisting(ref _promise);
                _promise = RequestOpen();
            }
        }

        public Promise StartKnocking()
        {
            if (_audioPlayer) _audioPlayer.Play();

            return Promise.CreateExisting(ref _promise);
        }

        public void ToggleBloackage(bool state)
        {
            if (_blockage) _blockage.SetActive(state);
        }

        private float GetOpenAngle()
        {
            Vector3 doorForward = _hinge.forward;
            Vector3 playerDir = (SJ26GameManager.Player.transform.position - _door.position).normalized;
            float dot = -Vector3.Dot(doorForward, playerDir);
            return dot > 0 ? _openAngle : -_openAngle;
        }

        private void OpenStep(float t, float target, float start)
        {
            float desired = Mathf.Lerp(start, target, t);
            float delta = desired - _lastAppliedAngle;

            _door.RotateAround(_hinge.position, Vector3.up, delta);

            _lastAppliedAngle = desired;
        }

        private Promise Open()
        {
            float start = 0f;
            float target = GetOpenAngle();
            _lastAppliedAngle = start;
            _animationSystem.RequestAnimation(_owner, _speed, (float t) =>
            {
                OpenStep(t, target, start);
            }).Then(_ =>
            {
                Promise.ResolveExisting(ref _promise);
                _curAngle = target;
                _isOpen = true;
            });

            return Promise.CreateExisting(ref _promise); ;
        }

        public Promise RequestOpen(bool force = false)
        {
            if (_isLocked && _as && _lockedClip)
            {
                _as.PlayOneShot(_lockedClip);
            }

            if (_isOpen || _isLocked || _animationSystem.HasAnimationRunning(_owner))
            {
                if (force)
                {
                    _isOpen = true;
                    _animationSystem.StopAllAnimations(_owner);
                    _curAngle = GetOpenAngle();
                    _door.RotateAround(_hinge.position, Vector3.up, _curAngle);
                }

                if (!_isOpen && _animationSystem.HasAnimationRunning(_owner))
                {
                    return _promise;
                }

                return null;
            }

            if (_as && _openClip) _as.PlayOneShot(_openClip);

            return Open();
        }

        private void Close()
        {
            float start = _curAngle;
            float target = 0f;
            _lastAppliedAngle = start;
            _animationSystem.RequestAnimation(_owner, _speed, (float t) =>
            {
                OpenStep(t, target, start);
            }).Then(_ =>
            {
                _curAngle = 0;
                _isOpen = false;
            });
        }

        public void RequestClose(bool force = false)
        {
            if (!_isOpen || _animationSystem.HasAnimationRunning(_owner))
            {
                if (force)
                {
                    _isOpen = false;
                    _animationSystem.StopAllAnimations(_owner);
                    _curAngle = 0f;
                    _door.RotateAround(_hinge.position, Vector3.up, _curAngle);
                }
                return;
            }

            if (_as && _closeClip) _as.PlayOneShot(_closeClip);

            Close();
        }
    }
}