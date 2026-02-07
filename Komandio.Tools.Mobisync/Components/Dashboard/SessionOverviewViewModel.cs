using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class SessionOverviewViewModel : ObservableObject
{
    [ObservableProperty] private string _jurisdiction = "DEEP SPACE";
    [ObservableProperty] private string _pilot = "N/A";
    [ObservableProperty] private string _uptime = "00:00:00";
    
    [ObservableProperty] private string _shard = "Unknown";
    [ObservableProperty] private string _gpu = "-";
    [ObservableProperty] private string _accountId = "-";
    [ObservableProperty] private string _env = "PUB";
    [ObservableProperty] private string _build = "-";
    [ObservableProperty] private string _input = "Keyboard/Mouse";
    [ObservableProperty] private string _serverIp = "-";

    private DateTime? _sessionStartTime;
    private readonly HashSet<string> _inputs = new();

    public SessionOverviewViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            // 1. Fallback: Start timer on FIRST detected event if not set
            if (_sessionStartTime == null)
            {
                _sessionStartTime = m.Timestamp;
            }

            if (m is PlayerLoginEvent l) 
            {
                Pilot = l.Handle;
                // 2. Authoritative: Reset timer on actual login
                _sessionStartTime = l.Timestamp;
            }
            else if (m is JurisdictionEvent j) 
            {
                Jurisdiction = j.Name;
            }
            else if (m is SessionUptimeEvent u) 
            {
                Uptime = TimeSpan.FromSeconds(u.Seconds).ToString(@"hh\:mm\:ss");
                // 3. Sync: Adjust start time to match server uptime
                _sessionStartTime = m.Timestamp.AddSeconds(-u.Seconds);
            }
            else if (m is NetworkIdentityEvent net)
            {
                if (!string.IsNullOrEmpty(net.Shard)) Shard = net.Shard;
                if (!string.IsNullOrEmpty(net.EnvId)) Env = net.EnvId.ToUpper();
            }
            else if (m is ServerConnectionEvent s)
            {
                ServerIp = s.Address;
            }
            else if (m is GpuInfoEvent g)
            {
                if (!string.IsNullOrEmpty(g.Name)) Gpu = g.Name.Replace("AMD Radeon ", "").Replace("NVIDIA GeForce ", "");
            }
            else if (m is AccountInfoEvent acc)
            {
                AccountId = acc.AccountId;
            }
            else if (m is BuildInfoEvent b)
            {
                Build = b.BuildNumber;
            }
            else if (m is PeripheralEvent p)
            {
                if (_inputs.Add(p.DeviceName))
                {
                    Input = _inputs.Count > 1 ? "Dual/Multi Input" : p.DeviceName;
                }
            }

            // Continuous update
            if (_sessionStartTime.HasValue)
            {
                var diff = m.Timestamp - _sessionStartTime.Value;
                // Ensure we don't show negative time if clocks drift slightly
                if (diff.TotalSeconds >= 0)
                {
                    Uptime = diff.ToString(@"hh\:mm\:ss");
                }
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            Jurisdiction = "DEEP SPACE";
            Pilot = "N/A";
            Uptime = "00:00:00";
            Shard = "Unknown";
            Gpu = "-";
            AccountId = "-";
            Env = "PUB";
            Build = "-";
            Input = "Keyboard/Mouse";
            ServerIp = "-";
            _sessionStartTime = null;
            _inputs.Clear();
        });
    }
}