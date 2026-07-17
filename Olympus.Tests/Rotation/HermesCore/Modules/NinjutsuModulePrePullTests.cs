using Olympus.Data;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Verifies NinjutsuModule starts a Suiton mudra sequence during the pre-pull countdown.
///
/// UNTESTABILITY NOTE: Mudra dispatch calls SafeGameAccess.GetActionManager() which returns
/// null in tests, so InputNextMudra is a no-op. Tests assert decision state only:
/// MudraHelper.TargetNinjutsu, MudraHelper.IsSequenceActive, and Debug.NinjutsuState.
/// </summary>
public class NinjutsuModulePrePullTests
{
    private readonly NinjutsuModule _module = new();

    private static Configuration MakeConfig(bool enablePrePull = true, bool enableNinjutsu = true)
    {
        var cfg = HermesTestContext.CreateDefaultNinjaConfiguration();
        cfg.PrePull.EnablePrePullActions = enablePrePull;
        cfg.Ninja.EnableNinjutsu = enableNinjutsu;
        return cfg;
    }

    [Fact]
    public void Suiton_StartsSequence_WhenWithinCountdownWindow()
    {
        var mudraHelper = new MudraHelper();
        var debugState = new HermesDebugState();
        var config = MakeConfig();
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HermesTestContext.Create(
            config: config,
            inCombat: false,
            hasSuiton: false,
            mudraHelper: mudraHelper,
            debugState: debugState,
            countdownRemaining: 5f);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Equal(NINActions.NinjutsuType.Suiton, mudraHelper.TargetNinjutsu);
        Assert.True(mudraHelper.IsSequenceActive);
        Assert.Contains("Pre-pull", debugState.NinjutsuState);
    }

    /// <summary>
    /// Negative -- same setup as positive except countdownRemaining is null.
    /// Assert decision state confirms the else branch ran ("Not in combat"),
    /// not the pre-pull branch. A buggy always-firing gate would set a "Pre-pull: ..."
    /// string and fail this assertion.
    /// </summary>
    [Fact]
    public void Suiton_DoesNotStart_WhenNoCountdown()
    {
        var mudraHelper = new MudraHelper();
        var debugState = new HermesDebugState();
        var config = MakeConfig();
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HermesTestContext.Create(
            config: config,
            inCombat: false,
            hasSuiton: false,
            mudraHelper: mudraHelper,
            debugState: debugState,
            countdownRemaining: null);       // only discriminating variable vs positive

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Equal(NINActions.NinjutsuType.None, mudraHelper.TargetNinjutsu);
        Assert.False(mudraHelper.IsSequenceActive);
        Assert.Equal("Not in combat", debugState.NinjutsuState); // else branch ran
    }

    /// <summary>
    /// Toggle-off companion -- countdown within window but EnableNinjutsu is false.
    /// Debug state must be "Not in combat" (else branch), not any Pre-pull string.
    /// </summary>
    [Fact]
    public void Suiton_DoesNotStart_WhenNinjutsuDisabled()
    {
        var mudraHelper = new MudraHelper();
        var debugState = new HermesDebugState();
        var config = MakeConfig(enableNinjutsu: false); // only discriminating variable
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HermesTestContext.Create(
            config: config,
            inCombat: false,
            hasSuiton: false,
            mudraHelper: mudraHelper,
            debugState: debugState,
            countdownRemaining: 5f);         // same as positive

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Equal(NINActions.NinjutsuType.None, mudraHelper.TargetNinjutsu);
        Assert.False(mudraHelper.IsSequenceActive);
        Assert.Equal("Not in combat", debugState.NinjutsuState); // else branch ran
    }
}
