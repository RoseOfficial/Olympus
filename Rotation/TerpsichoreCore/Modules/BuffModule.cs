using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles Dancer dance execution, buff management, and oGCD optimization.
/// Manages dances (Standard/Technical Step), Devilment, Flourish,
/// and oGCD damage (Fan Dance I/II/III/IV).
/// </summary>
public sealed class BuffModule : ITerpsichoreModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool IsInBurst => _burstWindowService?.IsInBurstWindow == true;

    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    public bool TryExecute(ITerpsichoreContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Execute dance steps when dancing
        if (context.IsDancing)
        {
            if (TryExecuteDanceStep(context))
                return true;
        }

        // oGCDs require weave windows
        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

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

        // Priority 2: Technical Step (2-min burst window opener)
        if (TryTechnicalStep(context))
            return true;

        // Priority 3: Devilment (weave after Technical Finish)
        if (TryDevilment(context))
            return true;

        // Priority 4: Standard Step (30s CD, use on cooldown)
        if (TryStandardStep(context))
            return true;

        // Priority 5: Flourish (during burst, grants all procs)
        if (TryFlourish(context))
            return true;

        // Priority 6: Fan Dance IV (Fourfold proc, use during burst)
        if (TryFanDanceIV(context, target))
            return true;

        // Priority 7: Fan Dance III (Threefold proc, use before I/II)
        if (TryFanDanceIII(context, target))
            return true;

        // Priority 8: Fan Dance I/II (dump feathers at 4 or during burst)
        if (TryFanDance(context, target))
            return true;

        context.Debug.BuffState = "No buff action";
        return false;
    }

    public void UpdateDebugState(ITerpsichoreContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(ITerpsichoreContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Dance Execution

    private bool TryExecuteDanceStep(ITerpsichoreContext context)
    {
        var player = context.Player;
        var currentStep = context.CurrentStep;

        // Get the action for the current step
        var stepAction = DNCActions.GetStepAction(currentStep);
        if (stepAction == null)
        {
            // No step to execute, try finish
            return TryDanceFinish(context);
        }

        // Execute the step - dance steps are GCDs with 1s recast
        if (!context.CanExecuteGcd)
        {
            context.Debug.BuffState = $"Step {currentStep} - waiting for GCD";
            return false;
        }

        if (context.ActionService.ExecuteGcd(stepAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = stepAction.Name;
            context.Debug.BuffState = $"Dance step: {stepAction.Name}";

            // Training: Record dance step execution
            TrainingHelper.Decision(context.TrainingService)
                .Action(stepAction.ActionId, stepAction.Name)
                .AsRangedDamage()
                .Reason(
                    $"Dance step {context.StepIndex + 1}",
                    "Dance steps must be executed in the correct order shown on the Step Gauge. Each step " +
                    "corresponds to a specific button (Emboite=Red, Entrechat=Blue, Jete=Green, Pirouette=Yellow). " +
                    "Complete all steps quickly to finish the dance.")
                .Factors($"Step {context.StepIndex + 1}/{(context.StepIndex >= 2 ? 4 : 2)}", "Dance in progress")
                .Alternatives("Wait for dance to end")
                .Tip("Execute dance steps as quickly as possible - each step is a 1s GCD.")
                .Concept(DncConcepts.DanceExecution)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.DanceExecution, true, "Step execution");

            return true;
        }

        return false;
    }

    private bool TryDanceFinish(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which finish based on step count
        // Standard Step = 2 steps, Technical Step = 4 steps
        var completedSteps = context.StepIndex;

        // Check if we've completed all required steps
        // Technical Step requires 4 steps
        if (completedSteps >= 4 && level >= DNCActions.TechnicalFinish.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.TechnicalFinish.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.TechnicalFinish, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.TechnicalFinish.Name;
                    context.Debug.BuffState = "Technical Finish";

                    // Notify coordination service that we used the raid buff
                    context.PartyCoordinationService?.OnRaidBuffUsed(DNCActions.TechnicalFinish.ActionId, 120_000);

                    // Training: Record Technical Finish
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.TechnicalFinish.ActionId, DNCActions.TechnicalFinish.Name)
                        .AsRaidBuff()
                        .Reason(
                            "4-step dance complete - Technical Finish!",
                            "Technical Finish is DNC's main raid buff providing 5% damage bonus to the party for 20s. " +
                            "It's on a 2-minute cooldown and should align with other party raid buffs. " +
                            "Follow immediately with Devilment and Flourish for maximum burst.")
                        .Factors("4 dance steps completed", "2-minute raid buff", "Party damage +5%")
                        .Alternatives("Dance not complete")
                        .Tip("Technical Finish is your most important raid buff - always complete the 4-step dance.")
                        .Concept(DncConcepts.TechnicalStep)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.TechnicalStep, true, "Raid buff applied");
                    context.TrainingService?.RecordConceptApplication(DncConcepts.BurstAlignment, true, "Burst window opened");

                    return true;
                }
            }
        }

        // Standard Step requires 2 steps
        if (completedSteps >= 2)
        {
            if (context.ActionService.IsActionReady(DNCActions.StandardFinish.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.StandardFinish, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.StandardFinish.Name;
                    context.Debug.BuffState = "Standard Finish";

                    // Training: Record Standard Finish
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.StandardFinish.ActionId, DNCActions.StandardFinish.Name)
                        .AsSong("Standard Step", 30f)
                        .Reason(
                            "2-step dance complete - Standard Finish!",
                            "Standard Finish provides a personal 5% damage buff for 60s and deals high damage. " +
                            "It's on a 30s cooldown and should be used on cooldown outside of Technical Step windows. " +
                            "The buff also applies to your dance partner.")
                        .Factors("2 dance steps completed", "30s cooldown", "Personal +5% damage")
                        .Alternatives("Dance not complete")
                        .Tip("Keep Standard Finish buff active at all times - use on cooldown.")
                        .Concept(DncConcepts.StandardStep)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.StandardStep, true, "Personal buff applied");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Dance Initiation

    private bool TryTechnicalStep(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableTechnicalStep) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.TechnicalStep.MinLevel)
            return false;

        // Don't start if already dancing
        if (context.IsDancing)
            return false;

        // Check if Technical Step is ready
        if (!context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Technical Step (phase soon)";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(DNCActions.TechnicalFinish.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
                // Fall through to execute - don't try to align when heavily desynced
            }
            // Check if another DPS is about to use a raid buff
            // If so, align our burst with theirs
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = "Aligning with party burst";
                // Fall through to execute and announce our intent
            }

            // Announce our intent to use Technical Finish (the buff effect)
            partyCoord.AnnounceRaidBuffIntent(DNCActions.TechnicalFinish.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(DNCActions.TechnicalStep, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.TechnicalStep.Name;
            context.Debug.BuffState = "Technical Step";

            // Training: Record Technical Step initiation
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.TechnicalStep.ActionId, DNCActions.TechnicalStep.Name)
                .AsRaidBuff()
                .Reason(
                    "Starting 4-step Technical dance",
                    "Technical Step begins a 4-step dance sequence that ends with Technical Finish, DNC's 2-minute " +
                    "raid buff. Time this to align with other party raid buffs. After finishing, immediately use " +
                    "Devilment and Flourish for maximum burst damage.")
                .Factors("Off cooldown", "Not already dancing", "2-minute burst window")
                .Alternatives("Already dancing", "Phase transition soon", "Raid buffs not aligned")
                .Tip("Plan your Technical Step to align with party buffs every 2 minutes.")
                .Concept(DncConcepts.TechnicalStep)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.BurstAlignment, true, "Burst preparation");

            return true;
        }

        return false;
    }

    private bool TryStandardStep(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableStandardStep) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.StandardStep.MinLevel)
            return false;

        // Don't start if already dancing
        if (context.IsDancing)
            return false;

        // Check if Standard Step is ready
        if (!context.ActionService.IsActionReady(DNCActions.StandardStep.ActionId))
            return false;

        // Don't use Standard Step if Technical Step is coming up soon (within 5s)
        if (level >= DNCActions.TechnicalStep.MinLevel)
        {
            var techCd = context.ActionService.GetCooldownRemaining(DNCActions.TechnicalStep.ActionId);
            if (techCd > 0 && techCd < 5f)
            {
                context.Debug.BuffState = "Holding Standard for Technical";
                return false;
            }
        }

        if (context.ActionService.ExecuteOgcd(DNCActions.StandardStep, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.StandardStep.Name;
            context.Debug.BuffState = "Standard Step";

            // Training: Record Standard Step initiation
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.StandardStep.ActionId, DNCActions.StandardStep.Name)
                .AsSong("None", 0f)
                .Reason(
                    "Starting 2-step Standard dance",
                    "Standard Step begins a 2-step dance sequence that ends with Standard Finish. Use on cooldown " +
                    "to maintain the 5% damage buff. Hold if Technical Step will be ready within 5 seconds to " +
                    "avoid delaying your burst window.")
                .Factors("Off cooldown", "Not already dancing", "Technical Step not imminent")
                .Alternatives("Technical Step coming soon", "Already dancing")
                .Tip("Keep Standard Finish buff active - it's your most important personal buff.")
                .Concept(DncConcepts.StandardStep)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.DanceTimers, true, "Dance initiated");

            return true;
        }

        return false;
    }

    #endregion

    #region Burst Buffs

    private bool TryDevilment(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableDevilment) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Devilment.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasDevilment)
            return false;

        // Use after Technical Finish for optimal burst
        // Or if Technical Step not available
        bool shouldUse = context.HasTechnicalFinish ||
                         level < DNCActions.TechnicalStep.MinLevel ||
                         !context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId);

        if (!shouldUse)
        {
            context.Debug.BuffState = "Waiting for Technical Finish";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.Devilment.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Devilment (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DNCActions.Devilment, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Devilment.Name;
            context.Debug.BuffState = "Devilment";

            // Training: Record Devilment decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Devilment.ActionId, DNCActions.Devilment.Name)
                .AsRangedBurst()
                .Reason(
                    "Devilment for burst window",
                    "Devilment provides +20% Critical Hit and Direct Hit rate for 20s. Always use immediately " +
                    "after Technical Finish to maximize burst damage. Also grants Flourishing Starfall at Lv.90+ " +
                    "for Starfall Dance.")
                .Factors("Technical Finish active", "+20% Crit/DH", "Grants Starfall Dance proc (Lv.90+)")
                .Alternatives("Wait for Technical Finish", "Already active")
                .Tip("Devilment is your personal burst buff - pair it with Technical Finish.")
                .Concept(DncConcepts.Devilment)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.Devilment, true, "Burst buff activated");
            context.TrainingService?.RecordConceptApplication(DncConcepts.BurstAlignment, true, "Burst window");

            return true;
        }

        return false;
    }

    private bool TryFlourish(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableFlourish) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Flourish.MinLevel)
            return false;

        // Don't use during dance
        if (context.IsDancing)
            return false;

        // Use during burst window (Devilment or Technical active)
        bool shouldUse = context.HasDevilment || context.HasTechnicalFinish;

        // Or use if buffs won't be up for a while
        if (!shouldUse)
        {
            var devilmentCd = context.ActionService.GetCooldownRemaining(DNCActions.Devilment.ActionId);
            shouldUse = devilmentCd > 30f;
        }

        if (!shouldUse)
            return false;

        // Don't overcap procs - wait if we have symmetry/flow
        if (context.HasSilkenSymmetry || context.HasSilkenFlow)
        {
            context.Debug.BuffState = "Consume procs before Flourish";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.Flourish.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.Flourish, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Flourish.Name;
            context.Debug.BuffState = "Flourish";

            // Training: Record Flourish decision
            var burstReason = context.HasDevilment || context.HasTechnicalFinish
                ? "Used during burst window"
                : "Used to prevent overcapping";
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Flourish.ActionId, DNCActions.Flourish.Name)
                .AsRangedBurst()
                .Reason(
                    burstReason,
                    "Flourish grants all four procs (Silken Symmetry, Silken Flow, Threefold Fan, Fourfold Fan). " +
                    "Best used during burst windows to maximize proc damage. Don't use if you already have " +
                    "Symmetry/Flow procs to avoid overcapping.")
                .Factors("No existing procs", context.HasDevilment ? "Devilment active" : "Burst window", "Grants all 4 procs")
                .Alternatives("Already have Symmetry/Flow procs", "Would overcap procs")
                .Tip("Consume Symmetry/Flow procs before using Flourish to avoid wasting procs.")
                .Concept(DncConcepts.Flourish)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.Flourish, true, "All procs granted");

            return true;
        }

        return false;
    }

    #endregion

    #region Fan Dance oGCDs

    private bool TryFanDanceIV(ITerpsichoreContext context, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableFanDanceIV) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDanceIV.MinLevel)
            return false;

        if (!context.HasFourfoldFanDance)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIV.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceIV, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDanceIV.Name;
            context.Debug.BuffState = "Fan Dance IV";

            // Training: Record Fan Dance IV decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.FanDanceIV.ActionId, DNCActions.FanDanceIV.Name)
                .AsProc("Fourfold Fan Dance")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Fourfold Fan proc - highest priority oGCD",
                    "Fan Dance IV is granted by Flourish (Fourfold Fan Dance buff). It's a high-potency cone AoE " +
                    "that should be used before other Fan Dances. Use during burst windows for maximum damage.")
                .Factors("Fourfold Fan Dance proc active", "High potency oGCD", "Cone AoE")
                .Alternatives("No Fourfold proc")
                .Tip("Fan Dance IV has the highest priority among Fan Dances - use it first.")
                .Concept(DncConcepts.FourfoldFan)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.FourfoldFan, true, "Proc consumed");

            return true;
        }

        return false;
    }

    private bool TryFanDanceIII(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDanceIII.MinLevel)
            return false;

        if (!context.HasThreefoldFanDance)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIII.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceIII, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDanceIII.Name;
            context.Debug.BuffState = "Fan Dance III";

            // Training: Record Fan Dance III decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.FanDanceIII.ActionId, DNCActions.FanDanceIII.Name)
                .AsProc("Threefold Fan Dance")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Threefold Fan proc - use before Fan Dance I/II",
                    "Fan Dance III is granted by Fan Dance I/II (Threefold Fan Dance buff). It's a high-potency " +
                    "cone AoE oGCD. Use it before spending more feathers to avoid losing the proc.")
                .Factors("Threefold Fan Dance proc active", "Cone AoE oGCD", "Triggers from Fan Dance I/II")
                .Alternatives("No Threefold proc")
                .Tip("Fan Dance III has higher priority than Fan Dance I/II - consume it first.")
                .Concept(DncConcepts.ThreefoldFan)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.ThreefoldFan, true, "Proc consumed");

            return true;
        }

        return false;
    }

    private bool TryFanDance(ITerpsichoreContext context, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableFanDance) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDance.MinLevel)
            return false;

        if (context.Feathers == 0)
            return false;

        // Dump at 4 feathers to prevent overcap, or during burst
        bool shouldUse = context.Feathers >= 4 ||
                         context.HasDevilment ||
                         context.HasTechnicalFinish;

        // Hold 3 for burst if not in burst window
        if (!shouldUse && context.Feathers <= 3)
        {
            context.Debug.BuffState = $"Holding feathers ({context.Feathers}/4)";
            return false;
        }

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // Use Fan Dance II for AoE (3+ targets)
        if (enemyCount >= 3 && level >= DNCActions.FanDanceII.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.FanDanceII.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceII, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.FanDanceII.Name;
                    context.Debug.BuffState = $"Fan Dance II ({context.Feathers} feathers)";

                    // Training: Record Fan Dance II decision
                    var fanDanceIIReason = context.Feathers >= 4 ? "Preventing feather overcap" :
                        context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "AoE damage";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.FanDanceII.ActionId, DNCActions.FanDanceII.Name)
                        .AsRangedResource("Feathers", context.Feathers)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason(
                            $"Fan Dance II ({fanDanceIIReason})",
                            "Fan Dance II is the AoE feather spender for 3+ targets. Each use consumes 1 feather " +
                            "and can proc Threefold Fan Dance. Dump feathers at 4 to prevent overcap, or during burst.")
                        .Factors($"Feathers: {context.Feathers}/4", $"{enemyCount} enemies", "AoE feather spender")
                        .Alternatives("No feathers", "Single target (use Fan Dance I)")
                        .Tip("Use Fan Dance II at 3+ targets to spend feathers efficiently.")
                        .Concept(DncConcepts.FanDanceUsage)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.FeatherGauge, true, "Feather spent");

                    return true;
                }
            }
        }

        // Single target Fan Dance I
        if (!context.ActionService.IsActionReady(DNCActions.FanDance.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDance.Name;
            context.Debug.BuffState = $"Fan Dance ({context.Feathers} feathers)";

            // Training: Record Fan Dance I decision
            var fanDanceIReason = context.Feathers >= 4 ? "Preventing feather overcap" :
                context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "Feather dump";
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.FanDance.ActionId, DNCActions.FanDance.Name)
                .AsRangedResource("Feathers", context.Feathers)
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"Fan Dance I ({fanDanceIReason})",
                    "Fan Dance I is the single-target feather spender. Each use consumes 1 feather and can proc " +
                    "Threefold Fan Dance. Dump feathers at 4 to prevent overcap, or spend freely during burst windows.")
                .Factors($"Feathers: {context.Feathers}/4", "Single target", "Can proc Threefold Fan")
                .Alternatives("No feathers", "3+ enemies (use Fan Dance II)")
                .Tip("Dump feathers at 4 to prevent overcap, or spend during burst windows.")
                .Concept(DncConcepts.FanDanceUsage)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.FeatherGauge, true, "Feather spent");
            if (context.Feathers >= 4)
                context.TrainingService?.RecordConceptApplication(DncConcepts.FeatherOvercapping, true, "Prevented overcap");

            return true;
        }

        return false;
    }

    #endregion
}
