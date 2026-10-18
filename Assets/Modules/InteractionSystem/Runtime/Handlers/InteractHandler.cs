using System.Collections.Generic;
using UnityEngine;
using InteractionSystem.Interfaces;
using InteractionSystem.Hint;

namespace InteractionSystem.Handlers
{
    [System.Serializable]
    public class InteractionSettings
    {
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Collide;
        public Transform InteractionPoint;
        public LayerMask InteractionLayer;
        public LayerMask ObstacleLayerMask;
        public float InteractionRadius = 0.1f;
        public float SphereCastRadius = 0.1f;
        public float NearbyHintRadius = 3f;
    }

    internal sealed class InteractionHandler
    {
        private static readonly int HIT_LIMIT = 64;
        private const float LATCH_DURATION = 0.2f;

        private readonly InteractorController _controller;
        private readonly InteractionSettings _settings;

        private RaycastHit[] _tmpHits = new RaycastHit[HIT_LIMIT];
        private readonly Collider[] _overlapHits = new Collider[HIT_LIMIT];
        private readonly List<IInteractable> _activeNearbyHints = new(HIT_LIMIT);

        private IInteractable _lastPossibleInteractable;

        private float _actionlatchTimer;

        public bool CanInteract { get; set; }
        public bool CantInteract { get => !CanInteract; set => CanInteract = !value; }

        public bool HideAllHints { get; set; }

        public InteractionHandler(InteractorController controller,InteractionSettings settings) 
        { 
            _controller = controller;
            _settings = settings;
            CanInteract = true;
            HideAllHints = false;

        }

        private void StartInteraction(
            int slot, 
            IInteractable interactable
        )
        {
            interactable.Interact(slot, _controller);
        }

        private bool IsBlockedByObstacle(Vector3 from, Vector3 to, LayerMask obstacleMask)
        {
            return Physics.Linecast(from, to, obstacleMask, _settings.TriggerInteraction);
        }

        private IInteractable GetPossibleInteractable(
            Transform from
        )
        {
            Vector3 fromPos = from.position;
            Vector3 dir = (_settings.InteractionPoint.position - fromPos).normalized;

            float bestDist = float.MaxValue;
            IInteractable best = null;

            int hitCount = Physics.RaycastNonAlloc(
                fromPos,
                dir,
                _tmpHits,
                _settings.InteractionRadius,
                _settings.InteractionLayer,
                _settings.TriggerInteraction
            );

            EvaluateHits(hitCount, ref best, ref bestDist);

            if (best == null)
            {
                hitCount = Physics.SphereCastNonAlloc(
                    fromPos,
                    _settings.SphereCastRadius,
                    dir,
                    _tmpHits,
                    _settings.InteractionRadius,
                    _settings.InteractionLayer,
                    _settings.TriggerInteraction
                );

                EvaluateHits(hitCount, ref best, ref bestDist);
            }

            if (best != null)
            {
                Vector3 targetPos = (best as Component).transform.position;

                if (IsBlockedByObstacle(fromPos, targetPos, _settings.ObstacleLayerMask))
                    return null;
            }

            return best;
        }

        private void EvaluateHits(
            int hitCount,
            ref IInteractable best,
            ref float bestDist
        )
        {
            for (int i = 0; i < hitCount; i++)
            {
                ref RaycastHit hit = ref _tmpHits[i];

                if 
                (
                    !hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable) ||
                    !interactable.CanInteract
                ) continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = interactable;
                }
            }
        }

        private void ShowNearbyHints(Transform from)
        {
            int count = Physics.OverlapSphereNonAlloc(
                from.position,
                _settings.NearbyHintRadius,
                _overlapHits,
                _settings.InteractionLayer,
                _settings.TriggerInteraction
            );

            for (int i = 0; i < _activeNearbyHints.Count; i++)
            {
                _activeNearbyHints[i].HintInRange = false;
            }

            Vector3 eyePos = from.position;

            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapHits[i];
                if (!col.TryGetComponent<IInteractable>(out var interactable)) continue;
                if (!interactable.CanInteract) continue;

                Vector3 targetPos = interactable.GetTransform().position;

                if (IsBlockedByObstacle(eyePos, targetPos, _settings.ObstacleLayerMask) || interactable == _lastPossibleInteractable)
                    continue;

                InteractionHint hint = interactable.GetHint();
                hint.HideAction();
                hint.ShowName();
                MoveHint(from, hint);

                if (!hint.gameObject.activeSelf)
                    hint.Enable();

                interactable.HintInRange = true;
                if (!_activeNearbyHints.Contains(interactable))
                    _activeNearbyHints.Add(interactable);
            }

            for (int i = _activeNearbyHints.Count - 1; i >= 0; i--)
            {
                var interactable = _activeNearbyHints[i];
                if (!interactable.HintInRange)
                {
                    interactable.GetHint().Disable();
                    _activeNearbyHints.RemoveAt(i);
                }
            }
        }

        public void CheckForInteraction(
            int slot,
            Transform from
        )
        {
            if (CantInteract) return;
            IInteractable interactable = GetPossibleInteractable(from);
            if (interactable != null) StartInteraction(slot, interactable);
        }

        public bool CheckForPossibleInteraction(Transform from, out IInteractable possibleInteractable)
        {
            if (HideAllHints)
            {
                possibleInteractable = null;
                return false;
            }

            IInteractable found = GetPossibleInteractable(from);

            if (found != null)
            {
                _actionlatchTimer = LATCH_DURATION;
                if (_lastPossibleInteractable != found && _lastPossibleInteractable != null)
                    _lastPossibleInteractable.GetHint().Disable();

                _lastPossibleInteractable = found;
            }
            else if (_actionlatchTimer > 0)
            {
                _actionlatchTimer -= Time.deltaTime;
                found = _lastPossibleInteractable;
            }

            ShowNearbyHints(from);

            if (found == null || CantInteract)
            {
                _lastPossibleInteractable?.GetHint().Disable();
                _lastPossibleInteractable = null;
                possibleInteractable = null;
                return false;
            }

            InteractionHint hint = found.GetHint();
            hint.Enable();
            hint.ShowAction();
            hint.HideName();
            MoveHint(from, hint);

            possibleInteractable = found;
            return true;
        }

        public void DisableAllHints()
        {
            foreach (IInteractable interactable in _activeNearbyHints) interactable?.GetHint().Disable();
            _lastPossibleInteractable?.GetHint().Disable();
        }

        public void MoveHint(Transform from, InteractionHint hint)
        {
            if (hint == null) return;
            hint.MoveHint(from);
        }
    }
}