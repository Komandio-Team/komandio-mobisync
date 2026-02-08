using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;
using Serilog;

namespace Komandio.Tools.Mobisync.Services
{
    public class LogMonitorService : ILogMonitorService
    {
        private readonly List<ILogEventProcessor> _processors = new();
        private readonly List<DynamicProcessorRule> _dynamicRules = new();
        private readonly IMessenger _messenger;
        private readonly ISettingsService _settings;
        
        private readonly ConcurrentQueue<GameEvent> _eventQueue = new();
        private readonly ConcurrentQueue<GameEvent> _replayQueue = new();
        
        private CancellationTokenSource? _cts;
        private Task? _producerTask;
        private Task? _consumerTask;

        public event Action<GameEvent>? OnEventDetected;
        public event Action<string>? OnRawLineDetected;
        
        public bool IsRunning => _producerTask is { IsCompleted: false };
        public bool IsCatchingUp { get; private set; }
        public double CatchUpProgress { get; private set; }
        public int ProcessedLineCount { get; private set; }
        public int TotalLineCount { get; private set; }

        public LogMonitorService(ISettingsService settings, IMessenger? messenger = null)
        {
            _settings = settings;
            _messenger = messenger ?? WeakReferenceMessenger.Default;
            
            _processors.Add(new CharacterStatusProcessor());
            _processors.Add(new BuildInfoProcessor());
            _processors.Add(new JurisdictionProcessor());
            _processors.Add(new ArmisticeProcessor());
            _processors.Add(new SessionUptimeProcessor());
            _processors.Add(new KillProcessor());
            _processors.Add(new VehicleProcessor());
            _processors.Add(new LocationRequestProcessor(settings));
            _processors.Add(new MedicalProcessor());
            _processors.Add(new QuantumProcessor());
            _processors.Add(new DeathSpawnProcessor());
            _processors.Add(new MissionProcessor(_messenger));
            _processors.Add(new HeartbeatProcessor());
            _processors.Add(new HardwareProcessor());
            _processors.Add(new NetworkProcessor(_messenger));
            _processors.Add(new SessionStartProcessor(_messenger));
        }

        public void SetDynamicRules(IEnumerable<DynamicProcessorRule> rules)
        {
            lock (_dynamicRules)
            {
                _dynamicRules.Clear();
                _dynamicRules.AddRange(rules);
            }
        }

        public void Start(string logFilePath, bool readFromBeginning = false)
        {
            if (IsRunning) return;

            Log.Information("Engine: Starting. Path: {Path}, FromBeginning: {ReadFromBeginning}", logFilePath, readFromBeginning);
            
            _cts = new CancellationTokenSource();
            _eventQueue.Clear();
            _replayQueue.Clear();
            
            CatchUpProgress = 0;
            ProcessedLineCount = 0;
            TotalLineCount = 0;

            _producerTask = Task.Run(() => ProducerLoopAsync(logFilePath, readFromBeginning, _cts.Token));
            _consumerTask = Task.Run(() => ConsumerLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { Task.WaitAll(new[] { _producerTask, _consumerTask }.Where(t => t != null).ToArray()!, 1000); } catch { }
            finally { _cts?.Dispose(); _cts = null; _producerTask = null; _consumerTask = null; IsCatchingUp = false; CatchUpProgress = 0; }
        }

        public void ProcessSingleLine(string line)
        {
            var events = ScanLineForEvents(line);
            foreach (var evt in events)
            {
                OnEventDetected?.Invoke(evt);
                _messenger.Send<GameEvent>(evt);
            }
            OnRawLineDetected?.Invoke(line);
        }

        private List<GameEvent> ScanLineForEvents(string line)
        {
            var matches = new List<GameEvent>();
            var ts = ParseTimestamp(line);

            foreach (var processor in _processors)
            {
                if (processor.CanProcess(line))
                {
                    var gameEvent = processor.Process(line);
                    if (gameEvent != null) matches.Add(gameEvent);
                }
            }

            lock (_dynamicRules)
            {
                foreach (var rule in _dynamicRules)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(rule.Regex)) continue;
                        var match = Regex.Match(line, rule.Regex);
                        if (match.Success) matches.Add(new DynamicEvent(ts, rule, match));
                    }
                    catch { }
                }
            }
            return matches;
        }

        private async Task ProducerLoopAsync(string logFilePath, bool readFromBeginning, CancellationToken ct)
        {
            if (!File.Exists(logFilePath)) return;
            try
            {
                using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                IsCatchingUp = readFromBeginning;
                long totalBytes = stream.Length;
                
                if (!readFromBeginning) stream.Seek(0, SeekOrigin.End);

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line is null)
                    {
                        if (IsCatchingUp) 
                        {
                            IsCatchingUp = false;
                            CatchUpProgress = 100;
                        }
                        await Task.Delay(250, ct);
                        continue;
                    }

                    ProcessedLineCount++;
                    if (IsCatchingUp && totalBytes > 0)
                    {
                        CatchUpProgress = (double)stream.Position / totalBytes * 100;
                    }

                    if (!IsCatchingUp) OnRawLineDetected?.Invoke(line);

                    var events = ScanLineForEvents(line);
                    foreach (var evt in events)
                    {
                        if (IsCatchingUp) _replayQueue.Enqueue(evt);
                        else _eventQueue.Enqueue(evt);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { Log.Error(ex, "Engine: Producer Error"); }
        }

        private async Task ConsumerLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_replayQueue.TryDequeue(out var replayEvent))
                {
                    bool showLogs = _settings.ShowReplayedLogs;
                    var evtToPublish = showLogs ? replayEvent : new SilentStateUpdateEvent(replayEvent);

                    OnEventDetected?.Invoke(evtToPublish);
                    _messenger.Send<GameEvent>(evtToPublish);
                }
                else if (_eventQueue.TryDequeue(out var liveEvent))
                {
                    OnEventDetected?.Invoke(liveEvent);
                    _messenger.Send<GameEvent>(liveEvent);
                }
                else await Task.Delay(50, ct);
            }
        }

        public static DateTime ParseTimestamp(string line)
        {
            var match = LogRegex.Timestamp().Match(line);
            return match is { Success: true } && DateTime.TryParse(match.Groups[1].Value, out var dt) ? dt : DateTime.UtcNow;
        }

        public void Dispose() => Stop();
    }

    public static partial class LogRegex
    {
        [GeneratedRegex(@"<(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z)>", RegexOptions.IgnoreCase)]
        public static partial Regex Timestamp();

        [GeneratedRegex(@"Handle\[(?<handle>[^\]]+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex CharacterLogin();

        [GeneratedRegex(@"BackupNameAttachment="".*?Build\((?<build>\d+)\)", RegexOptions.IgnoreCase)]
        public static partial Regex BuildInfo();

        [GeneratedRegex(@"Added notification ""Entered (?<name>.*?) Jurisdiction:? """, RegexOptions.IgnoreCase)]
        public static partial Regex Jurisdiction();

        [GeneratedRegex(@"Added notification ""(?<state>Entering|Leaving) Armistice Zone", RegexOptions.IgnoreCase)]
        public static partial Regex Armistice();

        [GeneratedRegex(@"Session Uptime: (?<seconds>\d+)s", RegexOptions.IgnoreCase)]
        public static partial Regex SessionUptime();

        [GeneratedRegex(@"Location\[(?<loc>[^\]]+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex LocationId();

        [GeneratedRegex(@"SubsumptionMissionComponent gained authority (?<name>\w+)", RegexOptions.IgnoreCase)]
        public static partial Regex AreaAuthority();

        [GeneratedRegex(@"Obstructing Entity (?<id>[^ \[\]]+)", RegexOptions.IgnoreCase)]
        public static partial Regex Obstruction();

        [GeneratedRegex(@"ATC Location: (?<loc>[^ \[\]\r\n]+)", RegexOptions.IgnoreCase)]
        public static partial Regex AtcLocation();

        [GeneratedRegex(@"Station_DockingTube_(?<id>[^ \r\n\[\]]+)", RegexOptions.IgnoreCase)]
        public static partial Regex StationDocking();

        [GeneratedRegex(@"Quantum travel (?<state>started|finished|aborted)", RegexOptions.IgnoreCase)]
        public static partial Regex QuantumState();

        [GeneratedRegex(@"<Actor Death> Victim: '(?<victim>.*?)' Killer: '(?<killer>.*?)' \(Reason: '(?<reason>.*?)'\)", RegexOptions.IgnoreCase)]
        public static partial Regex ActorDeath();

        [GeneratedRegex(@"[Nn]otification ""You have (?<action>joined|left)(?: the)? channel '@vehicle_Name(?<name>[^ ']+)", RegexOptions.IgnoreCase)]
        public static partial Regex VehicleChannel();

        [GeneratedRegex(@"CVehicleMovementBase::(Clear|Set)Driver: .*? control token for '(?<name>.*?)'", RegexOptions.IgnoreCase)]
        public static partial Regex VehicleControl();

        [GeneratedRegex(@"Added notification ""(?<text>[^""]*?)""", RegexOptions.IgnoreCase)]
        public static partial Regex MissionNotificationText();

        [GeneratedRegex(@"MissionId:\s*\[\s*(?<missionId>[^\]]*?)\s*\]", RegexOptions.IgnoreCase)]
        public static partial Regex MissionId();

        [GeneratedRegex(@"ObjectiveId:\s*\[\s*(?<objectiveId>[^\]]*?)\s*\]", RegexOptions.IgnoreCase)]
        public static partial Regex ObjectiveId();

        [GeneratedRegex(@"ObjectiveUpserted push message for: mission_id (?<missionId>[^ ]+) - objective_id (?<objectiveId>[^ ]+) - state (?<state>[^ ]+)", RegexOptions.IgnoreCase)]
        public static partial Regex MissionObjectiveUpserted();

        [GeneratedRegex(@"Objective updated id=(?<objectiveId>[^,]*?), .*? uiDisplay\[.*?\]\[Text=(?<text>.*?)\]", RegexOptions.IgnoreCase)]
        public static partial Regex MissionObjectiveTechnicalText();

        [GeneratedRegex(@"MissionEnded push message for: mission_id (?<missionId>[^ ]+) - mission_state (?<state>[^ ]+)", RegexOptions.IgnoreCase)]
        public static partial Regex MissionEnded();

        [GeneratedRegex(@"Creating objective marker: missionId \[(?<missionId>[^\]]+)\], .*? contract \[(?<contract>[^\]]+)\], objectiveId \[(?<objectiveId>[^\]]+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex MissionMarkerCreated();

        [GeneratedRegex(@"AddToPlayerDataBank>.*?missionId\[\s*(?<missionId>[^\]]*?)\s*\], objectiveId\[\s*(?<objectiveId>[^\]]*?)\s*\]", RegexOptions.IgnoreCase)]
        public static partial Regex MissionMarkerAdded();

        [GeneratedRegex(@"RemoveFromPlayerDataBank>.*?missionId\[\s*(?<missionId>[^\]]*?)\s*\], objectiveId\[\s*(?<objectiveId>[^\]]*?)\s*\]", RegexOptions.IgnoreCase)]
        public static partial Regex MissionMarkerRemoved();

        [GeneratedRegex(@"(Character killed|OnClientSpawned\] Spawned!)", RegexOptions.IgnoreCase)]
        public static partial Regex DeathSpawn();

        [GeneratedRegex(@"\[STAMINA\] Player started (?<type>suffocating|depressurization)", RegexOptions.IgnoreCase)]
        public static partial Regex MedicalAlert();

        [GeneratedRegex(@"Host CPU: (?<name>.*)", RegexOptions.IgnoreCase)]
        public static partial Regex HostCpu();

        [GeneratedRegex(@"Logical CPU Count: (?<count>\d+)", RegexOptions.IgnoreCase)]
        public static partial Regex CpuCount();

        [GeneratedRegex(@"- (?<name>.*?) \(vendor", RegexOptions.IgnoreCase)]
        public static partial Regex GpuName();

        [GeneratedRegex(@"Dedicated video memory: (?<mem>\d+) MB", RegexOptions.IgnoreCase)]
        public static partial Regex GpuMemory();

        [GeneratedRegex(@"(?<total>\d+)MB physical memory installed, (?<avail>\d+)MB available", RegexOptions.IgnoreCase)]
        public static partial Regex PhysicalMemory();

        [GeneratedRegex(@"Current display mode is (?<res>[\dx]+)", RegexOptions.IgnoreCase)]
        public static partial Regex DisplayMode();

        [GeneratedRegex(@"Borderless at (?<hz>[\d\.]+)Hz", RegexOptions.IgnoreCase)]
        public static partial Regex DisplayHz();

        [GeneratedRegex(@"- Connected joystick\d+: \s*(?<name>.*?)\s*\{", RegexOptions.IgnoreCase)]
        public static partial Regex JoystickConnected();

        [GeneratedRegex(@"\[Trace\] @session:\s*'(?<id>[^']+)'", RegexOptions.IgnoreCase)]
        public static partial Regex TraceSession();

        [GeneratedRegex(@"\[Trace\] @host_session:\s*'(?<shard>[^']+)'", RegexOptions.IgnoreCase)]
        public static partial Regex TraceShard();

        [GeneratedRegex(@"\[Trace\] @env_session:\s*'(?<env>[^']+)'", RegexOptions.IgnoreCase)]
        public static partial Regex TraceEnv();

        [GeneratedRegex(@"to endpoint (?<url>[^ ]+)", RegexOptions.IgnoreCase)]
        public static partial Regex RpcEndpoint();

        [GeneratedRegex(@"Connection requested to: (?<ip>[\d\.]+)", RegexOptions.IgnoreCase)]
        public static partial Regex ServerConnection();

        [GeneratedRegex(@"\[Notice\] <Join PU> address\[(?<ip>[\d\.]+)\] port\[\d+\] shard\[(?<shard>[^\]]+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex JoinPu();

        [GeneratedRegex(@"\[Notice\] <Update Shard Id> New Shard Id: (?<shard>[^ \.]+)", RegexOptions.IgnoreCase)]
        public static partial Regex UpdateShard();

        [GeneratedRegex(@"AccountID\[(?<id>\d+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex AccountId();

        [GeneratedRegex(@"requested inventory for Location\[(?<loc>[^\]]+)\]", RegexOptions.IgnoreCase)]
        public static partial Regex InventoryRequest();
    }

    public interface ILogEventProcessor
    {
        bool CanProcess(string line);
        GameEvent? Process(string line);
    }

    public class CharacterStatusProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Handle[");
        public GameEvent? Process(string line) => LogRegex.CharacterLogin().Match(line) is { Success: true } m ? new PlayerLoginEvent(LogMonitorService.ParseTimestamp(line), m.Groups["handle"].Value) : null;
    }

    public class BuildInfoProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("BackupNameAttachment=");
        public GameEvent? Process(string line) => LogRegex.BuildInfo().Match(line) is { Success: true } m ? new BuildInfoEvent(LogMonitorService.ParseTimestamp(line), m.Groups["build"].Value) : null;
    }

    public class JurisdictionProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Jurisdiction");
        public GameEvent? Process(string line) => LogRegex.Jurisdiction().Match(line) is { Success: true } m ? new JurisdictionEvent(LogMonitorService.ParseTimestamp(line), m.Groups["name"].Value) : null;
    }

    public class ArmisticeProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Armistice Zone");
        public GameEvent? Process(string line) => LogRegex.Armistice().Match(line) is { Success: true } m ? new ArmisticeEvent(LogMonitorService.ParseTimestamp(line), m.Groups["state"].Value == "Entering") : null;
    }

    public class SessionUptimeProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Session Uptime:");
        public GameEvent? Process(string line) => LogRegex.SessionUptime().Match(line) is { Success: true } m ? new SessionUptimeEvent(LogMonitorService.ParseTimestamp(line), int.Parse(m.Groups["seconds"].Value)) : null;
    }

    public class KillProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Actor Death");
        public GameEvent? Process(string line) => LogRegex.ActorDeath().Match(line) is { Success: true } m ? new CombatDeathEvent(LogMonitorService.ParseTimestamp(line), m.Groups["victim"].Value, m.Groups["killer"].Value, m.Groups["reason"].Value) : null;
    }

    public class VehicleProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("channel '@vehicle_Name") || line.Contains("control token for '");
        public GameEvent? Process(string line)
        {
            var ts = LogMonitorService.ParseTimestamp(line);
            var channelMatch = LogRegex.VehicleChannel().Match(line);
            if (channelMatch.Success) return new VehicleEvent(ts, CleanName(channelMatch.Groups["name"].Value), channelMatch.Groups["action"].Value == "joined" ? "CONNECTED" : "DISCONNECTED");
            var controlMatch = LogRegex.VehicleControl().Match(line);
            if (controlMatch.Success) return new VehicleEvent(ts, CleanName(controlMatch.Groups["name"].Value), line.Contains("SetDriver") ? "CONNECTED" : "OUT OF SEAT");
            return null;
        }

        private string CleanName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "UNKNOWN";
            
            // Remove the common 'vehicle_Name' internal prefix if it leaked through
            var name = input.Replace("vehicle_Name", "");

            var parts = name.Split('_');
            string result;

            if (parts.Length > 1)
            {
                // Discard the first part if it's likely a manufacturer code (e.g., AEGS, RSI, DRAK)
                // We take all parts EXCEPT the first one
                result = string.Join(" ", parts.Skip(1));
            }
            else
            {
                result = name;
            }

            return result.Replace("_", " ").ToUpper().Trim();
        }
    }

    public class LocationRequestProcessor : ILogEventProcessor
    {
        private readonly ISettingsService _settings;
        public LocationRequestProcessor(ISettingsService settings) => _settings = settings;
        public bool CanProcess(string line) => line.Contains("Location[") || line.Contains("Obstructing Entity") || line.Contains("ATC Location:") || line.Contains("Station_DockingTube") || line.Contains("requested inventory");
        public GameEvent? Process(string line)
        {
            var ts = LogMonitorService.ParseTimestamp(line);
            
            var inv = LogRegex.InventoryRequest().Match(line);
            if (inv.Success)
            {
                var locId = inv.Groups["loc"].Value;
                var name = _settings.LocationMapping.TryGetValue(locId, out var n) ? n : locId;
                return new LocationChangeEvent(ts, name);
            }

            var atc = LogRegex.AtcLocation().Match(line);
            if (atc.Success && _settings.LocationMapping.TryGetValue(atc.Groups["loc"].Value, out var n1)) return new LocationChangeEvent(ts, n1);
            var dock = LogRegex.StationDocking().Match(line);
            if (dock.Success && _settings.LocationMapping.TryGetValue(dock.Groups["id"].Value, out var n2)) return new LocationChangeEvent(ts, n2);
            var loc = LogRegex.LocationId().Match(line);
            if (loc.Success && _settings.LocationMapping.TryGetValue(loc.Groups["loc"].Value, out var n3)) return new LocationChangeEvent(ts, n3);
            var obs = LogRegex.Obstruction().Match(line);
            if (obs.Success && _settings.LocationMapping.TryGetValue(obs.Groups["id"].Value, out var n4)) return new LocationChangeEvent(ts, "NEAR " + n4);
            return null;
        }
    }

    public class MedicalProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("[STAMINA]");
        public GameEvent? Process(string line) => LogRegex.MedicalAlert().Match(line) is { Success: true } m ? new MedicalAlertEvent(LogMonitorService.ParseTimestamp(line), m.Groups["type"].Value) : null;
    }

    public class QuantumProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Quantum travel");
        public GameEvent? Process(string line) => LogRegex.QuantumState().Match(line) is { Success: true } m ? new QuantumEvent(LogMonitorService.ParseTimestamp(line), m.Groups["state"].Value) : null;
    }

    public class DeathSpawnProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.Contains("Character killed") || line.Contains("Spawned!");
        public GameEvent? Process(string line) => LogRegex.DeathSpawn().Match(line) is { Success: true } m ? new DeathSpawnEvent(LogMonitorService.ParseTimestamp(line), m.Value.Contains("Spawned!")) : null;
    }

    public class MissionProcessor : ILogEventProcessor
    {
        private readonly IMessenger _messenger;
        private string? _pendingText;
        public MissionProcessor(IMessenger messenger) => _messenger = messenger;
        public bool CanProcess(string line) => line.Contains("Added notification") || line.Contains("<MissionEnded>") || line.Contains("<ObjectiveUpserted>") || line.Contains("Creating objective marker") || line.Contains("MissionId:") || line.Contains("AddToPlayerDataBank") || line.Contains("RemoveFromPlayerDataBank") || line.Contains("UpdateActiveObjective");
        public GameEvent? Process(string line)
        {
            var ts = LogMonitorService.ParseTimestamp(line);
            if (line.Contains("<ObjectiveUpserted>")) { var m = LogRegex.MissionObjectiveUpserted().Match(line); if (m.Success) return new ObjectiveUpdateEvent(ts, m.Groups["missionId"].Value, m.Groups["objectiveId"].Value, NormalizeState(m.Groups["state"].Value)); }
            if (line.Contains("AddToPlayerDataBank")) { var m = LogRegex.MissionMarkerAdded().Match(line); if (m.Success) return new ObjectiveUpdateEvent(ts, m.Groups["missionId"].Value.Trim(), m.Groups["objectiveId"].Value.Trim(), "TRACKED"); }
            if (line.Contains("RemoveFromPlayerDataBank")) { var m = LogRegex.MissionMarkerRemoved().Match(line); if (m.Success) return new ObjectiveUpdateEvent(ts, m.Groups["missionId"].Value.Trim(), m.Groups["objectiveId"].Value.Trim(), "UNTRACKED"); }
            if (line.Contains("Added notification")) { var m = LogRegex.MissionNotificationText().Match(line); if (m.Success) _pendingText = m.Groups["text"].Value; }
            if (line.Contains("MissionId:"))
            {
                var idM = LogRegex.MissionId().Match(line); var oIdM = LogRegex.ObjectiveId().Match(line);
                if (idM.Success)
                {
                    var mId = idM.Groups["missionId"].Value.Trim(); var oId = oIdM.Success ? oIdM.Groups["objectiveId"].Value.Trim() : "";
                    var text = _pendingText ?? ""; _pendingText = null;
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text.Contains("Accepted:")) return new MissionAcceptedEvent(ts, mId, text.Replace("Contract Accepted:", "").Trim());
                        if (text.Contains("New Objective:")) return new ObjectiveUpdateEvent(ts, mId, oId, "INPROGRESS", text.Replace("New Objective:", "").Trim());
                        if (text.Contains("Objective Complete:")) return new ObjectiveUpdateEvent(ts, mId, oId, "COMPLETED", text.Replace("Objective Complete:", "").Trim());
                        if (text.Contains("Contract Complete:")) return new MissionEndedEvent(ts, mId, "MISSION_STATE_SUCCEEDED");
                    }
                }
            }
            if (line.Contains("UpdateActiveObjective")) 
            { 
                var m = LogRegex.MissionObjectiveTechnicalText().Match(line); 
                if (m.Success) return new ObjectiveUpdateEvent(ts, "", m.Groups["objectiveId"].Value, "INPROGRESS", m.Groups["text"].Value);
            }
            if (line.Contains("Creating objective marker")) { var m = LogRegex.MissionMarkerCreated().Match(line); if (m.Success) { var mId = m.Groups["missionId"].Value; var oId = m.Groups["objectiveId"].Value; _messenger.Send(new ObjectiveUpdateEvent(ts, mId, oId, "PENDING")); return new MissionAcceptedEvent(ts, mId, m.Groups["contract"].Value); } }
            if (line.Contains("<MissionEnded>")) { var m = LogRegex.MissionEnded().Match(line); if (m.Success) return new MissionEndedEvent(ts, m.Groups["missionId"].Value, m.Groups["state"].Value); }
            return null;
        }
        private string NormalizeState(string raw) { var s = raw.ToUpper(); if (s.Contains("COMPLETED") || s.Contains("SUCCEEDED")) return "COMPLETED"; if (s.Contains("FAILED")) return "FAILED"; if (s.Contains("INPROGRESS")) return "INPROGRESS"; return "PENDING"; }
    }

    public class HeartbeatProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => line.StartsWith("<");
        public GameEvent? Process(string line) => new HeartbeatEvent(LogMonitorService.ParseTimestamp(line));
    }

    public class HardwareProcessor : ILogEventProcessor
    {
        public bool CanProcess(string line) => 
            line.IndexOf("CPU", StringComparison.OrdinalIgnoreCase) >= 0 || 
            line.IndexOf("memory", StringComparison.OrdinalIgnoreCase) >= 0 || 
            line.IndexOf("display", StringComparison.OrdinalIgnoreCase) >= 0 || 
            line.IndexOf("joystick", StringComparison.OrdinalIgnoreCase) >= 0 || 
            line.IndexOf("vendor", StringComparison.OrdinalIgnoreCase) >= 0 || 
            line.IndexOf("adapter", StringComparison.OrdinalIgnoreCase) >= 0;
        
        public GameEvent? Process(string line)
        {
            var ts = LogMonitorService.ParseTimestamp(line);
            
            var cpu = LogRegex.HostCpu().Match(line);
            if (cpu.Success) return new CpuInfoEvent(ts, cpu.Groups["name"].Value.Trim(), 0);
            
            var cores = LogRegex.CpuCount().Match(line);
            if (cores.Success) return new CpuInfoEvent(ts, "", int.Parse(cores.Groups["count"].Value));

            var mem = LogRegex.PhysicalMemory().Match(line);
            if (mem.Success) return new MemoryInfoEvent(ts, mem.Groups["total"].Value, mem.Groups["avail"].Value);

            var gpu = LogRegex.GpuName().Match(line);
            if (gpu.Success)
            {
                var name = gpu.Groups["name"].Value.Trim();
                if (name.Contains("Microsoft Basic Render Driver")) return null;
                return new GpuInfoEvent(ts, name, "");
            }

            var vram = LogRegex.GpuMemory().Match(line);
            if (vram.Success) return new GpuInfoEvent(ts, "", vram.Groups["mem"].Value);

            var disp = LogRegex.DisplayMode().Match(line);
            if (disp.Success) return new DisplayInfoEvent(ts, disp.Groups["res"].Value, "");

            var hz = LogRegex.DisplayHz().Match(line);
            if (hz.Success) return new DisplayInfoEvent(ts, "", hz.Groups["hz"].Value);

            var joy = LogRegex.JoystickConnected().Match(line);
            if (joy.Success) return new PeripheralEvent(ts, joy.Groups["name"].Value.Trim());

            return null;
        }
    }

    public class NetworkProcessor : ILogEventProcessor
    {
        private readonly IMessenger _messenger;
        public NetworkProcessor(IMessenger messenger) => _messenger = messenger;

        public bool CanProcess(string line) => 
            line.Contains("[Trace]") || 
            line.Contains("endpoint") || 
            line.Contains("AccountID") || 
            line.Contains("Connection requested to") ||
            line.Contains("<Join PU>") ||
            line.Contains("<Update Shard Id>");

        public GameEvent? Process(string line)
        {
            var ts = LogMonitorService.ParseTimestamp(line);

            var join = LogRegex.JoinPu().Match(line);
            if (join.Success)
            {
                _messenger.Send(new ServerConnectionEvent(ts, join.Groups["ip"].Value));
                return new NetworkIdentityEvent(ts, GetFriendlyShardName(join.Groups["shard"].Value), "", "", "");
            }

            var update = LogRegex.UpdateShard().Match(line);
            if (update.Success) return new NetworkIdentityEvent(ts, GetFriendlyShardName(update.Groups["shard"].Value), "", "", "");

            var server = LogRegex.ServerConnection().Match(line);
            if (server.Success) return new ServerConnectionEvent(ts, server.Groups["ip"].Value);

            var session = LogRegex.TraceSession().Match(line);
            if (session.Success) return new NetworkIdentityEvent(ts, "", "", session.Groups["id"].Value, "");

            var shard = LogRegex.TraceShard().Match(line);
            if (shard.Success) return new NetworkIdentityEvent(ts, GetFriendlyShardName(shard.Groups["shard"].Value), "", "", "");

            var env = LogRegex.TraceEnv().Match(line);
            if (env.Success) return new NetworkIdentityEvent(ts, "", "", "", env.Groups["env"].Value);

            var ep = LogRegex.RpcEndpoint().Match(line);
            if (ep.Success) return new NetworkIdentityEvent(ts, "", ep.Groups["url"].Value, "", "");

            var acc = LogRegex.AccountId().Match(line);
            if (acc.Success) return new AccountInfoEvent(ts, acc.Groups["id"].Value);

            return null;
        }

        private string GetFriendlyShardName(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Equals("local_shard", StringComparison.OrdinalIgnoreCase)) 
                return "FRONTEND";

            var parts = raw.Split('_');
            if (parts.Length < 2) return raw.ToUpper();

            // Format: pub_euw1b_..._080
            // Find the region part (usually euw1b, usw2a, etc.)
            var regionPart = parts.FirstOrDefault(p => p.Length >= 3 && (p.StartsWith("eu") || p.StartsWith("us") || p.StartsWith("ap") || p.StartsWith("au")));
            
            if (string.IsNullOrEmpty(regionPart)) return raw.ToUpper();

            string region = regionPart.Substring(0, 2).ToLower() switch {
                "eu" => "EU",
                "us" => "US",
                "ap" => "ASIA",
                "au" => "AUS",
                _ => regionPart.Substring(0, 2).ToUpper()
            };

            string zone = "";
            if (regionPart.Length > 2)
            {
                zone = regionPart[2].ToString().ToLower() switch {
                    "w" => "WEST",
                    "e" => "EAST",
                    "c" => "CENTRAL",
                    _ => ""
                };
            }

            var shardNum = parts.Last().TrimStart('0');
            if (string.IsNullOrEmpty(shardNum)) shardNum = "0";

            return $"{region} {zone} {shardNum}".Replace("  ", " ").Trim();
        }
    }

    public class SessionStartProcessor : ILogEventProcessor
    {
        private readonly IMessenger _messenger;
        public SessionStartProcessor(IMessenger messenger) => _messenger = messenger;
        
        public bool CanProcess(string line) => line.Contains("Log started on");
        
        public GameEvent? Process(string line)
        {
            _messenger.Send(new ClearFeedsMessage());
            return null;
        }
    }
}
