using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace HaloShift
{
    public class ControlsWindow : Form
    {
        // For dragging the window
        private bool _dragging = false;
        private Point _dragCursorPoint;
        private Point _dragFormPoint;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public ControlsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "HaloShift - Controller Mappings";
            this.ClientSize = new Size(520, 620);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 32);

            // Enable dragging
            this.MouseDown += Form_MouseDown;

            // Set icon from embedded resource
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

            // Title label with close button
            var titleLabel = new Label
            {
                Text = "HaloShift Controller",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Cursor = Cursors.SizeAll
            };
            titleLabel.MouseDown += Form_MouseDown;
            this.Controls.Add(titleLabel);

            // Close X button in top right
            var closeX = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 16),
                AutoSize = true,
                Location = new Point(485, 15),
                ForeColor = Color.FromArgb(180, 180, 180),
                Cursor = Cursors.Hand
            };
            closeX.Click += (s, e) => this.Close();
            closeX.MouseEnter += (s, e) => closeX.ForeColor = Color.White;
            closeX.MouseLeave += (s, e) => closeX.ForeColor = Color.FromArgb(180, 180, 180);
            this.Controls.Add(closeX);

            // Activation info panel
            var activationPanel = CreateDarkPanel(60, 100);
            var activationTitle = new Label
            {
                Text = "Activate Mouse Mode",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 10),
                ForeColor = Color.FromArgb(100, 200, 255)
            };
            activationPanel.Controls.Add(activationTitle);

            var activationKeys = new Label
            {
                Text = "LB + RB + Y",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 40),
                ForeColor = Color.FromArgb(0, 200, 100)
            };
            activationPanel.Controls.Add(activationKeys);

            var activationDesc = new Label
            {
                Text = "Press all three buttons together to toggle between Controller and Mouse Mode",
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Size = new Size(450, 30),
                Location = new Point(15, 70),
                ForeColor = Color.FromArgb(180, 180, 180)
            };
            activationPanel.Controls.Add(activationDesc);
            this.Controls.Add(activationPanel);

            // Mouse mode section
            var mouseModeLabel = new Label
            {
                Text = "Mouse Mode",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 175),
                ForeColor = Color.FromArgb(100, 200, 255)
            };
            this.Controls.Add(mouseModeLabel);

            var mousePanel = CreateDarkPanel(205, 150);
            AddMappingLabel(mousePanel, "Left Stick", "Move mouse cursor", 0);
            AddMappingLabel(mousePanel, "Right Trigger", "Left mouse click", 30);
            AddMappingLabel(mousePanel, "Left Trigger", "Right mouse click", 60);
            AddMappingLabel(mousePanel, "LB", "F11 (Fullscreen)", 90);
            this.Controls.Add(mousePanel);

            // Global shortcuts section
            var globalLabel = new Label
            {
                Text = "Global Shortcuts (Both Modes)",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 370),
                ForeColor = Color.FromArgb(100, 200, 255)
            };
            this.Controls.Add(globalLabel);

            var globalPanel = CreateDarkPanel(400, 150);
            AddMappingLabel(globalPanel, "X", "Escape key", 0);
            AddMappingLabel(globalPanel, "B", "Browser Back", 30);
            AddMappingLabel(globalPanel, "LT + RT + A", "Ctrl + W (Close tab)", 60);
            AddMappingLabel(globalPanel, "LT + RT + B", "Alt + F4 (Close window)", 90);
            this.Controls.Add(globalPanel);

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(120, 40),
                Location = new Point(200, 565),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);

            this.ResumeLayout(false);
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private Panel CreateDarkPanel(int y, int height)
        {
            var panel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(480, height),
                BackColor = Color.FromArgb(45, 45, 48),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(5)
            };

            return panel;
        }

        private void AddMappingLabel(Panel panel, string button, string action, int yOffset)
        {
            var buttonLabel = new Label
            {
                Text = button,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 15 + yOffset),
                ForeColor = Color.FromArgb(100, 200, 255)
            };
            panel.Controls.Add(buttonLabel);

            var arrow = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(160, 15 + yOffset),
                ForeColor = Color.FromArgb(150, 150, 150)
            };
            panel.Controls.Add(arrow);

            var actionLabel = new Label
            {
                Text = action,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(185, 15 + yOffset),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            panel.Controls.Add(actionLabel);
        }
    }
}
