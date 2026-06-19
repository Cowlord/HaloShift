using Xunit;

namespace HaloShift.Tests
{
    public class KeyViewModelTests
    {
        // --- Character key construction ---

        [Fact]
        public void CharacterKey_Label_IsPrimaryChar()
        {
            var key = new KeyViewModel('A', '!', 70, 48, KeyType.Character);
            Assert.Equal("A", key.Label);
        }

        [Fact]
        public void CharacterKey_HasCorrectProperties()
        {
            var key = new KeyViewModel('Q', '@', 70, 48, KeyType.Character);
            Assert.Equal('Q', key.PrimaryCharacter);
            Assert.Equal('@', key.AlternateCharacter);
            Assert.Equal(70, key.Width);
            Assert.Equal(48, key.Height);
            Assert.Equal(KeyType.Character, key.KeyType);
            Assert.False(key.IsGap);
            Assert.True(key.IsKeyVisible);
        }

        // --- Function key construction ---

        [Fact]
        public void FunctionKey_Label_IsProvidedString()
        {
            var key = new KeyViewModel("ESC", null, 50, 36, KeyType.Function, 0x1B);
            Assert.Equal("ESC", key.Label);
            Assert.Null(key.PrimaryCharacter);
            Assert.Equal((byte)0x1B, key.VirtualKey);
        }

        // --- Modifier key ---

        [Fact]
        public void ModifierKey_HasCorrectType()
        {
            var key = new KeyViewModel("CTRL", null, 90, 48, KeyType.Modifier, 0x11);
            Assert.Equal(KeyType.Modifier, key.KeyType);
            Assert.Equal("CTRL", key.Label);
        }

        // --- LayerToggle key ---

        [Fact]
        public void LayerToggleKey_HasCorrectType()
        {
            var key = new KeyViewModel("SYM", null, 90, 48, KeyType.LayerToggle);
            Assert.Equal(KeyType.LayerToggle, key.KeyType);
            Assert.Equal("SYM", key.Label);
        }

        // --- Gap / Spacer ---

        [Fact]
        public void CreateSpacer_IsGap()
        {
            var spacer = KeyViewModel.CreateSpacer(30, 36);
            Assert.True(spacer.IsGap);
            Assert.False(spacer.IsKeyVisible);
            Assert.Equal(30, spacer.Width);
            Assert.Equal(36, spacer.Height);
        }

        // --- Display property ---

        [Fact]
        public void Display_MatchesLabel()
        {
            var key = new KeyViewModel('X', null, 70, 48, KeyType.Character);
            Assert.Equal(key.Label, key.Display);
        }

        // --- UpdateLabel ---

        [Fact]
        public void UpdateLabel_NoSymbolLayer_ShowsPrimary()
        {
            var key = new KeyViewModel('a', '!', 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: false, shiftActive: false, capsLockActive: false);
            Assert.Equal("a", key.Label);
        }

        [Fact]
        public void UpdateLabel_SymbolLayer_ShowsAlternate()
        {
            var key = new KeyViewModel('1', '!', 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: true, shiftActive: false, capsLockActive: false);
            Assert.Equal("!", key.Label);
        }

        [Fact]
        public void UpdateLabel_ShiftActive_UppercaseLetter()
        {
            var key = new KeyViewModel('a', null, 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: false, shiftActive: true, capsLockActive: false);
            Assert.Equal("A", key.Label);
        }

        [Fact]
        public void UpdateLabel_CapsLockActive_UppercaseLetter()
        {
            var key = new KeyViewModel('a', null, 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: false, shiftActive: false, capsLockActive: true);
            Assert.Equal("A", key.Label);
        }

        [Fact]
        public void UpdateLabel_NoShiftNoCaps_LowercaseLetter()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: false, shiftActive: false, capsLockActive: false);
            Assert.Equal("a", key.Label);
        }

        [Fact]
        public void UpdateLabel_SymbolLayerWithNoAlternate_ShowsPrimary()
        {
            var key = new KeyViewModel('Q', null, 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: true, shiftActive: false, capsLockActive: false);
            Assert.Equal("q", key.Label);
        }

        [Fact]
        public void UpdateLabel_NonCharacterKeyType_NoChange()
        {
            var key = new KeyViewModel("ENTER", null, 110, 48, KeyType.Function, 0x0D);
            key.UpdateLabel(symbolLayer: true, shiftActive: true, capsLockActive: true);
            Assert.Equal("ENTER", key.Label);
        }

        [Fact]
        public void UpdateLabel_NonLetterCharacter_ShowsAsIs()
        {
            var key = new KeyViewModel(';', ':', 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: false, shiftActive: false, capsLockActive: false);
            Assert.Equal(";", key.Label);
        }

        [Fact]
        public void UpdateLabel_SymbolLayerNonLetter_ShowsAlternate()
        {
            var key = new KeyViewModel(';', ':', 70, 48, KeyType.Character);
            key.UpdateLabel(symbolLayer: true, shiftActive: false, capsLockActive: false);
            Assert.Equal(":", key.Label);
        }

        // --- GetActiveCharacter ---

        [Fact]
        public void GetActiveCharacter_NormalLayer_ReturnsPrimary()
        {
            var key = new KeyViewModel('A', '!', 70, 48, KeyType.Character);
            Assert.Equal('A', key.GetActiveCharacter(symbolLayer: false));
        }

        [Fact]
        public void GetActiveCharacter_SymbolLayer_ReturnsAlternate()
        {
            var key = new KeyViewModel('1', '!', 70, 48, KeyType.Character);
            Assert.Equal('!', key.GetActiveCharacter(symbolLayer: true));
        }

        [Fact]
        public void GetActiveCharacter_SymbolLayerNoAlternate_ReturnsPrimary()
        {
            var key = new KeyViewModel('Q', null, 70, 48, KeyType.Character);
            Assert.Equal('Q', key.GetActiveCharacter(symbolLayer: true));
        }

        [Fact]
        public void GetActiveCharacter_StringLabel_ReturnsNullChar()
        {
            var key = new KeyViewModel("ESC", null, 50, 36, KeyType.Function, 0x1B);
            Assert.Equal('\0', key.GetActiveCharacter(symbolLayer: false));
        }

        // --- RequiresShift (letter characters only - safe on Linux) ---

        [Fact]
        public void RequiresShift_Letter_ShiftActive_ReturnsTrue()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            Assert.True(key.RequiresShift(symbolLayer: false, shiftActive: true, capsLockActive: false));
        }

        [Fact]
        public void RequiresShift_Letter_CapsLockActive_ReturnsTrue()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            Assert.True(key.RequiresShift(symbolLayer: false, shiftActive: false, capsLockActive: true));
        }

        [Fact]
        public void RequiresShift_Letter_NoModifiers_ReturnsFalse()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            Assert.False(key.RequiresShift(symbolLayer: false, shiftActive: false, capsLockActive: false));
        }

        // --- IsSelected / IsActive / Background ---

        [Fact]
        public void IsSelected_DefaultIsFalse()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            Assert.False(key.IsSelected);
        }

        [Fact]
        public void IsSelected_WhenSet_ChangesBackground()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            var initialBg = key.Background;

            key.IsSelected = true;

            Assert.NotEqual(initialBg, key.Background);
            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.SelectedKeyBackground, key.Background);
            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.SelectedKeyBorder, key.BorderBrush);
        }

        [Fact]
        public void IsSelected_WhenUnset_RestoresBackground()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            key.IsSelected = true;
            key.IsSelected = false;

            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.KeyBackground, key.Background);
            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.KeyBorder, key.BorderBrush);
        }

        [Fact]
        public void IsActive_WhenSet_ChangesBackground()
        {
            var key = new KeyViewModel("CTRL", null, 90, 48, KeyType.Modifier, 0x11);
            key.IsActive = true;

            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.ActiveKeyBackground, key.Background);
        }

        [Fact]
        public void IsSelected_TakesPrecedence_OverIsActive()
        {
            var key = new KeyViewModel("CTRL", null, 90, 48, KeyType.Modifier, 0x11);
            key.IsActive = true;
            key.IsSelected = true;

            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.SelectedKeyBackground, key.Background);
        }

        // --- PropertyChanged notifications ---

        [Fact]
        public void Label_Change_FiresPropertyChanged()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            var changedProperties = new System.Collections.Generic.List<string>();
            key.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

            key.Label = "B";

            Assert.Contains("Label", changedProperties);
            Assert.Contains("Display", changedProperties);
        }

        [Fact]
        public void IsSelected_Change_FiresPropertyChanged()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            bool fired = false;
            key.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "IsSelected") fired = true;
            };

            key.IsSelected = true;
            Assert.True(fired);
        }

        [Fact]
        public void IsActive_Change_FiresPropertyChanged()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            bool fired = false;
            key.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "IsActive") fired = true;
            };

            key.IsActive = true;
            Assert.True(fired);
        }

        [Fact]
        public void Label_NoChange_DoesNotFirePropertyChanged()
        {
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            bool fired = false;
            key.PropertyChanged += (_, __) => fired = true;

            key.Label = "A"; // same value
            Assert.False(fired);
        }
    }
}
