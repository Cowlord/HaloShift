using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HaloShift
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IsVisible = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
