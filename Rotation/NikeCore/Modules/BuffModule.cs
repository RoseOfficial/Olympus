using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.NikeCore.Modules;

/// <summary>
/// Handles Samurai buff management.
/// Manages Meikyo Shisui, Ikishoten, Shoha, and defensive cooldowns.
/// </summary>
public sealed class BuffModule : INikeModule
{
    public int Priority => 20; // Before Damage
    public string Name => "Buff";

    // Thresholds
    private const int KenkiThresholdForIkishoten = 50; // Don't waste Kenki
    private const int MeditationMaxStacks = 3;
    private const float BuffRefreshThreshold = 5f; // Refresh buffs when < 5s remaining

    public bool TryExecute(INikeContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // oGCD Phase only
        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        // Priority 1: Shoha at 3 Meditation stacks
        if (TryShoha(context, target))
            return true;

        // Priority 2: Zanshin when ready (high priority oGCD)
        if (TryZanshin(context, target))
            return true;

        // Priority 3: Ikishoten for Kenki + Ogi Namikiri Ready
        if (TryIkishoten(context))
            return true;

        // Priority 4: Meikyo Shisui for combo skip
        if (TryMeikyoShisui(context))
            return true;

        // Priority 5: Senei/Guren burst
        if (TryBurstKenki(context, target))
            return true;

        // Priority 6: True North for positionals
        if (TryTrueNorth(context))
            return true;

        context.Debug.BuffState = "No buff action";
        return false;
    }

    public void UpdateDebugState(INikeContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(INikeContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Shoha

    private bool TryShoha(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.Shoha.MinLevel)
            return false;

        // Need 3 Meditation stacks
        if (context.Meditation < MeditationMaxStacks)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.Shoha.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SAMActions.Shoha, target.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.Shoha.Name;
            context.Debug.BuffState = $"Shoha ({context.Meditation} Med)";
            return true;
        }

        return false;
    }

    #endregion

    #region Zanshin

    private bool TryZanshin(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.Zanshin.MinLevel)
            return false;

        // Need Zanshin Ready buff and 50 Kenki
        if (!context.HasZanshinReady)
            return false;

        if (context.Kenki < 50)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.Zanshin.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SAMActions.Zanshin, target.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.Zanshin.Name;
            context.Debug.BuffState = "Zanshin";
            return true;
        }

        return false;
    }

    #endregion

    #region Ikishoten

    private bool TryIkishoten(INikeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.Ikishoten.MinLevel)
            return false;

        // Don't use if we'd overcap Kenki
        if (context.Kenki > KenkiThresholdForIkishoten)
            return false;

        // Don't use if we already have Ogi Namikiri Ready
        if (context.HasOgiNamikiriReady)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.Ikishoten.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Ikishoten (phase soon)";
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
                context.Debug.BuffState = "Aligning Ikishoten with party burst";
                // Fall through to execute - we want to burst WITH the party
            }
            // Note: SAM has no raid buff to announce - we just listen and align
        }

        if (context.ActionService.ExecuteOgcd(SAMActions.Ikishoten, player.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.Ikishoten.Name;
            context.Debug.BuffState = "Ikishoten";
            return true;
        }

        return false;
    }

    #endregion

    #region Meikyo Shisui

    private bool TryMeikyoShisui(INikeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.MeikyoShisui.MinLevel)
            return false;

        // Don't use if we already have Meikyo active
        if (context.HasMeikyoShisui)
            return false;

        // Check if we should use Meikyo based on buff state
        // Use Meikyo when:
        // 1. We need to refresh Fugetsu/Fuka soon
        // 2. We have 0 Sen and want to quickly build to 3
        var shouldUseMeikyo = false;

        // Need to refresh buffs
        if (!context.HasFugetsu || context.FugetsuRemaining < BuffRefreshThreshold)
            shouldUseMeikyo = true;
        if (!context.HasFuka || context.FukaRemaining < BuffRefreshThreshold)
            shouldUseMeikyo = true;

        // Want to quickly build Sen
        if (context.SenCount == 0 && context.InCombat)
            shouldUseMeikyo = true;

        if (!shouldUseMeikyo)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.MeikyoShisui.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SAMActions.MeikyoShisui, player.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.MeikyoShisui.Name;
            context.Debug.BuffState = "Meikyo Shisui";
            return true;
        }

        return false;
    }

    #endregion

    #region Burst Kenki (Senei/Guren)

    private bool TryBurstKenki(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least 25 Kenki
        if (context.Kenki < 25)
            return false;

        // Count nearby enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3 && level >= SAMActions.Guren.MinLevel;

        if (useAoe)
        {
            if (!context.ActionService.IsActionReady(SAMActions.Guren.ActionId))
                return false;

            if (context.ActionService.ExecuteOgcd(SAMActions.Guren, player.GameObjectId))
            {
                context.Debug.PlannedAction = SAMActions.Guren.Name;
                context.Debug.BuffState = $"Guren ({enemyCount} enemies)";
                return true;
            }
        }
        else if (level >= SAMActions.Senei.MinLevel)
        {
            if (!context.ActionService.IsActionReady(SAMActions.Senei.ActionId))
                return false;

            if (context.ActionService.ExecuteOgcd(SAMActions.Senei, target.GameObjectId))
            {
                context.Debug.PlannedAction = SAMActions.Senei.Name;
                context.Debug.BuffState = "Senei";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region True North

    private bool TryTrueNorth(INikeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.TrueNorth.MinLevel)
            return false;

        // Already have True North
        if (context.HasTrueNorth)
            return false;

        // Target immune to positionals
        if (context.TargetHasPositionalImmunity)
            return false;

        // Check if we're about to use a positional finisher and not in position
        var needPositional = false;

        // Will use Gekko (rear) soon
        if (context.HasGetsu == false && context.ComboStep == 2 && context.LastComboAction == SAMActions.Jinpu.ActionId)
        {
            if (!context.IsAtRear)
                needPositional = true;
        }

        // Will use Kasha (flank) soon
        if (context.HasKa == false && context.ComboStep == 2 && context.LastComboAction == SAMActions.Shifu.ActionId)
        {
            if (!context.IsAtFlank)
                needPositional = true;
        }

        // During Meikyo, we'll be using finishers
        if (context.HasMeikyoShisui && (!context.IsAtRear && !context.IsAtFlank))
        {
            needPositional = true;
        }

        if (!needPositional)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.TrueNorth.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SAMActions.TrueNorth, player.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.TrueNorth.Name;
            context.Debug.BuffState = "True North";
            return true;
        }

        return false;
    }

    #endregion
}
