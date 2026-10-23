using InteractionSystem.Actions;
using UnityEngine;

namespace ColbyO.SJ26
{
    public class TakeMugAction : MoveAction
    {
        public override string ActionName => "Pickup Mug";

        public override bool IsEnabled
        {
            get => _owner.TryGetComponent<Mug>(out var mug) && !mug.IsBrewing;
            set
        {
            
        } }
    }
}
