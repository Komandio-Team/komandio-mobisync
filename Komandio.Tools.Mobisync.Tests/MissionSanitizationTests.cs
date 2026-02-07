using System.Reflection;
using Komandio.Tools.Mobisync.Components.Contracts;
using Komandio.Tools.Mobisync.Services;
using Moq;
using Xunit;

namespace Komandio.Tools.Mobisync.Tests;

public class MissionSanitizationTests
{
    [Theory]
    [InlineData("CleanAir_Killship_Hard_3", "CLEAN AIR KILLSHIP HARD 3")]
    [InlineData("CleanAir_EscortShip_Easy_0", "CLEAN AIR ESCORT SHIP EASY 0")]
    [InlineData("TheCollector_Vehicle_Ground_Ursa_Medical", "THE COLLECTOR VEHICLE GROUND URSA MEDICAL")]
    [InlineData("Alliance Aid: Hauler Hunters", "ALLIANCE AID: HAULER HUNTERS")]
    [InlineData("  Alliance Aid: Supply Thief: ", "ALLIANCE AID: SUPPLY THIEF")]
    [InlineData("Stanton_4b_Clio", "STANTON 4B CLIO")]
    public void ContractName_ShouldBeSanitized(string raw, string expected)
    {
        // Arrange
        var settingsMock = new Mock<ISettingsService>();
        var viewModel = new ContractsViewModel();
        
        // Use reflection to access the private FormatContractName method
        var method = typeof(ContractsViewModel).GetMethod("FormatContractName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = (string)method!.Invoke(viewModel, [raw])!;
        
        // Assert
        Assert.Equal(expected, result);
    }
}
