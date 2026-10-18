using System;

namespace PlazmaGames.Core.FSM
{
    public abstract class BaseTransition<TID> : ITransitionCallback where TID : IComparable
    {
        public TID from;
        public TID to;
        public bool forceTransition;

        public BaseTransition(TID from, TID to, bool forceTransition = false)
        {
            this.from = from;
            this.to = to;
            this.forceTransition = forceTransition;
        }

        public virtual void Init() { }

        public abstract bool CanTransition();
        public abstract void BeforeTransition();
        public abstract void AfterTransition();
    }
}
