using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles Dancer dance execution, buff management, and oGCD optimization (scheduler-driven).
/// Manages dances (Standard/Technical Step), Devilment, Flourish, Fan Dance I/II/III/IV,
/// dance partner selection. Pre-combat dances + Closed Position fire before InCombat gate.
/// </summary>
public sealed class BuffModule : ITerpsichoreModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool IsInBurst => BurstHoldHelper.IsInBurst(_burstWindowService);

    public int Priority => 20;
    public string Name => "Buff";

    public bool TryExecute(ITerpsichoreContext context, bool isMoving) => false;

    public void UpdateDebugState(ITerpsichoreContext context) { }

    public void CollectCandidates(ITerpsichoreContext context, RotationScheduler scheduler, bool isMoving)
    {
        var player = context.Player;

        // Always: dance step execution (highest GCD priority, including pre-pull)
        if (context.IsDancing)
        {
            TryPushDanceStep(context, scheduler);
            TryPushDanceFinish(context, scheduler);
        }

        // Pre-combat: partner + Standard Step
        if (!context.InCombat)
        {
            TryPushClosedPosition(context, scheduler);
            if (!context.IsDancing && !context.HasStandardFinish)
                TryPushStandardStep(context, scheduler);
            context.Debug.BuffState = context.IsDancing ? "Dancing (pre-pull)" : "Not in combat";
            return;
        }

        // In-combat oGCDs
        TryPushClosedPosition(context, scheduler);

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return;
        }

        if (!context.HasStandardFinish)
            TryPushStandardStep(context, scheduler);

        TryPushTechnicalStep(context, scheduler);
        TryPushDevilment(context, scheduler);
        if (context.Configuration.Dancer.UseStandardStepOnCooldown)
            TryPushStandardStep(context, scheduler);
        TryPushFlourish(context, scheduler);
        TryPushFanDanceIV(context, scheduler, target);
        TryPushFanDanceIII(context, scheduler, target);
        TryPushFanDance(context, scheduler, target);
    }

    private void TryPushDanceStep(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        var stepAction = DNCActions.GetStepAction(context.CurrentStep);
        if (stepAction == null) return;

        var ability = stepAction.ActionId switch
        {
            var id when id == DNCActions.Emboite.ActionId => TerpsichoreAbilities.Emboite,
            var id when id == DNCActions.Entrechat.ActionId => TerpsichoreAbilities.Entrechat,
            var id when id == DNCActions.Jete.ActionId => TerpsichoreAbilities.Jete,
            var id when id == DNCActions.Pirouette.ActionId => TerpsichoreAbilities.Pirouette,
            _ => TerpsichoreAbilities.Emboite,
        };

        scheduler.PushGcd(ability, context.Player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = stepAction.Name;
                context.Debug.BuffState = $"Dance step: {stepAction.Name}";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(stepAction.ActionId, stepAction.Name)
                    .AsRangedDamage()
                    .Reason($"Dance step {context.StepIndex + 1}",
                        "Dance steps must be executed in the correct order shown on the Step Gauge. Each step " +
                        "corresponds to a specific button (Emboite=Red, Entrechat=Blue, Jete=Green, Pirouette=Yellow). " +
                        "Complete all steps quickly to finish the dance.")
                    .Factors($"Step {context.StepIndex + 1}/{(context.StepIndex >= 2 ? 4 : 2)}", "Dance in progress")
                    .Alternatives("Wait for dance to end")
                    .Tip("Execute dance steps as quickly as possible - each step is a 1s GCD.")
                    .Concept(DncConcepts.DanceExecution)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.DanceExecution, true, "Step execution");
            });
    }

    private void TryPushDanceFinish(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;
        var completedSteps = context.StepIndex;

        if (completedSteps >= 4 && level >= DNCActions.TechnicalFinish.MinLevel
            && context.ActionService.IsActionReady(DNCActions.TechnicalFinish.ActionId))
        {
            scheduler.PushGcd(TerpsichoreAbilities.TechnicalFinish, player.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.TechnicalFinish.Name;
                    context.Debug.BuffState = "Technical Finish";
                    context.PartyCoordinationService?.OnRaidBuffUsed(DNCActions.TechnicalFinish.ActionId, 120_000);

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.TechnicalFinish.ActionId, DNCActions.TechnicalFinish.Name)
                        .AsRaidBuff()
                        .Reason("4-step dance complete - Technical Finish!",
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
                });
            return;
        }

        if (completedSteps >= 2
            && context.ActionService.IsActionReady(DNCActions.StandardFinish.ActionId))
        {
            scheduler.PushGcd(TerpsichoreAbilities.StandardFinish, player.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.StandardFinish.Name;
                    context.Debug.BuffState = "Standard Finish";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.StandardFinish.ActionId, DNCActions.StandardFinish.Name)
                        .AsSong("Standard Step", 30f)
                        .Reason("2-step dance complete - Standard Finish!",
                            "Standard Finish provides a personal 5% damage buff for 60s and deals high damage. " +
                            "It's on a 30s cooldown and should be used on cooldown outside of Technical Step windows. " +
                            "The buff also applies to your dance partner.")
                        .Factors("2 dance steps completed", "30s cooldown", "Personal +5% damage")
                        .Alternatives("Dance not complete")
                        .Tip("Keep Standard Finish buff active at all times - use on cooldown.")
                        .Concept(DncConcepts.StandardStep)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.StandardStep, true, "Personal buff applied");
                });
        }
    }

    private void TryPushTechnicalStep(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableTechnicalStep) return;
        var player = context.Player;
        if (player.Level < DNCActions.TechnicalStep.MinLevel) return;
        if (context.IsDancing) return;
        if (!context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService, context.Configuration.Dancer.TechnicalHoldTime))
        {
            context.Debug.BuffState = "Holding Technical Step (phase soon)";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(DNCActions.TechnicalFinish.ActionId))
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning with party burst";
            partyCoord.AnnounceRaidBuffIntent(DNCActions.TechnicalFinish.ActionId);
        }

        scheduler.PushOgcd(TerpsichoreAbilities.TechnicalStep, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.TechnicalStep.Name;
                context.Debug.BuffState = "Technical Step";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.TechnicalStep.ActionId, DNCActions.TechnicalStep.Name)
                    .AsRaidBuff()
                    .Reason("Starting 4-step Technical dance",
                        "Technical Step begins a 4-step dance sequence that ends with Technical Finish, DNC's 2-minute " +
                        "raid buff. Time this to align with other party raid buffs. After finishing, immediately use " +
                        "Devilment and Flourish for maximum burst damage.")
                    .Factors("Off cooldown", "Not already dancing", "2-minute burst window")
                    .Alternatives("Already dancing", "Phase transition soon", "Raid buffs not aligned")
                    .Tip("Plan your Technical Step to align with party buffs every 2 minutes.")
                    .Concept(DncConcepts.TechnicalStep)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.BurstAlignment, true, "Burst preparation");
            });
    }

    private void TryPushStandardStep(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableStandardStep) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DNCActions.StandardStep.MinLevel) return;
        if (context.IsDancing) return;
        if (!context.ActionService.IsActionReady(DNCActions.StandardStep.ActionId)) return;

        if (context.Configuration.Dancer.DelayStandardForTechnical
            && level >= DNCActions.TechnicalStep.MinLevel)
        {
            var techCd = context.ActionService.GetCooldownRemaining(DNCActions.TechnicalStep.ActionId);
            var holdWindow = context.Configuration.Dancer.StandardHoldForTechnical;
            if (techCd > 0 && techCd < holdWindow)
            {
                context.Debug.BuffState = "Holding Standard for Technical";
                return;
            }
        }

        scheduler.PushOgcd(TerpsichoreAbilities.StandardStep, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.StandardStep.Name;
                context.Debug.BuffState = "Standard Step";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.StandardStep.ActionId, DNCActions.StandardStep.Name)
                    .AsSong("None", 0f)
                    .Reason("Starting 2-step Standard dance",
                        "Standard Step begins a 2-step dance sequence that ends with Standard Finish. Use on cooldown " +
                        "to maintain the 5% damage buff. Hold if Technical Step will be ready within 5 seconds to " +
                        "avoid delaying your burst window.")
                    .Factors("Off cooldown", "Not already dancing", "Technical Step not imminent")
                    .Alternatives("Technical Step coming soon", "Already dancing")
                    .Tip("Keep Standard Finish buff active - it's your most important personal buff.")
                    .Concept(DncConcepts.StandardStep)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.DanceTimers, true, "Dance initiated");
            });
    }

    private void TryPushClosedPosition(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < DNCActions.ClosedPosition.MinLevel) return;
        if (context.Configuration.Dancer.PartnerSelectionMode == PartnerSelection.Manual) return;
        if (context.IsDancing) return;

        var needsPartner = !context.HasDancePartner;
        if (!needsPartner && context.Configuration.Dancer.AutoRepartner)
            needsPartner = context.PartyHelper.ShouldUpdatePartner(player, context.StatusHelper);
        if (!needsPartner) return;

        var partner = context.PartyHelper.SelectDancePartner(player, context.Configuration.Dancer.PartnerSelectionMode);
        if (partner == null) return;
        if (!context.ActionService.IsActionReady(DNCActions.ClosedPosition.ActionId)) return;

        scheduler.PushOgcd(TerpsichoreAbilities.ClosedPosition, partner.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.ClosedPosition.Name;
                context.Debug.BuffState = $"Closed Position → {partner.Name?.TextValue ?? "Partner"}";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.ClosedPosition.ActionId, DNCActions.ClosedPosition.Name)
                    .AsSong("Dance Partner", float.MaxValue)
                    .Target(partner.Name?.TextValue ?? "Partner")
                    .Reason("Applied dance partner",
                        "Closed Position designates a dance partner who shares your Standard Finish buff and generates " +
                        "Esprit when dealing damage. Always keep a partner selected for maximum Esprit generation.")
                    .Factors(context.HasDancePartner ? "Partner update needed" : "No partner set",
                             $"Selected: {partner.Name?.TextValue ?? "Partner"}")
                    .Alternatives("Manual partner selection", "Solo (no party)")
                    .Tip("Keep Closed Position active at all times — your partner generates Esprit and gets your Standard Finish buff.")
                    .Concept(DncConcepts.ClosedPosition)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.ClosedPosition, true, "Partner set");
            });
    }

    private void TryPushDevilment(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableDevilment) return;
        if (!context.Configuration.Dancer.UseDevilmentAfterTechnical) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DNCActions.Devilment.MinLevel) return;
        if (context.HasDevilment) return;

        bool shouldUse = context.HasTechnicalFinish
                         || level < DNCActions.TechnicalStep.MinLevel
                         || !context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId);
        if (!shouldUse) return;
        if (!context.ActionService.IsActionReady(DNCActions.Devilment.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Devilment (phase soon)";
            return;
        }

        scheduler.PushOgcd(TerpsichoreAbilities.Devilment, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Devilment.Name;
                context.Debug.BuffState = "Devilment";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Devilment.ActionId, DNCActions.Devilment.Name)
                    .AsRangedBurst()
                    .Reason("Devilment for burst window",
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
            });
    }

    private void TryPushFlourish(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableFlourish) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DNCActions.Flourish.MinLevel) return;
        if (context.IsDancing) return;

        bool shouldUse;
        if (level < DNCActions.Devilment.MinLevel) shouldUse = true;
        else
        {
            var devilmentCd = context.ActionService.GetCooldownRemaining(DNCActions.Devilment.ActionId);
            shouldUse = devilmentCd > 55f;
        }
        if (!shouldUse)
        {
            context.Debug.BuffState = "Holding Flourish (burst minute)";
            return;
        }
        if (!context.ActionService.IsActionReady(DNCActions.Flourish.ActionId)) return;

        scheduler.PushOgcd(TerpsichoreAbilities.Flourish, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Flourish.Name;
                context.Debug.BuffState = "Flourish";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Flourish.ActionId, DNCActions.Flourish.Name)
                    .AsRangedBurst()
                    .Reason("Flourish on the off-minute (Devilment on long CD)",
                        "Flourish grants all four procs (Silken Symmetry, Silken Flow, Threefold Fan, Fourfold Fan). " +
                        "Use it on the off 2-minute when Devilment is on long cooldown — the 4 procs fill GCD uptime " +
                        "between bursts. Inside Devilment the burst window is already full of Tech procs and weaves, " +
                        "so Flourish there just overwrites and drops procs.")
                    .Factors("Devilment on long cooldown (>55s)", "Fills off-minute GCDs", "Grants all 4 procs")
                    .Alternatives("Devilment imminent or active — save Flourish for the off-minute")
                    .Tip("Flourish on the 1:00 and 3:00 beats, not during Technical/Devilment.")
                    .Concept(DncConcepts.Flourish)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.Flourish, true, "All procs granted");
            });
    }

    private void TryPushFanDanceIV(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableFanDanceIV) return;
        var player = context.Player;
        if (player.Level < DNCActions.FanDanceIV.MinLevel) return;
        if (!context.HasFourfoldFanDance) return;
        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIV.ActionId)) return;

        scheduler.PushOgcd(TerpsichoreAbilities.FanDanceIV, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.FanDanceIV.Name;
                context.Debug.BuffState = "Fan Dance IV";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.FanDanceIV.ActionId, DNCActions.FanDanceIV.Name)
                    .AsProc("Fourfold Fan Dance")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Fourfold Fan proc - highest priority oGCD",
                        "Fan Dance IV is granted by Flourish (Fourfold Fan Dance buff). It's a high-potency cone AoE " +
                        "that should be used before other Fan Dances. Use during burst windows for maximum damage.")
                    .Factors("Fourfold Fan Dance proc active", "High potency oGCD", "Cone AoE")
                    .Alternatives("No Fourfold proc")
                    .Tip("Fan Dance IV has the highest priority among Fan Dances - use it first.")
                    .Concept(DncConcepts.FourfoldFan)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.FourfoldFan, true, "Proc consumed");
            });
    }

    private void TryPushFanDanceIII(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableFanDance) return;
        var player = context.Player;
        if (player.Level < DNCActions.FanDanceIII.MinLevel) return;
        if (!context.HasThreefoldFanDance) return;
        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIII.ActionId)) return;

        scheduler.PushOgcd(TerpsichoreAbilities.FanDanceIII, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.FanDanceIII.Name;
                context.Debug.BuffState = "Fan Dance III";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.FanDanceIII.ActionId, DNCActions.FanDanceIII.Name)
                    .AsProc("Threefold Fan Dance")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Threefold Fan proc - use before Fan Dance I/II",
                        "Fan Dance III is granted by Fan Dance I/II (Threefold Fan Dance buff). It's a high-potency " +
                        "cone AoE oGCD. Use it before spending more feathers to avoid losing the proc.")
                    .Factors("Threefold Fan Dance proc active", "Cone AoE oGCD", "Triggers from Fan Dance I/II")
                    .Alternatives("No Threefold proc")
                    .Tip("Fan Dance III has higher priority than Fan Dance I/II - consume it first.")
                    .Concept(DncConcepts.ThreefoldFan)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.ThreefoldFan, true, "Proc consumed");
            });
    }

    private void TryPushFanDance(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableFanDance) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DNCActions.FanDance.MinLevel) return;

        var featherConfig = context.Configuration.Dancer;
        if (context.Feathers < featherConfig.FanDanceMinFeathers) return;

        bool inBurst = context.HasDevilment || context.HasTechnicalFinish;
        bool shouldUse = context.Feathers >= featherConfig.FeatherOvercapThreshold || inBurst;
        if (!shouldUse && featherConfig.SaveFeathersForBurst) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (enemyCount >= context.Configuration.Dancer.AoEMinTargets
            && level >= DNCActions.FanDanceII.MinLevel
            && context.ActionService.IsActionReady(DNCActions.FanDanceII.ActionId))
        {
            scheduler.PushOgcd(TerpsichoreAbilities.FanDanceII, player.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.FanDanceII.Name;
                    context.Debug.BuffState = $"Fan Dance II ({context.Feathers} feathers)";
                    var fanDanceIIReason = context.Feathers >= 4 ? "Preventing feather overcap"
                                         : context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "AoE damage";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.FanDanceII.ActionId, DNCActions.FanDanceII.Name)
                        .AsRangedResource("Feathers", context.Feathers)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Fan Dance II ({fanDanceIIReason})",
                            "Fan Dance II is the AoE feather spender for 3+ targets. Each use consumes 1 feather " +
                            "and can proc Threefold Fan Dance. Dump feathers at 4 to prevent overcap, or during burst.")
                        .Factors($"Feathers: {context.Feathers}/4", $"{enemyCount} enemies", "AoE feather spender")
                        .Alternatives("No feathers", "Single target (use Fan Dance I)")
                        .Tip("Use Fan Dance II at 3+ targets to spend feathers efficiently.")
                        .Concept(DncConcepts.FanDanceUsage)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.FeatherGauge, true, "Feather spent");
                });
            return;
        }

        if (!context.ActionService.IsActionReady(DNCActions.FanDance.ActionId)) return;
        scheduler.PushOgcd(TerpsichoreAbilities.FanDance, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.FanDance.Name;
                context.Debug.BuffState = $"Fan Dance ({context.Feathers} feathers)";
                var fanDanceIReason = context.Feathers >= 4 ? "Preventing feather overcap"
                                    : context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "Feather dump";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.FanDance.ActionId, DNCActions.FanDance.Name)
                    .AsRangedResource("Feathers", context.Feathers)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Fan Dance I ({fanDanceIReason})",
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
            });
    }
}
