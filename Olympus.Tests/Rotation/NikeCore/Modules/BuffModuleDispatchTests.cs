using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Nike (SAM) BuffModule.
/// Verifies the MeikyoShisui Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void MeikyoShisui_Dispatches_WhenEnabled()
    {
        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableMeikyoShisui = true;
        config.Samurai.EnableShoha = false;    // ensure Shoha (priority 1) does not intercept
        config.Samurai.EnableZanshin = false;   // ensure Zanshin (priority 1) does not intercept
        config.Samurai.EnableIkishoten = false; // ensure Ikishoten (priority 2) does not intercept

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasMeikyoShisui: false,
            hasFugetsu: false); // hasFugetsu=false triggers shouldUseMeikyo=true

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void MeikyoShisui_DoesNotDispatch_WhenDisabled()
    {
        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableMeikyoShisui = false;
        config.Samurai.EnableShoha = false;
        config.Samurai.EnableZanshin = false;
        config.Samurai.EnableIkishoten = false;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasMeikyoShisui: false,
            hasFugetsu: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
