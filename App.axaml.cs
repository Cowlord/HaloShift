using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace HaloShift
{
    public partial class App : Application
    {
        private DispatcherTimer? _updateTimer;
        private ControllerManager? _controllerManager;
        private ModeManager? _modeManager;
        private TrayIconManager? _trayIconManager;
        private SensitivityOverlayWindow? _sensitivityOverlay;
        private VirtualKeyboardWindow? _virtualKeyboard;
        private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;
        private bool _wasConnected;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var settings = AppSettings.Load();
            _controllerManager = new ControllerManager();
            _modeManager = new ModeManager();
            _modeManager.OnSensitivityPersist = newValue =>
            {
                settings.MouseSensitivity = newValue;
                settings.Save();
            };

            _trayIconManager = new TrayIconManager();
            _trayIconManager.ToggleModeRequested += (_, __) => _modeManager?.SwitchMode(ModeChangeInitiator.UserMenu);
            _trayIconManager.ShowKeyboardRequested += (_, __) => _virtualKeyboard?.ShowKeyboard();
            _trayIconManager.ExitRequested += (_, __) => _desktopLifetime?.Shutdown();

            _modeManager.ModeChanged += ModeManager_ModeChanged;
            _modeManager.SensitivityChanged += ModeManager_SensitivityChanged;
            _modeManager.ShowKeyboardRequested += ModeManager_ShowKeyboardRequested;

            _sensitivityOverlay = new SensitivityOverlayWindow();
            _virtualKeyboard = new VirtualKeyboardWindow();
            _virtualKeyboard.KeyboardClosed += (_, __) => { };

            var mainWindow = new MainWindow();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktopLifetime = desktop;
                desktop.MainWindow = mainWindow;
                desktop.Exit += OnDesktopExit;
            }

            mainWindow.Hide();
            _trayIconManager.AttachToWindow(mainWindow);

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            _wasConnected = _controllerManager.IsConnected;
            base.OnFrameworkInitializationCompleted();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_controllerManager == null || _modeManager == null)
                return;

            _controllerManager.Update();
            bool isConnected = _controllerManager.IsConnected;
            var currentState = _controllerManager.GetCurrentState();

            if (_wasConnected && !isConnected && _modeManager.CurrentMode == AppMode.Mouse)
            {
                _trayIconManager?.UpdateTooltip("HaloShift - Controller Mode");
                _modeManager.SwitchMode(ModeChangeInitiator.UserMenu);
            }
            _wasConnected = isConnected;

            _modeManager.Update(currentState);

            if (_virtualKeyboard?.IsVisible == true)
            {
                if (isConnected && _modeManager.CurrentMode == AppMode.Mouse)
                    _modeManager.HandleMouseModePointerInput(currentState);

                if (isConnected)
                    _virtualKeyboard.HandleInput(currentState);

                return;
            }

            if (_modeManager.CurrentMode == AppMode.Mouse && isConnected)
            {
                _modeManager.HandleMouseModeInput(currentState);
            }
        }

        private void ModeManager_ModeChanged(object? sender, ModeChangedEventArgs e)
        {
            if (e.NewMode == AppMode.Mouse)
            {
                _trayIconManager?.UpdateTooltip("HaloShift - Mouse Mode");
                _sensitivityOverlay?.HideOverlay();
                PlayModeSound("sound_activate.wav");
            }
            else
            {
                _trayIconManager?.UpdateTooltip("HaloShift - Controller Mode");
                _virtualKeyboard?.DismissRestoringPreviousFocus();
                PlayModeSound("sound_deactivate.wav");
            }
        }

        private void PlayModeSound(string fileName)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, fileName);
                if (File.Exists(path))
                {
                    using var player = new SoundPlayer(path);
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HaloShift] Unable to play mode sound: {ex.Message}");
            }
        }

        private void ModeManager_SensitivityChanged(object? sender, SensitivityChangedEventArgs e)
        {
            _sensitivityOverlay?.ShowValue(e.NewSensitivity);
        }

        private void ModeManager_ShowKeyboardRequested(object? sender, EventArgs e)
        {
            _virtualKeyboard?.ShowKeyboard();
        }

        private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer = null;
            _trayIconManager?.Dispose();
            _controllerManager?.Dispose();
            _virtualKeyboard?.Close();
            _sensitivityOverlay?.Close();
        }
    }
}
