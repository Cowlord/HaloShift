using SharpDX.XInput;
using System;
using Xunit;

namespace HaloShift.Tests
{
    public class ModeManagerTests : IDisposable
    {
        private readonly ModeManager _sut;

        public ModeManagerTests()
        {
            InputSimulator.Reset();
            ControlsWindow.Reset();
            _sut = new ModeManager();
        }

        public void Dispose()
        {
            InputSimulator.Reset();
            ControlsWindow.Reset();
        }

        // --- Initial State ---

        [Fact]
        public void InitialMode_IsController()
        {
            Assert.Equal(AppMode.Controller, _sut.CurrentMode);
        }

        [Fact]
        public void InitialSensitivity_IsDefault()
        {
            Assert.Equal(0.5f, _sut.MouseSensitivity);
        }

        // --- SetMouseSensitivity ---

        [Fact]
        public void SetMouseSensitivity_WithinRange_SetsValue()
        {
            _sut.SetMouseSensitivity(1.5f);
            Assert.Equal(1.5f, _sut.MouseSensitivity);
        }

        [Fact]
        public void SetMouseSensitivity_BelowMin_ClampsToMin()
        {
            _sut.SetMouseSensitivity(0.1f);
            Assert.Equal(0.5f, _sut.MouseSensitivity);
        }

        [Fact]
        public void SetMouseSensitivity_AboveMax_ClampsToMax()
        {
            _sut.SetMouseSensitivity(10.0f);
            Assert.Equal(3.0f, _sut.MouseSensitivity);
        }

        [Fact]
        public void SetMouseSensitivity_AtMin_SetsValue()
        {
            _sut.SetMouseSensitivity(0.5f);
            Assert.Equal(0.5f, _sut.MouseSensitivity);
        }

        [Fact]
        public void SetMouseSensitivity_AtMax_SetsValue()
        {
            _sut.SetMouseSensitivity(3.0f);
            Assert.Equal(3.0f, _sut.MouseSensitivity);
        }

        [Fact]
        public void SetMouseSensitivity_NegativeValue_ClampsToMin()
        {
            _sut.SetMouseSensitivity(-5.0f);
            Assert.Equal(0.5f, _sut.MouseSensitivity);
        }

        // --- SwitchMode ---

        [Fact]
        public void SwitchMode_FromController_GoesToMouse()
        {
            _sut.SwitchMode();
            Assert.Equal(AppMode.Mouse, _sut.CurrentMode);
        }

        [Fact]
        public void SwitchMode_FromMouse_GoesToController()
        {
            _sut.SwitchMode();
            _sut.SwitchMode();
            Assert.Equal(AppMode.Controller, _sut.CurrentMode);
        }

        [Fact]
        public void SwitchMode_TogglesTwice_ReturnsToOriginal()
        {
            var original = _sut.CurrentMode;
            _sut.SwitchMode();
            _sut.SwitchMode();
            Assert.Equal(original, _sut.CurrentMode);
        }

        [Fact]
        public void SwitchMode_FiresModeChangedEvent()
        {
            ModeChangedEventArgs? received = null;
            _sut.ModeChanged += (_, e) => received = e;

            _sut.SwitchMode();

            Assert.NotNull(received);
            Assert.Equal(AppMode.Mouse, received!.NewMode);
        }

        [Fact]
        public void SwitchMode_DefaultInitiator_IsGamepad()
        {
            ModeChangedEventArgs? received = null;
            _sut.ModeChanged += (_, e) => received = e;

            _sut.SwitchMode();

            Assert.Equal(ModeChangeInitiator.Gamepad, received!.Initiator);
        }

        [Fact]
        public void SwitchMode_UserMenuInitiator_IsReported()
        {
            ModeChangedEventArgs? received = null;
            _sut.ModeChanged += (_, e) => received = e;

            _sut.SwitchMode(ModeChangeInitiator.UserMenu);

            Assert.Equal(ModeChangeInitiator.UserMenu, received!.Initiator);
        }

        [Fact]
        public void SwitchMode_ToController_ReleasesMouseButtons()
        {
            _sut.SwitchMode(); // Controller → Mouse
            InputSimulator.Reset();

            // Simulate holding RT (left mouse button) via HandleMouseModePointerInput
            var gamepad = CreateGamepad(rightTrigger: 255);
            _sut.HandleMouseModePointerInput(gamepad);
            Assert.Contains("LeftMouseButtonDown", InputSimulator.CallLog);

            InputSimulator.Reset();
            _sut.SwitchMode(); // Mouse → Controller (should release held buttons)
            Assert.Contains("LeftMouseButtonUp", InputSimulator.CallLog);
        }

        // --- SuppressButtonEdgesUntilRelease ---

        [Fact]
        public void SuppressButtonEdgesUntilRelease_PreventsNextEdge()
        {
            _sut.SwitchMode(); // Go to Mouse mode
            InputSimulator.Reset();

            _sut.SuppressButtonEdgesUntilRelease();

            // Y button press should be suppressed (keyboard request)
            var gamepad = CreateGamepad(buttons: GamepadButtonFlags.Y);
            _sut.Update(gamepad);

            bool keyboardRequested = false;
            _sut.ShowKeyboardRequested += (_, __) => keyboardRequested = true;

            // Y is already held, so no edge transition → no keyboard request
            _sut.Update(gamepad);
            Assert.False(keyboardRequested);
        }

        // --- Update (View hold) ---

        [Fact]
        public void Update_ViewNotHeld_DoesNotSwitchMode()
        {
            var gamepad = CreateGamepad();
            _sut.Update(gamepad);
            Assert.Equal(AppMode.Controller, _sut.CurrentMode);
        }

        [Fact]
        public void Update_YButtonInMouseMode_RequestsKeyboard()
        {
            _sut.SwitchMode(); // Controller → Mouse

            bool keyboardRequested = false;
            _sut.ShowKeyboardRequested += (_, __) => keyboardRequested = true;

            // First frame: Y not pressed
            _sut.Update(CreateGamepad());
            // Second frame: Y pressed (edge)
            _sut.Update(CreateGamepad(buttons: GamepadButtonFlags.Y));

            Assert.True(keyboardRequested);
        }

        [Fact]
        public void Update_YButtonInControllerMode_DoesNotRequestKeyboard()
        {
            bool keyboardRequested = false;
            _sut.ShowKeyboardRequested += (_, __) => keyboardRequested = true;

            _sut.Update(CreateGamepad());
            _sut.Update(CreateGamepad(buttons: GamepadButtonFlags.Y));

            Assert.False(keyboardRequested);
        }

        [Fact]
        public void Update_YButtonHeld_OnlyFiresOnce()
        {
            _sut.SwitchMode();

            int count = 0;
            _sut.ShowKeyboardRequested += (_, __) => count++;

            _sut.Update(CreateGamepad());
            _sut.Update(CreateGamepad(buttons: GamepadButtonFlags.Y));
            _sut.Update(CreateGamepad(buttons: GamepadButtonFlags.Y)); // still held
            _sut.Update(CreateGamepad(buttons: GamepadButtonFlags.Y)); // still held

            Assert.Equal(1, count);
        }

        // --- HandleMouseModeInput (sensitivity) ---

        [Fact]
        public void HandleMouseModeInput_DPadUp_IncreasesSensitivity()
        {
            _sut.SwitchMode();
            float initial = _sut.MouseSensitivity;

            // First frame: no D-Pad
            _sut.HandleMouseModeInput(CreateGamepad());
            // Second frame: D-Pad Up pressed (edge)
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.DPadUp));

            Assert.True(_sut.MouseSensitivity > initial);
        }

        [Fact]
        public void HandleMouseModeInput_DPadDown_DecreasesNotBelowMin()
        {
            _sut.SwitchMode();
            _sut.SetMouseSensitivity(0.5f); // at min

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.DPadDown));

            Assert.Equal(0.5f, _sut.MouseSensitivity);
        }

        [Fact]
        public void HandleMouseModeInput_DPadUp_FiresSensitivityChanged()
        {
            _sut.SwitchMode();

            SensitivityChangedEventArgs? received = null;
            _sut.SensitivityChanged += (_, e) => received = e;

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.DPadUp));

            Assert.NotNull(received);
            Assert.True(received!.NewSensitivity > 0.5f);
        }

        [Fact]
        public void HandleMouseModeInput_DPadDown_FiresSensitivityChanged()
        {
            _sut.SwitchMode();
            _sut.SetMouseSensitivity(1.0f);

            SensitivityChangedEventArgs? received = null;
            _sut.SensitivityChanged += (_, e) => received = e;

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.DPadDown));

            Assert.NotNull(received);
            Assert.True(received!.NewSensitivity < 1.0f);
        }

        [Fact]
        public void HandleMouseModeInput_SensitivityPersistCallback_Called()
        {
            _sut.SwitchMode();

            float? persisted = null;
            _sut.OnSensitivityPersist = v => persisted = v;

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.DPadUp));

            Assert.NotNull(persisted);
        }

        // --- HandleMouseModeInput (button combos) ---

        [Fact]
        public void HandleMouseModeInput_XButton_SendsEscape()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.X));

            Assert.Contains("PressKey(0x1B)", InputSimulator.CallLog); // VK_ESCAPE
        }

        [Fact]
        public void HandleMouseModeInput_RightStickClick_SendsF5()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.RightThumb));

            Assert.Contains("PressKey(0x74)", InputSimulator.CallLog); // VK_F5
        }

        [Fact]
        public void HandleMouseModeInput_LeftStickClick_SendsWinKey()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.LeftThumb));

            Assert.Contains("PressKey(0x5B)", InputSimulator.CallLog); // VK_LWIN
        }

        [Fact]
        public void HandleMouseModeInput_LBAlone_SendsF11()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.LeftShoulder));

            Assert.Contains("PressKey(0x7A)", InputSimulator.CallLog); // VK_F11
        }

        [Fact]
        public void HandleMouseModeInput_LBPlusView_ShowsControls()
        {
            _sut.SwitchMode();

            bool controlsRequested = false;
            _sut.ShowControlsRequested += (_, __) => controlsRequested = true;

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(
                buttons: GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.Back));

            Assert.True(controlsRequested);
        }

        [Fact]
        public void HandleMouseModeInput_BButton_WhenControlsOpen_ClosesControls()
        {
            _sut.SwitchMode();
            ControlsWindow.IsOpen = true;
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.B));

            Assert.False(ControlsWindow.IsOpen);
        }

        [Fact]
        public void HandleMouseModeInput_BButton_WhenControlsClosed_SendsBrowserBack()
        {
            _sut.SwitchMode();
            ControlsWindow.IsOpen = false;
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(buttons: GamepadButtonFlags.B));

            Assert.Contains("PressKey(0xA6)", InputSimulator.CallLog); // VK_BROWSER_BACK
        }

        [Fact]
        public void HandleMouseModeInput_LTRTXCombo_SendsMiddleClick()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(
                buttons: GamepadButtonFlags.X, leftTrigger: 255, rightTrigger: 255));

            Assert.Contains("MiddleClick", InputSimulator.CallLog);
        }

        [Fact]
        public void HandleMouseModeInput_XWithTriggers_DoesNotSendEscape()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModeInput(CreateGamepad());
            _sut.HandleMouseModeInput(CreateGamepad(
                buttons: GamepadButtonFlags.X, leftTrigger: 255, rightTrigger: 255));

            Assert.DoesNotContain("PressKey(0x1B)", InputSimulator.CallLog);
        }

        // --- HandleMouseModePointerInput (trigger-based mouse buttons) ---

        [Fact]
        public void HandleMouseModePointerInput_RTOnly_LeftMouseDown()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModePointerInput(CreateGamepad(rightTrigger: 255));

            Assert.Contains("LeftMouseButtonDown", InputSimulator.CallLog);
        }

        [Fact]
        public void HandleMouseModePointerInput_LTOnly_RightMouseDown()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModePointerInput(CreateGamepad(leftTrigger: 255));

            Assert.Contains("RightMouseButtonDown", InputSimulator.CallLog);
        }

        [Fact]
        public void HandleMouseModePointerInput_BothTriggers_NoMouseButton()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModePointerInput(CreateGamepad(leftTrigger: 255, rightTrigger: 255));

            Assert.DoesNotContain("LeftMouseButtonDown", InputSimulator.CallLog);
            Assert.DoesNotContain("RightMouseButtonDown", InputSimulator.CallLog);
        }

        [Fact]
        public void HandleMouseModePointerInput_RTRelease_LeftMouseUp()
        {
            _sut.SwitchMode();

            _sut.HandleMouseModePointerInput(CreateGamepad(rightTrigger: 255));
            InputSimulator.Reset();
            _sut.HandleMouseModePointerInput(CreateGamepad());

            Assert.Contains("LeftMouseButtonUp", InputSimulator.CallLog);
        }

        [Fact]
        public void HandleMouseModePointerInput_LeftStickMovement_MovesMouse()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModePointerInput(CreateGamepad(leftThumbX: 32767));

            Assert.Contains(InputSimulator.CallLog, c => c.StartsWith("MoveMouse("));
        }

        [Fact]
        public void HandleMouseModePointerInput_LeftStickInDeadzone_NoMovement()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            // Values within 15% deadzone (0.15 * 32767 ≈ 4915)
            _sut.HandleMouseModePointerInput(CreateGamepad(leftThumbX: 3000));

            Assert.DoesNotContain(InputSimulator.CallLog, c => c.StartsWith("MoveMouse("));
        }

        [Fact]
        public void HandleMouseModePointerInput_RightStickScroll_TriggersWheel()
        {
            _sut.SwitchMode();
            InputSimulator.Reset();

            _sut.HandleMouseModePointerInput(CreateGamepad(rightThumbY: 32767));

            Assert.Contains(InputSimulator.CallLog, c => c.StartsWith("MouseWheel("));
        }

        // --- HandleMouseModeInput returns true ---

        [Fact]
        public void HandleMouseModeInput_ReturnsTrue()
        {
            _sut.SwitchMode();
            var result = _sut.HandleMouseModeInput(CreateGamepad());
            Assert.True(result);
        }

        // --- ReleaseHeldMouseButtons ---

        [Fact]
        public void ReleaseHeldMouseButtons_WhenNoButtonsHeld_DoesNothing()
        {
            InputSimulator.Reset();
            _sut.ReleaseHeldMouseButtons();
            Assert.Empty(InputSimulator.CallLog);
        }

        // --- Helper ---

        private static Gamepad CreateGamepad(
            GamepadButtonFlags buttons = GamepadButtonFlags.None,
            byte leftTrigger = 0,
            byte rightTrigger = 0,
            short leftThumbX = 0,
            short leftThumbY = 0,
            short rightThumbX = 0,
            short rightThumbY = 0)
        {
            return new Gamepad
            {
                Buttons = buttons,
                LeftTrigger = leftTrigger,
                RightTrigger = rightTrigger,
                LeftThumbX = leftThumbX,
                LeftThumbY = leftThumbY,
                RightThumbX = rightThumbX,
                RightThumbY = rightThumbY
            };
        }
    }
}
