using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class LocationStatViewModel : ObservableObject
{
    [ObservableProperty] private string _value = "DEEP SPACE";

    public LocationStatViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is LocationChangeEvent loc) Value = loc.LocationId;
            else if (actualEvent is JurisdictionEvent jur) Value = jur.Name;
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            Value = "DEEP SPACE";
        });
    }
}