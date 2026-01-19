using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace HaloShift
{
    public partial class MainForm : Form
    {
        private ControllerManager _controllerManager;
        private ModeManager _modeManager;
        private Timer _updateTimer;
        private NotifyIcon _notifyIcon;

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

            _modeManager.ModeChanged += ModeManager_ModeChanged;
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

            // Try to load icon if it exists
            try
            {
                var iconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
            }
            catch { }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "HaloShift - Controller Mode";

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow(this.Handle, SW_RESTORE));
            contextMenu.Items.Add("Toggle Mode", null, (s, e) => _modeManager.SwitchMode());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => this.Close());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowWindow(this.Handle, SW_RESTORE);
                    SetForegroundWindow(this.Handle);
                }
            };
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new Timer();
            _updateTimer.Interval = 16; // ~60 FPS for responsive input handling
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_controllerManager.IsConnected)
                return;

            _controllerManager.Update();
            var currentState = _controllerManager.GetCurrentState();

            // Update mode based on input
            _modeManager.Update(currentState);

            // Handle mode-specific input
            if (_modeManager.CurrentMode == AppMode.Mouse)
            {
                _modeManager.HandleMouseModeInput(currentState);
            }
        }

        private void ModeManager_ModeChanged(object sender, ModeChangedEventArgs e)
        {
            if (e.NewMode == AppMode.Mouse)
            {
                // Bring window to foreground
                if (IsIconic(this.Handle))
                {
                    ShowWindow(this.Handle, SW_RESTORE);
                }
                SetForegroundWindow(this.Handle);
                this.WindowState = FormWindowState.Normal;
                _notifyIcon.Text = "HaloShift - Mouse Mode";
            }
            else
            {
                // Minimize window
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _notifyIcon?.Dispose();
            _modeManager = null;

            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            base.OnResize(e);
        }
    }
}
