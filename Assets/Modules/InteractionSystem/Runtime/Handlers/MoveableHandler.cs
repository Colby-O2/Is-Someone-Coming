using InteractionSystem.Helpers;
using InteractionSystem.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace InteractionSystem.Handlers
{
    internal class MoveableProfile
    {
        public Transform obj;
        public MoveableSettings settings;
        public List<Collider> colliders;
        public List<Rigidbody> rigs;
        public Quaternion rotationOffset;

        public MoveableProfile(Transform mover, Transform obj, MoveableSettings settings)
        {
            this.obj = obj;
            this.settings = settings;
            if (settings.IgnoreChildren)
            {
                this.rigs = obj.GetComponents<Rigidbody>()
                    .ToList();
            }
            else
            {
                this.rigs = obj.GetComponentsInChildren<Rigidbody>(false)
                    .ToList();
            }
            this.colliders = obj.GetComponentsInChildren<Collider>(false)
                .Where(c => c.enabled)
                .Where(c => (c.excludeLayers & LayerMask.GetMask("KeepOn")) == 0)
                .ToList();
            this.rotationOffset = Quaternion.Inverse(mover.rotation) * obj.rotation;
        }
    }

    [System.Serializable]
    internal class MoveSettings
    {
        public float MoveSpeed;
        public float MoveRadius;
        public bool DisabledHintsOnMove = true;
    }

    internal sealed class MoveableHandler
    {
        private enum MoveState { None, Starting, Moving, Ending }

        private readonly InteractorController _controller;
        private readonly MoveSettings _settings;

        private MoveableProfile _profile;
        private MoveState _state = MoveState.None;

        private Vector3 _target;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private readonly int _collisionMask;

        public bool IsMoving => _profile != null;

        public MoveableHandler(InteractorController controller, MoveSettings settings)
        {
            _controller = controller;
            _settings = settings;

            _collisionMask =
                ~LayerMask.GetMask("Player") ^
                LayerMask.GetMask("PlayerBounds") ^
                LayerMask.GetMask("Interactable");
        }

        public void Move(Transform from, Transform obj, MoveableSettings settings)
        {
            if (IsMoving || _state == MoveState.Ending) return;

            _profile = new MoveableProfile(from, obj, settings);
            TogglePhysics(false);

            _target = obj.position;
            _state = MoveState.Starting;

            _controller.ChangeState(InteractionState.Moving);
        }

        public void EndMove()
        {
            if (!IsMoving || _state == MoveState.Starting) return;

            _state = MoveState.Ending;
            TogglePhysics(true);
        }

        public void HandleFixedUpdate(Transform from)
        {
            if (!IsMoving) return;

            float distance = _profile.settings.HoldDistance;

            int size = Physics.SphereCastNonAlloc(
                from.position,
                _settings.MoveRadius,
                from.forward,
                _hits,
                _profile.settings.HoldDistance,
                _collisionMask
                );

            RaycastHit? hit = _hits
                    .Cast<RaycastHit?>()
                    .Take(size)
                    .FirstOrDefault(h => h?.transform != _profile.obj);

            if (hit.HasValue)
            {
                distance = hit.Value.distance;
            }

            _target = from.position + from.forward * distance;
        }

        public void HandleUpdate(Transform from)
        {
            if (!IsMoving) return;

            _profile.obj.position = Vector3.Lerp(
                _profile.obj.position,
                _target,
                Time.deltaTime * _settings.MoveSpeed
            );

            ApplyRotation(from);
        }

        public void HandleLateUpdate()
        {
            switch (_state)
            {
                case MoveState.Starting:
                    _state = MoveState.Moving;
                    break;

                case MoveState.Ending:
                    _profile = null;
                    _state = MoveState.None;
                    _controller.ChangeState(InteractionState.CheckingFor);
                    break;
            }
        }

        public Transform GetMovingObject() => _profile?.obj;

        private void IgnorePlayerCollisions(bool ignore)
        {
            foreach (Collider objCol in _profile.colliders)
            {
                foreach (Collider playerCol in _controller.Colliders)
                {
                    Physics.IgnoreCollision(objCol, playerCol, ignore);
                }
            }
        }

        private void TogglePhysics(bool enable)
        {
            _profile.rigs.ForEach(r => r.isKinematic = !enable);
            IgnorePlayerCollisions(!enable);
        }

        private void ApplyRotation(Transform from)
        {
            switch (_profile.settings.RotationType)
            {
                case MoveableSettings.RotateType.JustYAxis:
                    float y = (from.rotation * _profile.rotationOffset).eulerAngles.y;
                    _profile.obj.eulerAngles = _profile.obj.eulerAngles.SetY(y);
                    break;

                case MoveableSettings.RotateType.Full:
                    _profile.obj.rotation = from.rotation * _profile.rotationOffset;
                    break;
            }
        }
    }
}
