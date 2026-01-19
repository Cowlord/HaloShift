using System;
using SharpDX.XInput;

namespace HaloShift
{
    public enum AppMode
    {
        Controller,
        Mouse
    }

    public class ModeManager
    {
        private AppMode _currentMode = AppMode.Controller;
        public AppMode CurrentMode => _currentMode;

        public event EventHandler<ModeChangedEventArgs> ModeChanged;

        private const float TRIGGER_THRESHOLD = 0.5f; // Normalized: 0.0 to 1.0
        private bool _toggleButtonWasPressed = false;
        private bool _f11ButtonWasPressed = false;
        private bool _xButtonWasPressed = false;
        private bool _altF4ComboWasPressed = false;
        private bool _ctrlWComboWasPressed = false;
        private bool _bButtonWasPressed = false;

        public void Update(Gamepad gamepad)
        {
            // Check for mode toggle: LB + RB + Y
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool y = (gamepad.Buttons & GamepadButtonFlags.Y) != 0;
            bool allThreePressed = lb && rb && y;

            // Only trigger toggle on the transition from unpressed to pressed
            if (allThreePressed && !_toggleButtonWasPressed)
            {
                SwitchMode();
            }

            _toggleButtonWasPressed = allThreePressed;
        }

        public void SwitchMode()
        {
            AppMode newMode = _currentMode == AppMode.Controller ? AppMode.Mouse : AppMode.Controller;

            if (_currentMode != newMode)
            {
                _currentMode = newMode;
                ModeChanged?.Invoke(this, new ModeChangedEventArgs(newMode));
            }
        }

        public bool HandleMouseModeInput(Gamepad gamepad)
        {
            // Left Stick → move mouse
            HandleLeftStickMovement(gamepad);

            // RT → left click
            float rtTrigger = gamepad.RightTrigger / 255f;
            if (rtTrigger > TRIGGER_THRESHOLD)
            {
                InputSimulator.LeftClick();
            }

            // LT → right click
            float ltTrigger = gamepad.LeftTrigger / 255f;
            if (ltTrigger > TRIGGER_THRESHOLD)
            {
                InputSimulator.RightClick();
            }

            // Get button states
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool y = (gamepad.Buttons & GamepadButtonFlags.Y) != 0;
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool x = (gamepad.Buttons & GamepadButtonFlags.X) != 0;
            bool b = (gamepad.Buttons & GamepadButtonFlags.B) != 0;
            bool a = (gamepad.Buttons & GamepadButtonFlags.A) != 0;

            // Get trigger states
            float ltTrigger2 = gamepad.LeftTrigger / 255f;
            float rtTrigger2 = gamepad.RightTrigger / 255f;
            bool ltPressed = ltTrigger2 > TRIGGER_THRESHOLD;
            bool rtPressed = rtTrigger2 > TRIGGER_THRESHOLD;

            // X → Esc
            if (x && !_xButtonWasPressed)
            {
                // VK_ESCAPE = 0x1B
                InputSimulator.PressKey(0x1B);
            }
            _xButtonWasPressed = x;

            // LT + RT + B → Alt+F4
            bool altF4Combo = ltPressed && rtPressed && b;
            if (altF4Combo && !_altF4ComboWasPressed)
            {
                // Send Alt+F4
                InputSimulator.SendKey(0x12, true);  // Alt down
                System.Threading.Thread.Sleep(10);
                InputSimulator.SendKey(0x73, true);  // F4 down
                System.Threading.Thread.Sleep(50);
                InputSimulator.SendKey(0x73, false); // F4 up
                System.Threading.Thread.Sleep(10);
                InputSimulator.SendKey(0x12, false); // Alt up
                System.Threading.Thread.Sleep(10);
            }
            _altF4ComboWasPressed = altF4Combo;

            // LT + RT + A → Ctrl+W
            bool ctrlWCombo = ltPressed && rtPressed && a;
            if (ctrlWCombo && !_ctrlWComboWasPressed)
            {
                // Send Ctrl+W
                InputSimulator.SendKey(0x11, true);  // Ctrl down
                System.Threading.Thread.Sleep(10);
                InputSimulator.SendKey(0x57, true);  // W down
                System.Threading.Thread.Sleep(50);
                InputSimulator.SendKey(0x57, false); // W up
                System.Threading.Thread.Sleep(10);
                InputSimulator.SendKey(0x11, false); // Ctrl up
                System.Threading.Thread.Sleep(10);
            }
            _ctrlWComboWasPressed = ctrlWCombo;

            // B → Browser Back (only if not part of a combo)
            if (b && !ltPressed && !rtPressed && !_bButtonWasPressed)
            {
                // VK_BROWSER_BACK = 0xA6
                InputSimulator.PressKey(0xA6);
            }
            _bButtonWasPressed = b;

            // LB → send F11 (full-screen toggle) ONLY if not part of toggle combo
            bool lbAlone = lb && !rb && !y;

            // Only trigger F11 on the transition from unpressed to pressed
            if (lbAlone && !_f11ButtonWasPressed)
            {
                // VK_F11 = 0x7A
                InputSimulator.PressKey(0x7A);
            }

            _f11ButtonWasPressed = lbAlone;

            return true;
        }

        private void HandleLeftStickMovement(Gamepad gamepad)
        {
            const float DEADZONE = 0.15f; // Normalized: 0.0 to 1.0
            const float MAX_STICK_VALUE = 32767f;

            // Normalize stick values from -32768..32767 to -1.0..1.0
            float stickX = gamepad.LeftThumbX / MAX_STICK_VALUE;
            float stickY = gamepad.LeftThumbY / MAX_STICK_VALUE;

            // Apply deadzone
            if (Math.Abs(stickX) < DEADZONE)
                stickX = 0;
            if (Math.Abs(stickY) < DEADZONE)
                stickY = 0;

            // Apply smooth acceleration curve
            stickX = ApplyAccelerationCurve(stickX);
            stickY = ApplyAccelerationCurve(stickY);

            // Scale to pixel movement
            const float SENSITIVITY = 60f; // Pixels per frame (3000 DPI equivalent)
            int deltaX = (int)(stickX * SENSITIVITY);
            int deltaY = (int)(-stickY * SENSITIVITY); // Invert Y axis

            if (deltaX != 0 || deltaY != 0)
            {
                InputSimulator.MoveMouse(deltaX, deltaY);
            }
        }

        private float ApplyAccelerationCurve(float stick)
        {
            // Smooth acceleration: quadratic curve for more responsive control
            // s(x) = sign(x) * x^2
            return stick < 0 ? -(stick * stick) : stick * stick;
        }
    }

    public class ModeChangedEventArgs : EventArgs
    {
        public AppMode NewMode { get; set; }

        public ModeChangedEventArgs(AppMode newMode)
        {
            NewMode = newMode;
        }
    }
}
