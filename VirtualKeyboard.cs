using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Media;
using SharpDX.XInput;

namespace HaloShift
{
    public class VirtualKeyboard : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        // Xbox Official Colors
        private static readonly Color DarkBg = Color.FromArgb(16, 16, 20);
        private static readonly Color CardBg = Color.FromArgb(25, 25, 32);
        private static readonly Color AccentGreen = Color.FromArgb(16, 124, 16);
        private static readonly Color BrightGreen = Color.FromArgb(34, 177, 76);
        private static readonly Color SelectedColor = Color.FromArgb(107, 186, 71);
        private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private static readonly Color TextSecondary = Color.FromArgb(191, 191, 191);
        private static readonly Color BorderColor = Color.FromArgb(55, 55, 60);

        private const int KeyRowCount = 4;
        private const int SpecialKeyCount = 5;

        /// <summary>Virtual row index for Shift, Caps, Space, Enter, and Delete (not a string row).</summary>
        private const int SpecialRowIndex = KeyRowCount;

        private readonly string[] _keyboardLayoutAlpha = new[]
        {
            "1234567890",
            "QWERTYUIOP",
            "ASDFGHJKL",
            "ZXCVBNM"
        };

        /// <summary>Same geometry as alpha rows; toggled with Left Bumper while the keyboard is open.</summary>
        private readonly string[] _keyboardLayoutSymbol = new[]
        {
            "!@#$%^&*()",
            "[]{}|\\/_~*",
            "+-=_:;',.",
            "@,./?!~"
        };

        private bool _symbolLayer;

        private int _currentRow = 0;
        private int _currentCol = 0;
        private int _specialKeyIndex = 0; // For navigating special keys row
        private bool _firstInputFrame = true; // Skip first input to prevent immediate navigation
        private Panel _keyboardPanel;
        private Button _deleteButton;
        private Button _returnButton;
        private Button _shiftButton;
        private Button _capsLockButton;
        private Label _spaceBarLabel;
        private Label[,] _keyLabels;
        private IntPtr _previousWindow;
        private bool _shiftActive = false;
        private bool _capsLockActive = false;
        private SoundPlayer _navigationSound;
        private SoundPlayer _keyPressSound;
        private System.Windows.Forms.Timer _keyboardTimer;
        private bool _leftPressed = false;
        private bool _rightPressed = false;
        private bool _upPressed = false;
        private bool _downPressed = false;
        private bool _enterPressed = false;
        private bool _escPressed = false;
        private bool _lbPressed = false;

        public event EventHandler KeyboardClosed;

        public VirtualKeyboard()
        {
            InitializeComponent();
            LoadSounds();
            CreateKeyboard();
            UpdateSelection(false); // Don't play sound on initial load

            // Timer to poll keyboard state for arrow key navigation
            _keyboardTimer = new System.Windows.Forms.Timer();
            _keyboardTimer.Interval = 50; // Poll every 50ms
            _keyboardTimer.Tick += KeyboardTimer_Tick;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Virtual Keyboard";
            this.ClientSize = new Size(1000, 450);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = DarkBg;
            this.DoubleBuffered = true;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.85; // Very transparent background
            this.TransparencyKey = DarkBg; // Make background color transparent

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

            this.ResumeLayout(false);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE; // Prevent stealing focus
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Force window to stay on top AND not steal focus
            IntPtr exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, new IntPtr((int)exStyle | WS_EX_TOPMOST | WS_EX_NOACTIVATE));
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void CreateKeyboard()
        {
            // Keyboard panel
            _keyboardPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(960, 300),
                BackColor = DarkBg
            };
            this.Controls.Add(_keyboardPanel);

            // Create key layout with special keys integrated
            int maxCols = 12;
            _keyLabels = new Label[KeyRowCount, maxCols];

            int keyWidth = 60;
            int keyHeight = 50;
            int keySpacing = 8;
            int panelWidth = 960;

            // Row 0: Numbers
            CreateKeyRow(0, _keyboardLayoutAlpha[0], 0, keyWidth, keyHeight, keySpacing, panelWidth);

            // Row 1: QWERTYUIOP
            CreateKeyRow(1, _keyboardLayoutAlpha[1], 0, keyWidth, keyHeight, keySpacing, panelWidth);

            // Row 2: CAPS + ASDFGHJKL
            int row2Y = 2 * (keyHeight + keySpacing);
            // Caps Lock button
            _capsLockButton = new Button
            {
                Text = "CAPS",
                Location = new Point(10, row2Y),
                Size = new Size(80, keyHeight),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "CAPS",
                TabStop = false // Prevent Windows keyboard focus
            };
            _capsLockButton.FlatAppearance.BorderColor = BorderColor;
            _capsLockButton.FlatAppearance.BorderSize = 2;
            _capsLockButton.Click += (s, e) => ToggleCapsLock();
            _capsLockButton.Paint += (s, e) =>
            {
                var btn = s as Button;
                bool isSelected = btn.BackColor.R == SelectedColor.R &&
                                 btn.BackColor.G == SelectedColor.G &&
                                 btn.BackColor.B == SelectedColor.B;
                btn.FlatAppearance.BorderColor = isSelected ? BrightGreen : BorderColor;
            };
            _keyboardPanel.Controls.Add(_capsLockButton);

            CreateKeyRow(2, _keyboardLayoutAlpha[2], 98, keyWidth, keyHeight, keySpacing, panelWidth);

            // Row 3: SHIFT + ZXCVBNM
            int row3Y = 3 * (keyHeight + keySpacing);
            // Left Shift button
            _shiftButton = new Button
            {
                Text = "SHIFT",
                Location = new Point(10, row3Y),
                Size = new Size(100, keyHeight),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "SHIFT",
                TabStop = false // Prevent Windows keyboard focus
            };
            _shiftButton.FlatAppearance.BorderColor = BorderColor;
            _shiftButton.FlatAppearance.BorderSize = 2;
            _shiftButton.Click += (s, e) => ToggleShift();
            _shiftButton.Paint += (s, e) =>
            {
                var btn = s as Button;
                bool isSelected = btn.BackColor.R == SelectedColor.R &&
                                 btn.BackColor.G == SelectedColor.G &&
                                 btn.BackColor.B == SelectedColor.B;
                btn.FlatAppearance.BorderColor = isSelected ? BrightGreen : BorderColor;
            };
            _keyboardPanel.Controls.Add(_shiftButton);

            CreateKeyRow(3, _keyboardLayoutAlpha[3], 118, keyWidth, keyHeight, keySpacing, panelWidth);

            // Row 4: Special keys (Shift, Caps, Space, Enter, Delete)
            // These are handled separately in navigation logic
            int row4Y = 4 * (keyHeight + keySpacing);

            _spaceBarLabel = new Label
            {
                Text = "SPACE",
                Location = new Point((panelWidth - 400) / 2, row4Y),
                Size = new Size(400, keyHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                Tag = ' ',
                Cursor = Cursors.Hand
            };
            _spaceBarLabel.Click += (s, e) =>
            {
                if (s is Label lbl && lbl.Tag is char c)
                {
                    SendKeyToSystem(c);
                }
            };
            _spaceBarLabel.Paint += (s, e) =>
            {
                var lbl = s as Label;
                bool isSelected = lbl.BackColor.R == SelectedColor.R &&
                                 lbl.BackColor.G == SelectedColor.G &&
                                 lbl.BackColor.B == SelectedColor.B;
                using (var pen = new Pen(isSelected ? BrightGreen : BorderColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, lbl.Width - 3, lbl.Height - 3);
                }
            };
            _keyboardPanel.Controls.Add(_spaceBarLabel);

            _returnButton = new Button
            {
                Text = "ENTER",
                Location = new Point(690, row4Y),
                Size = new Size(120, keyHeight),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false // Prevent Windows keyboard focus
            };
            _returnButton.FlatAppearance.BorderColor = BorderColor;
            _returnButton.FlatAppearance.BorderSize = 2;
            _returnButton.Click += (s, e) => ReturnKey();
            _returnButton.Paint += (s, e) =>
            {
                var btn = s as Button;
                bool isSelected = btn.BackColor.R == SelectedColor.R &&
                                 btn.BackColor.G == SelectedColor.G &&
                                 btn.BackColor.B == SelectedColor.B;
                btn.FlatAppearance.BorderColor = isSelected ? BrightGreen : BorderColor;
            };
            _keyboardPanel.Controls.Add(_returnButton);

            _deleteButton = new Button
            {
                Text = "DEL",
                Location = new Point(820, row4Y),
                Size = new Size(120, keyHeight),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false // Prevent Windows keyboard focus
            };
            _deleteButton.FlatAppearance.BorderColor = BorderColor;
            _deleteButton.FlatAppearance.BorderSize = 2;
            _deleteButton.Click += (s, e) => Backspace();
            _deleteButton.Paint += (s, e) =>
            {
                var btn = s as Button;
                bool isSelected = btn.BackColor.R == SelectedColor.R &&
                                 btn.BackColor.G == SelectedColor.G &&
                                 btn.BackColor.B == SelectedColor.B;
                btn.FlatAppearance.BorderColor = isSelected ? BrightGreen : BorderColor;
            };
            _keyboardPanel.Controls.Add(_deleteButton);
        }

        private void CreateKeyRow(int rowIndex, string keys, int startX, int keyWidth, int keyHeight, int keySpacing, int panelWidth)
        {
            int y = rowIndex * (keyHeight + keySpacing);

            // If startX is 0, center the row
            if (startX == 0)
            {
                startX = (panelWidth - (keys.Length * (keyWidth + keySpacing))) / 2;
            }

            for (int col = 0; col < keys.Length; col++)
            {
                char key = keys[col];

                // Skip space - handled separately
                if (key == ' ') continue;

                int x = startX + col * (keyWidth + keySpacing);

                var keyLabel = new Label
                {
                    Text = key.ToString(),
                    Location = new Point(x, y),
                    Size = new Size(keyWidth, keyHeight),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = TextPrimary,
                    BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B),
                    Tag = key,
                    Cursor = Cursors.Hand
                };

                // Add click handler
                keyLabel.Click += (s, e) =>
                {
                    if (s is Label lbl && lbl.Tag is char c)
                    {
                        SendKeyToSystem(c);
                        // Reset shift after key press (but not caps lock)
                        if (_shiftActive)
                        {
                            _shiftActive = false;
                            UpdateShiftButton();
                            UpdateKeyLabels();
                        }
                    }
                };

                keyLabel.Paint += (s, e) =>
                {
                    var lbl = s as Label;
                    bool isSelected = lbl.BackColor.R == SelectedColor.R &&
                                     lbl.BackColor.G == SelectedColor.G &&
                                     lbl.BackColor.B == SelectedColor.B;

                    // Draw border
                    using (var pen = new Pen(isSelected ? BrightGreen : BorderColor, 2))
                    {
                        e.Graphics.DrawRectangle(pen, 1, 1, lbl.Width - 3, lbl.Height - 3);
                    }
                };

                _keyboardPanel.Controls.Add(keyLabel);
                _keyLabels[rowIndex, col] = keyLabel;
            }
        }

        private string GetKeyRow(int row) =>
            _symbolLayer ? _keyboardLayoutSymbol[row] : _keyboardLayoutAlpha[row];

        private void ApplyActiveKeyLayoutToLabels()
        {
            for (int row = 0; row < KeyRowCount; row++)
            {
                string keys = GetKeyRow(row);
                for (int col = 0; col < keys.Length; col++)
                {
                    if (_keyLabels[row, col] != null)
                    {
                        char c = keys[col];
                        _keyLabels[row, col].Tag = c;
                        _keyLabels[row, col].Text = c.ToString();
                    }
                }
            }

            UpdateKeyLabels();
        }

        private void ToggleSymbolLayer()
        {
            _shiftActive = false;
            _symbolLayer = !_symbolLayer;
            UpdateShiftButton();
            ApplyActiveKeyLayoutToLabels();
            UpdateSelection(false);
            PlayNavigationSound();
        }

        private static int MapColumnToSpecialKey(int column)
        {
            if (column < 2) return 0; // Shift
            if (column < 4) return 1; // Caps
            if (column < 7) return 2; // Space
            if (column < 9) return 3; // Enter
            return 4;                 // Delete
        }

        private static int MapSpecialKeyToColumn(int specialIndex)
        {
            if (specialIndex == 0) return 0;
            if (specialIndex == 1) return 2;
            if (specialIndex == 2) return 5;
            if (specialIndex == 3) return 7;
            return 9;
        }

        public void HandleInput(Gamepad gamepad)
        {
            // D-pad navigation
            bool dpadUp = (gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0;
            bool dpadDown = (gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0;
            bool dpadLeft = (gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0;
            bool dpadRight = (gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0;
            bool aButton = (gamepad.Buttons & GamepadButtonFlags.A) != 0;
            bool bButton = (gamepad.Buttons & GamepadButtonFlags.B) != 0;
            bool xButton = (gamepad.Buttons & GamepadButtonFlags.X) != 0;
            bool lbButton = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;

            // Controller-only inputs (keyboard is handled separately by KeyboardTimer_Tick)
            bool moveUp = dpadUp;
            bool moveDown = dpadDown;
            bool moveLeft = dpadLeft;
            bool moveRight = dpadRight;
            bool select = aButton;
            bool cancel = bButton;
            bool backspace = xButton;

            // Static variables to track button states
            if (!_buttonStates.ContainsKey("moveUp")) InitButtonStates();

            // On first frame, just update button states and return
            if (_firstInputFrame)
            {
                _buttonStates["moveUp"] = moveUp;
                _buttonStates["moveDown"] = moveDown;
                _buttonStates["moveLeft"] = moveLeft;
                _buttonStates["moveRight"] = moveRight;
                _buttonStates["select"] = select;
                _buttonStates["cancel"] = cancel;
                _buttonStates["backspace"] = backspace;
                _lbPressed = lbButton;
                _firstInputFrame = false;
                return;
            }

            // Toggle symbols / letters (Left Bumper)
            if (lbButton && !_lbPressed)
            {
                ToggleSymbolLayer();
            }
            _lbPressed = lbButton;

            // Move Up
            if (moveUp && !_buttonStates["moveUp"])
            {
                if (_currentRow > 0)
                {
                    _currentRow--;
                    // Adjust column/special key index if needed
                    if (_currentRow == SpecialRowIndex) // Special row
                    {
                        _specialKeyIndex = Math.Min(_specialKeyIndex, SpecialKeyCount - 1);
                    }
                    else
                    {
                        int maxCol = GetKeyRow(_currentRow).Length - 1;
                        if (_currentCol > maxCol) _currentCol = maxCol;
                    }
                    UpdateSelection(true);
                }
                else if (_currentRow == 0) // From row 0, wrap to special row 4
                {
                    _currentRow = SpecialRowIndex;
                    _specialKeyIndex = MapColumnToSpecialKey(_currentCol);
                    UpdateSelection(true);
                }
            }
            _buttonStates["moveUp"] = moveUp;

            // Move Down
            if (moveDown && !_buttonStates["moveDown"])
            {
                if (_currentRow < SpecialRowIndex)
                {
                    // Special handling for row 3 (ZXCVBNM) - C/V/B/N/M go to SPACE
                    if (_currentRow == 3 && _currentCol >= 2 && _currentCol <= 6) // C, V, B, N, M
                    {
                        _currentRow = SpecialRowIndex;
                        _specialKeyIndex = 2; // Space
                    }
                    else
                    {
                        _currentRow++;
                        // Adjust column/special key index if needed
                        if (_currentRow == SpecialRowIndex) // Special row
                        {
                            _specialKeyIndex = MapColumnToSpecialKey(_currentCol);
                        }
                        else
                        {
                            int maxCol = GetKeyRow(_currentRow).Length - 1;
                            if (_currentCol > maxCol) _currentCol = maxCol;
                        }
                    }
                    UpdateSelection(true);
                }
                else if (_currentRow == SpecialRowIndex) // From special row, wrap to row 0
                {
                    _currentRow = 0;
                    _currentCol = MapSpecialKeyToColumn(_specialKeyIndex);
                    UpdateSelection(true);
                }
            }
            _buttonStates["moveDown"] = moveDown;

            // Move Left
            if (moveLeft && !_buttonStates["moveLeft"])
            {
                if (_currentRow == SpecialRowIndex) // Special row
                {
                    if (_specialKeyIndex > 0)
                    {
                        _specialKeyIndex--;
                    }
                    else
                    {
                        _specialKeyIndex = SpecialKeyCount - 1;
                    }
                    UpdateSelection(true);
                }
                else if (_currentRow == 2 && _currentCol == 0) // From 'A', go to CAPS button
                {
                    _currentRow = SpecialRowIndex;
                    _specialKeyIndex = 1; // Caps
                    UpdateSelection(true);
                }
                else if (_currentRow == 3 && _currentCol == 0) // From 'Z', go to SHIFT button
                {
                    _currentRow = SpecialRowIndex;
                    _specialKeyIndex = 0; // Shift
                    UpdateSelection(true);
                }
                else
                {
                    if (_currentCol > 0)
                    {
                        _currentCol--;
                    }
                    else
                    {
                        // Wrap to end of row
                        _currentCol = GetKeyRow(_currentRow).Length - 1;
                    }
                    UpdateSelection(true);
                }
            }
            _buttonStates["moveLeft"] = moveLeft;

            // Move Right
            if (moveRight && !_buttonStates["moveRight"])
            {
                if (_currentRow == SpecialRowIndex) // Special row
                {
                    if (_specialKeyIndex < SpecialKeyCount - 1) // Move right in special row
                    {
                        _specialKeyIndex++;
                    }
                    else
                    {
                        // Wrap from Delete to Shift
                        _specialKeyIndex = 0;
                    }
                    UpdateSelection(true);
                }
                else
                {
                    int maxCol = GetKeyRow(_currentRow).Length - 1;
                    if (_currentCol < maxCol)
                    {
                        _currentCol++;
                    }
                    else if (_currentRow == 2) // From 'L', go to CAPS button
                    {
                        _currentRow = SpecialRowIndex;
                        _specialKeyIndex = 1; // Caps
                    }
                    else
                    {
                        // Wrap to start of row
                        _currentCol = 0;
                    }
                    UpdateSelection(true);
                }
            }
            _buttonStates["moveRight"] = moveRight;

            // Select key
            if (select && !_buttonStates["select"])
            {
                SelectCurrentKey();
            }
            _buttonStates["select"] = select;

            // Cancel - close keyboard
            if (cancel && !_buttonStates["cancel"])
            {
                CloseKeyboard();
            }
            _buttonStates["cancel"] = cancel;

            // Backspace
            if (backspace && !_buttonStates["backspace"])
            {
                Backspace();
            }
            _buttonStates["backspace"] = backspace;
        }

        private void KeyboardTimer_Tick(object sender, EventArgs e)
        {
            // Poll keyboard state for arrow key navigation
            bool leftNow = (GetAsyncKeyState(0x25) & 0x8000) != 0;  // VK_LEFT
            bool rightNow = (GetAsyncKeyState(0x27) & 0x8000) != 0; // VK_RIGHT
            bool upNow = (GetAsyncKeyState(0x26) & 0x8000) != 0;    // VK_UP
            bool downNow = (GetAsyncKeyState(0x28) & 0x8000) != 0;  // VK_DOWN
            bool enterNow = (GetAsyncKeyState(0x0D) & 0x8000) != 0; // VK_RETURN
            bool escNow = (GetAsyncKeyState(0x1B) & 0x8000) != 0;   // VK_ESCAPE

            // Left arrow - edge detection
            if (leftNow && !_leftPressed)
            {
                if (_currentRow == SpecialRowIndex) // Special row
                {
                    if (_specialKeyIndex > 0)
                    {
                        _specialKeyIndex--; UpdateSelection(true);
                    }
                    else
                    {
                        _specialKeyIndex = SpecialKeyCount - 1; UpdateSelection(true);
                    }
                }
                else if (_currentRow == 2 && _currentCol == 0) // From 'A', go to CAPS
                {
                    _currentRow = SpecialRowIndex; _specialKeyIndex = 1; UpdateSelection(true);
                }
                else if (_currentRow == 3 && _currentCol == 0) // From 'Z', go to SHIFT
                {
                    _currentRow = SpecialRowIndex; _specialKeyIndex = 0; UpdateSelection(true);
                }
                else
                {
                    if (_currentCol > 0) { _currentCol--; }
                    else { _currentCol = GetKeyRow(_currentRow).Length - 1; } // Wrap to end
                    UpdateSelection(true);
                }
            }
            _leftPressed = leftNow;

            // Right arrow - edge detection
            if (rightNow && !_rightPressed)
            {
                if (_currentRow == SpecialRowIndex) // Special row
                {
                    if (_specialKeyIndex < SpecialKeyCount - 1) { _specialKeyIndex++; UpdateSelection(true); }
                    else { _specialKeyIndex = 0; UpdateSelection(true); }
                }
                else
                {
                    int maxCol = GetKeyRow(_currentRow).Length - 1;
                    if (_currentCol < maxCol) { _currentCol++; }
                    else if (_currentRow == 2) { _currentRow = SpecialRowIndex; _specialKeyIndex = 1; }
                    else { _currentCol = 0; }
                    UpdateSelection(true);
                }
            }
            _rightPressed = rightNow;

            // Up arrow - edge detection
            if (upNow && !_upPressed)
            {
                if (_currentRow > 0)
                {
                    _currentRow--;
                    if (_currentRow == SpecialRowIndex) _specialKeyIndex = Math.Min(_specialKeyIndex, SpecialKeyCount - 1);
                    else { int maxCol = GetKeyRow(_currentRow).Length - 1; if (_currentCol > maxCol) _currentCol = maxCol; }
                    UpdateSelection(true);
                }
                else if (_currentRow == 0) // From row 0, wrap to special row
                {
                    _currentRow = SpecialRowIndex;
                    _specialKeyIndex = MapColumnToSpecialKey(_currentCol);
                    UpdateSelection(true);
                }
            }
            _upPressed = upNow;

            // Down arrow - edge detection
            if (downNow && !_downPressed)
            {
                if (_currentRow < SpecialRowIndex)
                {
                    // From row 3 (ZXCVBNM), C/V/B/N/M go to SPACE
                    if (_currentRow == 3 && _currentCol >= 2 && _currentCol <= 6)
                    {
                        _currentRow = SpecialRowIndex; _specialKeyIndex = 2;
                    }
                    else
                    {
                        _currentRow++;
                        if (_currentRow == SpecialRowIndex) { _specialKeyIndex = MapColumnToSpecialKey(_currentCol); }
                        else { int maxCol = GetKeyRow(_currentRow).Length - 1; if (_currentCol > maxCol) _currentCol = maxCol; }
                    }
                    UpdateSelection(true);
                }
                else if (_currentRow == SpecialRowIndex) // From special row, wrap to row 0
                {
                    _currentRow = 0;
                    _currentCol = MapSpecialKeyToColumn(_specialKeyIndex);
                    UpdateSelection(true);
                }
            }
            _downPressed = downNow;

            // Enter - edge detection
            if (enterNow && !_enterPressed)
            {
                SelectCurrentKey();
            }
            _enterPressed = enterNow;

            // Escape - edge detection
            if (escNow && !_escPressed)
            {
                CloseKeyboard();
            }
            _escPressed = escNow;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private System.Collections.Generic.Dictionary<string, bool> _buttonStates =
            new System.Collections.Generic.Dictionary<string, bool>();

        private void InitButtonStates()
        {
            // Initialize all states to false
            _buttonStates["moveUp"] = false;
            _buttonStates["moveDown"] = false;
            _buttonStates["moveLeft"] = false;
            _buttonStates["moveRight"] = false;
            _buttonStates["select"] = false;
            _buttonStates["cancel"] = false;
            _buttonStates["backspace"] = false;
        }

        private void UpdateSelection(bool playSound = true)
        {
            // Reset all keys to default color
            for (int row = 0; row < KeyRowCount; row++)
            {
                for (int col = 0; col < GetKeyRow(row).Length; col++)
                {
                    if (_keyLabels[row, col] != null)
                    {
                        _keyLabels[row, col].BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                        _keyLabels[row, col].Invalidate();
                    }
                }
            }

            // Always reset special keys to their base state (active or inactive) - clear selection
            if (_shiftButton != null)
            {
                _shiftButton.BackColor = _shiftActive ?
                    Color.FromArgb(240, BrightGreen.R, BrightGreen.G, BrightGreen.B) :
                    Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                _shiftButton.Invalidate();
            }
            if (_capsLockButton != null)
            {
                _capsLockButton.BackColor = _capsLockActive ?
                    Color.FromArgb(240, BrightGreen.R, BrightGreen.G, BrightGreen.B) :
                    Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                _capsLockButton.Invalidate();
            }
            if (_spaceBarLabel != null)
            {
                _spaceBarLabel.BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                _spaceBarLabel.Invalidate();
            }
            if (_returnButton != null)
            {
                _returnButton.BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                _returnButton.Invalidate();
            }
            if (_deleteButton != null)
            {
                _deleteButton.BackColor = Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
                _deleteButton.Invalidate();
            }

            // Now highlight current selection
            if (_currentRow == SpecialRowIndex) // Special row - highlight the selected special key
            {
                Control selectedControl = null;
                switch (_specialKeyIndex)
                {
                    case 0: selectedControl = _shiftButton; break;
                    case 1: selectedControl = _capsLockButton; break;
                    case 2: selectedControl = _spaceBarLabel; break;
                    case 3: selectedControl = _returnButton; break;
                    case 4: selectedControl = _deleteButton; break;
                }
                if (selectedControl != null)
                {
                    selectedControl.BackColor = Color.FromArgb(240, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                    selectedControl.Invalidate();
                }
            }
            else if (_currentRow < SpecialRowIndex) // Regular key row
            {
                if (_keyLabels[_currentRow, _currentCol] != null)
                {
                    _keyLabels[_currentRow, _currentCol].BackColor = Color.FromArgb(240, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                    _keyLabels[_currentRow, _currentCol].Invalidate();
                }
            }

            // Play navigation sound only if requested
            if (playSound)
            {
                PlayNavigationSound();
            }
        }

        private void SelectCurrentKey()
        {
            if (_currentRow == SpecialRowIndex) // Special row
            {
                PlayKeyPressSound();
                switch (_specialKeyIndex)
                {
                    case 0: // Shift
                        ToggleShift();
                        break;
                    case 1: // Caps Lock
                        ToggleCapsLock();
                        break;
                    case 2: // Space
                        SendKeyToSystem(' ');
                        break;
                    case 3: // Enter
                        ReturnKey();
                        break;
                    case 4: // Delete
                        Backspace();
                        break;
                }
            }
            else
            {
                var keyLabel = _keyLabels[_currentRow, _currentCol];
                if (keyLabel != null && keyLabel.Tag is char key)
                {
                    // Send the key to the previously active window
                    SendKeyToSystem(key);

                    // Reset shift after key press (but not caps lock)
                    if (_shiftActive)
                    {
                        _shiftActive = false;
                        UpdateShiftButton();
                        UpdateKeyLabels();
                    }
                }
            }
        }

        private void SendKeyToSystem(char key)
        {
            // Play key press sound
            PlayKeyPressSound();

            // Determine if we should send uppercase
            bool shouldBeUppercase = _shiftActive || _capsLockActive;

            // For letters, convert case if needed
            char finalKey = key;
            if (char.IsLetter(key))
            {
                finalKey = shouldBeUppercase ? char.ToUpper(key) : char.ToLower(key);
            }

            // Convert character to virtual key code and send it
            short vkCode = VkKeyScan(finalKey);
            if (vkCode != -1)
            {
                byte virtualKey = (byte)(vkCode & 0xFF);
                bool needsShift = (vkCode & 0x100) != 0;

                if (needsShift)
                {
                    InputSimulator.SendKey(0x10, true); // Shift down
                    System.Threading.Thread.Sleep(10);
                }

                InputSimulator.PressKey(virtualKey);
                System.Threading.Thread.Sleep(10);

                if (needsShift)
                {
                    InputSimulator.SendKey(0x10, false); // Shift up
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private void Backspace()
        {
            // Send backspace to the system
            InputSimulator.PressKey(0x08); // VK_BACK
        }

        private void ReturnKey()
        {
            // Send Enter to the system
            InputSimulator.PressKey(0x0D); // VK_RETURN
        }

        private void ToggleShift()
        {
            _shiftActive = !_shiftActive;
            UpdateShiftButton();
            UpdateKeyLabels();
        }

        private void ToggleCapsLock()
        {
            _capsLockActive = !_capsLockActive;
            UpdateCapsLockButton();
            UpdateKeyLabels();
        }

        private void LoadSounds()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                // Load navigation sound (boop)
                var boopStream = assembly.GetManifestResourceStream("HaloShift.sound_boop.wav");
                if (boopStream != null)
                {
                    _navigationSound = new SoundPlayer(boopStream);
                    _navigationSound.Load();
                }

                // Load key press sound (glitch)
                var glitchStream = assembly.GetManifestResourceStream("HaloShift.sound_glitch.wav");
                if (glitchStream != null)
                {
                    _keyPressSound = new SoundPlayer(glitchStream);
                    _keyPressSound.Load();
                }
            }
            catch { }
        }

        private void PlayNavigationSound()
        {
            try
            {
                _navigationSound?.Play();
            }
            catch { }
        }

        private void PlayKeyPressSound()
        {
            try
            {
                _keyPressSound?.Play();
            }
            catch { }
        }

        private void UpdateShiftButton()
        {
            if (_shiftButton != null)
            {
                _shiftButton.BackColor = _shiftActive ?
                    Color.FromArgb(240, BrightGreen.R, BrightGreen.G, BrightGreen.B) :
                    Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
            }
        }

        private void UpdateCapsLockButton()
        {
            if (_capsLockButton != null)
            {
                _capsLockButton.BackColor = _capsLockActive ?
                    Color.FromArgb(240, BrightGreen.R, BrightGreen.G, BrightGreen.B) :
                    Color.FromArgb(240, CardBg.R, CardBg.G, CardBg.B);
            }
        }

        private void UpdateKeyLabels()
        {
            // Update all letter keys to show uppercase or lowercase
            bool shouldBeUppercase = _shiftActive || _capsLockActive;

            for (int row = 0; row < KeyRowCount; row++)
            {
                for (int col = 0; col < GetKeyRow(row).Length; col++)
                {
                    if (_keyLabels[row, col] != null)
                    {
                        char key = (char)_keyLabels[row, col].Tag;
                        if (char.IsLetter(key))
                        {
                            _keyLabels[row, col].Text = shouldBeUppercase ?
                                key.ToString().ToUpper() :
                                key.ToString().ToLower();
                        }
                    }
                }
            }
        }

        private void PerformClose()
        {
            _shiftActive = false;
            _capsLockActive = false;
            _symbolLayer = false;
            UpdateShiftButton();
            UpdateCapsLockButton();
            ApplyActiveKeyLayoutToLabels();

            KeyboardClosed?.Invoke(this, EventArgs.Empty);
            _keyboardTimer?.Stop();
            Hide();
        }

        private void CloseKeyboard()
        {
            PerformClose();
        }

        /// <summary>Closes the keyboard and attempts to return focus to the window that was active when the keyboard opened.</summary>
        public void DismissRestoringPreviousFocus()
        {
            if (!Visible)
                return;

            IntPtr previous = _previousWindow;
            PerformClose();

            if (previous != IntPtr.Zero && previous != Handle)
                SetForegroundWindow(previous);
        }

        public void ShowKeyboard()
        {
            // Store the currently active window before showing keyboard
            _previousWindow = GetForegroundWindow();

            _currentRow = 0;
            _currentCol = 0;
            _specialKeyIndex = 0;
            _shiftActive = false;
            _capsLockActive = false;
            _symbolLayer = false;
            _lbPressed = false;
            _firstInputFrame = true; // Reset first frame flag

            // Reset button states to prevent carryover from previous session
            _buttonStates.Clear();
            InitButtonStates();

            // Reset keyboard state tracking
            _leftPressed = (GetAsyncKeyState(0x25) & 0x8000) != 0;
            _rightPressed = (GetAsyncKeyState(0x27) & 0x8000) != 0;
            _upPressed = (GetAsyncKeyState(0x26) & 0x8000) != 0;
            _downPressed = (GetAsyncKeyState(0x28) & 0x8000) != 0;
            _enterPressed = (GetAsyncKeyState(0x0D) & 0x8000) != 0;
            _escPressed = (GetAsyncKeyState(0x1B) & 0x8000) != 0;

            UpdateShiftButton();
            UpdateCapsLockButton();
            UpdateKeyLabels();
            UpdateSelection(false); // Don't play sound when opening keyboard
            this.Show();

            // Start keyboard polling timer
            _keyboardTimer?.Start();

            // Re-enforce topmost after showing
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }
}
