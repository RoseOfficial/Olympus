using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Scholar;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles fairy management for Scholar.
/// Responsible for summoning, fairy abilities, and Seraph transformations.
/// </summary>
public sealed class FairyModule : IAthenaModule
{
    public int Priority => 3; // Very high priority - fairy is essential
    public string Name => "Fairy";

    // Training explanation arrays
    private static readonly string[] _summonSeraphAlternatives =
    {
        "Save for heavy damage phase",
        "Use Eos abilities instead",
        "Hold for emergency healing",
    };

    private static readonly string[] _feyUnionAlternatives =
    {
        "Excogitation (proactive heal)",
        "Lustrate (instant heal)",
        "Let Embrace handle it",
    };

    private static readonly string[] _feyIlluminationAlternatives =
    {
        "Direct heals (Indom, Lustrate)",
        "Whispering Dawn (HoT)",
        "Save for heavy healing phase",
    };

    private static readonly string[] _feyBlessingAlternatives =
    {
        "Whispering Dawn (HoT instead)",
        "Indomitability (Aetherflow cost)",
        "Save for emergency burst heal",
    };

    private static readonly string[] _whisperingDawnAlternatives =
    {
        "Fey Blessing (instant AoE heal)",
        "Indomitability (Aetherflow cost)",
        "Save for after next raidwide",
    };

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        // Priority 1: Summon fairy if not present
        if (TrySummonFairy(context, isMoving))
            return true;

        // Priority 2: Seraphism (level 100 transformation)
        if (context.CanExecuteOgcd && TrySeraphism(context))
            return true;

        // Priority 3: Summon Seraph
        if (context.CanExecuteOgcd && TrySummonSeraph(context))
            return true;

        // Priority 4: Consolation (Seraph ability)
        if (context.CanExecuteOgcd && TryConsolation(context))
            return true;

        // Priority 5: Fey Union (sustained single-target healing)
        if (context.CanExecuteOgcd && TryFeyUnion(context))
            return true;

        // Priority 6: Fey Blessing (AoE heal)
        if (context.CanExecuteOgcd && TryFeyBlessing(context))
            return true;

        // Priority 7: Whispering Dawn (AoE HoT)
        if (context.CanExecuteOgcd && TryWhisperingDawn(context))
            return true;

        // Priority 8: Fey Illumination (heal buff)
        if (context.CanExecuteOgcd && TryFeyIllumination(context))
            return true;

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        context.Debug.FairyState = context.FairyStateManager.CurrentState.ToString();
        context.Debug.FairyGauge = context.FairyGaugeService.CurrentGauge;
    }

    private bool TrySummonFairy(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.AutoSummonFairy)
            return false;

        if (!context.FairyStateManager.NeedsSummon)
            return false;

        // Don't summon during Dissipation
        if (context.FairyStateManager.IsDissipationActive)
            return false;

        if (player.Level < SCHActions.SummonEos.MinLevel)
            return false;

        // Can't summon while moving (has cast time)
        if (isMoving)
            return false;

        if (!context.ActionService.CanExecuteGcd)
            return false;

        var action = SCHActions.SummonEos;
        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Summoning Fairy";
            return true;
        }

        return false;
    }

    private bool TrySummonSeraph(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphStrategy == SeraphUsageStrategy.Manual)
            return false;

        if (player.Level < SCHActions.SummonSeraph.MinLevel)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SCHActions.SummonSeraph.ActionId))
            return false;

        // SaveForDamage: Check if party HP is low enough
        if (config.SeraphStrategy == SeraphUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold)
                return false;
        }

        var action = SCHActions.SummonSeraph;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Seraph";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
                var usedForDamage = config.SeraphStrategy == SeraphUsageStrategy.SaveForDamage;

                var shortReason = usedForDamage
                    ? $"Summon Seraph - party HP {avgHp:P0}"
                    : "Summon Seraph - on cooldown";

                var factors = new[]
                {
                    $"Strategy: {config.SeraphStrategy}",
                    usedForDamage ? $"Party HP: {avgHp:P0} (below threshold)" : "Using on cooldown",
                    "Transforms Eos into Seraph (22s)",
                    "Grants 2 charges of Consolation",
                    "Seraph provides stronger healing",
                };

                var alternatives = _summonSeraphAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Summon Seraph",
                    Category = "Fairy",
                    TargetName = null,
                    ShortReason = shortReason,
                    DetailedReason = $"Summoned Seraph to replace Eos for 22 seconds. {(usedForDamage ? $"Party HP at {avgHp:P0}, below the {config.SeraphPartyHpThreshold:P0} threshold. " : "Using on cooldown for maximum value. ")}Seraph provides 2 Consolation charges (AoE heal + shield) and upgraded Embrace healing. Use both Consolation charges before Seraph expires!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Seraph is powerful but temporary. Always use both Consolation charges! Plan Seraph for heavy healing phases.",
                    ConceptId = SchConcepts.SeraphUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySeraphism(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphismStrategy == SeraphismUsageStrategy.Manual)
            return false;

        if (player.Level < SCHActions.Seraphism.MinLevel)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SCHActions.Seraphism.ActionId))
            return false;

        // SaveForDamage: Check if party HP is low enough
        if (config.SeraphismStrategy == SeraphismUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold)
                return false;
        }

        var action = SCHActions.Seraphism;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Seraphism";
            return true;
        }

        return false;
    }

    private bool TryConsolation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableConsolation)
            return false;

        if (!context.FairyStateManager.CanUseSeraphAbilities)
            return false;

        if (player.Level < SCHActions.Consolation.MinLevel)
            return false;

        // Check cooldown and charges
        if (!context.ActionService.IsActionReady(SCHActions.Consolation.ActionId))
            return false;

        // Use when party needs healing
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.AoEHealThreshold && injuredCount < config.AoEHealMinTargets)
            return false;

        var action = SCHActions.Consolation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Consolation";
            return true;
        }

        return false;
    }

    private bool TryFeyUnion(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        if (player.Level < SCHActions.FeyUnion.MinLevel)
            return false;

        // Check gauge requirement
        if (context.FairyGaugeService.CurrentGauge < config.FeyUnionMinGauge)
            return false;

        // Don't start if already active
        if (context.StatusHelper.HasFeyUnionActive(player))
            return false;

        // Find target needing sustained healing
        var target = context.PartyHelper.FindFeyUnionTarget(player, config.FeyUnionThreshold);
        if (target == null)
            return false;

        var action = SCHActions.FeyUnion;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Union";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                var currentGauge = context.FairyGaugeService.CurrentGauge;

                var shortReason = $"Fey Union on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.FeyUnionThreshold:P0}",
                    $"Fairy Gauge: {currentGauge}/100",
                    "400 potency per tick (sustained)",
                    "Consumes 10 gauge per tick",
                };

                var alternatives = _feyUnionAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Fey Union",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Fey Union tether on {targetName} at {hpPercent:P0} HP. Fairy gauge at {currentGauge}/100. Fey Union provides powerful sustained healing (400 potency per tick) while consuming 10 gauge per tick. Best for sustained single-target healing like tank maintenance.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Fey Union is great for tank healing during sustained damage. Cancel it early if you need the gauge for Aetherpact or if the target is full HP.",
                    ConceptId = SchConcepts.FeyUnionUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryFeyBlessing(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        if (player.Level < SCHActions.FeyBlessing.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.FeyBlessing.ActionId))
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: deploy proactively before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Burst awareness: Deploy Fey Blessing proactively before burst windows
        // AoE instant heal provides burst healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Fey Blessing 3-8 seconds before burst
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Skip HP check if deploying proactively for raidwide/burst
        if (!raidwideImminent && !burstImminent)
        {
            if (avgHp > config.FeyBlessingThreshold)
                return false;
        }

        var action = SCHActions.FeyBlessing;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Blessing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (burstImminent) trigger = "DPS burst window imminent";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = $"Fey Blessing - {trigger}";

                var factors = new[]
                {
                    trigger,
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "350 potency instant AoE heal",
                    "Free fairy ability, no resource cost",
                };

                var alternatives = _feyBlessingAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Fey Blessing",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Fey Blessing for {injuredCount} injured party members. {trigger}. Fairy casts 350 potency instant AoE heal. Free burst healing with no resource cost! Great for topping off the party after damage.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Fey Blessing is free burst AoE healing. Use it for immediate party HP recovery. Pairs well with Whispering Dawn (HoT + burst).",
                    ConceptId = SchConcepts.FeyBlessingUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryWhisperingDawn(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        if (player.Level < SCHActions.WhisperingDawn.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.WhisperingDawn.ActionId))
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: deploy proactively before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Burst awareness: Deploy Whispering Dawn proactively before burst windows
        // AoE HoT provides sustained healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Whispering Dawn 3-8 seconds before burst
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Skip HP check if deploying proactively for raidwide/burst
        if (!raidwideImminent && !burstImminent)
        {
            if (avgHp > config.WhisperingDawnThreshold)
                return false;
            if (injuredCount < config.WhisperingDawnMinTargets)
                return false;
        }

        var action = SCHActions.WhisperingDawn;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Whispering Dawn";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (burstImminent) trigger = "DPS burst window imminent";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = $"Whispering Dawn - {trigger}";

                var factors = new[]
                {
                    trigger,
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "120 potency HoT (21s duration)",
                    "Free fairy ability, no resource cost",
                };

                var alternatives = _whisperingDawnAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Whispering Dawn",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Whispering Dawn for {injuredCount} injured party members. {trigger}. Fairy casts a 21s AoE HoT (120 potency per tick). Free healing with no resource cost! Best used after damage or proactively to top party off.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Whispering Dawn is free sustained healing. Use it often! Great after raidwides or when moving. The fairy has a separate action delay, so don't expect instant casts.",
                    ConceptId = SchConcepts.WhisperingDawnUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryFeyIllumination(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        if (player.Level < SCHActions.FeyIllumination.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.FeyIllumination.ActionId))
            return false;

        // Use proactively when party needs healing boost
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (lowestHp > 0.5f && avgHp > 0.8f)
            return false;

        var action = SCHActions.FeyIllumination;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Illumination";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Fey Illumination - party needs healing boost";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Lowest HP: {lowestHp:P0}",
                    "Increases healing magic potency by 10%",
                    "5% magic damage reduction",
                    "20s duration",
                };

                var alternatives = _feyIlluminationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Fey Illumination",
                    Category = "Defensive",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Fey Illumination to boost party healing. Party avg HP at {avgHp:P0}, lowest at {lowestHp:P0}. Fey Illumination increases all healing magic potency by 10% and provides 5% magic damage mitigation for 20 seconds. Free fairy ability!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Fey Illumination is great before heavy healing phases. The 10% healing boost affects all healers, making it excellent for prog or recovery situations.",
                    ConceptId = SchConcepts.FeyIlluminationUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
