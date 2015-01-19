using System;

namespace Clockwork.StateMachines
{
    public interface ITransition
    {
        IState Source { get; set; }

        IState Target { get; }

        TimeSpan Delay { get; }

        bool IsDelayed { get; }

        event EventHandler Executed;

        bool CanExecute(StateMachineEvent e);

        void Execute(IStateContext context, TimeSpan time);
    }
}
