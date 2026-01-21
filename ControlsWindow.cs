using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace HaloShift
{
    public class ControlsWindow : Form
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        // Colors
        private static readonly Color BgColor = Color.FromArgb(24, 24, 28);
        private static readonly Color PanelBgColor = Color.FromArgb(36, 36, 40);
        private static readonly Color GoldColor = Color.FromArgb(255, 215, 0);
        private static readonly Color GreenColor = Color.FromArgb(100, 255, 100);
        private static readonly Color CyanColor = Color.FromArgb(100, 200, 255);
        private static readonly Color GrayText = Color.FromArgb(180, 180, 180);

        private int _currentY = 60;

        public ControlsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "HaloShift - Controller Mappings";
            this.ClientSize = new Size(420, 720);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
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

            // Title
            var titleLabel = new Label
            {
                Text = "HaloShift Controller",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18),
                ForeColor = Color.White,
                Cursor = Cursors.SizeAll
            };
            titleLabel.MouseDown += Form_MouseDown;
            this.Controls.Add(titleLabel);

            // Close X button
            var closeX = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14),
                AutoSize = true,
                Location = new Point(385, 15),
                ForeColor = GrayText,
                Cursor = Cursors.Hand
            };
            closeX.Click += (s, e) => this.Close();
            closeX.MouseEnter += (s, e) => closeX.ForeColor = Color.White;
            closeX.MouseLeave += (s, e) => closeX.ForeColor = GrayText;
            this.Controls.Add(closeX);

            // Mode Toggle Panel
            var togglePanel = CreateGoldBorderPanel(60, 70);
            var toggleTitle = new Label
            {
                Text = "⚡ Mode Toggle",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 8),
                ForeColor = GoldColor
            };
            togglePanel.Controls.Add(toggleTitle);

            var toggleKeys = new Label
            {
                Text = "LB + RB + Y",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 30),
                ForeColor = GreenColor
            };
            togglePanel.Controls.Add(toggleKeys);
            this.Controls.Add(togglePanel);
            _currentY = 145;

            // Mouse Mode Mappings header
            var mouseModeLabel = new Label
            {
                Text = "🖱️  Mouse Mode Mappings",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, _currentY),
                ForeColor = CyanColor
            };
            this.Controls.Add(mouseModeLabel);
            _currentY += 28;

            // Mouse Actions section
            AddSectionHeader("Mouse Actions");
            AddMappingRow("Left Stick", "Move mouse cursor", "🎮", Color.FromArgb(255, 180, 0));
            AddMappingRow("Left Stick Click", "Windows key", "⊞", Color.FromArgb(100, 180, 255));
            AddMappingRow("Right Stick ↕", "Scroll wheel", "📜", Color.FromArgb(100, 180, 255));
            AddMappingRow("Right Stick Click", "F5 (Refresh)", "⟳", Color.FromArgb(100, 220, 180));
            AddMappingRow("RT", "Left mouse click", "👆", Color.FromArgb(255, 180, 0));
            AddMappingRow("LT", "Right mouse click", "👉", Color.FromArgb(255, 180, 0));
            AddMappingRow("LT + RT + X", "Middle mouse click", "🖱️", Color.FromArgb(100, 180, 255));

            // Navigation section
            AddSectionHeader("Navigation");
            AddMappingRow("X", "Escape key", "⊘", Color.FromArgb(66, 133, 244));
            AddMappingRow("B", "Browser Back", "◂", Color.FromArgb(234, 67, 53));

            // Window Control section
            AddSectionHeader("Window Control");
            AddMappingRow("LT + RT + A", "Ctrl + W", "✕", Color.FromArgb(52, 168, 83));
            AddMappingRow("LT + RT + B", "Alt + F4", "✕", Color.FromArgb(234, 67, 53));

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 36),
                Location = new Point(160, _currentY + 15),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);

            // Adjust window height
            this.ClientSize = new Size(420, _currentY + 70);

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

        private Panel CreateGoldBorderPanel(int y, int height)
        {
            var panel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(380, height),
                BackColor = Color.FromArgb(45, 42, 30)
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(GoldColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, panel.Width - 3, panel.Height - 3);
                }
            };

            return panel;
        }

        private void AddSectionHeader(string text)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, _currentY),
                ForeColor = GrayText
            };
            this.Controls.Add(label);
            _currentY += 22;
        }

        private void AddMappingRow(string button, string action, string icon, Color iconColor)
        {
            var rowPanel = new Panel
            {
                Location = new Point(20, _currentY),
                Size = new Size(380, 32),
                BackColor = PanelBgColor
            };

            // Icon
            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Location = new Point(10, 5),
                ForeColor = iconColor
            };
            rowPanel.Controls.Add(iconLabel);

            // Button name
            var buttonLabel = new Label
            {
                Text = button,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 6),
                ForeColor = CyanColor
            };
            rowPanel.Controls.Add(buttonLabel);

            // Arrow
            var arrow = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(175, 6),
                ForeColor = GrayText
            };
            rowPanel.Controls.Add(arrow);

            // Action
            var actionLabel = new Label
            {
                Text = action,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(200, 6),
                ForeColor = Color.White
            };
            rowPanel.Controls.Add(actionLabel);

            this.Controls.Add(rowPanel);
            _currentY += 34;
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
                path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
                path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
    }
}
