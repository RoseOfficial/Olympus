using System.Collections.Generic;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Scholar;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar FairyModule logic.
/// Covers fairy summon, Seraphism, Summon Seraph, Whispering Dawn, and Fey Blessing.
/// </summary>
public class FairyModuleTests
{
    private readonly FairyModule _module;

    public FairyModuleTests()
    {
        _module = new FairyModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is3()
    {
        Assert.Equal(3, _module.Priority);
    }

    [Fact]
    public void Name_IsFairy()
    {
        Assert.Equal("Fairy", _module.Name);
    }

    #endregion

    #region Fairy Summon Tests

    [Fact]
    public void TryExecute_FairyAbsentAutoSummonEnabled_TrySummonFairy()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.SummonEos.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Fairy is absent (NeedsSummon = true)
        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.None);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isMoving: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.SummonEos.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FairyAbsentAutoSummonDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.None);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_FairyAbsent_DissipationActive_DoesNotSummon()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Dissipation is active — fairy is intentionally dismissed, do not re-summon
        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Dissipated);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isMoving: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.SummonEos.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_FairyAbsent_Moving_DoesNotSummon()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.None);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isMoving: true);

        // Moving — SummonEos has a cast time
        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.SummonEos.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Whispering Dawn Tests

    [Fact]
    public void TryExecute_WhisperingDawnDisabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableFairyAbilities = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.WhisperingDawn.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_WhisperingDawn_FairyAvailable_PartyInjured_Executes()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableFairyAbilities = true;
        config.Scholar.WhisperingDawnThreshold = 0.80f;
        config.Scholar.WhisperingDawnMinTargets = 2;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 2, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.WhisperingDawn.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.WhisperingDawn.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
    }

    [Fact]
    public void TryExecute_WhisperingDawn_NoFairy_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableFairyAbilities = true;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.WhisperingDawn.ActionId)).Returns(true);

        // No fairy present
        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.None);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.WhisperingDawn.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Seraphism Tests

    [Fact]
    public void TryExecute_Seraphism_Manual_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.SeraphismStrategy = SeraphismUsageStrategy.Manual;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Even if Seraphism is ready, Manual strategy should not auto-use
        actionService.Setup(a => a.IsActionReady(SCHActions.Seraphism.ActionId)).Returns(true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Seraphism.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Seraphism_OnCooldown_NotReady_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.SeraphismStrategy = SeraphismUsageStrategy.OnCooldown;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Seraphism is on cooldown
        actionService.Setup(a => a.IsActionReady(SCHActions.Seraphism.ActionId)).Returns(false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Fey Blessing Tests

    [Fact]
    public void TryExecute_FeyBlessing_FairyAvailable_PartyInjured_Executes()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableFairyAbilities = true;
        config.Scholar.FeyBlessingThreshold = 0.70f;

        // Party avg HP below threshold (4 injured at 50%)
        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.FeyBlessing.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.FeyBlessing.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Seraphism check - need CanUseEosAbilities to return true for FeyBlessing
        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
    }

    [Fact]
    public void TryExecute_FeyBlessing_Disabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableFairyAbilities = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(FairyState.Eos);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.FeyBlessing.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
