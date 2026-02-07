using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Models;

public partial class MissionObjective : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    [ObservableProperty] private string _text = string.Empty;
    
    [ObservableProperty] private string _status = "PENDING"; // INPROGRESS, COMPLETED, FAILED
}

public partial class MissionModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    public DateTime AcceptedAt { get; init; }

    [ObservableProperty] private string _status = "ACCEPTED"; // ACCEPTED, COMPLETED, FAILED, ABANDONED
    [ObservableProperty] private string _currentObjectiveText = "WAITING FOR DATA...";
    [ObservableProperty] private bool _isFollowed;

    public ObservableCollection<MissionObjective> Objectives { get; } = new();
}
