using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Models;
using Olympus.Services.Party;
using Xunit;

namespace Olympus.Tests.Services.Party;

/// <summary>
/// Unit tests for ShieldTrackingService.
/// Uses mocked IObjectTable and IPartyList with zero members so Update() leaves
/// all dictionaries empty. Tests exercise all public query methods via their
/// empty-state and guard-rail behavior, plus static helper math.
/// </summary>
public class ShieldTrackingServiceTests
{
    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static ShieldTrackingService CreateService()
    {
        var objectTable = new Mock<IObjectTable>();
        objectTable.Setup(x => x.LocalPlayer).Returns((IPlayerCharacter?)null);

        var partyList = new Mock<IPartyList>();
        partyList.Setup(x => x.Length).Returns(0);
        partyList.Setup(x => x.GetEnumerator())
                 .Returns(Enumerable.Empty<IPartyMember>().GetEnumerator());

        var log = new Mock<IPluginLog>();

        return new ShieldTrackingService(objectTable.Object, partyList.Object, log.Object);
    }

    // -------------------------------------------------------------------------
    // GetShields
    // -------------------------------------------------------------------------

    [Fact]
    public void GetShields_UnknownTarget_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetShields(99u);

        // Assert
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetMitigations
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMitigations_UnknownTarget_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetMitigations(99u);

        // Assert
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetTotalShieldValue
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTotalShieldValue_UnknownTarget_ReturnsZero()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetTotalShieldValue(99u);

        // Assert
        Assert.Equal(0, result);
    }

    // -------------------------------------------------------------------------
    // GetCombinedMitigation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetCombinedMitigation_UnknownTarget_ReturnsZero()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetCombinedMitigation(99u);

        // Assert
        Assert.Equal(0f, result);
    }

    // -------------------------------------------------------------------------
    // HasAnyShield
    // -------------------------------------------------------------------------

    [Fact]
    public void HasAnyShield_UnknownTarget_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.HasAnyShield(99u);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // HasShield
    // -------------------------------------------------------------------------

    [Fact]
    public void HasShield_UnknownTarget_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.HasShield(99u, ShieldStatusIds.DivineBenison);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // HasAnyMitigation
    // -------------------------------------------------------------------------

    [Fact]
    public void HasAnyMitigation_UnknownTarget_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.HasAnyMitigation(99u);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // IsInvulnerable
    // -------------------------------------------------------------------------

    [Fact]
    public void IsInvulnerable_UnknownTarget_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsInvulnerable(99u);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetEffectiveHp
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEffectiveHp_UnknownTarget_ReturnsCurrentHp()
    {
        // Arrange
        var service = CreateService();

        // Act — no shield data, so effective HP == current HP
        var result = service.GetEffectiveHp(99u, 50000u);

        // Assert
        Assert.Equal(50000u, result);
    }

    // -------------------------------------------------------------------------
    // GetEffectiveHpWithPending
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEffectiveHpWithPending_UnknownTarget_ReturnsCurrentHpPlusPending()
    {
        // Arrange
        var service = CreateService();

        // Act — currentHp=1000, pending=500, no shield → 1500
        var result = service.GetEffectiveHpWithPending(99u, 1000u, 500);

        // Assert
        Assert.Equal(1500u, result);
    }

    [Fact]
    public void GetEffectiveHpWithPending_NegativePending_ClampsToZero()
    {
        // Arrange
        var service = CreateService();

        // Act — currentHp=100, pendingHeals=-200, no shield → Math.Max(0, 100 + (-200) + 0) = 0
        var result = service.GetEffectiveHpWithPending(99u, 100u, -200);

        // Assert
        Assert.Equal(0u, result);
    }

    // -------------------------------------------------------------------------
    // GetAllShields / GetAllMitigations
    // -------------------------------------------------------------------------

    [Fact]
    public void GetAllShields_EmptyState_ReturnsEmptyDictionary()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAllShields();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllMitigations_EmptyState_ReturnsEmptyDictionary()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAllMitigations();

        // Assert
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Clear
    // -------------------------------------------------------------------------

    [Fact]
    public void Clear_AlreadyEmpty_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert — must not throw
        var ex = Record.Exception(() => service.Clear());
        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // Update — no-op with empty party and no local player
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_NoPartyNoLocalPlayer_LeavesStateEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Update();

        // Assert — all dictionaries remain empty
        Assert.Empty(service.GetAllShields());
        Assert.Empty(service.GetAllMitigations());
    }

    // -------------------------------------------------------------------------
    // MitigationValues static math
    // -------------------------------------------------------------------------

    [Fact]
    public void MitigationValues_RampartPlusSentinel_IsMultiplicative()
    {
        // 20% + 30% multiplicative = 1 - (0.80 * 0.70) = 0.44
        var rampart  = MitigationValues.GetMitigationPercent(MitigationStatusIds.Rampart);   // 0.20
        var sentinel = MitigationValues.GetMitigationPercent(MitigationStatusIds.Sentinel);  // 0.30
        var combined = 1f - (1f - rampart) * (1f - sentinel);
        Assert.Equal(0.44f, combined, precision: 5);
    }

    [Fact]
    public void MitigationValues_GetMitigationPercent_Rampart_ReturnsPointTwo()
    {
        var result = MitigationValues.GetMitigationPercent(MitigationStatusIds.Rampart);
        Assert.Equal(0.20f, result, precision: 5);
    }

    [Fact]
    public void MitigationValues_GetMitigationPercent_UnknownStatus_ReturnsZero()
    {
        var result = MitigationValues.GetMitigationPercent(999999u);
        Assert.Equal(0f, result);
    }

    [Fact]
    public void MitigationValues_GetMitigationPercent_HallowedGround_ReturnsOne()
    {
        // Invulnerability = 100% mitigation
        var result = MitigationValues.GetMitigationPercent(MitigationStatusIds.HallowedGround);
        Assert.Equal(1.00f, result, precision: 5);
    }
}
