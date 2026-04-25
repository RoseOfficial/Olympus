using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope || !config.AutoCastHoroscope) return;
        if (player.Level < ASTActions.Horoscope.MinLevel) return;
        if (context.HasHoroscope || context.HasHoroscopeHelios) return;
        if (!context.ActionService.IsActionReady(ASTActions.Horoscope.ActionId)) return;

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > 0.85f && !raidwideImminent) return;

        var action = ASTActions.Horoscope;
        var capturedAvgHp = avgHp;
        var capturedRaidwideImminent = raidwideImminent;

        scheduler.PushOgcd(AstraeaAbilities.Horoscope, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.HoroscopeState = "Prepared";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedRaidwideImminent
                        ? "Horoscope prepared - raidwide incoming!"
                        : $"Horoscope prepared - party at {capturedAvgHp:P0}";

                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        capturedRaidwideImminent ? "Raidwide damage imminent" : "Proactive preparation",
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
                        DetailedReason = $"Horoscope prepared for upcoming healing. {(capturedRaidwideImminent ? "Raidwide damage expected soon - Horoscope will be ready to detonate!" : $"Party HP at {capturedAvgHp:P0} - preparing for healing needs.")} Remember to cast Helios/Aspected Helios to enhance Horoscope from 200 to 400 potency before detonating!",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Horoscope is a two-step ability: 1) Activate it 2) Detonate it. For maximum value, cast Helios after activating to enhance it to 400 potency. Plan ahead - the buff lasts 30s!",
                        ConceptId = AstConcepts.HoroscopeUsage,
                        Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.HoroscopeUsage, wasSuccessful: true, capturedRaidwideImminent ? "Proactive Horoscope for raidwide" : "Horoscope prepared");
                }
            });
    }
}
