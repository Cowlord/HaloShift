using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using SharpDX.XInput;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace HaloShift
{
    public enum KeyType
    {
        Character,
        Function,
        Modifier,
        LayerToggle
    }

    public partial class VirtualKeyboardWindow : Window
    {
        private readonly ObservableCollection<KeyboardRow> _rows = new();
        private bool _symbolLayer = false;
        private bool _shiftActive = false;
        private bool _capsLockActive = false;
        private bool _ctrlActive = false;
        private bool _altActive = false;
        private bool _winActive = false;
        private bool _firstInputFrame = true;
        private int _currentRow = 0;
        private int _currentCol = 0;
        private bool _dpadUpPressed;
        private bool _dpadDownPressed;
        private bool _dpadLeftPressed;
        private bool _dpadRightPressed;
        private bool _selectPressed;
        private bool _cancelPressed;
        private bool _backspacePressed;

        public event EventHandler? KeyboardClosed;

        public ObservableCollection<KeyboardRow> Rows => _rows;

        public VirtualKeyboardWindow()
        {
            InitializeComponent();
            DataContext = this;
            BuildKeyboardRows();
            UpdateSelection();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ShowKeyboard()
        {
            Show();
            Activate();

            // Position at the bottom-center of the primary screen
            var screens = Screens.All;
            var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault();

            if (primaryScreen != null)
            {
                var workingArea = primaryScreen.WorkingArea;

                // Center horizontally, position at bottom with small margin
                double x = workingArea.Position.X + (workingArea.Size.Width - Width) / 2;
                double y = workingArea.Position.Y + workingArea.Size.Height - Height - 8;

                Position = new PixelPoint((int)x, (int)y);
            }

            _firstInputFrame = true;
            _currentRow = 0;
            _currentCol = 0;
            UpdateSelection();
        }

        public void DismissRestoringPreviousFocus()
        {
            Hide();
            KeyboardClosed?.Invoke(this, EventArgs.Empty);
        }

        public void HandleInput(Gamepad gamepad)
        {
            if (_firstInputFrame)
            {
                _firstInputFrame = false;
            }

            bool up = (gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0;
            bool down = (gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0;
            bool left = (gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0;
            bool right = (gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0;
            bool select = (gamepad.Buttons & GamepadButtonFlags.A) != 0;
            bool cancel = (gamepad.Buttons & GamepadButtonFlags.B) != 0;
            bool backspace = (gamepad.Buttons & GamepadButtonFlags.X) != 0;

            if (up && !_dpadUpPressed)
                MoveUp();
            if (down && !_dpadDownPressed)
                MoveDown();
            if (left && !_dpadLeftPressed)
                MoveLeft();
            if (right && !_dpadRightPressed)
                MoveRight();
            if (select && !_selectPressed)
                SelectCurrentKey();
            if (cancel && !_cancelPressed)
                HideKeyboard();
            if (backspace && !_backspacePressed)
                Backspace();

            _dpadUpPressed = up;
            _dpadDownPressed = down;
            _dpadLeftPressed = left;
            _dpadRightPressed = right;
            _selectPressed = select;
            _cancelPressed = cancel;
            _backspacePressed = backspace;
        }

        private void MoveUp()
        {
            if (_currentRow > 0)
            {
                _currentRow--;
                _currentCol = Math.Min(_currentCol, Rows[_currentRow].Keys.Count - 1);
                UpdateSelection();
            }
        }

        private void MoveDown()
        {
            if (_currentRow < Rows.Count - 1)
            {
                _currentRow++;
                _currentCol = Math.Min(_currentCol, Rows[_currentRow].Keys.Count - 1);
                UpdateSelection();
            }
        }

        private void MoveLeft()
        {
            if (_currentCol > 0)
            {
                _currentCol--;
            }
            else if (_currentRow > 0)
            {
                _currentRow--;
                _currentCol = Rows[_currentRow].Keys.Count - 1;
            }
            UpdateSelection();
        }

        private void MoveRight()
        {
            if (_currentCol < Rows[_currentRow].Keys.Count - 1)
            {
                _currentCol++;
            }
            else if (_currentRow < Rows.Count - 1)
            {
                _currentRow++;
                _currentCol = 0;
            }
            UpdateSelection();
        }

        private void BuildKeyboardRows()
        {
            Rows.Clear();

            // Row 1: ESC + F1-F12 (thinner row with smaller height)
            AddRow(
                CreateFunctionKey("ESC", 0x1B, 60, 36),
                CreateFunctionKey("F1", 0x70, 50, 36),
                CreateFunctionKey("F2", 0x71, 50, 36),
                CreateFunctionKey("F3", 0x72, 50, 36),
                CreateFunctionKey("F4", 0x73, 50, 36),
                CreateFunctionKey("F5", 0x74, 50, 36),
                CreateFunctionKey("F6", 0x75, 50, 36),
                CreateFunctionKey("F7", 0x76, 50, 36),
                CreateFunctionKey("F8", 0x77, 50, 36),
                CreateFunctionKey("F9", 0x78, 50, 36),
                CreateFunctionKey("F10", 0x79, 50, 36),
                CreateFunctionKey("F11", 0x7A, 50, 36),
                CreateFunctionKey("F12", 0x7B, 50, 36)
            );

            // Row 2: TAB + Numbers + Back + right-side nav cluster
            AddRow(
                CreateFunctionKey("TAB", 0x09, 80),
                CreateCharKey('1', '!'),
                CreateCharKey('2', '@'),
                CreateCharKey('3', '#'),
                CreateCharKey('4', '$'),
                CreateCharKey('5', '%'),
                CreateCharKey('6', '^'),
                CreateCharKey('7', '&'),
                CreateCharKey('8', '*'),
                CreateCharKey('9', '('),
                CreateCharKey('0', ')'),
                CreateCharKey('-', '_'),
                CreateCharKey('=', '+'),
                CreateFunctionKey("BACK", 0x08, 90),
                CreateFunctionKey("INS", 0x2D, 50, 36),
                CreateFunctionKey("HOME", 0x24, 50, 36),
                CreateFunctionKey("PGUP", 0x21, 50, 36),
                CreateFunctionKey("LEFT", 0x25, 50, 36),
                CreateFunctionKey("DOWN", 0x28, 50, 36),
                CreateFunctionKey("RIGHT", 0x27, 50, 36),
                CreateFunctionKey("END", 0x23, 50, 36),
                CreateFunctionKey("PGDN", 0x22, 50, 36)
            );

            // Row 3: Q row + right-side special cluster
            AddRow(
                CreateCharKey('Q', null, 80),
                CreateCharKey('W', null),
                CreateCharKey('E', null),
                CreateCharKey('R', null),
                CreateCharKey('T', null),
                CreateCharKey('Y', null),
                CreateCharKey('U', null),
                CreateCharKey('I', null),
                CreateCharKey('O', null),
                CreateCharKey('P', null),
                CreateCharKey('[', '{'),
                CreateCharKey(']', '}'),
                CreateCharKey('\\', '|', 80),
                CreateFunctionKey("CAPS", 0x14, 50, 36),
                CreateFunctionKey("PRTSC", 0x2C, 50, 36),
                CreateFunctionKey("SCRLK", 0x91, 50, 36),
                CreateFunctionKey("PAUSE", 0x13, 50, 36),
                CreateFunctionKey("DEL", 0x2E, 50, 36)
            );

            // Row 4: CAPS + A row + ENTER
            AddRow(
                CreateModifierKey("CAPS", 0x14, 100),
                CreateCharKey('A', null),
                CreateCharKey('S', null),
                CreateCharKey('D', null),
                CreateCharKey('F', null),
                CreateCharKey('G', null),
                CreateCharKey('H', null),
                CreateCharKey('J', null),
                CreateCharKey('K', null),
                CreateCharKey('L', null),
                CreateCharKey(';', ':'),
                CreateCharKey('\'', '"'),
                CreateFunctionKey("ENTER", 0x0D, 110)
            );

            // Row 5: SHIFT + Z row + SHIFT
            AddRow(
                CreateModifierKey("SHIFT", 0x10, 120),
                CreateCharKey('Z', null),
                CreateCharKey('X', null),
                CreateCharKey('C', null),
                CreateCharKey('V', null),
                CreateCharKey('B', null),
                CreateCharKey('N', null),
                CreateCharKey('M', null),
                CreateCharKey(',', '<'),
                CreateCharKey('.', '>'),
                CreateCharKey('/', '?'),
                CreateModifierKey("SHIFT", 0x10, 120)
            );

            // Row 6: Bottom row with modifiers and space
            AddRow(
                CreateLayerToggleKey(_symbolLayer ? "ABC" : "SYM", 80),
                CreateModifierKey("CTRL", 0x11, 80),
                CreateModifierKey("WIN", 0x5B, 80),
                CreateModifierKey("ALT", 0x12, 80),
                CreateFunctionKey("SPACE", 0x20, 340),
                CreateModifierKey("ALT", 0x12, 80),
                CreateModifierKey("WIN", 0x5B, 80),
                CreateModifierKey("CTRL", 0x11, 80)
            );

            UpdateKeyLabels();
            ApplyModifierStates();
        }

        private void AddRow(params KeyViewModel[] keys)
        {
            var row = new KeyboardRow();
            foreach (var key in keys)
            {
                row.Keys.Add(key);
            }
            Rows.Add(row);
        }

        private KeyViewModel CreateCharKey(char primary, char? alternate, double width = 70, double height = 48)
        {
            return new KeyViewModel(primary, alternate, width, height, KeyType.Character);
        }

        private KeyViewModel CreateFunctionKey(string label, byte virtualKey, double width = 70, double height = 48)
        {
            return new KeyViewModel(label, null, width, height, KeyType.Function, virtualKey);
        }

        private KeyViewModel CreateFunctionKey(string label, double width = 70, double height = 48)
        {
            return new KeyViewModel(label, null, width, height, KeyType.Function, null);
        }

        private KeyViewModel CreateModifierKey(string label, byte virtualKey, double width, double height = 48)
        {
            return new KeyViewModel(label, null, width, height, KeyType.Modifier, virtualKey);
        }

        private KeyViewModel CreateLayerToggleKey(string label, double width, double height = 48)
        {
            return new KeyViewModel(label, null, width, height, KeyType.LayerToggle);
        }

        private void UpdateKeyLabels()
        {
            foreach (var row in Rows)
            {
                foreach (var key in row.Keys)
                {
                    if (key.KeyType == KeyType.Character)
                    {
                        key.UpdateLabel(_symbolLayer, _shiftActive, _capsLockActive);
                    }
                    else if (key.KeyType == KeyType.LayerToggle)
                    {
                        key.Label = _symbolLayer ? "ABC" : "SYM";
                    }
                }
            }
        }

        private void ApplyModifierStates()
        {
            foreach (var row in Rows)
            {
                foreach (var key in row.Keys)
                {
                    if (key.KeyType != KeyType.Modifier)
                        continue;

                    key.IsActive = key.Label switch
                    {
                        "CTRL" => _ctrlActive,
                        "ALT" => _altActive,
                        "WIN" => _winActive,
                        "SHIFT" => _shiftActive,
                        "CAPS" => _capsLockActive,
                        _ => false
                    };
                }
            }
        }

        private void UpdateSelection()
        {
            foreach (var row in Rows)
            {
                foreach (var key in row.Keys)
                {
                    key.IsSelected = false;
                }
            }

            if (Rows.Count == 0)
                return;

            _currentRow = Math.Clamp(_currentRow, 0, Rows.Count - 1);
            _currentCol = Math.Clamp(_currentCol, 0, Rows[_currentRow].Keys.Count - 1);
            Rows[_currentRow].Keys[_currentCol].IsSelected = true;
        }

        private void SelectCurrentKey()
        {
            var key = Rows[_currentRow].Keys[_currentCol];

            switch (key.KeyType)
            {
                case KeyType.LayerToggle:
                    _symbolLayer = !_symbolLayer;
                    UpdateKeyLabels();
                    break;
                case KeyType.Modifier:
                    ToggleModifier(key);
                    break;
                case KeyType.Function:
                    if (key.VirtualKey.HasValue)
                        SendVirtualKey(key.VirtualKey.Value);
                    break;
                case KeyType.Character:
                    if (key.PrimaryCharacter.HasValue)
                        SendCharacterKey(key);
                    break;
            }

            ApplyModifierStates();
        }

        private void ToggleModifier(KeyViewModel key)
        {
            switch (key.Label)
            {
                case "CTRL":
                    _ctrlActive = !_ctrlActive;
                    break;
                case "ALT":
                    _altActive = !_altActive;
                    break;
                case "WIN":
                    _winActive = !_winActive;
                    break;
                case "SHIFT":
                    _shiftActive = !_shiftActive;
                    break;
                case "CAPS":
                    _capsLockActive = !_capsLockActive;
                    break;
            }

            ApplyModifierStates();
        }

        private void SendCharacterKey(KeyViewModel key)
        {
            char character = key.GetActiveCharacter(_symbolLayer);
            bool shiftRequired = key.RequiresShift(_symbolLayer, _shiftActive, _capsLockActive);
            var modifiers = new ModifierState
            {
                Ctrl = _ctrlActive,
                Alt = _altActive,
                Win = _winActive,
                Shift = shiftRequired
            };

            SendVirtualKeyWithModifiers(character, modifiers);
        }

        private void SendVirtualKey(byte virtualKey)
        {
            var modifiers = new ModifierState
            {
                Ctrl = _ctrlActive,
                Alt = _altActive,
                Win = _winActive,
                Shift = _shiftActive
            };

            SendVirtualKeyWithModifiers(virtualKey, modifiers);
        }

        private void SendVirtualKeyWithModifiers(char character, ModifierState modifiers)
        {
            short vkCode = VkKeyScan(character);
            if (vkCode == -1)
                return;

            byte virtualKey = (byte)(vkCode & 0xFF);
            bool needsShift = (vkCode & 0x100) != 0;
            if (needsShift && !modifiers.Shift)
                modifiers.Shift = true;

            SendVirtualKeyWithModifiers(virtualKey, modifiers);
        }

        private void SendVirtualKeyWithModifiers(byte virtualKey, ModifierState modifiers)
        {
            if (modifiers.Ctrl)
                InputSimulator.SendKey(0x11, true);
            if (modifiers.Alt)
                InputSimulator.SendKey(0x12, true);
            if (modifiers.Win)
                InputSimulator.SendKey(0x5B, true);
            if (modifiers.Shift)
                InputSimulator.SendKey(0x10, true);

            InputSimulator.PressKey(virtualKey);

            if (modifiers.Shift)
                InputSimulator.SendKey(0x10, false);
            if (modifiers.Win)
                InputSimulator.SendKey(0x5B, false);
            if (modifiers.Alt)
                InputSimulator.SendKey(0x12, false);
            if (modifiers.Ctrl)
                InputSimulator.SendKey(0x11, false);
        }

        private void HideKeyboard()
        {
            Hide();
            KeyboardClosed?.Invoke(this, EventArgs.Empty);
        }

        private void Backspace()
        {
            SendVirtualKey(0x08);
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
    }

    public class KeyboardRow
    {
        public ObservableCollection<KeyViewModel> Keys { get; } = new();
    }

    public class ModifierState
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Win { get; set; }
        public bool Shift { get; set; }
    }

    public class KeyViewModel : INotifyPropertyChanged
    {
        private string _label;
        private IBrush _background;
        private bool _isSelected;
        private bool _isActive;

        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }
            }
        }

        public string Display => _label;

        public char? PrimaryCharacter { get; }
        public char? AlternateCharacter { get; }
        public double Width { get; }
        public double Height { get; }
        public KeyType KeyType { get; }
        public byte? VirtualKey { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    UpdateBackground();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    UpdateBackground();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
                }
            }
        }

        public IBrush Background
        {
            get => _background;
            private set
            {
                if (_background != value)
                {
                    _background = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public KeyViewModel(char primaryCharacter, char? alternateCharacter, double width, double height, KeyType keyType, byte? virtualKey = null)
        {
            PrimaryCharacter = primaryCharacter;
            AlternateCharacter = alternateCharacter;
            _label = primaryCharacter.ToString();
            Width = width;
            Height = height;
            KeyType = keyType;
            VirtualKey = virtualKey;
            _background = Brushes.Transparent;
        }

        public KeyViewModel(string label, char? alternateCharacter, double width, double height, KeyType keyType, byte? virtualKey = null)
        {
            _label = label;
            PrimaryCharacter = null;
            AlternateCharacter = alternateCharacter;
            Width = width;
            Height = height;
            KeyType = keyType;
            VirtualKey = virtualKey;
            _background = Brushes.Transparent;
        }

        public void UpdateLabel(bool symbolLayer, bool shiftActive, bool capsLockActive)
        {
            if (KeyType != KeyType.Character || !PrimaryCharacter.HasValue)
                return;

            char current = symbolLayer && AlternateCharacter.HasValue ? AlternateCharacter.Value : PrimaryCharacter.Value;
            if (char.IsLetter(current))
            {
                Label = (capsLockActive || shiftActive) ? char.ToUpper(current).ToString() : char.ToLower(current).ToString();
            }
            else
            {
                Label = current.ToString();
            }
        }

        public char GetActiveCharacter(bool symbolLayer)
        {
            return symbolLayer && AlternateCharacter.HasValue ? AlternateCharacter.Value : PrimaryCharacter ?? '\0';
        }

        public bool RequiresShift(bool symbolLayer, bool shiftActive, bool capsLockActive)
        {
            char current = GetActiveCharacter(symbolLayer);
            if (!char.IsLetter(current))
            {
                short vk = VkKeyScan(current);
                return (vk & 0x100) != 0 || shiftActive;
            }

            return shiftActive || capsLockActive;
        }

        private void UpdateBackground()
        {
            if (IsSelected)
            {
                Background = Brushes.DodgerBlue;
            }
            else if (IsActive)
            {
                Background = Brushes.DimGray;
            }
            else
            {
                Background = Brushes.Transparent;
            }
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
    }
}
