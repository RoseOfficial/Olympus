using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class HoroscopeDetonationHandler : IHealingHandler
{
    public int Priority => 30;
    public string Name => "HoroscopeDetonation";

    private static readonly string[] _alternatives =
    {
        "Let it expire naturally (wastes it)",
        "Celestial Opposition (if available)",
        "Wait for more injured targets",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryHoroscopeDetonation(context);

    private bool TryHoroscopeDetonation(IAstraeaContext context)
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
                    Alternatives = _alternatives,
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
}
