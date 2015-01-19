using System;

namespace Clockwork.StateMachines
{
    public delegate bool TransitionGuard(Transition transition, IStateContext context);

    public class Transition : ITransition
    {
        public IState Source { get; set; }

        public IState Target { get; set; }

        public TimeSpan Delay { get; set; }

        public event EventHandler Executed;

        public bool IsDelayed
        {
            get { return Delay > TimeSpan.Zero; }
        }

        public bool HasTarget
        {
            get { return Target != null; }
        }

        public TransitionGuard Guard { get; set; }

        public bool CanExecute(StateMachineEvent e)
        {
            if (Source.ActiveTime < Delay)
                return false;

            if (Guard != null && !Guard(this, null))
                return false;

            if (!CanAcceptEvent(e))
                return false;

            return true;
        }

        public Transition()
        {
        }

        public Transition(IState target)
        {
            Target = target;
        }

        public Transition(IState target, TimeSpan delay)
        {
            Target = target;
            Delay = delay;
        }

        public void Notify(StateMachine stateMachine, object sender, EventArgs args)
        {
            stateMachine.PostEvent(this, args);
        }

        public void Execute(IStateContext context, TimeSpan time)
        {
            var executed = Executed;
            if (executed != null)
                executed(this, EventArgs.Empty);
        }

        protected virtual bool CanAcceptEvent(StateMachineEvent e)
        {
            return true;
        }
    }
}
