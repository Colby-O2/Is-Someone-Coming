using System;
using System.Collections.Generic;

namespace PlazmaGames.Core.FSM
{
    public sealed class StateInfo<TState, TID> where TState : BaseState<TID> where TID : IComparable
    {
        public TState state;
        public List<BaseTransition<TID>> outGoingTransitions;

        public StateInfo(TState state = null)
        {
            this.state = state;
            outGoingTransitions = new List<BaseTransition<TID>>();
        }

        public void AddTransition(BaseTransition<TID> transition) => outGoingTransitions.Add(transition);
    }
}
