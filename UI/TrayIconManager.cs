using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace HaloShift
{
    public class TrayIconManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private readonly TrayIcon _trayIcon;
        private DateTime? _lastTrayClickUtc;

        public event EventHandler? ShowControlsRequested;
        public event EventHandler? ToggleModeRequested;
        public event EventHandler? ShowKeyboardRequested;
        public event EventHandler? OpenAudioLogRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager()
        {
            _trayIcon = new TrayIcon
            {
                ToolTipText = "HaloShift - Controller Mode",
                Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://HaloShift/AppIcon.ico"))))
            };

            _trayIcon.Clicked += OnTrayIconClicked;

            var menu = new NativeMenu();

            var controlsItem = new NativeMenuItem("Show Controls");
            controlsItem.Click += (_, __) => ShowControlsRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(controlsItem);

            var toggleItem = new NativeMenuItem("Toggle Mode");
            toggleItem.Click += (_, __) => ToggleModeRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(toggleItem);

            var keyboardItem = new NativeMenuItem("Show Keyboard");
            keyboardItem.Click += (_, __) => ShowKeyboardRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(keyboardItem);

            var openAudioLogItem = new NativeMenuItem("Open Audio Log");
            openAudioLogItem.Click += (_, __) => OpenAudioLogRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(openAudioLogItem);

            menu.Items.Add(new NativeMenuItemSeparator());

            var exitCommand = new AnonymousCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add(new NativeMenuItem("Exit")
            {
                Command = exitCommand
            });
            exitCommand.RaiseCanExecuteChanged();

            _trayIcon.Menu = menu;
        }

        public void AttachToApplication()
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

        private void OnTrayIconClicked(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            if (_lastTrayClickUtc.HasValue)
            {
                var elapsedMs = (now - _lastTrayClickUtc.Value).TotalMilliseconds;
                _lastTrayClickUtc = null;
                if (elapsedMs <= GetDoubleClickTime())
                {
                    ShowControlsRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            _lastTrayClickUtc = now;
        }

        public void Dispose()
        {
            _trayIcon.Clicked -= OnTrayIconClicked;
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
