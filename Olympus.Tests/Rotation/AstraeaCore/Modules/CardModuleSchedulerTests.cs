using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Scheduler-push tests for CardModule. Distinct behaviors:
/// - PlayCard pushes ALL 6 specific card actions (Balance/Bole/Arrow/Spear/Ewer/Spire)
///   so the scheduler can dispatch whichever the game allows at UseAction time.
/// - Draw pushes BOTH AstralDraw and UmbralDraw — the game's ActiveDraw state picks one.
/// - Divination pushes at priority 0 (above Resurrection) so card-buff timing wins.
/// </summary>
public class CardModuleSchedulerTests
{
    private readonly CardModule _module = new();

    [Fact]
    public void CollectCandidates_PlayCard_PushesAllSixSpecificCardActions()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = true;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: true);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 3, injuredCount: 1);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            cardService: cardService,
            level: 100,
            hasCard: true,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        // 6 specific card actions all pushed (priorities 2-7).
        var queue = scheduler.InspectOgcdQueue();
        var cardActionIds = new[]
        {
            ASTActions.TheBalance.ActionId,
            ASTActions.TheBole.ActionId,
            ASTActions.TheArrow.ActionId,
            ASTActions.TheSpear.ActionId,
            ASTActions.TheEwer.ActionId,
            ASTActions.TheSpire.ActionId,
        };

        foreach (var cardId in cardActionIds)
        {
            Assert.Contains(queue, c => c.Behavior.Action.ActionId == cardId);
        }
    }

    [Fact]
    public void CollectCandidates_PlayCard_NoCardInHand_PushesNoCards()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = true;

        var cardService = AstraeaTestContext.CreateMockCardService(hasCard: false);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            cardService: cardService,
            level: 100,
            hasCard: false,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        var cardActionIds = new[]
        {
            ASTActions.TheBalance.ActionId, ASTActions.TheBole.ActionId, ASTActions.TheArrow.ActionId,
            ASTActions.TheSpear.ActionId, ASTActions.TheEwer.ActionId, ASTActions.TheSpire.ActionId,
        };

        foreach (var cardId in cardActionIds)
        {
            Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == cardId);
        }
    }

    [Fact]
    public void CollectCandidates_Draw_InCombat_PushesBothAstralAndUmbral()
    {
        // The game's ActiveDraw state determines which draw fires; only one succeeds at
        // UseAction time. Pushing both lets the scheduler discover which the game allows.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == ASTActions.AstralDraw.ActionId);
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == ASTActions.UmbralDraw.ActionId);
    }

    [Fact]
    public void CollectCandidates_Draw_OutOfCombat_PushesNoDrawCandidates()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: false,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == ASTActions.AstralDraw.ActionId);
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == ASTActions.UmbralDraw.ActionId);
    }

    [Fact]
    public void CollectCandidates_CardsDisabled_PushesNothing()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            hasCard: true,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_Divination_InCombat_PushesAtHighestPriority()
    {
        // Divination push priority = 0; this beats Resurrection (1-2) and everything else.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCards = true;
        config.Astrologian.EnableDivination = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Divination.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        var divCandidate = Assert.Single(queue, c => c.Behavior.Action.ActionId == ASTActions.Divination.ActionId);
        Assert.Equal(0, divCandidate.Priority);
    }
}
