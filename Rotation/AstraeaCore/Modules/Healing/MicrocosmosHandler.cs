using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class MicrocosmosHandler : IHealingHandler
{
    public int Priority => 35;
    public string Name => "Microcosmos";

    private static readonly string[] _alternatives =
    {
        "Wait for timer to expire (auto-detonates)",
        "Let more damage accumulate",
        "Use other heals first",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryMicrocosmos(context);

    private bool TryMicrocosmos(IAstraeaContext context)
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
                    Alternatives = _alternatives,
                    Tip = "Microcosmos heals based on damage taken during Macrocosmos. Use Macrocosmos BEFORE big raidwides to capture the damage, then detonate for massive healing. Time it with predictable damage!",
                    ConceptId = AstConcepts.MacrocosmosUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
