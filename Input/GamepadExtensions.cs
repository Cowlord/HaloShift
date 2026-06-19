using SharpDX.XInput;

namespace HaloShift
{
    public static class GamepadExtensions
    {
        public static bool IsPressed(this Gamepad gamepad, GamepadButtonFlags button) =>
            (gamepad.Buttons & button) != 0;
    }
}
