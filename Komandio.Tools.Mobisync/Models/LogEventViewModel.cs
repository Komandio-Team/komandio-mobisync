using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Models;

public partial class LogEventViewModel : ObservableObject
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string TimeString => Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public string Icon { get; init; } = "Activity24";
    public Brush IconColor { get; init; } = Brushes.SlateGray;
    public string Category { get; init; } = "SYSTEM";
    public Dictionary<string, string> Meta { get; init; } = new();

    [ObservableProperty] private int _count = 1;
}