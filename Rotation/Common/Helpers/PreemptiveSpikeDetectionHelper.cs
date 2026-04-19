using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Timeline;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared spike/raidwide detection for preemptive healing handlers.
/// Combines timeline predictions, reactive damage-spike detection, and
/// pattern-based prediction into a single decision, then locates the
/// most endangered party member whose projected HP would fall below the
/// configured preemptive threshold.
/// </summary>
public static class PreemptiveSpikeDetectionHelper
{
    public readonly struct Result
    {
        public IBattleChara Target { get; init; }
        public float Severity { get; init; }
        public string Source { get; init; }
        public bool IsTimelineRaidwide { get; init; }
        public float TargetHpPercent { get; init; }
        public float ProjectedHpPercent { get; init; }
        public float PatternConfidence { get; init; }
    }

    /// <summary>
    /// Detects an imminent spike and returns the endangered target if one
    /// is below the preemptive threshold after projected damage. Returns
    /// null if no spike is imminent, no target is endangered, or severity
    /// is too low to act on.
    /// </summary>
    public static Result? Detect(
        IPlayerCharacter player,
        Configuration config,
        ISpikeTargetSource partyHelper,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        IHpPredictionService hpPredictionService,
        IShieldTrackingService? shieldTrackingService,
        ICoHealerDetectionService? coHealerDetectionService,
        ITimelineService? timelineService,
        IBossMechanicDetector? bossMechanicDetector,
        float avgPartyHpPercent)
    {
        var healing = config.Healing;

        if (!healing.EnablePreemptiveHealing)
            return null;

        var isTimelineRaidwide = TimelineHelper.IsRaidwideImminent(
            timelineService, bossMechanicDetector, config, out var raidwideSource);

        var timelineRaidwideInfo = isTimelineRaidwide
            ? TimelineHelper.GetNextRaidwide(timelineService, bossMechanicDetector, config)
            : null;

        var isReactiveSpike = damageTrendService.IsDamageSpikeImminent(0.7f);
        var isPredictedSpike = false;
        var patternConfidence = 0f;

        if (!isTimelineRaidwide)
        {
            foreach (var member in partyHelper.GetAllPartyMembers(player))
            {
                if (member.IsDead)
                    continue;

                var (predictedSeconds, confidence) = damageTrendService.PredictNextSpike(member.EntityId);

                if (confidence >= healing.SpikePatternConfidenceThreshold &&
                    predictedSeconds <= healing.SpikePredictionLookahead &&
                    confidence > patternConfidence)
                {
                    isPredictedSpike = true;
                    patternConfidence = confidence;
                }
            }
        }

        if (!isTimelineRaidwide && !isReactiveSpike && !isPredictedSpike)
            return null;

        var spikeSeverity = damageTrendService.GetSpikeSeverity(avgPartyHpPercent);

        if (isTimelineRaidwide && timelineRaidwideInfo.HasValue)
        {
            var timelineConfidence = timelineRaidwideInfo.Value.confidence;
            spikeSeverity = Math.Max(spikeSeverity, 0.6f + (timelineConfidence * 0.3f));
        }
        else if (isPredictedSpike && patternConfidence >= 0.8f)
        {
            spikeSeverity = Math.Max(spikeSeverity, 0.5f + (patternConfidence * 0.3f));
        }

        var bypassSeverityCheck = isTimelineRaidwide &&
                                  timelineRaidwideInfo.HasValue &&
                                  timelineRaidwideInfo.Value.secondsUntil <= 3f;
        if (!bypassSeverityCheck && spikeSeverity < 0.4f)
            return null;

        var target = partyHelper.FindMostEndangeredPartyMember(
            player, damageIntakeService, 0, damageTrendService, shieldTrackingService);
        if (target is null)
            return null;

        var targetHpPercent = partyHelper.GetHpPercent(target);
        var targetDamageRate = damageTrendService.GetCurrentDamageRate(target.EntityId, 3f);

        var initialLookahead = healing.UseSpellCastTimeForLookahead
            ? Math.Max(healing.MinPreemptiveLookahead, 1.5f)
            : 2f;

        var projectedDamage = targetDamageRate * initialLookahead;
        var projectedHp = target.CurrentHp > projectedDamage
            ? target.CurrentHp - (uint)projectedDamage
            : 0;
        var projectedHpPercent = (float)projectedHp / target.MaxHp;

        if (projectedHpPercent > healing.PreemptiveHealingThreshold)
            return null;

        var pendingHeals = hpPredictionService.GetPendingHealAmount(target.EntityId);
        if (healing.EnableCoHealerAwareness && coHealerDetectionService?.HasCoHealer == true)
        {
            var coHealerPendingHeals = coHealerDetectionService.CoHealerPendingHeals;
            if (coHealerPendingHeals.TryGetValue(target.EntityId, out var coHealerPending))
                pendingHeals += coHealerPending;
        }
        if (projectedHp + pendingHeals > target.MaxHp * healing.PreemptiveHealingThreshold)
            return null;

        string source;
        if (isTimelineRaidwide)
            source = $"timeline ({raidwideSource})";
        else if (isPredictedSpike)
            source = $"pattern (confidence {patternConfidence:P0})";
        else
            source = "reactive spike detection";

        return new Result
        {
            Target = target,
            Severity = spikeSeverity,
            Source = source,
            IsTimelineRaidwide = isTimelineRaidwide,
            TargetHpPercent = targetHpPercent,
            ProjectedHpPercent = projectedHpPercent,
            PatternConfidence = patternConfidence,
        };
    }
}
