using System;
using System.Collections.Generic;

namespace Clockwork.StateMachines
{
    public interface IState
    {
        bool IsAtomic { get; }

        bool IsParallel { get; }

        bool IsCompound { get; }

        IState Parent { get; set; }

        IState InitialState { get; }

        IEnumerable<IState> Children { get; }

        IEnumerable<ITransition> Transitions { get; }

        IEnumerable<IState> Configuration { get; }

        bool IsActive { get; }

        TimeSpan ActiveTime { get; }

        event EventHandler Entered;

        event EventHandler Exited;

        void Enter(IStateContext context);

        void Age(IStateContext context, TimeSpan elapsedTime);

        void Update(IStateContext context, TimeSpan elapsedTime);

        void Exit(IStateContext context);
    }
}
