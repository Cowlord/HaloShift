using System;
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

        public event EventHandler<ModeChangedEventArgs>? ModeChanged;
        public event EventHandler<SensitivityChangedEventArgs>? SensitivityChanged;
        public event EventHandler? ShowKeyboardRequested;
        public event EventHandler? ShowControlsRequested;

        public Action<float>? OnSensitivityPersist { get; set; }

        private const float TRIGGER_THRESHOLD = 0.5f; // Normalized: 0.0 to 1.0

        private DateTime _viewHeldSince = DateTime.MinValue;
        private bool _toggleFiredThisHold = false;
        private const double TOGGLE_HOLD_MS = 700;
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
        private bool _lbViewComboWasPressed = false;

        private bool _prevWantLeftMouseDown;
        private bool _prevWantRightMouseDown;

        // Sensitivity settings
        private float _mouseSensitivity = 0.5f;
        private const float MIN_SENSITIVITY = 0.5f;
        private const float MAX_SENSITIVITY = 3.0f;
        private const float SENSITIVITY_STEP = 0.1f;

        public float MouseSensitivity => _mouseSensitivity;

        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = Math.Clamp(sensitivity, MIN_SENSITIVITY, MAX_SENSITIVITY);
        }

        public void Update(Gamepad gamepad)
        {
            bool view = gamepad.IsPressed(GamepadButtonFlags.Back);

            if (view)
            {
                if (_viewHeldSince == DateTime.MinValue)
                    _viewHeldSince = DateTime.UtcNow;

                if (!_toggleFiredThisHold &&
                    (DateTime.UtcNow - _viewHeldSince).TotalMilliseconds >= TOGGLE_HOLD_MS)
                {
                    SwitchMode(ModeChangeInitiator.Gamepad);
                    _toggleFiredThisHold = true;
                }
            }
            else
            {
                _viewHeldSince = DateTime.MinValue;
                _toggleFiredThisHold = false;
            }

            bool y = gamepad.IsPressed(GamepadButtonFlags.Y);
            if (y && !_yButtonWasPressed && _currentMode == AppMode.Mouse)
            {
                ShowKeyboardRequested?.Invoke(this, EventArgs.Empty);
            }
            _yButtonWasPressed = y;
        }

        public void SwitchMode(ModeChangeInitiator initiator = ModeChangeInitiator.Gamepad)
        {
            AppMode newMode = _currentMode == AppMode.Controller ? AppMode.Mouse : AppMode.Controller;

            if (_currentMode != newMode)
            {
                if (newMode == AppMode.Controller)
                    ReleaseHeldMouseButtons();

                ResetTransientButtonStates();

                _currentMode = newMode;
                ModeChanged?.Invoke(this, new ModeChangedEventArgs(newMode, initiator));
            }
        }

        private void ResetTransientButtonStates()
        {
            _viewHeldSince = DateTime.MinValue;
            _toggleFiredThisHold = false;
            _f11ButtonWasPressed = false;
            _xButtonWasPressed = false;
            _altF4ComboWasPressed = false;
            _ctrlWComboWasPressed = false;
            _bButtonWasPressed = false;
            _middleClickComboWasPressed = false;
            _rightStickWasPressed = false;
            _leftStickWasPressed = false;
            _dpadUpWasPressed = false;
            _dpadDownWasPressed = false;
            _yButtonWasPressed = false;
            _lbViewComboWasPressed = false;
            _prevWantLeftMouseDown = false;
            _prevWantRightMouseDown = false;
        }

        /// <summary>
        /// After the virtual keyboard closes, ignore held buttons until they are released
        /// (e.g. B was used to dismiss the keyboard and must not trigger mouse-mode actions).
        /// </summary>
        public void SuppressButtonEdgesUntilRelease()
        {
            _f11ButtonWasPressed = true;
            _xButtonWasPressed = true;
            _altF4ComboWasPressed = true;
            _ctrlWComboWasPressed = true;
            _bButtonWasPressed = true;
            _middleClickComboWasPressed = true;
            _rightStickWasPressed = true;
            _leftStickWasPressed = true;
            _dpadUpWasPressed = true;
            _dpadDownWasPressed = true;
            _yButtonWasPressed = true;
            _lbViewComboWasPressed = true;
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

        private (bool LtPressed, bool RtPressed) ParseTriggers(Gamepad gamepad)
        {
            float lt = gamepad.LeftTrigger / 255f;
            float rt = gamepad.RightTrigger / 255f;
            return (lt > TRIGGER_THRESHOLD, rt > TRIGGER_THRESHOLD);
        }

        private static float NormalizeStick(short rawValue, float deadzone)
        {
            const float MAX_STICK_VALUE = 32767f;
            float normalized = rawValue / MAX_STICK_VALUE;
            return Math.Abs(normalized) < deadzone ? 0f : normalized;
        }

        /// <summary>
        /// Sticks, scroll, and LT/RT mouse buttons only — safe to run while the virtual keyboard is open
        /// (it owns face buttons, bumpers for typing, and D-pad navigation).
        /// </summary>
        public void HandleMouseModePointerInput(Gamepad gamepad)
        {
            HandleLeftStickMovement(gamepad);
            HandleRightStickScroll(gamepad);

            var (ltPressed, rtPressed) = ParseTriggers(gamepad);

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

            var (ltPressed, rtPressed) = ParseTriggers(gamepad);

            bool rb = gamepad.IsPressed(GamepadButtonFlags.RightShoulder);
            bool y = gamepad.IsPressed(GamepadButtonFlags.Y);
            bool lb = gamepad.IsPressed(GamepadButtonFlags.LeftShoulder);
            bool x = gamepad.IsPressed(GamepadButtonFlags.X);
            bool b = gamepad.IsPressed(GamepadButtonFlags.B);
            bool a = gamepad.IsPressed(GamepadButtonFlags.A);
            bool rightStickClick = gamepad.IsPressed(GamepadButtonFlags.RightThumb);
            bool leftStickClick = gamepad.IsPressed(GamepadButtonFlags.LeftThumb);
            bool view = gamepad.IsPressed(GamepadButtonFlags.Back);

            // LB + View → show controls (About)
            bool lbViewCombo = lb && view && !rb && !y;
            if (lbViewCombo && !_lbViewComboWasPressed)
                ShowControlsRequested?.Invoke(this, EventArgs.Empty);
            _lbViewComboWasPressed = lbViewCombo;

            bool dpadUp = gamepad.IsPressed(GamepadButtonFlags.DPadUp);
            bool dpadDown = gamepad.IsPressed(GamepadButtonFlags.DPadDown);

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
                InputSimulator.SendKeyComboAsync(VirtualKey.Alt, VirtualKey.F4);
            _altF4ComboWasPressed = altF4Combo;

            // LT + RT + A → Ctrl+W
            bool ctrlWCombo = ltPressed && rtPressed && a;
            if (ctrlWCombo && !_ctrlWComboWasPressed)
                InputSimulator.SendKeyComboAsync(VirtualKey.Ctrl, VirtualKey.W);
            _ctrlWComboWasPressed = ctrlWCombo;

            // B → close controls, or Browser Back (only if not part of a combo)
            if (b && !ltPressed && !rtPressed && !_bButtonWasPressed)
            {
                if (ControlsWindow.IsOpen)
                    ControlsWindow.CloseIfOpen();
                else
                    InputSimulator.PressKey(0xA6); // VK_BROWSER_BACK
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

            // LB → send F11 (full-screen toggle) when not combined with RB, Y, or View
            bool lbAlone = lb && !rb && !y && !view;

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
            const float DEADZONE = 0.15f;

            float stickX = NormalizeStick(gamepad.LeftThumbX, DEADZONE);
            float stickY = NormalizeStick(gamepad.LeftThumbY, DEADZONE);

            stickX = ApplyAccelerationCurve(stickX);
            stickY = ApplyAccelerationCurve(stickY);

            const float BASE_SENSITIVITY = 60f;
            float finalSensitivity = BASE_SENSITIVITY * _mouseSensitivity;
            int deltaX = (int)(stickX * finalSensitivity);
            int deltaY = (int)(-stickY * finalSensitivity);

            if (deltaX != 0 || deltaY != 0)
                InputSimulator.MoveMouse(deltaX, deltaY);
        }

        private void HandleRightStickScroll(Gamepad gamepad)
        {
            const float DEADZONE = 0.15f;
            const float SCROLL_SPEED = 120f;

            float stickY = NormalizeStick(gamepad.RightThumbY, DEADZONE);
            if (stickY == 0f)
                return;

            int scrollDelta = (int)(ApplyAccelerationCurve(stickY) * SCROLL_SPEED);
            if (scrollDelta != 0)
                InputSimulator.MouseWheel(scrollDelta);
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
            OnSensitivityPersist?.Invoke(newValue);
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
