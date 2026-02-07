using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class LocationStatView : UserControl
{
    public LocationStatView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<LocationStatViewModel>();
    }
}