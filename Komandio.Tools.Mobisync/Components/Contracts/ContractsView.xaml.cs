using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Contracts;

public partial class ContractsView : UserControl
{
    public ContractsView()
    {
        InitializeComponent();
        if (App.Host != null)
        {
            DataContext = App.Host.Services.GetRequiredService<ContractsViewModel>();
        }
    }
}
