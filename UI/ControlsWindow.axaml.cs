using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace HaloShift
{
    public partial class ControlsWindow : Window
    {
        private static ControlsWindow? _instance;

        public ControlsWindow()
        {
            InitializeComponent();

            if (this.FindControl<TextBlock>("VersionText") is { } versionText)
            {
                var version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                    ?? "1.0.0.0";
                int plusIndex = version.IndexOf('+');
                if (plusIndex > 0)
                    version = version[..plusIndex];
                versionText.Text = $"v{version}";
            }

            Closed += (_, __) =>
            {
                if (_instance == this)
                    _instance = null;
            };
        }

        public static bool IsOpen => _instance?.IsVisible == true;

        public static void ShowOrActivate()
        {
            if (_instance?.IsVisible == true)
            {
                _instance.Activate();
                return;
            }

            _instance = new ControlsWindow();
            _instance.Show();
            _instance.Activate();
        }

        public static void CloseIfOpen()
        {
            if (_instance?.IsVisible == true)
                ((Window)_instance).Close();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
