using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.SystemIntel;

public partial class SystemIntelViewModel : ObservableObject
{
    [ObservableProperty] private string _cpuName = "Unknown Processor";
    [ObservableProperty] private string _cpuCores = "-";
    
    [ObservableProperty] private string _gpuName = "Unknown Graphics";
    [ObservableProperty] private string _gpuMemory = "-";

    [ObservableProperty] private string _ramTotal = "-";
    [ObservableProperty] private string _ramUsage = "-";

    [ObservableProperty] private string _displayRes = "-";
    [ObservableProperty] private string _displayHz = "-";

    [ObservableProperty] private string _peripherals = "No Devices Detected";
    private readonly HashSet<string> _detectedPeripherals = new();

    [ObservableProperty] private string _shardId = "Searching...";
    [ObservableProperty] private string _environment = "Unknown";
    [ObservableProperty] private string _accountId = "-";
    [ObservableProperty] private string _endpoint = "-";
    [ObservableProperty] private string _sessionId = "-";
    [ObservableProperty] private string _buildVersion = "-";

    public SystemIntelViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            if (m is CpuInfoEvent cpu)
            {
                if (!string.IsNullOrEmpty(cpu.Name)) CpuName = cpu.Name;
                if (cpu.Cores > 0) CpuCores = $"{cpu.Cores} Logical Cores";
            }
            else if (m is GpuInfoEvent gpu)
            {
                if (!string.IsNullOrEmpty(gpu.Name)) GpuName = gpu.Name;
                if (!string.IsNullOrEmpty(gpu.Memory)) GpuMemory = $"{gpu.Memory} MB VRAM";
            }
            else if (m is MemoryInfoEvent mem)
            {
                RamTotal = $"{int.Parse(mem.Total) / 1024} GB ({mem.Total:N0} MB)";
                // Calculate usage %?
                if (double.TryParse(mem.Total, out double t) && double.TryParse(mem.Available, out double a))
                {
                    double used = ((t - a) / t) * 100;
                    RamUsage = $"~{used:F0}% In-Use at Startup";
                }
            }
            else if (m is DisplayInfoEvent disp)
            {
                if (!string.IsNullOrEmpty(disp.Resolution)) DisplayRes = disp.Resolution;
                if (!string.IsNullOrEmpty(disp.RefreshRate)) DisplayHz = $"@{double.Parse(disp.RefreshRate):F0}Hz";
            }
            else if (m is PeripheralEvent p)
            {
                if (_detectedPeripherals.Add(p.DeviceName))
                {
                    Peripherals = string.Join(", ", _detectedPeripherals);
                }
            }
            else if (m is NetworkIdentityEvent net)
            {
                if (!string.IsNullOrEmpty(net.Shard)) ShardId = net.Shard;
                if (!string.IsNullOrEmpty(net.EnvId)) Environment = net.EnvId.ToUpper();
                if (!string.IsNullOrEmpty(net.Endpoint)) Endpoint = net.Endpoint;
                if (!string.IsNullOrEmpty(net.SessionId)) SessionId = net.SessionId;
            }
            else if (m is AccountInfoEvent acc)
            {
                AccountId = acc.AccountId;
            }
            else if (m is BuildInfoEvent b)
            {
                BuildVersion = b.BuildNumber;
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            CpuName = "Unknown Processor";
            CpuCores = "-";
            GpuName = "Unknown Graphics";
            GpuMemory = "-";
            RamTotal = "-";
            RamUsage = "-";
            DisplayRes = "-";
            DisplayHz = "-";
            _detectedPeripherals.Clear();
            Peripherals = "No Devices Detected";
            ShardId = "Searching...";
            Environment = "Unknown";
            AccountId = "-";
            Endpoint = "-";
            SessionId = "-";
            BuildVersion = "-";
        });
    }
}