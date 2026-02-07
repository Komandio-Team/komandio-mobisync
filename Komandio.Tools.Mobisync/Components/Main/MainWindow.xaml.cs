using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;

namespace Komandio.Tools.Mobisync.Components.Main;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<MainViewModel>();
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);

        // Default: Top-left of primary
        this.Left = 0;
        this.Top = 0;

        if (App.Args.Contains("--secondary-screen"))
        {
            // Move to secondary screen if available
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length > 1)
            {
                var secondary = screens.FirstOrDefault(s => !s.Primary) ?? screens[1];
                var workingArea = secondary.WorkingArea;
                
                // Override XAML defaults to allow programmatic control
                this.MinHeight = 0;
                this.MinWidth = 0;

                // Convert physical pixels to WPF units using DPI scale
                this.Left = workingArea.Left / dpi.DpiScaleX;
                this.Top = workingArea.Top / dpi.DpiScaleY;
                this.Width = workingArea.Width / dpi.DpiScaleX;

                if (App.Args.Contains("--half-height"))
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.Height = (workingArea.Height / 2) / dpi.DpiScaleY;
                }
                else
                {
                    this.Height = workingArea.Height / dpi.DpiScaleY;
                    this.WindowState = System.Windows.WindowState.Maximized;
                }
            }
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OpenWebsite_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.komandio.com/") { UseShellExecute = true });
    }
}