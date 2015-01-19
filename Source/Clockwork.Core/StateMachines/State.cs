using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Clockwork.StateMachines
{
    public class State : IState
    {
        private ObservableCollection<IState> children;

        private ObservableCollection<ITransition> transitions;

        private IState parent;

        public bool IsActive { get; private set; }

        public TimeSpan ActiveTime { get; private set; }

        public IState Parent
        {
            get { return parent; }
            set
            {
                if (parent != value)
                {
                    parent = value;
                    OnParentChanged();
                }
            }
        }

        public bool IsAtomic
        {
            get { return children == null || children.Count == 0; }
        }

        public bool IsParallel { get; set; }

        public bool IsCompound
        {
            get { return !IsParallel && !IsAtomic; }
        }

        public IState InitialState { get; set; }

        public IList<IState> Children
        {
            get
            {
                EnsureChildren();
                return children;
            }
        }

        IEnumerable<IState> IState.Children
        {
            get { return children ?? Enumerable.Empty<IState>(); }
        }

        public IEnumerable<IState> Configuration
        {
            get
            {
                if (!IsActive)
                    yield break;

                if (children != null)
                {
                    foreach (IState child in children)
                        foreach (IState state in child.Configuration)
                            yield return state;
                }

                yield return this;
            }
        }

        public IList<ITransition> Transitions
        {
            get
            {
                EnsureTransitions();
                return transitions;
            }
        }

        IEnumerable<ITransition> IState.Transitions
        {
            get { return transitions ?? Enumerable.Empty<ITransition>(); }
        }

        public event EventHandler Entered;

        public event EventHandler Exited;

        public event EventHandler Updated;

        public void Enter(IStateContext context)
        {
            if (IsActive)
                return;

            IsActive = true;
            ActiveTime = TimeSpan.Zero;
            OnEnter(context);

            var entered = Entered;
            if (entered != null)
                entered(this, EventArgs.Empty);
        }

        public void Age(IStateContext context, TimeSpan elapsedTime)
        {
            if (!IsActive)
                return;

            ActiveTime += elapsedTime;
            OnUpdate(context, elapsedTime);

            var updated = Updated;
            if (updated != null)
                updated(this, EventArgs.Empty);

            if (children != null)
            {
                foreach (IState state in children)
                    state.Age(context, elapsedTime);
            }
        }

        public void Update(IStateContext context, TimeSpan elapsedTime)
        {
            OnUpdate(context, elapsedTime);

            var updated = Updated;
            if (updated != null)
                updated(this, EventArgs.Empty);
        }

        public void Exit(IStateContext context)
        {
            if (!IsActive)
                return;

            IsActive = false;
            ActiveTime = TimeSpan.Zero;
            OnExit(context);

            var exited = Exited;
            if (exited != null)
                exited(this, EventArgs.Empty);
        }

        protected virtual void OnEnter(IStateContext context) { }

        protected virtual void OnUpdate(IStateContext context, TimeSpan elapsedTime) { }

        protected virtual void OnExit(IStateContext context) { }

        protected virtual void OnParentChanged() { }

        private void EnsureChildren()
        {
            if (children != null)
                return;

            children = new ObservableCollection<IState>();

            children.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems != null)
                {
                    foreach (IState item in args.OldItems)
                    {
                        if (item.Parent == this)
                            item.Parent = null;

                        if (InitialState == item)
                            InitialState = children.FirstOrDefault();
                    }
                }

                if (args.NewItems != null)
                {
                    foreach (IState item in args.NewItems)
                        item.Parent = this;

                    if (args.NewItems.Count == children.Count)
                        InitialState = children.FirstOrDefault();
                }
            };
        }

        private void EnsureTransitions()
        {
            if (transitions != null)
                return;

            transitions = new ObservableCollection<ITransition>();

            transitions.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems != null)
                    foreach (ITransition item in args.OldItems)
                        item.Source = null;

                if (args.NewItems != null)
                    foreach (ITransition item in args.NewItems)
                        item.Source = this;
            };
        }
    }
}
