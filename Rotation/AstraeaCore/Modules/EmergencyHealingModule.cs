using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles emergency and setup healing for Astrologian: Horoscope (detonation + preparation),
/// Microcosmos, Earthly Star (detonation + placement), Synastry, Lady of Crowns, and Macrocosmos preparation.
/// </summary>
public sealed class EmergencyHealingModule
{
    private static readonly string[] _horoscopeDetonationAlternatives =
    {
        "Let it expire naturally (wastes it)",
        "Celestial Opposition (if available)",
        "Wait for more injured targets",
    };

    private static readonly string[] _microcosmosDetonationAlternatives =
    {
        "Wait for timer to expire (auto-detonates)",
        "Let more damage accumulate",
        "Use other heals first",
    };

    private static readonly string[] _synastryAlternatives =
    {
        "Direct heal the target instead",
        "Save for tankbuster sequences",
        "Use on different target",
    };

    private static readonly string[] _earthlyStarPlacementAlternatives =
    {
        "Wait for better timing",
        "Place on self instead of tank",
        "Save for emergency healing",
    };

    private static readonly string[] _ladyOfCrownsAlternatives =
    {
        "Lord of Crowns (400 potency damage)",
        "Celestial Opposition (if available)",
        "Save Lady for bigger emergency",
    };

    private static readonly string[] _horoscopePreparationAlternatives =
    {
        "Wait for damage before preparing",
        "Save for known raidwide timing",
        "Use other heals directly",
    };

    private static readonly string[] _macrocosmosAlternatives =
    {
        "Save for predictable big raidwide",
        "Use other healing first",
        "Wait for party to stack",
    };

    /// <summary>
    /// Tries oGCDs in order: HoroscopeDetonation, Microcosmos, EarthlyStarDetonation,
    /// Synastry, EarthlyStarPlacement, LadyOfCrowns. Does not check CanExecuteOgcd.
    /// </summary>
    public bool TryOgcd(AstraeaContext context)
    {
        if (TryHoroscopeDetonation(context))
            return true;
        if (TryMicrocosmos(context))
            return true;
        if (TryEarthlyStarDetonation(context))
            return true;
        if (TrySynastry(context))
            return true;
        if (TryEarthlyStarPlacement(context))
            return true;
        if (TryLadyOfCrowns(context))
            return true;
        return false;
    }

    /// <summary>
    /// Tries HoroscopePreparation (no isMoving guard) then MacrocosmosPreparation (!isMoving guard).
    /// Does not check CanExecuteGcd.
    /// </summary>
    public bool TryGcd(AstraeaContext context, bool isMoving)
    {
        if (TryHoroscopePreparation(context))
            return true;
        if (!isMoving && TryMacrocosmosPreparation(context))
            return true;
        return false;
    }

    private bool TryHoroscopeDetonation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope)
            return false;

        // Need Horoscope or Horoscope Helios buff active to detonate
        if (!context.HasHoroscope && !context.HasHoroscopeHelios)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.HoroscopeEnd.ActionId))
            return false;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.HoroscopeThreshold)
            return false;

        if (injured < config.HoroscopeMinTargets)
            return false;

        var action = ASTActions.HoroscopeEnd;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.HoroscopeState = "Detonated";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var isEnhanced = context.HasHoroscopeHelios;

                var shortReason = isEnhanced
                    ? $"Horoscope Helios detonated - {injured} at {avgHp:P0}"
                    : $"Horoscope detonated - {injured} at {avgHp:P0}";

                var factors = new[]
                {
                    isEnhanced ? "Enhanced with Helios (400 potency)" : "Basic Horoscope (200 potency)",
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    $"Min targets: {config.HoroscopeMinTargets}",
                    "oGCD - free AoE heal",
                };

                var alternatives = _horoscopeDetonationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Horoscope",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Horoscope detonated on {injured} injured party members at {avgHp:P0} average HP. {(isEnhanced ? "Enhanced with Helios for 400 potency - double the value!" : "Basic 200 potency heal. Consider using Helios after Horoscope to enhance it next time!")} Free oGCD heal that expires after 30s.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = isEnhanced
                        ? "Great! You enhanced Horoscope with Helios for double potency. This is the optimal way to use Horoscope!"
                        : "Horoscope can be enhanced to 400 potency by casting Helios/Aspected Helios while it's active. Try to enhance it when possible!",
                    ConceptId = AstConcepts.HoroscopeUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryMicrocosmos(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos)
            return false;

        // Need Macrocosmos buff active to use Microcosmos
        if (!context.HasMacrocosmos)
            return false;

        if (player.Level < ASTActions.Microcosmos.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Microcosmos.ActionId))
            return false;

        // Detonate when party needs healing
        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > 0.70f && injured < 3)
            return false;

        var action = ASTActions.Microcosmos;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.MacrocosmosState = "Detonated";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Microcosmos detonated - {injured} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    "Heals 50% of damage taken during Macrocosmos",
                    "Minimum 200 potency heal",
                    "oGCD detonation",
                };

                var alternatives = _microcosmosDetonationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Microcosmos",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Microcosmos (Macrocosmos detonation) used on {injured} injured party members at {avgHp:P0} average HP. Heals for 50% of all damage taken during the Macrocosmos buff (minimum 200 potency). The more damage absorbed, the bigger the heal!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Microcosmos heals based on damage taken during Macrocosmos. Use Macrocosmos BEFORE big raidwides to capture the damage, then detonate for massive healing. Time it with predictable damage!",
                    ConceptId = AstConcepts.MacrocosmosUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryEarthlyStarDetonation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (!context.IsStarPlaced)
            return false;

        if (player.Level < ASTActions.StellarDetonation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.StellarDetonation.ActionId))
            return false;

        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;

        // Check if star is mature (Giant Dominance)
        bool isMature = context.IsStarMature;

        // Timeline-aware: check for imminent raidwide
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Determine if we should detonate
        bool shouldDetonate = false;

        if (isMature)
        {
            // Mature star: detonate when party needs healing OR raidwide is imminent
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets || raidwideImminent)
                shouldDetonate = true;
        }
        else if (!config.WaitForGiantDominance)
        {
            // Immature star allowed: detonate if party needs healing or raidwide imminent
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets || raidwideImminent)
                shouldDetonate = true;
        }
        else
        {
            // Emergency detonation even if immature
            if (avgHp <= config.EarthlyStarEmergencyThreshold)
                shouldDetonate = true;
        }

        if (!shouldDetonate)
            return false;

        var action = ASTActions.StellarDetonation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            // Notify service that star was detonated
            context.EarthlyStarService.OnStarDetonated();

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = isMature ? "Detonated (Mature)" : "Detonated (Immature)";
            context.LogEarthlyStarDecision("Detonated", isMature ? "Mature star, party needs healing" : "Emergency detonate");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (avgHp <= config.EarthlyStarEmergencyThreshold) trigger = "Emergency HP";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = isMature
                    ? $"Giant Dominance detonated - {trigger}"
                    : $"Immature Star detonated - {trigger}";

                var factors = new[]
                {
                    isMature ? "Star MATURE (Giant Dominance)" : "Star immature",
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    trigger,
                    isMature ? "720 potency heal + 720 damage" : "360 potency heal + 360 damage",
                };

                var alternatives = new[]
                {
                    isMature ? "Detonation is optimal when mature" : "Wait for maturation (if safe)",
                    "Let star expire naturally",
                    "Use other oGCDs first",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Stellar Detonation",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Detonated Earthly Star ({(isMature ? "Giant Dominance, 720 potency" : "immature, 360 potency")}). Party avg HP at {avgHp:P0} with {injured} injured. {trigger}. {(isMature ? "Mature star provides maximum healing value!" : "Detonated early due to urgent healing need.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = isMature
                        ? "Perfect timing! Mature Earthly Star is AST's biggest AoE heal. Always aim for Giant Dominance when possible."
                        : "Sometimes you have to detonate early. An immature heal is better than letting the party die!",
                    ConceptId = AstConcepts.EarthlyStarMaturation,
                    Priority = isMature ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySynastry(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableSynastry)
            return false;

        if (player.Level < ASTActions.Synastry.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Synastry.ActionId))
            return false;

        // Already have Synastry active
        if (context.HasSynastry)
            return false;

        var target = context.PartyHelper.FindSynastryTarget(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.SynastryThreshold)
            return false;

        var action = ASTActions.Synastry;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target - Synastry mirrors heals to this target
            var healAmount = 1000; // Synastry itself doesn't heal, but reserves the target for coordination
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.SynastryState = "Active";
            context.Debug.SynastryTarget = target.Name.TextValue;

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);

                var shortReason = $"Synastry on {targetName} - sustained healing phase";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.SynastryThreshold:P0}",
                    isTank ? "Tank target - great for sustained tankbuster recovery" : "Non-tank target",
                    "40% of single-target heals mirrored",
                    "20s duration, 120s cooldown",
                };

                var alternatives = _synastryAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Synastry",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Synastry linked to {targetName} at {hpPercent:P0} HP. For the next 20 seconds, 40% of all your single-target heals will be mirrored to {targetName}. {(isTank ? "Excellent for sustained tank healing - heal anyone and the tank gets topped off too!" : "Useful during heavy damage phases.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Synastry is great when you need to heal multiple people but want to keep the tank topped. Link it to the tank, then heal whoever needs it - the tank gets healed too! Best for sustained damage phases.",
                    ConceptId = AstConcepts.SynastryUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryEarthlyStarPlacement(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.Manual)
            return false;

        if (player.Level < ASTActions.EarthlyStar.MinLevel)
            return false;

        // Don't place if star is already active
        if (context.IsStarPlaced)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EarthlyStar.ActionId))
            return false;

        // Timeline-aware: proactively place before raidwides
        // Earthly Star needs ~10s to mature for full Giant Dominance potency
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _,
            windowSeconds: 12f); // Longer window for Star maturation

        // Burst awareness: Place Earthly Star proactively before burst windows
        // Star needs ~10s to mature, so place 8-12s before burst
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Place Star 8-12 seconds before burst for maturation
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 8f && burstState.SecondsUntilBurst <= 12f)
            {
                burstImminent = true;
            }
        }

        // Only place proactively if raidwide or burst is imminent
        // Otherwise, rely on reactive placement when party HP drops
        if (!raidwideImminent && !burstImminent)
        {
            // Reactive placement: only place if party needs healing
            var (avgHp, _, _) = context.PartyHealthMetrics;
            if (avgHp > config.EarthlyStarDetonateThreshold)
                return false;
        }

        // Determine placement position (Earthly Star is ground-targeted, not entity-targeted)
        var targetPosition = player.Position;
        var targetName = "Self";

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.OnMainTank)
        {
            var tank = context.PartyHelper.FindTankInParty(player);
            if (tank != null)
            {
                targetPosition = tank.Position;
                targetName = tank.Name.TextValue;
            }
        }

        // Check if another Olympus healer already has a ground effect in this area
        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            targetPosition,
            ASTActions.EarthlyStar.ActionId,
            coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.EarthlyStarState = "Skipped (area covered)";
            return false;
        }

        var action = ASTActions.EarthlyStar;
        if (context.ActionService.ExecuteGroundTargetedOgcd(action, targetPosition))
        {
            // Notify service for state tracking
            context.EarthlyStarService.OnStarPlaced(targetPosition);

            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, targetPosition);

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = "Placed";
            var reason = raidwideImminent ? "Raidwide imminent" : (burstImminent ? "Burst imminent" : "Reactive");
            context.LogEarthlyStarDecision("Placed", $"{config.StarPlacement} ({targetName}) - {reason}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, _) = context.PartyHealthMetrics;
                var shortReason = raidwideImminent
                    ? $"Earthly Star placed - raidwide in ~10s!"
                    : burstImminent
                        ? $"Earthly Star placed - burst phase in ~10s"
                        : $"Earthly Star placed at {targetName}";

                var factors = new[]
                {
                    $"Placement: {config.StarPlacement} ({targetName})",
                    reason,
                    "Needs 10s to mature for Giant Dominance",
                    "Mature: 720 potency heal + 720 damage",
                    "Immature: 360 potency heal + 360 damage",
                };

                var alternatives = _earthlyStarPlacementAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Earthly Star",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Earthly Star placed at {targetName}'s position. {reason}. Star needs 10 seconds to mature into Giant Dominance (720 potency heal + damage). Placing proactively ensures it's ready when the party needs healing. {config.StarPlacement} strategy places star where it will hit the most party members.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Earthly Star is AST's strongest AoE heal when mature. Place it ~10s before you need it! Don't sit on cooldown - even immature detonation is better than not using it.",
                    ConceptId = AstConcepts.EarthlyStarPlacement,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryLadyOfCrowns(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMinorArcana)
            return false;

        // Only use Lady for emergency healing
        if (config.MinorArcanaStrategy != Config.MinorArcanaUsageStrategy.EmergencyOnly)
            return false;

        // Check if we have Lady card
        if (!context.CardService.HasLady)
            return false;

        if (player.Level < ASTActions.LadyOfCrowns.MinLevel)
            return false;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.LadyOfCrownsThreshold)
            return false;

        if (injured < 2)
            return false;

        var action = ASTActions.LadyOfCrowns;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CardState = "Lady Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Lady of Crowns - emergency AoE heal at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    $"Threshold: {config.LadyOfCrownsThreshold:P0}",
                    "400 potency AoE heal",
                    "Uses Minor Arcana card",
                };

                var alternatives = _ladyOfCrownsAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Lady of Crowns",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Lady of Crowns used for emergency AoE healing. Party at {avgHp:P0} average HP with {injured} injured. Lady provides 400 potency AoE heal - saving this from Minor Arcana instead of using Lord for damage is a healing gain when the party needs it!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Minor Arcana gives either Lord (damage) or Lady (heal). Lady is free AoE healing when you need it! In farm content, you might always use Lord for damage, but in prog or hard content, Lady can save the day.",
                    ConceptId = AstConcepts.MinorArcanaUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryHoroscopePreparation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope || !config.AutoCastHoroscope)
            return false;

        if (player.Level < ASTActions.Horoscope.MinLevel)
            return false;

        // Already have Horoscope buff
        if (context.HasHoroscope || context.HasHoroscopeHelios)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Horoscope.ActionId))
            return false;

        // Timeline-aware: prepare before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Only prepare if party might need healing soon (proactive) OR raidwide is imminent
        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > 0.85f && !raidwideImminent)
            return false;

        var action = ASTActions.Horoscope;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.HoroscopeState = "Prepared";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = raidwideImminent
                    ? "Horoscope prepared - raidwide incoming!"
                    : $"Horoscope prepared - party at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    raidwideImminent ? "Raidwide damage imminent" : "Proactive preparation",
                    "200 potency base (400 if enhanced)",
                    "Use Helios to enhance to 400 potency",
                    "30s buff duration",
                };

                var alternatives = _horoscopePreparationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Horoscope",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Horoscope prepared for upcoming healing. {(raidwideImminent ? "Raidwide damage expected soon - Horoscope will be ready to detonate!" : $"Party HP at {avgHp:P0} - preparing for healing needs.")} Remember to cast Helios/Aspected Helios to enhance Horoscope from 200 to 400 potency before detonating!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Horoscope is a two-step ability: 1) Activate it 2) Detonate it. For maximum value, cast Helios after activating to enhance it to 400 potency. Plan ahead - the buff lasts 30s!",
                    ConceptId = AstConcepts.HoroscopeUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryMacrocosmosPreparation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos || !config.AutoUseMacrocosmos)
            return false;

        if (player.Level < ASTActions.Macrocosmos.MinLevel)
            return false;

        // Already have Macrocosmos buff
        if (context.HasMacrocosmos)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Macrocosmos.ActionId))
            return false;

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.Macrocosmos.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.MacrocosmosMinTargets)
            return false;

        // Use proactively when party is taking damage
        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > config.MacrocosmosThreshold)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MacrocosmosState = "Skipped (remote mit)";
            return false;
        }

        // Macrocosmos is a GCD that deals damage and applies the buff
        var action = ASTActions.Macrocosmos;
        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.MacrocosmosState = "Applied";
            partyCoord?.OnCooldownUsed(action.ActionId, 180_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Macrocosmos applied - capturing damage ({membersInRange} in range)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Members in range: {membersInRange}",
                    $"Min targets: {config.MacrocosmosMinTargets}",
                    "Captures 50% of damage taken",
                    "Detonate with Microcosmos for big heal",
                };

                var alternatives = _macrocosmosAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Macrocosmos",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Macrocosmos applied to {membersInRange} party members at {avgHp:P0} average HP. For the next 15 seconds, 50% of all damage taken is captured. Detonate with Microcosmos for a massive heal proportional to damage absorbed (minimum 200 potency). This is AST's most powerful healing tool when used correctly!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Macrocosmos is AMAZING before big raidwides! Apply it before the damage hits, let the party take the hit, then detonate for massive healing. The more damage absorbed, the bigger the heal. Time it with fight mechanics!",
                    ConceptId = AstConcepts.MacrocosmosUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
