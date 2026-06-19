using System;
using System.Runtime.InteropServices;

namespace HaloShift
{
    public static class InputSimulator
    {
        private static readonly int InputSize = Marshal.SizeOf(typeof(INPUT));

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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

        private static void DispatchInputs(INPUT[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return;

            SendInput((uint)inputs.Length, inputs, InputSize);
        }

        public static void MoveMouse(int deltaX, int deltaY)
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = deltaX,
                        dy = deltaY,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MOVE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            DispatchInputs(new[] { input });
        }

        public static void LeftMouseButtonDown()
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTDOWN,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            DispatchInputs(new[] { input });
        }

        public static void LeftMouseButtonUp()
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            DispatchInputs(new[] { input });
        }

        public static void RightMouseButtonDown()
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_RIGHTDOWN,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            DispatchInputs(new[] { input });
        }

        public static void RightMouseButtonUp()
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_RIGHTUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            DispatchInputs(new[] { input });
        }

        public static void MiddleClick()
        {
            var downInput = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MIDDLEDOWN,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var upInput = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MIDDLEUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            DispatchInputs(new[] { downInput, upInput });
        }

        public static void MouseWheel(int delta)
        {
            var input = new INPUT
            {
                Type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)delta,
                        dwFlags = MOUSEEVENTF_WHEEL,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            DispatchInputs(new[] { input });
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

        /// <summary>
        /// Sends a key combination (modifier(s) + key) with proper timing.
        /// </summary>
        public static void SendKeyCombo(byte key, params byte[] modifiers)
        {
            foreach (var mod in modifiers)
                SendKey(mod, true);
            System.Threading.Thread.Sleep(10);

            SendKey(key, true);
            System.Threading.Thread.Sleep(50);
            SendKey(key, false);

            System.Threading.Thread.Sleep(10);
            for (int i = modifiers.Length - 1; i >= 0; i--)
                SendKey(modifiers[i], false);
        }

    }
}
