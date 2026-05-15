using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Windows.Input;

namespace HaloShift
{
    public class TrayIconManager : IDisposable
    {
        private readonly TrayIcon _trayIcon;

        public event EventHandler? ToggleModeRequested;
        public event EventHandler? ShowKeyboardRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager()
        {
            _trayIcon = new TrayIcon
            {
                ToolTipText = "HaloShift - Controller Mode",
                Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://HaloShift/AppIcon.ico"))))
            };

            var menu = new NativeMenu();

            var toggleItem = new NativeMenuItem("Toggle Mode");
            toggleItem.Click += (_, __) => ToggleModeRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(toggleItem);

            var keyboardItem = new NativeMenuItem("Show Keyboard");
            keyboardItem.Click += (_, __) => ShowKeyboardRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(keyboardItem);

            var exitCommand = new AnonymousCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add(new NativeMenuItem("Exit")
            {
                Command = exitCommand
            });
            exitCommand.RaiseCanExecuteChanged();

            _trayIcon.Menu = menu;
        }

        public void AttachToWindow(Window window)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var icons = TrayIcon.GetIcons(app);
            if (icons == null)
            {
                icons = new TrayIcons();
                TrayIcon.SetIcons(app, icons);
            }

            if (!icons.Contains(_trayIcon))
            {
                icons.Add(_trayIcon);
            }

            _trayIcon.IsVisible = true;
        }

        public void UpdateTooltip(string tooltip)
        {
            _trayIcon.ToolTipText = tooltip;
        }

        public void ShowBalloonTip(int durationMilliseconds, string title, string text)
        {
            // Avalonia currently does not support native balloon tips on all platforms,
            // so this is a no-op placeholder for later platform-specific extension.
        }

        public void Dispose()
        {
            _trayIcon.IsVisible = false;
            _trayIcon.Menu = null;
        }
    }

    internal class AnonymousCommand : ICommand
    {
        private readonly Action _action;

        public AnonymousCommand(Action action)
        {
            _action = action;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _action();

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
