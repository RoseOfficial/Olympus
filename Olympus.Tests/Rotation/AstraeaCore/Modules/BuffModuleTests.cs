using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for Astrologian BuffModule logic.
/// Covers Lightspeed strategies (OnCooldown, SaveForMovement, SaveForRaise)
/// and LucidDreaming MP threshold behavior.
/// </summary>
public class BuffModuleTests
{
    private readonly BuffModule _module;

    public BuffModuleTests()
    {
        _module = new BuffModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is30()
    {
        Assert.Equal(30, _module.Priority);
    }

    [Fact]
    public void Name_IsBuff()
    {
        Assert.Equal("Buff", _module.Name);
    }

    #endregion

    #region Lightspeed — Disabled

    [Fact]
    public void TryExecute_LightspeedDisabled_DoesNotFireLightspeed()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = false;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Lightspeed — OnCooldown Strategy

    [Fact]
    public void TryExecute_Lightspeed_OnCooldown_InCombat_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteOgcd: true,
            canExecuteGcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Lightspeed_OnCooldown_OutOfCombat_DoesNotFire()
    {
        // BuffModule.RequiresCombat = true — does not fire outside combat
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: false, // Out of combat
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Lightspeed — SaveForMovement Strategy

    [Fact]
    public void TryExecute_Lightspeed_SaveForMovement_NotMoving_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.SaveForMovement;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Not moving — should NOT fire with SaveForMovement strategy
        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Lightspeed_SaveForMovement_IsMoving_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.SaveForMovement;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteOgcd: true,
            canExecuteGcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Moving — should fire with SaveForMovement strategy
        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Lightspeed — SaveForRaise Strategy

    [Fact]
    public void TryExecute_Lightspeed_SaveForRaise_NeverFires()
    {
        // SaveForRaise means the buff module never uses Lightspeed (resurrection module handles it)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.SaveForRaise;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true,
            isMoving: true); // Even while moving, SaveForRaise never fires

        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.Lightspeed.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Lightspeed — Level Guard

    [Fact]
    public void TryExecute_Lightspeed_LevelTooLow_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: (byte)(ASTActions.Lightspeed.MinLevel - 1),
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region LucidDreaming — MP Threshold

    [Fact]
    public void TryExecute_LucidDreaming_MpAboveThreshold_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = false;
        config.Astrologian.EnableLucidDreaming = true;
        config.Astrologian.LucidDreamingThreshold = 0.70f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.LucidDreaming.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            currentMp: 8000,  // 80% of 10000 — above 70% threshold
            maxMp: 10000,
            maxHp: 50000,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_MpBelowThreshold_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = false;
        config.Astrologian.EnableLucidDreaming = true;
        config.Astrologian.LucidDreamingThreshold = 0.70f;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteOgcd: true,
            canExecuteGcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.LucidDreaming.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LucidDreaming.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            currentMp: 5000, // 50% of 10000 — below 70% threshold
            maxMp: 10000,
            maxHp: 50000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LucidDreamingDisabled_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableLightspeed = false;
        config.Astrologian.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.LucidDreaming.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            inCombat: true,
            currentMp: 3000, // Low MP, but disabled
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
