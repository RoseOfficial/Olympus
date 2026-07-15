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
        var objectTable = new Mock<IObjectTable>();
        objectTable.SetupGet(o => o.LocalPlayer).Returns(localPlayer.Object);

        var service = new BurstWindowService(
            partyCoordinationService: null,
            combatEventService: combatEvents.Object,
            partyList: null,
            objectTable: objectTable.Object);

        return (service, combatEvents);
    }

    [Fact]
    public void CastEvent_RaidBuffFromSelf_OpensBurstWindow()
    {
        var (service, combatEvents) = BuildWithCastEvents(localPlayerEntityId: 100U);
        var player = new Mock<IPlayerCharacter>();

        // Build the snapshot first so OnAbilityUsed can identify the local player.
        service.Update(player.Object);

        combatEvents.Raise(
            x => x.OnAbilityUsed += null,
            100U, // self
            DRGActions.BattleLitany.ActionId);
        // Cast-event signal is merged into burst state on the next frame update.
        service.Update(player.Object);

        Assert.True(service.IsInBurstWindow);
        Assert.InRange(service.SecondsRemainingInBurst, 19f, 20f);
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
    public void CastEvent_RaidBuffBeforeFirstUpdate_IsIgnored()
    {
        // Verifies that party membership is enforced via the frame-thread snapshot, not by
        // enumerating IPartyList on the hook thread. A caster absent from the snapshot
        // (because Update has never run) is rejected, even for a party member entity ID.
        // Snapshot staleness is at most one frame by design.
        var (service, combatEvents) = BuildWithCastEvents(localPlayerEntityId: 100U);

        // Fire the event BEFORE any Update — snapshot is empty, so entity 200 is unknown.
        combatEvents.Raise(
            x => x.OnAbilityUsed += null,
            200U, // not in snapshot yet (local player is 100, and no Update has run)
            DRGActions.BattleLitany.ActionId);

        Assert.False(service.IsInBurstWindow);
    }

    [Fact]
    public void CastEvent_MultipleRaidBuffs_ExtendsToMaxDuration()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();

        // Build the snapshot before the first event so the caster is recognized.
        service.Update(player.Object);

        // BattleVoice = 15s
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, BRDActions.BattleVoice.ActionId);
        service.Update(player.Object);
        Assert.InRange(service.SecondsRemainingInBurst, 14f, 15f);

        // BattleLitany = 20s, longer → should extend
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, DRGActions.BattleLitany.ActionId);
        service.Update(player.Object);
        Assert.InRange(service.SecondsRemainingInBurst, 19f, 20f);

        // BattleVoice again, shorter → should NOT shrink the window
        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, BRDActions.BattleVoice.ActionId);
        service.Update(player.Object);
        Assert.InRange(service.SecondsRemainingInBurst, 19f, 20f);
    }

    [Fact]
    public void CastEvent_BurstHistoryRecordsWindowStart()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();

        Assert.Empty(service.BurstWindowHistory);

        // Build the snapshot first so OnAbilityUsed recognizes the local player.
        service.Update(player.Object);

        combatEvents.Raise(x => x.OnAbilityUsed += null, 100U, DRGActions.BattleLitany.ActionId);
        service.Update(player.Object);

        Assert.True(service.IsInBurstWindow);
        // History only records once the window actually ends; an open cast-event window
        // must not append an entry (regression guard for the near-zero-duration bug).
        Assert.Empty(service.BurstWindowHistory);
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

    // -------------------------------------------------------------------------
    // Synthetic burst cycle (solo dummy / pre-first-window)
    // -------------------------------------------------------------------------

    [Fact]
    public void SyntheticCycle_BeforeFirstWindow_ReportsSecondsUntilOpener()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(3f);

        service.Update(player.Object);

        Assert.False(service.IsInBurstWindow);
        // First synthetic window opens at 7.8s; at 3s elapsed the opener is 4.8s away.
        Assert.InRange(service.SecondsUntilNextBurst, 4.7f, 4.9f);
        Assert.True(service.IsBurstImminent(thresholdSeconds: 5f));
    }

    [Fact]
    public void SyntheticCycle_InsideWindow_OpensBurstWindow()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(10f);

        service.Update(player.Object);

        Assert.True(service.IsInBurstWindow);
        // Window spans 7.8s-27.8s; at 10s elapsed, ~17.8s remain.
        Assert.InRange(service.SecondsRemainingInBurst, 17.7f, 17.9f);
    }

    [Fact]
    public void SyntheticCycle_AfterWindowEnds_RecordsHistoryAndPredictsNext()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();

        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(10f);
        service.Update(player.Object);
        Assert.True(service.IsInBurstWindow);

        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(30f);
        service.Update(player.Object);

        Assert.False(service.IsInBurstWindow);
        Assert.Single(service.BurstWindowHistory);
        // Timer path takes over from the recorded window end (~100s gap).
        Assert.InRange(service.SecondsUntilNextBurst, 90f, 100f);
    }

    [Fact]
    public void SyntheticCycle_OutOfCombat_Inert()
    {
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(0f);

        service.Update(player.Object);

        Assert.False(service.IsInBurstWindow);
        Assert.Equal(-1f, service.SecondsUntilNextBurst);
    }

    // -------------------------------------------------------------------------
    // Finding #18: _lastBurstWindowEnd cleared on combat end
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_OnCombatEnd_ClearsLastBurstWindowEnd_SyntheticCycleResumesInFight2()
    {
        // Arrange: run fight 1 through a synthetic burst window so _lastBurstWindowEnd is set.
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();

        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(10f);
        service.Update(player.Object);   // inside synthetic window
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(30f);
        service.Update(player.Object);   // window closed; _lastBurstWindowEnd is set

        // Timer-based prediction is now active (> 90s until next window).
        Assert.InRange(service.SecondsUntilNextBurst, 90f, 100f);

        // Act: leave combat, then start fight 2 at 3s elapsed.
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(0f);
        service.Update(player.Object);   // combat end; must clear _lastBurstWindowEnd

        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(3f);
        service.Update(player.Object);

        // Assert: synthetic cycle drives prediction again (opener is 4.8s away at 3s elapsed),
        // not the stale timer-based value from fight 1 (~99+ seconds).
        Assert.False(service.IsInBurstWindow);
        Assert.InRange(service.SecondsUntilNextBurst, 4.7f, 4.9f);
    }

    // -------------------------------------------------------------------------
    // Finding #19: ResetHistory called on combat end
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_OnCombatEnd_ClearsHistory()
    {
        // Arrange: fight 1 produces one history entry.
        var (service, combatEvents) = BuildWithCastEvents();
        var player = new Mock<IPlayerCharacter>();

        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(10f);
        service.Update(player.Object);
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(30f);
        service.Update(player.Object);

        Assert.Single(service.BurstWindowHistory);

        // Act: leave combat.
        combatEvents.Setup(x => x.GetCombatDurationSeconds()).Returns(0f);
        service.Update(player.Object);

        // Assert: history is cleared so FightSummaryService grades only the current fight.
        Assert.Empty(service.BurstWindowHistory);
    }
}
