using System.Collections.Generic;
using Xunit;

namespace HaloShift.Tests
{
    public class KeyboardRowTests
    {
        [Fact]
        public void Keys_DefaultEmpty()
        {
            var row = new KeyboardRow();
            Assert.Empty(row.Keys);
        }

        [Fact]
        public void RightCluster_DefaultNull()
        {
            var row = new KeyboardRow();
            Assert.Null(row.RightCluster);
            Assert.False(row.HasRightCluster);
        }

        [Fact]
        public void HasRightCluster_TrueWhenSet()
        {
            var row = new KeyboardRow { RightCluster = new KeyboardClusterRow() };
            Assert.True(row.HasRightCluster);
        }

        [Fact]
        public void HighlightNavCluster_DefaultFalse()
        {
            var row = new KeyboardRow();
            Assert.False(row.HighlightNavCluster);
        }

        [Fact]
        public void HighlightNavCluster_FiresPropertyChanged()
        {
            var row = new KeyboardRow();
            var changedProps = new List<string>();
            row.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

            row.HighlightNavCluster = true;

            Assert.Contains("HighlightNavCluster", changedProps);
            Assert.Contains("RightClusterBorderBrush", changedProps);
        }

        [Fact]
        public void HighlightNavCluster_SameValue_DoesNotFire()
        {
            var row = new KeyboardRow();
            bool fired = false;
            row.PropertyChanged += (_, __) => fired = true;

            row.HighlightNavCluster = false; // same as default
            Assert.False(fired);
        }

        [Fact]
        public void RightClusterBorderBrush_ChangesWithHighlight()
        {
            var row = new KeyboardRow();
            var normalBrush = row.RightClusterBorderBrush;

            row.HighlightNavCluster = true;
            var highlightBrush = row.RightClusterBorderBrush;

            Assert.NotEqual(normalBrush, highlightBrush);
            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.NavClusterActiveBorder, highlightBrush);
        }

        [Fact]
        public void RightClusterBorderBrush_Normal_IsClusterBorder()
        {
            var row = new KeyboardRow();
            Assert.Equal(VirtualKeyboardWindowDefaultBrushes.ClusterBorder, row.RightClusterBorderBrush);
        }

        [Fact]
        public void Keys_CanAddKeys()
        {
            var row = new KeyboardRow();
            var key = new KeyViewModel('A', null, 70, 48, KeyType.Character);
            row.Keys.Add(key);

            Assert.Single(row.Keys);
            Assert.Equal(key, row.Keys[0]);
        }
    }

    public class KeyboardClusterRowTests
    {
        [Fact]
        public void Keys_DefaultEmpty()
        {
            var cluster = new KeyboardClusterRow();
            Assert.Empty(cluster.Keys);
        }

        [Fact]
        public void Keys_CanAddKeys()
        {
            var cluster = new KeyboardClusterRow();
            var key = new KeyViewModel("INS", null, 70, 48, KeyType.Function, 0x2D);
            cluster.Keys.Add(key);

            Assert.Single(cluster.Keys);
        }
    }

    public class ModifierStateTests
    {
        [Fact]
        public void Default_AllFalse()
        {
            var state = new ModifierState();
            Assert.False(state.Ctrl);
            Assert.False(state.Alt);
            Assert.False(state.Win);
            Assert.False(state.Shift);
        }

        [Fact]
        public void CanSetAllModifiers()
        {
            var state = new ModifierState
            {
                Ctrl = true,
                Alt = true,
                Win = true,
                Shift = true
            };

            Assert.True(state.Ctrl);
            Assert.True(state.Alt);
            Assert.True(state.Win);
            Assert.True(state.Shift);
        }

        [Fact]
        public void CanSetIndividualModifiers()
        {
            var state = new ModifierState { Ctrl = true };
            Assert.True(state.Ctrl);
            Assert.False(state.Alt);
            Assert.False(state.Win);
            Assert.False(state.Shift);
        }
    }

    public class KeyTypeEnumTests
    {
        [Fact]
        public void HasExpectedValues()
        {
            Assert.Equal(0, (int)KeyType.Character);
            Assert.Equal(1, (int)KeyType.Function);
            Assert.Equal(2, (int)KeyType.Modifier);
            Assert.Equal(3, (int)KeyType.LayerToggle);
        }
    }

    public class KeyboardNavigationZoneEnumTests
    {
        [Fact]
        public void HasExpectedValues()
        {
            Assert.Equal(0, (int)KeyboardNavigationZone.Main);
            Assert.Equal(1, (int)KeyboardNavigationZone.NavCluster);
        }
    }
}
