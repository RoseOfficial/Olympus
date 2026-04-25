using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos || !config.AutoUseMacrocosmos) return;
        if (player.Level < ASTActions.Macrocosmos.MinLevel) return;
        if (context.HasMacrocosmos) return;
        if (!context.ActionService.IsActionReady(ASTActions.Macrocosmos.ActionId)) return;

        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.Macrocosmos.RadiusSquared)
                membersInRange++;
        }
        if (membersInRange < config.MacrocosmosMinTargets) return;

        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > config.MacrocosmosThreshold) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MacrocosmosState = "Skipped (remote mit)";
            return;
        }

        var action = ASTActions.Macrocosmos;
        var capturedAvgHp = avgHp;
        var capturedMembersInRange = membersInRange;

        scheduler.PushGcd(AstraeaAbilities.Macrocosmos, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.MacrocosmosState = "Applied";
                partyCoord?.OnCooldownUsed(action.ActionId, 180_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Macrocosmos applied - capturing damage ({capturedMembersInRange} in range)";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Members in range: {capturedMembersInRange}",
                        $"Min targets: {config.MacrocosmosMinTargets}",
                        "Captures 50% of damage taken",
                        "Detonate with Microcosmos for big heal",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Macrocosmos",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Macrocosmos applied to {capturedMembersInRange} party members at {capturedAvgHp:P0} average HP. For the next 15 seconds, 50% of all damage taken is captured. Detonate with Microcosmos for a massive heal proportional to damage absorbed (minimum 200 potency). This is AST's most powerful healing tool when used correctly!",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Macrocosmos is AMAZING before big raidwides! Apply it before the damage hits, let the party take the hit, then detonate for massive healing. The more damage absorbed, the bigger the heal. Time it with fight mechanics!",
                        ConceptId = AstConcepts.MacrocosmosUsage,
                        Priority = ExplanationPriority.High,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.MacrocosmosUsage, wasSuccessful: true, "Macrocosmos applied - damage capture started");
                }
            });
    }
}
