using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class LadyOfCrownsHandler : IHealingHandler
{
    public int Priority => 60;
    public string Name => "LadyOfCrowns";

    private static readonly string[] _alternatives =
    {
        "Lord of Crowns (400 potency damage)",
        "Celestial Opposition (if available)",
        "Save Lady for bigger emergency",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMinorArcana) return;
        if (config.MinorArcanaStrategy != MinorArcanaUsageStrategy.EmergencyOnly) return;
        if (!context.CardService.HasLady) return;
        if (player.Level < ASTActions.LadyOfCrowns.MinLevel) return;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.LadyOfCrownsThreshold) return;
        if (injured < 2) return;

        var action = ASTActions.LadyOfCrowns;
        var capturedAvgHp = avgHp;
        var capturedInjured = injured;

        scheduler.PushOgcd(AstraeaAbilities.LadyOfCrowns, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.CardState = "Lady Used";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Lady of Crowns - emergency AoE heal at {capturedAvgHp:P0}";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjured}",
                        $"Threshold: {config.LadyOfCrownsThreshold:P0}",
                        "400 potency AoE heal",
                        "Uses Minor Arcana card",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Lady of Crowns",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Lady of Crowns used for emergency AoE healing. Party at {capturedAvgHp:P0} average HP with {capturedInjured} injured. Lady provides 400 potency AoE heal - saving this from Minor Arcana instead of using Lord for damage is a healing gain when the party needs it!",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Minor Arcana gives either Lord (damage) or Lady (heal). Lady is free AoE healing when you need it! In farm content, you might always use Lord for damage, but in prog or hard content, Lady can save the day.",
                        ConceptId = AstConcepts.MinorArcanaUsage,
                        Priority = ExplanationPriority.High,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.MinorArcanaUsage, wasSuccessful: true, "Lady of Crowns emergency AoE heal");
                }
            });
    }
}
