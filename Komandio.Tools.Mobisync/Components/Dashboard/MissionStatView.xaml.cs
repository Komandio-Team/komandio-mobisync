using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class MissionStatView : UserControl
{
    public MissionStatView()
    {
        InitializeComponent();
        if (App.Host != null)
        {
            DataContext = App.Host.Services.GetRequiredService<MissionStatViewModel>();
        }
    }
}
