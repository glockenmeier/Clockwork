using BEPUphysics;
using BEPUutilities.Threading;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Games;
using System.Diagnostics;

namespace Clockwork.Physics
{
    public class PhysicsSystem : GameSystem
    {
        private double previousTimeMeasurement;
        private double accumulatedPhysicsTime;
        private int accumulatedPhysicsFrames;
        private TaskParallelLooper parallelLooper;

        public Space Space { get; private set; }

        public double PhysicsTime { get; private set; }

        public PhysicsSystem(IServiceRegistry registry)
            : base(registry)
        {
            Enabled = true;
            
            parallelLooper = new TaskParallelLooper();

            Space = new Space(parallelLooper);
            Space.ForceUpdater.Gravity = new Vector3(0, -9.81f, 0);

            registry.AddService(typeof(PhysicsSystem), this);
        }

        public override void Update(GameTime gameTime)
        {
            double elapsedTime = gameTime.Elapsed.TotalSeconds;
            long startTime = Stopwatch.GetTimestamp();

            Space.Update((float)elapsedTime);

            long endTime = Stopwatch.GetTimestamp();
            accumulatedPhysicsTime += (endTime - startTime) / (double)Stopwatch.Frequency;
            accumulatedPhysicsFrames++;
            previousTimeMeasurement += elapsedTime;

            if (previousTimeMeasurement > 0.3f)
            {
                previousTimeMeasurement -= 0.3f;
                PhysicsTime = accumulatedPhysicsTime / accumulatedPhysicsFrames;
                accumulatedPhysicsTime = 0;
                accumulatedPhysicsFrames = 0;
            }
        }
    }
}
