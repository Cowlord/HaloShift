using Xunit;

namespace HaloShift.Tests
{
    public class VirtualKeyTests
    {
        [Fact]
        public void Escape_Is0x1B()
        {
            Assert.Equal(0x1B, VirtualKey.Escape);
        }

        [Fact]
        public void F5_Is0x74()
        {
            Assert.Equal(0x74, VirtualKey.F5);
        }

        [Fact]
        public void F11_Is0x7A()
        {
            Assert.Equal(0x7A, VirtualKey.F11);
        }

        [Fact]
        public void LWin_Is0x5B()
        {
            Assert.Equal(0x5B, VirtualKey.LWin);
        }

        [Fact]
        public void BrowserBack_Is0xA6()
        {
            Assert.Equal(0xA6, VirtualKey.BrowserBack);
        }

        [Fact]
        public void Alt_Is0x12()
        {
            Assert.Equal(0x12, VirtualKey.Alt);
        }

        [Fact]
        public void F4_Is0x73()
        {
            Assert.Equal(0x73, VirtualKey.F4);
        }

        [Fact]
        public void Ctrl_Is0x11()
        {
            Assert.Equal(0x11, VirtualKey.Ctrl);
        }

        [Fact]
        public void W_Is0x57()
        {
            Assert.Equal(0x57, VirtualKey.W);
        }
    }
}
