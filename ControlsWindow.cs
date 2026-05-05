using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

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

        // Palette from HTML mock
        private static readonly Color ContainerBg = Color.FromArgb(13, 13, 13);
        private static readonly Color PanelBg = Color.FromArgb(26, 26, 26);
        private static readonly Color BorderAccent = Color.FromArgb(34, 170, 34);
        private static readonly Color BorderSubtle = Color.FromArgb(51, 51, 51);
        private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private static readonly Color TextSecondary = Color.FromArgb(170, 170, 170);
        private static readonly Color TextMuted = Color.FromArgb(136, 136, 136);
        private static readonly Color AccentGreen = Color.FromArgb(34, 255, 34);
        private static readonly Color AccentYellow = Color.FromArgb(255, 255, 0);
        private static readonly Color AccentBlue = Color.FromArgb(0, 255, 255);
        private static readonly Color AccentRed = Color.FromArgb(255, 51, 51);
        private static readonly Color AccentOrange = Color.FromArgb(255, 136, 0);

        private const int FormWidth = 700;
        private const int ContentMargin = 16;
        private const int SectionGap = 12;

        private static string AppVersion => $"v{GetAssemblyVersion()}";

        public ControlsWindow()
        {
            InitializeComponent();
        }

        private Control CreateInputControl(string input)
        {
            // Check for combo inputs ending with a button letter (e.g., "LT + RT + X")
            var comboMatch = System.Text.RegularExpressions.Regex.Match(input, @"^(.+\+\s*)([XYAB])$");
            if (comboMatch.Success)
            {
                string prefix = comboMatch.Groups[1].Value;
                string buttonLetter = comboMatch.Groups[2].Value;
                Color buttonColor = buttonLetter switch
                {
                    "Y" => AccentYellow,
                    "X" => AccentBlue,
                    "B" => AccentRed,
                    "A" => AccentGreen,
                    _ => Color.White
                };
                bool isFilled = buttonLetter == "Y";

                var prefixLabel = new Label
                {
                    Text = prefix,
                    Font = new Font("Segoe UI", 11, FontStyle.Regular),
                    ForeColor = Color.FromArgb(176, 176, 176),
                    AutoSize = true,
                    Location = new Point(0, 4)
                };

                var badge = CreateButtonBadge(buttonLetter, buttonColor, isFilled);
                badge.Location = new Point(prefixLabel.PreferredWidth, 0);

                var container = new Panel
                {
                    Size = new Size(prefixLabel.PreferredWidth + badge.Width, 28),
                    BackColor = Color.Transparent
                };
                container.Controls.Add(prefixLabel);
                container.Controls.Add(badge);

                return container;
            }

            // Detect single button inputs and render as badges
            string singleButton = null;
            Color singleColor = Color.Transparent;
            bool singleFilled = false;

            if (input.EndsWith(" Y") || input == "Y")
            {
                singleButton = "Y";
                singleColor = AccentYellow;
                singleFilled = true;
            }
            else if (input.EndsWith(" X") || input == "X")
            {
                singleButton = "X";
                singleColor = AccentBlue;
                singleFilled = false;
            }
            else if (input.EndsWith(" B") || input == "B")
            {
                singleButton = "B";
                singleColor = AccentRed;
                singleFilled = false;
            }
            else if (input.EndsWith(" A") || input == "A")
            {
                singleButton = "A";
                singleColor = AccentGreen;
                singleFilled = false;
            }

            // Render as badge if single button, otherwise as text label
            if (singleButton != null)
            {
                var badge = CreateButtonBadge(singleButton, singleColor, singleFilled);
                badge.Size = new Size(32, 28);
                return badge;
            }
            else
            {
                return new Label
                {
                    Text = input,
                    Font = new Font("Segoe UI", 11, FontStyle.Regular),
                    ForeColor = Color.FromArgb(176, 176, 176),
                    AutoSize = true
                };
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "HaloShift - Controller Mappings";
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ContainerBg;
            this.MouseDown += Form_MouseDown;
            this.DoubleBuffered = true;
            this.Padding = new Padding(2);

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

            int currentY = 0;

            // Header
            var header = CreateHeader();
            header.Location = new Point(0, currentY);
            this.Controls.Add(header);
            currentY += header.Height;

            // Mode Indicator
            var modeIndicator = CreateModeIndicator();
            modeIndicator.Location = new Point(ContentMargin, currentY + ContentMargin);
            this.Controls.Add(modeIndicator);
            currentY += modeIndicator.Height + ContentMargin + SectionGap;

            // Content - 2x2 grid
            int contentWidth = FormWidth - (ContentMargin * 2);
            int sectionWidth = (contentWidth - SectionGap) / 2;

            // Row 1: Button Mappings | Mouse Controls
            var section1 = CreateSection("Button Mappings", new[]
            {
                ("🟨 Y", "Virtual Keyboard", AccentYellow),
                ("🟦 X", "Escape", AccentBlue),
                ("🟥 B", "Browser Back", AccentRed),
                ("🟩 A", "Space / Confirm", AccentGreen)
            }, sectionWidth);
            section1.Location = new Point(ContentMargin, currentY);
            this.Controls.Add(section1);

            var section2 = CreateSection("Mouse Controls", new[]
            {
                ("🕹️ Left Stick", "Move cursor", AccentBlue),
                ("LS (click)", "Windows key", AccentBlue),
                ("🕹️ Right Stick", "Scroll wheel", AccentBlue),
                ("RT (hold)", "Left mouse button", AccentOrange),
                ("LT (hold)", "Right mouse button", AccentOrange)
            }, sectionWidth);
            section2.Location = new Point(ContentMargin + sectionWidth + SectionGap, currentY);
            this.Controls.Add(section2);

            int row1Height = Math.Max(section1.Height, section2.Height);
            currentY += row1Height + SectionGap;

            // Row 2: Triggers & Bumpers | Virtual Keyboard & Advanced
            var section3 = CreateSection("Triggers & Bumpers", new[]
            {
                ("LT + RT + X", "Middle click", AccentBlue),
                ("LB + RB + Y", "Mode toggle", AccentYellow),
                ("LT + RT + A", "Ctrl + W", AccentGreen),
                ("LT + RT + B", "Alt + F4", AccentRed)
            }, sectionWidth);
            section3.Location = new Point(ContentMargin, currentY);
            this.Controls.Add(section3);

            var section4 = CreateSection("Virtual Keyboard", new[]
            {
                ("🎮 D-Pad", "Navigate keys", AccentBlue),
                ("LB", "Symbols ↔ letters", AccentYellow),
                ("A", "Select key", AccentGreen),
                ("X", "Backspace", AccentBlue),
                ("B", "Close keyboard", AccentRed)
            }, sectionWidth);
            section4.Location = new Point(ContentMargin + sectionWidth + SectionGap, currentY);
            this.Controls.Add(section4);

            int row2Height = Math.Max(section3.Height, section4.Height);
            currentY += row2Height + ContentMargin;

            // Footer
            var footer = CreateFooter();
            footer.Location = new Point(0, currentY);
            this.Controls.Add(footer);
            currentY += footer.Height;

            this.ClientSize = new Size(FormWidth, currentY);
            this.Paint += DrawFormBorder;

            this.ResumeLayout(false);
        }

        private Panel CreateHeader()
        {
            var header = new Panel
            {
                Size = new Size(FormWidth, 56),
                BackColor = ContainerBg
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderAccent, 2);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            header.MouseDown += Form_MouseDown;

            var titleLabel = new Label
            {
                Text = "HALOSHIFT",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(ContentMargin, 14),
                ForeColor = TextPrimary,
                Cursor = Cursors.SizeAll
            };
            titleLabel.MouseDown += Form_MouseDown;
            header.Controls.Add(titleLabel);

            var versionLabel = new Label
            {
                Text = AppVersion,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(titleLabel.Right + 12, 20),
                ForeColor = TextMuted
            };
            header.Controls.Add(versionLabel);

            var closeBtn = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 18, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(FormWidth - 40, 12),
                ForeColor = TextMuted,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => this.Close();
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = TextPrimary;
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = TextMuted;
            header.Controls.Add(closeBtn);

            return header;
        }

        private Panel CreateModeIndicator()
        {
            int width = FormWidth - (ContentMargin * 2);
            var panel = new Panel
            {
                Size = new Size(width, 52),
                BackColor = PanelBg
            };
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderAccent, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var flow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            flow.Controls.Add(CreateHintText("Press"));
            flow.Controls.Add(CreateButtonBadge("LB", AccentGreen, false));
            flow.Controls.Add(CreateHintText("+"));
            flow.Controls.Add(CreateButtonBadge("RB", AccentGreen, false));
            flow.Controls.Add(CreateHintText("+"));
            flow.Controls.Add(CreateButtonBadge("Y", AccentYellow, true));
            flow.Controls.Add(CreateHintText("to toggle"));

            panel.Controls.Add(flow);
            panel.Layout += (s, e) =>
            {
                flow.Location = new Point((panel.Width - flow.Width) / 2, (panel.Height - flow.Height) / 2);
            };

            return panel;
        }

        private Label CreateHintText(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = AccentGreen,
                AutoSize = true,
                Margin = new Padding(4, 4, 4, 0)
            };
        }

        private Control CreateButtonBadge(string text, Color borderColor, bool filled)
        {
            if (text == "A" || text == "B" || text == "X" || text == "Y")
            {
                var buttonImage = LoadButtonIndicatorImage(text);
                if (buttonImage != null)
                {
                    return new PictureBox
                    {
                        Image = buttonImage,
                        Size = new Size(28, 28),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Transparent,
                        Margin = new Padding(2, 2, 2, 0)
                    };
                }
            }

            var badge = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = filled ? Color.FromArgb(32, 32, 32) : borderColor,
                BackColor = filled ? borderColor : Color.FromArgb(34, 34, 34),
                AutoSize = false,
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(2, 2, 2, 0)
            };
            badge.Paint += (s, e) =>
            {
                using var pen = new Pen(borderColor, 2);
                e.Graphics.DrawRectangle(pen, 1, 1, badge.Width - 3, badge.Height - 3);
            };
            return badge;
        }

        private Image LoadButtonIndicatorImage(string button)
        {
            try
            {
                string fileName = $"{button}_button.png";
                string[] candidatePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "assets", fileName),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", fileName),
                    Path.Combine(Application.StartupPath, "assets", fileName)
                };

                foreach (var path in candidatePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var source = Image.FromStream(fs);
                        return new Bitmap(source);
                    }
                }
            }
            catch
            {
                // Fall back to text badge if image cannot be loaded.
            }

            return null;
        }

        private Panel CreateSection(string title, (string input, string action, Color color)[] rows, int width)
        {
            int rowHeight = 32;
            int titleHeight = 24;
            int padding = 12;
            int totalHeight = padding + titleHeight + (rows.Length * rowHeight) + padding;

            var section = new Panel
            {
                Size = new Size(width, totalHeight),
                BackColor = PanelBg
            };
            section.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderSubtle, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, section.Width - 1, section.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(padding, padding)
            };
            section.Controls.Add(titleLabel);

            int y = padding + titleHeight;
            for (int i = 0; i < rows.Length; i++)
            {
                var (input, action, color) = rows[i];
                bool isLast = i == rows.Length - 1;

                var rowPanel = new Panel
                {
                    Location = new Point(padding, y),
                    Size = new Size(width - (padding * 2), rowHeight),
                    BackColor = Color.Transparent
                };

                if (!isLast)
                {
                    rowPanel.Paint += (s, e) =>
                    {
                        using var pen = new Pen(Color.FromArgb(34, 34, 34), 1);
                        e.Graphics.DrawLine(pen, 0, rowPanel.Height - 1, rowPanel.Width, rowPanel.Height - 1);
                    };
                }

                var inputControl = CreateInputControl(input);
                inputControl.Location = new Point(0, (rowHeight - inputControl.Height) / 2);
                rowPanel.Controls.Add(inputControl);

                var actionLabel = new Label
                {
                    Text = action,
                    Font = new Font("Segoe UI", 11, FontStyle.Regular),
                    ForeColor = TextSecondary,
                    AutoSize = true
                };
                actionLabel.Location = new Point(rowPanel.Width - actionLabel.PreferredWidth, 6);
                rowPanel.Controls.Add(actionLabel);

                section.Controls.Add(rowPanel);
                y += rowHeight;
            }

            return section;
        }

        private Panel CreateFooter()
        {
            var footer = new Panel
            {
                Size = new Size(FormWidth, 64),
                BackColor = ContainerBg
            };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderSubtle, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            var closeButton = new Button
            {
                Text = "CLOSE",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(120, 40),
                BackColor = BorderAccent,
                ForeColor = Color.FromArgb(13, 13, 13),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(51, 221, 51);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = BorderAccent;
            closeButton.Click += (s, e) => this.Close();
            closeButton.Location = new Point((FormWidth - closeButton.Width) / 2, (footer.Height - closeButton.Height) / 2);
            footer.Controls.Add(closeButton);

            return footer;
        }

        private void DrawFormBorder(object sender, PaintEventArgs e)
        {
            using var pen = new Pen(BorderAccent, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
        }

        private static string GetAssemblyVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = info ?? asm.GetName().Version?.ToString() ?? "1.0.0.0";
            int plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
            {
                version = version[..plusIndex];
            }
            return version;
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
