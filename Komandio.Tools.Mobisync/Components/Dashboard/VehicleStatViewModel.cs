using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class VehicleStatViewModel : ObservableObject
{
    [ObservableProperty] private string _vehicleName = "NONE DETECTED";
    [ObservableProperty] private string _status = "OFFLINE";
    [ObservableProperty] private string _label = "LAST VEHICLE";

    public VehicleStatViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is VehicleEvent s)
            {
                // ALWAYS normalize vehicle names for display
                var name = s.VehicleName.ToUpper().Trim();

                if (s.Action == "CONNECTED")
                {
                    VehicleName = name;
                    Status = "CONNECTED";
                }
                else if (s.Action == "DISCONNECTED")
                {
                    Status = "OFFLINE";
                }
                else if (s.Action == "OUT OF SEAT")
                {
                    Status = "OUT OF SEAT";
                }
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            VehicleName = "NONE DETECTED";
            Status = "OFFLINE";
            Label = "LAST VEHICLE";
        });
    }
}
