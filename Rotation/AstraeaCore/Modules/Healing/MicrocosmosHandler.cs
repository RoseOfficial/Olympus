using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos) return;
        if (!context.HasMacrocosmos) return;
        if (player.Level < ASTActions.Microcosmos.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.Microcosmos.ActionId)) return;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > 0.70f && injured < 3) return;

        var action = ASTActions.Microcosmos;
        var capturedAvgHp = avgHp;
        var capturedInjured = injured;

        scheduler.PushOgcd(AstraeaAbilities.Microcosmos, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.MacrocosmosState = "Detonated";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Microcosmos detonated - {capturedInjured} injured at {capturedAvgHp:P0}";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjured}",
                        "Heals 50% of damage taken during Macrocosmos",
                        "Minimum 200 potency heal",
                        "oGCD detonation",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Microcosmos",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Microcosmos (Macrocosmos detonation) used on {capturedInjured} injured party members at {capturedAvgHp:P0} average HP. Heals for 50% of all damage taken during the Macrocosmos buff (minimum 200 potency). The more damage absorbed, the bigger the heal!",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Microcosmos heals based on damage taken during Macrocosmos. Use Macrocosmos BEFORE big raidwides to capture the damage, then detonate for massive healing. Time it with predictable damage!",
                        ConceptId = AstConcepts.MacrocosmosUsage,
                        Priority = ExplanationPriority.High,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.MacrocosmosUsage, wasSuccessful: true, "Macrocosmos detonated via Microcosmos");
                }
            });
    }
}
