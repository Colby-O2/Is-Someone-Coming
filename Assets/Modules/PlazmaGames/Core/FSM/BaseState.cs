using System;

namespace PlazmaGames.Core.FSM
{
    public abstract class BaseState<TID> where TID : IComparable
    {
        protected bool requireTimeToExit;
        protected bool instantTransition;
        protected TID name;

        public BaseState(bool requireTimeToExit = false, bool instantTransition = false)
        {
            this.requireTimeToExit = requireTimeToExit;
            this.instantTransition = instantTransition;
            this.Init();
        }

        public bool HasExitTime() => requireTimeToExit;
        public bool HasInstantExit() => instantTransition;
        public TID GetName() => name;
        public void SetName(TID name) => this.name = name;

        public virtual void Init() { }

        public abstract void Enter();

        public abstract void Update();

        public abstract void FixedUpdate();

        public abstract void ExitRequest();

        public abstract void Exit();
    }
}