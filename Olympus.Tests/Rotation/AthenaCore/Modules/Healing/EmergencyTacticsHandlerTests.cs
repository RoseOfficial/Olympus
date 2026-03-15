using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Tests for EmergencyTacticsHandler.
/// Positive path not testable: TryEmergencyTactics requires HasGalvanize(target) == true.
/// HasGalvanize reads target.StatusList, which is a sealed class backed by native game memory
/// — it cannot be instantiated or mocked with Moq. All tests here cover early-exit guard paths.
/// </summary>
public class EmergencyTacticsHandlerTests
{
    private readonly EmergencyTacticsHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEmergencyTactics = false;
        var context = AthenaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEmergencyTactics = true;
        config.Scholar.EmergencyTacticsThreshold = 0.40f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        // HasEmergencyTactics checks StatusList on player (null → false), so we skip that guard
        actionService.Setup(x => x.IsActionReady(SCHActions.EmergencyTactics.ActionId)).Returns(false);

        var injuredMember = MockBuilders.CreateMockBattleChara(currentHp: 20000, maxHp: 100000); // 20%
        var partyHelper = new TestableAthenaPartyHelper(new[] { injuredMember.Object });

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEmergencyTactics = true;
        config.Scholar.EmergencyTacticsThreshold = 0.40f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.EmergencyTactics.ActionId)).Returns(true);

        // Member above threshold (50% > 40%)
        var member = MockBuilders.CreateMockBattleChara(currentHp: 50000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { member.Object });

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoGalvanizeOnTarget_Skips()
    {
        // With null StatusList, HasGalvanize returns false → Emergency Tactics should not fire
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEmergencyTactics = true;
        config.Scholar.EmergencyTacticsThreshold = 0.40f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.EmergencyTactics.ActionId)).Returns(true);

        // Member below threshold but null StatusList → HasGalvanize = false
        var member = MockBuilders.CreateMockBattleChara(currentHp: 20000, maxHp: 100000); // 20%
        var partyHelper = new TestableAthenaPartyHelper(new[] { member.Object });

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        // No Galvanize → should skip
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoPartyMembers_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEmergencyTactics = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.EmergencyTactics.ActionId)).Returns(true);

        // Empty party — FindLowestHpPartyMember returns null
        var partyHelper = new TestableAthenaPartyHelper(System.Array.Empty<Dalamud.Game.ClientState.Objects.Types.IBattleChara>());

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
