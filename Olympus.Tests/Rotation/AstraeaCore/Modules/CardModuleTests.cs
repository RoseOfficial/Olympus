using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
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
/// Tests for Astrologian CardModule logic.
/// Covers Draw, Play, Divination, Astrodyne, and Minor Arcana behavior.
/// </summary>
public class CardModuleTests
{
    private readonly CardModule _module;

    public CardModuleTests()
    {
        _module = new CardModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is3()
    {
        Assert.Equal(3, _module.Priority);
    }

    [Fact]
    public void Name_IsCard()
    {
        Assert.Equal("Card", _module.Name);
    }

    #endregion

    #region Cards Disabled

    [Fact]
    public void TryExecute_CardsDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region GCD Window — No Execution Without oGCD

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        // Card actions are all oGCDs; without an oGCD window nothing fires
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            sealCount: 3,
            uniqueSealCount: 3);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Divination Tests

    [Fact]
    public void TryExecute_Divination_InCombat_Ready_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = true;
        config.Astrologian.EnableCards = false; // Disable card draw/play to isolate Divination
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Divination.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Divination.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Re-enable cards just for divination
        config.Astrologian.EnableCards = true;

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 50);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Divination.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Divination_NotInCombat_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Divination.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: false, // Not in combat
            level: 50);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Divination.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Divination_Disabled_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Divination.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 50);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Divination.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Divination_LevelTooLow_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Divination.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 49); // Divination requires level 50

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Divination.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Astrodyne Tests

    [Fact]
    public void TryExecute_Astrodyne_3Seals_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAstrodyne = true;
        config.Astrologian.AstrodyneMinSeals = 1;
        config.Astrologian.EnableDivination = false; // Disable higher priority

        var cardService = AstraeaTestContext.CreateMockCardService(
            sealCount: 3,
            uniqueSealCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Astrodyne.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Astrodyne.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            sealCount: 3,
            uniqueSealCount: 3,
            level: 50);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Astrodyne.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Astrodyne_InsufficientSeals_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAstrodyne = true;
        config.Astrologian.AstrodyneMinSeals = 2;
        config.Astrologian.EnableDivination = false;

        // Only 2 seals but CanUseAstrodyne requires 3
        var cardService = AstraeaTestContext.CreateMockCardService(
            sealCount: 2,
            uniqueSealCount: 2);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Astrodyne.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            sealCount: 2,
            uniqueSealCount: 2,
            level: 50);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Astrodyne.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Astrodyne_Disabled_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableDivination = false;

        var cardService = AstraeaTestContext.CreateMockCardService(
            sealCount: 3,
            uniqueSealCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Astrodyne.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            sealCount: 3,
            uniqueSealCount: 3,
            level: 50);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Astrodyne.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Astrodyne_UniqueSealsLessThanMinimum_DoesNotFire()
    {
        // 3 seals total but only 1 unique — config requires 2 unique seals minimum
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAstrodyne = true;
        config.Astrologian.AstrodyneMinSeals = 2;
        config.Astrologian.EnableDivination = false;

        var cardService = AstraeaTestContext.CreateMockCardService(
            sealCount: 3,
            uniqueSealCount: 1); // Only 1 unique seal

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Astrodyne.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            sealCount: 3,
            uniqueSealCount: 1,
            level: 50);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Astrodyne.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Draw Tests

    [Fact]
    public void TryExecute_Draw_InCombat_NoCards_DrawsCard()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        // No card in hand
        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // No action is "ready" in the traditional sense for draws; ExecuteOgcd decides
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AstralDraw.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 30);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AstralDraw.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Draw_NotInCombat_DoesNotDraw()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: false, // Not in combat
            level: 30);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AstralDraw.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.UmbralDraw.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Draw_LevelTooLow_DoesNotDraw()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 29); // Below AstralDraw level 30

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AstralDraw.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Draw_UmbralDrawFallback_WhenAstralDrawFails()
    {
        // Astral Draw fails (returns false) — should fall back to Umbral Draw
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AstralDraw.ActionId),
                It.IsAny<ulong>()))
            .Returns(false); // Astral fails
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.UmbralDraw.ActionId),
                It.IsAny<ulong>()))
            .Returns(true); // Umbral succeeds

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 30);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.UmbralDraw.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Play Card Tests

    [Fact]
    public void TryExecute_PlayCard_NoCardInHand_SkipsPlay()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        // No card to play
        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // All draw actions return false so card play would be attempted but finds nothing
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(false);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 30,
            hasCard: false);

        _module.TryExecute(context, isMoving: false);

        // No specific card should have fired
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.TheBalance.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.TheSpear.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_PlayBalance_HasCardInHand_FallsBackToSelf_Fires()
    {
        // CreateMockBattleChara returns Mock<IBattleChara>, not IPlayerCharacter.
        // Role detection in BasePartyHelper (IsMeleeDps, IsTankRole, etc.) checks
        // "chara is IPlayerCharacter" and returns false for plain IBattleChara mocks.
        // The party member therefore matches no role bucket and FindBalanceTarget
        // falls through to the self-fallback (returns player). This test verifies
        // the play action still fires on self rather than being skipped entirely.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;
        config.Astrologian.EnableMinorArcana = false;

        var cardService = AstraeaTestContext.CreateMockCardService(
            hasCard: true,
            currentCard: ASTActions.CardType.TheBalance);

        // Party member is IBattleChara, so role detection skips it and self-fallback triggers
        var target = MockBuilders.CreateMockBattleChara(entityId: 2u);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { target.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.TheBalance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 30,
            hasCard: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.TheBalance.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Minor Arcana Tests

    [Fact]
    public void TryExecute_MinorArcana_OnCooldown_Ready_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.OnCooldown;
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;

        // No card in hand, no Minor Arcana yet
        var cardService = AstraeaTestContext.CreateMockCardService(
            hasCard: false,
            hasLord: false,
            hasLady: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Draw actions fail (no regular cards)
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a =>
                    a.ActionId == ASTActions.AstralDraw.ActionId ||
                    a.ActionId == ASTActions.UmbralDraw.ActionId),
                It.IsAny<ulong>()))
            .Returns(false);
        actionService.Setup(x => x.IsActionReady(ASTActions.MinorArcana.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.MinorArcana.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 70);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.MinorArcana.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_MinorArcana_Disabled_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = false;
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(false);
        actionService.Setup(x => x.IsActionReady(ASTActions.MinorArcana.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 70);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.MinorArcana.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_MinorArcana_AlreadyHaveMinorArcana_DoesNotDrawAgain()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.OnCooldown;
        config.Astrologian.EnableDivination = false;
        config.Astrologian.EnableAstrodyne = false;

        // Already have Lord in hand
        var cardService = AstraeaTestContext.CreateMockCardService(
            hasCard: false,
            hasLord: true,
            minorArcana: ASTActions.CardType.Lord);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(false);
        actionService.Setup(x => x.IsActionReady(ASTActions.MinorArcana.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            actionService: actionService,
            canExecuteOgcd: true,
            inCombat: true,
            level: 70);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.MinorArcana.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion
}
