using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.CalliopeCore.Modules;

/// <summary>
/// Handles Bard song rotation, buff management, and oGCD optimization.
/// Manages songs (WM -> MB -> AP), Raging Strikes, Battle Voice, Radiant Finale, Barrage,
/// and oGCD damage (Empyreal Arrow, Sidewinder, Pitch Perfect, Bloodletter).
/// </summary>
public sealed class BuffModule : ICalliopeModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool IsInBurst => BurstHoldHelper.IsInBurst(_burstWindowService);

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

    #region Pitch Perfect

    private bool TryPitchPerfect(ICalliopeContext context, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnablePitchPerfect) return false;

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

            // Training: Record Pitch Perfect decision
            var reason = context.Repertoire >= 3 ? "Maximum stacks" : "Song ending soon";
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.PitchPerfect.ActionId, BRDActions.PitchPerfect.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"Pitch Perfect ({context.Repertoire} stacks, {reason})",
                    "Pitch Perfect is only usable during Wanderer's Minuet. Damage scales with Repertoire stacks: " +
                    "1 stack = 100 potency, 2 stacks = 220 potency, 3 stacks = 360 potency. Always aim for 3 stacks, " +
                    "but use remaining stacks before WM ends.")
                .Factors($"Repertoire: {context.Repertoire}/3", $"Song timer: {context.SongTimer:F1}s", "Wanderer's Minuet active")
                .Alternatives("Wait for 3 stacks", "Song not ending soon")
                .Tip("Use Pitch Perfect at 3 stacks for maximum damage. Don't waste stacks when WM is about to end.")
                .Concept(BrdConcepts.PitchPerfect)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.PitchPerfect, context.Repertoire >= 3, "Stack consumption");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RepertoireStacks, true, "Repertoire management");

            return true;
        }

        return false;
    }

    #endregion

    #region Song Rotation

    private bool TrySongRotation(ICalliopeContext context)
    {
        if (!context.Configuration.Bard.EnableSongRotation)
            return false;

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

                    // Training: Record Wanderer's Minuet decision
                    var previousSongWm = context.NoSongActive ? "None" :
                        context.IsMagesBalladActive ? "Mage's Ballad" :
                        context.IsArmysPaeonActive ? "Army's Paeon" : "Unknown";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BRDActions.WanderersMinuet.ActionId, BRDActions.WanderersMinuet.Name)
                        .AsSong(previousSongWm, context.SongTimer)
                        .Reason(
                            $"Wanderer's Minuet (switching from {previousSongWm})",
                            "Wanderer's Minuet is the highest priority song for burst. Grants Repertoire stacks for Pitch Perfect. " +
                            "Standard rotation: WM → MB → AP → WM. Start burst with WM for Raging Strikes alignment.")
                        .Factors(context.NoSongActive ? "No song active" : $"Previous song: {previousSongWm}", "Highest priority song", "Enables Pitch Perfect")
                        .Alternatives("Current song still has time", "WM on cooldown")
                        .Tip("Always start your song rotation with Wanderer's Minuet. It aligns best with 2-minute burst windows.")
                        .Concept(BrdConcepts.WanderersMinuet)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.WanderersMinuet, true, "Song activation");
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.SongRotation, true, "Song rotation");
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.SongSwitching, !context.NoSongActive, "Song transition");

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

                // Training: Record Mage's Ballad decision
                var previousSongMb = context.NoSongActive ? "None" :
                    context.IsWanderersMinuetActive ? "Wanderer's Minuet" :
                    context.IsArmysPaeonActive ? "Army's Paeon" : "Unknown";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.MagesBallad.ActionId, BRDActions.MagesBallad.Name)
                    .AsSong(previousSongMb, context.SongTimer)
                    .Reason(
                        $"Mage's Ballad (switching from {previousSongMb})",
                        "Mage's Ballad resets Bloodletter/Rain of Death cooldown on Repertoire procs. Second in the song rotation. " +
                        "Great for oGCD damage uptime. Provides 1% damage buff to party.")
                    .Factors(context.NoSongActive ? "No song active" : $"Previous song: {previousSongMb}", "Resets Bloodletter on procs", "WM on cooldown")
                    .Alternatives("Wanderer's Minuet available", "Current song still has time")
                    .Tip("Use Mage's Ballad after WM ends. Spam Bloodletter when it resets from Repertoire procs.")
                    .Concept(BrdConcepts.MagesBallad)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.MagesBallad, true, "Song activation");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.SongRotation, true, "Song rotation");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.SongSwitching, !context.NoSongActive, "Song transition");

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

                    // Training: Record Army's Paeon decision
                    var previousSongAp = context.NoSongActive ? "None" :
                        context.IsWanderersMinuetActive ? "Wanderer's Minuet" :
                        context.IsMagesBalladActive ? "Mage's Ballad" : "Unknown";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BRDActions.ArmysPaeon.ActionId, BRDActions.ArmysPaeon.Name)
                        .AsSong(previousSongAp, context.SongTimer)
                        .Reason(
                            $"Army's Paeon (switching from {previousSongAp})",
                            "Army's Paeon grants attack speed stacks via Repertoire. Lowest priority song, used as filler. " +
                            "Cut early (around 12s remaining) to realign with WM for burst windows.")
                        .Factors(context.NoSongActive ? "No song active" : $"Previous song: {previousSongAp}", "Filler song", "WM and MB on cooldown")
                        .Alternatives("WM or MB available", "Current song still has time")
                        .Tip("Army's Paeon is the filler song. Cut it early to get back to WM faster for burst alignment.")
                        .Concept(BrdConcepts.ArmysPaeon)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.ArmysPaeon, true, "Song activation");
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.SongRotation, true, "Song rotation");
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.SongSwitching, !context.NoSongActive, "Song transition");

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
        if (!context.Configuration.Bard.EnableRagingStrikes) return false;

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
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Raging Strikes (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BRDActions.RagingStrikes, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RagingStrikes.Name;
            context.Debug.BuffState = "Raging Strikes";

            // Training: Record Raging Strikes decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.RagingStrikes.ActionId, BRDActions.RagingStrikes.Name)
                .AsRangedBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason(
                    "Raging Strikes (2-minute burst window)",
                    "Raging Strikes is BRD's personal 2-minute buff (+15% damage). Always align with Wanderer's Minuet " +
                    "for maximum Pitch Perfect damage. Follow with Battle Voice and Radiant Finale.")
                .Factors(context.IsWanderersMinuetActive ? "WM active" : "WM not needed at this level", "120s cooldown ready")
                .Alternatives("Wait for WM alignment", "Phase transition soon")
                .Tip("Use Raging Strikes during Wanderer's Minuet. Follow immediately with Battle Voice and Radiant Finale.")
                .Concept(BrdConcepts.RagingStrikes)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RagingStrikes, true, "Burst activation");

            return true;
        }

        return false;
    }

    private bool TryBattleVoice(ICalliopeContext context)
    {
        if (!context.Configuration.Bard.EnableBattleVoice) return false;

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
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Battle Voice (phase soon)";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(BRDActions.BattleVoice.ActionId))
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

            // Announce our intent to use Battle Voice
            partyCoord.AnnounceRaidBuffIntent(BRDActions.BattleVoice.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(BRDActions.BattleVoice, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.BattleVoice.Name;
            context.Debug.BuffState = "Battle Voice";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(BRDActions.BattleVoice.ActionId, 120_000);

            // Training: Record Battle Voice decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.BattleVoice.ActionId, BRDActions.BattleVoice.Name)
                .AsRaidBuff()
                .Reason(
                    "Battle Voice (party-wide raid buff)",
                    "Battle Voice grants +20% direct hit rate to the entire party for 20s. Always use with Raging Strikes " +
                    "during burst windows. Coordinate with other DPS raid buffs for maximum party damage.")
                .Factors(context.HasRagingStrikes ? "Raging Strikes active" : "RS on cooldown", "120s cooldown ready", "Party burst alignment")
                .Alternatives("Wait for Raging Strikes", "Phase transition soon")
                .Tip("Use Battle Voice immediately after Raging Strikes. This is your party contribution to 2-minute burst.")
                .Concept(BrdConcepts.BattleVoice)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.BattleVoice, true, "Raid buff activation");

            return true;
        }

        return false;
    }

    private bool TryRadiantFinale(ICalliopeContext context)
    {
        if (!context.Configuration.Bard.EnableRadiantFinale) return false;

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

        // Party coordination: Synchronize with other Olympus instances
        // Note: Radiant Finale follows Battle Voice in the burst sequence,
        // so we check alignment but don't announce separately (BV already announced)
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            if (!partyCoord.IsRaidBuffAligned(BRDActions.RadiantFinale.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using RF independently";
                // Fall through to execute
            }

            // Announce Radiant Finale intent (shorter CD than BV, separate coordination)
            partyCoord.AnnounceRaidBuffIntent(BRDActions.RadiantFinale.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(BRDActions.RadiantFinale, player.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RadiantFinale.Name;
            context.Debug.BuffState = $"Radiant Finale ({context.CodaCount} Coda)";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(BRDActions.RadiantFinale.ActionId, 110_000);

            // Training: Record Radiant Finale decision
            var codaBonus = context.CodaCount switch
            {
                1 => "2%",
                2 => "4%",
                3 => "6%",
                _ => "0%"
            };
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.RadiantFinale.ActionId, BRDActions.RadiantFinale.Name)
                .AsRaidBuff()
                .Reason(
                    $"Radiant Finale ({context.CodaCount} Coda = {codaBonus} party damage)",
                    $"Radiant Finale grants party damage bonus based on Coda: 1 Coda = 2%, 2 Coda = 4%, 3 Coda = 6%. " +
                    "Each song played grants a Coda. Use during burst window with RS and BV. Grants Radiant Encore Ready.")
                .Factors($"Coda: {context.CodaCount}/3", context.HasRagingStrikes ? "RS active" : "RS not active", context.HasBattleVoice ? "BV active" : "BV not active")
                .Alternatives("Wait for more Coda", "Wait for RS/BV")
                .Tip("Aim for 3 Coda before Radiant Finale for maximum 6% party damage. Follow up with Radiant Encore.")
                .Concept(BrdConcepts.RadiantFinale)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RadiantFinale, context.CodaCount >= 3, "Coda optimization");

            return true;
        }

        return false;
    }

    #endregion

    #region Barrage

    private bool TryBarrage(ICalliopeContext context)
    {
        if (!context.Configuration.Bard.EnableBarrage) return false;

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

            // Training: Record Barrage decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.Barrage.ActionId, BRDActions.Barrage.Name)
                .AsRangedBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason(
                    "Barrage (triple Refulgent Arrow)",
                    "Barrage makes your next Refulgent Arrow hit 3 times. Huge burst damage. " +
                    "Always use during Raging Strikes. Wait for Hawk's Eye proc, then use Refulgent. Grants Resonant Arrow Ready.")
                .Factors(context.HasRagingStrikes ? "RS active" : "RS on cooldown", "120s cooldown ready")
                .Alternatives("Wait for Raging Strikes")
                .Tip("Use Barrage during RS, then immediately Refulgent Arrow (wait for proc if needed). Follow with Resonant Arrow.")
                .Concept(BrdConcepts.Barrage)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.Barrage, context.HasRagingStrikes, "Burst window usage");

            return true;
        }

        return false;
    }

    #endregion

    #region oGCD Damage

    private bool TryEmpyrealArrow(ICalliopeContext context, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableEmpyrealArrow) return false;

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

            // Training: Record Empyreal Arrow decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.EmpyrealArrow.ActionId, BRDActions.EmpyrealArrow.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Empyreal Arrow (guaranteed Repertoire)",
                    "Empyreal Arrow is a high-potency oGCD that guarantees a Repertoire proc regardless of which song is active. " +
                    "Use on cooldown. During WM, this means guaranteed Pitch Perfect stack.")
                .Factors("15s cooldown ready", context.IsWanderersMinuetActive ? "WM active (Pitch Perfect stack)" : "Song active")
                .Alternatives("On cooldown")
                .Tip("Use Empyreal Arrow on cooldown. It's free damage and a guaranteed Repertoire proc.")
                .Concept(BrdConcepts.EmpyrealArrow)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.EmpyrealArrow, true, "oGCD usage");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RepertoireStacks, true, "Repertoire generation");

            return true;
        }

        return false;
    }

    private bool TrySidewinder(ICalliopeContext context, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableSidewinder) return false;

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

            // Training: Record Sidewinder decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BRDActions.Sidewinder.ActionId, BRDActions.Sidewinder.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Sidewinder (burst damage oGCD)",
                    "Sidewinder is a high-potency oGCD on a 60s cooldown. Use during burst windows with Raging Strikes " +
                    "for maximum benefit from the damage buff.")
                .Factors(context.HasRagingStrikes ? "RS active" : "RS on cooldown", "60s cooldown ready")
                .Alternatives("Wait for Raging Strikes")
                .Tip("Use Sidewinder during Raging Strikes windows. It's one of your highest damage oGCDs.")
                .Concept(BrdConcepts.EmpyrealArrow) // Grouped with other oGCDs
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.EmpyrealArrow, context.HasRagingStrikes, "Burst oGCD usage");

            return true;
        }

        return false;
    }

    private bool TryBloodletter(ICalliopeContext context, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableBloodletter) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.Bloodletter.MinLevel)
            return false;

        if (context.BloodletterCharges == 0)
            return false;

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // Use Rain of Death for AoE (3+ targets)
        if (context.Configuration.Bard.EnableAoERotation &&
            enemyCount >= context.Configuration.Bard.AoEMinTargets && level >= BRDActions.RainOfDeath.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.RainOfDeath.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(BRDActions.RainOfDeath, target.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.RainOfDeath.Name;
                    context.Debug.BuffState = $"Rain of Death ({context.BloodletterCharges} charges)";

                    // Training: Record Rain of Death decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BRDActions.RainOfDeath.ActionId, BRDActions.RainOfDeath.Name)
                        .AsAoE(enemyCount)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason(
                            $"Rain of Death (AoE, {context.BloodletterCharges} charges)",
                            "Rain of Death is the AoE version of Bloodletter. Use at 3+ targets. " +
                            "Shares charges with Bloodletter. During Mage's Ballad, Repertoire resets the cooldown.")
                        .Factors($"Enemies: {enemyCount}", $"Charges: {context.BloodletterCharges}/3", context.IsMagesBalladActive ? "MB resets on procs" : "")
                        .Alternatives("Use Bloodletter for single target")
                        .Tip("Switch to Rain of Death at 3+ enemies. Spam during Mage's Ballad when charges reset.")
                        .Concept(BrdConcepts.BloodletterManagement)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.BloodletterManagement, true, "AoE charge usage");

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

            // Training: Record Bloodletter decision
            var bloodletterReason = context.IsMagesBalladActive ? "MB resets on procs" :
                         context.BloodletterCharges >= 3 ? "Preventing overcap" :
                         context.HasRagingStrikes ? "Burst window" : "Using charges";
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"{action.Name} ({bloodletterReason})",
                    "Bloodletter has 3 charges (15s recharge). During Mage's Ballad, Repertoire procs reset the cooldown, " +
                    "so spam it aggressively. Don't let charges overcap. Use during burst for extra damage.")
                .Factors($"Charges: {context.BloodletterCharges}/3", context.IsMagesBalladActive ? "MB active (resets on procs)" : "", context.HasRagingStrikes ? "RS active" : "")
                .Alternatives("Save for MB phase", "Wait for burst window")
                .Tip("During Mage's Ballad, spam Bloodletter as charges reset. Otherwise, don't overcap.")
                .Concept(BrdConcepts.BloodletterManagement)
                .Record();
            context.TrainingService?.RecordConceptApplication(BrdConcepts.BloodletterManagement, true, "Charge usage");

            return true;
        }

        return false;
    }

    #endregion
}
