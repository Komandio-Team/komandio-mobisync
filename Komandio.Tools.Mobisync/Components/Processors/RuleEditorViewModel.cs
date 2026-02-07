using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;
using Komandio.Tools.Mobisync.Services;
using Serilog;

namespace Komandio.Tools.Mobisync.Components.Processors;

public enum TimestampStyle
{
    ModernTerminal, // [23:58:20] - Cyan
    ClassicDev,     // 23:58:20.736 › - Amber
    CyberAudit,     // (23:58:20) - Purple
    StealthPro,     // 23:58:20 | - Slate
    FullIso         // [2026-02-06 23:58:20] - White
}

public partial class RuleEditorViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILogMonitorService _monitor;

    [ObservableProperty] private DynamicProcessorRule? _rule;
    [ObservableProperty] private TimestampStyle _selectedStyle = TimestampStyle.StealthPro;
    [ObservableProperty] private double _selectedFontSize = 12;
    
    public ObservableCollection<RawLogLine> TestBench { get; } = new();
    
    public Dictionary<TimestampStyle, string> StyleOptions { get; } = new()
    {
        { TimestampStyle.ModernTerminal, "Modern Terminal" },
        { TimestampStyle.ClassicDev, "Classic Dev" },
        { TimestampStyle.CyberAudit, "Cyber Audit" },
        { TimestampStyle.StealthPro, "Stealth Pro" },
        { TimestampStyle.FullIso, "Full ISO" }
    };

    public List<double> AvailableFontSizes { get; } = new() { 8, 9, 10, 11, 12, 14, 16 };

    public RuleEditorViewModel(ISettingsService settings, ILogMonitorService monitor)
    {
        _settings = settings;
        _monitor = monitor;

        WeakReferenceMessenger.Default.Register<DynamicProcessorRule>(this, (r, m) =>
        {
            Rule = m;
            TriggerScroll();
        });

        WeakReferenceMessenger.Default.Register<LogPathChangedMessage>(this, (r, m) =>
        {
            _ = LoadLogPreviewAsync(m.NewPath);
        });

        if (!string.IsNullOrEmpty(_settings.LogPath))
        {
            _ = LoadLogPreviewAsync(_settings.LogPath);
        }

        _monitor.OnRawLineDetected += HandleRawLine;
    }

    private void HandleRawLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            TestBench.Insert(0, new RawLogLine 
            { 
                Content = line, 
                Timestamp = LogMonitorService.ParseTimestamp(line) 
            });

            // Keep only the last 100 lines
            if (TestBench.Count > 100)
            {
                TestBench.RemoveAt(100);
            }
        });
    }

    private async Task LoadLogPreviewAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        try
        {
            var lines = await Task.Run(() =>
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length == 0) return new List<string>();

                long seekPos = Math.Max(0, stream.Length - 262144);
                stream.Seek(seekPos, SeekOrigin.Begin);

                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                if (seekPos > 0) reader.ReadLine();

                var result = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) result.Add(line);
                }
                
                return result.AsEnumerable().Reverse().Take(100).ToList();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TestBench.Clear();
                foreach (var line in lines)
                {
                    TestBench.Add(new RawLogLine 
                    { 
                        Content = line, 
                        Timestamp = LogMonitorService.ParseTimestamp(line) 
                    });
                }
            });
        }
        catch { }
    }

    partial void OnRuleChanged(DynamicProcessorRule? value)
    {
        if (value != null)
        {
            value.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DynamicProcessorRule.Regex)) 
                {
                    OnPropertyChanged(nameof(Rule));
                    TriggerScroll();
                }
            };
        }
    }

    private void TriggerScroll()
    {
        if (Rule != null && !string.IsNullOrEmpty(Rule.Regex))
        {
            WeakReferenceMessenger.Default.Send(new ScrollToFirstMatchMessage(Rule.Regex));
        }
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Save();
        _monitor.SetDynamicRules(_settings.CustomProcessors);
    }

    [RelayCommand]
    private void OpenExternal()
    {
        if (string.IsNullOrEmpty(_settings.LogPath) || !File.Exists(_settings.LogPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_settings.LogPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Processors: Failed to open log externally");
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (Rule != null && !Rule.IsBuiltIn)
        {
            _settings.CustomProcessors.Remove(Rule);
            _settings.Save();
            _monitor.SetDynamicRules(_settings.CustomProcessors);
            WeakReferenceMessenger.Default.Send("RuleDeleted");
            Rule = null;
        }
    }
}
