using System;

namespace Clockwork.StateMachines
{
    public class StateMachineEvent : IComparable<StateMachineEvent>
    {
        public TimeSpan Time { get; set; }

        public object Sender { get; set; }

        public EventArgs Arguments { get; set; }

        public StateMachineEvent(object sender, EventArgs arguments)
        {
            Time = TimeSpan.MinValue;
            Sender = sender;
            Arguments = arguments;
        }

        public StateMachineEvent(TimeSpan time, object sender, EventArgs arguments)
        {
            Time = time;
            Sender = sender;
            Arguments = arguments;
        }

        public int CompareTo(StateMachineEvent other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}
