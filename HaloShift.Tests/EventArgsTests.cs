using Xunit;

namespace HaloShift.Tests
{
    public class ModeChangedEventArgsTests
    {
        [Fact]
        public void Constructor_SetsNewMode()
        {
            var args = new ModeChangedEventArgs(AppMode.Mouse);
            Assert.Equal(AppMode.Mouse, args.NewMode);
        }

        [Fact]
        public void Constructor_DefaultInitiator_IsGamepad()
        {
            var args = new ModeChangedEventArgs(AppMode.Mouse);
            Assert.Equal(ModeChangeInitiator.Gamepad, args.Initiator);
        }

        [Fact]
        public void Constructor_WithInitiator_SetsInitiator()
        {
            var args = new ModeChangedEventArgs(AppMode.Controller, ModeChangeInitiator.UserMenu);
            Assert.Equal(ModeChangeInitiator.UserMenu, args.Initiator);
        }

        [Fact]
        public void NewMode_IsSettable()
        {
            var args = new ModeChangedEventArgs(AppMode.Controller);
            args.NewMode = AppMode.Mouse;
            Assert.Equal(AppMode.Mouse, args.NewMode);
        }

        [Fact]
        public void Initiator_IsSettable()
        {
            var args = new ModeChangedEventArgs(AppMode.Controller);
            args.Initiator = ModeChangeInitiator.UserMenu;
            Assert.Equal(ModeChangeInitiator.UserMenu, args.Initiator);
        }
    }

    public class SensitivityChangedEventArgsTests
    {
        [Fact]
        public void Constructor_SetsNewSensitivity()
        {
            var args = new SensitivityChangedEventArgs(1.5f);
            Assert.Equal(1.5f, args.NewSensitivity);
        }

        [Fact]
        public void NewSensitivity_IsReadOnly()
        {
            var args = new SensitivityChangedEventArgs(2.0f);
            Assert.Equal(2.0f, args.NewSensitivity);
        }

        [Theory]
        [InlineData(0.5f)]
        [InlineData(1.0f)]
        [InlineData(3.0f)]
        public void Constructor_AcceptsVariousValues(float value)
        {
            var args = new SensitivityChangedEventArgs(value);
            Assert.Equal(value, args.NewSensitivity);
        }
    }

    public class AppModeEnumTests
    {
        [Fact]
        public void AppMode_HasControllerAndMouse()
        {
            Assert.Equal(0, (int)AppMode.Controller);
            Assert.Equal(1, (int)AppMode.Mouse);
        }
    }

    public class ModeChangeInitiatorEnumTests
    {
        [Fact]
        public void ModeChangeInitiator_HasGamepadAndUserMenu()
        {
            Assert.Equal(0, (int)ModeChangeInitiator.Gamepad);
            Assert.Equal(1, (int)ModeChangeInitiator.UserMenu);
        }
    }
}
