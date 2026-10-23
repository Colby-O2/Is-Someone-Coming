using UnityEngine;

using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.Math;

using ColbyO.SJ26.Input;
using InteractionSystem.Interfaces;
using System;

namespace ColbyO.SJ26
{
    public enum MovementState
    {
        Grounded,
        Airborne
    }

    [RequireComponent(typeof(CharacterController))]
    public class MovementController : MonoBehaviour, IPlayerController
    {
        private readonly float GRAVITY = -9.8f;
        private readonly float GROUNDED_VEL = -2f;

        [SerializeField] private MovementSettings _settings;
        [SerializeField] private GameObject _flashlight;

        [Header("Debug")]
        [SerializeField, ReadOnly] private MovementState _state;
        [SerializeField, ReadOnly] private float _timeOffGround;
        [SerializeField, ReadOnly] private Vector3 _velocity;
        [SerializeField, ReadOnly] private Vector2 _horizontalVelocity;
        [SerializeField, ReadOnly] private Vector2 _movement;

        private CharacterController _controller;
        private ViewController _viewController;
        private SitController _sitController;
        private IInputMonoSystem _input;

        private float Speed => _settings.Speed;
        public float GravityScale = 1.0f;
        private float Gravity => GRAVITY * _settings.GravityMul * GravityScale;
        [NonSerialized] public Vector3? LerpTo = null;

        public bool LockJustMove = false;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _viewController = GetComponentInChildren<ViewController>();
            _sitController = GetComponentInParent<SitController>();
            _input = GameManager.GetMonoSystem<IInputMonoSystem>();

            DisableFlashlight();
        }

        private void Update()
        {
            if (SJ26GameManager.LockMovement || SJ26GameManager.IsPaused || _sitController.HasDetachedHead()) return;
            _movement = Vector2.ClampMagnitude(_input.RawMovement, 1f);
            ApplyGravity();
            UpdateMovement();
            MoveController();
        }

        private void ApplyGravity()
        {
            _velocity.y = _state == MovementState.Airborne
                ? Mathf.MoveTowards(_velocity.y, -_settings.TerminalVelocity, -Gravity * Time.deltaTime)
                : GROUNDED_VEL * GravityScale;
        }

        private void UpdateHorizontalVelocity(float controlMultiplier)
        {
            Vector3 local = new Vector3(_movement.x, 0f, _movement.y);
            float multiplier = 1f;
            if (_movement.y < 0f) multiplier *= _settings.BackwardSpeedMul;
            if (_movement.x != 0f) multiplier *= _settings.StrafingSpeedMul;
            Vector3 target3D = multiplier * Speed * transform.TransformDirection(local);
            Vector2 target2D = new Vector2(target3D.x, target3D.z) * controlMultiplier;

            _horizontalVelocity = Vector2.Lerp(
                _horizontalVelocity,
                target2D,
                _settings.InputSmoothing * Time.deltaTime
            );

            _velocity.x = _horizontalVelocity.x;
            _velocity.z = _horizontalVelocity.y;
        }

        private void UpdateGrounded()
        {
            UpdateHorizontalVelocity(1f);
            if (!_controller.isGrounded) _state = MovementState.Airborne;
        }

        private void UpdateAirborne()
        {
            UpdateHorizontalVelocity(_settings.AirControl);
            if (_controller.isGrounded)
            {
                _state = MovementState.Grounded;
                _velocity.y = GROUNDED_VEL * GravityScale;
            }
        }

        private void MoveController() => _controller.Move(_velocity * Time.deltaTime);

        private void UpdateMovement()
        {
            switch (_state)
            {
                case MovementState.Grounded: UpdateGrounded(); break;
                case MovementState.Airborne: UpdateAirborne(); break;
            }
        }

        public void Move(Vector3 d)
        {
            bool prev = _controller.enabled;
            _controller.enabled = false;
            transform.position += d;
            _controller.enabled = prev;
        }

        public void MoveTo(Vector3 d)
        {
            bool prev = _controller.enabled;
            _controller.enabled = false;
            transform.position = d;
            _controller.enabled = prev;
        }

        public void MoveToY(float y)
        {
            bool prev = _controller.enabled;
            _controller.enabled = false;
            transform.position = transform.position.SetY(y);
            _controller.enabled = prev;
        }

        public void Freeze()
        {
            SJ26GameManager.LockMovement = true;
        }

        public void Unfreeze()
        {
            SJ26GameManager.LockMovement = false;
        }

        public void EnableFlashlight()
        {
            _flashlight.SetActive(true);
        }

        public void DisableFlashlight()
        {
            _flashlight.SetActive(false);
        }
    }
}