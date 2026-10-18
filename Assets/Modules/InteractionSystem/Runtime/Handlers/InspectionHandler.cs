using UnityEngine;
using UnityEngine.InputSystem;

using InteractionSystem.Attribute;
using InteractionSystem.Controls;
using InteractionSystem.Helpers;
using InteractionSystem.Settings;
using InteractionSystem.UI;
using InteractionSystem.Interfaces;
using System.Collections.Generic;

namespace InteractionSystem.Handlers
{
    internal class InspectorProfile
    {
        public Transform Obj;
        public MathExt.Transform StartTransform;
        public InspectableSettings Settings;
        public int OriginalLayer;
        public bool InitalRigidBodyState = false;

        public InspectorProfile(Transform obj, MathExt.Transform startTransform, InspectableSettings settings)
        {
            this.Obj = obj;
            this.StartTransform = startTransform;
            this.Settings = settings;
        }
    }

    [System.Serializable]
    internal class InspectSettings
    {
        public Camera InspectionCamera;
        public string InspectionLayer = "Inspecting";
        public InspectionUIController InspectUI;
        public Light InspectionLight;
        public LayerMask InspectorClickableLayer;
    }

    internal sealed class InspectionHandler
    {
        private const float TRANSITION_TIME = 0.3f;

        private readonly InteractorController _controller;
        private readonly InspectSettings _settings;

        private List<InspectorProfile> _profiles;
        private InspectorProfile _currentProfile;
        private int _currentProfileIndex = -1;

        private MathExt.Transform _pickupTarget = new();
        private MathExt.Transform _lookAtTarget = new();
        private MathExt.Transform _headStart = new();

        private Transform _origHeadLoc; 

        private float _transition;

        private IInspectorClickable _currentClickable;
        private IInspectorClickable _currentHoverable;

        private enum State { Starting, Inspecting, Stopping }
        [SerializeField, ReadOnly] private State _state;

        public List<InspectorProfile> GetProfiles => _profiles;
        public InspectorProfile GetProfile => _currentProfile;
        public bool IsInspecting() => _currentProfile != null;

        public InspectionHandler(InteractorController controller, InspectSettings settings)
        {
            _controller = controller;
            _settings = settings;
            _profiles = new List<InspectorProfile>();
            if (_settings.InspectionLight) _settings.InspectionLight.gameObject.SetActive(false);

            GameObject headLoc = new GameObject("HeadLocRef");
            headLoc.SetActive(false);
            _origHeadLoc = headLoc.transform;
        }

        public void SwitchInspectionTo(int index)
        {
            if (_profiles.Count == 0) return;

            _currentProfile?.Obj.gameObject.SetActive(false);
            _currentProfileIndex = index;
            _currentProfile = _profiles[index];
            _currentProfile.Obj.gameObject.SetActive(true);
        }

        public void Inspect(Transform from, InspectorProfile profile)
        {
            if (IsInspecting()) return;

            _controller.ChangeState(InteractionState.Inspecting);
            ToggleLight(true);

            _profiles.Clear();

            if (profile.Settings.ItemList == null || profile.Settings.ItemList.Count == 0)
            {
                _currentProfile = profile;
            }
            else
            {
                foreach (InspectSubComponent comp in profile.Settings.ItemList)
                {
                    Transform t = comp.Transform;
                    InspectableSettings settings = profile.Settings.CreateCopy();
                    settings.ReadText = comp.ReadTextOverride;

                    InspectorProfile p = new InspectorProfile(t, MathExt.Transform.FromLocal(t), settings);
                    _profiles.Add(p);
                    t.gameObject.SetActive(false);
                }

                SwitchInspectionTo(0);
            }

            _state = State.Starting;

            _origHeadLoc.parent = from.transform.parent;
            _origHeadLoc.SetPositionAndRotation(from.transform.position, from.transform.rotation);
            _headStart = MathExt.Transform.FromLocal(from);

            if (_profiles.Count == 0)
            {
                SwitchToInspectionLayer(_currentProfile);
                DisableRigidbody(_currentProfile);
            }
            else
            {
                foreach (InspectorProfile p in _profiles)
                {
                    SwitchToInspectionLayer(p);
                    DisableRigidbody(p);
                }
            }

            CreateTargets(from);
            ShowInspectorView();

            _controller.Controls.InspectionClickAction.performed += CheckForClickable;
            _controller.Controls.InspectionClickAction.canceled += CancelClickable;

            if (profile.Settings.LookType == InspectableLookType.Pickup && _profiles.Count > 0)
            {
                for (int i = 0; i < _profiles.Count; i++)
                {
                    if (i == _currentProfileIndex) continue;
                    _pickupTarget.ApplyTo(_profiles[i].Obj);
                }
            }
        }

        public void StopInspecting(Transform from)
        {
            if (!IsInspecting()) return;

            _controller.Controls.InspectionClickAction.performed -= CheckForClickable;
            _controller.Controls.InspectionClickAction.canceled -= CancelClickable;

            _state = State.Stopping;

            _currentHoverable?.OnHoverExit();
            _currentHoverable = null;

            _currentClickable?.OnRelease();
            _currentClickable = null;

            _pickupTarget.rotation = _currentProfile.Obj.rotation;
            _lookAtTarget = new MathExt.Transform(from);
        }

        public void HandleInteractPress(Transform from)
        {
            if (_state == State.Inspecting)
                StopInspecting(from);
        }

        private void CheckForClickable(InputAction.CallbackContext ctx)
        {
            if (_state == State.Inspecting)
            {
                Vector2 mousePos = VirtualCaster.GetVirtualMousePosition();
                if (VirtualCaster.Instance.Raycast(mousePos, out IInspectorClickable clickable, Mathf.Infinity, _settings.InspectorClickableLayer))
                {
                    if (clickable != null)
                    {
                        _currentClickable = clickable;
                        clickable.OnClick();
                    }
                }
            }
        }

        private void CheckForDragEvent()
        {
            if (_currentClickable != null)
            {
                _currentClickable.OnDrag();
            }
        }

        private void CheckForHoverEvent()
        {
            if (_currentClickable == null)
            {
                Vector2 mousePos = VirtualCaster.GetVirtualMousePosition();
                if (VirtualCaster.Instance.Raycast(mousePos, out IInspectorClickable clickable, Mathf.Infinity, _settings.InspectorClickableLayer))
                {
                    if (clickable != null && _currentHoverable != clickable)
                    {
                        _currentHoverable?.OnHoverExit();
                        _currentHoverable = clickable;
                        clickable.OnHoverEnter();
                    }
                }
                else
                {
                    _currentHoverable?.OnHoverExit();
                    _currentHoverable = null;
                }
            }
            else
            {
                _currentHoverable?.OnHoverExit();
                _currentHoverable = null;
            }

            _currentHoverable?.OnHover();
        }

        private void CancelClickable(InputAction.CallbackContext ctx)
        {
            if (_currentClickable != null)
            {
                _currentClickable.OnRelease();
                _currentClickable = null;
            }
        }

        public bool IsInLayerMask(GameObject obj, LayerMask mask)
        {
            return (mask.value & (1 << obj.layer)) != 0;
        }

        private void SetLayerRecursively(Transform root, int layer)
        {
            if (!IsInLayerMask(root.gameObject, _settings.InspectorClickableLayer)) root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null || !child.gameObject.activeSelf) continue;
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void SwitchToInspectionLayer(InspectorProfile profile)
        {
            if (_settings.InspectionCamera)
            {
                if (!_settings.InspectionCamera.gameObject.activeSelf) _settings.InspectionCamera.gameObject.SetActive(true);
                profile.OriginalLayer = profile.Obj.gameObject.layer;
                SetLayerRecursively(profile.Obj, LayerMask.NameToLayer(_settings.InspectionLayer));
            }

        }

        private void RestoreLayer(InspectorProfile profile)
        {
            if (_settings.InspectionCamera)
            {
                if (_settings.InspectionCamera.gameObject.activeSelf) _settings.InspectionCamera.gameObject.SetActive(false);
                SetLayerRecursively(profile.Obj, profile.OriginalLayer);
            }
        }

        private void DisableRigidbody(InspectorProfile profile)
        {
            if (profile.Obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                profile.InitalRigidBodyState = rb.isKinematic;
                rb.isKinematic = true;
            }
        }

        private void RestoreRigidbody(InspectorProfile profile)
        {
            if (profile.Obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = profile.InitalRigidBodyState;
            }
        }

        public void HandleUpdate(Transform from)
        {
            if (!IsInspecting()) return;

            if (_controller.Controls.InspectionLeaveAction.WasPressedThisFrame())
                HandleInteractPress(from);

            HandleTransitions(from);
            if (!IsInspecting()) return;

            if (_state == State.Inspecting)
            {
                CheckForHoverEvent();
                CheckForDragEvent();

                if (_profiles.Count > 0 && _controller.Controls.InspectionNextAction.WasPerformedThisFrame())
                {
                    SwitchInspectionTo(Mathf.Min(_currentProfileIndex + 1, _profiles.Count - 1));
                }
                else if (_profiles.Count > 0 && _controller.Controls.InspectionBackAction.WasPerformedThisFrame())
                {
                    SwitchInspectionTo(Mathf.Max(0, _currentProfileIndex - 1));
                }
            }

            UpdateInspectorView();
            HandleCursor();
            HandleZoom(from);
            HandleReading();
            HandleRotation(from);
        }

        private void HandleCursor()
        {
            float sensitivity = InputDeviceHandler.IsCurrentGamepad
                ? _controller.Controls.ControllerMouseSensitivity
                : _controller.Controls.KeybaordMouseSensitivity;

            Vector2 delta = _controller.Controls.InspectionCursorAction.ReadValue<Vector2>() * sensitivity;

            VirtualCaster.WrapCursorPosition(delta);
        }

        private void HandleTransitions(Transform from)
        {
            if (_state is not (State.Starting or State.Stopping)) return;

            float dir = _state == State.Starting ? 1f : -1f;
            _transition = Mathf.Clamp01(_transition + Time.deltaTime / TRANSITION_TIME * dir);

            CreateTargets(from);

            if (_currentProfile.Settings.LookType == InspectableLookType.LookAt) HeadTransition(from);
            else if (_currentProfile.Settings.LookType == InspectableLookType.Pickup)
                PickupTransition();

            if (_state == State.Starting && _transition >= 1f) _state = State.Inspecting;
            if (_state == State.Stopping && _transition <= 0f) EndInspection();
        }

        private void HeadTransition(Transform from)
            => MathExt.Transform.Slerp(new MathExt.Transform(_origHeadLoc.position, _origHeadLoc.rotation), _lookAtTarget, _transition).ApplyTo(from);

        private void PickupTransition()
        {
            _currentProfile.Obj.position =
                Vector3.Slerp(_currentProfile.StartTransform.position, _pickupTarget.position, _transition);

            if (_currentProfile.Settings.RotatePickup || _currentProfile.Settings.AllowRotate)
                _currentProfile.Obj.rotation =
                    Quaternion.Slerp(_currentProfile.StartTransform.rotation, _pickupTarget.rotation, _transition);
        }

        private void EndInspection()
        {
            if (_profiles.Count == 0)
            {
                RestoreLayer(_currentProfile);
                RestoreRigidbody(_currentProfile);

            }
            else
            {
                foreach (InspectorProfile p in  _profiles)
                {
                    RestoreLayer(p);
                    RestoreRigidbody(p);
                }
            }

            ToggleLight(false);

            for (int i = 0; i < _profiles.Count; i++)
            {
                _profiles[i].StartTransform.ApplyToLocal(_profiles[i].Obj);
                if (!_profiles[i].Obj.gameObject.activeSelf) _profiles[i].Obj.gameObject.SetActive(true);
            }
            _profiles.Clear();
            _currentProfile.StartTransform.ApplyToLocal(_currentProfile.Obj);
            _currentProfile = null;
            _currentProfileIndex = -1;

            _controller.ChangeState(InteractionState.CheckingFor);
            HideInspectorView();
        }

        private void HandleZoom(Transform from)
        {
            if (_state != State.Inspecting || !_currentProfile.Settings.AllowZoom) return;

            float scroll = (InputDeviceHandler.IsCurrentGamepad ? 
                _controller.Controls.ControllerZoomSensitivity : 
                _controller.Controls.KeybaordZoomSensitivity) * 
                _controller.Controls.InspectionZoomAction.ReadValue<float>();

            if (Mathf.Abs(scroll) < Mathf.Epsilon) return;

            MathExt.Transform trans = new MathExt.Transform(from);
            trans.Translate(Vector3.forward * (scroll * 0.1f));

            float dist = Vector3.Distance(_currentProfile.Obj.position, trans.position);
            if (dist > _currentProfile.Settings.ZoomMin
                && dist <= Vector3.Distance(_lookAtTarget.position, _currentProfile.Obj.position))
            {
                from.position = trans.position;
            }
        }

        private void HandleReading()
        {
            if (_state != State.Inspecting) return;
            if (string.IsNullOrWhiteSpace(_currentProfile.Settings.ReadText)) return;

            if (_controller.Controls.InspectionReadAction.WasPressedThisFrame())
            {
                _settings.InspectUI.SetReadText(_currentProfile.Settings.ReadText);
                _settings.InspectUI?.ToggleReadPanel();
            }
        }

        private void HandleRotation(Transform from)
        {
            if (_state != State.Inspecting || !_currentProfile.Settings.AllowRotate) return;
            if (InputDeviceHandler.IsCurrentKeybaord && !Mouse.current.rightButton.isPressed) return;

            Vector2 delta = (InputDeviceHandler.IsCurrentGamepad ? 
                _controller.Controls.ControllerRotateSensitivity : 
                _controller.Controls.KeybaordRotateSensitivity) * 
                _controller.Controls.InspectionRotateAction.ReadValue<Vector2>() / 3f;

            if (_currentProfile.Settings.LookType == InspectableLookType.Pickup)
                RotatePickup(from, delta);
            else
                RotateLookAt(from, delta);
        }

        private void CreateTargets(Transform from)
        {
            switch (_currentProfile.Settings.LookType)
            {
                case InspectableLookType.Pickup:
                    CreatePickupTarget(from);
                    _lookAtTarget = new MathExt.Transform(from);
                    break;

                case InspectableLookType.LookAt:
                    CreateLookAtTarget(from);
                    break;
            }
        }

        private void CreatePickupTarget(Transform from)
        {
            Quaternion offsetRot =
                Quaternion.AngleAxis(_currentProfile.Settings.PickupRotation.y, from.up) *
                Quaternion.AngleAxis(_currentProfile.Settings.PickupRotation.x, from.right) *
                Quaternion.AngleAxis(_currentProfile.Settings.PickupRotation.z, from.forward) *
                (Quaternion.AngleAxis(180, from.up) * from.rotation);

            Vector3 pos = from.position + from.forward * _currentProfile.Settings.OffsetDistance;
            _pickupTarget = new MathExt.Transform(pos, offsetRot);
        }

        private void CreateLookAtTarget(Transform from)
        {
            if (_currentProfile.Settings.LookAtTarget != null)
            {
                _lookAtTarget = new MathExt.Transform(_currentProfile.Settings.LookAtTarget);
                return;
            }

            Vector3 dir = Vector3.Normalize(from.position - _currentProfile.Obj.position);
            Vector3 pos = _currentProfile.Obj.position + dir * _currentProfile.Settings.OffsetDistance;
            _lookAtTarget = new MathExt.Transform(pos, Quaternion.LookRotation(-dir, Vector3.up));
        }

        private void RotatePickup(Transform from, Vector2 d)
        {
            Quaternion rot = Quaternion.AngleAxis(-d.x, from.up) *
                             Quaternion.AngleAxis(d.y, from.right);

            _currentProfile.Obj.rotation = rot * _currentProfile.Obj.rotation;
        }

        private void RotateLookAt(Transform from, Vector2 d)
        {
            from.RotateAround(_currentProfile.Obj.position, Vector3.up, d.x);
            from.RotateAround(_currentProfile.Obj.position, from.right, -d.y);
        }

        private void ToggleLight(bool on)
        {
            if (_settings.InspectionLight)
                _settings.InspectionLight.gameObject.SetActive(on);
        }

        private void ShowInspectorView()
        {
            if (!_settings.InspectUI) return;

            _settings.InspectUI.OnShow?.Invoke();

            _settings.InspectUI.SetReadPanel(false);
            UpdateInspectorView();
        }

        private void UpdateInspectorView()
        {
            bool hasTitle = !string.IsNullOrWhiteSpace(_currentProfile.Settings.Title);
            _settings.InspectUI.SetTitle(hasTitle);

            string title = _currentProfile.Settings.Title;

            _settings.InspectUI.SetTitleText(title);
            _settings.InspectUI.SetInteractButton(_currentProfile.Settings.HasInteractions);
            _settings.InspectUI.SetRotateButton(_currentProfile.Settings.AllowRotate);
            _settings.InspectUI.SetZoomButton(_currentProfile.Settings.AllowZoom);
            _settings.InspectUI.SetReadButton(!string.IsNullOrWhiteSpace(_currentProfile.Settings.ReadText));
            _settings.InspectUI.SetReadText(_currentProfile.Settings.ReadText);
            _settings.InspectUI.SetNextButton(_profiles.Count > 0 && _currentProfileIndex != _profiles.Count - 1);
            _settings.InspectUI.SetBackButton(_profiles.Count > 0 && _currentProfileIndex != 0);
        }

        private void HideInspectorView()
        {
            _settings.InspectUI?.OnHide?.Invoke();
        }
    }
}
