using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;

namespace HaloShift
{
    public partial class App : Application
    {
        private DispatcherTimer? _pollTimer;
        private bool _pollInProgress;
        private ControllerManager? _controllerManager;
        private ModeManager? _modeManager;
        private TrayIconManager? _trayIconManager;
        private SensitivityOverlayWindow? _sensitivityOverlay;
        private VirtualKeyboardWindow? _virtualKeyboard;
        private MainWindow? _mainWindow;
        private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;
        private bool _wasConnected;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Initialize crash detection and restart management first
            CrashHandler.Initialize();
            RestartManager.StartWatcher();
            RestartManager.ResetRestartCount();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktopLifetime = desktop;
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                desktop.Exit += OnDesktopExit;
            }

            var settings = AppSettings.Load();
            StartupManager.SetStartup(settings.StartOnBoot);
            _controllerManager = new ControllerManager();
            _modeManager = new ModeManager();
            _modeManager.SetMouseSensitivity(settings.MouseSensitivity);
            _modeManager.OnSensitivityPersist = newValue =>
            {
                settings.MouseSensitivity = newValue;
                settings.Save();
            };

            _trayIconManager = new TrayIconManager();
            _trayIconManager.ShowControlsRequested += (_, __) => ControlsWindow.ShowOrActivate();
            _trayIconManager.ToggleModeRequested += (_, __) => _modeManager?.SwitchMode(ModeChangeInitiator.UserMenu);
            _trayIconManager.ShowKeyboardRequested += (_, __) =>
                PostToUi(() => _virtualKeyboard?.ShowKeyboard());
            _trayIconManager.ExitRequested += (_, __) => _desktopLifetime?.Shutdown();

            _modeManager.ModeChanged += ModeManager_ModeChanged;
            _modeManager.SensitivityChanged += ModeManager_SensitivityChanged;
            _modeManager.ShowKeyboardRequested += ModeManager_ShowKeyboardRequested;
            _modeManager.ShowControlsRequested += ModeManager_ShowControlsRequested;

            _sensitivityOverlay = new SensitivityOverlayWindow();

            _virtualKeyboard = new VirtualKeyboardWindow();
            _virtualKeyboard.KeyboardClosed += (_, __) =>
                _modeManager?.SuppressButtonEdgesUntilRelease();

            _mainWindow = new MainWindow();
            _mainWindow.HideMouseModeStatus();
            if (_desktopLifetime != null)
                _desktopLifetime.MainWindow = _mainWindow;
            _mainWindow.Show();

            _trayIconManager.AttachToApplication();

            _wasConnected = _controllerManager.IsConnected;
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8)
            };
            _pollTimer.Tick += (_, __) => PollControllerInput();
            _pollTimer.Start();

            base.OnFrameworkInitializationCompleted();
        }

        private void PollControllerInput()
        {
            if (_pollInProgress)
                return;

            _pollInProgress = true;
            try
            {
                if (_controllerManager == null || _modeManager == null)
                    return;

                _controllerManager.Update();
                bool isConnected = _controllerManager.IsConnected;
                var currentState = _controllerManager.GetCurrentState();

                if (_wasConnected && !isConnected && _modeManager.CurrentMode == AppMode.Mouse)
                {
                    PostToUi(() =>
                    {
                        _trayIconManager?.UpdateTooltip("HaloShift - Controller Mode");
                        _modeManager?.SwitchMode(ModeChangeInitiator.UserMenu);
                    });
                }
                _wasConnected = isConnected;

                _modeManager.Update(currentState);

                bool keyboardOpen = _virtualKeyboard?.IsKeyboardOpen == true;
                if (keyboardOpen)
                {
                    if (isConnected && _modeManager.CurrentMode == AppMode.Mouse)
                        _modeManager.HandleMouseModePointerInput(currentState);

                    if (isConnected)
                        _virtualKeyboard?.HandleInput(currentState);

                    return;
                }

                if (_modeManager.CurrentMode == AppMode.Mouse && isConnected)
                    _modeManager.HandleMouseModeInput(currentState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Controller poll error: {ex.Message}");
            }
            finally
            {
                _pollInProgress = false;
            }
        }

        private static void PostToUi(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action);
        }

        private void ModeManager_ModeChanged(object? sender, ModeChangedEventArgs e)
        {
            PostToUi(() =>
            {
                if (e.NewMode == AppMode.Mouse)
                {
                    _trayIconManager?.UpdateTooltip("HaloShift - Mouse Mode");
                    _sensitivityOverlay?.HideOverlay();
                    _mainWindow?.ShowMouseModeStatus();
                    Win32Sound.PlayWavFile("sound_activate.wav");
                }
                else
                {
                    _trayIconManager?.UpdateTooltip("HaloShift - Controller Mode");
                    _virtualKeyboard?.DismissRestoringPreviousFocus();
                    _mainWindow?.HideMouseModeStatus();
                    Win32Sound.PlayWavFile("sound_deactivate.wav");
                }
            });
        }

        private void ModeManager_SensitivityChanged(object? sender, SensitivityChangedEventArgs e)
        {
            PostToUi(() => _sensitivityOverlay?.ShowValue(e.NewSensitivity));
        }

        private void ModeManager_ShowKeyboardRequested(object? sender, EventArgs e)
        {
            PostToUi(() => _virtualKeyboard?.ShowKeyboard());
        }

        private void ModeManager_ShowControlsRequested(object? sender, EventArgs e)
        {
            PostToUi(ControlsWindow.ShowOrActivate);
        }

        private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _pollTimer?.Stop();
            _pollTimer = null;
            _modeManager?.ReleaseHeldMouseButtons();
            _trayIconManager?.Dispose();
            _controllerManager?.Dispose();
            // Note: VirtualKeyboardWindow and SensitivityOverlayWindow cannot close
            // because their Closing events are canceled. Just hide them.
            _virtualKeyboard?.HideKeyboard();
            _sensitivityOverlay?.HideOverlay();
            _mainWindow?.Close();

            // Clean shutdown - remove crash detection
            CrashHandler.Shutdown();
        }
    }
}
