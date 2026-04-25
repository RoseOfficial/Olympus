using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for ShieldHealingHandler. The CRITICAL behavior is the Eukrasia
/// direct-dispatch carve-out: when Eukrasia is not active and shielding is needed,
/// the handler MUST call ActionService.ExecuteOgcd(SGEActions.Eukrasia, ...) DIRECTLY
/// instead of pushing to the scheduler. This bypasses the scheduler's CanExecuteOgcd
/// gate which would otherwise block oGCDs during the GCD pass.
/// See CLAUDE.md "SGE Eukrasia timing" note.
/// </summary>
public class ShieldHealingHandlerSchedulerTests
{
    private readonly ShieldHealingHandler _handler = new();

    [Fact]
    public void CollectCandidates_NoEukrasiaPartyNeedsShield_DirectDispatchesEukrasia()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianPrognosis = true;
        config.Sage.AoEHealMinTargets = 2;
        config.Sage.AoEHealThreshold = 0.85f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // Party at 70% with 4 injured — meets the AoE threshold for Eukrasian Prognosis activation.
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((avgHpPercent: 0.70f, lowestHpPercent: 0.50f, injuredCount: 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            hasEukrasia: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Eukrasia must be DIRECT-DISPATCHED via ExecuteOgcd, NOT pushed to scheduler.
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Once);

        // The scheduler must NOT have an Eukrasia candidate.
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SGEActions.Eukrasia.ActionId);
    }

    // Note: the "HasEukrasia is true → pushes E.Prognosis/E.Diagnosis" path is not unit-testable
    // — context.HasEukrasia reads via AsclepiusStatusHelper.HasEukrasia(Player) which iterates
    // Player.StatusList (a Dalamud native struct). See CLAUDE.md "not unit-testable" caveats.
    // The critical carve-out (direct-dispatch Eukrasia when not active) is covered by
    // CollectCandidates_NoEukrasiaPartyNeedsShield_DirectDispatchesEukrasia above.

    [Fact]
    public void CollectCandidates_Moving_PushesNothingAndDoesNotDirectDispatch()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianPrognosis = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            hasEukrasia: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: true);

        // No direct dispatch, no scheduler push.
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Never);
        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }
}
