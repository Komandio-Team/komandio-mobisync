using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;
using Komandio.Tools.Mobisync.Services;
using Serilog;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class DashboardFeedViewModel : ObservableObject
{
    private readonly ILogMonitorService _monitor;
    private readonly DispatcherTimer _progressTimer;
    private readonly ICollectionView _eventsView;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isCatchingUp;
    [ObservableProperty] private double _catchUpProgress;
    [ObservableProperty] private int _processedCount;
    [ObservableProperty] private string _searchQuery = string.Empty;

    public ObservableCollection<LogEventViewModel> Events { get; } = new();
    public ICollectionView FilteredEvents => _eventsView;

    public DashboardFeedViewModel(ILogMonitorService monitor)
    {
        _monitor = monitor;
        Log.Debug("UI: DashboardFeedViewModel initialized.");

        _eventsView = CollectionViewSource.GetDefaultView(Events);
        _eventsView.Filter = FilterEvents;

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _progressTimer.Tick += (s, e) => {
            IsRunning = _monitor.IsRunning;
            IsCatchingUp = _monitor.IsCatchingUp;
            CatchUpProgress = _monitor.CatchUpProgress;
            ProcessedCount = _monitor.ProcessedLineCount;
        };
        _progressTimer.Start();

        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            if (m is SilentStateUpdateEvent) return;

            var uiEvent = EventMapper.Map(m);
            if (uiEvent != null)
            {
                void AddEvent()
                {
                    var last = Events.FirstOrDefault();
                    if (last != null && last.Title == uiEvent.Title && last.Description == uiEvent.Description)
                    {
                        last.Count++;
                    }
                    else
                    {
                        Events.Insert(0, uiEvent);
                        if (Events.Count > 500) Events.RemoveAt(500);
                    }
                }

                if (Application.Current != null) Application.Current.Dispatcher.Invoke(AddEvent);
                else AddEvent();
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            if (Application.Current != null) Application.Current.Dispatcher.Invoke(() => Events.Clear());
            else Events.Clear();
        });
    }

    private bool FilterEvents(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        if (obj is not LogEventViewModel evt) return true;

        var query = SearchQuery.ToLower().Trim();
        return evt.Title.ToLower().Contains(query) || evt.Description.ToLower().Contains(query);
    }

    partial void OnSearchQueryChanged(string value)
    {
        _eventsView.Refresh();
    }

    [RelayCommand]
    private void ClearFeed()
    {
        Events.Clear();
    }
}