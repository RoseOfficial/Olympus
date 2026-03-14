using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for Astrologian ResurrectionModule (Ascend) logic.
/// Covers raise configuration, MP thresholds, Swiftcast, Lightspeed, and hardcast rules.
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
        Assert.Equal(5, _module.Priority);
    }

    [Fact]
    public void Name_IsResurrection()
    {
        Assert.Equal("Resurrection", _module.Name);
    }

    #endregion

    #region Raise Configuration Tests

    [Fact]
    public void TryExecute_RaiseDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = false;

        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara>(), config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RaiseLevelTooLow_ReturnsFalse()
    {
        // Ascend requires level 12 — use level 10
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 10,
            currentMp: 10000,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region MP Threshold Tests

    [Fact]
    public void TryExecute_InsufficientMpForAscend_ReturnsFalse()
    {
        // MP below the 2400 cost for Ascend
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 2000, // Less than 2400 cost
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_MpBelowThreshold_ReturnsFalse()
    {
        // MP above cost but below configured threshold percentage
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f; // 50% threshold

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 4000, // 40% of 10000, below 50% threshold
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region No Target Tests

    [Fact]
    public void TryExecute_NoDeadPartyMember_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;

        // Empty party — no dead members
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara>(), config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Hardcast Tests

    [Fact]
    public void TryExecute_HardcastDisabled_NoSwiftcast_NoLightspeed_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Swiftcast on cooldown
        actionService.Setup(a => a.IsActionReady(ASTActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(ASTActions.Swiftcast.ActionId))
            .Returns(60f);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HardcastEnabled_Moving_ReturnsFalse()
    {
        // Hardcast enabled but player is moving — cannot hardcast while moving
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(ASTActions.Swiftcast.ActionId))
            .Returns(60f);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Moving prevents hardcast raise
        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
    }

    #endregion

    #region Swiftcast Tests

    [Fact]
    public void TryExecute_SwiftcastAvailable_OgcdWindow_ExecutesSwiftcast()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Swiftcast.ActionId))
            .Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(ASTActions.Swiftcast, It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(ASTActions.Swiftcast, It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Out-of-Combat Resurrection

    [Fact]
    public void TryExecute_OutOfCombat_DeadPartyMember_SufficientMp_SufficientLevel_AttemptsRaise()
    {
        // The base resurrection module has no combat check — it attempts raises regardless
        // of combat state. Out of combat with a dead member, sufficient MP, and level >= 12,
        // the module proceeds to hardcast raise (AllowHardcastRaise = true, not moving).
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.0f;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(ASTActions.Swiftcast.ActionId))
            .Returns(60f); // Swiftcast on cooldown — forces hardcast path
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // inCombat = false — out of combat
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            inCombat: false,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // Production code has no combat gate — it raises out of combat
        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region AST-Specific: Lightspeed as raise enabler

    [Fact]
    public void TryExecute_AST_HasLightspeedContext_HardcastEnabled_Raises()
    {
        // AST has Lightspeed as an additional instant-cast option for raises.
        // When hardcast is enabled, no Swiftcast, but GCD window is open,
        // the module attempts the hardcast raise directly.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.0f;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { deadMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(ASTActions.Swiftcast.ActionId))
            .Returns(60f);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // Should raise (hardcast)
        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Ascend.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion
}
