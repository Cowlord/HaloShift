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

        // Xbox Official Colors
        private static readonly Color DarkBg = Color.FromArgb(16, 16, 20);
        private static readonly Color CardBg = Color.FromArgb(25, 25, 32);
        private static readonly Color AccentGreen = Color.FromArgb(16, 124, 16);
        private static readonly Color BrightGreen = Color.FromArgb(34, 177, 76);
        private static readonly Color XboxYellow = Color.FromArgb(255, 198, 0);
        private static readonly Color XboxBlue = Color.FromArgb(16, 124, 201);
        private static readonly Color XboxRed = Color.FromArgb(201, 43, 36);
        private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private static readonly Color TextSecondary = Color.FromArgb(191, 191, 191);
        private static readonly Color TextTertiary = Color.FromArgb(127, 127, 127);
        private static readonly Color BorderColor = Color.FromArgb(55, 55, 60);

        private int _currentY = 50;

        public ControlsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "HaloShift - Controller Mappings";
            this.ClientSize = new Size(500, 800);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = DarkBg;
            this.MouseDown += Form_MouseDown;
            this.DoubleBuffered = true;

            // Set icon
            try
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("HaloShift.AppIcon.ico"))
                {
                    if (stream != null) this.Icon = new Icon(stream);
                }
            }
            catch { }

            // Header Panel
            var headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 50),
                BackColor = Color.FromArgb(20, 20, 26)
            };
            headerPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(BorderColor, 1), 0, headerPanel.Height - 1,
                    headerPanel.Width, headerPanel.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = "HALOSHIFT",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 12),
                ForeColor = TextPrimary,
                Cursor = Cursors.SizeAll
            };
            titleLabel.MouseDown += Form_MouseDown;
            headerPanel.Controls.Add(titleLabel);

            var closeBtn = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Location = new Point(465, 15),
                ForeColor = TextTertiary,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => this.Close();
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = XboxRed;
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = TextTertiary;
            headerPanel.Controls.Add(closeBtn);
            this.Controls.Add(headerPanel);

            _currentY = 65;

            // Mode Status Card
            AddModeCard();

            // Sections
            AddSectionTitle("BUTTON MAPPINGS");
            AddButtonGroup("FACE BUTTONS", new[] {
                ("Y (alone)", "Show Virtual Keyboard", XboxYellow),
                ("X", "Escape", Color.FromArgb(100, 150, 255)),
                ("B", "Browser Back", XboxRed),
                ("A", "Space / Confirm", BrightGreen)
            });

            AddSectionTitle("MOUSE CONTROLS");
            AddButtonGroup("MOVEMENT & CLICKS", new[] {
                ("Left Stick", "Cursor", Color.FromArgb(100, 200, 255)),
                ("Left Click", "Windows Key", Color.FromArgb(100, 200, 255)),
                ("Right Stick ↕", "Scroll Wheel", Color.FromArgb(100, 200, 255)),
                ("RT", "Left Click", Color.FromArgb(200, 150, 100)),
                ("LT", "Right Click", Color.FromArgb(200, 150, 100)),
                ("LT + RT + X", "Middle Click", Color.FromArgb(200, 150, 100))
            });

            AddSectionTitle("ADVANCED");
            AddButtonGroup("COMBOS", new[] {
                ("LB + RB + Y", "Mode Toggle", AccentGreen),
                ("LT + RT + A", "Ctrl + W", XboxRed),
                ("LT + RT + B", "Alt + F4", XboxRed)
            });

            AddSectionTitle("VIRTUAL KEYBOARD");
            AddButtonGroup("KEYBOARD CONTROLS", new[] {
                ("D-Pad", "Navigate Keys", Color.FromArgb(150, 150, 255)),
                ("A", "Select Key", BrightGreen),
                ("X", "Backspace", Color.FromArgb(100, 150, 255)),
                ("B", "Close Keyboard", XboxRed)
            });

            // Footer
            var closeButton = new Button
            {
                Text = "CLOSE",
                Size = new Size(120, 40),
                Location = new Point(190, _currentY + 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = BrightGreen;
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = AccentGreen;
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);

            this.ClientSize = new Size(500, _currentY + 80);
            this.ResumeLayout(false);
        }

        private void AddModeCard()
        {
            var card = new Panel
            {
                Location = new Point(20, _currentY),
                Size = new Size(460, 100),
                BackColor = CardBg
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(AccentGreen, 2), 0, 0, card.Width - 1, card.Height - 1);
            };

            var modeLabel = new Label
            {
                Text = "⚡ CURRENT MODE",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 8),
                ForeColor = TextTertiary
            };
            card.Controls.Add(modeLabel);

            var activeMode = new Label
            {
                Text = "MOUSE MODE ACTIVE",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 26),
                ForeColor = BrightGreen
            };
            card.Controls.Add(activeMode);

            var toggleInfo = new Label
            {
                Text = "Press LB + RB + Y to toggle",
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                Location = new Point(12, 60),
                ForeColor = TextTertiary
            };
            card.Controls.Add(toggleInfo);

            this.Controls.Add(card);
            _currentY += 110;
        }

        private void AddSectionTitle(string text)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, _currentY),
                ForeColor = TextTertiary
            };
            this.Controls.Add(label);
            _currentY += 28;
        }

        private void AddButtonGroup(string groupName, (string button, string action, Color color)[] mappings)
        {
            var groupPanel = new Panel
            {
                Location = new Point(20, _currentY),
                Size = new Size(460, mappings.Length * 38 + 16),
                BackColor = CardBg
            };
            groupPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(BorderColor, 1), 0, 0, groupPanel.Width - 1, groupPanel.Height - 1);
            };

            int groupY = 10;
            foreach (var (button, action, color) in mappings)
            {
                AddMappingItem(groupPanel, button, action, color, groupY);
                groupY += 38;
            }

            this.Controls.Add(groupPanel);
            _currentY += groupPanel.Height + 12;
        }

        private void AddMappingItem(Panel parent, string button, string action, Color buttonColor, int y)
        {
            // Button indicator circle
            var buttonCircle = new Label
            {
                Text = "●",
                Font = new Font("Arial", 14),
                AutoSize = true,
                Location = new Point(12, y + 2),
                ForeColor = buttonColor
            };
            parent.Controls.Add(buttonCircle);

            // Button name
            var buttonLabel = new Label
            {
                Text = button,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(35, y + 3),
                ForeColor = TextPrimary
            };
            parent.Controls.Add(buttonLabel);

            // Arrow separator
            var arrow = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(180, y + 4),
                ForeColor = BorderColor
            };
            parent.Controls.Add(arrow);

            // Action
            var actionLabel = new Label
            {
                Text = action,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(210, y + 4),
                ForeColor = TextSecondary
            };
            parent.Controls.Add(actionLabel);
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
