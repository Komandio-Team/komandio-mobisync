using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Processors;

public partial class RegexGuideView : UserControl
{
    public RegexGuideView()
    {
        InitializeComponent();
        DataContext = App.Host?.Services.GetRequiredService<RegexGuideViewModel>();
    }
}