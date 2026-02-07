using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class SessionOverviewView : UserControl
{
    public SessionOverviewView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<SessionOverviewViewModel>();
    }
}