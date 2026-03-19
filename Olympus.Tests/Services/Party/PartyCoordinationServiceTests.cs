using System;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Services.Party;
using Xunit;

namespace Olympus.Tests.Services.Party;

/// <summary>
/// Unit tests for PartyCoordinationService.
/// Only pure-logic methods that do not require live Dalamud IPC are exercised.
/// All tests use EnablePartyCoordination = true and EnableCooldownCoordination = true
/// so the guard clauses inside the methods are satisfied.
/// </summary>
public class PartyCoordinationServiceTests
{
    // -------------------------------------------------------------------------
    // Setup helper
    // -------------------------------------------------------------------------

    private static PartyCoordinationService CreateService(
        bool enableCoordination = true,
        int expiryMs = 3000,
        bool enableHealerRoleCoord = true,
        Func<DateTime>? clock = null)
    {
        var config = new PartyCoordinationConfig
        {
            EnablePartyCoordination = enableCoordination,
            EnableCooldownCoordination = true,
            EnableRaidBuffCoordination = true,
            HealReservationExpiryMs = expiryMs,
            EnableHealerRoleCoordination = enableHealerRoleCoord,
        };
        var log = new Mock<IPluginLog>();
        return new PartyCoordinationService(config, log.Object, clock);
    }

    // -------------------------------------------------------------------------
    // WasPartyMitigationUsedRecently — empty state
    // -------------------------------------------------------------------------

    [Fact]
    public void WasPartyMitigationUsedRecently_WhenNoRecentUsage_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act — no cooldowns tracked yet
        var result = service.WasPartyMitigationUsedRecently(withinSeconds: 3f);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // WasPartyMitigationUsedRecently — after HandleRemoteCooldownUsed
    // -------------------------------------------------------------------------

    [Fact]
    public void WasPartyMitigationUsedRecently_WhenMitigationUsedRecently_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Temperance (16536) is a party mitigation in CoordinatedCooldowns.PartyMitigations.
        // The message must come from a different instance ID so it is not filtered.
        var remoteInstanceId = Guid.NewGuid();
        var message = new CooldownUsedMessage(remoteInstanceId, ActionIds.Temperance, recastTimeMs: 120_000);

        // Act
        service.HandleRemoteCooldownUsed(message);
        var result = service.WasPartyMitigationUsedRecently(withinSeconds: 3f);

        // Assert — zero-second window must never satisfy "within 0 seconds"
        Assert.False(service.WasPartyMitigationUsedRecently(withinSeconds: 0f));
        Assert.True(result);
    }

    [Fact]
    public void WasPartyMitigationUsedRecently_WhenCoordinationDisabled_ReturnsFalse()
    {
        // Arrange — coordination is off; all guard clauses short-circuit to false
        var service = CreateService(enableCoordination: false);
        var remoteInstanceId = Guid.NewGuid();
        var message = new CooldownUsedMessage(remoteInstanceId, ActionIds.Temperance, recastTimeMs: 120_000);
        service.HandleRemoteCooldownUsed(message);

        // Act
        var result = service.WasPartyMitigationUsedRecently(withinSeconds: 3f);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetBurstWindowState — fresh service
    // -------------------------------------------------------------------------

    [Fact]
    public void GetBurstWindowState_WhenNoBurstAnnounced_ReturnsNoInfoState()
    {
        // Arrange — no remote DPS instances registered, so HasRemoteDps is false.
        // GetBurstWindowState returns BurstWindowState.NoInfo immediately.
        var service = CreateService();

        // Act
        var state = service.GetBurstWindowState();

        // Assert
        Assert.False(state.IsActive);
        Assert.False(state.IsImminent);
        Assert.False(state.HasBurstInfo);
    }

    // -------------------------------------------------------------------------
    // IsTargetReservedByOther — no reservations
    // -------------------------------------------------------------------------

    [Fact]
    public void IsTargetReservedByOther_WhenNoReservations_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsTargetReservedByOther(entityId: 100u);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // IsTargetReservedByOther — after HandleRemoteHealIntent
    // -------------------------------------------------------------------------

    [Fact]
    public void IsTargetReservedByOther_AfterRemoteHealIntent_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Message must come from a different instance so the self-filter is bypassed.
        var remoteInstanceId = Guid.NewGuid();
        var message = new HealIntentMessage(
            instanceId: remoteInstanceId,
            targetEntityId: 100u,
            estimatedHealAmount: 5000,
            actionId: 1u,
            castTimeMs: 0);

        // Act
        service.HandleRemoteHealIntent(message);
        var result = service.IsTargetReservedByOther(entityId: 100u);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTargetReservedByOther_WhenReservationExpired_ReturnsFalse()
    {
        // Arrange — expiry set to 1000ms; advance a controllable clock past expiry instead of sleeping
        var fakeTime = DateTime.UtcNow;
        var service = CreateService(expiryMs: 1000, clock: () => fakeTime);
        var remoteInstanceId = Guid.NewGuid();
        var message = new HealIntentMessage(
            instanceId: remoteInstanceId,
            targetEntityId: 0x1111,
            estimatedHealAmount: 5000,
            actionId: 1u,
            castTimeMs: 0);

        service.HandleRemoteHealIntent(message);

        // Advance clock 2 seconds past the 1000ms expiry window
        fakeTime = fakeTime.AddSeconds(2);

        // Act + Assert
        Assert.False(service.IsTargetReservedByOther(entityId: 0x1111));
    }

    [Fact]
    public void IsTargetReservedByOther_ForDifferentEntity_ReturnsFalse()
    {
        // Arrange — reservation is for entity 100, query is for entity 999
        var service = CreateService();

        var remoteInstanceId = Guid.NewGuid();
        var message = new HealIntentMessage(
            instanceId: remoteInstanceId,
            targetEntityId: 100u,
            estimatedHealAmount: 5000,
            actionId: 1u,
            castTimeMs: 0);

        service.HandleRemoteHealIntent(message);

        // Act
        var result = service.IsTargetReservedByOther(entityId: 999u);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetRemotePendingHealAmount — after HandleRemoteHealIntent
    // -------------------------------------------------------------------------

    [Fact]
    public void GetRemotePendingHealAmount_WhenNoReservations_ReturnsZero()
    {
        var service = CreateService();

        Assert.Equal(0, service.GetRemotePendingHealAmount(entityId: 100u));
    }

    [Fact]
    public void GetRemotePendingHealAmount_AfterRemoteHealIntent_ReturnsEstimatedAmount()
    {
        // Arrange
        var service = CreateService();

        var remoteInstanceId = Guid.NewGuid();
        var message = new HealIntentMessage(
            instanceId: remoteInstanceId,
            targetEntityId: 200u,
            estimatedHealAmount: 7500,
            actionId: 1u,
            castTimeMs: 0);

        service.HandleRemoteHealIntent(message);

        // Act
        var amount = service.GetRemotePendingHealAmount(entityId: 200u);

        // Assert
        Assert.Equal(7500, amount);
    }

    // -------------------------------------------------------------------------
    // IsPrimaryHealer — fresh service with no remote healers
    // -------------------------------------------------------------------------

    [Fact]
    public void IsPrimaryHealer_WhenNoRemoteHealers_ReturnsTrue()
    {
        // No competing remote healer roles registered → local instance is primary.
        var service = CreateService();

        Assert.True(service.IsPrimaryHealer);
    }

    [Fact]
    public void IsPrimaryHealer_WhenCoordinationDisabled_ReturnsTrue()
    {
        // When coordination is disabled, IsPrimaryHealer always returns true.
        var service = CreateService(enableCoordination: false);

        Assert.True(service.IsPrimaryHealer);
    }

    [Fact]
    public void IsPrimaryHealer_WhenHealerRoleCoordinationDisabled_ReturnsTrue()
    {
        // Arrange — party coordination enabled but healer role coord disabled
        var service = CreateService(enableHealerRoleCoord: false);

        // Act + Assert — disabled healer role coordination → local instance treated as primary
        Assert.True(service.IsPrimaryHealer);
    }

    // -------------------------------------------------------------------------
    // IsPartyCoordinationEnabled
    // -------------------------------------------------------------------------

    [Fact]
    public void IsPartyCoordinationEnabled_WhenConfigTrue_ReturnsTrue()
    {
        var service = CreateService(enableCoordination: true);

        Assert.True(service.IsPartyCoordinationEnabled);
    }

    [Fact]
    public void IsPartyCoordinationEnabled_WhenConfigFalse_ReturnsFalse()
    {
        var service = CreateService(enableCoordination: false);

        Assert.False(service.IsPartyCoordinationEnabled);
    }

    // -------------------------------------------------------------------------
    // RemoteInstanceCount — fresh service
    // -------------------------------------------------------------------------

    [Fact]
    public void RemoteInstanceCount_AfterConstruction_ReturnsZero()
    {
        var service = CreateService();

        Assert.Equal(0, service.RemoteInstanceCount);
    }

    // -------------------------------------------------------------------------
    // GetRemoteReservations — fresh service
    // -------------------------------------------------------------------------

    [Fact]
    public void GetRemoteReservations_WhenEmpty_ReturnsEmptyDictionary()
    {
        var service = CreateService();

        Assert.Empty(service.GetRemoteReservations());
    }

    // -------------------------------------------------------------------------
    // HandleRemoteCooldownUsed — ignores own messages
    // -------------------------------------------------------------------------

    [Fact]
    public void HandleRemoteCooldownUsed_OwnInstanceId_IsIgnored()
    {
        // Arrange — message has the same InstanceId as the service itself
        var service = CreateService();
        var ownId = service.InstanceId;
        var message = new CooldownUsedMessage(ownId, ActionIds.Temperance, recastTimeMs: 120_000);

        // Act
        service.HandleRemoteCooldownUsed(message);

        // Assert — own message is filtered; no cooldown tracked
        Assert.False(service.WasPartyMitigationUsedRecently(withinSeconds: 3f));
    }
}
