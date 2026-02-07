using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Services;

namespace Komandio.Tools.Mobisync.Components.Dashboard
{
    public partial class ReplayConfigViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;
        private readonly ILogMonitorService _monitor;

        [ObservableProperty] private bool _readFromBeginning;
        [ObservableProperty] private bool _showReplayedLogs;
        [ObservableProperty] private bool _isInteractionEnabled = true;

        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(IsShowReplayedLogsEnabled))]
        private bool _isMonitoring;

        public bool IsShowReplayedLogsEnabled => IsInteractionEnabled && ReadFromBeginning;

        public ReplayConfigViewModel(ISettingsService settings, ILogMonitorService monitor)
        {
            _settings = settings;
            _monitor = monitor;
            
            ReadFromBeginning = _settings.ReadFromBeginning;
            ShowReplayedLogs = _settings.ShowReplayedLogs;

            WeakReferenceMessenger.Default.Register<ApplicationStateMessage>(this, (r, m) =>
            {
                IsInteractionEnabled = !m.IsRunning && !m.IsSimulating;
                IsMonitoring = m.IsRunning;
                
                OnPropertyChanged(nameof(IsShowReplayedLogsEnabled));
            });
        }

        partial void OnReadFromBeginningChanged(bool value)
        {
            _settings.ReadFromBeginning = value;
            _settings.Save();
            OnPropertyChanged(nameof(IsShowReplayedLogsEnabled));
        }

        partial void OnShowReplayedLogsChanged(bool value)
        {
            _settings.ShowReplayedLogs = value;
            _settings.Save();
        }
    }
}
