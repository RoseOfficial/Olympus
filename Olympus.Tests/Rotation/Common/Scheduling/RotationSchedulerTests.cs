using Dalamud.Plugin.Services;
using Moq;
using Olympus.Models.Action;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Scheduling;

public class RotationSchedulerTests
{
    private static RotationScheduler Build(
        Mock<IActionService>? actionService = null,
        Mock<IJobGauges>? jobGauges = null,
        Configuration? config = null)
    {
        actionService ??= new Mock<IActionService>();
        jobGauges ??= new Mock<IJobGauges>();
        // Tests that assert on GateFailReasons need IsDebugWindowOpen = true; callers that
        // explicitly want it closed pass their own Configuration.
        config ??= new Configuration { IsDebugWindowOpen = true };
        return new RotationScheduler(actionService.Object, jobGauges.Object, config);
    }

    [Fact]
    public void Dispatch_EmptyQueue_ReturnsNotDispatched()
    {
        var scheduler = Build();
        var ctx = new Mock<IRotationContext>().Object;

        var gcdResult = scheduler.DispatchGcd(ctx);
        var ogcdResult = scheduler.DispatchOgcd(ctx);

        Assert.False(gcdResult.Dispatched);
        Assert.False(ogcdResult.Dispatched);
    }

    [Fact]
    public void Reset_ClearsBothQueues()
    {
        var scheduler = Build();
        var behavior = TestBehaviors.InstantGcd();
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);
        scheduler.PushOgcd(behavior, targetId: 1, priority: 10);

        scheduler.Reset();

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Push_AddsCandidatesToTheCorrectQueue()
    {
        var scheduler = Build();
        var behavior = TestBehaviors.InstantGcd();

        scheduler.PushGcd(behavior, targetId: 123, priority: 5);
        scheduler.PushOgcd(behavior, targetId: 456, priority: 1);

        Assert.Single(scheduler.InspectGcdQueue());
        Assert.Single(scheduler.InspectOgcdQueue());
        Assert.Equal(123ul, scheduler.InspectGcdQueue()[0].TargetId);
        Assert.Equal(456ul, scheduler.InspectOgcdQueue()[0].TargetId);
    }

    [Fact]
    public void Dispatch_LevelTooLow_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 3001, minLevel: 90);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Level"));
    }

    [Fact]
    public void Dispatch_LevelMatches_AdvancesPastLevelGate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 3002, minLevel: 60);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_ToggleReturnsFalse_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var config = new Configuration { IsDebugWindowOpen = true };
        var scheduler = Build(actionService, config: config);

        var behavior = TestBehaviors.InstantGcd(actionId: 4001) with { Toggle = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Toggle"));
    }

    [Fact]
    public void Dispatch_ProcBuffMissing_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 5001) with { ProcBuff = 9999 };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("ProcBuff"));
    }

    [Fact]
    public void Dispatch_ProcBuffPresent_AdvancesPastProcGate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.PlayerHasStatus(9999)).Returns(true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 5002) with { ProcBuff = 9999 };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_ComboStepPredicateFalse_SkipsCandidate()
    {
        var jobGauges = new Mock<IJobGauges>();
        var scheduler = Build(jobGauges: jobGauges);

        var behavior = TestBehaviors.InstantGcd(actionId: 6001) with { ComboStep = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("ComboStep"));
    }

    [Fact]
    public void Dispatch_ComboStepThrows_RecordsErrorAndSkips()
    {
        var jobGauges = new Mock<IJobGauges>();
        var errorMetrics = new Mock<Olympus.Services.IErrorMetricsService>();
        var scheduler = new RotationScheduler(
            new Mock<IActionService>().Object,
            jobGauges.Object,
            new Configuration(),
            null,
            errorMetrics.Object);

        var behavior = TestBehaviors.InstantGcd(actionId: 6002) with
        {
            ComboStep = _ => throw new System.InvalidOperationException("bad gauge")
        };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        errorMetrics.Verify(
            x => x.RecordError("Scheduler", It.Is<string>(s => s.Contains("ComboStep"))),
            Times.Once);
    }

    [Fact]
    public void Dispatch_AdjustedProbeMismatch_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.GetAdjustedActionId(100u)).Returns(200u);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 300) with { AdjustedActionProbe = 100u };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Adjusted"));
    }

    [Fact]
    public void Dispatch_AdjustedProbeMatches_AdvancesPastProbe()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.GetAdjustedActionId(100u)).Returns(300u);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 300) with { AdjustedActionProbe = 100u };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_LevelReplacementApplies_UsesUpgradedActionForDispatch()
    {
        var baseAction = new ActionDefinition
        {
            ActionId = 7001,
            Name = "Nebula",
            MinLevel = 38,
            Category = ActionCategory.oGCD,
            TargetType = ActionTargetType.Self,
            CastTime = 0f,
            RecastTime = 120f,
            Range = 0f,
        };
        var upgraded = new ActionDefinition
        {
            ActionId = 7002,
            Name = "GreatNebula",
            MinLevel = 92,
            Category = ActionCategory.oGCD,
            TargetType = ActionTargetType.Self,
            CastTime = 0f,
            RecastTime = 120f,
            Range = 0f,
        };

        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == 7002),
            It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = new AbilityBehavior
        {
            Action = baseAction,
            LevelReplacements = new[] { ((byte)92, upgraded) },
        };
        var ctx = CreateContextWithPlayerLevel(100);
        scheduler.PushOgcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchOgcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == 7002),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Dispatch_TargetMissing_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 8001);

        var ctx = CreateContextWithPlayerLevelAndTarget(level: 80, targetId: 999, targetExists: false);
        scheduler.PushGcd(behavior, targetId: 999, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Target"));
    }

    [Fact]
    public void Dispatch_TargetIdZero_SkipsTargetValidation()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 8002);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_OgcdOnCooldown_SkipsCandidateViaPreGate()
    {
        // Non-charge oGCDs on their own cooldown are rejected by the pre-gate.
        // GetCurrentCharges (via IsActionReady) correctly reflects oGCD own-CD state
        // because oGCDs are on their own cooldown groups, separate from the global GCD.
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(3.2f);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantOgcd(actionId: 9001);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushOgcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchOgcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Cooldown"));
    }

    [Fact]
    public void Dispatch_GcdOnOwnCooldown_SkipsViaDispatchRejected()
    {
        // Non-charge GCDs bypass the pre-cooldown gate (because GetCurrentCharges returns
        // 0 during the global GCD roll, which would falsely reject queue-window dispatches).
        // A GCD that is actually on its own independent cooldown is rejected at dispatch
        // time: ExecuteGcd returns false when UseAction rejects the action. The scheduler
        // records "DispatchRejected" and moves to the next candidate.
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(3.2f);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(false);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 9001);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("DispatchRejected"));
    }

    [Fact]
    public void Dispatch_GcdInQueueWindow_DispatchesEvenWhenIsActionReadyFalse()
    {
        // Regression test for the scheduler queue-window bug: during the ~0.5s queue window
        // the global GCD (group 57) is still rolling, which makes GetCurrentCharges (and
        // thus IsActionReady) return 0 for plain GCDs. A pre-cooldown gate using IsActionReady
        // would incorrectly reject these dispatches even though UseAction would accept them
        // and queue the action for rollover. The scheduler skips the pre-gate for GCDs, so
        // ExecuteGcd (which mirrors UseAction's queue-window semantics) gets to decide.
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false); // global GCD rolling
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true); // UseAction accepts queue-window
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 9101);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 9101),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Dispatch_MechanicGateInactiveForInstants_DoesNotCheckTimeline()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 10001) with { MechanicGate = true };

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_ReplacementBaseIdSet_UsesRawVariant()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcdRaw(
                It.IsAny<ActionDefinition>(), 2260u, It.IsAny<ulong>()))
            .Returns(true);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 2267) with { ReplacementBaseId = 2260u };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteGcdRaw(
            It.IsAny<ActionDefinition>(), 2260u, It.IsAny<ulong>()), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void Dispatch_MultipleCandidates_LowerPriorityWinsWhenAllGatePass()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var high = TestBehaviors.InstantGcd(actionId: 11001);
        var low = TestBehaviors.InstantGcd(actionId: 11002);
        var ctx = CreateContextWithPlayerLevel(80);

        scheduler.PushGcd(low, targetId: 0, priority: 100);
        scheduler.PushGcd(high, targetId: 0, priority: 1);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        Assert.Equal(11001u, result.Winner!.Action.ActionId);
    }

    [Fact]
    public void Dispatch_TopPriorityFails_NextCandidateWins()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(11001u)).Returns(false); // first one on cooldown
        actionService.Setup(x => x.IsActionReady(11002u)).Returns(true);  // second one ready
        actionService.Setup(x => x.GetCooldownRemaining(11001u)).Returns(5f);
        actionService.Setup(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 11002),
            It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 11001), 0, priority: 1);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 11002), 0, priority: 2);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        Assert.Equal(11002u, result.Winner!.Action.ActionId);
    }

    [Fact]
    public void Dispatch_ActionServiceRejects_TriesNextCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 12001),
            It.IsAny<ulong>())).Returns(false); // first one rejected
        actionService.Setup(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 12002),
            It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 12001), 0, priority: 1);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 12002), 0, priority: 2);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        Assert.Equal(12002u, result.Winner!.Action.ActionId);
    }

    [Fact]
    public void Dispatch_InvokesOnDispatchedCallback_AfterSuccessfulDispatch()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var callbackInvoked = false;
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 13001), 0, priority: 1,
            onDispatched: _ => callbackInvoked = true);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Dispatch_DoesNotInvokeCallback_WhenDispatchFails()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(false);
        var scheduler = Build(actionService);

        var callbackInvoked = false;
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(TestBehaviors.InstantGcd(actionId: 13002), 0, priority: 1,
            onDispatched: _ => callbackInvoked = true);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void PushGroundTargetedOgcd_AddsCandidateWithPosition()
    {
        var scheduler = Build();
        var behavior = TestBehaviors.InstantOgcd(actionId: 7001);
        var position = new System.Numerics.Vector3(10f, 0f, 20f);

        scheduler.PushGroundTargetedOgcd(behavior, position, priority: 1);

        Assert.Single(scheduler.InspectOgcdQueue());
        var candidate = scheduler.InspectOgcdQueue()[0];
        Assert.Equal(position, candidate.GroundPosition);
        Assert.Equal(0ul, candidate.TargetId);
    }

    [Fact]
    public void DispatchOgcd_GroundTargetedCandidate_CallsExecuteGroundTargetedOgcd()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGroundTargetedOgcd(
                It.IsAny<ActionDefinition>(), It.IsAny<System.Numerics.Vector3>()))
            .Returns(true);

        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantOgcd(actionId: 7002);
        var position = new System.Numerics.Vector3(5f, 0f, 5f);
        var ctx = CreateContextWithPlayerLevel(80);

        scheduler.PushGroundTargetedOgcd(behavior, position, priority: 1);
        var result = scheduler.DispatchOgcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == 7002), position), Times.Once);
        actionService.Verify(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void DispatchOgcd_GroundTargetedReject_RecordsFailReason()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGroundTargetedOgcd(
                It.IsAny<ActionDefinition>(), It.IsAny<System.Numerics.Vector3>()))
            .Returns(false);

        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantOgcd(actionId: 7003);
        var ctx = CreateContextWithPlayerLevel(80);

        scheduler.PushGroundTargetedOgcd(behavior, System.Numerics.Vector3.Zero, priority: 1);
        var result = scheduler.DispatchOgcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("DispatchRejected"));
    }

    [Fact]
    public void DispatchOgcd_GroundTargetedSkipsTargetIdGate()
    {
        // Ground-targeted candidates have TargetId=0 by design — the target gate
        // (which validates the target exists in ObjectTable) should not block them.
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGroundTargetedOgcd(
                It.IsAny<ActionDefinition>(), It.IsAny<System.Numerics.Vector3>()))
            .Returns(true);

        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantOgcd(actionId: 7004);
        // Context has ObjectTable; ground-target candidate's TargetId = 0 should bypass it.
        var ctx = CreateContextWithPlayerLevelAndTarget(level: 80, targetId: 0, targetExists: false);

        scheduler.PushGroundTargetedOgcd(behavior, new System.Numerics.Vector3(1f, 0f, 1f), priority: 1);
        var result = scheduler.DispatchOgcd(ctx);

        Assert.True(result.Dispatched);
    }

    private static IRotationContext CreateContextWithPlayerLevel(byte level)
    {
        var mock = new Mock<IRotationContext>();
        var player = new Mock<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        player.Setup(p => p.Level).Returns(level);
        mock.Setup(c => c.Player).Returns(player.Object);
        mock.Setup(c => c.Configuration).Returns(new Configuration());
        return mock.Object;
    }

    private static IRotationContext CreateContextWithPlayerLevelAndTarget(
        byte level, ulong targetId, bool targetExists)
    {
        var mock = new Mock<IRotationContext>();
        var player = new Mock<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        player.Setup(p => p.Level).Returns(level);
        mock.Setup(c => c.Player).Returns(player.Object);
        mock.Setup(c => c.Configuration).Returns(new Configuration());
        var objectTable = new Mock<Dalamud.Plugin.Services.IObjectTable>();
        if (!targetExists)
        {
            objectTable.Setup(t => t.SearchById(targetId)).Returns((Dalamud.Game.ClientState.Objects.Types.IGameObject?)null);
        }
        mock.Setup(c => c.ObjectTable).Returns(objectTable.Object);
        return mock.Object;
    }

    private static IRotationContext CreateContextWithTargetingService(
        byte level, ITargetingService targetingService)
    {
        var mock = new Mock<IRotationContext>();
        var player = new Mock<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        player.Setup(p => p.Level).Returns(level);
        mock.Setup(c => c.Player).Returns(player.Object);
        mock.Setup(c => c.Configuration).Returns(new Configuration());
        mock.Setup(c => c.TargetingService).Returns(targetingService);
        return mock.Object;
    }

    [Fact]
    public void Dispatch_NoTargetingOverride_DispatchesToPushedTargetId()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 20001);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 999, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteGcd(
            It.IsAny<ActionDefinition>(),
            999ul), Times.Once);
    }

    [Fact]
    public void Dispatch_TargetingOverride_DispatchesToResolvedTarget()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        // The targeting service resolves HighestHp to enemy 888.
        var resolvedEnemy = new Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        resolvedEnemy.Setup(e => e.GameObjectId).Returns(888ul);
        var targetingService = new Mock<ITargetingService>();
        targetingService
            .Setup(s => s.FindEnemy(EnemyTargetingStrategy.HighestHp, It.IsAny<float>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(resolvedEnemy.Object);

        var behavior = TestBehaviors.InstantGcd(actionId: 20002) with { TargetingOverride = EnemyTargetingStrategy.HighestHp };
        var ctx = CreateContextWithTargetingService(80, targetingService.Object);
        scheduler.PushGcd(behavior, targetId: 999, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        // Dispatched to the override-resolved enemy (888), not the pushed id (999).
        actionService.Verify(x => x.ExecuteGcd(
            It.IsAny<ActionDefinition>(),
            888ul), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.IsAny<ActionDefinition>(),
            999ul), Times.Never);
    }

    [Fact]
    public void Dispatch_TargetingOverride_FallsBackToPushedIdWhenServiceReturnsNull()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        // The targeting service finds no enemy for HighestHp.
        var targetingService = new Mock<ITargetingService>();
        targetingService
            .Setup(s => s.FindEnemy(EnemyTargetingStrategy.HighestHp, It.IsAny<float>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleNpc?)null);

        var behavior = TestBehaviors.InstantGcd(actionId: 20003) with { TargetingOverride = EnemyTargetingStrategy.HighestHp };
        var ctx = CreateContextWithTargetingService(80, targetingService.Object);
        scheduler.PushGcd(behavior, targetId: 999, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        // Resolution failed: must fall back to the module-pushed id (999).
        actionService.Verify(x => x.ExecuteGcd(
            It.IsAny<ActionDefinition>(),
            999ul), Times.Once);
    }

    // --- Finding #28: GateFailReasons is empty (no allocation) when debug window is closed ---

    [Fact]
    public void Dispatch_DebugWindowClosed_GateFailReasonsEmpty_EvenOnGateFailure()
    {
        // When IsDebugWindowOpen is false (the default), RecordFail is a no-op.
        // Even with a gate failure the returned GateFailReasons must be empty
        // (no heap string or array allocated on the hot path).
        var actionService = new Mock<IActionService>();
        var config = new Configuration { IsDebugWindowOpen = false };
        var scheduler = Build(actionService, config: config);

        // A toggle that always returns false forces a "Toggle" gate failure every time.
        var behavior = TestBehaviors.InstantGcd(actionId: 30001) with { Toggle = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Empty(result.GateFailReasons);
    }

    [Fact]
    public void Dispatch_DebugWindowOpen_GateFailReasonsPopulated()
    {
        // When IsDebugWindowOpen is true, gate failure reasons are recorded and returned.
        var actionService = new Mock<IActionService>();
        var config = new Configuration { IsDebugWindowOpen = true };
        var scheduler = Build(actionService, config: config);

        var behavior = TestBehaviors.InstantGcd(actionId: 30002) with { Toggle = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Toggle"));
    }

    // --- Finding #33: Memo key includes range — same strategy, different ranges resolve independently ---

    private static ActionDefinition MakeActionWithRange(uint actionId, float range) => new()
    {
        ActionId = actionId,
        Name = $"TestAction{actionId}",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = range,
    };

    [Fact]
    public void Dispatch_TargetingOverride_SameStrategyAndRange_SecondCandidateReusesMemo()
    {
        // Two candidates share the same TargetingOverride strategy AND the same Range.
        // The first candidate's ExecuteGcd is rejected (returns false), so the scheduler
        // continues to the second candidate. Because (Strategy, Range) is identical, the
        // second candidate must reuse the memo entry — FindEnemy must be called exactly
        // once, not twice.
        var actionService = new Mock<IActionService>();
        actionService
            .Setup(x => x.ExecuteGcd(It.Is<ActionDefinition>(a => a.ActionId == 32001), It.IsAny<ulong>()))
            .Returns(false); // first candidate rejected at dispatch
        actionService
            .Setup(x => x.ExecuteGcd(It.Is<ActionDefinition>(a => a.ActionId == 32002), It.IsAny<ulong>()))
            .Returns(true);  // second candidate succeeds
        var scheduler = Build(actionService);

        var resolvedEnemy = new Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        resolvedEnemy.Setup(e => e.GameObjectId).Returns(777ul);
        var targetingService = new Mock<ITargetingService>();
        targetingService
            .Setup(s => s.FindEnemy(
                EnemyTargetingStrategy.HighestHp,
                25f,
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(resolvedEnemy.Object);

        var behaviorA = new AbilityBehavior
        {
            Action = MakeActionWithRange(actionId: 32001, range: 25f),
            TargetingOverride = EnemyTargetingStrategy.HighestHp,
        };
        var behaviorB = new AbilityBehavior
        {
            Action = MakeActionWithRange(actionId: 32002, range: 25f),
            TargetingOverride = EnemyTargetingStrategy.HighestHp,
        };

        var ctx = CreateContextWithTargetingService(80, targetingService.Object);
        scheduler.PushGcd(behaviorA, targetId: 999, priority: 1); // evaluated first, rejected
        scheduler.PushGcd(behaviorB, targetId: 999, priority: 2); // hits memo, dispatched

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        // FindEnemy called once — the second candidate reused the memoised resolution.
        targetingService.Verify(
            s => s.FindEnemy(
                EnemyTargetingStrategy.HighestHp,
                25f,
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()),
            Times.Once);
        // The second candidate dispatches to the memoised enemy (777).
        actionService.Verify(
            x => x.ExecuteGcd(It.Is<ActionDefinition>(a => a.ActionId == 32002), 777ul),
            Times.Once);
    }

    [Fact]
    public void Dispatch_TargetingOverride_SameStrategyDifferentRanges_ResolveIndependentlyWithinPass()
    {
        // Both candidates share TargetingOverride = HighestHp but have different Range values.
        // The short-range candidate (priority 1, range 3f) is evaluated first and its
        // ExecuteGcd is rejected, so the scheduler continues to the long-range candidate
        // (priority 2, range 25f). With the range-inclusive memo key the long-range candidate
        // gets its own FindEnemy call and resolves to enemy 200. Without the fix (key = strategy
        // only), it would reuse the memo for (HighestHp -> enemy 100) and dispatch to 100 instead.
        var actionService = new Mock<IActionService>();
        // Short-range action: dispatch is rejected so the scheduler falls through.
        actionService
            .Setup(x => x.ExecuteGcd(It.Is<ActionDefinition>(a => a.ActionId == 31001), It.IsAny<ulong>()))
            .Returns(false);
        // Long-range action: dispatch succeeds.
        actionService
            .Setup(x => x.ExecuteGcd(It.Is<ActionDefinition>(a => a.ActionId == 31002), It.IsAny<ulong>()))
            .Returns(true);
        var scheduler = Build(actionService);

        // HighestHp at range 3f resolves to enemy 100 (close target).
        var closeEnemy = new Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        closeEnemy.Setup(e => e.GameObjectId).Returns(100ul);
        // HighestHp at range 25f resolves to enemy 200 (higher HP but farther away).
        var farEnemy = new Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        farEnemy.Setup(e => e.GameObjectId).Returns(200ul);

        var targetingService = new Mock<ITargetingService>();
        targetingService
            .Setup(s => s.FindEnemy(EnemyTargetingStrategy.HighestHp, 3f,
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(closeEnemy.Object);
        targetingService
            .Setup(s => s.FindEnemy(EnemyTargetingStrategy.HighestHp, 25f,
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(farEnemy.Object);

        var behaviorShort = new AbilityBehavior
        {
            Action = MakeActionWithRange(actionId: 31001, range: 3f),
            TargetingOverride = EnemyTargetingStrategy.HighestHp,
        };
        var behaviorLong = new AbilityBehavior
        {
            Action = MakeActionWithRange(actionId: 31002, range: 25f),
            TargetingOverride = EnemyTargetingStrategy.HighestHp,
        };

        var ctx = CreateContextWithTargetingService(80, targetingService.Object);
        scheduler.PushGcd(behaviorShort, targetId: 999, priority: 1); // evaluated first
        scheduler.PushGcd(behaviorLong, targetId: 999, priority: 2);  // falls through to this

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
        // The long-range candidate must dispatch to its own resolution (enemy 200), not
        // to the short-range candidate's memoised resolution (enemy 100).
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 31002), 200ul), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == 31002), 100ul), Times.Never);
    }
}
