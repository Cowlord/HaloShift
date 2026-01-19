using System;
using System.Drawing;
using System.Windows.Forms;

namespace HaloShift
{
    public class ControlsWindow : Form
    {
        public ControlsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "HaloShift - Controller Mappings";
            this.ClientSize = new Size(500, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);

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

            // Title label
            var titleLabel = new Label
            {
                Text = "Controller Button Mappings",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            this.Controls.Add(titleLabel);

            // Mode toggle section
            var modeTogglePanel = CreateMappingPanel("Mode Toggle", 60);
            AddMappingLabel(modeTogglePanel, "LB + RB + Y", "Toggle between Controller and Mouse Mode");
            this.Controls.Add(modeTogglePanel);

            // Mouse mode section
            var mouseModeLabel = new Label
            {
                Text = "Mouse Mode Mappings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 150)
            };
            this.Controls.Add(mouseModeLabel);

            var mousePanel = CreateMappingPanel("", 180);
            AddMappingLabel(mousePanel, "Left Stick", "Move mouse cursor", 0);
            AddMappingLabel(mousePanel, "Right Trigger", "Left mouse click", 30);
            AddMappingLabel(mousePanel, "Left Trigger", "Right mouse click", 60);
            AddMappingLabel(mousePanel, "LB", "F11 (Fullscreen)", 90);
            this.Controls.Add(mousePanel);

            // Global shortcuts section
            var globalLabel = new Label
            {
                Text = "Global Shortcuts (Both Modes)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 320)
            };
            this.Controls.Add(globalLabel);

            var globalPanel = CreateMappingPanel("", 345);
            AddMappingLabel(globalPanel, "X", "Escape key", 0);
            AddMappingLabel(globalPanel, "B", "Browser Back", 30);
            AddMappingLabel(globalPanel, "LT + RT + A", "Ctrl + W (Close tab)", 60);
            AddMappingLabel(globalPanel, "LT + RT + B", "Alt + F4 (Close window)", 90);
            this.Controls.Add(globalPanel);

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 35),
                Location = new Point(200, 490),
                Font = new Font("Segoe UI", 10),
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

        private Panel CreateMappingPanel(string title, int y)
        {
            var panel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(460, title == "" ? 140 : 70),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            if (!string.IsNullOrEmpty(title))
            {
                var label = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(10, 10)
                };
                panel.Controls.Add(label);

                var descLabel = new Label
                {
                    Text = "Switch between Controller and Mouse Mode",
                    Font = new Font("Segoe UI", 9),
                    AutoSize = false,
                    Size = new Size(440, 40),
                    Location = new Point(10, 35),
                    ForeColor = Color.Gray
                };
                panel.Controls.Add(descLabel);
            }

            return panel;
        }

        private void AddMappingLabel(Panel panel, string button, string action, int yOffset = 30)
        {
            var buttonLabel = new Label
            {
                Text = button,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10 + yOffset),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panel.Controls.Add(buttonLabel);

            var actionLabel = new Label
            {
                Text = "→  " + action,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(150, 10 + yOffset),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            panel.Controls.Add(actionLabel);
        }
    }
}
