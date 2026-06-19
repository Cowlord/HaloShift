using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
