using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Vehicles;

public partial class VehiclesViewModel : ObservableObject
{
    private readonly ICollectionView _eventsView;
    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _activeVehicle = "NO ACTIVE VEHICLE";

    [ObservableProperty]
    private string _activeVehicleStatus = "OFFLINE";

    [ObservableProperty]
    private string _activeVehicleLabel = "LAST VEHICLE";

    public VehiclesViewModel()
    {
        _eventsView = CollectionViewSource.GetDefaultView(Events);
        _eventsView.Filter = FilterEvents;

        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            var isSilent = m is SilentStateUpdateEvent;
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is VehicleEvent vehicleEvent)
            {
                void UpdateState()
                {
                    if (vehicleEvent.Action == "CONNECTED")
                    {
                        ActiveVehicle = vehicleEvent.VehicleName;
                        ActiveVehicleStatus = "CONNECTED";
                    }
                    else if (vehicleEvent.Action == "DISCONNECTED" && vehicleEvent.VehicleName == ActiveVehicle)
                    {
                        ActiveVehicleStatus = "OFFLINE";
                    }
                    else if (vehicleEvent.Action == "OUT OF SEAT" && vehicleEvent.VehicleName == ActiveVehicle)
                    {
                        ActiveVehicleStatus = "OUT OF SEAT";
                    }
                }

                if (Application.Current != null) Application.Current.Dispatcher.Invoke(UpdateState);
                else UpdateState();
            }

            if (isSilent) return;

            var uiEvent = EventMapper.Map(actualEvent);
            if (uiEvent != null && uiEvent.Category == "VEHICLES")
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
            void Clear()
            {
                Events.Clear();
                ActiveVehicle = "NONE DETECTED";
                ActiveVehicleStatus = "OFFLINE";
                ActiveVehicleLabel = "LAST VEHICLE";
            }
            if (Application.Current != null) Application.Current.Dispatcher.Invoke(Clear);
            else Clear();
        });
    }

    private bool FilterEvents(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        if (obj is not LogEventViewModel evt) return true;
        var query = SearchQuery.ToLower().Trim();
        return evt.Title.ToLower().Contains((string)query) || evt.Description.ToLower().Contains((string)query);
    }

    partial void OnSearchQueryChanged(string value) => _eventsView.Refresh();

    public ObservableCollection<LogEventViewModel> Events { get; } = new();
    public ICollectionView FilteredEvents => _eventsView;
}