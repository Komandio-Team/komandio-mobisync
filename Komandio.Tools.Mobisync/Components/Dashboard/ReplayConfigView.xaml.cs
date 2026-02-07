using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class ReplayConfigView : UserControl
{
    public ReplayConfigView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<ReplayConfigViewModel>();
    }
}