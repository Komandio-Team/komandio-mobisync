using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Processors;

public partial class RuleListView : UserControl
{
    public RuleListView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<RuleListViewModel>();
    }
}