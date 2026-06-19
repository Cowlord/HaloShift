using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HaloShift
{
    public static class InputSimulator
    {
        private static readonly int InputSize = Marshal.SizeOf(typeof(INPUT));

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint Type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        [DllImport("user32.dll")]
        public static extern short VkKeyScan(char ch);

        private static void DispatchInputs(INPUT[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return;

            uint sent = SendInput((uint)inputs.Length, inputs, InputSize);
            if (sent != (uint)inputs.Length)
            {
                int err = Marshal.GetLastWin32Error();
                Debug.WriteLine($"SendInput sent {sent} of {inputs.Length} inputs; Win32 error {err}.");
            }
        }

        private static INPUT CreateMouseInput(uint flags, int dx = 0, int dy = 0, uint mouseData = 0)
        {
            return new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = dx,
                        dy = dy,
                        mouseData = mouseData,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        public static void MoveMouse(int deltaX, int deltaY)
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_MOVE, deltaX, deltaY) });
        }

        public static void LeftClick()
        {
            DispatchInputs(new[] {
                CreateMouseInput(MOUSEEVENTF_LEFTDOWN),
                CreateMouseInput(MOUSEEVENTF_LEFTUP)
            });
        }

        public static void LeftMouseButtonDown()
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_LEFTDOWN) });
        }

        public static void LeftMouseButtonUp()
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_LEFTUP) });
        }

        public static void RightClick()
        {
            DispatchInputs(new[] {
                CreateMouseInput(MOUSEEVENTF_RIGHTDOWN),
                CreateMouseInput(MOUSEEVENTF_RIGHTUP)
            });
        }

        public static void RightMouseButtonDown()
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_RIGHTDOWN) });
        }

        public static void RightMouseButtonUp()
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_RIGHTUP) });
        }

        public static void MiddleClick()
        {
            DispatchInputs(new[] {
                CreateMouseInput(MOUSEEVENTF_MIDDLEDOWN),
                CreateMouseInput(MOUSEEVENTF_MIDDLEUP)
            });
        }

        public static void MouseWheel(int delta)
        {
            DispatchInputs(new[] { CreateMouseInput(MOUSEEVENTF_WHEEL, mouseData: (uint)delta) });
        }

        public static void SendKey(byte keyCode, bool keyDown)
        {
            uint flags = keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;
            if (keyCode == VirtualKey.LWin)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            var input = new INPUT
            {
                Type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            DispatchInputs(new[] { input });
        }

        public static void PressKey(byte keyCode, int holdMilliseconds = 10)
        {
            SendKey(keyCode, true);
            if (holdMilliseconds > 0)
                System.Threading.Thread.Sleep(holdMilliseconds);
            SendKey(keyCode, false);
        }

        public static void SendKeyComboAsync(byte modifierKey, byte mainKey)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    SendKey(modifierKey, true);
                    System.Threading.Thread.Sleep(10);
                    SendKey(mainKey, true);
                    System.Threading.Thread.Sleep(50);
                    SendKey(mainKey, false);
                    System.Threading.Thread.Sleep(10);
                    SendKey(modifierKey, false);
                    System.Threading.Thread.Sleep(10);
                }
                catch
                {
                }
            });
        }

        public static void GetMousePosition(out int x, out int y)
        {
            GetCursorPos(out POINT point);
            x = point.X;
            y = point.Y;
        }
    }
}
