using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace HaloShift
{
    public static class StartupManager
    {
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "HaloShift";

        private static string ExePath =>
            Process.GetCurrentProcess().MainModule?.FileName ??
            Path.Combine(AppContext.BaseDirectory, "HaloShift.exe");

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enabled)
        {
            if (enabled)
                AddEntry();
            else
                RemoveAllEntries();
        }

        private static void AddEntry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                key?.SetValue(AppName, $"\"{ExePath}\"");
            }
            catch
            {
            }
        }

        public static void RemoveAllEntries()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                key?.DeleteValue(AppName, throwOnMissingValue: false);
            }
            catch
            {
            }
        }
    }
}
