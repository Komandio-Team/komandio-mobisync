using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class KillsStatView : UserControl
{
    public KillsStatView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<KillsStatViewModel>();
    }
}