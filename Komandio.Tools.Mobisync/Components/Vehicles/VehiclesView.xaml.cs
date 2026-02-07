using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Vehicles;

public partial class VehiclesView
{
    public VehiclesView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<VehiclesViewModel>();
    }
}
