using System.Windows.Media;

namespace Komandio.Tools.Mobisync.Models;

public static class EventMapper
{
    public static LogEventViewModel? Map(GameEvent gameEvent)
    {
        if (gameEvent is DynamicEvent de)
        {
            string title = de.Rule.TitleTemplate;
            string desc = de.Rule.DescriptionTemplate;
            for (int i = 1; i < de.Match.Groups.Count; i++)
            {
                title = title.Replace($"{{{i}}}", de.Match.Groups[i].Value);
                desc = desc.Replace($"{{{i}}}", de.Match.Groups[i].Value);
            }

            var iconName = de.Rule.Icon;
            if (iconName == "Activity24") iconName = "Heartpulse24";

            return new LogEventViewModel
            {
                Title = title,
                Description = desc,
                Timestamp = de.Timestamp,
                Icon = iconName,
                Category = de.Rule.Category,
                IconColor = de.Rule.Category == "KILLS" ? Brushes.Red : Brushes.DeepSkyBlue
            };
        }

        return gameEvent switch
        {
            PlayerLoginEvent l => new LogEventViewModel { Title = "Pilot Identified", Description = $"Authenticated as {l.Handle}", Timestamp = l.Timestamp, Icon = "Person24", Category = "SYSTEM", IconColor = Brushes.CornflowerBlue },
            JurisdictionEvent j => new LogEventViewModel { Title = "Jurisdiction Change", Description = j.Name, Timestamp = j.Timestamp, Icon = "Location24", Category = "AREAS", IconColor = Brushes.SeaGreen },
            ArmisticeEvent a => new LogEventViewModel { Title = "Security Alert", Description = a.IsEntering ? "Entering Armistice" : "Leaving Armistice", Timestamp = a.Timestamp, Icon = "Shield24", Category = "AREAS", IconColor = Brushes.Cyan },
            CombatDeathEvent k => new LogEventViewModel { Title = "Combat Result", Description = $"{k.Killer} eliminated {k.Victim}", Timestamp = k.Timestamp, Icon = "Dismiss24", Category = "KILLS", IconColor = Brushes.Red },
            LocationChangeEvent loc => new LogEventViewModel { Title = "Location Update", Description = $"Area identified: {loc.LocationId}", Timestamp = loc.Timestamp, Icon = "Map24", Category = "AREAS", IconColor = Brushes.DeepSkyBlue },
            MedicalAlertEvent med => new LogEventViewModel { Title = "Medical Alert", Description = $"CRITICAL: Player started {med.Type}!", Timestamp = med.Timestamp, Icon = "Warning24", Category = "SYSTEM", IconColor = Brushes.Red },
            QuantumEvent q => new LogEventViewModel { Title = "Quantum Link", Description = $"Quantum travel {q.State}", Timestamp = q.Timestamp, Icon = "Rocket24", Category = "SYSTEM", IconColor = Brushes.Cyan },
            DeathSpawnEvent ds => new LogEventViewModel { Title = ds.IsSpawn ? "Life Support" : "Vital Signs Lost", Description = ds.IsSpawn ? "New actor clone spawned" : "Character killed in action", Timestamp = ds.Timestamp, Icon = ds.IsSpawn ? "Heart24" : "Dismiss24", Category = "SYSTEM", IconColor = ds.IsSpawn ? Brushes.Lime : Brushes.OrangeRed },
            VehicleEvent s => new LogEventViewModel { Title = $"Vehicle {s.Action}", Description = s.VehicleName, Timestamp = s.Timestamp, Icon = "VehicleBus24", Category = "VEHICLES", IconColor = Brushes.Gold },
            MissionAcceptedEvent accepted => new LogEventViewModel
            {
                Title = "CONTRACT ACCEPTED",
                Description = accepted.ContractName.Replace("_", " ").ToUpper(),
                Timestamp = accepted.Timestamp,
                Category = "CONTRACTS",
                Icon = "ClipboardTask24",
                IconColor = GetFrozenBrush("#5E5CE6")
            },
            ObjectiveUpdateEvent update => new LogEventViewModel
            {
                Title = update.State == "COMPLETED" ? "OBJECTIVE DONE" : "NEW OBJECTIVE",
                Description = string.IsNullOrEmpty(update.Text) ? "TECHNICAL UPDATE" : update.Text.ToUpper(),
                Timestamp = update.Timestamp,
                Category = "CONTRACTS",
                Icon = update.State == "COMPLETED" ? "CheckmarkCircle24" : "ArrowCircleRight24",
                IconColor = GetFrozenBrush(update.State == "COMPLETED" ? "#4ADE80" : "#5E5CE6")
            },
            MissionEndedEvent ended => new LogEventViewModel
            {
                Title = ended.State.Contains("FAILED") ? "CONTRACT FAILED" : "CONTRACT COMPLETE",
                Description = ended.State.Replace("MISSION_STATE_", ""),
                Timestamp = ended.Timestamp,
                Category = "CONTRACTS",
                Icon = ended.State.Contains("FAILED") ? "DismissCircle24" : "CheckmarkCircle24",
                IconColor = GetFrozenBrush(ended.State.Contains("FAILED") ? "#F87171" : "#4ADE80")
            },
            _ => null
        };
    }

    private static Brush GetFrozenBrush(string hex)
    {
        var brush = (Brush)new BrushConverter().ConvertFrom(hex)!;
        brush.Freeze();
        return brush;
    }
}