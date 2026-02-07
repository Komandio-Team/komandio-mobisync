using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;
using Serilog;

namespace Komandio.Tools.Mobisync.Components.Contracts;

public partial class ContractsViewModel : ObservableObject
{
    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _completedCount;
    [ObservableProperty] private int _failedCount;

    public ObservableCollection<MissionModel> ActiveMissions { get; } = new();
    public ObservableCollection<MissionModel> HistoryMissions { get; } = new();

    private string? _pendingFocusObjectiveId;

    public ContractsViewModel(IMessenger? messenger = null)
    {
        var msg = messenger ?? WeakReferenceMessenger.Default;
        msg.Register<GameEvent>(this, (r, m) =>
        {
            var isSilent = m is SilentStateUpdateEvent;
            var actualEvent = m is SilentStateUpdateEvent silent ? silent.WrappedEvent : m;

            if (actualEvent is MissionAcceptedEvent accepted) HandleAccepted(accepted, isSilent);
            else if (actualEvent is ObjectiveUpdateEvent update) HandleObjectiveUpdate(update, isSilent);
            else if (actualEvent is MissionEndedEvent ended) HandleMissionEnded(ended, isSilent);
        });

        msg.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            Log.Debug("ContractsVM: Clearing all feeds");
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => {
                    ActiveMissions.Clear();
                    HistoryMissions.Clear();
                    ActiveCount = 0;
                    CompletedCount = 0;
                    FailedCount = 0;
                    _pendingFocusObjectiveId = null;
                });
            }
        });
    }

    private void UpdateTrackingStatus(MissionModel target)
    {
        bool changed = false;
        foreach (var m in ActiveMissions) 
        {
            if (m != target && m.IsFollowed)
            {
                m.Status = "ACCEPTED";
                m.IsFollowed = false;
                changed = true;
            }
        }
        
        if (!target.IsFollowed)
        {
            target.IsFollowed = true;
            target.Status = "TRACKING";
            changed = true;
        }

        if (changed)
        {
            int currentIndex = ActiveMissions.IndexOf(target);
            if (currentIndex > 0)
            {
                ActiveMissions.Move(currentIndex, 0);
            }
        }
    }

    private void HandleAccepted(MissionAcceptedEvent e, bool isSilent)
    {
        void Action()
        {
            var friendlyName = FormatContractName(e.ContractName);
            var mission = ActiveMissions.FirstOrDefault(m => string.Equals(m.Id, e.MissionId, StringComparison.OrdinalIgnoreCase));

            if (mission != null)
            {
                if (IsTechnicalName(mission.Name) && !IsTechnicalName(friendlyName))
                {
                    mission.Name = friendlyName;
                }
                UpdateTrackingStatus(mission);
                return;
            }

            mission = new MissionModel
            {
                Id = e.MissionId,
                Name = friendlyName,
                AcceptedAt = e.Timestamp,
                Status = "ACCEPTED"
            };

            ActiveMissions.Insert(0, mission);
            UpdateTrackingStatus(mission);
            ActiveCount = ActiveMissions.Count;
        }

        if (Application.Current != null) Application.Current.Dispatcher.Invoke(Action);
        else Action();
    }

    private void HandleObjectiveUpdate(ObjectiveUpdateEvent e, bool isSilent)
    {
        void Action()
        {
            // 1. Authoritative Lookup (Match by MissionId OR objective contents)
            var mission = !string.IsNullOrEmpty(e.MissionId) 
                ? ActiveMissions.FirstOrDefault(m => string.Equals(m.Id, e.MissionId, StringComparison.OrdinalIgnoreCase))
                : ActiveMissions.FirstOrDefault(m => m.Objectives.Any(o => string.Equals(o.Id, e.ObjectiveId, StringComparison.OrdinalIgnoreCase)));

            if (mission == null) 
            {
                if (string.IsNullOrEmpty(e.MissionId)) _pendingFocusObjectiveId = e.ObjectiveId;
                return;
            }

            string? incomingText = e.Text;
            bool isTechnical = incomingText?.Contains("~") == true;
            bool isDataValid = !string.IsNullOrEmpty(incomingText) && !isTechnical;

            // 2. Authoritative Tracking (Follow the HUD)
            bool isFocusSignal = string.IsNullOrEmpty(e.MissionId) || string.Equals(e.ObjectiveId, _pendingFocusObjectiveId, StringComparison.OrdinalIgnoreCase);
            if (isFocusSignal || isDataValid)
            {
                UpdateTrackingStatus(mission);
                if (isFocusSignal) _pendingFocusObjectiveId = null;
            }

            if (isDataValid && incomingText != null)
            {
                var cleanText = incomingText.ToUpper().TrimEnd(':').Trim();
                if (cleanText != "ACTIVE" && cleanText != "INITIALIZING" && cleanText != "WAITING FOR DATA")
                {
                    mission.CurrentObjectiveText = cleanText;
                }
            }

            // 3. Objective Management (STRICT DE-DUPLICATION)
            if (!string.IsNullOrEmpty(e.ObjectiveId))
            {
                // Find by ID OR by Text to prevent duplicate lines in the list
                var obj = mission.Objectives.FirstOrDefault(o => 
                    string.Equals(o.Id, e.ObjectiveId, StringComparison.OrdinalIgnoreCase) ||
                    (isDataValid && string.Equals(o.Text, incomingText?.ToUpper(), StringComparison.OrdinalIgnoreCase)));

                if (obj == null)
                {
                    if (isDataValid && incomingText != null)
                    {
                        mission.Objectives.Add(new MissionObjective { Id = e.ObjectiveId, Text = incomingText.ToUpper(), Status = e.State });
                    }
                }
                else
                {
                    // Merge/Update existing
                    if (isDataValid && incomingText != null) obj.Text = incomingText.ToUpper();
                    if (e.State != "TRACKED" && e.State != "UNTRACKED") obj.Status = e.State;
                }
            }
        }

        if (Application.Current != null) Application.Current.Dispatcher.Invoke(Action);
        else Action();
    }

    private void HandleMissionEnded(MissionEndedEvent e, bool isSilent)
    {
        void Action()
        {
            if (HistoryMissions.Any(m => string.Equals(m.Id, e.MissionId, StringComparison.OrdinalIgnoreCase)))
                return;

            var mission = ActiveMissions.FirstOrDefault(m => string.Equals(m.Id, e.MissionId, StringComparison.OrdinalIgnoreCase));
            if (mission == null) 
            {
                mission = new MissionModel { Id = e.MissionId, Name = "UNKNOWN CONTRACT" };
            }
            else
            {
                ActiveMissions.Remove(mission);
            }
            
            var state = e.State.ToUpper();
            if (state.Contains("COMPLETED") || state.Contains("SUCCEEDED"))
            {
                mission.Status = "SUCCESS";
                CompletedCount++;
            }
            else
            {
                mission.Status = "FAILED";
                FailedCount++;
            }

            HistoryMissions.Insert(0, mission);
            ActiveCount = ActiveMissions.Count;
        }

        if (Application.Current != null) Application.Current.Dispatcher.Invoke(Action);
        else Action();
    }

    private bool IsTechnicalName(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        return name.Contains('_') || (!name.Contains(' ') && System.Text.RegularExpressions.Regex.IsMatch(name, @"[a-z][A-Z]"));
    }

    private string FormatContractName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "UNKNOWN CONTRACT";
        var name = raw.Trim();

        string[] enginePrefixes = { "Contract Accepted:", "Contract Complete:", "Contract Available:", "New Objective:", "Objective Complete:" };
        foreach(var p in enginePrefixes) {
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(p.Length).Trim();
            }
        }

        name = name.Replace("_", " ");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2");
        var result = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").ToUpper().Trim();
        return result.TrimStart(':').TrimStart('-').TrimEnd(':').TrimEnd('.').Trim();
    }
}