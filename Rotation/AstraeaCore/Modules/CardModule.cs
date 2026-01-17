using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;

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
            return true;
        }

        // Try Umbral Draw if Astral didn't work
        if (context.ActionService.ExecuteOgcd(ASTActions.UmbralDraw, player.GameObjectId))
        {
            context.Debug.PlannedAction = ASTActions.UmbralDraw.Name;
            context.Debug.DrawState = "Drawing (Umbral)";
            context.LogCardDecision("Umbral Draw", "Self", "Draw umbral cards");
            return true;
        }

        return false;
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
            return true;
        }

        return false;
    }

    #endregion
}
