using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class DataSourceView : UserControl
{
    public DataSourceView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<DataSourceViewModel>();
    }
}