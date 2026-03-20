using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class HoroscopePreparationHandler : IHealingHandler
{
    public int Priority => 65;
    public string Name => "HoroscopePreparation";

    private static readonly string[] _alternatives =
    {
        "Wait for damage before preparing",
        "Save for known raidwide timing",
        "Use other heals directly",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryHoroscopePreparation(context);

    private bool TryHoroscopePreparation(IAstraeaContext context)
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

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Horoscope",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Horoscope prepared for upcoming healing. {(raidwideImminent ? "Raidwide damage expected soon - Horoscope will be ready to detonate!" : $"Party HP at {avgHp:P0} - preparing for healing needs.")} Remember to cast Helios/Aspected Helios to enhance Horoscope from 200 to 400 potency before detonating!",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Horoscope is a two-step ability: 1) Activate it 2) Detonate it. For maximum value, cast Helios after activating to enhance it to 400 potency. Plan ahead - the buff lasts 30s!",
                    ConceptId = AstConcepts.HoroscopeUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.HoroscopeUsage, wasSuccessful: true, raidwideImminent ? "Proactive Horoscope for raidwide" : "Horoscope prepared");
            }

            return true;
        }

        return false;
    }
}
