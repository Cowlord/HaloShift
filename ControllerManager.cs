using System;
using SharpDX.XInput;

namespace HaloShift
{
    public class ControllerManager : IDisposable
    {
        private Controller _controller;
        private Gamepad _previousState;
        private bool _disposed = false;

        public event EventHandler<GamepadStateChangedEventArgs> StateChanged;

        public ControllerManager()
        {
            // Initialize the first controller (index 0)
            _controller = new Controller(UserIndex.One);
            _previousState = new Gamepad();
        }

        public void Update()
        {
            if (_controller?.IsConnected != true)
                return;

            var state = _controller.GetState().Gamepad;

            // Check if state changed
            if (state.Buttons != _previousState.Buttons ||
                state.LeftTrigger != _previousState.LeftTrigger ||
                state.RightTrigger != _previousState.RightTrigger ||
                state.LeftThumbX != _previousState.LeftThumbX ||
                state.LeftThumbY != _previousState.LeftThumbY ||
                state.RightThumbX != _previousState.RightThumbX ||
                state.RightThumbY != _previousState.RightThumbY)
            {
                StateChanged?.Invoke(this, new GamepadStateChangedEventArgs(state));
                _previousState = state;
            }
        }

        public Gamepad GetCurrentState()
        {
            return _controller?.IsConnected == true ? _controller.GetState().Gamepad : _previousState;
        }

        public bool IsConnected => _controller?.IsConnected == true;

        public void Dispose()
        {
            if (_disposed)
                return;

            // Controller from SharpDX doesn't need explicit disposal
            _disposed = true;
        }
    }

    public class GamepadStateChangedEventArgs : EventArgs
    {
        public Gamepad State { get; set; }

        public GamepadStateChangedEventArgs(Gamepad state)
        {
            State = state;
        }
    }
}
