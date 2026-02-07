using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Models;

public class RawLogLine : ObservableObject
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string TimeString => Timestamp.ToLocalTime().ToString("HH:mm:ss");
}