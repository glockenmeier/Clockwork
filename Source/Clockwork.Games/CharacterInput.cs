using BEPUphysics.Character;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Input;
using System.Threading.Tasks;

namespace Clockwork
{
    public class CharacterInput : Script
    {
        private IVirtualButton MoveX;
        private IVirtualButton MoveY;
        private IVirtualButton LookX;
        private IVirtualButton LookY;
        private IVirtualButton Zoom;

        private IVirtualButton Jump;
        private IVirtualButton Crouch;
        private IVirtualButton Walk;
        private IVirtualButton Sprint;

        private IVirtualButton ToggleLookFree;
        private IVirtualButton ToggleLookAround;

        public CharacterController Character { get; set; }

        public Camera Camera { get; set; }

        public CharacterInput(IServiceRegistry registry) : base(registry)
        {
            MoveX = new VirtualButtonTwoWay(VirtualButton.Keyboard.Q, VirtualButton.Keyboard.E);
            MoveY = new VirtualButtonTwoWay(VirtualButton.Keyboard.W, VirtualButton.Keyboard.S);
            LookX = VirtualButton.Mouse.PositionX;
            LookY = VirtualButton.Mouse.PositionY;
            Zoom = new VirtualButtonTwoWay(VirtualButton.Keyboard.PageDown, VirtualButton.Keyboard.PageUp);

            Jump = VirtualButton.Keyboard.Space;
            Crouch = VirtualButton.Keyboard.LeftCtrl;
            Walk = VirtualButton.Keyboard.LeftShift;
            Sprint = VirtualButton.Keyboard.LeftAlt;

            ToggleLookFree = VirtualButton.Keyboard.C;
            ToggleLookAround = VirtualButton.Keyboard.F;
        }

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                await Script.NextFrame();

                if (Input.IsKeyPressed(Keys.Space))
                    Character.Jump();

                Vector2 movementDirection = Vector2.Zero;

                if (Input.IsKeyDown(Keys.W))
                    movementDirection.Y = 1;
                else if (Input.IsKeyDown(Keys.S))
                    movementDirection.Y = -1;

                if (Input.IsKeyDown(Keys.E))
                    movementDirection.X = 1;
                else if (Input.IsKeyDown(Keys.Q))
                    movementDirection.X = -1;

                if (movementDirection != Vector2.Zero)
                    movementDirection.Normalize();

                Character.HorizontalMotionConstraint.MovementDirection = movementDirection;
            }
        }
    }
}
