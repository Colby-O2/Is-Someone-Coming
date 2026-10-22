using UnityEngine;

using PlazmaGames.Core;

using InteractionSystem;
using InteractionSystem.Actions;

using ColbyO.SJ26.Dialogue;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class TalkAction : InteractionAction
    {
        [SerializeField] private string DialogueName;
        [SerializeField] private bool _isSingleUse;

        private Promise _promise;

        public override string ActionName => "Talk";

        public override void Execute(InteractorController interactor)
        {
            IsEnabled = !_isSingleUse;
            _owner.CanInteract = false;
            GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(DialogueName)
            .Then(_ =>
            {
                Promise.ResolveExisting(ref _promise);
                _owner.CanInteract = true;
            });
        }

        public void SetDialogue(string dialogueName)
        {
            DialogueName = dialogueName;
        }

        public Promise AwaitTalk()
        {
            _promise = new Promise();
            return _promise;
        }
    }
}