using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar ResurrectionModule logic.
/// Scholar uses the shared BaseResurrectionModule — raise requires Swiftcast or hardcast.
/// </summary>
public class ResurrectionModuleTests
{
    private readonly ResurrectionModule _module;

    public ResurrectionModuleTests()
    {
        _module = new ResurrectionModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is5()
    {
        // BaseResurrectionModule has priority 5 (highest)
        Assert.Equal(5, _module.Priority);
    }

    [Fact]
    public void Name_IsResurrection()
    {
        Assert.Equal("Resurrection", _module.Name);
    }

    #endregion

    #region Raise Disabled Tests

    [Fact]
    public void TryExecute_RaiseDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoDeadMembers_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;

        // All party members are alive
        var aliveMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 50000, maxHp: 50000, isDead: false);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { aliveMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Swiftcast Raise Tests

    [Fact]
    public void TryExecute_DeadTarget_SwiftcastAvailable_TriesRaise()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: true);

        // Swiftcast is ready as an oGCD — module will use it first, then raise on next GCD
        actionService.Setup(a => a.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Swiftcast.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            currentMp: 9000, // Enough MP (need 2400)
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        // Module should use Swiftcast oGCD first (then raise next frame)
        Assert.True(result);
    }

    [Fact]
    public void TryExecute_DeadTarget_InsufficientMp_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 1u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: true);

        // MP is below threshold (5% when threshold is 10%)
        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            currentMp: 500,   // 5% of 10000
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Hardcast Raise Tests

    [Fact]
    public void TryExecute_DeadTarget_HardcastAllowed_NoSwiftcast_CanStillRaise()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Swiftcast is NOT ready — must hardcast
        actionService.Setup(a => a.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);
        // Swiftcast is on a long cooldown (>10s) — triggers hardcast path
        actionService.Setup(a => a.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(55f);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Resurrection.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            currentMp: 9000,
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Resurrection.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_DeadTarget_HardcastDisabled_NoSwiftcast_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Swiftcast is NOT ready, hardcast disabled
        actionService.Setup(a => a.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            currentMp: 9000,
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Out-of-Combat Tests

    [Fact]
    public void TryExecute_OutOfCombat_DeadTarget_CanRaise()
    {
        // Scholar can raise out of combat (no restriction in BaseResurrectionModule)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        // Swiftcast is on a long cooldown (>10s) — triggers hardcast path
        actionService.Setup(a => a.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(55f);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Resurrection.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: false,
            currentMp: 9000,
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Resurrection.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion
}
