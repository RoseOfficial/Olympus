using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer oGCD abilities.
/// Manages Muses, Portraits, Subtractive Palette, and utility oGCDs.
/// </summary>
public sealed class BuffModule : IIrisModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IIrisContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for damage oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Priority 1: Star Prism finisher during Starstruck (this is actually a GCD but instant)
        // This is handled in DamageModule

        // Priority 2: Portraits - use immediately when ready
        if (TryPortrait(context, target))
            return true;

        // Priority 3: Starry Muse (align with raid buffs, use off cooldown)
        if (TryStarryMuse(context))
            return true;

        // Priority 4: Living Muse (dump charges during burst)
        if (TryLivingMuse(context, target))
            return true;

        // Priority 5: Striking Muse (use when hammer painted)
        if (TryStrikingMuse(context))
            return true;

        // Priority 6: Subtractive Palette (when gauge >= 50 and not already active)
        if (TrySubtractivePalette(context))
            return true;

        // Priority 7: Lucid Dreaming (when MP < 70%)
        if (TryLucidDreaming(context))
            return true;

        // Priority 8: Tempera Coat for mitigation
        // (Not automated - save for tankbusters)

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IIrisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IIrisContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryPortrait(IIrisContext context, IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Check Madeen first (higher priority, Lv.96+)
        if (context.MadeenReady && level >= PCTActions.RetributionOfTheMadeen.MinLevel)
        {
            if (context.ActionService.ExecuteOgcd(PCTActions.RetributionOfTheMadeen, target.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.RetributionOfTheMadeen.Name;
                context.Debug.BuffState = "Madeen";
                return true;
            }
        }

        // Check Mog of the Ages
        if (context.MogReady && level >= PCTActions.MogOfTheAges.MinLevel)
        {
            if (context.ActionService.ExecuteOgcd(PCTActions.MogOfTheAges, target.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.MogOfTheAges.Name;
                context.Debug.BuffState = "Mog";
                return true;
            }
        }

        return false;
    }

    private bool TryStarryMuse(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StarryMuse.MinLevel)
            return false;

        if (!context.StarryMuseReady)
            return false;

        // Need landscape canvas painted
        if (!context.HasLandscapeCanvas)
        {
            context.Debug.BuffState = "Need Landscape for Starry";
            return false;
        }

        // Don't use if already active
        if (context.HasStarryMuse)
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Starry Muse (phase soon)";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(PCTActions.StarryMuse.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
                // Fall through to execute - don't try to align when heavily desynced
            }
            // Check if another DPS is about to use a raid buff
            // If so, align our burst with theirs
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                // Another player is about to burst - align with them
                context.Debug.BuffState = "Aligning with party burst";
                // Fall through to execute and announce our intent
            }

            // Announce our intent to use Starry Muse
            partyCoord.AnnounceRaidBuffIntent(PCTActions.StarryMuse.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(PCTActions.StarryMuse, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StarryMuse.Name;
            context.Debug.BuffState = "Starry Muse (burst)";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(PCTActions.StarryMuse.ActionId, 120_000);

            return true;
        }

        return false;
    }

    private bool TryLivingMuse(IIrisContext context, IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.LivingMuse.MinLevel)
            return false;

        if (!context.LivingMuseReady)
            return false;

        // Need creature canvas painted
        if (!context.HasCreatureCanvas)
        {
            context.Debug.BuffState = "Need Creature for Muse";
            return false;
        }

        // Get the appropriate muse action based on creature type
        var museAction = PCTActions.GetLivingMuse(context.CreatureMotifType);

        if (context.ActionService.ExecuteOgcd(museAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = museAction.Name;
            context.Debug.BuffState = $"Living Muse ({context.LivingMuseCharges - 1} charges)";
            return true;
        }

        return false;
    }

    private bool TryStrikingMuse(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StrikingMuse.MinLevel)
            return false;

        if (!context.StrikingMuseReady)
            return false;

        // Need weapon canvas painted
        if (!context.HasWeaponCanvas)
        {
            context.Debug.BuffState = "Need Weapon for Striking";
            return false;
        }

        // Don't use if already have Hammer Time
        if (context.HasHammerTime)
            return false;

        if (context.ActionService.ExecuteOgcd(PCTActions.StrikingMuse, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StrikingMuse.Name;
            context.Debug.BuffState = "Striking Muse";
            return true;
        }

        return false;
    }

    private bool TrySubtractivePalette(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.SubtractivePalette.MinLevel)
            return false;

        if (!context.SubtractivePaletteReady)
            return false;

        // Already have subtractive buff
        if (context.HasSubtractivePalette)
            return false;

        // Need 50+ gauge
        if (!context.CanUseSubtractivePalette)
        {
            context.Debug.BuffState = $"Need 50 gauge ({context.PaletteGauge})";
            return false;
        }

        // Prefer to use during burst window
        // But don't overcap gauge (use at 75+ regardless)
        if (!context.IsInBurstWindow && context.PaletteGauge < 75)
        {
            context.Debug.BuffState = "Hold Subtractive for burst";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(PCTActions.SubtractivePalette, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.SubtractivePalette.Name;
            context.Debug.BuffState = "Subtractive Palette";
            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.LucidDreaming.MinLevel)
            return false;

        if (!context.LucidDreamingReady)
            return false;

        // Use when MP is below 70%
        if (context.MpPercent > 0.7f)
            return false;

        if (context.ActionService.ExecuteOgcd(PCTActions.LucidDreaming, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.LucidDreaming.Name;
            context.Debug.BuffState = "Lucid Dreaming (MP)";
            return true;
        }

        return false;
    }

    #endregion
}
