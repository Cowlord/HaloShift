using System;
using SharpDX.XInput;

namespace HaloShift
{
    public class ControllerManager : IDisposable
    {
        private readonly Controller[] _controllers = new Controller[4];
        private int _activeIndex;
        private Gamepad _previousState;
        private Gamepad _currentState;
        private bool _isConnected;
        private bool _disposed = false;

        public event EventHandler<GamepadStateChangedEventArgs>? StateChanged;

        public ControllerManager()
        {
            for (int i = 0; i < 4; i++)
                _controllers[i] = new Controller((UserIndex)i);

            SelectFirstConnectedSlot();
            _previousState = new Gamepad();
            _currentState = new Gamepad();
            _isConnected = Active.IsConnected;
        }

        private Controller Active => _controllers[_activeIndex];

        /// <summary>Selects the lowest-index XInput slot that has a controller connected.</summary>
        private void SelectFirstConnectedSlot()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_controllers[i].IsConnected)
                {
                    _activeIndex = i;
                    return;
                }
            }
        }

        public void Update()
        {
            if (!Active.IsConnected)
                SelectFirstConnectedSlot();

            if (!Active.IsConnected)
            {
                _isConnected = false;
                _currentState = new Gamepad();
                _previousState = new Gamepad();
                return;
            }

            _isConnected = true;
            var state = Active.GetState().Gamepad;
            _currentState = state;

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
            return _isConnected ? _currentState : new Gamepad();
        }

        public bool IsConnected => _isConnected;

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
