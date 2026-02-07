using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Services;

public interface ILogMonitorService : IDisposable
{
    bool IsRunning { get; }
    bool IsCatchingUp { get; }
    double CatchUpProgress { get; }
    int ProcessedLineCount { get; }
    int TotalLineCount { get; }
    event Action<GameEvent> OnEventDetected;
    event Action<string> OnRawLineDetected;
    void SetDynamicRules(IEnumerable<DynamicProcessorRule> rules);
    void Start(string logFilePath, bool readFromBeginning = false);
    void Stop();
    void ProcessSingleLine(string line);
}