using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Components.Areas;
using Komandio.Tools.Mobisync.Components.Contracts;
using Komandio.Tools.Mobisync.Components.Dashboard;
using Komandio.Tools.Mobisync.Components.Kills;
using Komandio.Tools.Mobisync.Components.Processors;
using Komandio.Tools.Mobisync.Components.SystemIntel;
using Komandio.Tools.Mobisync.Components.Vehicles;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Services;

namespace Komandio.Tools.Mobisync.Components.Main;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILogMonitorService _monitor;
    private readonly DispatcherTimer _progressTimer;

    [ObservableProperty] private DashboardViewModel _dashboard;
    [ObservableProperty] private AreasViewModel _areas;
    [ObservableProperty] private ProcessorsViewModel _processors;
    [ObservableProperty] private KillsViewModel _kills;
    [ObservableProperty] private VehiclesViewModel _vehicles;
    [ObservableProperty] private ContractsViewModel _contracts;
    [ObservableProperty] private SystemIntelViewModel _systemIntel;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _currentViewName = "DASHBOARD";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isCatchingUp;
    [ObservableProperty] private double _catchUpProgress;
    [ObservableProperty] private int _processedCount;

    // Direct counts for InfoBadges
    [ObservableProperty] private int _allCount;
    [ObservableProperty] private int _areasCount;
    [ObservableProperty] private int _killsCount;
    [ObservableProperty] private int _vehiclesCount;
    [ObservableProperty] private int _contractsCount;

    public bool CanStartMonitoring => !string.IsNullOrEmpty(_settings.LogPath);

    public MainViewModel(
        ISettingsService settings, 
        ILogMonitorService monitor,
        DashboardViewModel dashboard,
        AreasViewModel areas,
        ProcessorsViewModel processors,
        KillsViewModel kills,
        VehiclesViewModel vehicles,
        ContractsViewModel contracts,
        SystemIntelViewModel systemIntel)
    {
        _settings = settings;
        _monitor = monitor;
        Dashboard = dashboard;
        Areas = areas;
        Processors = processors;
        Kills = kills;
        Vehicles = vehicles;
        Contracts = contracts;
        SystemIntel = systemIntel;

        CurrentView = Dashboard;
        
        // Sync counts using Properties instead of fields
        Dashboard.Feed.Events.CollectionChanged += (s, e) => AllCount = Dashboard.Feed.Events.Count;
        Areas.Events.CollectionChanged += (s, e) => AreasCount = Areas.Events.Count;
        Kills.Events.CollectionChanged += (s, e) => KillsCount = Kills.Events.Count;
        Vehicles.Events.CollectionChanged += (s, e) => VehiclesCount = Vehicles.Events.Count;
        Contracts.ActiveMissions.CollectionChanged += (s, e) => SyncContractsCount();
        Contracts.HistoryMissions.CollectionChanged += (s, e) => SyncContractsCount();

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _progressTimer.Tick += (s, e) => {
            IsCatchingUp = _monitor.IsCatchingUp;
            CatchUpProgress = _monitor.CatchUpProgress;
            ProcessedCount = _monitor.ProcessedLineCount;
        };
        _progressTimer.Start();

        WeakReferenceMessenger.Default.Register<LogPathChangedMessage>(this, (r, m) => OnPropertyChanged(nameof(CanStartMonitoring)));

        ProcessArgs();
    }

    private void SyncContractsCount()
    {
        ContractsCount = Contracts.ActiveMissions.Count + Contracts.HistoryMissions.Count;
    }

    private void ProcessArgs()
    {
        var viewArg = App.Args.FirstOrDefault(a => a.StartsWith("--view="));
        if (viewArg != null)
        {
            var viewName = viewArg.Split('=').Last();
            ChangeView(viewName);
        }

        if (App.Args.Contains("--auto-start"))
        {
            Task.Run(async () => {
                await Task.Delay(1000);
                Application.Current.Dispatcher.Invoke(() => {
                    if (CanStartMonitoring && !IsRunning) ToggleMonitor();
                });
            });
        }
    }

    [RelayCommand]
    private void ToggleMonitor()
    {
        if (IsRunning)
        {
            _monitor.Stop();
            IsRunning = false;
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new ClearFeedsMessage());
            _monitor.Start(_settings.LogPath, _settings.ReadFromBeginning);
            IsRunning = true;
        }
        
        WeakReferenceMessenger.Default.Send(new ApplicationStateMessage(IsRunning, false));
    }

    [RelayCommand]
    private void ChangeView(string viewName)
    {
        CurrentViewName = viewName;
        CurrentView = viewName.ToUpper() switch
        {
            "DASHBOARD" => Dashboard,
            "AREAS" => Areas,
            "PROCESSORS" => Processors,
            "KILLS" => Kills,
            "VEHICLES" => Vehicles,
            "CONTRACTS" => Contracts,
            "SYSTEM" => SystemIntel,
            _ => Dashboard
        };
    }
}