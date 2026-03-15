using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar HealingModule coordinator logic.
/// Covers oGCD/GCD routing, Aetherflow resource management, AoE healing,
/// movement constraints, and an AoE regression test to prevent v4.10.7-class counting bugs.
/// Individual handler behavior is tested in AthenaCore/Modules/Healing/ test files.
/// </summary>
public class HealingModuleTests
{
    private readonly HealingModule _module;

    public HealingModuleTests()
    {
        _module = new HealingModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is10()
    {
        Assert.Equal(10, _module.Priority);
    }

    [Fact]
    public void Name_IsHealing()
    {
        Assert.Equal("Healing", _module.Name);
    }

    #endregion

    #region General Healing State Tests

    [Fact]
    public void TryExecute_HealingDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.EnableHealing = false;

        var context = AthenaTestContext.Create(
            config: config,
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoWindowAvailable_ReturnsFalse()
    {
        // Neither GCD nor oGCD window — nothing can fire
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();

        var context = AthenaTestContext.Create(
            config: config,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Lustrate Tests — Aetherflow oGCD Heal

    [Fact]
    public void TryExecute_LustrateDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableRecitation = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000); // 30% HP
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Lustrate.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LustrateReady_InjuredTarget_Executes()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 0;

        // Member at 30% HP — below the 55% threshold
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Lustrate.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Lustrate.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Lustrate_NoAetherflow_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 0;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        // Zero Aetherflow stacks — cannot use Lustrate
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(0);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            aetherflowStacks: 0);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Lustrate.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Lustrate_TargetAboveThreshold_DoesNotFire()
    {
        // Member at 80% HP — above the 55% threshold
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 0;

        var healthyMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 40000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { healthyMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Lustrate.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region AoE Heal Tests — Succor/Indomitability

    [Fact]
    public void TryExecute_AoEHealDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSuccor = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableRecitation = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_AoEHeal_Moving_ReturnsFalse()
    {
        // Succor has a cast time — cannot use while moving
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSuccor = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableRecitation = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Moving blocks GCD AoE heals
        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
    }

    #endregion

    #region AoE Regression — Real Party Counting (v4.10.7 class fix)

    /// <summary>
    /// Regression test: confirms AoE heal fires correctly when enough members are injured.
    /// Uses a real TestableAthenaPartyHelper so actual counting logic is exercised.
    /// This test class prevents v4.10.7-class bugs where mocked counts bypassed real logic.
    /// </summary>
    [Fact]
    public void AoEHeal_RealPartyHelper_8Members3Injured_ThresholdMet_TriesAoEHeal()
    {
        // 8-person raid: 5 healthy (96% HP), 3 injured (50% HP)
        // AoEHealMinTargets = 3 → threshold is met, module should attempt AoE heal
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSuccor = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AoEHealThreshold = 0.80f;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 5, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);
        actionService.Setup(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // With real counting, 3 injured members meets the AoEHealMinTargets = 3 threshold
        Assert.True(result);
    }

    [Fact]
    public void AoEHeal_RealPartyHelper_4ManDungeon_3Injured_ThresholdMet_TriesAoEHeal()
    {
        // 4-man dungeon: 1 healthy (96% HP), 3 injured (50% HP)
        // AoEHealMinTargets = 3 → threshold met, module should attempt AoE heal
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSuccor = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AoEHealThreshold = 0.80f;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);
        actionService.Setup(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // Real party counting: 3 injured meets the AoEHealMinTargets = 3 threshold
        Assert.True(result);
    }

    [Fact]
    public void AoEHeal_RealPartyHelper_8Members2Injured_BelowThreshold_DoesNotAoEHeal()
    {
        // Only 2 injured members — below AoEHealMinTargets = 3, should not fire AoE heal
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSuccor = true;
        config.Scholar.EnableExcogitation = false;
        config.Scholar.EnableLustrate = false;
        config.Scholar.EnableIndomitability = false;
        config.Scholar.EnableRecitation = false;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AoEHealThreshold = 0.80f;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 6, injuredCount: 2, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // With real counting, 2 injured is below the minimum of 3
        Assert.False(result);
    }

    #endregion

    #region Routing Tests — oGCD vs GCD list isolation

    [Fact]
    public void TryExecute_WhenCanExecuteOgcdOnly_DoesNotRunGcdHandlers()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        var context = AthenaTestContext.Create(actionService: actionService,
            canExecuteGcd: false, canExecuteOgcd: true);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_WhenCanExecuteGcdOnly_DoesNotRunOgcdHandlers()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        var context = AthenaTestContext.Create(actionService: actionService,
            canExecuteGcd: true, canExecuteOgcd: false);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
