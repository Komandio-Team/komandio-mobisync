using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Services;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class DataSourceViewModel : ObservableObject
{
    private readonly ILogMonitorService _monitor;
    private readonly ISettingsService _settings;
    [ObservableProperty] private bool _isInteractionEnabled = true;

    [ObservableProperty] private string _logPath = string.Empty;

    public DataSourceViewModel(ISettingsService settings, ILogMonitorService monitor)
    {
        _settings = settings;
        _monitor = monitor;
        LogPath = _settings.LogPath;

        WeakReferenceMessenger.Default.Register<ApplicationStateMessage>(this, (r, m) =>
        {
            IsInteractionEnabled = !m.IsRunning && !m.IsSimulating;
        });
    }

    [RelayCommand]
    private void SelectLogFile()
    {
        var dialog = new OpenFileDialog { Filter = "Log Files (*.log)|*.log" };
        if (dialog.ShowDialog() == true)
        {
            LogPath = dialog.FileName;
            _settings.LogPath = LogPath;
            _settings.Save();
            
            WeakReferenceMessenger.Default.Send(new LogPathChangedMessage(LogPath));
        }
    }
}