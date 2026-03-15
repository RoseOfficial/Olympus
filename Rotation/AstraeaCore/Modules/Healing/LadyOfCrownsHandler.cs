using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryLadyOfCrowns(context);

    private bool TryLadyOfCrowns(IAstraeaContext context)
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
                    Alternatives = _alternatives,
                    Tip = "Minor Arcana gives either Lord (damage) or Lady (heal). Lady is free AoE healing when you need it! In farm content, you might always use Lord for damage, but in prog or hard content, Lady can save the day.",
                    ConceptId = AstConcepts.MinorArcanaUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
