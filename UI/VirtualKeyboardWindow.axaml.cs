using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
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

    public enum KeyboardNavigationZone
    {
        Main,
        NavCluster
    }

    public partial class VirtualKeyboardWindow : Window
    {
        private const double ClusterKeyWidth = 50;
        private const double ClusterKeyHeight = 36;
        private const double ClusterGapWidth = 50;
        private const int TopRowIndex = 0;
        private const int FirstNavigableRowIndex = 1;
        private const int NavClusterRowCount = 3;
        private const int ArrowUpRowIndex = 3;
        private const int ArrowKeysRowIndex = 4;

        private static readonly HashSet<string> ExcludedNavLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "PRTSC", "SCRLK", "PAUSE",
            "INS", "HOME", "PGUP", "DEL", "END", "PGDN",
            "UP", "DOWN", "LEFT", "RIGHT"
        };

        private readonly ObservableCollection<KeyboardRow> _rows = new();
        private bool _symbolLayer;
        private bool _shiftActive;
        private bool _capsLockActive;
        private bool _ctrlActive;
        private bool _altActive;
        private bool _winActive;
        private bool _firstInputFrame = true;
        private bool _hasBeenShown;
        private KeyboardNavigationZone _zone = KeyboardNavigationZone.Main;
        private int _currentRow;
        private int _currentCol;
        private int _navRow;
        private int _navCol;
        private int _savedMainRow;
        private int _savedMainCol;
        private bool _rbPressed;
        private bool _lbRbComboPressed;
        private bool _dpadUpPressed;
        private bool _dpadDownPressed;
        private bool _dpadLeftPressed;
        private bool _dpadRightPressed;
        private bool _selectPressed;
        private bool _cancelPressed;
        private bool _backspacePressed;
        private bool _lbPressed;
        private DispatcherTimer? _arrowPulseTimer;
        private IntPtr _previousWindow;

        public event EventHandler? KeyboardClosed;

        public bool IsKeyboardOpen { get; private set; }

        public bool IsNavClusterActive => _zone == KeyboardNavigationZone.NavCluster;

        public ObservableCollection<KeyboardRow> Rows => _rows;

        public VirtualKeyboardWindow()
        {
            InitializeComponent();
            DataContext = this;
            BuildKeyboardRows();
            UpdateSelection();
            // Don't call Hide() - IsVisible is already set to False in XAML

            // Prevent window from ever actually closing
            // The app manages visibility with IsVisible property
            Closing += (sender, e) => e.Cancel = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ShowKeyboard()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(ShowKeyboard);
                return;
            }

            _previousWindow = GetForegroundWindow();
            IsKeyboardOpen = true;

            if (!_hasBeenShown)
            {
                Show();
                _hasBeenShown = true;
            }

            IsVisible = true;
            Activate();

            var screens = Screens.All;
            var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault();

            if (primaryScreen != null)
            {
                var workingArea = primaryScreen.WorkingArea;
                double x = workingArea.Position.X + (workingArea.Size.Width - Width) / 2;
                double y = workingArea.Position.Y + workingArea.Size.Height - Height - 8;
                Position = new PixelPoint((int)x, (int)y);
            }

            ResetNavigationZone();
            _firstInputFrame = true;
            _currentRow = FirstNavigableRowIndex;
            _currentCol = 0;
            UpdateSelection();
        }

        public void DismissRestoringPreviousFocus()
        {
            HideKeyboard();
        }

        public void HandleInput(Gamepad gamepad)
        {
            bool up = (gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0;
            bool down = (gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0;
            bool left = (gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0;
            bool right = (gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0;
            bool select = (gamepad.Buttons & GamepadButtonFlags.A) != 0;
            bool cancel = (gamepad.Buttons & GamepadButtonFlags.B) != 0;
            bool backspace = (gamepad.Buttons & GamepadButtonFlags.X) != 0;
            bool lb = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            bool rb = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            bool lbRbCombo = lb && rb;

            if (_firstInputFrame)
            {
                _dpadUpPressed = up;
                _dpadDownPressed = down;
                _dpadLeftPressed = left;
                _dpadRightPressed = right;
                _selectPressed = select;
                _cancelPressed = cancel;
                _backspacePressed = backspace;
                _lbPressed = lb;
                _rbPressed = rb;
                _lbRbComboPressed = lbRbCombo;
                _firstInputFrame = false;
                return;
            }

            if (lbRbCombo && !_lbRbComboPressed)
            {
                ToggleNavClusterZone();
            }
            else if (rb && !lb)
            {
                if (up && !_dpadUpPressed)
                    SendArrowKey(0x26);
                if (down && !_dpadDownPressed)
                    SendArrowKey(0x28);
                if (left && !_dpadLeftPressed)
                    SendArrowKey(0x25);
                if (right && !_dpadRightPressed)
                    SendArrowKey(0x27);
            }
            else if (_zone == KeyboardNavigationZone.NavCluster)
            {
                if (up && !_dpadUpPressed)
                    MoveNavClusterUp();
                if (down && !_dpadDownPressed)
                    MoveNavClusterDown();
                if (left && !_dpadLeftPressed)
                    MoveNavClusterLeft();
                if (right && !_dpadRightPressed)
                    MoveNavClusterRight();
            }
            else
            {
                if (lb && !_lbPressed && !rb)
                {
                    _symbolLayer = !_symbolLayer;
                    UpdateKeyLabels();
                }

                if (up && !_dpadUpPressed)
                    MoveUp();
                if (down && !_dpadDownPressed)
                    MoveDown();
                if (left && !_dpadLeftPressed)
                    MoveLeft();
                if (right && !_dpadRightPressed)
                    MoveRight();
            }

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
            _lbPressed = lb;
            _rbPressed = rb;
            _lbRbComboPressed = lbRbCombo;
        }

        private void ResetNavigationZone()
        {
            _arrowPulseTimer?.Stop();
            _zone = KeyboardNavigationZone.Main;
            _navRow = 0;
            _navCol = 0;
            _rbPressed = false;
            _lbRbComboPressed = false;
            UpdateNavClusterHighlight();
        }

        private void ToggleNavClusterZone()
        {
            if (_zone == KeyboardNavigationZone.Main)
            {
                _savedMainRow = _currentRow;
                _savedMainCol = _currentCol;
                _zone = KeyboardNavigationZone.NavCluster;
                _navRow = 0;
                _navCol = 0;
            }
            else
            {
                _zone = KeyboardNavigationZone.Main;
                _currentRow = _savedMainRow;
                _currentCol = _savedMainCol;
            }

            UpdateNavClusterHighlight();
            UpdateSelection();
        }

        private void UpdateNavClusterHighlight()
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (row.HasRightCluster)
                    row.HighlightNavCluster = IsNavClusterActive && i < NavClusterRowCount;
            }
        }

        // Nav cluster keys live on keyboard rows 0–2 (F-row, number row, Q-row).
        private KeyViewModel GetNavClusterKey(int row, int col) =>
            Rows[row].RightCluster!.Keys[col];

        private bool TryGetArrowKey(byte virtualKey, out KeyViewModel? key)
        {
            key = null;
            switch (virtualKey)
            {
                case 0x26:
                    key = Rows[ArrowUpRowIndex].RightCluster!.Keys[1];
                    return !key.IsGap;
                case 0x28:
                    key = Rows[ArrowKeysRowIndex].RightCluster!.Keys[1];
                    return !key.IsGap;
                case 0x25:
                    key = Rows[ArrowKeysRowIndex].RightCluster!.Keys[0];
                    return !key.IsGap;
                case 0x27:
                    key = Rows[ArrowKeysRowIndex].RightCluster!.Keys[2];
                    return !key.IsGap;
                default:
                    return false;
            }
        }

        private void SendArrowKey(byte virtualKey)
        {
            SendVirtualKey(virtualKey);
            if (TryGetArrowKey(virtualKey, out var key) && key != null)
                PulseArrowKey(key);
        }

        private void PulseArrowKey(KeyViewModel key)
        {
            _arrowPulseTimer?.Stop();

            foreach (var k in EnumerateAllKeys())
                k.IsSelected = false;

            key.IsSelected = true;

            _arrowPulseTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(175) };
            _arrowPulseTimer.Tick -= OnArrowPulseTimerTick;
            _arrowPulseTimer.Tick += OnArrowPulseTimerTick;
            _arrowPulseTimer.Start();
        }

        private void OnArrowPulseTimerTick(object? sender, EventArgs e)
        {
            _arrowPulseTimer?.Stop();
            UpdateSelection();
        }

        private void MoveNavClusterUp()
        {
            _navRow = (_navRow + NavClusterRowCount - 1) % NavClusterRowCount;
            UpdateSelection();
        }

        private void MoveNavClusterDown()
        {
            _navRow = (_navRow + 1) % NavClusterRowCount;
            UpdateSelection();
        }

        private void MoveNavClusterLeft()
        {
            _navCol = (_navCol + NavClusterRowCount - 1) % NavClusterRowCount;
            UpdateSelection();
        }

        private void MoveNavClusterRight()
        {
            _navCol = (_navCol + 1) % NavClusterRowCount;
            UpdateSelection();
        }

        private int LastNavigableRowIndex => Rows.Count - 1;

        private bool IsRowInWrapRange(int row) =>
            row >= FirstNavigableRowIndex && row <= LastNavigableRowIndex;

        private bool IsValidNavigationRow(int row) =>
            row == TopRowIndex || IsRowInWrapRange(row);

        private static bool IsExcludedFromNavigation(KeyViewModel key)
        {
            if (key.IsGap)
                return true;

            var label = key.Label;
            if (string.IsNullOrEmpty(label))
                return false;

            return ExcludedNavLabels.Contains(label);
        }

        private IReadOnlyList<KeyViewModel> GetNavigableKeys(int row)
        {
            if (row == TopRowIndex)
                return Rows[TopRowIndex].Keys.Where(k => !k.IsGap).ToList();

            if (!IsRowInWrapRange(row))
                return Array.Empty<KeyViewModel>();

            return Rows[row].Keys.Where(k => !IsExcludedFromNavigation(k)).ToList();
        }

        private void EnsureValidNavigationRow()
        {
            if (!IsValidNavigationRow(_currentRow))
                _currentRow = FirstNavigableRowIndex;
        }

        private void MoveUp()
        {
            if (_currentRow == TopRowIndex)
            {
                _currentRow = LastNavigableRowIndex;
                ClampColumnForCurrentFocus();
                UpdateSelection();
                return;
            }

            EnsureValidNavigationRow();

            var keys = GetNavigableKeys(_currentRow);
            if (keys.Count == 0)
                return;

            if (_currentRow == FirstNavigableRowIndex)
                _currentRow = TopRowIndex;
            else
                _currentRow--;

            ClampColumnForCurrentFocus();
            UpdateSelection();
        }

        private void MoveDown()
        {
            if (_currentRow == TopRowIndex)
            {
                _currentRow = FirstNavigableRowIndex;
                ClampColumnForCurrentFocus();
                UpdateSelection();
                return;
            }

            EnsureValidNavigationRow();

            var keys = GetNavigableKeys(_currentRow);
            if (keys.Count == 0)
                return;

            if (_currentRow < LastNavigableRowIndex)
                _currentRow++;
            else
                _currentRow = FirstNavigableRowIndex;

            ClampColumnForCurrentFocus();
            UpdateSelection();
        }

        private void MoveLeft()
        {
            EnsureValidNavigationRow();

            var keys = GetNavigableKeys(_currentRow);
            if (keys.Count == 0)
                return;

            if (_currentCol > 0)
                _currentCol--;
            else
                _currentCol = keys.Count - 1;

            UpdateSelection();
        }

        private void MoveRight()
        {
            EnsureValidNavigationRow();

            var keys = GetNavigableKeys(_currentRow);
            if (keys.Count == 0)
                return;

            if (_currentCol < keys.Count - 1)
                _currentCol++;
            else
                _currentCol = 0;

            UpdateSelection();
        }

        private void ClampColumnForCurrentFocus()
        {
            var keys = GetNavigableKeys(_currentRow);
            if (keys.Count == 0)
                _currentCol = 0;
            else
                _currentCol = Math.Clamp(_currentCol, 0, keys.Count - 1);
        }

        private void BuildKeyboardRows()
        {
            Rows.Clear();

            // F-row: ESC + F1-F12
            var fRow = AddMainRow(
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
            AttachRightCluster(fRow,
                CreateFunctionKey("PRTSC", 0x2C, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("SCRLK", 0x91, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("PAUSE", 0x13, ClusterKeyWidth, ClusterKeyHeight));

            // Number row
            var numRow = AddMainRow(
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
                CreateFunctionKey("BACK", 0x08, 90)
            );
            AttachRightCluster(numRow,
                CreateFunctionKey("INS", 0x2D, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("HOME", 0x24, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("PGUP", 0x21, ClusterKeyWidth, ClusterKeyHeight));

            // Q row
            var qRow = AddMainRow(
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
                CreateCharKey('\\', '|', 80)
            );
            AttachRightCluster(qRow,
                CreateFunctionKey("DEL", 0x2E, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("END", 0x23, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("PGDN", 0x22, ClusterKeyWidth, ClusterKeyHeight));

            // A row + ENTER
            var aRow = AddMainRow(
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
            AttachRightCluster(aRow,
                CreateGap(ClusterGapWidth, ClusterKeyHeight),
                CreateFunctionKey("UP", 0x26, ClusterKeyWidth, ClusterKeyHeight),
                CreateGap(ClusterGapWidth, ClusterKeyHeight));

            // Z row + shifts
            var zRow = AddMainRow(
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
            AttachRightCluster(zRow,
                CreateFunctionKey("LEFT", 0x25, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("DOWN", 0x28, ClusterKeyWidth, ClusterKeyHeight),
                CreateFunctionKey("RIGHT", 0x27, ClusterKeyWidth, ClusterKeyHeight));

            // Bottom row
            AddMainRow(
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

        private static void AttachRightCluster(KeyboardRow row, params KeyViewModel[] keys)
        {
            row.RightCluster = new KeyboardClusterRow();
            foreach (var key in keys)
                row.RightCluster.Keys.Add(key);
        }

        private KeyboardRow AddMainRow(params KeyViewModel[] keys)
        {
            var row = new KeyboardRow();
            foreach (var key in keys)
                row.Keys.Add(key);
            Rows.Add(row);
            return row;
        }

        private static KeyViewModel CreateGap(double width, double height)
        {
            return KeyViewModel.CreateSpacer(width, height);
        }

        private KeyViewModel CreateCharKey(char primary, char? alternate, double width = 70, double height = 48)
        {
            return new KeyViewModel(primary, alternate, width, height, KeyType.Character);
        }

        private KeyViewModel CreateFunctionKey(string label, byte virtualKey, double width = 70, double height = 48)
        {
            return new KeyViewModel(label, null, width, height, KeyType.Function, virtualKey);
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
            foreach (var key in EnumerateAllKeys())
            {
                if (key.KeyType == KeyType.Character)
                    key.UpdateLabel(_symbolLayer, _shiftActive, _capsLockActive);
                else if (key.KeyType == KeyType.LayerToggle)
                    key.Label = _symbolLayer ? "ABC" : "SYM";
            }
        }

        private void ApplyModifierStates()
        {
            foreach (var key in EnumerateAllKeys())
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

        private IEnumerable<KeyViewModel> EnumerateAllKeys()
        {
            foreach (var row in Rows)
            {
                foreach (var key in row.Keys.Where(k => !k.IsGap))
                    yield return key;
            }

            foreach (var row in Rows)
            {
                if (row.RightCluster == null)
                    continue;
                foreach (var key in row.RightCluster.Keys.Where(k => !k.IsGap))
                    yield return key;
            }
        }

        private void UpdateSelection()
        {
            foreach (var key in EnumerateAllKeys())
                key.IsSelected = false;

            if (_zone == KeyboardNavigationZone.NavCluster)
            {
                _navRow = Math.Clamp(_navRow, 0, NavClusterRowCount - 1);
                _navCol = Math.Clamp(_navCol, 0, NavClusterRowCount - 1);
                GetNavClusterKey(_navRow, _navCol).IsSelected = true;
                return;
            }

            EnsureValidNavigationRow();
            var keys = GetNavigableKeys(_currentRow);

            if (keys.Count == 0)
                return;

            _currentCol = Math.Clamp(_currentCol, 0, keys.Count - 1);
            keys[_currentCol].IsSelected = true;
        }

        private void SelectCurrentKey()
        {
            KeyViewModel key;

            if (_zone == KeyboardNavigationZone.NavCluster)
            {
                _navRow = Math.Clamp(_navRow, 0, NavClusterRowCount - 1);
                _navCol = Math.Clamp(_navCol, 0, NavClusterRowCount - 1);
                key = GetNavClusterKey(_navRow, _navCol);
            }
            else
            {
                EnsureValidNavigationRow();
                var keys = GetNavigableKeys(_currentRow);

                if (keys.Count == 0)
                    return;

                key = keys[Math.Clamp(_currentCol, 0, keys.Count - 1)];
            }

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

        public void HideKeyboard()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(HideKeyboard);
                return;
            }

            if (!IsKeyboardOpen)
                return;

            ResetNavigationZone();
            var previous = _previousWindow;
            IsKeyboardOpen = false;
            IsVisible = false;
            KeyboardClosed?.Invoke(this, EventArgs.Empty);
            RestorePreviousFocus(previous);
        }

        private void RestorePreviousFocus(IntPtr previous)
        {
            if (previous == IntPtr.Zero)
                return;

            try
            {
                if (TryGetPlatformHandle() is { } handle && handle.Handle != previous)
                    SetForegroundWindow(previous);
            }
            catch
            {
                // Best-effort focus restore
            }
        }

        private void Backspace()
        {
            SendVirtualKey(0x08);
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    public class KeyboardRow : INotifyPropertyChanged
    {
        private bool _highlightNavCluster;

        public ObservableCollection<KeyViewModel> Keys { get; } = new();
        public KeyboardClusterRow? RightCluster { get; set; }
        public bool HasRightCluster => RightCluster != null;

        public bool HighlightNavCluster
        {
            get => _highlightNavCluster;
            set
            {
                if (_highlightNavCluster == value)
                    return;

                _highlightNavCluster = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighlightNavCluster)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RightClusterBorderBrush)));
            }
        }

        public IBrush RightClusterBorderBrush => HighlightNavCluster
            ? VirtualKeyboardWindowDefaultBrushes.NavClusterActiveBorder
            : VirtualKeyboardWindowDefaultBrushes.ClusterBorder;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class KeyboardClusterRow
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
        private IBrush _borderBrush;
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
        public bool IsGap { get; }
        public bool IsKeyVisible => !IsGap;

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

        public IBrush BorderBrush
        {
            get => _borderBrush;
            private set
            {
                if (_borderBrush != value)
                {
                    _borderBrush = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BorderBrush)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static KeyViewModel CreateSpacer(double width, double height)
        {
            return new KeyViewModel(string.Empty, null, width, height, KeyType.Function, isGap: true);
        }

        public KeyViewModel(char primaryCharacter, char? alternateCharacter, double width, double height, KeyType keyType, byte? virtualKey = null, bool isGap = false)
        {
            PrimaryCharacter = primaryCharacter;
            AlternateCharacter = alternateCharacter;
            _label = primaryCharacter.ToString();
            Width = width;
            Height = height;
            KeyType = keyType;
            VirtualKey = virtualKey;
            IsGap = isGap;
            _background = VirtualKeyboardWindowDefaultBrushes.KeyBackground;
            _borderBrush = VirtualKeyboardWindowDefaultBrushes.KeyBorder;
        }

        public KeyViewModel(string label, char? alternateCharacter, double width, double height, KeyType keyType, byte? virtualKey = null, bool isGap = false)
        {
            _label = label;
            PrimaryCharacter = null;
            AlternateCharacter = alternateCharacter;
            Width = width;
            Height = height;
            KeyType = keyType;
            VirtualKey = virtualKey;
            IsGap = isGap;
            _background = VirtualKeyboardWindowDefaultBrushes.KeyBackground;
            _borderBrush = VirtualKeyboardWindowDefaultBrushes.KeyBorder;
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
                Background = VirtualKeyboardWindowDefaultBrushes.SelectedKeyBackground;
                BorderBrush = VirtualKeyboardWindowDefaultBrushes.SelectedKeyBorder;
            }
            else if (IsActive)
            {
                Background = VirtualKeyboardWindowDefaultBrushes.ActiveKeyBackground;
                BorderBrush = VirtualKeyboardWindowDefaultBrushes.KeyBorder;
            }
            else
            {
                Background = VirtualKeyboardWindowDefaultBrushes.KeyBackground;
                BorderBrush = VirtualKeyboardWindowDefaultBrushes.KeyBorder;
            }
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
    }

    internal static class VirtualKeyboardWindowDefaultBrushes
    {
        public static readonly IBrush KeyBackground = new SolidColorBrush(Color.Parse("#FF5C6370"));
        public static readonly IBrush KeyBorder = new SolidColorBrush(Color.Parse("#FF6B7280"));
        public static readonly IBrush ClusterBorder = new SolidColorBrush(Color.Parse("#FF4B5563"));
        public static readonly IBrush NavClusterActiveBorder = new SolidColorBrush(Color.Parse("#FF3B82F6"));
        public static readonly IBrush ActiveKeyBackground = new SolidColorBrush(Color.Parse("#FF4B5563"));
        public static readonly IBrush SelectedKeyBackground = new SolidColorBrush(Color.Parse("#FF2563EB"));
        public static readonly IBrush SelectedKeyBorder = new SolidColorBrush(Color.Parse("#FF3B82F6"));
    }
}
