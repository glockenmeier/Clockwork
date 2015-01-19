using System.Collections.Generic;
using System.Linq;

namespace Clockwork.StateMachines
{
    public static class StateExtensions
    {
        public static bool IsDescendantOf(this IState state, IState suspectedAncestor)
        {
            return state.Ancestors(null).Contains(suspectedAncestor);
        }

        public static bool IsSelfOrDescendantOf(this IState state, IState suspectedAncestor)
        {
            return state == suspectedAncestor || state.Ancestors(null).Contains(suspectedAncestor);
        }

        public static IEnumerable<IState> Ancestors(this IState state, IState upperBound)
        {
            if (state.Parent == null || state.Parent == upperBound)
                return Enumerable.Empty<IState>();

            return state.Parent.SelfAndAncestors(upperBound);
        }

        public static IEnumerable<IState> SelfAndAncestors(this IState state, IState upperBound)
        {
            yield return state;

            foreach (IState ancestor in state.Ancestors(upperBound))
                yield return ancestor;
        }

        public static bool IsPreemptedBy(this IState state, ISet<ITransition> transitions)
        {
            foreach (var transition in transitions)
            {
                if (transition.Target == null)
                    continue;

                IState lca = transition.LeastCommonAncestor();

                return state.IsDescendantOf(lca);
            }

            return false;
        }

        public static IState LeastCommonAncestor(this ITransition transition)
        {
            if (transition.Target == null || transition.Source == null)
                return null;

            foreach (IState state in transition.Source.SelfAndAncestors(null))
            {
                if (transition.Target.IsSelfOrDescendantOf(state))
                    return state;
            }

            return null;
        }
    }
}
