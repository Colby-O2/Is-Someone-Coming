using ColbyO.SJ26.Dispatch;
using InteractionSystem;
using InteractionSystem.Actions;
using PlazmaGames.Core;
using UnityEngine;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class PhoneAction : InteractionAction
    {
        public override string ActionName => "Answer Phone";

        public override bool IsEnabled { get => GameManager.GetMonoSystem<IDispatchMonoSystem>().PhoneIsRinging();
            set { }
        }

        public override void Execute(InteractorController interactor)
        {
            GameManager.GetMonoSystem<IDispatchMonoSystem>().AnswerPhone();
        }
    }
}
