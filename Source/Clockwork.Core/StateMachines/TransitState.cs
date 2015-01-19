using System;
using System.Collections.Generic;
using System.Linq;

namespace Clockwork.StateMachines
{
    public class TransitState : IState, ITransition
    {
        public bool IsAtomic
        {
            get { return true; }
        }

        public bool IsParallel
        {
            get { return false; }
        }

        public bool IsCompound
        {
            get { return false; }
        }

        public IState Parent { get; set; }

        public IState InitialState
        {
            get { return null; }
        }

        public IEnumerable<IState> Children
        {
            get { return Enumerable.Empty<IState>(); }
        }

        public IEnumerable<ITransition> Transitions
        {
            get { yield return this; }
        }

        public IEnumerable<IState> Configuration
        {
            get { yield return this; }
        }

        public bool IsActive { get; private set; }

        public TimeSpan ActiveTime { get; private set; }

        public event EventHandler Entered;

        public event EventHandler Exited;

        public void Enter(IStateContext context)
        {
            if (IsActive)
                return;

            IsActive = true;

            var entered = Entered;
            if (entered != null) entered(this, EventArgs.Empty);
        }

        public void Age(IStateContext context, TimeSpan elapsedTime)
        {
            if (IsActive)
                ActiveTime += elapsedTime;
        }

        public void Update(IStateContext context, TimeSpan elapsedTime)
        {
        }

        public void Exit(IStateContext context)
        {
            if (!IsActive)
                return;

            IsActive = false;
            ActiveTime = TimeSpan.Zero;

            var exited = Exited;
            if (exited != null) exited(this, EventArgs.Empty);
        }

        public IState Source
        {
            get { return this; }
            set { throw new InvalidOperationException("TransitState acts as it's own implicit source state."); }
        }

        public IState Target { get; set; }

        public TimeSpan Delay { get; set; }

        public bool IsDelayed
        {
            get { return Delay > TimeSpan.Zero; }
        }

        public event EventHandler Executed;

        public bool CanExecute(StateMachineEvent e)
        {
            if (Delay > Source.ActiveTime)
                return false;

            return true;
        }

        public void Execute(IStateContext context, TimeSpan time)
        {
            if (!IsActive)
                return;

            var executed = Executed;
            if (executed != null) executed(this, EventArgs.Empty);
        }
    }
}
