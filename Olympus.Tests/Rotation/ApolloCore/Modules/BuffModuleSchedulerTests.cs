using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;
using IBattleNpc = Dalamud.Game.ClientState.Objects.Types.IBattleNpc;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Scheduler-push tests for Apollo BuffModule. Critical: Asylum is ground-targeted,
/// so the candidate must have GroundPosition set (TargetId = 0).
/// </summary>
public class BuffModuleSchedulerTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void CollectCandidates_AsylumReadyAndPartyInjured_PushesGroundTargetedCandidate()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.Asylum.ActionId)).Returns(true);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // Configure party helper to report 4 injured for the AoE check inside Asylum push.
        partyHelper.Setup(p => p.GetAllPartyMembers(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<bool>()))
            .Returns(new System.Collections.Generic.List<Dalamud.Game.ClientState.Objects.Types.IBattleChara>());

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        // Override PartyHealthMetrics on the context to report injured count > 0.
        // Easier: just verify the *gate* logic by passing a context where Asylum
        // is ready and injuredCount > 0 from the helper.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((avgHpPercent: 0.50f, lowestHpPercent: 0.50f, injuredCount: 4));

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        // Asylum must be present: injuredCount = 4, IsActionReady = true, EnableAsylum = true.
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.Asylum.ActionId);
        var asylumCandidate = queue.First(c => c.Behavior.Action.ActionId == WHMActions.Asylum.ActionId);
        // Ground-targeted: GroundPosition must be set and TargetId must be 0 (not a unit target).
        Assert.NotNull(asylumCandidate.GroundPosition);
        Assert.Equal(0ul, asylumCandidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_AsylumDisabled_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.Asylum.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Asylum.ActionId);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: false,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    /// <summary>
    /// At max charges (2/2) but blood lily is full (3/3): next GCD will be Afflatus Misery
    /// (costs 0 MP), so burning a ThinAir charge on overcap prevention wastes it.
    /// The push must be suppressed.
    /// </summary>
    [Fact]
    public void CollectCandidates_ThinAir_AtMaxCharges_BloodLilyFull_SkipsPush()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.ThinAir.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.ThinAir.ActionId)).Returns(2u);
        actionService.Setup(x => x.GetMaxCharges(WHMActions.ThinAir.ActionId, It.IsAny<uint>())).Returns((ushort)2);

        var context = BuildThinAirContext(
            config: config,
            actionService: actionService,
            bloodLilyCount: 3,
            hasFreecure: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.ThinAir.ActionId);
    }

    /// <summary>
    /// A low-HP target would normally trigger "For Cure II" ThinAir, but the Freecure proc
    /// already makes the next Cure II free (0 MP). No charge should be spent.
    /// </summary>
    [Fact]
    public void CollectCandidates_ThinAir_CureIITarget_Freecure_SkipsPush()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.ThinAir.ActionId)).Returns(true);
        // Charges below max so the overcap block does not fire.
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.ThinAir.ActionId)).Returns(1u);
        actionService.Setup(x => x.GetMaxCharges(WHMActions.ThinAir.ActionId, It.IsAny<uint>())).Returns((ushort)2);

        var lowHpTarget = new Mock<IBattleChara>();
        lowHpTarget.Setup(x => x.CurrentHp).Returns(30000u);
        lowHpTarget.Setup(x => x.MaxHp).Returns(50000u);
        lowHpTarget.Setup(x => x.GameObjectId).Returns(8ul);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(p => p.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.60f);

        var context = BuildThinAirContext(
            config: config,
            actionService: actionService,
            bloodLilyCount: 0,
            hasFreecure: true,
            partyHelper: partyHelper);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.ThinAir.ActionId);
    }

    /// <summary>
    /// Builds a direct IApolloContext mock for ThinAir tests that need controllable
    /// BloodLilyCount and HasFreecure — both come from SafeGameAccess in the real
    /// ApolloContext and are always 0/false in unit tests.
    /// </summary>
    private static IApolloContext BuildThinAirContext(
        Configuration config,
        Mock<IActionService> actionService,
        int bloodLilyCount,
        bool hasFreecure,
        Mock<IPartyHelper>? partyHelper = null)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);
        partyHelper ??= MockBuilders.CreateMockPartyHelper();
        var mpForecast = MockBuilders.CreateMockMpForecastService();
        var playerStats = MockBuilders.CreateMockPlayerStatsService();
        var targetingService = MockBuilders.CreateMockTargetingService();

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.ActionService).Returns(actionService.Object);
        ctx.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        ctx.Setup(x => x.PlayerStatsService).Returns(playerStats.Object);
        ctx.Setup(x => x.MpForecastService).Returns(mpForecast.Object);
        ctx.Setup(x => x.TargetingService).Returns(targetingService.Object);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.InCombat).Returns(true);
        ctx.Setup(x => x.HasThinAir).Returns(false);
        ctx.Setup(x => x.HasFreecure).Returns(hasFreecure);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);
        ctx.Setup(x => x.LilyCount).Returns(0);
        ctx.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        return ctx.Object;
    }

    [Fact]
    public void CollectCandidates_PresenceOfMindReady_PushesIt()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnablePresenceOfMind = true;
        config.Buffs.DelayPoMForRaise = false;
        config.Buffs.StackPoMWithAssize = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.PresenceOfMind.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == WHMActions.PresenceOfMind.ActionId);
    }

    [Fact]
    public void CollectCandidates_AetherialShift_AutoFalse_NeverPushed()
    {
        // Auto = false (default) must suppress the push even when Enable = true,
        // the ability is ready, and the target is positioned correctly for a dash.
        // All gates downstream of AutoAetherialShift are satisfied here to prove
        // that AutoAetherialShift itself is the only thing blocking the push.
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnableAetherialShift = true;
        config.Buffs.AutoAetherialShift = false;   // the gate under test

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.AetherialShift.ActionId)).Returns(true);

        // Place enemy 30y directly in front of player (player at origin, rotation 0 = facing +Z).
        // distance 30 > spellRange 25, dot = 1.0 > 0.7 — all downstream gates pass.
        var fakeEnemy = new Mock<IBattleNpc>();
        fakeEnemy.Setup(e => e.Position).Returns(new System.Numerics.Vector3(0, 0, 30));

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(fakeEnemy.Object);

        // Default player mock: position = (0,0,0), Rotation = 0f (Moq default for float).
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.AetherialShift.ActionId);
    }

    [Fact]
    public void CollectCandidates_AetherialShift_AutoTrue_ConditionsMet_PushesCandidate()
    {
        // Auto = true and player is out of cast range but facing the target → must push.
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnableAetherialShift = true;
        config.Buffs.AutoAetherialShift = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.AetherialShift.ActionId)).Returns(true);

        // Player at origin, rotation 0 → forward = (sin(0), 0, cos(0)) = (0, 0, 1).
        // Enemy at (0, 0, 30): distance 30 > spellRange 25, dot = 1.0 > 0.7.
        var fakeEnemy = new Mock<IBattleNpc>();
        fakeEnemy.Setup(e => e.Position).Returns(new System.Numerics.Vector3(0, 0, 30));

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(fakeEnemy.Object);

        // Default player mock: position = (0,0,0), Rotation = 0f (Moq default for float).
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.AetherialShift.ActionId);
    }

    [Fact]
    public void CollectCandidates_AetherialShift_Conjurer_NeverPushed()
    {
        // Aetherial Shift is WHM-exclusive. Even with Auto=true and all geometry conditions
        // satisfied, a Conjurer player must never push the candidate.
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnableAetherialShift = true;
        config.Buffs.AutoAetherialShift = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.AetherialShift.ActionId)).Returns(true);

        // Enemy at (0, 0, 30): same geometry as the positive test — all gates pass except job.
        var fakeEnemy = new Mock<IBattleNpc>();
        fakeEnemy.Setup(e => e.Position).Returns(new System.Numerics.Vector3(0, 0, 30));

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(fakeEnemy.Object);

        // Build a Conjurer player: ClassJob.RowId = 6. RowRef<ClassJob>(null!, conjurerId, null)
        // stores the RowId without accessing game data, so null ExcelModule is safe here.
        var conjurerClassJob = new RowRef<ClassJob>(null!, JobRegistry.Conjurer, null);
        var playerMock = MockBuilders.CreateMockPlayerCharacter(level: 100);
        playerMock.Setup(x => x.ClassJob).Returns(conjurerClassJob);

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(playerMock.Object);
        ctx.Setup(x => x.ActionService).Returns(actionService.Object);
        ctx.Setup(x => x.TargetingService).Returns(targetingService.Object);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.InCombat).Returns(true);
        ctx.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.PartyHelper).Returns(MockBuilders.CreateMockPartyHelper().Object);
        ctx.Setup(x => x.MpForecastService).Returns(MockBuilders.CreateMockMpForecastService().Object);
        ctx.Setup(x => x.PlayerStatsService).Returns(MockBuilders.CreateMockPlayerStatsService().Object);
        ctx.Setup(x => x.HasThinAir).Returns(false);
        ctx.Setup(x => x.HasFreecure).Returns(false);
        ctx.Setup(x => x.HasSwiftcast).Returns(false);
        ctx.Setup(x => x.BloodLilyCount).Returns(0);
        ctx.Setup(x => x.LilyCount).Returns(0);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(ctx.Object, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.AetherialShift.ActionId);
    }

    [Fact]
    public void CollectCandidates_ThinAir_DoesNotPushForRaise_WhenMpAboveAbsoluteButBelowPercentThreshold()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnableThinAir = true;
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f;
        config.EnableHealing = false; // disable AoE/CureII branches so only the raise branch can trigger

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.ThinAir.ActionId)).Returns(true);
        // Charges: 0/2 — not at cap, so the cap-avoidance branch cannot fire
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.ThinAir.ActionId)).Returns(0u);
        actionService.Setup(x => x.GetMaxCharges(WHMActions.ThinAir.ActionId, It.IsAny<uint>())).Returns((ushort)2);

        // currentMp 3000 / default maxMp 10000 = 30%, below 50% threshold
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            currentMp: 3000,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.ThinAir.ActionId);
    }

    [Fact]
    public void CollectCandidates_ThinAir_PushesForRaise_WhenMpAboveBothAbsoluteAndPercentThreshold()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnableThinAir = true;
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f;
        config.EnableHealing = false; // disable AoE/CureII branches so only the raise branch can trigger

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.ThinAir.ActionId)).Returns(true);
        // Charges: 0/2 — not at cap
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.ThinAir.ActionId)).Returns(0u);
        actionService.Setup(x => x.GetMaxCharges(WHMActions.ThinAir.ActionId, It.IsAny<uint>())).Returns((ushort)2);

        // currentMp 6000 / default maxMp 10000 = 60%, above 50% threshold
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            currentMp: 6000,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.ThinAir.ActionId);
    }
}
