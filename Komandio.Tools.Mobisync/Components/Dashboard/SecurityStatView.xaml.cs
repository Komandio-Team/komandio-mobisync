using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class SecurityStatView : UserControl
{
    public SecurityStatView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<SecurityStatViewModel>();
    }
}