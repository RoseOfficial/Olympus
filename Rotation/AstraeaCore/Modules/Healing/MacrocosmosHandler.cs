using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class MacrocosmosHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "Macrocosmos";

    private static readonly string[] _alternatives =
    {
        "Save for predictable big raidwide",
        "Use other healing first",
        "Wait for party to stack",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        if (isMoving) return false;
        return TryMacrocosmosPreparation(context);
    }

    private bool TryMacrocosmosPreparation(IAstraeaContext context)
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
                    Alternatives = _alternatives,
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
