using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Linq;

namespace HaloShift
{
    public partial class SensitivityOverlayWindow : Window
    {
        private readonly DispatcherTimer _hideTimer;

        public SensitivityOverlayWindow()
        {
            InitializeComponent();
            IsVisible = false;
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _hideTimer.Tick += (_, __) => HideOverlay();

            // Prevent window from ever actually closing
            Closing += (sender, e) => e.Cancel = true;
        }

        public void ShowValue(float newSensitivity)
        {
            if (this.FindControl<TextBlock>("ValueText") is { } valueText)
                valueText.Text = $"Sensitivity: {newSensitivity:F1}";

            var screens = Screens.All;
            var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault();
            if (primaryScreen != null)
            {
                var workArea = primaryScreen.WorkingArea;
                int x = workArea.X + (workArea.Width - (int)Width) / 2;
                int y = workArea.Y + (int)(workArea.Height * 0.15);
                Position = new PixelPoint(x, y);
            }

            if (!IsVisible)
                IsVisible = true;

            Activate();
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        public void HideOverlay()
        {
            _hideTimer.Stop();
            IsVisible = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
