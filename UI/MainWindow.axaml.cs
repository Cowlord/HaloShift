using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HaloShift
{
    public partial class MainWindow : Window
    {
        private bool _allowClosing;

        public MainWindow()
        {
            InitializeComponent();
            IsVisible = false;
            ShowInTaskbar = false;

            // Prevent unexpected closes - app explicitly calls Close() when needed
            Closing += (sender, e) =>
            {
                if (!_allowClosing)
                    e.Cancel = true;
            };
        }

        public void ShowMouseModeStatus()
        {
            if (this.FindControl<TextBlock>("StatusText") is { } statusText)
                statusText.Text = "Mode: Mouse";

            Width = 300;
            Height = 80;
            Opacity = 1;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            IsVisible = true;
        }

        public void HideMouseModeStatus()
        {
            IsVisible = false;
            Opacity = 0;
            Position = new PixelPoint(-32000, -32000);
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (this.FindControl<TextBlock>("StatusText") is { } statusText)
                statusText.Text = "Mode: Controller";
        }

        public new void Close()
        {
            _allowClosing = true;
            base.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
