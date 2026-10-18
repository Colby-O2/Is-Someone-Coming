using InteractionSystem.Handlers;
using InteractionSystem.Helpers;
using InteractionSystem.Settings;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace InteractionSystem.Actions
{
    [System.Serializable]
    public class InspectAction : InteractionAction
    {
        [SerializeField] private string _actionNameOverride;
        [SerializeField] private InspectableSettings _settings;

        private InspectorProfile _profile;

        public void SetTitle(string title) => _settings.Title = title;

        public override string ActionName => string.IsNullOrEmpty(_actionNameOverride) ? "Inspect" : _actionNameOverride;

        public override void Initialize(Interactable owner)
        {
            base.Initialize(owner);
            _settings.Bounds = owner.BoundingRadius();
            IsEnabled = true;
        }

        public override bool CanExecute() => IsEnabled;

        public override void Execute(InteractorController interactor)
        {
            _profile = new InspectorProfile(_owner.transform, MathExt.Transform.FromLocal(_owner.transform), _settings);
            interactor.GetInspectorHandler().Inspect(interactor.GetCameraTransform(), _profile);
        }
    }
}
