using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;

namespace HaloShift
{
    public partial class SensitivityOverlayWindow : Window
    {
        private readonly DispatcherTimer _hideTimer;

        public SensitivityOverlayWindow()
        {
            InitializeComponent();
            Hide(); // Ensure window starts hidden
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _hideTimer.Tick += (_, __) => HideOverlay();
        }

        public void ShowValue(float newSensitivity)
        {
            if (this.FindControl<TextBlock>("ValueText") is { } valueText)
            {
                valueText.Text = newSensitivity.ToString("0.0");
            }

            if (!IsVisible)
            {
                Show();
            }

            Activate();
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        public void HideOverlay()
        {
            _hideTimer.Stop();
            Hide();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
