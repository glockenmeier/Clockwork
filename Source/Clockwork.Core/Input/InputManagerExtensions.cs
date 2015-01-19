using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Input;
using System;
using System.Runtime.InteropServices;

namespace Clockwork
{
    public static class InputManagerExtensions
    {
        public static void SetMousePosition(this InputManager inputManager, Vector2 position)
        {
            var clientSize = inputManager.Game.Window.ClientBounds.Size;
            Point point = new Point((int)(position.X * clientSize.Width), (int)(position.Y * clientSize.Height));
            ClientToScreen(GetActiveWindow(), ref point);
            SetCursorPos(point.X, point.Y);
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", EntryPoint = "ClientToScreen")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point position);

        [DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
        private static extern IntPtr GetActiveWindow();
    }
}
