using Moq;
using Olympus.Rotation.KratosCore.Abilities;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.KratosCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Kratos (Monk) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class KratosTests
{
    #region Module Priority Tests

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
        Assert.Equal(20, new BuffModule().Priority);
        Assert.Equal(30, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void KratosDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new KratosDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.BeastChakraState);
    }

    [Fact]
    public void KratosDebugState_CanBeModified()
    {
        var debugState = new KratosDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Bootshine",
            DamageState = "Opo-opo form",
            Chakra = 5,
            HasRiddleOfFire = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Bootshine", debugState.PlannedAction);
        Assert.Equal("Opo-opo form", debugState.DamageState);
        Assert.Equal(5, debugState.Chakra);
        Assert.True(debugState.HasRiddleOfFire);
    }

    [Fact]
    public void KratosDebugState_FormatBeastChakra_ReturnsAllEmpty_WhenNone()
    {
        var result = KratosDebugState.FormatBeastChakra(0, 0, 0);
        Assert.Equal("[---]", result);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void KratosContext_StoresPlayerReference()
    {
        var context = KratosTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void KratosContext_TracksCombatState()
    {
        var context = KratosTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = KratosTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void KratosContext_TracksGcdState()
    {
        var context = KratosTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = KratosTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void KratosContext_HasDebugState()
    {
        var context = KratosTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void KratosContext_ConfigurationIsAccessible()
    {
        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableRiddleOfFire = false;

        var context = KratosTestContext.Create(config: config);

        Assert.False(context.Configuration.Monk.EnableRiddleOfFire);
    }

    [Fact]
    public void KratosContext_ChakraIsAccessible()
    {
        var context = KratosTestContext.Create(chakra: 5);
        Assert.Equal(5, context.Chakra);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_PreCombat_ChakraBelow5_PushesMediation()
    {
        // DamageModule pushes Meditation pre-combat when Chakra < 5.
        // No IsActionReady check on this path — pushes unconditionally once level and chakra gates pass.
        var module = new DamageModule();
        var context = KratosTestContext.Create(inCombat: false, chakra: 0, level: 100);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == KratosAbilities.Meditation && c.Priority == 10);
    }

    [Fact]
    public void DamageModule_CollectCandidates_PreCombat_ChakraAtMax_PushesNothing()
    {
        // When Chakra is at maximum (5) out of combat, no pre-combat Meditation is pushed.
        var module = new DamageModule();
        var context = KratosTestContext.Create(inCombat: false, chakra: 5, level: 100);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var context = KratosTestContext.Create(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_RiddleOfFire_PushedAtPriority1_WhenDisciplinedFistActive()
    {
        // Riddle of Fire requires: enabled (default), level met, not already active,
        // DisciplinedFist active (required gate), action ready, no burst hold.
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = KratosTestContext.Create(
            inCombat: true,
            actionService: actionService,
            hasRiddleOfFire: false,
            hasDisciplinedFist: true,
            level: 100);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var module = new BuffModule();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == KratosAbilities.RiddleOfFire && c.Priority == 1);
    }

    [Fact]
    public void BuffModule_CollectCandidates_RiddleOfFire_NotPushed_WhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = KratosTestContext.Create(
            inCombat: true,
            actionService: actionService,
            hasRiddleOfFire: true,
            riddleOfFireRemaining: 15f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var module = new BuffModule();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == KratosAbilities.RiddleOfFire);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_RiddleOfFireEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Monk.EnableRiddleOfFire);
    }

    [Fact]
    public void Configuration_DefaultChakraMinGauge_Is5()
    {
        var config = new Configuration();
        Assert.Equal(5, config.Monk.ChakraMinGauge);
    }

    #endregion
}
