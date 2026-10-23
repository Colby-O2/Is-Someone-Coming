using InteractionSystem;
using InteractionSystem.Actions;
using PlazmaGames.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class RadioAction : InteractionAction
    {
        public override string ActionName => "Play";

        private Promise _prmoise;

        public override void Initialize(Interactable owner)
        {
            base.Initialize(owner);
            IsEnabled = false;
        }

        public Promise WaitForPlay()
        {
            Debug.Log("Waiting For Radio!");
            IsEnabled = true;
            return Promise.CreateExisting(ref _prmoise);
        }

        public override void Execute(InteractorController interactor)
        {
            if (_prmoise == null) return;

            Promise.ResolveExisting(ref _prmoise);
            IsEnabled = false;
        }
    }
}