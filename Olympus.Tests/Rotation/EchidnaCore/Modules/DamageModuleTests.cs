using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Helpers;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.EchidnaCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region GCD — Reawaken Sequence

    [Fact]
    public void TryExecute_Reawaken_FiresWithSufficientOfferings()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            serpentOffering: 50,        // Enough to enter Reawaken
            isReawakened: false,
            hasHuntersInstinct: true,
            huntersInstinctRemaining: 20f,
            hasSwiftscaled: true,
            swiftscaledRemaining: 20f,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Reawaken_FiresWithReadyToReawaken()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            serpentOffering: 0,         // Not enough normally, but has Ready to Reawaken
            hasReadyToReawaken: true,
            isReawakened: false,
            hasHuntersInstinct: true,
            huntersInstinctRemaining: 20f,
            hasSwiftscaled: true,
            swiftscaledRemaining: 20f,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Reawaken_SkipsWhenInsufficientOfferings()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SteelFangs.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SteelFangs.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            serpentOffering: 40,        // Below 50 minimum
            hasReadyToReawaken: false,
            isReawakened: false,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Reawaken_SkipsWithoutHuntersInstinct()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Reawaken.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SteelFangs.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SteelFangs.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            serpentOffering: 50,
            isReawakened: false,
            hasHuntersInstinct: false,  // Missing Hunter's Instinct
            hasSwiftscaled: true,
            swiftscaledRemaining: 20f,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Reawaken.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Reawaken Generation Sequence

    [Fact]
    public void TryExecute_FirstGeneration_FiresAtFiveTribute()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.FirstGeneration.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FirstGeneration.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: true,         // In Reawaken state
            anguineTribute: 5,          // First Generation uses 5 tribute
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FirstGeneration.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Ouroboros_FiresAtOneTribute()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Ouroboros.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Ouroboros.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: true,
            anguineTribute: 1,          // Final tribute → Ouroboros
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Ouroboros.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region GCD — Twinblade Combo

    [Fact]
    public void TryExecute_HuntersCoil_FiresWhenDreadwindyReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.HuntersCoil.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HuntersCoil.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            serpentOffering: 0,
            dreadCombo: VPRActions.DreadCombo.DreadwindyReady, // In twinblade combo
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HuntersCoil.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HuntersCoil_SkipsWhenDreadComboIsNone()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.HuntersCoil.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SteelFangs.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SteelFangs.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            dreadCombo: VPRActions.DreadCombo.None, // No twinblade combo
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HuntersCoil.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Vicewinder

    [Fact]
    public void TryExecute_Vicewinder_FiresWhenNoxiousGnashMissing()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Vicewinder.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Vicewinder.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            serpentOffering: 0,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: false, // No debuff → Vicewinder fires
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Vicewinder.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region GCD — Dual Wield Combo

    [Fact]
    public void TryExecute_SteelFangs_FiresAsComboStarter()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SteelFangs.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SteelFangs.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            serpentOffering: 0,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            comboStep: 0,   // No combo in progress
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SteelFangs.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HuntersSting_FiresAfterSteelFangs()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.HuntersSting.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HuntersSting.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            serpentOffering: 0,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            comboStep: 1,
            lastComboAction: VPRActions.SteelFangs.ActionId, // After Steel Fangs → Hunter's Sting
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HuntersSting.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HindstingStrike_FiresWithHindstungVenom()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.HindstingStrike.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HindstingStrike.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isReawakened: false,
            serpentOffering: 0,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            comboStep: 2,
            lastComboAction: VPRActions.HuntersSting.ActionId, // From Hunter's Sting path
            hasHindstungVenom: true, // Hindstung → use rear (HindstingStrike)
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.HindstingStrike.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region oGCD — Poised Follow-ups

    [Fact]
    public void TryExecute_Twinfang_FiresWhenPoisedForTwinfang()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Twinfang.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Twinfang.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            hasPoisedForTwinfang: true, // Twinfang proc active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Twinfang.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Twinblood_FiresWhenPoisedForTwinblood()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Twinblood.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Twinblood.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            hasPoisedForTwinfang: false,
            hasPoisedForTwinblood: true, // Twinblood proc active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Twinblood.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Twinfang_SkipsWhenNoPoisedProc()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.Twinfang.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            hasPoisedForTwinfang: false,  // No proc
            hasPoisedForTwinblood: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Twinfang.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region oGCD — Legacy During Reawaken

    [Fact]
    public void TryExecute_FirstLegacy_FiresWhenSerpentComboIsFirstLegacy()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.FirstLegacy.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FirstLegacy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: true,
            anguineTribute: 4,
            serpentCombo: VPRActions.SerpentCombo.FirstLegacy,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FirstLegacy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SecondLegacy_FiresWhenSerpentComboIsSecondLegacy()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondLegacy.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SecondLegacy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: true,
            anguineTribute: 3,
            serpentCombo: VPRActions.SerpentCombo.SecondLegacy,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SecondLegacy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ThirdLegacy_FiresWhenSerpentComboIsThirdLegacy()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.ThirdLegacy.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.ThirdLegacy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: true,
            anguineTribute: 2,
            serpentCombo: VPRActions.SerpentCombo.ThirdLegacy,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.ThirdLegacy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FourthLegacy_FiresWhenSerpentComboIsFourthLegacy()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.FourthLegacy.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FourthLegacy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: true,
            anguineTribute: 1,
            serpentCombo: VPRActions.SerpentCombo.FourthLegacy,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.FourthLegacy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Legacy_SkipsWhenSerpentComboIsNone()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: true,
            anguineTribute: 4,
            serpentCombo: VPRActions.SerpentCombo.None,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a =>
                a.ActionId == VPRActions.FirstLegacy.ActionId ||
                a.ActionId == VPRActions.SecondLegacy.ActionId ||
                a.ActionId == VPRActions.ThirdLegacy.ActionId ||
                a.ActionId == VPRActions.FourthLegacy.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region oGCD — Death Rattle / Last Lash

    [Fact]
    public void TryExecute_DeathRattle_FiresWhenSerpentComboIsDeathRattle()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.DeathRattle.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.DeathRattle.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: false,
            serpentCombo: VPRActions.SerpentCombo.DeathRattle,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.DeathRattle.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LastLash_FiresWhenSerpentComboIsLastLash()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.LastLash.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.LastLash.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            isReawakened: false,
            serpentCombo: VPRActions.SerpentCombo.LastLash,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.LastLash.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_DeathRattle_SkipsAtLowLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 91,
            serpentCombo: VPRActions.SerpentCombo.DeathRattle,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.DeathRattle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region oGCD — Role Actions

    [Fact]
    public void TryExecute_SecondWind_FiresWhenHpLow()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondWind.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SecondWind.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSecondWind = true;
        config.Viper.SecondWindHpThreshold = 0.5f;

        // Player at 30% HP — below 50% threshold
        var player = MockBuilders.CreateMockPlayerCharacter(level: 100, currentHp: 3000, maxHp: 10000);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var context = EchidnaTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            config: config,
            actionService: actionService,
            targetingService: targeting);

        // Override player HP via a fresh context with the custom player
        var mockContext = new Mock<IEchidnaContext>();
        mockContext.Setup(x => x.InCombat).Returns(true);
        mockContext.Setup(x => x.CanExecuteGcd).Returns(false);
        mockContext.Setup(x => x.CanExecuteOgcd).Returns(true);
        mockContext.Setup(x => x.Player).Returns(player.Object);
        mockContext.Setup(x => x.Configuration).Returns(config);
        mockContext.Setup(x => x.ActionService).Returns(actionService.Object);
        mockContext.Setup(x => x.TargetingService).Returns(targeting.Object);
        mockContext.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mockContext.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mockContext.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);
        mockContext.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mockContext.Setup(x => x.IsReawakened).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mockContext.Setup(x => x.RattlingCoils).Returns(0);
        mockContext.Setup(x => x.SerpentOffering).Returns(0);
        mockContext.Setup(x => x.AnguineTribute).Returns(0);
        mockContext.Setup(x => x.HasNoxiousGnash).Returns(true);
        mockContext.Setup(x => x.NoxiousGnashRemaining).Returns(15f);
        mockContext.Setup(x => x.StatusHelper).Returns(new EchidnaStatusHelper());
        mockContext.Setup(x => x.IsAtRear).Returns(false);
        mockContext.Setup(x => x.IsAtFlank).Returns(false);
        mockContext.Setup(x => x.HasTrueNorth).Returns(false);
        mockContext.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mockContext.Setup(x => x.PartyList).Returns(MockBuilders.CreateMockPartyList().Object);

        var result = _module.TryExecute(mockContext.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SecondWind.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SecondWind_SkipsWhenHpFull()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondWind.ActionId)).Returns(true);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSecondWind = true;
        config.Viper.SecondWindHpThreshold = 0.5f;

        // Player at 100% HP — above threshold, should not fire
        var player = MockBuilders.CreateMockPlayerCharacter(level: 100, currentHp: 10000, maxHp: 10000);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mockContext = new Mock<IEchidnaContext>();
        mockContext.Setup(x => x.InCombat).Returns(true);
        mockContext.Setup(x => x.CanExecuteGcd).Returns(false);
        mockContext.Setup(x => x.CanExecuteOgcd).Returns(true);
        mockContext.Setup(x => x.Player).Returns(player.Object);
        mockContext.Setup(x => x.Configuration).Returns(config);
        mockContext.Setup(x => x.ActionService).Returns(actionService.Object);
        mockContext.Setup(x => x.TargetingService).Returns(targeting.Object);
        mockContext.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mockContext.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mockContext.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);
        mockContext.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mockContext.Setup(x => x.IsReawakened).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mockContext.Setup(x => x.RattlingCoils).Returns(0);
        mockContext.Setup(x => x.SerpentOffering).Returns(0);
        mockContext.Setup(x => x.AnguineTribute).Returns(0);
        mockContext.Setup(x => x.HasNoxiousGnash).Returns(true);
        mockContext.Setup(x => x.NoxiousGnashRemaining).Returns(15f);
        mockContext.Setup(x => x.StatusHelper).Returns(new EchidnaStatusHelper());
        mockContext.Setup(x => x.IsAtRear).Returns(false);
        mockContext.Setup(x => x.IsAtFlank).Returns(false);
        mockContext.Setup(x => x.HasTrueNorth).Returns(false);
        mockContext.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mockContext.Setup(x => x.PartyList).Returns(MockBuilders.CreateMockPartyList().Object);

        _module.TryExecute(mockContext.Object, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SecondWind.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Bloodbath_FiresWhenHpLow()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Disable SecondWind so Bloodbath can fire
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondWind.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.Bloodbath.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Bloodbath.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSecondWind = false;
        config.Viper.EnableBloodbath = true;
        config.Viper.BloodbathHpThreshold = 0.7f;

        // Player at 50% HP — below 70% threshold, no Bloodbath buff
        var player = MockBuilders.CreateMockPlayerCharacter(level: 100, currentHp: 5000, maxHp: 10000);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mockContext = new Mock<IEchidnaContext>();
        mockContext.Setup(x => x.InCombat).Returns(true);
        mockContext.Setup(x => x.CanExecuteGcd).Returns(false);
        mockContext.Setup(x => x.CanExecuteOgcd).Returns(true);
        mockContext.Setup(x => x.Player).Returns(player.Object);
        mockContext.Setup(x => x.Configuration).Returns(config);
        mockContext.Setup(x => x.ActionService).Returns(actionService.Object);
        mockContext.Setup(x => x.TargetingService).Returns(targeting.Object);
        mockContext.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mockContext.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mockContext.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);
        mockContext.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mockContext.Setup(x => x.IsReawakened).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mockContext.Setup(x => x.RattlingCoils).Returns(0);
        mockContext.Setup(x => x.SerpentOffering).Returns(0);
        mockContext.Setup(x => x.AnguineTribute).Returns(0);
        mockContext.Setup(x => x.HasNoxiousGnash).Returns(true);
        mockContext.Setup(x => x.NoxiousGnashRemaining).Returns(15f);
        mockContext.Setup(x => x.StatusHelper).Returns(new EchidnaStatusHelper());
        mockContext.Setup(x => x.IsAtRear).Returns(false);
        mockContext.Setup(x => x.IsAtFlank).Returns(false);
        mockContext.Setup(x => x.HasTrueNorth).Returns(false);
        mockContext.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mockContext.Setup(x => x.PartyList).Returns(MockBuilders.CreateMockPartyList().Object);

        var result = _module.TryExecute(mockContext.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Bloodbath.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Feint_FiresWhenEnabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Disable SecondWind and Bloodbath so Feint can fire
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondWind.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.Bloodbath.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.Feint.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Feint.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSecondWind = false;
        config.Viper.EnableBloodbath = false;
        config.Viper.EnableFeint = true;

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mockContext = new Mock<IEchidnaContext>();
        mockContext.Setup(x => x.InCombat).Returns(true);
        mockContext.Setup(x => x.CanExecuteGcd).Returns(false);
        mockContext.Setup(x => x.CanExecuteOgcd).Returns(true);
        mockContext.Setup(x => x.Player).Returns(player.Object);
        mockContext.Setup(x => x.Configuration).Returns(config);
        mockContext.Setup(x => x.ActionService).Returns(actionService.Object);
        mockContext.Setup(x => x.TargetingService).Returns(targeting.Object);
        mockContext.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mockContext.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mockContext.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);
        mockContext.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mockContext.Setup(x => x.IsReawakened).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mockContext.Setup(x => x.RattlingCoils).Returns(0);
        mockContext.Setup(x => x.SerpentOffering).Returns(0);
        mockContext.Setup(x => x.AnguineTribute).Returns(0);
        mockContext.Setup(x => x.HasNoxiousGnash).Returns(true);
        mockContext.Setup(x => x.NoxiousGnashRemaining).Returns(15f);
        mockContext.Setup(x => x.StatusHelper).Returns(new EchidnaStatusHelper());
        mockContext.Setup(x => x.IsAtRear).Returns(false);
        mockContext.Setup(x => x.IsAtFlank).Returns(false);
        mockContext.Setup(x => x.HasTrueNorth).Returns(false);
        mockContext.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mockContext.Setup(x => x.PartyList).Returns(MockBuilders.CreateMockPartyList().Object);

        var result = _module.TryExecute(mockContext.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.Feint.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TrueNorth_FiresWhenOutOfPositionAndEnabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Disable higher-priority role actions so TrueNorth can fire
        actionService.Setup(x => x.IsActionReady(VPRActions.SecondWind.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.Bloodbath.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.Feint.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(VPRActions.TrueNorth.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.TrueNorth.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSecondWind = false;
        config.Viper.EnableBloodbath = false;
        config.Viper.EnableFeint = false;
        config.Viper.EnableTrueNorth = true;

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mockContext = new Mock<IEchidnaContext>();
        mockContext.Setup(x => x.InCombat).Returns(true);
        mockContext.Setup(x => x.CanExecuteGcd).Returns(false);
        mockContext.Setup(x => x.CanExecuteOgcd).Returns(true);
        mockContext.Setup(x => x.Player).Returns(player.Object);
        mockContext.Setup(x => x.Configuration).Returns(config);
        mockContext.Setup(x => x.ActionService).Returns(actionService.Object);
        mockContext.Setup(x => x.TargetingService).Returns(targeting.Object);
        mockContext.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mockContext.Setup(x => x.Debug).Returns(new EchidnaDebugState());
        mockContext.Setup(x => x.SerpentCombo).Returns(VPRActions.SerpentCombo.None);
        mockContext.Setup(x => x.DreadCombo).Returns(VPRActions.DreadCombo.None);
        mockContext.Setup(x => x.IsReawakened).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinfang).Returns(false);
        mockContext.Setup(x => x.HasPoisedForTwinblood).Returns(false);
        mockContext.Setup(x => x.RattlingCoils).Returns(0);
        mockContext.Setup(x => x.SerpentOffering).Returns(0);
        mockContext.Setup(x => x.AnguineTribute).Returns(0);
        mockContext.Setup(x => x.HasNoxiousGnash).Returns(true);
        mockContext.Setup(x => x.NoxiousGnashRemaining).Returns(15f);
        mockContext.Setup(x => x.StatusHelper).Returns(new EchidnaStatusHelper());
        mockContext.Setup(x => x.IsAtRear).Returns(false);
        mockContext.Setup(x => x.IsAtFlank).Returns(false);
        mockContext.Setup(x => x.HasTrueNorth).Returns(false);  // No TrueNorth buff active
        mockContext.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mockContext.Setup(x => x.PartyList).Returns(MockBuilders.CreateMockPartyList().Object);

        var result = _module.TryExecute(mockContext.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.TrueNorth.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region GCD — Writhing Snap

    [Fact]
    public void TryExecute_WrithingSnap_FiresWhenMovingAndNoCoils()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.WrithingSnap.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.WrithingSnap.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            rattlingCoils: 0,
            isReawakened: false,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            serpentOffering: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.WrithingSnap.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WrithingSnap_SkipsWhenHasRattlingCoils()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.UncoiledFury.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.UncoiledFury.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            rattlingCoils: 1,
            isReawakened: false,
            dreadCombo: VPRActions.DreadCombo.None,
            hasNoxiousGnash: true,
            noxiousGnashRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: true);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.WrithingSnap.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy()
    {
        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(1UL);
        enemy.Setup(x => x.Name).Returns(new Dalamud.Game.Text.SeStringHandling.SeString());
        enemy.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return enemy;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        return targeting;
    }

    private static IEchidnaContext CreateContext(
        bool inCombat,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        byte level = 100,
        int serpentOffering = 0,
        int anguineTribute = 0,
        int rattlingCoils = 0,
        bool isReawakened = false,
        VPRActions.DreadCombo dreadCombo = VPRActions.DreadCombo.None,
        VPRActions.SerpentCombo serpentCombo = VPRActions.SerpentCombo.None,
        bool hasHuntersInstinct = false,
        float huntersInstinctRemaining = 0f,
        bool hasSwiftscaled = false,
        float swiftscaledRemaining = 0f,
        bool hasReadyToReawaken = false,
        bool hasPoisedForTwinfang = false,
        bool hasPoisedForTwinblood = false,
        bool hasHindstungVenom = false,
        bool hasNoxiousGnash = false,
        float noxiousGnashRemaining = 0f,
        int comboStep = 0,
        uint lastComboAction = 0,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return EchidnaTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            comboStep: comboStep,
            lastComboAction: lastComboAction,
            serpentOffering: serpentOffering,
            anguineTribute: anguineTribute,
            rattlingCoils: rattlingCoils,
            isReawakened: isReawakened,
            dreadCombo: dreadCombo,
            serpentCombo: serpentCombo,
            hasHuntersInstinct: hasHuntersInstinct,
            huntersInstinctRemaining: huntersInstinctRemaining,
            hasSwiftscaled: hasSwiftscaled,
            swiftscaledRemaining: swiftscaledRemaining,
            hasReadyToReawaken: hasReadyToReawaken,
            hasPoisedForTwinfang: hasPoisedForTwinfang,
            hasPoisedForTwinblood: hasPoisedForTwinblood,
            hasHindstungVenom: hasHindstungVenom,
            hasNoxiousGnash: hasNoxiousGnash,
            noxiousGnashRemaining: noxiousGnashRemaining,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
