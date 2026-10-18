using System;

namespace PlazmaGames.Core.FSM
{
    public class Transition<TID> : BaseTransition<TID> where TID : IComparable
    {
        public Action<Transition<TID>> beforeTransition;
        public Action<Transition<TID>> afterTransition;
        public Func<Transition<TID>, bool> condition;

        public Transition(
            TID from,
            TID to,
            Action<Transition<TID>> beforeTransition = null,
            Action<Transition<TID>> afterTransition = null,
            Func<Transition<TID>, bool> condition = null,
            bool forceTransition = false
            ) : base(from, to, forceTransition)
        {
            this.beforeTransition = beforeTransition;
            this.afterTransition = afterTransition;
            this.condition = condition;
        }

        public override bool CanTransition()
        {
            return condition == null || condition(this);
        }

        public override void BeforeTransition() => beforeTransition?.Invoke(this);

        public override void AfterTransition() => afterTransition?.Invoke(this);
    }

    internal class ReverseTransition<TID> : BaseTransition<TID> where TID : IComparable
    {
        public BaseTransition<TID> forwardTransition;

        public ReverseTransition(
            BaseTransition<TID> forwardTransition,
            bool forceTransition = false
            ) : base(forwardTransition.to, forwardTransition.from, forceTransition)
        {
            this.forwardTransition = forwardTransition;
        }

        public override bool CanTransition() => !forwardTransition.CanTransition();

        public override void BeforeTransition() => forwardTransition.AfterTransition();

        public override void AfterTransition() => forwardTransition.BeforeTransition();
    }

    internal class Transition : Transition<string>
    {
        public Transition(
            string from,
            string to,
            Action<Transition<string>> beforeTransition = null,
            Action<Transition<string>> afterTransition = null,
            Func<Transition<string>, bool> condition = null,
            bool forceTransition = false
            ) : base(from, to, beforeTransition, afterTransition, condition, forceTransition)
        {
            this.beforeTransition = beforeTransition;
            this.afterTransition = afterTransition;
            this.condition = condition;
        }
    }

    internal class ReverseTransition : ReverseTransition<string>
    {
        public ReverseTransition(
            BaseTransition<string> forwardTransition,
            bool forceTransition = false
            ) : base(forwardTransition, forceTransition)
        {
            this.forwardTransition = forwardTransition;
        }
    }
}
