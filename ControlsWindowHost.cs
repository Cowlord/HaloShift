using System;
using System.Windows.Forms;

namespace HaloShift
{
    /// <summary>Shows the WinForms controller-mappings window from the Avalonia app.</summary>
    public static class ControlsWindowHost
    {
        private static ControlsWindow? _controlsWindow;

        public static void ShowAbout()
        {
            if (_controlsWindow == null || _controlsWindow.IsDisposed)
            {
                _controlsWindow = new ControlsWindow();
                _controlsWindow.FormClosed += (_, _) => _controlsWindow = null;
            }

            if (!_controlsWindow.Visible)
                _controlsWindow.Show();

            _controlsWindow.BringToFront();
            _controlsWindow.Activate();
        }
    }
}
