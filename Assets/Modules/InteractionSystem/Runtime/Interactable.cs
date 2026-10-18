using InteractionSystem.Actions;
using InteractionSystem.Hint;
using InteractionSystem.Interfaces;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Interactable : MonoBehaviour, IInteractable
    {
        private static Transform _hintContaienr;

        [SerializeField] private string _objectName;
        [SerializeField] private bool _isInteractable = true;

        [Header("Audio")]
        [SerializeField] private AudioSource _as;

        [Header("Interaction Slots")]
        [SerializeReference] private InteractionAction[] _actions = new InteractionAction[4];

        [SerializeField] private SphereCollider _hintBounds;
        private InteractionHint _hint;

        public bool CanInteract { get; set; }
        public bool HintInRange { get; set; }
        public bool IsInteractable => CanInteract && _actions.Any(a => a != null && a.CanExecute());

        private void Awake()
        {
            if (_hintContaienr == null)
            {
                _hintContaienr = new GameObject("Hint Holder").transform;
            }

            if  (!_hintBounds)
            {
                _hintBounds = GetComponent<SphereCollider>();
                if (_hintBounds) _hintBounds.enabled = false;
            }

            CanInteract = _isInteractable;

            if (!_hint)
            {
                _hint = Instantiate(Resources.Load<InteractionHint>("Hint"));
                _hint.transform.SetParent(_hintContaienr);
            }

            _hint.SetOwner(GetComponentInParent<Interactable>());
            _hint.Disable();
        }

        private void Start()
        {
            foreach (var action in _actions)
                action?.Initialize(this);

            InteractorController interactorController = FindAnyObjectByType<InteractorController>();
            _hint.SetPlayer(interactorController.transform);
            _hint.SetPlayerCamera(interactorController.GetCameraTransform());
            _hint.Set(_objectName, _actions);
        }

        private void Update()
        {
            _hint.Set(_objectName, _actions);
        }

        public void Interact(int actionIndex, InteractorController interactor)
        {
            if (!IsInteractable) return;
            if (actionIndex < 0 || actionIndex >= _actions.Length) return;
            if (_actions[actionIndex] != null && _actions[actionIndex].CanExecute())
            {
                if (_as && _actions[actionIndex].Clip != null)
                {
                    Debug.Log("Yo!");
                    _as.PlayOneShot(_actions[actionIndex].Clip);
                }
                _actions[actionIndex].Execute(interactor);
            }
            _hint.Set(_objectName, _actions);
        }

        public void SetAction(int slot, InteractionAction action)
        {
            if (slot > 4)
            {
                Debug.LogWarning($"An Interactable Only Has Four Slot. You Tried Assigning Slot {slot}.");
                return;
            }
            _actions[slot] = action;
            action.Initialize(this);
        }

        public T GetAction<T>() where T : InteractionAction
        {
            foreach (InteractionAction a in _actions) if (a is T action) return action;
            return null;
        }

        public bool TryGetAction<Action>(out Action action) where Action : InteractionAction
        {
            action = GetAction<Action>();
            return action != null;
        }

        public bool HasAction<Action>() where Action : InteractionAction
        {
            return GetAction<Action>() != null;
        }

        public SphereCollider BoundingRadius() => _hintBounds;
        public Transform GetTransform() => transform;

        public InteractionHint GetHint() => _hint;

        public void Restart() { }
    }
}
