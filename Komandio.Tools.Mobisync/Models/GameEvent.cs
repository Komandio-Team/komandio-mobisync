using System.Text.RegularExpressions;

namespace Komandio.Tools.Mobisync.Models;

public abstract record GameEvent(DateTime Timestamp);

public record PlayerLoginEvent(DateTime Timestamp, string Handle) : GameEvent(Timestamp);

public record BuildInfoEvent(DateTime Timestamp, string BuildNumber) : GameEvent(Timestamp);

public record JurisdictionEvent(DateTime Timestamp, string Name) : GameEvent(Timestamp);

public record ArmisticeEvent(DateTime Timestamp, bool IsEntering) : GameEvent(Timestamp);

public record SessionUptimeEvent(DateTime Timestamp, int Seconds) : GameEvent(Timestamp);

public record LocationChangeEvent(DateTime Timestamp, string LocationId) : GameEvent(Timestamp);

public record MedicalAlertEvent(DateTime Timestamp, string Type) : GameEvent(Timestamp);

public record QuantumEvent(DateTime Timestamp, string State) : GameEvent(Timestamp);

public record DeathSpawnEvent(DateTime Timestamp, bool IsSpawn) : GameEvent(Timestamp);

public record CombatDeathEvent(DateTime Timestamp, string Victim, string Killer, string Reason) : GameEvent(Timestamp);



public record VehicleEvent(DateTime Timestamp, string VehicleName, string Action) : GameEvent(Timestamp);







public record MissionAcceptedEvent(DateTime Timestamp, string MissionId, string ContractName) : GameEvent(Timestamp);







public record ObjectiveUpdateEvent(DateTime Timestamp, string MissionId, string ObjectiveId, string State, string? Text = null) : GameEvent(Timestamp);







public record MissionEndedEvent(DateTime Timestamp, string MissionId, string State) : GameEvent(Timestamp);







public record DynamicEvent(DateTime Timestamp, DynamicProcessorRule Rule, Match Match) : GameEvent(Timestamp);

public record HeartbeatEvent(DateTime Timestamp) : GameEvent(Timestamp);

public record CpuInfoEvent(DateTime Timestamp, string Name, int Cores) : GameEvent(Timestamp);
public record GpuInfoEvent(DateTime Timestamp, string Name, string Memory) : GameEvent(Timestamp);
public record MemoryInfoEvent(DateTime Timestamp, string Total, string Available) : GameEvent(Timestamp);
public record DisplayInfoEvent(DateTime Timestamp, string Resolution, string RefreshRate) : GameEvent(Timestamp);
public record PeripheralEvent(DateTime Timestamp, string DeviceName) : GameEvent(Timestamp);
public record NetworkIdentityEvent(DateTime Timestamp, string Shard, string Endpoint, string SessionId, string EnvId) : GameEvent(Timestamp);
public record AccountInfoEvent(DateTime Timestamp, string AccountId) : GameEvent(Timestamp);

public record ServerConnectionEvent(DateTime Timestamp, string Address) : GameEvent(Timestamp);


