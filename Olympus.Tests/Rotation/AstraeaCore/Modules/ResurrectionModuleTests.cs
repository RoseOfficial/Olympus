using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Scheduler-push tests for Astraea (AST) ResurrectionModule.
///
/// Context constraints that apply to ALL tests here:
/// - Player.StatusList = null, so HasSwiftcast and HasLightspeed are always false.
///   This means the "instant GCD while buff is active" branch of TryPushRaise cannot
///   be exercised. Instead the four covered paths are:
///     1. TryPushSwiftcast: Swiftcast oGCD pushed when dead member exists and Swiftcast ready.
///     2. TryPushLightspeed: Lightspeed oGCD pushed when Swiftcast unavailable.
///     3. Hardcast: Ascend GCD pushed when both CDs long and ShouldWaitForPreRaiseBuff = false.
///     4. ShouldWaitForPreRaiseBuff gate: Ascend NOT pushed when Lightspeed CD <= 10f.
/// </summary>
public class ResurrectionModuleTests
{
    private readonly ResurrectionModule _module = new();

    // -----------------------------------------------------------------------
    // 1. Swiftcast push
    // -----------------------------------------------------------------------

    [Fact]
    public void TryPushSwiftcast_PushesSwiftcastOgcd_WhenDeadMemberExistsAndSwiftcastReady()
    {
        var actionService = MockBuilders.CreateMockActionService();
        // Default CreateMockActionService already returns IsActionReady = true for all
        // actions, but explicit setup makes the test intent clearer.
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AstraeaTestContext.Create(
            actionService: actionService,
            partyHelper: CreatePartyWithDeadMember(),
            level: 90);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AstraeaAbilities.Swiftcast && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // 2. Lightspeed push (Swiftcast on cooldown, Lightspeed ready)
    // -----------------------------------------------------------------------

    [Fact]
    public void TryPushLightspeed_PushesLightspeedOgcd_WhenSwiftcastUnavailableAndLightspeedReady()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AstraeaTestContext.Create(
            actionService: actionService,
            partyHelper: CreatePartyWithDeadMember(),
            level: 90);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AstraeaAbilities.Lightspeed && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // 3. Hardcast raise (both Swiftcast and Lightspeed on long cooldown)
    // -----------------------------------------------------------------------

    [Fact]
    public void TryPushRaise_PushesAscendGcd_WhenHardcastAllowedAndBothInstantCastCooldownsAreLong()
    {
        // Swiftcast CD > 10f triggers hardcast branch in TryPushRaise.
        // Lightspeed CD > 10f satisfies ShouldWaitForPreRaiseBuff returning false,
        // so Ascend is pushed to the GCD queue.
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(30f);
        actionService.Setup(x => x.GetCooldownRemaining(ASTActions.Lightspeed.ActionId)).Returns(30f);

        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.AllowHardcastRaise = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AstraeaTestContext.Create(
            actionService: actionService,
            partyHelper: CreatePartyWithDeadMember(),
            config: config,
            level: 90,
            isMoving: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AstraeaAbilities.Ascend && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // 4. ShouldWaitForPreRaiseBuff gate blocks hardcast
    // -----------------------------------------------------------------------

    [Fact]
    public void TryPushRaise_DoesNotPushAscend_WhenLightspeedCooldownIsShort()
    {
        // Lightspeed CD <= 10f means ShouldWaitForPreRaiseBuff returns true.
        // The module must NOT push Ascend — the player should wait for Lightspeed to be ready.
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(30f);
        // Lightspeed CD = 5f (< 10f threshold) triggers the pre-raise-buff wait.
        actionService.Setup(x => x.GetCooldownRemaining(ASTActions.Lightspeed.ActionId)).Returns(5f);

        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.AllowHardcastRaise = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AstraeaTestContext.Create(
            actionService: actionService,
            partyHelper: CreatePartyWithDeadMember(),
            config: config,
            level: 90,
            isMoving: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == AstraeaAbilities.Ascend);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a party helper containing one dead member (EntityId=2, different from
    /// the player's default EntityId=1 in CreateMockPlayerCharacter). Position is
    /// Vector3.Zero, same as the player, so the range check passes.
    /// </summary>
    private static TestableAstraeaPartyHelper CreatePartyWithDeadMember()
    {
        var dead = MockBuilders.CreateMockBattleChara(
            entityId: 2u,
            currentHp: 0,
            maxHp: 50000,
            isDead: true);
        return new TestableAstraeaPartyHelper(new List<IBattleChara> { dead.Object });
    }
}
