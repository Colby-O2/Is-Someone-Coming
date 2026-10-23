using InteractionSystem.Settings;
using UnityEngine;

namespace InteractionSystem.Actions
{
    [System.Serializable]
    public class MoveAction : InteractionAction
    {
        [SerializeField] private string _actionNameOverride;
        [SerializeField] private MoveableSettings _settings;
        [SerializeField] private bool _detachFromParent = false;

        public override string ActionName => string.IsNullOrEmpty(_actionNameOverride) ? "Move" : _actionNameOverride;
        public override bool IsEnabled { get; set; }

        public override void Initialize(Interactable owner)
        {
            base.Initialize(owner);
            _settings.Bounds = owner.BoundingRadius();
            IsEnabled = true;
        }

        public override bool CanExecute() => IsEnabled;

        public override void Execute(InteractorController interactor)
        {
            if (_detachFromParent) _owner.transform.parent = null;
            interactor.GetMoveableHandler().Move(interactor.GetCameraTransform(), _owner.transform, _settings);
        }
    }
}
