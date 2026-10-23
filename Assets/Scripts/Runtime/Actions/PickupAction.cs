using InteractionSystem;
using InteractionSystem.Actions;
using PlazmaGames.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class PickupAction : InteractionAction
    {
        public override string ActionName => "Pickup";

        private Promise _prmoise;

        public Promise WaitForPickup()
        {
            return Promise.CreateExisting(ref _prmoise);
        }

        public override void Execute(InteractorController interactor)
        {
            if (_prmoise == null) return;

            Promise.ResolveExisting(ref _prmoise);
            IsEnabled = false;
            _owner.gameObject.SetActive(false);
        }
    }
}