using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Tests for DefensiveModule. All 21 rotations are scheduler-migrated;
/// TryExecute is a stub returning false. Tests exercise CollectCandidates
/// to verify candidates are pushed (or withheld) under the correct gate
/// conditions for each defensive ability.
/// </summary>
public class DefensiveModuleTests
{
    private readonly DefensiveModule _module = new();

    #region Module Properties

    [Fact]
    public void Priority_Is20()
    {
        Assert.Equal(20, _module.Priority);
    }

    [Fact]
    public void Name_IsDefensive()
    {
        Assert.Equal("Defensive", _module.Name);
    }

    #endregion

    #region Combat Gate

    /// <summary>
    /// CollectCandidates returns immediately when not in combat — no candidates of
    /// any kind should appear in the oGCD queue.
    /// </summary>
    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: false,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    #endregion

    #region Temperance

    /// <summary>
    /// When EnableTemperance is false the config gate fires immediately and no
    /// Temperance candidate reaches the queue, even with the party at low HP.
    /// </summary>
    [Fact]
    public void CollectCandidates_TemperanceDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableTemperance = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.50f, 0.40f, 4)); // low HP — would satisfy shouldUse if not gated

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Temperance.ActionId);
    }

    /// <summary>
    /// Level 70 is below Temperance's MinLevel of 80; the level gate blocks the push
    /// regardless of party HP.
    /// </summary>
    [Fact]
    public void CollectCandidates_TemperanceLevelTooLow_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableTemperance = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.50f, 0.40f, 4)); // low HP — would satisfy shouldUse if not gated

        // Temperance requires level 80; level 70 fails the level check.
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 70,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Temperance.ActionId);
    }

    /// <summary>
    /// When all gates pass (enabled, level met, ready, party HP below threshold),
    /// Temperance is pushed at priority 80 in the oGCD queue.
    /// </summary>
    [Fact]
    public void CollectCandidates_TemperanceReady_LowPartyHp_PushesAtPriority80()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableTemperance = true;
        config.Defensive.DefensiveCooldownThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.Temperance.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // avgHp 50% < threshold 80% — satisfies shouldUse; partyCoord is null so no remote mit check.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.50f, 0.40f, 4));

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.Temperance.ActionId);
        var candidate = queue.First(c => c.Behavior.Action.ActionId == WHMActions.Temperance.ActionId);
        Assert.Equal(80, candidate.Priority);
    }

    #endregion

    #region Divine Benison

    /// <summary>
    /// When EnableDivineBenison is false the config gate fires and no candidate
    /// is pushed, even with a tank target present.
    /// </summary>
    [Fact]
    public void CollectCandidates_DivineBenisonDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineBenison = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.DivineBenison.ActionId);
    }

    /// <summary>
    /// Level 50 is below Divine Benison's MinLevel of 66; the level check inside
    /// ActionValidator.CanExecute blocks the push.
    /// </summary>
    [Fact]
    public void CollectCandidates_DivineBenisonLevelTooLow_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineBenison = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        // Divine Benison requires level 66; level 50 fails the level gate.
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 50,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.DivineBenison.ActionId);
    }

    #endregion

    #region Aquaveil

    /// <summary>
    /// When EnableAquaveil is false the config gate blocks the push.
    /// </summary>
    [Fact]
    public void CollectCandidates_AquaveilDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableAquaveil = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Aquaveil.ActionId);
    }

    /// <summary>
    /// Level 80 is below Aquaveil's MinLevel of 86; the level gate inside
    /// ActionValidator.CanExecute blocks the push.
    /// </summary>
    [Fact]
    public void CollectCandidates_AquaveilLevelTooLow_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableAquaveil = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        // Aquaveil requires level 86; level 80 fails the level gate.
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 80,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Aquaveil.ActionId);
    }

    #endregion

    #region Plenary Indulgence

    /// <summary>
    /// When EnablePlenaryIndulgence is false the config gate in ActionValidator blocks
    /// the push, even with enough injured party members.
    /// </summary>
    [Fact]
    public void CollectCandidates_PlenaryDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnablePlenaryIndulgence = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4)); // enough injured — would satisfy shouldUse if not gated

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PlenaryIndulgence.ActionId);
    }

    /// <summary>
    /// Level 60 is below Plenary Indulgence's MinLevel of 70; ActionValidator.CanExecute
    /// blocks the push at the level check.
    /// </summary>
    [Fact]
    public void CollectCandidates_PlenaryLevelTooLow_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnablePlenaryIndulgence = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        // Plenary Indulgence requires level 70; level 60 fails the level gate.
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 60,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PlenaryIndulgence.ActionId);
    }

    /// <summary>
    /// When all gates pass (enabled, level met, ready, enough injured members, and
    /// UseDefensivesWithAoEHeals is on), Plenary Indulgence is pushed at priority 100.
    /// </summary>
    [Fact]
    public void CollectCandidates_PlenaryReady_EnoughInjured_PushesAtPriority100()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnablePlenaryIndulgence = true;
        config.Defensive.UseDefensivesWithAoEHeals = true;
        config.Healing.AoEHealMinTargets = 3;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.PlenaryIndulgence.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // injuredCount=3 satisfies injuredCount >= AoEHealMinTargets=3.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 3));

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.PlenaryIndulgence.ActionId);
        var candidate = queue.First(c => c.Behavior.Action.ActionId == WHMActions.PlenaryIndulgence.ActionId);
        Assert.Equal(100, candidate.Priority);
    }

    #endregion

    #region Liturgy of the Bell

    /// <summary>
    /// When EnableLiturgyOfTheBell is false the ActionValidator config gate blocks the push,
    /// even with enough injured party members. Note: Liturgy is NOT gated on EnableHealing —
    /// only on its own toggle.
    /// </summary>
    [Fact]
    public void CollectCandidates_LiturgyDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 3)); // enough injured — would satisfy threshold if not gated

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
    }

    /// <summary>
    /// When only 1 party member is injured the injuredCount lt 2 guard fires,
    /// blocking the push even though the action is enabled and ready.
    /// </summary>
    [Fact]
    public void CollectCandidates_LiturgyNotEnoughInjured_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.LiturgyOfTheBell.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // injuredCount=1 fails the injuredCount >= 2 gate in TryPushLiturgyOfTheBell.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.90f, 0.80f, 1));

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
    }

    /// <summary>
    /// When all gates pass (enabled, level 90 met, ready, injuredCount >= 2), the Bell
    /// is pushed as a ground-targeted oGCD at priority 130 — TargetId is 0 and
    /// GroundPosition is non-null (placement at player position when no tank is found).
    /// </summary>
    [Fact]
    public void CollectCandidates_LiturgyReady_EnoughInjured_PushesGroundTargetedAtPriority130()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.LiturgyOfTheBell.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // injuredCount=3 >= 2; FindTankInParty returns null by default (placement at player pos).
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 3));

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
        var candidate = queue.First(c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
        Assert.Equal(130, candidate.Priority);
        // Ground-targeted push: GroundPosition must be set, TargetId must be 0.
        Assert.NotNull(candidate.GroundPosition);
        Assert.Equal(0ul, candidate.TargetId);
    }

    /// <summary>
    /// When a raidwide is imminent (timeline prediction within the preparation window) the Bell
    /// must be pushed proactively even with a healthy party (injuredCount=0). The reactive
    /// injuredCount gate is bypassed when raidwideImminent is true.
    /// </summary>
    [Fact]
    public void CollectCandidates_LiturgyRaidwideImminent_HealthyParty_PushesGroundTargetedAtPriority130()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = true;
        config.Timeline.EnableTimelinePredictions = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.LiturgyOfTheBell.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // injuredCount=0 — reactive gate alone would block the push.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.95f, 0.90f, 0));

        var timelineService = new Mock<ITimelineService>();
        timelineService.Setup(t => t.IsActive).Returns(true);
        timelineService.Setup(t => t.Confidence).Returns(0.95f);
        timelineService.Setup(t => t.NextRaidwide).Returns(new MechanicPrediction(
            secondsUntil: 4f, type: TimelineEntryType.Raidwide, name: "Flare", confidence: 0.95f));

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            timelineService: timelineService.Object,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
        var candidate = queue.First(c => c.Behavior.Action.ActionId == WHMActions.LiturgyOfTheBell.ActionId);
        Assert.Equal(130, candidate.Priority);
        // Ground-targeted push: GroundPosition must be set, TargetId must be 0.
        Assert.NotNull(candidate.GroundPosition);
        Assert.Equal(0ul, candidate.TargetId);
    }

    #endregion

    #region Divine Caress

    /// <summary>
    /// When EnableDivineCaress is false the config gate fires and no candidate is pushed.
    /// (In practice DivineCaress also requires the DivineGrace proc status, which is never
    /// present on the mocked player, so this test specifically isolates the config toggle.)
    /// </summary>
    [Fact]
    public void CollectCandidates_DivineCaressDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineCaress = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.DivineCaress.ActionId);
    }

    #endregion

    #region Healing Master Toggle

    /// <summary>
    /// When EnableHealing is false, every defensive that gates on it
    /// (Temperance, Divine Benison, Plenary Indulgence, Aquaveil, Divine Caress)
    /// must be absent from the queue. Note: Liturgy of the Bell is NOT gated on
    /// EnableHealing, so it is intentionally excluded from this assertion.
    /// </summary>
    [Fact]
    public void CollectCandidates_HealingDisabled_PushesNoHealingGatedDefensives()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = false;
        config.Defensive.EnableTemperance = true;
        config.Defensive.EnableDivineBenison = true;
        config.Defensive.EnablePlenaryIndulgence = true;
        config.Defensive.EnableAquaveil = true;
        config.Defensive.EnableDivineCaress = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.50f, 0.30f, 4)); // conditions that would fire all defensives if healing were on

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.Temperance.ActionId);
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.DivineBenison.ActionId);
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.PlenaryIndulgence.ActionId);
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.Aquaveil.ActionId);
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.DivineCaress.ActionId);
    }

    #endregion
}
