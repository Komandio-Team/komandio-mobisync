using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Kills;

public partial class KillsView : UserControl
{
    public KillsView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<KillsViewModel>();
    }
}