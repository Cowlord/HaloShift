using Avalonia;
using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace HaloShift
{
    internal static class Program
    {
        private const string MutexName = @"Global\HaloShift_SingleInstance";
        private const string BundleExtractEnvVar = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";
        private const string RelaunchedEnvVar = "HALOSHIFT_BUNDLE_RELAUNCHED";

        [System.STAThread]
        public static void Main(string[] args)
        {
            if (RelaunchWithStableBundleExtractDir(args))
                return;

            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
                return;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }

        /// <summary>
        /// The published app is a self-extracting single file (PublishSingleFile +
        /// IncludeAllContentForSelfExtract), so the .NET host extracts the runtime and
        /// bundled content to DOTNET_BUNDLE_EXTRACT_BASE_DIR (default: %TEMP%\.net\HaloShift)
        /// on every launch and keeps those files loaded for the process lifetime. Since that
        /// folder lives under the OS temp directory, temp-cleanup tools can delete it out from
        /// under the running app, crashing it and breaking bundled assets (e.g. sound files).
        /// This env var can only be honored if it's set before the host starts extracting, so
        /// relaunch once with it pointed at a stable, non-temp location.
        /// </summary>
        private static bool RelaunchWithStableBundleExtractDir(string[] args)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RelaunchedEnvVar)))
                return false;

            var desiredDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HaloShift", "bundle");

            if (string.Equals(Environment.GetEnvironmentVariable(BundleExtractEnvVar), desiredDir, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false
                };
                foreach (var arg in args)
                    psi.ArgumentList.Add(arg);

                psi.Environment[BundleExtractEnvVar] = desiredDir;
                psi.Environment[RelaunchedEnvVar] = "1";

                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
