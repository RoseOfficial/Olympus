using System.Collections.Generic;
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
/// Tests for Astrologian DamageModule logic.
/// Covers Malefic single-target, Combust DoT, Oracle (Divining proc),
/// Lord of Crowns, level-based action selection, and movement constraints.
/// </summary>
public class DamageModuleTests
{
    private readonly DamageModule _module;

    public DamageModuleTests()
    {
        _module = new DamageModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is50()
    {
        Assert.Equal(50, _module.Priority);
    }

    [Fact]
    public void Name_IsDamage()
    {
        Assert.Equal("Damage", _module.Name);
    }

    #endregion

    #region General Damage Guards

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: false,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_DamageDisabled_NoSingleTarget()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableOracle = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Single-Target Damage — Malefic

    [Fact]
    public void TryExecute_MaleficFires_WhenEnemyFound()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = true;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.FallMalefic.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90, // Level 90 uses Fall Malefic (level 82+)
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.FallMalefic.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Moving_NoLightspeed_DoesNotFireMalefic()
    {
        // Malefic has a cast time — cannot be used while moving unless Lightspeed is active
        // Default context has null StatusList → HasLightspeed = false
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = true;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableOracle = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        // Moving prevents Malefic (has a cast time, no Lightspeed)
        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region DoT — Combust Level Progression

    [Fact]
    public void TryExecute_Combust_Applied_WhenTargetHasNoDoT()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = true;
        config.Astrologian.EnableAoEDamage = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        // Level 90 → Combust III (ActionId = 16554)
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.CombustIII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.CombustIII.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_DoT_NotApplied_WhenAboveRefreshThreshold()
    {
        // No target needs DoT (FindEnemyNeedingDot returns null when DoT has >3s remaining)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = true;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.DotRefreshThreshold = 3f;

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_DoTDisabled_DoesNotApplyCombust()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableAoEDamage = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(t => t.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad =>
                ad.ActionId == ASTActions.Combust.ActionId ||
                ad.ActionId == ASTActions.CombustII.ActionId ||
                ad.ActionId == ASTActions.CombustIII.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Level-Based Action Selection

    [Theory]
    [InlineData(1, 3596u)]   // Malefic
    [InlineData(54, 3598u)]  // Malefic II
    [InlineData(64, 7442u)]  // Malefic III
    [InlineData(72, 16555u)] // Malefic IV
    [InlineData(82, 25871u)] // Fall Malefic
    [InlineData(100, 25871u)]
    public void GetDamageGcdForLevel_ReturnsCorrectMaleficVersion(byte level, uint expectedActionId)
    {
        var action = ASTActions.GetDamageGcdForLevel(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(4, 838u)]    // Combust
    [InlineData(46, 843u)]   // Combust II
    [InlineData(72, 1881u)]  // Combust III
    [InlineData(100, 1881u)]
    public void GetDotStatusId_ReturnsCorrectStatusForLevel(byte level, uint expectedStatusId)
    {
        var statusId = ASTActions.GetDotStatusId(level);
        Assert.Equal(expectedStatusId, statusId);
    }

    #endregion

    #region Oracle — Divining Proc

    [Fact]
    public void TryExecute_Oracle_NoDiviningBuff_DoesNotFire()
    {
        // HasDivining = false (null StatusList) → Oracle never fires
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableOracle = true;
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Oracle.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Oracle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Lord of Crowns

    [Fact]
    public void TryExecute_NoLordCard_DoesNotFireLordOfCrowns()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableOracle = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var cardService = AstraeaTestContext.CreateMockCardService(hasLord: false);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            cardService: cardService,
            level: 100,
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LordOfCrowns.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HasLordCard_WithEnemy_FiresLordOfCrowns()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableOracle = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LordOfCrowns.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var cardService = AstraeaTestContext.CreateMockCardService(hasLord: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            cardService: cardService,
            level: 100,
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LordOfCrowns.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion
}
