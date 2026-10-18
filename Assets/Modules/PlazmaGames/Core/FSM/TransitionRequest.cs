using System;

namespace PlazmaGames.Core.FSM
{
    public sealed class TransitionRequest<TID> where TID : IComparable
    {
        public TID target;
        public ITransitionCallback callback;
        public bool isPending;

        public TransitionRequest(TID target, ITransitionCallback callback = null)
        {
            this.target = target;
            this.callback = callback;
            this.isPending = true;
        }

        public void MarkCompleted()
        {
            isPending = false;
        }

    }
}
