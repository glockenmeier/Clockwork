using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Input;
using System;
using System.Threading.Tasks;

namespace Clockwork
{
    public class FreeCameraController : Script
    {
        public Camera Camera { get; set; }

        public FreeCameraController(IServiceRegistry registry) : base(registry)
        {
        }

        public override async Task Execute()
        {
            Vector2 oldMousePosition = Input.MousePosition;

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                var elapsedTime = Game.UpdateTime.Elapsed;

                var mouseDelta = Input.MousePosition - Vector2.One * 0.5f;//oldMousePosition;
                oldMousePosition = Input.MousePosition;

                if (Camera == null)
                    continue;

                Vector3 offset = Vector3.Zero;
                float yaw = 0;
                float pitch = 0;
                float zoom = 0;

                float movement = (float)elapsedTime.TotalSeconds * 10 * Vector3.Distance(Camera.Target, Camera.Position);

                if (Input.IsKeyDown(Keys.W))
                    offset.Z = -movement;

                if (Input.IsKeyDown(Keys.S))
                    offset.Z = movement;

                if (Input.IsKeyDown(Keys.Q))
                    offset.X = -movement;

                if (Input.IsKeyDown(Keys.E))
                    offset.X = movement;

                if (Input.IsKeyDown(Keys.PageDown))
                    offset.Y = -movement;

                if (Input.IsKeyDown(Keys.PageUp))
                    offset.Y = movement;

                if (Input.IsMouseButtonDown(MouseButton.Middle))
                {
                    offset.X = -10.0f * mouseDelta.X;
                    offset.Y = 10.0f * mouseDelta.Y;
                }

                zoom = (float)Math.Exp(0.005 * Input.MouseWheelDelta);

                /*if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    yaw = -2.0f * mouseDelta.X;
                    pitch = -2.0f * mouseDelta.Y;
                }*/
                yaw = -2.0f * mouseDelta.X;
                pitch = -2.0f * mouseDelta.Y;

                Camera.Orbit(yaw, pitch);
                Camera.Pan(offset);
                Camera.Zoom(zoom);
            }
        }
    }
}
