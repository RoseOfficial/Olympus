using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Scholar;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar BuffModule logic.
/// Covers Lucid Dreaming (MP management) and Dissipation (Aetherflow + healing buff).
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

    #region Lucid Dreaming Tests

    [Fact]
    public void TryExecute_LucidDreamingDisabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableLucidDreaming = false;
        config.Scholar.EnableDissipation = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            currentMp: 5000, // 50% MP — below 70% threshold
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_MpAboveThreshold_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableLucidDreaming = true;
        config.HealerShared.LucidDreamingThreshold = 0.70f;
        config.Scholar.EnableDissipation = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            currentMp: 9000, // 90% MP — above 70% threshold
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_MpBelowThreshold_Executes()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableLucidDreaming = true;
        config.HealerShared.LucidDreamingThreshold = 0.70f;
        config.Scholar.EnableDissipation = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            currentMp: 5000, // 50% MP — below 70% threshold
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NotInCombat_DoesNotFire()
    {
        // SCH BuffModule requires combat
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableLucidDreaming = true;
        config.HealerShared.LucidDreamingThreshold = 0.70f;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: false, // Out of combat
            currentMp: 5000,
            maxMp: 10000);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Dissipation Tests

    [Fact]
    public void TryExecute_DissipationDisabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableDissipation = false;
        config.HealerShared.EnableLucidDreaming = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(0);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            aetherflowStacks: 0);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Dissipation.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Dissipation_FairyNotPresent_DoesNotFire()
    {
        // Cannot use Dissipation without a fairy to dismiss
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableDissipation = true;
        config.HealerShared.EnableLucidDreaming = false;
        config.Scholar.DissipationMaxFairyGauge = 30;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Dissipation.ActionId)).Returns(true);

        // No fairy present
        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.None);
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(0);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            aetherflowStacks: 0,
            fairyGauge: 10);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Dissipation.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Dissipation_AlreadyHasStacks_DoesNotFire()
    {
        // Dissipation should not be used if there are already Aetherflow stacks (would waste it)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableDissipation = true;
        config.HealerShared.EnableLucidDreaming = false;
        config.Scholar.DissipationMaxFairyGauge = 30;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Dissipation.ActionId)).Returns(true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);
        // Already has 2 stacks — should not use Dissipation
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(2);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            aetherflowStacks: 2,
            fairyGauge: 10);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Dissipation.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
