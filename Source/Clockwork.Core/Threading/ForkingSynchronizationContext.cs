using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Paradox;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clockwork.Threading
{
    public class ForkingSynchronizationContext : SynchronizationContext
    {
        private readonly MicroThread microThread;
        public readonly SynchronizationContext MicroThreadSynchronizationContext;

        public ForkingSynchronizationContext(MicroThread microThread, SynchronizationContext microthreadSyncContext)
        {
            this.microThread = microThread;
            MicroThreadSynchronizationContext = microthreadSyncContext;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (microThread.Scheduler.RunningMicroThread == microThread)
            {
                d(state);
            }
            else if (microThread.State == MicroThreadState.Completed || true /* microThread.ScheduledLinkedListNode.Index != -1 */) // TODO: Find workaround?
            {
                microThread.Scheduler.Add(() =>
                {
                    SetSynchronizationContext(this);
                    d(state);
                    return Task.FromResult(0);
                });
            }
            else
            {
                MicroThreadSynchronizationContext.Post(d, state);
            }
        }
    }

    public static class MicroThreadExtensions
    {
        public static MicroThread AddForking(this ScriptSystem scriptSystem, Func<Task> microThreadFunction)
        {
            return scriptSystem.Scheduler.AddForking(microThreadFunction);
        }

        public static MicroThread AddForking(this ScriptSystem scriptSystem, IScript script)
        {
            return scriptSystem.Scheduler.AddForking(script.Execute);
        }

        public static MicroThread AddForking(this Scheduler scheduler, Func<Task> microThreadFunction, MicroThreadFlags flags = MicroThreadFlags.None)
        {
            return scheduler.Add(async () =>
                {
                    var microthreadSyncContext = SynchronizationContext.Current;
                    var synchronizationContext = new ForkingSynchronizationContext(MicroThread.Current, microthreadSyncContext);
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                    await microThreadFunction();
                });
        }
    }
}
