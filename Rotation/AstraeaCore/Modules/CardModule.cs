using Olympus.Config;
using Olympus.Data;
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
        // Update card state from service
        context.Debug.CurrentCardType = context.CurrentCard.ToString();
        context.Debug.MinorArcanaType = context.MinorArcana.ToString();
        context.Debug.SealCount = context.SealCount;
        context.Debug.UniqueSealCount = context.UniqueSealCount;

        // Update draw state
        if (context.HasCard)
            context.Debug.DrawState = "Card Ready";
        else if (context.ActionService.IsActionReady(ASTActions.AstralDraw.ActionId))
            context.Debug.DrawState = "Ready to Draw";
        else
            context.Debug.DrawState = "On Cooldown";

        // Update Astrodyne state
        if (context.CanUseAstrodyne)
            context.Debug.AstrodyneState = $"Ready ({context.UniqueSealCount} unique)";
        else
            context.Debug.AstrodyneState = $"{context.SealCount}/3 seals";

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
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        // Must have a card to play
        if (!context.HasCard)
            return false;

        var cardType = context.CurrentCard;

        // Get the appropriate play action and target based on card type
        var (action, target) = cardType switch
        {
            ASTActions.CardType.TheBalance => (
                player.Level >= ASTActions.PlayI.MinLevel ? ASTActions.PlayI : null,
                context.PartyHelper.FindBalanceTarget(player)),
            ASTActions.CardType.TheSpear => (
                player.Level >= ASTActions.PlayII.MinLevel ? ASTActions.PlayII : null,
                context.PartyHelper.FindSpearTarget(player)),
            ASTActions.CardType.Lord => (
                player.Level >= ASTActions.PlayIII.MinLevel ? ASTActions.PlayIII : null,
                context.PartyHelper.FindLordTarget(player)),
            _ => (null, null)
        };

        if (action == null || target == null)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlayState = $"{cardType} → {target.Name.TextValue}";
            context.LogCardDecision(cardType.ToString(), target.Name.TextValue, GetCardTargetReason(cardType, target));
            return true;
        }

        return false;
    }

    private bool TryDraw(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        // Already have a card
        if (context.HasCard)
            return false;

        if (player.Level < ASTActions.AstralDraw.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.AstralDraw.ActionId))
            return false;

        // Only draw in combat to avoid wasting cards
        if (!context.InCombat)
            return false;

        var action = ASTActions.AstralDraw;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DrawState = "Drawing";
            context.LogCardDecision("Astral Draw", "Self", "Draw new card");
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

    #region Helper Methods

    private string GetCardTargetReason(ASTActions.CardType cardType, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        bool isMelee = AstraeaPartyHelper.IsMeleeDps(target);
        bool isRanged = AstraeaPartyHelper.IsRangedPhysicalDps(target) || AstraeaPartyHelper.IsCasterDps(target);
        bool isTank = AstraeaPartyHelper.IsTankRole(target);

        return cardType switch
        {
            ASTActions.CardType.TheBalance when isMelee => "Melee DPS (optimal)",
            ASTActions.CardType.TheBalance when isRanged => "Ranged DPS (fallback)",
            ASTActions.CardType.TheBalance when isTank => "Tank (no DPS available)",
            ASTActions.CardType.TheSpear when isRanged => "Ranged DPS (optimal)",
            ASTActions.CardType.TheSpear when isMelee => "Melee DPS (fallback)",
            ASTActions.CardType.TheSpear when isTank => "Tank (no DPS available)",
            ASTActions.CardType.Lord => "DPS target",
            _ => "Available target"
        };
    }

    #endregion
}
