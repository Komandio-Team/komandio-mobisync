using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Models;

public partial class SessionStats : ObservableObject
{
    [ObservableProperty] private int _activeContracts;
    [ObservableProperty] private string _build = "Unknown";
    [ObservableProperty] private bool _inArmistice;
    [ObservableProperty] private string _jurisdiction = "DEEP SPACE";
    [ObservableProperty] private int _kills;
    [ObservableProperty] private string _pilot = "N/A";
    [ObservableProperty] private string _uptime = "00:00:00";
}