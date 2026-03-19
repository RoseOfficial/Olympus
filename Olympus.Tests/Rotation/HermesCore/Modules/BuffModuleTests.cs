using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    #region Guard Conditions

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_MudraActive_ReturnsFalse()
    {
        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isMudraActive: true);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Mudra active", context.Debug.BuffState);
    }

    #endregion

    #region Kunai's Bane / Trick Attack

    [Fact]
    public void TryExecute_KunaisBane_FiresWhenSuitonUp()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasSuiton: true,
            suitonRemaining: 15f,
            hasTenriJindoReady: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_KunaisBane_SkipsWhenNoSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasSuiton: false, // No Suiton
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TrickAttack_UsedBelowLevel92()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.TrickAttack.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.TrickAttack.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 60, // Below KunaisBane MinLevel (92)
            hasSuiton: true,
            hasTenriJindoReady: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TrickAttack.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Tenri Jindo

    [Fact]
    public void TryExecute_TenriJindo_FiresWhenReadyAtLevel100()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenriJindo.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasTenriJindoReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenriJindo.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenriJindo_SkipsWhenNotReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasTenriJindoReady: false, // Not ready
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenriJindo.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TenriJindo_SkipsBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 90, // Below TenriJindo MinLevel (100)
            hasTenriJindoReady: true,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenriJindo.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Kassatsu

    [Fact]
    public void TryExecute_Kassatsu_FiresWhenAvailable()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Tenri Jindo not ready, KunaisBane/Suiton not up, Mug/Dokumori not ready
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Mug.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Dokumori.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Kassatsu.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Kassatsu.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasKassatsu: false, // Not active
            hasSuiton: false,
            hasTenriJindoReady: false,
            hasDokumoriOnTarget: true, // Prevent Dokumori from firing
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Kassatsu.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Kassatsu_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasKassatsu: true, // Already active
            hasSuiton: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Kassatsu.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Kassatsu_SkipsBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 40, // Below Kassatsu MinLevel (50)
            hasKassatsu: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Kassatsu.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Bunshin

    [Fact]
    public void TryExecute_Bunshin_FiresWhenNinki50OrMore()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Clear path to Bunshin: no tenri, no kunais, no mug, no kassatsu, no tcj
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Dokumori.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Kassatsu.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.TenChiJin.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Bunshin.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bunshin.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            ninki: 50, // At threshold
            hasBunshin: false,
            hasSuiton: false,
            hasTenriJindoReady: false,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasDokumoriOnTarget: true, // Prevent Dokumori
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bunshin.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Bunshin_SkipsWhenNinkiBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            ninki: 40, // Below threshold (50)
            hasBunshin: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bunshin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Bunshin_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            ninki: 100,
            hasBunshin: true, // Already active
            bunshinStacks: 5,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bunshin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Bunshin_SkipsBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 70, // Below Bunshin MinLevel (80)
            ninki: 100,
            hasBunshin: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bunshin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Ten Chi Jin

    [Fact]
    public void TryExecute_TenChiJin_FiresWhenAvailableAndNotMoving()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Clear path to TCJ: block all higher priority actions
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Dokumori.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Kassatsu.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.TenChiJin.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenChiJin.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isMoving: false,
            level: 100,
            hasTenChiJin: false,
            hasSuiton: false,
            hasTenriJindoReady: false,
            hasKassatsu: false,
            hasDokumoriOnTarget: true, // Prevent Dokumori
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenChiJin.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenChiJin_SkipsWhenMoving()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isMoving: true, // Moving — TCJ should be skipped
            level: 100,
            hasTenChiJin: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: true);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenChiJin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TenChiJin_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasTenChiJin: true, // Already active
            tenChiJinStacks: 3,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenChiJin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Meisui

    [Fact]
    public void TryExecute_Meisui_FiresWhenSuitonUpAndKunaisBaneOnCooldown()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // All higher priority actions blocked
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false); // KunaisBane on CD
        actionService.Setup(x => x.IsActionReady(NINActions.Dokumori.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Kassatsu.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.TenChiJin.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Bunshin.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(NINActions.Meisui.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Meisui.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasSuiton: true, // Need Suiton for Meisui
            hasTenriJindoReady: false,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasBunshin: true, // Prevent Bunshin from triggering
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Meisui.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Meisui_SkipsWhenKunaisBaneReady_SaveSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // KunaisBane IS ready — TryMeisui's KunaisBaneReady guard must be what prevents Meisui
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(NINActions.Meisui.ActionId)).Returns(true);
        // Explicitly stub KunaisBane's ExecuteOgcd to return false so TryKunaisBane falls
        // through (Suiton is up but KunaisBane fails to fire), letting execution reach TryMeisui.
        // If Meisui's guard were removed, Meisui would fire and the Assert.False below would fail.
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
                It.IsAny<ulong>()))
            .Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Meisui.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasSuiton: true,
            hasTenriJindoReady: false,
            actionService: actionService,
            targetingService: targeting);

        // TryMeisui's KunaisBaneReady guard returns false before reaching ExecuteOgcd(Meisui).
        // If the guard were removed, ExecuteOgcd(Meisui) returns true → TryExecute returns true
        // → the Assert.False below would fail, catching the regression.
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Meisui.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Meisui_SkipsWhenNoSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasSuiton: false, // No Suiton
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Meisui.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Config Toggle Disable Tests

    [Fact]
    public void TryExecute_Mug_WhenDisabled_NeverFires()
    {
        // At level 66+, Mug is replaced by Dokumori. EnableMug = false disables both.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableMug = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Dokumori.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSuiton: true,
            suitonRemaining: 20f,
            hasTenriJindoReady: false,
            level: 66,
            actionService: actionService,
            targetingService: targeting,
            config: config);

        _module.TryExecute(context, isMoving: false);

        // Both Mug and Dokumori should never fire
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Mug.ActionId || a.ActionId == NINActions.Dokumori.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TenriJindo_WhenDisabled_NeverFires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableTenriJindo = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.TenriJindo.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTenriJindoReady: true,
            level: 100,
            actionService: actionService,
            targetingService: targeting,
            config: config);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.TenriJindo.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    private static IHermesContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        bool canExecuteGcd = false,
        bool isMoving = false,
        byte level = 100,
        int ninki = 0,
        bool isMudraActive = false,
        bool hasKassatsu = false,
        bool hasTenChiJin = false,
        int tenChiJinStacks = 0,
        bool hasSuiton = false,
        float suitonRemaining = 0f,
        bool hasBunshin = false,
        int bunshinStacks = 0,
        bool hasTenriJindoReady = false,
        bool hasDokumoriOnTarget = false,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Configuration? config = null)
    {
        return HermesTestContext.Create(
            config: config,
            level: level,
            inCombat: inCombat,
            isMoving: isMoving,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            ninki: ninki,
            isMudraActive: isMudraActive,
            hasKassatsu: hasKassatsu,
            hasTenChiJin: hasTenChiJin,
            tenChiJinStacks: tenChiJinStacks,
            hasSuiton: hasSuiton,
            suitonRemaining: suitonRemaining,
            hasBunshin: hasBunshin,
            bunshinStacks: bunshinStacks,
            hasTenriJindoReady: hasTenriJindoReady,
            hasDokumoriOnTarget: hasDokumoriOnTarget,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
