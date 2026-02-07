using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Processors;

public partial class ProcessorsView : UserControl
{
    public ProcessorsView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<ProcessorsViewModel>();
    }
}