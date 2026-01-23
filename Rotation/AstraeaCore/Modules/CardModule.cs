using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles card system for the Astrologian rotation.
/// Includes Draw, Play, Minor Arcana, Astrodyne, and Divination.
/// DPS-focused strategy: play cards on highest-contributing DPS members.
/// </summary>
public sealed class CardModule : IAstraeaModule
{
    public int Priority => 3; // Very high priority - cards should be played immediately
    public string Name => "Card";

    public bool TryExecute(AstraeaContext context, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCards)
            return false;

        // Only execute card abilities as oGCDs
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Divination (party damage buff) - use during burst windows
        if (TryDivination(context))
            return true;

        // Priority 2: Astrodyne (consume 3 seals for buff)
        if (TryAstrodyne(context))
            return true;

        // Priority 3: Play held card
        if (TryPlayCard(context))
            return true;

        // Priority 4: Draw a new card
        if (TryDraw(context))
            return true;

        // Priority 5: Minor Arcana (if we want to use Lord for damage or have excess cards)
        if (TryMinorArcana(context))
            return true;

        return false;
    }

    public void UpdateDebugState(AstraeaContext context)
    {
        // Update card state from service (Dawntrail: 4 cards in hand)
        context.Debug.CurrentCardType = context.CurrentCard.ToString();
        context.Debug.MinorArcanaType = context.MinorArcana.ToString();
        context.Debug.SealCount = context.SealCount;
        context.Debug.UniqueSealCount = context.UniqueSealCount;

        // Update card state with Dawntrail info - include raw types for debugging
        // Astral cards (Balance, Bole, Arrow) = Play I for melee
        // Umbral cards (Spear, Ewer, Spire) = Play II for ranged
        var totalCards = context.TotalCardsInHand;
        var astralCount = context.BalanceCount;  // Renamed: counts all astral cards
        var umbralCount = context.SpearCount;    // Renamed: counts all umbral cards
        var rawTypes = context.CardService.RawCardTypes;
        context.Debug.CardState = totalCards > 0
            ? $"{totalCards} cards ({astralCount} Astral/{umbralCount} Umbral) Raw: {rawTypes}"
            : "No cards";

        // Update draw state
        if (context.HasCard)
            context.Debug.DrawState = $"Cards: {astralCount} Astral, {umbralCount} Umbral";
        else if (context.ActionService.IsActionReady(ASTActions.AstralDraw.ActionId))
            context.Debug.DrawState = "Ready to Draw";
        else
            context.Debug.DrawState = "On Cooldown";

        // Update Astrodyne state (removed in Dawntrail)
        context.Debug.AstrodyneState = "N/A (Dawntrail)";

        // Update Divination state
        if (context.HasDivining)
            context.Debug.DivinationState = "Oracle Ready";
        else if (context.ActionService.IsActionReady(ASTActions.Divination.ActionId))
            context.Debug.DivinationState = "Ready";
        else
            context.Debug.DivinationState = "On Cooldown";
    }

    #region Card Actions

    private bool TryDivination(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableDivination)
            return false;

        if (player.Level < ASTActions.Divination.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Divination.ActionId))
            return false;

        // Use Divination on cooldown during combat
        // Could be enhanced with burst window detection
        if (!context.InCombat)
            return false;

        var action = ASTActions.Divination;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DivinationState = "Used";
            context.LogCardDecision("Divination", "Party", "Party damage buff");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = "Divination - 6% party damage buff!";

                var factors = new[]
                {
                    "6% damage buff for party",
                    "15s duration",
                    "120s cooldown",
                    "Used on cooldown in combat",
                    "Aligns with other raid buffs",
                };

                var alternatives = new[]
                {
                    "Hold for burst window alignment",
                    "Wait for more party members in range",
                    "Coordinate with other AST if present",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Divination",
                    Category = "Buff",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = "Divination provides 6% damage buff to all party members in range for 15 seconds. This is AST's main raid contribution beyond healing. Use it on cooldown during burst windows, or align with other 2-minute raid buffs for maximum party DPS.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Divination is your biggest DPS contribution! Try to align it with other 2-minute buffs like Battle Litany, Battle Voice, Chain Stratagem, etc. Don't hold it too long - losing a use is worse than imperfect alignment.",
                    ConceptId = AstConcepts.DivinationTiming,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryAstrodyne(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableAstrodyne)
            return false;

        if (player.Level < ASTActions.Astrodyne.MinLevel)
            return false;

        // Need 3 seals to use Astrodyne
        if (!context.CanUseAstrodyne)
            return false;

        // Check minimum unique seals requirement
        if (context.UniqueSealCount < config.AstrodyneMinSeals)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Astrodyne.ActionId))
            return false;

        var action = ASTActions.Astrodyne;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.AstrodyneState = $"Used ({context.UniqueSealCount} unique)";
            context.LogCardDecision("Astrodyne", "Self", $"{context.UniqueSealCount} unique seals");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var uniqueCount = context.UniqueSealCount;
                var buffDescription = uniqueCount switch
                {
                    3 => "All buffs: Haste + Damage + MP regen",
                    2 => "Two buffs based on seal types",
                    _ => "One buff based on seal type",
                };

                var shortReason = $"Astrodyne ({uniqueCount} unique seals) - {buffDescription}";

                var factors = new[]
                {
                    $"Unique seals: {uniqueCount}",
                    $"Total seals: {context.SealCount}",
                    buffDescription,
                    "Consumes all 3 seals",
                    "120s cooldown",
                };

                var alternatives = new[]
                {
                    uniqueCount < 3 ? "Wait for 3 unique seals (optimal)" : "N/A - already optimal",
                    "Save for specific buff timing",
                    "Use immediately to enable more card draws",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Astrodyne",
                    Category = "Buff",
                    TargetName = "Self",
                    ShortReason = shortReason,
                    DetailedReason = $"Astrodyne consumed {context.SealCount} seals ({uniqueCount} unique). {buffDescription}. 3 unique seals gives maximum value: 10% haste (faster GCDs), 5% damage, and MP regeneration. Playing cards strategically to collect different seal types is key!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = uniqueCount == 3
                        ? "Perfect! 3 unique seals gives you all three buffs. This is optimal Astrodyne usage!"
                        : "Try to collect 3 different seal types for maximum Astrodyne value. Play cards on appropriate targets (melee vs ranged) to control which seals you get.",
                    ConceptId = AstConcepts.AstrodyneBuilding,
                    Priority = uniqueCount == 3 ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPlayCard(AstraeaContext context)
    {
        var player = context.Player;

        // Must have a card to play
        if (!context.HasCard)
        {
            context.Debug.PlayState = "No cards in hand";
            return false;
        }

        // In Dawntrail, we need to use the SPECIFIC card action that matches what's drawn.
        // The game's GetActionStatus only returns 0 (usable) for the actual drawn card.
        // Try all card actions - only the one matching our drawn card will succeed.

        var target = context.PartyHelper.FindBalanceTarget(player);
        if (target == null)
        {
            context.Debug.PlayState = "No valid target";
            return false;
        }

        context.Debug.PlayState = $"Trying cards → {target.Name.TextValue}";

        // Try astral cards (melee DPS buff priority): Balance, Bole, Arrow
        if (TryPlaySpecificCard(context, ASTActions.TheBalance, target))
            return true;
        if (TryPlaySpecificCard(context, ASTActions.TheBole, target))
            return true;
        if (TryPlaySpecificCard(context, ASTActions.TheArrow, target))
            return true;

        // Try umbral cards (ranged DPS buff priority): Spear, Ewer, Spire
        if (TryPlaySpecificCard(context, ASTActions.TheSpear, target))
            return true;
        if (TryPlaySpecificCard(context, ASTActions.TheEwer, target))
            return true;
        if (TryPlaySpecificCard(context, ASTActions.TheSpire, target))
            return true;

        context.Debug.PlayState = "No usable cards";
        return false;
    }

    private bool TryPlaySpecificCard(AstraeaContext context, ActionDefinition cardAction, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;

        if (player.Level < cardAction.MinLevel)
            return false;

        if (context.ActionService.ExecuteOgcd(cardAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = cardAction.Name;
            context.Debug.PlayState = $"{cardAction.Name} → {target.Name.TextValue}";
            context.LogCardDecision(cardAction.Name, target.Name.TextValue, "Specific card played");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var targetJob = target.ClassJob.RowId;
                var isMelee = JobRegistry.IsMeleeDps(targetJob) || JobRegistry.IsTank(targetJob);
                var isAstral = cardAction.ActionId == ASTActions.TheBalance.ActionId ||
                               cardAction.ActionId == ASTActions.TheBole.ActionId ||
                               cardAction.ActionId == ASTActions.TheArrow.ActionId;

                var shortReason = $"{cardAction.Name} on {targetName} ({(isMelee ? "melee" : "ranged")})";

                var factors = new[]
                {
                    $"Card: {cardAction.Name}",
                    $"Target: {targetName}",
                    isAstral ? "Astral card (melee bonus)" : "Umbral card (ranged bonus)",
                    isMelee ? "Target is melee/tank" : "Target is ranged/caster/healer",
                    "6% damage buff for 15s",
                };

                var alternatives = new[]
                {
                    "Play on different target",
                    "Hold for higher DPS player",
                    "Redraw for different card (if available)",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = cardAction.ActionId,
                    ActionName = cardAction.Name,
                    Category = "Card",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Played {cardAction.Name} on {targetName}. {(isAstral ? "Astral cards (Balance/Bole/Arrow) give bonus damage to melee." : "Umbral cards (Spear/Ewer/Spire) give bonus damage to ranged.")} {(isMelee == isAstral ? "Good match! Target role matches card type." : "Role mismatch - still provides 6% buff but no bonus.")} Always prioritize highest DPS players.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = isMelee == isAstral
                        ? "Great card placement! Matching card type to role maximizes damage buff value."
                        : "Consider the role matching: Astral cards are better on melee, Umbral cards on ranged. But any buff is better than no buff!",
                    ConceptId = AstConcepts.CardManagement,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryDraw(AstraeaContext context)
    {
        var player = context.Player;

        if (player.Level < ASTActions.AstralDraw.MinLevel)
            return false;

        // Only draw in combat to avoid wasting cards
        if (!context.InCombat)
            return false;

        // In Dawntrail, AST alternates between Astral and Umbral draws.
        // The game will only allow one of them based on the current ActiveDraw state.
        // Try Astral Draw first (gives Spear for ranged), then Umbral Draw (gives Balance for melee).
        // Note: Don't check IsActionReady - draw actions don't use traditional charges.
        // ExecuteOgcd will fail gracefully if the action can't be used.

        // Try Astral Draw first
        if (context.ActionService.ExecuteOgcd(ASTActions.AstralDraw, player.GameObjectId))
        {
            context.Debug.PlannedAction = ASTActions.AstralDraw.Name;
            context.Debug.DrawState = "Drawing (Astral)";
            context.LogCardDecision("Astral Draw", "Self", "Draw astral cards");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                RecordDrawExplanation(context, "Astral Draw", true);
            }

            return true;
        }

        // Try Umbral Draw if Astral didn't work
        if (context.ActionService.ExecuteOgcd(ASTActions.UmbralDraw, player.GameObjectId))
        {
            context.Debug.PlannedAction = ASTActions.UmbralDraw.Name;
            context.Debug.DrawState = "Drawing (Umbral)";
            context.LogCardDecision("Umbral Draw", "Self", "Draw umbral cards");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                RecordDrawExplanation(context, "Umbral Draw", false);
            }

            return true;
        }

        return false;
    }

    private void RecordDrawExplanation(AstraeaContext context, string drawType, bool isAstral)
    {
        var shortReason = $"{drawType} - getting new cards";

        var factors = new[]
        {
            $"Draw type: {drawType}",
            isAstral ? "Draws Balance/Bole/Arrow" : "Draws Spear/Ewer/Spire",
            "Dawntrail: Alternates between Astral and Umbral",
            $"Current cards in hand: {context.TotalCardsInHand}",
            "Draw immediately to maximize card plays",
        };

        var alternatives = new[]
        {
            "Wait for current cards to be played",
            "Save draw charges for burst windows",
            "Let timer expire (not recommended)",
        };

        context.TrainingService!.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = isAstral ? ASTActions.AstralDraw.ActionId : ASTActions.UmbralDraw.ActionId,
            ActionName = drawType,
            Category = "Card",
            TargetName = "Self",
            ShortReason = shortReason,
            DetailedReason = $"{drawType} used to draw new cards. {(isAstral ? "Astral Draw gives Balance/Bole/Arrow (melee-focused cards)." : "Umbral Draw gives Spear/Ewer/Spire (ranged-focused cards).")} In Dawntrail, AST alternates between Astral and Umbral draws automatically. Keep drawing and playing cards to maximize party buff uptime!",
            Factors = factors,
            Alternatives = alternatives,
            Tip = "Card uptime is key to AST DPS contribution! Draw on cooldown, play immediately, and repeat. Don't let cards sit in hand - they provide no value until played.",
            ConceptId = AstConcepts.DrawTiming,
            Priority = ExplanationPriority.Normal,
        });
    }

    private bool TryMinorArcana(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMinorArcana)
            return false;

        if (player.Level < ASTActions.MinorArcana.MinLevel)
            return false;

        // Already have a Minor Arcana card (Lady or Lord)
        if (context.HasMinorArcana)
            return false;

        // Minor Arcana strategy determines when to draw
        bool shouldDraw = config.MinorArcanaStrategy switch
        {
            MinorArcanaUsageStrategy.OnCooldown => true,
            MinorArcanaUsageStrategy.SaveForBurst => context.ActionService.IsActionReady(ASTActions.Divination.ActionId),
            MinorArcanaUsageStrategy.EmergencyOnly => false, // Only use Lady from HealingModule
            _ => false
        };

        if (!shouldDraw)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.MinorArcana.ActionId))
            return false;

        var action = ASTActions.MinorArcana;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CardState = "Minor Arcana";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var strategy = config.MinorArcanaStrategy;
                var shortReason = $"Minor Arcana drawn ({strategy})";

                var factors = new[]
                {
                    $"Strategy: {strategy}",
                    "Will draw Lord (damage) or Lady (heal)",
                    "60s cooldown",
                    strategy == MinorArcanaUsageStrategy.SaveForBurst ? "Divination is ready - burst timing" : "Used on cooldown",
                    "Lord: 250 potency damage, Lady: 400 potency AoE heal",
                };

                var alternatives = new[]
                {
                    "Save for emergency (Lady)",
                    "Align with burst windows",
                    "Use Lord immediately for damage",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Minor Arcana",
                    Category = "Card",
                    TargetName = "Self",
                    ShortReason = shortReason,
                    DetailedReason = $"Minor Arcana drawn using {strategy} strategy. You'll receive either Lord of Crowns (250 potency damage) or Lady of Crowns (400 potency AoE heal). Lord is free damage during DPS phases, Lady is emergency healing. Choose your strategy based on content difficulty!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "In farm content, use Minor Arcana on cooldown for Lord damage. In progression, consider saving for Lady heals. The 50/50 RNG means you should plan for both outcomes!",
                    ConceptId = AstConcepts.MinorArcanaUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    #endregion
}
