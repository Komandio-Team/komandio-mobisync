using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Areas;

public partial class AreasView : UserControl
{
    public AreasView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<AreasViewModel>();
    }
}