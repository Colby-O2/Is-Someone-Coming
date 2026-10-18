using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Core.FSM
{
    public abstract class StateMachine<TState, TID> where TState : BaseState<TID> where TID : IComparable
    {
        public TState activeState;
        protected TID startState;
        protected TID lastState;
        protected List<BaseTransition<TID>> activeTransitions;
        protected List<BaseTransition<TID>> globalTransitions;
        protected TransitionRequest<TID> pendingTransition;
        protected Dictionary<TID, StateInfo<TState, TID>> stateInfoByName;

        public StateMachine()
        {
            activeTransitions = new List<BaseTransition<TID>>();
            globalTransitions = new List<BaseTransition<TID>>();
            stateInfoByName = new Dictionary<TID, StateInfo<TState, TID>>();
            lastState = default;
            startState = default;
        }

        public StateMachine(TID startState) : this()
        {
            this.startState = startState;
        }

        public virtual TID GetCurrentStateID() => activeState.GetName();

        public TState GetCurrentState() => activeState;

        public virtual bool HasPendingTransition() => (pendingTransition != null) && pendingTransition.isPending;

        public virtual void SetStartState(TID startState) => this.startState = startState;

        public abstract void StateCanExit();

        public abstract void RequestStateChange(TID name, bool force = false, ITransitionCallback callback = null);

        public abstract void AddState(TID name, TState state);

        public abstract void AddTransition(BaseTransition<TID> transition);

        public abstract void AddTwoWayTransition(BaseTransition<TID> transition);

        public abstract void Callback(string id);
        
        public abstract void Init();

        public abstract void FixedUpdate();

        public abstract void Update();

        public abstract void ForceExit();

        public abstract void Exit();
    }

    public class FiniteStateMachine<TID> : StateMachine<State<TID>, TID> where TID : IComparable
    {
        public FiniteStateMachine() : base() { }

        public FiniteStateMachine(TID startState) : base(startState) { }

        protected virtual void ChangeState(TID name, ITransitionCallback callback = null)
        {
            callback?.BeforeTransition();
            activeState?.Exit();

            StateInfo<State<TID>, TID > stateInfo = CreateOrGetStateInfo(name, out bool stateExisted);

            if (!stateExisted || stateInfo == null)
            {
                Debug.LogError(" State " + name + " Doesn't Exist!");
                return;
            }

            if (activeState != null) lastState = activeState.GetName();
            activeState = stateInfo.state;
            activeState.Enter();

            activeTransitions = stateInfo.outGoingTransitions;
            foreach (BaseTransition<TID> transition in activeTransitions) transition.Init();


            callback?.AfterTransition();

            if (activeState.HasInstantExit())
            {
                bool hasVaildTransition = CheckDirectTransitions() || CheckGlobalTransitions();
                if (!hasVaildTransition) RequestStateChange(lastState);
            }
        }

        protected virtual bool TryTransition(BaseTransition<TID> transition)
        {
            bool canTransition = transition.CanTransition();

            if (canTransition) RequestStateChange(transition.to ?? lastState, transition.forceTransition, transition);

            return canTransition;
        }

        protected virtual bool CheckTransitions(List<BaseTransition<TID>> transitions)
        {
            foreach (BaseTransition<TID> transition in transitions)
            {
                if (activeState.GetName().CompareTo(transition.to) != 0 && TryTransition(transition)) return true;
            }

            return false;
        }

        protected virtual bool CheckGlobalTransitions() => CheckTransitions(globalTransitions);

        protected virtual bool CheckDirectTransitions() => CheckTransitions(activeTransitions);

        protected virtual StateInfo<State<TID>, TID> CreateOrGetStateInfo(TID name)
        {
            if (!stateInfoByName.TryGetValue(name, out StateInfo<State<TID>, TID> stateInfo))
            {
                stateInfo = new StateInfo<State<TID>, TID>();
                stateInfoByName.Add(name, stateInfo);
            }

            return stateInfo;
        }

        protected virtual StateInfo<State<TID>, TID> CreateOrGetStateInfo(TID name, out bool exist)
        {
            exist = stateInfoByName.TryGetValue(name, out StateInfo<State<TID>, TID> stateInfo);
            if (!exist)
            {
                stateInfo = new StateInfo<State<TID>, TID>();
                stateInfoByName.Add(name, stateInfo);
            }

            return stateInfo;
        }

        public override void StateCanExit()
        {
            if (!HasPendingTransition()) return;

            ChangeState(pendingTransition.target, pendingTransition.callback);
            pendingTransition = null;
        }

        public override void RequestStateChange(TID name, bool force = false, ITransitionCallback callback = null)
        {
            if (!activeState.HasExitTime() || force) ChangeState(name, callback);
            else
            {
                pendingTransition = new TransitionRequest<TID>(name, callback);
                activeState.ExitRequest();
            }
        }

        public override void AddState(TID name, State<TID> state)
        {
            state.SetName(name);
            state.SetParnetStateMachine(this);
            state.Init();

            StateInfo<State<TID>, TID> stateInfo = CreateOrGetStateInfo(name);
            stateInfo.state = state;

            if (stateInfoByName.Count == 1 && startState == null) SetStartState(name);
        }

        public override void AddTransition(BaseTransition<TID> transition)
        {
            transition.Init();

            if (transition.from != null)
            {
                StateInfo<State<TID>, TID> stateInfo = CreateOrGetStateInfo(transition.from);
                stateInfo.AddTransition(transition);
            }
            else globalTransitions.Add(transition);
        }

        public override void AddTwoWayTransition(BaseTransition<TID> transition)
        {
            AddTransition(transition);
            AddTransition(new ReverseTransition<TID>(transition));
        }

        public override void Callback(string id)
        {
            activeState?.Callback(id);
        }

        public override void Init()
        {
            if (startState == null)
            {
                Debug.LogError("Missing Start State at Init() of StateMachine!");
                return;
            }

            ChangeState(startState);
            lastState = startState;
        }

        public override void FixedUpdate()
        {
            activeState?.FixedUpdate();
        }

        public override void Update()
        {
            CheckDirectTransitions();
            CheckGlobalTransitions();
            activeState?.Update();
        }

        public override void ForceExit()
        {
            activeState?.Exit();
            activeState = null;
        }

        public override void Exit()
        {
            if (activeState.HasExitTime()) activeState?.ExitRequest();
            else ForceExit();
        }
    }

    public class FiniteStateMachine : FiniteStateMachine<string>
    {
        public FiniteStateMachine() : base() { }

        public FiniteStateMachine(string startState) : base(startState) { }
    }
}