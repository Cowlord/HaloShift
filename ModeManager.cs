using System;
using System.Threading.Tasks;
using SharpDX.XInput;

namespace HaloShift
{
    public enum AppMode
    {
        Controller,
        Mouse
    }

    /// <summary>Where a mode switch was initiated (used for tray-specific UX).</summary>
    public enum ModeChangeInitiator
    {
        Gamepad,
        UserMenu
    }

    public class ModeManager
    {
        private AppMode _currentMode = AppMode.Controller;
        public AppMode CurrentMode => _currentMode;

        public event EventHandler<ModeChangedEventArgs> ModeChanged;
        public event EventHandler<SensitivityChangedEventArgs> SensitivityChanged;
        public event EventHandler ShowKeyboardRequested;

        private const float TRIGGER_THRESHOLD = 0.5f; // Normalized: 0.0 to 1.0

        /// <summary>How long View+Menu must be held before the mode toggles (milliseconds).</summary>
        private const double ModeToggleHoldMs = 0.0;

        private DateTime? _viewMenuHoldStartedUtc;
        private bool _viewMenuHoldToggleFired;
        private bool _f11ButtonWasPressed = false;
        private bool _xButtonWasPressed = false;
        private bool _altF4ComboWasPressed = false;
        private bool _ctrlWComboWasPressed = false;
        private bool _bButtonWasPressed = false;
        private bool _middleClickComboWasPressed = false;
        private bool _rightStickWasPressed = false;
        private bool _leftStickWasPressed = false;
        private bool _dpadUpWasPressed = false;
        private bool _dpadDownWasPressed = false;
        private bool _yButtonWasPressed = false;

        private bool _prevWantLeftMouseDown;
        private bool _prevWantRightMouseDown;

        // Sensitivity settings
        private float _mouseSensitivity = 0.5f;
        private const float MIN_SENSITIVITY = 0.5f;
        private const float MAX_SENSITIVITY = 3.0f;
        private const float SENSITIVITY_STEP = 0.1f;

        public float MouseSensitivity => _mouseSensitivity;

        public void Update(Gamepad gamepad)
        {
            // Mode toggle: hold View (Back) + Menu (Start) — XInput names
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool y = (gamepad.Buttons & GamepadButtonFlags.Y) != 0;
            bool view = (gamepad.Buttons & GamepadButtonFlags.Back) != 0;
            bool menu = (gamepad.Buttons & GamepadButtonFlags.Start) != 0;
            bool viewMenuHeld = view && menu;

            if (viewMenuHeld)
            {
                if (_viewMenuHoldStartedUtc == null)
                    _viewMenuHoldStartedUtc = DateTime.UtcNow;
                else if (!_viewMenuHoldToggleFired &&
                         (DateTime.UtcNow - _viewMenuHoldStartedUtc.Value).TotalMilliseconds >= ModeToggleHoldMs)
                {
                    SwitchMode(ModeChangeInitiator.Gamepad);
                    _viewMenuHoldToggleFired = true;
                }
            }
            else
            {
                _viewMenuHoldStartedUtc = null;
                _viewMenuHoldToggleFired = false;
            }

            // Check for Y button alone (show keyboard) - only in Mouse mode
            bool yAlone = y && !lb && !rb;
            if (yAlone && !_yButtonWasPressed && _currentMode == AppMode.Mouse)
            {
                ShowKeyboardRequested?.Invoke(this, EventArgs.Empty);
            }
            _yButtonWasPressed = yAlone;
        }

        public void SwitchMode(ModeChangeInitiator initiator = ModeChangeInitiator.Gamepad)
        {
            AppMode newMode = _currentMode == AppMode.Controller ? AppMode.Mouse : AppMode.Controller;

            if (_currentMode != newMode)
            {
                if (newMode == AppMode.Controller)
                    ReleaseHeldMouseButtons();

                _currentMode = newMode;
                ModeChanged?.Invoke(this, new ModeChangedEventArgs(newMode, initiator));
            }
        }

        public void ReleaseHeldMouseButtons()
        {
            if (_prevWantLeftMouseDown)
            {
                InputSimulator.LeftMouseButtonUp();
                _prevWantLeftMouseDown = false;
            }

            if (_prevWantRightMouseDown)
            {
                InputSimulator.RightMouseButtonUp();
                _prevWantRightMouseDown = false;
            }
        }

        /// <summary>
        /// Sticks, scroll, and LT/RT mouse buttons only — safe to run while the virtual keyboard is open
        /// (it owns face buttons, bumpers for typing, and D-pad navigation).
        /// </summary>
        public void HandleMouseModePointerInput(Gamepad gamepad)
        {
            HandleLeftStickMovement(gamepad);
            HandleRightStickScroll(gamepad);

            float ltTrigger = gamepad.LeftTrigger / 255f;
            float rtTrigger = gamepad.RightTrigger / 255f;
            bool ltPressed = ltTrigger > TRIGGER_THRESHOLD;
            bool rtPressed = rtTrigger > TRIGGER_THRESHOLD;

            bool wantLeftMouseDown = rtPressed && !ltPressed;
            bool wantRightMouseDown = ltPressed && !rtPressed;

            if (wantLeftMouseDown && !_prevWantLeftMouseDown)
                InputSimulator.LeftMouseButtonDown();
            else if (!wantLeftMouseDown && _prevWantLeftMouseDown)
                InputSimulator.LeftMouseButtonUp();
            _prevWantLeftMouseDown = wantLeftMouseDown;

            if (wantRightMouseDown && !_prevWantRightMouseDown)
                InputSimulator.RightMouseButtonDown();
            else if (!wantRightMouseDown && _prevWantRightMouseDown)
                InputSimulator.RightMouseButtonUp();
            _prevWantRightMouseDown = wantRightMouseDown;
        }

        public bool HandleMouseModeInput(Gamepad gamepad)
        {
            HandleMouseModePointerInput(gamepad);

            // Trigger states for combos below (same thresholds as pointer mapping)
            float ltTrigger = gamepad.LeftTrigger / 255f;
            float rtTrigger = gamepad.RightTrigger / 255f;
            bool ltPressed = ltTrigger > TRIGGER_THRESHOLD;
            bool rtPressed = rtTrigger > TRIGGER_THRESHOLD;

            // Get button states
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool y = (gamepad.Buttons & GamepadButtonFlags.Y) != 0;
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool x = (gamepad.Buttons & GamepadButtonFlags.X) != 0;
            bool b = (gamepad.Buttons & GamepadButtonFlags.B) != 0;
            bool a = (gamepad.Buttons & GamepadButtonFlags.A) != 0;
            bool rightStickClick = (gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0;
            bool leftStickClick = (gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0;

            // Get D-Pad states
            bool dpadUp = (gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0;
            bool dpadDown = (gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0;

            // D-Pad Up → Increase sensitivity
            if (dpadUp && !_dpadUpWasPressed)
            {
                float newSensitivity = Math.Min(_mouseSensitivity + SENSITIVITY_STEP, MAX_SENSITIVITY);
                if (newSensitivity != _mouseSensitivity)
                {
                    _mouseSensitivity = newSensitivity;
                    RaiseSensitivityChanged(newSensitivity);
                }
            }
            _dpadUpWasPressed = dpadUp;

            // D-Pad Down → Decrease sensitivity
            if (dpadDown && !_dpadDownWasPressed)
            {
                float newSensitivity = Math.Max(_mouseSensitivity - SENSITIVITY_STEP, MIN_SENSITIVITY);
                if (newSensitivity != _mouseSensitivity)
                {
                    _mouseSensitivity = newSensitivity;
                    RaiseSensitivityChanged(newSensitivity);
                }
            }
            _dpadDownWasPressed = dpadDown;

            // LT + RT + X → Middle click
            bool middleClickCombo = ltPressed && rtPressed && x;
            if (middleClickCombo && !_middleClickComboWasPressed)
            {
                InputSimulator.MiddleClick();
            }
            _middleClickComboWasPressed = middleClickCombo;

            // X → Esc (only if not part of middle click combo)
            if (x && !ltPressed && !rtPressed && !_xButtonWasPressed)
            {
                // VK_ESCAPE = 0x1B
                InputSimulator.PressKey(0x1B);
            }
            _xButtonWasPressed = x && !ltPressed && !rtPressed;

            // LT + RT + B → Alt+F4
            bool altF4Combo = ltPressed && rtPressed && b;
            if (altF4Combo && !_altF4ComboWasPressed)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        InputSimulator.SendKey(0x12, true);  // Alt down
                        System.Threading.Thread.Sleep(10);
                        InputSimulator.SendKey(0x73, true);  // F4 down
                        System.Threading.Thread.Sleep(50);
                        InputSimulator.SendKey(0x73, false); // F4 up
                        System.Threading.Thread.Sleep(10);
                        InputSimulator.SendKey(0x12, false); // Alt up
                        System.Threading.Thread.Sleep(10);
                    }
                    catch
                    {
                        // Ignore; combo is best-effort
                    }
                });
            }
            _altF4ComboWasPressed = altF4Combo;

            // LT + RT + A → Ctrl+W
            bool ctrlWCombo = ltPressed && rtPressed && a;
            if (ctrlWCombo && !_ctrlWComboWasPressed)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        InputSimulator.SendKey(0x11, true);  // Ctrl down
                        System.Threading.Thread.Sleep(10);
                        InputSimulator.SendKey(0x57, true);  // W down
                        System.Threading.Thread.Sleep(50);
                        InputSimulator.SendKey(0x57, false); // W up
                        System.Threading.Thread.Sleep(10);
                        InputSimulator.SendKey(0x11, false); // Ctrl up
                        System.Threading.Thread.Sleep(10);
                    }
                    catch
                    {
                        // Ignore; combo is best-effort
                    }
                });
            }
            _ctrlWComboWasPressed = ctrlWCombo;

            // B → Browser Back (only if not part of a combo)
            if (b && !ltPressed && !rtPressed && !_bButtonWasPressed)
            {
                // VK_BROWSER_BACK = 0xA6
                InputSimulator.PressKey(0xA6);
            }
            _bButtonWasPressed = b;

            // Right Stick Click → F5
            if (rightStickClick && !_rightStickWasPressed)
            {
                // VK_F5 = 0x74
                InputSimulator.PressKey(0x74);
            }
            _rightStickWasPressed = rightStickClick;

            // Left Stick Click → Windows key
            if (leftStickClick && !_leftStickWasPressed)
            {
                // VK_LWIN = 0x5B
                InputSimulator.PressKey(0x5B);
            }
            _leftStickWasPressed = leftStickClick;

            // LB → send F11 (full-screen toggle) when not combined with RB or Y
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

            // Scale to pixel movement with sensitivity multiplier
            const float BASE_SENSITIVITY = 60f; // Pixels per frame (3000 DPI equivalent)
            float finalSensitivity = BASE_SENSITIVITY * _mouseSensitivity;
            int deltaX = (int)(stickX * finalSensitivity);
            int deltaY = (int)(-stickY * finalSensitivity); // Invert Y axis

            if (deltaX != 0 || deltaY != 0)
            {
                InputSimulator.MoveMouse(deltaX, deltaY);
            }
        }

        private void HandleRightStickScroll(Gamepad gamepad)
        {
            const float DEADZONE = 0.15f; // Normalized: 0.0 to 1.0
            const float MAX_STICK_VALUE = 32767f;
            const float SCROLL_SPEED = 120f; // Windows standard scroll delta is 120

            // Normalize stick Y value from -32768..32767 to -1.0..1.0
            float stickY = gamepad.RightThumbY / MAX_STICK_VALUE;

            // Apply deadzone
            if (Math.Abs(stickY) < DEADZONE)
                return;

            // Apply smooth scrolling with acceleration curve
            float scrollAmount = ApplyAccelerationCurve(stickY) * SCROLL_SPEED;
            int scrollDelta = (int)scrollAmount;

            if (scrollDelta != 0)
            {
                InputSimulator.MouseWheel(scrollDelta);
            }
        }

        private float ApplyAccelerationCurve(float stick)
        {
            // Smooth acceleration: quadratic curve for more responsive control
            // s(x) = sign(x) * x^2
            return stick < 0 ? -(stick * stick) : stick * stick;
        }

        private void RaiseSensitivityChanged(float newValue)
        {
            SensitivityChanged?.Invoke(this, new SensitivityChangedEventArgs(newValue));
        }
    }

    public class ModeChangedEventArgs : EventArgs
    {
        public AppMode NewMode { get; set; }

        public ModeChangeInitiator Initiator { get; set; }

        public ModeChangedEventArgs(AppMode newMode, ModeChangeInitiator initiator = ModeChangeInitiator.Gamepad)
        {
            NewMode = newMode;
            Initiator = initiator;
        }
    }

    public class SensitivityChangedEventArgs : EventArgs
    {
        public float NewSensitivity { get; }

        public SensitivityChangedEventArgs(float newSensitivity)
        {
            NewSensitivity = newSensitivity;
        }
    }
}
