using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Services;

public interface ISettingsService
{
    string LogPath { get; set; }
    bool ReadFromBeginning { get; set; }
    bool ShowReplayedLogs { get; set; }
    List<DynamicProcessorRule> CustomProcessors { get; set; }
    Dictionary<string, string> LocationMapping { get; }
    void Load();
    void Save();
}