using System;
using SharpDX.XInput;

namespace HaloShift
{
    public class ControllerManager : IDisposable
    {
        private readonly Controller[] _controllers = new Controller[4];
        private int _activeIndex;
        private Gamepad _currentState;
        private bool _isConnected;
        private bool _disposed = false;

        public ControllerManager()
        {
            for (int i = 0; i < 4; i++)
                _controllers[i] = new Controller((UserIndex)i);

            SelectFirstConnectedSlot();
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
                return;
            }

            _isConnected = true;
            _currentState = Active.GetState().Gamepad;
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

}
