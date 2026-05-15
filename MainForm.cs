using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Media;
using DrawingColor = System.Drawing.Color;

namespace HaloShift
{
    public partial class MainForm : Form
    {
        private ControllerManager _controllerManager;
        private ModeManager _modeManager;
        private Timer _updateTimer;
        private NotifyIcon _notifyIcon;
        private ControlsWindow _controlsWindow;
        private SoundPlayer _activateSound;
        private SoundPlayer _deactivateSound;
        private SensitivityOverlay _sensitivityOverlay;
        private VirtualKeyboard _virtualKeyboard;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        private const int SW_RESTORE = 9;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public MainForm(ControllerManager controllerManager)
        {
            _controllerManager = controllerManager;
            _modeManager = new ModeManager();

            InitializeComponent();
            SetupTrayIcon();
            SetupUpdateTimer();
            LoadSounds();
            SetupSensitivityOverlay();
            SetupVirtualKeyboard();

            _modeManager.ModeChanged += ModeManager_ModeChanged;
            _modeManager.SensitivityChanged += ModeManager_SensitivityChanged;
            _modeManager.ShowKeyboardRequested += ModeManager_ShowKeyboardRequested;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "HaloShift Controller";
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Location = new System.Drawing.Point(-10000, -10000); // Off-screen

            // Set form icon from embedded resource
            try
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("HaloShift.AppIcon.ico"))
                {
                    if (stream != null)
                    {
                        this.Icon = new Icon(stream);
                    }
                }
            }
            catch { }

            // Hide from Alt+Tab by setting as tool window
            IntPtr exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, new IntPtr((int)exStyle | WS_EX_TOOLWINDOW));

            // Status label
            var statusLabel = new Label
            {
                Text = "Mode: Controller",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(statusLabel);
            this.Tag = statusLabel;

            this.ResumeLayout(false);
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Application;

            // Try to load icon from embedded resource
            try
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("HaloShift.AppIcon.ico"))
                {
                    if (stream != null)
                    {
                        _notifyIcon.Icon = new Icon(stream);
                    }
                }
            }
            catch { }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "HaloShift - Controller Mode";

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Controls", null, (s, e) => ShowControlsWindow());
            contextMenu.Items.Add("Toggle Mode", null, (s, e) => _modeManager.SwitchMode(ModeChangeInitiator.UserMenu));
            contextMenu.Items.Add("Show Keyboard", null, (s, e) => _virtualKeyboard?.ShowKeyboard());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => this.Close());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowControlsWindow();
                }
            };
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new Timer();
            _updateTimer.Interval = 8; // ~120 Hz for low-latency input handling
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            _controllerManager.Update();
            bool isConnected = _controllerManager.IsConnected;
            var currentState = _controllerManager.GetCurrentState();

            // Mode toggle (hold View+Menu) must run even when the virtual keyboard is open; otherwise
            // switching back to controller mode is impossible without the tray menu.
            // Do not bail out when XInput is briefly disconnected — that was skipping this entirely.
            _modeManager.Update(currentState);

            // Virtual keyboard owns D-pad / face buttons for typing, but sticks + triggers should
            // still move the cursor and click (otherwise opening the keyboard disables mouse mode entirely).
            if (_virtualKeyboard?.Visible == true)
            {
                if (isConnected && _modeManager.CurrentMode == AppMode.Mouse)
                    _modeManager.HandleMouseModePointerInput(currentState);
                if (isConnected)
                    _virtualKeyboard.HandleInput(currentState);
                return;
            }

            // Mouse mapping requires a live XInput controller
            if (_modeManager.CurrentMode == AppMode.Mouse && isConnected)
            {
                _modeManager.HandleMouseModeInput(currentState);
            }
        }

        private void ModeManager_SensitivityChanged(object sender, SensitivityChangedEventArgs e)
        {
            _sensitivityOverlay?.ShowValue(e.NewSensitivity);
        }

        private void ModeManager_ShowKeyboardRequested(object sender, EventArgs e)
        {
            _virtualKeyboard?.ShowKeyboard();
        }

        private void ModeManager_ModeChanged(object sender, ModeChangedEventArgs e)
        {
            if (e.NewMode == AppMode.Mouse)
            {
                // Play activation sound
                PlayActivationSound();

                // Show and bring window to foreground
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                if (IsIconic(this.Handle))
                {
                    ShowWindow(this.Handle, SW_RESTORE);
                }
                SetForegroundWindow(this.Handle);
                _notifyIcon.Text = "HaloShift - Mouse Mode";
            }
            else
            {
                // Play deactivation sound
                PlayDeactivationSound();

                // Close the virtual keyboard when switching back to controller mode and restore focus to the game
                _virtualKeyboard?.DismissRestoringPreviousFocus();

                // Completely hide the window
                this.Visible = false;
                this.WindowState = FormWindowState.Minimized;
                this.Location = new System.Drawing.Point(-10000, -10000);
                _notifyIcon.Text = "HaloShift - Controller Mode";
            }

            // Update status label
            if (this.Tag is Label statusLabel)
            {
                statusLabel.Text = $"Mode: {e.NewMode}";
            }
        }

        private void LoadSounds()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                // Load activation sound from embedded resource
                var activateStream = assembly.GetManifestResourceStream("HaloShift.sound_activate.wav");
                if (activateStream != null)
                {
                    _activateSound = new SoundPlayer(activateStream);
                    _activateSound.Load();
                }

                // Load deactivation sound from embedded resource
                var deactivateStream = assembly.GetManifestResourceStream("HaloShift.sound_deactivate.wav");
                if (deactivateStream != null)
                {
                    _deactivateSound = new SoundPlayer(deactivateStream);
                    _deactivateSound.Load();
                }
            }
            catch { }
        }

        private void PlayActivationSound()
        {
            try
            {
                if (_activateSound != null)
                {
                    _activateSound.Play();
                }
            }
            catch { }
        }

        private void PlayDeactivationSound()
        {
            try
            {
                if (_deactivateSound != null)
                {
                    _deactivateSound.Play();
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            if (_modeManager != null)
            {
                _modeManager.ModeChanged -= ModeManager_ModeChanged;
                _modeManager.SensitivityChanged -= ModeManager_SensitivityChanged;
                _modeManager.ShowKeyboardRequested -= ModeManager_ShowKeyboardRequested;
                _modeManager.ReleaseHeldMouseButtons();
            }

            _notifyIcon?.Dispose();

            try
            {
                _activateSound?.Dispose();
                _deactivateSound?.Dispose();
            }
            catch { }

            _modeManager = null;
            _sensitivityOverlay?.Dispose();
            _virtualKeyboard?.Dispose();

            base.OnFormClosing(e);
        }

        private void SetupSensitivityOverlay()
        {
            _sensitivityOverlay = new SensitivityOverlay();
        }

        private void SetupVirtualKeyboard()
        {
            _virtualKeyboard = new VirtualKeyboard();
            _virtualKeyboard.KeyboardClosed += (s, e) =>
            {
                // Keyboard was closed, resume normal operation
            };
        }

        private void ShowControlsWindow()
        {
            // Ensure only one instance of ControlsWindow is created
            if (_controlsWindow == null || _controlsWindow.IsDisposed)
            {
                _controlsWindow = new ControlsWindow();
                _controlsWindow.FormClosed += (s, e) => _controlsWindow = null;
            }

            if (!_controlsWindow.Visible)
            {
                _controlsWindow.Show();
            }

            _controlsWindow.BringToFront();
            _controlsWindow.Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            base.OnResize(e);
        }

        private class SensitivityOverlay : Form
        {
            private readonly Label _label;
            private readonly Timer _hideTimer;

            public SensitivityOverlay()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                BackColor = DrawingColor.FromArgb(30, 30, 30);
                Opacity = 0.9;
                Size = new Size(220, 60);

                _label = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    ForeColor = DrawingColor.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold)
                };
                Controls.Add(_label);

                _hideTimer = new Timer();
                _hideTimer.Interval = 1500;
                _hideTimer.Tick += (s, e) =>
                {
                    Hide();
                    _hideTimer.Stop();
                };
            }

            public void ShowValue(float sensitivity)
            {
                _label.Text = $"Sensitivity: {sensitivity:F1}";

                var screen = Screen.FromPoint(Cursor.Position);
                var workArea = screen.WorkingArea;
                Location = new Point(
                    workArea.Left + (workArea.Width - Width) / 2,
                    workArea.Top + (int)(workArea.Height * 0.15));

                Show();
                BringToFront();

                _hideTimer.Stop();
                _hideTimer.Start();
            }
        }
    }
}
