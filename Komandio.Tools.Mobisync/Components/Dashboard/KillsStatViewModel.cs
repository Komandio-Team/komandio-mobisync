using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class KillsStatViewModel : ObservableObject
{
    [ObservableProperty] private int _count;
    private string _pilotHandle = "N/A";

    public KillsStatViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is PlayerLoginEvent l) _pilotHandle = l.Handle;
            else if (actualEvent is CombatDeathEvent k && k.Killer == _pilotHandle) Count++;
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            Count = 0;
            _pilotHandle = "N/A";
        });
    }
}