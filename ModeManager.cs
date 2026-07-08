using System;
using System.Diagnostics;
using System.Linq;
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
        private bool _esoAlternativeToggleWasPressed = false;

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

        /// <summary>
        /// Checks if eso64.exe is currently running
        /// </summary>
        private bool IsEsoRunning()
        {
            try
            {
                bool isRunning = Process.GetProcessesByName("eso64").Any();
                System.Diagnostics.Debug.WriteLine($"ESO process check: {isRunning}");
                return isRunning;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("ESO process check failed");
                return false;
            }
        }

        public void Update(Gamepad gamepad)
        {
            bool view = (gamepad.Buttons & GamepadButtonFlags.Back) != 0;
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool y = (gamepad.Buttons & GamepadButtonFlags.Y) != 0;
            bool esoRunning = IsEsoRunning();

            // Only allow View button toggle if ESO is not running
            if (view && !esoRunning)
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

            // Alternative toggle for ESO: LB + RB + View (works in both modes)
            bool esoAlternativeToggle = lb && rb && view && !y;
            if (esoAlternativeToggle && !_esoAlternativeToggleWasPressed && esoRunning)
            {
                System.Diagnostics.Debug.WriteLine($"ESO Alternative Toggle triggered - ESO running: {esoRunning}");
                SwitchMode(ModeChangeInitiator.Gamepad);
            }
            _esoAlternativeToggleWasPressed = esoAlternativeToggle;

            // Y button → show keyboard (only in mouse mode)
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
            _esoAlternativeToggleWasPressed = false;
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
            _esoAlternativeToggleWasPressed = true;
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
            bool view = (gamepad.Buttons & GamepadButtonFlags.Back) != 0;

            // LB + View → show controls (About)
            bool lbViewCombo = lb && view && !rb && !y;
            if (lbViewCombo && !_lbViewComboWasPressed)
                ShowControlsRequested?.Invoke(this, EventArgs.Empty);
            _lbViewComboWasPressed = lbViewCombo;

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
                InputSimulator.PressKey(VirtualKey.Escape);
            }
            _xButtonWasPressed = x && !ltPressed && !rtPressed;

            // LT + RT + B → Alt+F4
            bool altF4Combo = ltPressed && rtPressed && b;
            if (altF4Combo && !_altF4ComboWasPressed)
            {
                _ = Task.Run(() =>
                {
                    try { InputSimulator.SendKeyCombo(VirtualKey.F4, VirtualKey.Alt); }
                    catch { }
                });
            }
            _altF4ComboWasPressed = altF4Combo;

            // LT + RT + A → Ctrl+W
            bool ctrlWCombo = ltPressed && rtPressed && a;
            if (ctrlWCombo && !_ctrlWComboWasPressed)
            {
                _ = Task.Run(() =>
                {
                    try { InputSimulator.SendKeyCombo(VirtualKey.W, VirtualKey.Ctrl); }
                    catch { }
                });
            }
            _ctrlWComboWasPressed = ctrlWCombo;

            // B → close controls, or Browser Back (only if not part of a combo)
            if (b && !ltPressed && !rtPressed && !_bButtonWasPressed)
            {
                if (ControlsWindow.IsOpen)
                    ControlsWindow.CloseIfOpen();
                else
                    InputSimulator.PressKey(VirtualKey.BrowserBack);
            }
            _bButtonWasPressed = b;

            // Right Stick Click → F5
            if (rightStickClick && !_rightStickWasPressed)
            {
                InputSimulator.PressKey(VirtualKey.F5);
            }
            _rightStickWasPressed = rightStickClick;

            // Left Stick Click → Windows key
            if (leftStickClick && !_leftStickWasPressed)
            {
                InputSimulator.PressKey(VirtualKey.LWin);
            }
            _leftStickWasPressed = leftStickClick;

            // LB → send F11 (full-screen toggle) when not combined with RB, Y, or View
            bool lbAlone = lb && !rb && !y && !view;

            // Only trigger F11 on the transition from unpressed to pressed
            if (lbAlone && !_f11ButtonWasPressed)
            {
                InputSimulator.PressKey(VirtualKey.F11);
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
