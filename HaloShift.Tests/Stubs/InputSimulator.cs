using System.Collections.Generic;

namespace HaloShift
{
    /// <summary>
    /// Test stub replacing the real Win32 P/Invoke InputSimulator.
    /// Records all calls so tests can verify input actions.
    /// </summary>
    public static class InputSimulator
    {
        public static List<string> CallLog { get; } = new();

        public static void Reset() => CallLog.Clear();

        public static void MoveMouse(int deltaX, int deltaY)
            => CallLog.Add($"MoveMouse({deltaX},{deltaY})");

        public static void LeftClick()
            => CallLog.Add("LeftClick");

        public static void LeftMouseButtonDown()
            => CallLog.Add("LeftMouseButtonDown");

        public static void LeftMouseButtonUp()
            => CallLog.Add("LeftMouseButtonUp");

        public static void RightClick()
            => CallLog.Add("RightClick");

        public static void RightMouseButtonDown()
            => CallLog.Add("RightMouseButtonDown");

        public static void RightMouseButtonUp()
            => CallLog.Add("RightMouseButtonUp");

        public static void MiddleClick()
            => CallLog.Add("MiddleClick");

        public static void MouseWheel(int delta)
            => CallLog.Add($"MouseWheel({delta})");

        public static void SendKey(byte keyCode, bool keyDown)
            => CallLog.Add($"SendKey(0x{keyCode:X2},{keyDown})");

        public static void PressKey(byte keyCode, int holdMilliseconds = 10)
            => CallLog.Add($"PressKey(0x{keyCode:X2})");

        public static void GetMousePosition(out int x, out int y)
        {
            x = 0;
            y = 0;
        }
    }
}
