using System.Collections.Generic;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar DefensiveModule logic.
/// Covers Expedient (party mitigation + sprint) and Deployment Tactics (shield spreading).
/// </summary>
public class DefensiveModuleTests
{
    private readonly DefensiveModule _module;

    public DefensiveModuleTests()
    {
        _module = new DefensiveModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is20()
    {
        Assert.Equal(20, _module.Priority);
    }

    [Fact]
    public void Name_IsDefensive()
    {
        Assert.Equal("Defensive", _module.Name);
    }

    #endregion

    #region Expedient Tests

    [Fact]
    public void TryExecute_ExpedientDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = false;
        config.Scholar.EnableDeploymentTactics = false;

        // Party at low HP to trigger Expedient if it were enabled
        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Expedient.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Expedient_PartyHealthy_DoesNotFire()
    {
        // Party avg HP is above ExpedientThreshold — should not fire
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;

        // All healthy: avg HP = 96%, above 60% threshold
        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 8, injuredCount: 0, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(true);

        var context = AthenaTestContext.CreateWithRealPartyHelper(
            realPartyHelper: partyHelper,
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Expedient.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Expedient_BelowLevel90_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(true);

        // Level 89 — below Expedient's level 90 requirement
        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 89,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.Expedient.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Deployment Tactics Tests

    [Fact]
    public void TryExecute_DeploymentTacticsDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableDeploymentTactics = false;
        config.Scholar.EnableExpedient = false;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.DeploymentTactics.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_DefensiveNotInCombat_ReturnsFalse()
    {
        // BaseDefensiveModule only fires in combat for SCH
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.EnableDeploymentTactics = true;

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion
}
