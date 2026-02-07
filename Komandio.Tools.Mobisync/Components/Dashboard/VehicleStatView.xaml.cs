using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class VehicleStatView
{
    public VehicleStatView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<VehicleStatViewModel>();
    }
}
