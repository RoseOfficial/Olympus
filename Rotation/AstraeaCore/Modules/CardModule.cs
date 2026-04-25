using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles card system for the Astrologian rotation (scheduler-driven).
/// Push priorities are 0-9 so card play wins against Resurrection (1-2) when needed.
/// PlayCard pushes all 6 specific card actions; the scheduler dispatches whichever the
/// game allows (only the actually-drawn card succeeds at UseAction).
/// </summary>
public sealed class CardModule : IAstraeaModule
{
    public int Priority => 3;
    public string Name => "Card";

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        if (!config.EnableCards) return;

        TryPushDivination(context, scheduler);
        TryPushAstrodyne(context, scheduler);
        TryPushPlayCard(context, scheduler);
        TryPushDraw(context, scheduler);
        TryPushMinorArcana(context, scheduler);
    }

    public void UpdateDebugState(IAstraeaContext context)
    {
        context.Debug.CurrentCardType = context.CurrentCard.ToString();
        context.Debug.MinorArcanaType = context.MinorArcana.ToString();
        context.Debug.SealCount = context.SealCount;
        context.Debug.UniqueSealCount = context.UniqueSealCount;

        var totalCards = context.TotalCardsInHand;
        var astralCount = context.BalanceCount;
        var umbralCount = context.SpearCount;
        var rawTypes = context.CardService.RawCardTypes;
        context.Debug.CardState = totalCards > 0
            ? $"{totalCards} cards ({astralCount} Astral/{umbralCount} Umbral) Raw: {rawTypes}"
            : "No cards";

        if (context.HasCard)
            context.Debug.DrawState = $"Cards: {astralCount} Astral, {umbralCount} Umbral";
        else if (context.ActionService.IsActionReady(ASTActions.AstralDraw.ActionId))
            context.Debug.DrawState = "Ready to Draw";
        else
            context.Debug.DrawState = "On Cooldown";

        context.Debug.AstrodyneState = "N/A (Dawntrail)";

        if (context.HasDivining)
            context.Debug.DivinationState = "Oracle Ready";
        else if (context.ActionService.IsActionReady(ASTActions.Divination.ActionId))
            context.Debug.DivinationState = "Ready";
        else
            context.Debug.DivinationState = "On Cooldown";
    }

    private void TryPushDivination(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableDivination) return;
        if (player.Level < ASTActions.Divination.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.Divination.ActionId)) return;
        if (!context.InCombat) return;

        scheduler.PushOgcd(AstraeaAbilities.Divination, player.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = ASTActions.Divination.Name;
                context.Debug.DivinationState = "Used";
                context.LogCardDecision("Divination", "Party", "Party damage buff");
                context.TrainingService?.RecordConceptApplication(AstConcepts.DivinationTiming, wasSuccessful: true, "Divination burst buff deployed");
            });
    }

    private void TryPushAstrodyne(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableAstrodyne) return;
        if (player.Level < ASTActions.Astrodyne.MinLevel) return;
        if (!context.CanUseAstrodyne) return;
        if (context.UniqueSealCount < config.AstrodyneMinSeals) return;
        if (!context.ActionService.IsActionReady(ASTActions.Astrodyne.ActionId)) return;

        var capturedUniqueCount = context.UniqueSealCount;

        scheduler.PushOgcd(AstraeaAbilities.Astrodyne, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = ASTActions.Astrodyne.Name;
                context.Debug.AstrodyneState = $"Used ({capturedUniqueCount} unique)";
                context.LogCardDecision("Astrodyne", "Self", $"{capturedUniqueCount} unique seals");
                context.TrainingService?.RecordConceptApplication(AstConcepts.AstrodyneBuilding, wasSuccessful: capturedUniqueCount == 3, $"{capturedUniqueCount} unique seals consumed");
            });
    }

    private void TryPushPlayCard(IAstraeaContext context, RotationScheduler scheduler)
    {
        var player = context.Player;

        if (!context.HasCard) { context.Debug.PlayState = "No cards in hand"; return; }

        var target = context.PartyHelper.FindBalanceTarget(player);
        if (target == null) { context.Debug.PlayState = "No valid target"; return; }

        // Push all 6 specific card actions. The scheduler will attempt each in priority order;
        // only the actually-drawn card succeeds at UseAction time. Astral cards (Balance/Bole/Arrow)
        // pushed before umbral cards (Spear/Ewer/Spire) to match legacy ordering.
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheBalance, ASTActions.TheBalance, target, priority: 2);
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheBole, ASTActions.TheBole, target, priority: 3);
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheArrow, ASTActions.TheArrow, target, priority: 4);
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheSpear, ASTActions.TheSpear, target, priority: 5);
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheEwer, ASTActions.TheEwer, target, priority: 6);
        TryPushSpecificCard(context, scheduler, AstraeaAbilities.TheSpire, ASTActions.TheSpire, target, priority: 7);
    }

    private void TryPushSpecificCard(IAstraeaContext context, RotationScheduler scheduler,
        AbilityBehavior behavior, ActionDefinition action, IBattleChara target, int priority)
    {
        var player = context.Player;
        if (player.Level < action.MinLevel) return;

        var capturedAction = action;
        var capturedTarget = target;

        scheduler.PushOgcd(behavior, target.GameObjectId, priority: priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = capturedAction.Name;
                var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                context.Debug.PlayState = $"{capturedAction.Name} -> {targetName}";
                context.LogCardDecision(capturedAction.Name, targetName, "Specific card played");
                context.TrainingService?.RecordConceptApplication(AstConcepts.CardManagement, wasSuccessful: true, "Card played");
            });
    }

    private void TryPushDraw(IAstraeaContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < ASTActions.AstralDraw.MinLevel) return;
        if (!context.InCombat) return;

        // Push both Astral and Umbral draws — only one will succeed based on game's ActiveDraw state.
        scheduler.PushOgcd(AstraeaAbilities.AstralDraw, player.GameObjectId, priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = ASTActions.AstralDraw.Name;
                context.Debug.DrawState = "Drawing (Astral)";
                context.LogCardDecision("Astral Draw", "Self", "Draw astral cards");
                context.TrainingService?.RecordConceptApplication(AstConcepts.DrawTiming, wasSuccessful: true, "Astral Draw executed");
            });

        scheduler.PushOgcd(AstraeaAbilities.UmbralDraw, player.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = ASTActions.UmbralDraw.Name;
                context.Debug.DrawState = "Drawing (Umbral)";
                context.LogCardDecision("Umbral Draw", "Self", "Draw umbral cards");
                context.TrainingService?.RecordConceptApplication(AstConcepts.DrawTiming, wasSuccessful: true, "Umbral Draw executed");
            });
    }

    private void TryPushMinorArcana(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMinorArcana) return;
        if (player.Level < ASTActions.MinorArcana.MinLevel) return;
        if (context.HasMinorArcana) return;

        bool shouldDraw = config.MinorArcanaStrategy switch
        {
            MinorArcanaUsageStrategy.OnCooldown => true,
            MinorArcanaUsageStrategy.SaveForBurst => context.ActionService.IsActionReady(ASTActions.Divination.ActionId),
            MinorArcanaUsageStrategy.EmergencyOnly => false,
            _ => false
        };
        if (!shouldDraw) return;
        if (!context.ActionService.IsActionReady(ASTActions.MinorArcana.ActionId)) return;

        scheduler.PushOgcd(AstraeaAbilities.MinorArcana, player.GameObjectId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = ASTActions.MinorArcana.Name;
                context.Debug.CardState = "Minor Arcana";
                context.TrainingService?.RecordConceptApplication(AstConcepts.MinorArcanaUsage, wasSuccessful: true, "Minor Arcana drawn");
            });
    }
}
