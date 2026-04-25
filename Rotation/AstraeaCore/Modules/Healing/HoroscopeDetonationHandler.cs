using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope) return;
        if (!context.HasHoroscope && !context.HasHoroscopeHelios) return;
        if (!context.ActionService.IsActionReady(ASTActions.HoroscopeEnd.ActionId)) return;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.HoroscopeThreshold) return;
        if (injured < config.HoroscopeMinTargets) return;

        var action = ASTActions.HoroscopeEnd;
        var capturedAvgHp = avgHp;
        var capturedInjured = injured;

        scheduler.PushOgcd(AstraeaAbilities.HoroscopeEnd, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.HoroscopeState = "Detonated";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var isEnhanced = context.HasHoroscopeHelios;
                    var shortReason = isEnhanced
                        ? $"Horoscope Helios detonated - {capturedInjured} at {capturedAvgHp:P0}"
                        : $"Horoscope detonated - {capturedInjured} at {capturedAvgHp:P0}";
                    var factors = new[]
                    {
                        isEnhanced ? "Enhanced with Helios (400 potency)" : "Basic Horoscope (200 potency)",
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjured}",
                        $"Min targets: {config.HoroscopeMinTargets}",
                        "oGCD - free AoE heal",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Horoscope",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Horoscope detonated on {capturedInjured} injured party members at {capturedAvgHp:P0} average HP. {(isEnhanced ? "Enhanced with Helios for 400 potency - double the value!" : "Basic 200 potency heal. Consider using Helios after Horoscope to enhance it next time!")} Free oGCD heal that expires after 30s.",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = isEnhanced
                            ? "Great! You enhanced Horoscope with Helios for double potency. This is the optimal way to use Horoscope!"
                            : "Horoscope can be enhanced to 400 potency by casting Helios/Aspected Helios while it's active. Try to enhance it when possible!",
                        ConceptId = AstConcepts.HoroscopeUsage,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.HoroscopeUsage, wasSuccessful: isEnhanced, isEnhanced ? "Enhanced Horoscope detonated" : "Unenhanced Horoscope detonated");
                }
            });
    }
}
