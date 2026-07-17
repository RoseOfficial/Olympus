using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.EchidnaCore.Abilities;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.EchidnaCore.Modules;

/// <summary>
/// Verifies two pre-downtime gates added to VPR DamageModule:
///   1. Reawaken long-commit block: Reawaken is blocked within 15s of downtime when
///      no ReadyToReawaken proc is active (5+ GCDs consumed mid-sequence).
///   2. Reawaken proc escape: HasReadyToReawaken overrides the block.
///   3. UncoiledFury downtime dump: fires when downtime within 8s even if
///      RattlingCoils is below the normal "should use" threshold.
///   4. UncoiledFury not pushed when no timeline and coils below threshold.
/// </summary>
public class DamageModuleVprDowntimeTests
{
    // -----------------------------------------------------------------------
    // 1. Reawaken blocked when downtime within 15s and no proc
    // -----------------------------------------------------------------------

    [Fact]
    public void Reawaken_Blocked_WhenDowntimeWithin15s_NoProc()
    {
        var module = new DamageModule();

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableReawaken = true;
        config.Viper.EnableBurstPooling = false; // isolate the downtime block

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateReawakenContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            timelineService: timeline,
            serpentOffering: 50,
            hasReadyToReawaken: false,
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            hasNoxiousGnash: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // !false && ShouldDumpForDowntime(5s, 15s) = true → blocked → NOT in GCD queue
        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == EchidnaAbilities.Reawaken);
    }

    // -----------------------------------------------------------------------
    // 2. Reawaken proc overrides the downtime block
    // -----------------------------------------------------------------------

    [Fact]
    public void Reawaken_ProcEscapes_DowntimeBlock()
    {
        var module = new DamageModule();

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableReawaken = true;
        config.Viper.EnableBurstPooling = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateReawakenContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            timelineService: timeline,
            serpentOffering: 50,
            hasReadyToReawaken: true,  // proc bypasses the block
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            hasNoxiousGnash: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // !true && ... = false → block skipped → subsequent gates pass → Reawaken pushed at priority 1
        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == EchidnaAbilities.Reawaken && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // 3. UncoiledFury fires when downtime within 8s (burst hold bypassed by dump)
    // -----------------------------------------------------------------------

    [Fact]
    public void UncoiledFury_Dump_Fires_WhenDowntimeImminent()
    {
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        var module = new DamageModule(burstService.Object);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableUncoiledFury = true;
        config.Viper.RattlingCoilMinStacks = 2; // coils=1 < max=2
        config.Viper.EnableBurstPooling = true;
        config.Viper.SaveRattlingCoilForBurst = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(VPRActions.UncoiledFury.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(false);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateUncoiledFuryContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            timelineService: timeline,
            rattlingCoils: 1);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // dumpForDowntime=true, (true && 1>=1=true) → shouldUse=true → UncoiledFury pushed at priority 3
        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == EchidnaAbilities.UncoiledFury && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // 4. UncoiledFury held when burst imminent, SaveRattlingCoil=true, coils below max
    //    (no timeline → dumpForDowntime=false → the burst hold gate fires)
    // -----------------------------------------------------------------------

    [Fact]
    public void UncoiledFury_HeldByBurstHold_WhenNoTimeline()
    {
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        var module = new DamageModule(burstService.Object);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableUncoiledFury = true;
        config.Viper.RattlingCoilMinStacks = 2;         // coils=1 < max=2
        config.Viper.EnableBurstPooling = true;
        config.Viper.SaveRattlingCoilForBurst = true;   // burst hold gate active

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(VPRActions.UncoiledFury.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateUncoiledFuryContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            timelineService: null,
            rattlingCoils: 1);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // dumpForDowntime=false AND burst imminent AND SaveRattlingCoil AND coils < max → held
        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == EchidnaAbilities.UncoiledFury);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 100ul)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.GameObjectId).Returns(objectId);
        m.Setup(x => x.CurrentHp).Returns(10000u);
        m.Setup(x => x.MaxHp).Returns(10000u);
        return m;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    private static IEchidnaContext CreateReawakenContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int serpentOffering = 50,
        bool hasReadyToReawaken = false,
        bool hasHuntersInstinct = false,
        float huntersInstinctRemaining = 20f,
        bool hasSwiftscaled = false,
        float swiftscaledRemaining = 20f,
        bool hasNoxiousGnash = false,
        float noxiousGnashRemaining = 20f,
        byte level = 100)
    {
        config ??= EchidnaTestContext.CreateDefaultViperConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IEchidnaContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // VPR gauge
        mock.Setup(x => x.SerpentOffering).Returns(serpentOffering);
        mock.Setup(x => x.AnguineTribute).Returns(0);
        mock.Setup(x => x.RattlingCoils).Returns(0);
        mock.Setup(x => x.IsReawakened).Returns(false);
        mock.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mock.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);

        // Buff state
        mock.Setup(x => x.HasReadyToReawaken).Returns(hasReadyToReawaken);
        mock.Setup(x => x.HasHuntersInstinct).Returns(hasHuntersInstinct);
        mock.Setup(x => x.HuntersInstinctRemaining).Returns(huntersInstinctRemaining);
        mock.Setup(x => x.HasSwiftscaled).Returns(hasSwiftscaled);
        mock.Setup(x => x.SwiftscaledRemaining).Returns(swiftscaledRemaining);
        mock.Setup(x => x.HasNoxiousGnash).Returns(hasNoxiousGnash);
        mock.Setup(x => x.NoxiousGnashRemaining).Returns(noxiousGnashRemaining);
        mock.Setup(x => x.HasHonedSteel).Returns(false);
        mock.Setup(x => x.HasHonedReavers).Returns(false);
        mock.Setup(x => x.HasFlankstungVenom).Returns(false);
        mock.Setup(x => x.HasHindstungVenom).Returns(false);
        mock.Setup(x => x.HasFlanksbaneVenom).Returns(false);
        mock.Setup(x => x.HasHindsbaneVenom).Returns(false);
        mock.Setup(x => x.HasGrimskinsVenom).Returns(false);
        mock.Setup(x => x.HasGrimhuntersVenom).Returns(false);
        mock.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mock.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);

        return mock.Object;
    }

    private static IEchidnaContext CreateUncoiledFuryContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int rattlingCoils = 1,
        byte level = 100)
    {
        config ??= EchidnaTestContext.CreateDefaultViperConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IEchidnaContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // VPR gauge
        mock.Setup(x => x.SerpentOffering).Returns(0);
        mock.Setup(x => x.AnguineTribute).Returns(0);
        mock.Setup(x => x.RattlingCoils).Returns(rattlingCoils);
        mock.Setup(x => x.IsReawakened).Returns(false);
        mock.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mock.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);

        // All buff defaults to false/zero to suppress Reawaken and other GCDs
        mock.Setup(x => x.HasReadyToReawaken).Returns(false);
        mock.Setup(x => x.HasHuntersInstinct).Returns(false);
        mock.Setup(x => x.HuntersInstinctRemaining).Returns(0f);
        mock.Setup(x => x.HasSwiftscaled).Returns(false);
        mock.Setup(x => x.SwiftscaledRemaining).Returns(0f);
        mock.Setup(x => x.HasNoxiousGnash).Returns(false);
        mock.Setup(x => x.NoxiousGnashRemaining).Returns(0f);
        mock.Setup(x => x.HasHonedSteel).Returns(false);
        mock.Setup(x => x.HasHonedReavers).Returns(false);
        mock.Setup(x => x.HasFlankstungVenom).Returns(false);
        mock.Setup(x => x.HasHindstungVenom).Returns(false);
        mock.Setup(x => x.HasFlanksbaneVenom).Returns(false);
        mock.Setup(x => x.HasHindsbaneVenom).Returns(false);
        mock.Setup(x => x.HasGrimskinsVenom).Returns(false);
        mock.Setup(x => x.HasGrimhuntersVenom).Returns(false);
        mock.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mock.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);

        return mock.Object;
    }
}
