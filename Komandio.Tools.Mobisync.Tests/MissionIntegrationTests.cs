using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Components.Contracts;
using Komandio.Tools.Mobisync.Services;
using Moq;
using Xunit;

namespace Komandio.Tools.Mobisync.Tests;

public class MissionIntegrationTests
{
    [Fact]
    public void LogProcessing_ShouldProduceCorrectContractState()
    {
        // Arrange
        var messenger = new StrongReferenceMessenger();
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.LocationMapping).Returns(new Dictionary<string, string>());
        
        var monitor = new LogMonitorService(settingsMock.Object, messenger);
        var viewModel = new ContractsViewModel(messenger);
        
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "reference.log");
        var lines = File.ReadAllLines(logPath);

        // Act - Process the log synchronously
        monitor.ProcessSingleLine("<2026-02-06T12:38:05.357Z> [Notice] <MissionEnded> Received MissionEnded push message for: mission_id 2edcff7c-fe60-473f-98ae-c4205d796d93 - mission_state MISSION_STATE_FAILED [Team_GameServices][Missions]");
        Assert.Equal(1, viewModel.FailedCount);

        foreach (var line in lines)
        {
            monitor.ProcessSingleLine(line);
        }

        // Assert
        Assert.True(viewModel.CompletedCount >= 4, $"Expected at least 4 unique completed missions, but found {viewModel.CompletedCount}");
        
        // Total missions in this log segment including successes and technical ends
        Assert.True(viewModel.HistoryMissions.Count >= 5, $"Expected at least 5 history items, but found {viewModel.HistoryMissions.Count}");
        
        // Verify specifically sanitized names in History
        var historyDetails = string.Join(", ", viewModel.HistoryMissions.Select(m => $"'{m.Name}' - '{m.CurrentObjectiveText}'"));
        var hasSupplyThief = viewModel.HistoryMissions.Any(m => m.Name == "ALLIANCE AID: SUPPLY THIEF");
        Assert.True(hasSupplyThief, $"Did not find sanitized 'ALLIANCE AID: SUPPLY THIEF' in history. Found: {historyDetails}");

        // Verify at least one Supply Thief has its text
        var supplyThieves = viewModel.HistoryMissions.Where(m => m.Name == "ALLIANCE AID: SUPPLY THIEF").ToList();
        Assert.True(supplyThieves.Any(m => m.CurrentObjectiveText != "ACTIVE"), "All Supply Thief missions were stuck at ACTIVE");

        var completed = viewModel.HistoryMissions.Where(m => m.Status == "SUCCESS").ToList();
        Assert.True(completed.Count >= 4, $"Expected 4 success missions, but found {completed.Count}");

        // Verify no objectives are stuck at placeholders
        foreach(var mission in viewModel.ActiveMissions.Concat(viewModel.HistoryMissions))
        {
            Assert.DoesNotContain("INITIALIZING", mission.CurrentObjectiveText);
        }
    }
}