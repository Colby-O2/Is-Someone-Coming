using InteractionSystem;
using InteractionSystem.Actions;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class TestAction : InteractionAction
    {
        public override string ActionName => "Test";

        public override void Execute(InteractorController interactor)
        {
            Debug.Log("WOW!");
        }
    }
}