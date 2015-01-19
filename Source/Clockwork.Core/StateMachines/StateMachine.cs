using SiliconStudio.Core.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clockwork.StateMachines
{
    /// <summary>
    /// Represents a hierarchical finite state machine.
    /// </summary>
    public class StateMachine : IStateContext
    {
        private struct ActiveState
        {
            public IState State;
            public TimeSpan Time;
        }

        private readonly FastList<ActiveState> configuration = new FastList<ActiveState>();
        private readonly FastList<StateMachineEvent> events = new FastList<StateMachineEvent>();
        private TimeSpan lastEventTime;
        private TimeSpan totalTime;

        public IState Root { get; set; }

        public IEnumerable<IState> Configuration
        {
            get { return configuration.Select(activeState => activeState.State); }
        }

        /// <summary>
        /// Post an event that will be handles when the state machine is next updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void PostEvent(object sender, EventArgs args)
        {
            events.Add(new StateMachineEvent(sender, args));
        }

        public void Update(TimeSpan elapsedTime)
        {
            totalTime += elapsedTime;

            if (configuration.Count == 0)
            {
                lastEventTime = totalTime - elapsedTime;
                Transition fuse = new Transition(Root);
                EnterStates(new[] { fuse });
            }

            foreach (var e in events)
            {
                if (e.Time < TimeSpan.Zero)
                    e.Time = totalTime;
            }

            bool noMoreEvents = false;

            while (!noMoreEvents)
            {
                bool isStable = false;
                while (!isStable)
                {
                    var enabledTransitions = GetEnabledTransitions(null);

                    if (enabledTransitions.Count <= 0)
                        isStable = true;
                    else
                        Microstep(enabledTransitions);
                }

                if (events.Count <= 0 || events[0].Time > totalTime)
                {
                    noMoreEvents = true;
                    UpdateStates(totalTime - lastEventTime);
                    lastEventTime = totalTime;
                }
                else
                {
                    StateMachineEvent nextEvent = events.OrderBy(e => e.Time).First();

                    if (nextEvent.Time > totalTime)
                        break;

                    events.Remove(nextEvent);

                    TimeSpan timeTillNextEvent = nextEvent.Time - lastEventTime;
                    lastEventTime = nextEvent.Time;
                    UpdateStates(timeTillNextEvent);

                    var enabledTransitions = GetEnabledTransitions(nextEvent);
                    Microstep(enabledTransitions);
                }
            }
        }

        private void UpdateStates(TimeSpan elapsedTime)
        {
            for (int i = 0; i < configuration.Count; i++)
            {
                var activeState = configuration[i];
                activeState.Time += elapsedTime;
                configuration[i] = activeState;
                activeState.State.Update(this, elapsedTime);
            }
        }

        private void PostTimeTickEvent(TimeSpan totalEventTime)
        {
            events.Add(new StateMachineEvent(totalEventTime, this, null));
        }

        private void Microstep(IEnumerable<ITransition> enabledTransitions)
        {
            ExitStates(enabledTransitions);
            ExecuteTransitionContent(enabledTransitions);
            EnterStates(enabledTransitions);
        }

        private void ExitStates(IEnumerable<ITransition> enabledTransitions)
        {
            HashSet<IState> statesToExit = new HashSet<IState>(); // exit less than

            foreach (var transition in enabledTransitions)
            {
                if (transition.Target == null)
                    continue;

                var lca = transition.LeastCommonAncestor();

                foreach (var state in Configuration)
                {
                    if (state.IsDescendantOf(lca))
                        statesToExit.Add(state);
                }
            }

            foreach (var state in statesToExit)
            {
                state.Exit(this);
                configuration.RemoveAll(activeState => activeState.State == state);
            }
        }

        private void ExecuteTransitionContent(IEnumerable<ITransition> enabledTransitions)
        {
            foreach (var transition in enabledTransitions)
                transition.Execute(this, lastEventTime);
        }

        private void EnterStates(IEnumerable<ITransition> enabledTransitions)
        {
            HashSet<IState> statesToEnter = new HashSet<IState>();

            foreach (var transition in enabledTransitions)
            {
                if (transition.Target == null)
                    continue;

                var lca = transition.LeastCommonAncestor();

                AddStatesToEnter(transition.Target, lca, statesToEnter);
                
                if (lca != null && lca.IsParallel)
                {
                    foreach (var child in lca.Children)
                        AddStatesToEnter(child, lca, statesToEnter);
                }
            }

            foreach (var state in statesToEnter.Reverse())
            {
                configuration.Add(new ActiveState { State = state });
                state.Enter(this);

                foreach (var transition in state.Transitions)
                {
                    if (transition.IsDelayed /*&& transition.Trigger == null*/)
                        PostTimeTickEvent(lastEventTime + transition.Delay);
                }
            }
        }

        private void AddStatesToEnter(IState state, IState root, ISet<IState> statesToEnter)
        {
            if (state == root)
                return;

            statesToEnter.Add(state);

            if (state.IsParallel)
            {
                foreach (var child in state.Children)
                    AddStatesToEnter(child, state, statesToEnter);
            }
            else if (state.IsCompound)
            {
                if (state.InitialState != null)
                    AddStatesToEnter(state.InitialState, state, statesToEnter);
            }

            foreach (IState ancestor in state.Ancestors(root))
            {
                statesToEnter.Add(ancestor);
                if (ancestor.IsParallel)
                {
                    foreach (IState child in ancestor.Children)
                    {
                        if (statesToEnter.Any(stateToEnter => stateToEnter.IsDescendantOf(child)))
                            AddStatesToEnter(child, ancestor, statesToEnter);
                    }
                }
            }
        }

        private ICollection<ITransition> GetEnabledTransitions(StateMachineEvent e)
        {
            HashSet<ITransition> enabledTransitions = new HashSet<ITransition>();

            foreach (var state in Configuration)
            {
                // TODO: Check. Why would non-atomic states not be able to fire transitions?
                // if (!state.IsAtomic)
                //     continue;

                if (state.IsPreemptedBy(enabledTransitions))
                    continue;

                var transitions =
                    from ancestor in state.SelfAndAncestors(null)
                    from transition in ancestor.Transitions
                    where transition.CanExecute(e)
                    select transition;

                var firstTransition = transitions.FirstOrDefault();
                if (firstTransition != null)
                    enabledTransitions.Add(firstTransition);
            }

            return enabledTransitions;
        }
    }
}
