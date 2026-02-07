using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private DashboardFeedViewModel _feed;

    public DashboardViewModel(DashboardFeedViewModel feed)
    {
        Feed = feed;
    }
}