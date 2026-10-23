using InteractionSystem;
using InteractionSystem.Actions;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class OpenElevatorAction : InteractionAction
    {
        public override string ActionName => "Call";

        [SerializeField] private Elevator _elevator;

        public override void Initialize(Interactable owner)
        {
            base.Initialize(owner);
            IsEnabled = false;
        }

        public override void Execute(InteractorController interactor)
        {
            _elevator.Open(1.5f);
            IsEnabled = false;
        }
    }
}