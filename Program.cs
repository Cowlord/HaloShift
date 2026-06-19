using Avalonia;
using Avalonia.Controls;
using System.Threading;

namespace HaloShift
{
    internal static class Program
    {
        private const string MutexName = @"Global\HaloShift_SingleInstance";

        [System.STAThread]
        public static void Main(string[] args)
        {
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
                return;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
