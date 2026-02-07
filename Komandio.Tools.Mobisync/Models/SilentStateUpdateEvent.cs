namespace Komandio.Tools.Mobisync.Models;

/// <summary>
/// A wrapper for events that should only update internal state but NOT appear in UI feeds (Dashboard, Areas, Kills, etc).
/// </summary>
public record SilentStateUpdateEvent(GameEvent WrappedEvent) : GameEvent(WrappedEvent.Timestamp);
