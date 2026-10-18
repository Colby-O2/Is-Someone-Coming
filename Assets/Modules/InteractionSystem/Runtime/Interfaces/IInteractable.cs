using InteractionSystem.Hint;
using UnityEngine;

namespace InteractionSystem.Interfaces
{
    internal interface IInteractable
    {
        public bool HintInRange { get; set; }

        public bool CanInteract { get; set; }

        public InteractionHint GetHint();

        public SphereCollider BoundingRadius();

        public void Interact(int actionIndex, InteractorController interactor);

        public Transform GetTransform();

        public void Restart();
    }
}