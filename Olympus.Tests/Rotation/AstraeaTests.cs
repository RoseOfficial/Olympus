using Moq;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Astraea (Astrologian) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AstraeaTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_CardIsHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(card.Priority < resurrection.Priority);
        Assert.True(card.Priority < healing.Priority);
        Assert.True(card.Priority < defensive.Priority);
        Assert.True(card.Priority < buff.Priority);
        Assert.True(card.Priority < damage.Priority);
    }

    [Fact]
    public void ModulePriorities_ResurrectionBeforeHealing()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();

        Assert.True(resurrection.Priority < healing.Priority);
    }

    [Fact]
    public void ModulePriorities_HealingBeforeDefensive()
    {
        var healing = new HealingModule();
        var defensive = new DefensiveModule();

        Assert.True(healing.Priority < defensive.Priority);
    }

    [Fact]
    public void ModulePriorities_DefensiveBeforeBuff()
    {
        var defensive = new DefensiveModule();
        var buff = new BuffModule();

        Assert.True(defensive.Priority < buff.Priority);
    }

    [Fact]
    public void ModulePriorities_BuffBeforeDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        Assert.Equal(3, new CardModule().Priority);
        Assert.Equal(5, new ResurrectionModule().Priority);
        Assert.Equal(10, new HealingModule().Priority);
        Assert.Equal(20, new DefensiveModule().Priority);
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Card", new CardModule().Name);
        Assert.Equal("Resurrection", new ResurrectionModule().Name);
        Assert.Equal("Healing", new HealingModule().Name);
        Assert.Equal("Defensive", new DefensiveModule().Name);
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void AstraeaDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AstraeaDebugState();

        Assert.Equal("Idle", debugState.PlanningState);
        Assert.Equal("None", debugState.PlannedAction);
        Assert.Equal("Idle", debugState.DpsState);
        Assert.Equal("None", debugState.CurrentCardType);
        Assert.Equal("Idle", debugState.CardState);
        Assert.Equal("Idle", debugState.DrawState);
        Assert.Equal("Not Placed", debugState.EarthlyStarState);
    }

    [Fact]
    public void AstraeaDebugState_CanBeModified()
    {
        var debugState = new AstraeaDebugState
        {
            CurrentCardType = "The Balance",
            CardState = "Ready to play",
            SealCount = 3,
            UniqueSealCount = 3
        };

        Assert.Equal("The Balance", debugState.CurrentCardType);
        Assert.Equal("Ready to play", debugState.CardState);
        Assert.Equal(3, debugState.SealCount);
        Assert.Equal(3, debugState.UniqueSealCount);
    }

    [Fact]
    public void AstraeaDebugState_EarthlyStarFields_DefaultToIdle()
    {
        var debugState = new AstraeaDebugState();

        Assert.Equal("Not Placed", debugState.EarthlyStarState);
        Assert.Equal(0f, debugState.StarTimeRemaining);
        Assert.False(debugState.IsStarMature);
        Assert.Equal(0, debugState.StarTargetsInRange);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AstraeaContext_StoresPlayerReference()
    {
        var context = AstraeaTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void AstraeaContext_TracksCombatState()
    {
        var context = AstraeaTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AstraeaTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AstraeaContext_TracksGcdState()
    {
        var context = AstraeaTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AstraeaTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AstraeaContext_HasDebugState()
    {
        var context = AstraeaTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AstraeaContext_ConfigurationIsAccessible()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.BeneficThreshold = 0.45f;

        var context = AstraeaTestContext.Create(config: config);

        Assert.Equal(0.45f, context.Configuration.Astrologian.BeneficThreshold);
    }

    [Fact]
    public void AstraeaContext_CurrentCardIsAccessible()
    {
        var cardService = AstraeaTestContext.CreateMockCardService(
            hasCard: true,
            currentCard: Olympus.Data.ASTActions.CardType.TheBalance);

        var context = AstraeaTestContext.Create(cardService: cardService);

        Assert.Equal(Olympus.Data.ASTActions.CardType.TheBalance, context.CurrentCard);
    }

    [Fact]
    public void AstraeaContext_HasCardReflectsCardState()
    {
        var contextWithCard = AstraeaTestContext.Create(hasCard: true);
        Assert.True(contextWithCard.HasCard);

        var contextNoCard = AstraeaTestContext.Create(hasCard: false);
        Assert.False(contextNoCard.HasCard);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = AstraeaTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDamageDisabled()
    {
        var module = new DamageModule();
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableOracle = false;

        var enemy = new Moq.Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var context = AstraeaTestContext.Create(
            config: config,
            targetingService: targetingService,
            inCombat: true,
            canExecuteGcd: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void HealingModule_ReturnsFalse_WhenNoGcdOrOgcdAvailable()
    {
        var module = new HealingModule();
        var context = AstraeaTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void ResurrectionModule_ReturnsFalse_WhenRaiseDisabled()
    {
        var module = new ResurrectionModule();
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = false;

        var context = AstraeaTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void CardModule_ReturnsFalse_WhenNoCardAndNoOgcd()
    {
        var module = new CardModule();
        var context = AstraeaTestContext.Create(
            hasCard: false,
            canExecuteOgcd: false,
            canExecuteGcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_EnableCards_IsTrue_ByDefault()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableCards);
    }

    [Fact]
    public void Configuration_EnableDivination_IsTrue_ByDefault()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableDivination);
    }

    [Fact]
    public void Configuration_DefaultAstrologianConfiguration_HasHealingEnabled()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableBenefic);
        Assert.True(config.Astrologian.EnableBeneficII);
        Assert.True(config.Astrologian.EnableEssentialDignity);
    }

    #endregion
}
