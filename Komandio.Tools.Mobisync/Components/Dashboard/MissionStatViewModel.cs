using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class MissionStatViewModel : ObservableObject
{
    [ObservableProperty] private int _count;
    private readonly HashSet<string> _activeMissionIds = new();

    public MissionStatViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is MissionAcceptedEvent accepted)
            {
                var id = accepted.MissionId.ToLower().Trim();
                if (!_activeMissionIds.Contains(id))
                {
                    _activeMissionIds.Add(id);
                    Count = _activeMissionIds.Count;
                }
            }
            else if (actualEvent is MissionEndedEvent ended)
            {
                var id = ended.MissionId.ToLower().Trim();
                if (_activeMissionIds.Remove(id))
                {
                    Count = _activeMissionIds.Count;
                }
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            _activeMissionIds.Clear();
            Count = 0;
        });
    }
}
