using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.CalliopeCore.Modules;

/// <summary>
/// Handles Bard song rotation, buff management, and oGCD optimization.
/// Manages songs (WM -> MB -> AP), Raging Strikes, Battle Voice, Radiant Finale, Barrage,
/// and oGCD damage (Empyreal Arrow, Sidewinder, Pitch Perfect, Bloodletter).
/// </summary>
public sealed class BuffModule : ICalliopeModule
{
    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    // Song switching thresholds
    private const float SongSwitchThreshold = 3f; // Switch songs when timer drops below this

    public bool TryExecute(ICalliopeContext context, bool isMoving)
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

        // Find target for targeted oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // Priority 1: Pitch Perfect at 3 stacks (WM only)
        if (TryPitchPerfect(context, target))
            return true;

        // Priority 2: Song rotation (WM -> MB -> AP)
        if (TrySongRotation(context))
            return true;

        // Priority 3: Burst window buffs (RS -> BV -> RF)
        if (TryBurstBuffs(context))
            return true;

        // Priority 4: Barrage (use with Refulgent Arrow for burst)
        if (TryBarrage(context))
            return true;

        // Priority 5: Empyreal Arrow (guaranteed Repertoire)
        if (TryEmpyrealArrow(context, target))
            return true;

        // Priority 6: Sidewinder (burst damage)
        if (TrySidewinder(context, target))
            return true;

        // Priority 7: Bloodletter / Heartbreak Shot (dump charges)
        if (TryBloodletter(context, target))
            return true;

        context.Debug.BuffState = "No buff action";
        return false;
    }

    public void UpdateDebugState(ICalliopeContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(ICalliopeContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Pitch Perfect

    private bool TryPitchPerfect(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.PitchPerfect.MinLevel)
            return false;

        // Only usable during Wanderer's Minuet
        if (!context.IsWanderersMinuetActive)
            return false;

        // Use at 3 stacks for maximum damage (360 potency)
        // Or use remaining stacks before song ends
        bool shouldUse = context.Repertoire >= 3 ||
                         (context.Repertoire > 0 && context.SongTimer < SongSwitchThreshold);

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.PitchPerfect.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(BRDActions.PitchPerfect, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.PitchPerfect.Name;
            context.Debug.BuffState = $"Pitch Perfect ({context.Repertoire} stacks)";
            return true;
        }

        return false;
    }

    #endregion

    #region Song Rotation

    private bool TrySongRotation(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least Mage's Ballad for song rotation
        if (level < BRDActions.MagesBallad.MinLevel)
            return false;

        // Standard song rotation: WM -> MB -> AP -> WM
        // Switch when current song timer is low or no song active

        bool needSong = context.NoSongActive ||
                        (context.SongTimer < SongSwitchThreshold && !context.IsArmysPaeonActive) ||
                        (context.IsArmysPaeonActive && context.SongTimer < 12f); // Cut AP early for realignment

        if (!needSong)
            return false;

        // Priority: WM > MB > AP
        // WM is highest priority for burst alignment

        // Try Wanderer's Minuet
        if (level >= BRDActions.WanderersMinuet.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.WanderersMinuet.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(BRDActions.WanderersMinuet, player.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.WanderersMinuet.Name;
                    context.Debug.BuffState = "Wanderer's Minuet";
                    return true;
                }
            }
        }

        // Try Mage's Ballad
        if (context.ActionService.IsActionReady(BRDActions.MagesBallad.ActionId))
        {
            if (context.ActionService.ExecuteOgcd(BRDActions.MagesBallad, player.GameObjectId))
            {
                context.Debug.PlannedAction = BRDActions.MagesBallad.Name;
                context.Debug.BuffState = "Mage's Ballad";
                return true;
            }
        }

        // Try Army's Paeon
        if (level >= BRDActions.ArmysPaeon.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.ArmysPaeon.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(BRDActions.ArmysPaeon, player.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.ArmysPaeon.Name;
                    context.Debug.BuffState = "Army's Paeon";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Burst Buffs

    private bool TryBurstBuffs(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Raging Strikes first (2 minute CD)
        if (TryRagingStrikes(context))
            return true;

        // Battle Voice after Raging Strikes (2 minute CD)
        if (TryBattleVoice(context))
            return true;

        // Radiant Finale when we have Coda (110s CD)
        if (TryRadiantFinale(context))
            return true;

        return false;
    }

    private bool TryRagingStrikes(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.RagingStrikes.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasRagingStrikes)
            return false;

        // Use during WM for optimal burst alignment
        // Or use on cooldown if WM not available
        bool shouldUse = context.IsWanderersMinuetActive ||
                         level < BRDActions.WanderersMinuet.MinLevel;

        if (!shouldUse)
        {
            context.Debug.BuffState = "Waiting for WM alignment";
            return false;
        }

        if (!context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Raging Strikes (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BRDActions.RagingStrikes, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RagingStrikes.Name;
            context.Debug.BuffState = "Raging Strikes";
            return true;
        }

        return false;
    }

    private bool TryBattleVoice(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.BattleVoice.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasBattleVoice)
            return false;

        // Use with Raging Strikes for burst window
        if (!context.HasRagingStrikes && context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId))
        {
            // Raging Strikes should go first
            return false;
        }

        if (!context.ActionService.IsActionReady(BRDActions.BattleVoice.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Battle Voice (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BRDActions.BattleVoice, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.BattleVoice.Name;
            context.Debug.BuffState = "Battle Voice";
            return true;
        }

        return false;
    }

    private bool TryRadiantFinale(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.RadiantFinale.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasRadiantFinale)
            return false;

        // Need at least 1 Coda, ideally 3
        if (context.CodaCount == 0)
        {
            context.Debug.BuffState = "No Coda for RF";
            return false;
        }

        // Use during burst window with RS and BV
        bool shouldUse = context.HasRagingStrikes && context.HasBattleVoice;

        // Or use if we have 3 Coda and can't wait
        if (context.CodaCount >= 3)
            shouldUse = true;

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.RadiantFinale.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(BRDActions.RadiantFinale, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RadiantFinale.Name;
            context.Debug.BuffState = $"Radiant Finale ({context.CodaCount} Coda)";
            return true;
        }

        return false;
    }

    #endregion

    #region Barrage

    private bool TryBarrage(ICalliopeContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.Barrage.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasBarrage)
            return false;

        // Use during burst window
        bool shouldUse = context.HasRagingStrikes ||
                         !context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId);

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.Barrage.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(BRDActions.Barrage, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.Barrage.Name;
            context.Debug.BuffState = "Barrage";
            return true;
        }

        return false;
    }

    #endregion

    #region oGCD Damage

    private bool TryEmpyrealArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.EmpyrealArrow.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.EmpyrealArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(BRDActions.EmpyrealArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.EmpyrealArrow.Name;
            context.Debug.BuffState = "Empyreal Arrow";
            return true;
        }

        return false;
    }

    private bool TrySidewinder(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.Sidewinder.MinLevel)
            return false;

        // Prefer using during burst window
        bool shouldUse = context.HasRagingStrikes ||
                         !context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId);

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.Sidewinder.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(BRDActions.Sidewinder, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.Sidewinder.Name;
            context.Debug.BuffState = "Sidewinder";
            return true;
        }

        return false;
    }

    private bool TryBloodletter(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.Bloodletter.MinLevel)
            return false;

        if (context.BloodletterCharges == 0)
            return false;

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // Use Rain of Death for AoE (3+ targets)
        if (enemyCount >= 3 && level >= BRDActions.RainOfDeath.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.RainOfDeath.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(BRDActions.RainOfDeath, target.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.RainOfDeath.Name;
                    context.Debug.BuffState = $"Rain of Death ({context.BloodletterCharges} charges)";
                    return true;
                }
            }
        }

        // Use during MB (resets on proc) or if near cap
        bool shouldUse = context.IsMagesBalladActive ||
                         context.BloodletterCharges >= 3 ||
                         context.HasRagingStrikes;

        if (!shouldUse)
            return false;

        var action = BRDActions.GetBloodletter(level);
        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"{action.Name} ({context.BloodletterCharges} charges)";
            return true;
        }

        return false;
    }

    #endregion
}
