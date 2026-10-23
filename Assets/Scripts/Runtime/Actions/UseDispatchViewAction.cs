using InteractionSystem;
using InteractionSystem.Actions;
using InteractionSystem.Controls;
using InteractionSystem.UI;
using PlazmaGames.Core;
using Unity.VisualScripting;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class UseDispatchViewAction : InspectAction
    {
        public override string ActionName => "Dispatch Units";

        private Promise _promise;

        public bool ReallyCanInteract = false;

        public Promise WaitForEnter()
        {
            return Promise.CreateExisting(ref _promise);
        }

        public override bool IsEnabled { get => ReallyCanInteract && SJ26GameManager.PlayerSit.HasDetachedHead();
            set
        {
            
        } }

        public override void Execute(InteractorController interactor)
        {
            base.Execute(interactor);
            VirtualCaster.ShowCursor();
            Promise.ResolveExisting(ref _promise);
        }
    }
}