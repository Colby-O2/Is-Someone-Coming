using InteractionSystem;
using InteractionSystem.Actions;
using InteractionSystem.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class UseDispatchViewAction : InspectAction
    {
        public override string ActionName => "Start Dispatching";

        public override void Execute(InteractorController interactor)
        {
            base.Execute(interactor);
            VirtualCaster.ShowCursor();
        }
    }
}