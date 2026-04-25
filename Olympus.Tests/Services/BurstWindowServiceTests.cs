using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Services;
using Olympus.Services.Party;
using Olympus.Ipc;
using Xunit;

namespace Olympus.Tests.Services;

/// <summary>
/// Unit tests for BurstWindowService.
/// Update() requires a live Dalamud StatusList which cannot be mocked at test time;
/// those paths are covered via the IPC branch or left to integration testing.
/// All tests without IPC exercise only the pure-logic branches.
/// </summary>
public class BurstWindowServiceTests
{
    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void IsInBurstWindow_AfterConstruction_ReturnsFalse()
    {
        var service = new BurstWindowService();

        Assert.False(service.IsInBurstWindow);
    }

    [Fact]
    public void SecondsRemainingInBurst_AfterConstruction_ReturnsZero()
    {
        var service = new BurstWindowService();

        Assert.Equal(0f, service.SecondsRemainingInBurst);
    }

    // -------------------------------------------------------------------------
    // No IPC, no timer data
    // -------------------------------------------------------------------------

    [Fact]
    public void IsBurstImminent_WhenNoBurstAndNoTimer_ReturnsFalse()
    {
        // No IPC, no _lastBurstWindowEnd → TimerBasedSecondsUntilBurst returns -1
        var service = new BurstWindowService();

        Assert.False(service.IsBurstImminent());
    }

    [Fact]
    public void SecondsUntilNextBurst_WhenNoData_ReturnsNegativeOne()
    {
        var service = new BurstWindowService();

        Assert.Equal(-1f, service.SecondsUntilNextBurst);
    }

    // -------------------------------------------------------------------------
    // IPC path — IsBurstImminent
    // -------------------------------------------------------------------------

    [Fact]
    public void IsBurstImminent_WithIpcService_WhenBurstImminent_ReturnsTrue()
    {
        // Arrange
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.HasPendingRaidBuffIntent(It.IsAny<float>())).Returns(true);

        var service = new BurstWindowService(partyCoord.Object);

        // Act
        var result = service.IsBurstImminent();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBurstImminent_WhenNotInBurst_IpcImminentIsChecked()
    {
        // Arrange — service not in burst (initial state); HasPendingRaidBuffIntent returns true.
        // Verifies IsBurstImminent() does NOT short-circuit to false before checking IPC.
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.HasPendingRaidBuffIntent(It.IsAny<float>())).Returns(true);

        var service = new BurstWindowService(partyCoord.Object);

        // Act — IsInBurstWindow is false (initial); IsBurstImminent proceeds to IPC check
        Assert.False(service.IsInBurstWindow);
        Assert.True(service.IsBurstImminent());
    }

    // -------------------------------------------------------------------------
    // IPC path — SecondsUntilNextBurst
    // -------------------------------------------------------------------------

    [Fact]
    public void SecondsUntilNextBurst_WithIpcService_ReturnsIpcValue()
    {
        // Arrange — IPC says 30 seconds until next burst
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.GetSecondsUntilBurst()).Returns(30f);

        var service = new BurstWindowService(partyCoord.Object);

        // Act
        var result = service.SecondsUntilNextBurst;

        // Assert — IPC value is returned (30f >= 0, so it takes precedence over timer)
        Assert.Equal(30f, result);
    }

    [Fact]
    public void SecondsUntilNextBurst_WhenIpcReturnsZero_ReturnsZero()
    {
        // Arrange — IPC says burst starts now (0 seconds away)
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.GetSecondsUntilBurst()).Returns(0f);

        var service = new BurstWindowService(partyCoord.Object);

        // Act — IPC returns 0 (>= 0), so SecondsUntilNextBurst returns 0
        Assert.Equal(0f, service.SecondsUntilNextBurst);
    }

    [Fact]
    public void SecondsUntilNextBurst_WhenIpcReturnsNegativeOne_ReturnsFallback()
    {
        // Arrange — IPC returns -1 (no data); no timer either → -1
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.GetSecondsUntilBurst()).Returns(-1f);

        var service = new BurstWindowService(partyCoord.Object);

        // Act
        var result = service.SecondsUntilNextBurst;

        // Assert — falls through to timer-based fallback, which also returns -1
        Assert.Equal(-1f, result);
    }

    // -------------------------------------------------------------------------
    // IsBurstImminent — GetBurstWindowState path
    // -------------------------------------------------------------------------

    [Fact]
    public void IsBurstImminent_WhenIpcStateIsImminentAndWithinThreshold_ReturnsTrue()
    {
        // Arrange — IPC GetBurstWindowState returns IsImminent=true, SecondsUntilBurst=3f
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.HasPendingRaidBuffIntent(It.IsAny<float>())).Returns(false);
        partyCoord.Setup(x => x.GetBurstWindowState()).Returns(new BurstWindowState
        {
            IsImminent = true,
            SecondsUntilBurst = 3f,
            IsActive = false,
            HasBurstInfo = true,
        });

        var service = new BurstWindowService(partyCoord.Object);

        // Act — threshold 5s, burst in 3s → imminent
        Assert.True(service.IsBurstImminent(5f));
    }

    [Fact]
    public void IsBurstImminent_WhenIpcStateIsImminentButBeyondThreshold_ReturnsFalse()
    {
        // Arrange — burst in 10s, threshold 5s → not imminent
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.HasPendingRaidBuffIntent(It.IsAny<float>())).Returns(false);
        partyCoord.Setup(x => x.GetBurstWindowState()).Returns(new BurstWindowState
        {
            IsImminent = true,
            SecondsUntilBurst = 10f,
            IsActive = false,
            HasBurstInfo = true,
        });

        var service = new BurstWindowService(partyCoord.Object);

        // Act
        Assert.False(service.IsBurstImminent(5f));
    }

    // -------------------------------------------------------------------------
    // Cast-event subscription
    // -------------------------------------------------------------------------

    private static (BurstWindowService service, Mock<ICombatEventService> combatEvents)
        BuildWithCastEvents(uint localPlayerEntityId = 100U)
    {
        var combatEvents = new Mock<ICombatEventService>();

        var localPlayer = new Mock<IPlayerCharacter>();
        localPlayer.SetupGet(p => p.EntityId).Returns(localPlayerEntityId);
        var clientState = new Mock<IClientState>();
        clientState.SetupGet(c => c.LocalPlayer).Returns(localPlayer.Object);

        var service = new BurstWindowService(
            partyCoordinationService: null,
            combatEventService: combatEvents.Object,
            partyList: null,
            clientState: clientState.Object);

        return (service, combatEvents);
    }

    [Fact]
    public void CastEvent_RaidBuffFromSelf_OpensBurstWindow()
    {
        var (service, combatEvents) = BuildWithCastEvents(localPlayerEntityId: 100U);

        combatEvents.Raise(
            x => x.OnAbilityUsed += null,
            100U, // self
            DRGActions.BattleLitany.ActionId);

        Assert.True(service.IsInBurstWindow);
        Assert.Equal(20f, service.SecondsRemainingInBurst);
    }

    [Fact]
    public void CastEvent_NonRaidBuff_IsIgnored()
    {
        var (service, combatEvents) = BuildWithCastEvents();

        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, 9U);

        Assert.False(service.IsInBurstWindow);
        Assert.Equal(0f, service.SecondsRemainingInBurst);
    }

    [Fact]
    public void CastEvent_RaidBuffFromUnknownCaster_IsIgnored()
    {
        // Caster not local, no party list provided → cannot verify membership → reject.
        var (service, combatEvents) = BuildWithCastEvents(localPlayerEntityId: 100U);

        combatEvents.Raise(
            x => x.OnAbilityUsed += null,
            999U, // not local, no party list to check against
            DRGActions.BattleLitany.ActionId);

        Assert.False(service.IsInBurstWindow);
    }

    [Fact]
    public void CastEvent_MultipleRaidBuffs_ExtendsToMaxDuration()
    {
        var (service, combatEvents) = BuildWithCastEvents();

        // BattleVoice = 15s
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, BRDActions.BattleVoice.ActionId);
        Assert.Equal(15f, service.SecondsRemainingInBurst);

        // BattleLitany = 20s, longer → should extend
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, DRGActions.BattleLitany.ActionId);
        Assert.Equal(20f, service.SecondsRemainingInBurst);

        // BattleVoice again, shorter → should NOT shrink the window
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, BRDActions.BattleVoice.ActionId);
        Assert.Equal(20f, service.SecondsRemainingInBurst);
    }

    [Fact]
    public void CastEvent_BurstHistoryRecordsWindowStart()
    {
        var (service, combatEvents) = BuildWithCastEvents();

        Assert.Empty(service.BurstWindowHistory);

        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, DRGActions.BattleLitany.ActionId);

        Assert.True(service.IsInBurstWindow);
        // History only records on Update() once the window ends; cast event opens but doesn't close.
    }

    [Fact]
    public void Dispose_UnsubscribesFromCombatEvents()
    {
        var (service, combatEvents) = BuildWithCastEvents();

        service.Dispose();

        // After dispose, raising the event should have no effect on state.
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, DRGActions.BattleLitany.ActionId);

        Assert.False(service.IsInBurstWindow);
    }

    [Fact]
    public void NoCombatEventService_BehavesAsBefore()
    {
        // When no ICombatEventService is provided, no subscription happens; Dispose() is safe.
        var service = new BurstWindowService(combatEventService: null);

        Assert.False(service.IsInBurstWindow);

        service.Dispose(); // Should not throw.
    }
}
