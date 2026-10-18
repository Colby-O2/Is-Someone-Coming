using UnityEngine;

namespace InteractionSystem.Actions
{
    [System.Serializable]
    public abstract class InteractionAction
    {
        protected Interactable _owner;

        public AudioClip Clip;

        public virtual bool IsEnabled { get; set; }

        public abstract string ActionName { get; }
        public virtual void Initialize(Interactable owner)
        {
            this._owner = owner;
            IsEnabled = true;
        }

        public virtual bool CanExecute() => IsEnabled;
        public abstract void Execute(InteractorController interactor);
    }
}