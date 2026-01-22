using Olympus.Data;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.EchidnaCore.Modules;

/// <summary>
/// Handles the Viper buff management.
/// Manages Serpent's Ire (party buff) and Reawaken timing.
/// </summary>
public sealed class BuffModule : IEchidnaModule
{
    public int Priority => 20; // After role actions
    public string Name => "Buff";

    public bool TryExecute(IEchidnaContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Serpent's Ire (party buff)
        if (TrySerpentsIre(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IEchidnaContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IEchidnaContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Serpent's Ire

    private bool TrySerpentsIre(IEchidnaContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < VPRActions.SerpentsIre.MinLevel)
            return false;

        // Check if Serpent's Ire is ready
        if (!context.ActionService.IsActionReady(VPRActions.SerpentsIre.ActionId))
        {
            context.Debug.BuffState = "Serpent's Ire on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Serpent's Ire (phase soon)";
            return false;
        }

        // Requirements for optimal Serpent's Ire usage:
        // 1. Have Noxious Gnash on target (damage buff)
        // 2. Have both Hunter's Instinct and Swiftscaled active
        // 3. Good Serpent Offering to follow up with Reawaken
        if (!context.HasNoxiousGnash)
        {
            context.Debug.BuffState = "Waiting for Noxious Gnash";
            return false;
        }

        if (!context.HasHuntersInstinct || !context.HasSwiftscaled)
        {
            context.Debug.BuffState = "Waiting for buffs";
            return false;
        }

        // Party coordination: Align with party burst window
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if party is about to burst - if so, execute to align
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = "Aligning Serpent's Ire with party burst";
                // Fall through to execute - we want to burst WITH the party
            }
            // Note: VPR has no raid buff to announce - we just listen and align
        }

        // Use Serpent's Ire
        if (context.ActionService.ExecuteOgcd(VPRActions.SerpentsIre, player.GameObjectId))
        {
            context.Debug.PlannedAction = VPRActions.SerpentsIre.Name;
            context.Debug.BuffState = "Activating Serpent's Ire";
            return true;
        }

        return false;
    }

    #endregion
}
