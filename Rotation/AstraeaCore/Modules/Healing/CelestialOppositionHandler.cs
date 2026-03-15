using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class CelestialOppositionHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "CelestialOpposition";

    private static readonly string[] _alternatives =
    {
        "Earthly Star (higher potency if mature)",
        "Helios Conjunction (GCD AoE)",
        "Save for predictable raidwide",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryCelestialOpposition(context);

    private bool TryCelestialOpposition(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCelestialOpposition)
            return false;

        if (player.Level < ASTActions.CelestialOpposition.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CelestialOpposition.ActionId))
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        var action = ASTActions.CelestialOpposition;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.CelestialOppositionState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CelestialOppositionState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injured) = context.PartyHealthMetrics;

                var shortReason = $"Celestial Opposition - {injured} injured at {avgHp:P0} avg";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    "200 potency heal + 15s regen",
                    "60s cooldown",
                    "oGCD - free healing",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Celestial Opposition",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Celestial Opposition used on {injured} injured party members at {avgHp:P0} average HP. Provides 200 potency instant heal plus a 15s regen (100 potency/tick). Free oGCD AoE heal on 60s cooldown - excellent value!",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Celestial Opposition is a free AoE heal + regen. Use it liberally! The regen continues ticking even while you DPS. Don't hold it for emergencies - that's what Essential Dignity is for.",
                    ConceptId = AstConcepts.CelestialOppositionUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private static bool ShouldUseAoEHeal(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHealthMetrics;

        // Timeline-aware: use before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if party needs healing OR raidwide is imminent
        return (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
    }
}
