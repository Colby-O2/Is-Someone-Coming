using PlazmaGames.Core.MonoSystem;
using UnityEngine.Events;
using UnityEngine;

namespace ColbyO.SJ26.Input
{
    public interface IInputMonoSystem : IMonoSystem
    {
        public UnityEvent InteractCallback { get; }
        public Vector2 RawMovement { get; }
        public Vector2 RawLook { get; }

        public void EnableMovement();
        public void DisableMovement();
    }
}