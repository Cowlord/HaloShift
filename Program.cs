using Avalonia;
using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HaloShift
{
    internal static class Program
    {
        private const string MutexName = @"Global\HaloShift_SingleInstance";

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    CrashHandler.LogUnhandledException("AppDomain", ex);
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                CrashHandler.LogUnhandledException("UnobservedTask", e.Exception);
                e.SetObserved();
            };

            // Check if we're running in watcher mode
            if (args.Length > 0 && args[0] == "--watcher")
            {
                RestartManager.RunSelfWatcher();
                return;
            }

            // Check if we're being started by a watcher with parent process ID
            if (args.Length > 1 && int.TryParse(args[0], out int parentProcessId))
            {
                RestartManager.RunWatcher(parentProcessId);
                return;
            }

            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
                return;

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
            }
            catch (Exception ex)
            {
                CrashHandler.LogUnhandledException("Main", ex);
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
