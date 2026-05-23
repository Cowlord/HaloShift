using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HaloShift
{
    internal static class Win32Sound
    {
        private const uint SND_ASYNC = 0x0001;
        private const uint SND_NODEFAULT = 0x0002;
        private const uint SND_FILENAME = 0x00020000;
        private const uint SND_SYSTEM = 0x00200000;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        public static void PlayWavFile(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
                return;

            PlaySound(path, IntPtr.Zero, SND_ASYNC | SND_NODEFAULT | SND_FILENAME | SND_SYSTEM);
        }
    }
}
