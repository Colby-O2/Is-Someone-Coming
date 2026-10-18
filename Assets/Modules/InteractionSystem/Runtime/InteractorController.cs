using InteractionSystem.Attribute;
using InteractionSystem.Controls;
using InteractionSystem.Handlers;
using InteractionSystem.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace InteractionSystem
{
    public enum InteractionState
    {
        CheckingFor,
        Moving,
        Inspecting,
        Disabled

    }

    public sealed class InteractorController : MonoBehaviour
    {
        private enum InteractionSlot
        {
            Slot1 = 0,
            Slot2 = 1,
            Slot3 = 2,
            Slot4 = 3
        }

        [Header("Controls")]
        [SerializeField] private ControlScheme _controls;

        [Header("References")]
        [SerializeField] private IPlayerController _player;
        [SerializeField] private Transform _playerCamera;

        [Header("Settings")]
        [SerializeField] private InteractionSettings _interactionSettings;
        [SerializeField] private MoveSettings _moveableSettings;
        [SerializeField] private InspectSettings _inpsectSettings;

        [Header("Interaction State")]
        [SerializeField, ReadOnly] private InteractionState _state;

        private Collider[] _colliders;

        private InteractionHandler _interactionHandler;
        private MoveableHandler _moveableHandler;
        private InspectionHandler _inspectorHandler;

        public ControlScheme Controls { get => _controls; }
        public bool CanInteract {get => _interactionHandler?.CanInteract ?? false; set => _interactionHandler.CanInteract = value; }

        internal InteractionHandler GetInteractionHandler() => _interactionHandler;
        internal MoveableHandler GetMoveableHandler() => _moveableHandler;
        internal InspectionHandler GetInspectorHandler() => _inspectorHandler;
        public Transform GetCameraTransform() => _playerCamera;
        public InteractionState CurrentState { get => _state; }
        public Collider[] Colliders { get => _colliders; }

        private void Awake()
        {
            if (_player == null) _player = GetComponent<IPlayerController>();
            if (!_inpsectSettings.InspectUI) _inpsectSettings.InspectUI = FindAnyObjectByType<InspectionUIController>();
            if (!_controls) _controls = Resources.Load<ControlScheme>("ControlSchemes/DefaultInteractionControls");

            _state = InteractionState.CheckingFor;

            _interactionHandler = new InteractionHandler(this, _interactionSettings);
            _moveableHandler = new MoveableHandler(this, _moveableSettings);
            _inspectorHandler = new InspectionHandler(this, _inpsectSettings);

            CachePlayerColliders();
        }

        private void Update()
        {
            switch (_state)
            {
                case InteractionState.CheckingFor: CheckForInteractions(); break;
                case InteractionState.Moving: UpdateMover(); break;
            }

            _inspectorHandler.HandleUpdate(_playerCamera);
        }

        private void FixedUpdate()
        {
            switch (_state)
            {
                case InteractionState.Moving: _moveableHandler.HandleFixedUpdate(_playerCamera); break;
            }
        }

        private void LateUpdate()
        {
            switch (_state)
            {
                case InteractionState.Moving: _moveableHandler.HandleLateUpdate(); break;
            }

            CheckForPossibleInteractions();
        }

        private void OnEnable()
        {
            Controls.Enable();
        }

        private void OnDisable()
        {
            Controls.Disable();
        }

        public bool IsInspecting() => _inspectorHandler.IsInspecting();

        private void CachePlayerColliders()
        {
            _colliders ??= GetComponentsInChildren<Collider>();
        }

        public void ChangeState(InteractionState state)
        {
            _state = state;
            CanInteract = false;

            switch (_state)
            {
                case InteractionState.CheckingFor:
                    _player.Unfreeze();
                    CanInteract = true;
                    _interactionHandler.HideAllHints = false;
                    break;

                case InteractionState.Moving:
                    _player.Unfreeze();
                    _interactionHandler.DisableAllHints();
                    _interactionHandler.HideAllHints = _moveableSettings.DisabledHintsOnMove;
                    break;

                case InteractionState.Inspecting:
                    _player.Freeze();
                    _interactionHandler.DisableAllHints();
                    _interactionHandler.HideAllHints = true;
                    break;
                case InteractionState.Disabled:
                    _interactionHandler.DisableAllHints();
                    _interactionHandler.HideAllHints = true;
                    break;
            }
        }


        public void UpdateMover()
        {
            if (Controls.DropAction.WasPressedThisFrame())
            {
                _moveableHandler.EndMove();
            }

            _moveableHandler.HandleUpdate(_playerCamera);
        }

        private void CheckForInteractions()
        {
            if (Controls.Slot1InteractAction.WasPressedThisFrame())
            {
                _interactionHandler.CheckForInteraction((int)InteractionSlot.Slot1, _playerCamera);
            }
            else if (Controls.Slot2InteractAction.WasPressedThisFrame())
            {
                _interactionHandler.CheckForInteraction((int)InteractionSlot.Slot2, _playerCamera);
            }
            else if (Controls.Slot3InteractAction.WasPressedThisFrame())
            {
                _interactionHandler.CheckForInteraction((int)InteractionSlot.Slot3, _playerCamera);
            }
            else if (Controls.Slot4InteractAction.WasPressedThisFrame())
            {
                _interactionHandler.CheckForInteraction((int)InteractionSlot.Slot4, _playerCamera);
            }
        }

        private void CheckForPossibleInteractions()
        {
            _interactionHandler.CheckForPossibleInteraction(
                _playerCamera,
                out IInteractable possibleInteractable
            );
        }

    }
}
