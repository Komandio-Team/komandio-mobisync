using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class DashboardFeedView : UserControl
{
    public DashboardFeedView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<DashboardFeedViewModel>();
    }
}