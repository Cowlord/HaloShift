using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace HaloShift
{
    internal static class Win32WindowHelper
    {
        private const int GwlExstyle = -20;
        private const int WsExToolwindow = 0x00000080;
        private const int WsExAppwindow = 0x00040000;

        private const uint SwpNomove = 0x0002;
        private const uint SwpNosize = 0x0001;
        private const uint SwpNozorder = 0x0004;
        private const uint SwpNoactivate = 0x0010;
        private const uint SwpFramechanged = 0x0020;

        public static void ExcludeFromTaskSwitcher(this Window window)
        {
            void Apply(object? sender, EventArgs e)
            {
                window.Opened -= Apply;
                ApplyExcludeFromTaskSwitcher(window);
            }

            window.Opened += Apply;
        }

        private static void ApplyExcludeFromTaskSwitcher(Window window)
        {
            if (window.TryGetPlatformHandle() is not { } platformHandle)
                return;

            var hwnd = platformHandle.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var exStyle = (int)GetWindowLong(hwnd, GwlExstyle);
            exStyle &= ~WsExAppwindow;
            exStyle |= WsExToolwindow;
            SetWindowLong(hwnd, GwlExstyle, (IntPtr)exStyle);

            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SwpNomove | SwpNosize | SwpNozorder | SwpNoactivate | SwpFramechanged);
        }

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }
}
