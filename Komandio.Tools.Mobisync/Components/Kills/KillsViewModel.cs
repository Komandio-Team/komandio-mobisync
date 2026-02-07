using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Kills;

public partial class KillsViewModel : ObservableObject
{
    private readonly ICollectionView _eventsView;
    [ObservableProperty] private string _searchQuery = string.Empty;

    public KillsViewModel()
    {
        _eventsView = CollectionViewSource.GetDefaultView(Events);
        _eventsView.Filter = FilterEvents;

        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            if (m is SilentStateUpdateEvent) return;

            var uiEvent = EventMapper.Map(m);
            if (uiEvent != null && uiEvent.Category == "KILLS")
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
                        if (Events.Count > 100) Events.RemoveAt(100);
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

    partial void OnSearchQueryChanged(string value) => _eventsView.Refresh();

    public ObservableCollection<LogEventViewModel> Events { get; } = new();
    public ICollectionView FilteredEvents => _eventsView;
}
