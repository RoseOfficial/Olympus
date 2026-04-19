using Olympus.Timeline;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared decision logic for blocking cast-time damage GCDs when a raidwide
/// or tank buster is predicted to hit before the cast would complete.
/// Any rotation (healer, caster DPS, physical ranged, tank) can call
/// <see cref="ShouldBlock"/> before executing a cast-time GCD.
///
/// Instant GCDs (castTime &lt;= 0) always return false -- wrapping every
/// GCD call site is safe.
///
/// Caveat: <c>castTime</c> must reflect the effective cast time considering
/// current buffs. For buff-instant cases (BLM Firestarter, PLD Divine Might,
/// Swiftcast, etc.) pass <c>0</c> or skip the gate in the calling rotation.
/// Passing the base action cast time when a buff has made the cast instant
/// produces a false-positive block.
/// </summary>
public static class MechanicCastGate
{
    public static bool ShouldBlock(IRotationContext context, float castTime)
    {
        if (castTime <= 0f) return false;

        var cfg = context.Configuration.Timeline;
        if (!cfg.EnableMechanicAwareCasting) return false;
        if (!cfg.EnableTimelinePredictions) return false;

        var timeline = context.TimelineService;
        if (timeline == null || !timeline.IsActive) return false;
        if (timeline.Confidence < cfg.TimelineConfidenceThreshold) return false;

        var deadline = castTime + 0.5f;

        var raidwide = timeline.NextRaidwide;
        if (raidwide.HasValue && raidwide.Value.SecondsUntil > 0f && raidwide.Value.SecondsUntil <= deadline)
            return true;

        var tankbuster = timeline.NextTankBuster;
        if (tankbuster.HasValue && tankbuster.Value.SecondsUntil > 0f && tankbuster.Value.SecondsUntil <= deadline)
            return true;

        return false;
    }

    /// <summary>
    /// Produces a human-readable debug string describing why the gate would block,
    /// for surfacing in a module's debug state field.
    /// </summary>
    public static string FormatBlockedState(IRotationContext context)
    {
        var timeline = context.TimelineService;
        if (timeline == null) return "Held cast (mechanic)";

        var rw = timeline.NextRaidwide;
        var tb = timeline.NextTankBuster;

        bool rwCloser = rw.HasValue && (!tb.HasValue || rw.Value.SecondsUntil <= tb.Value.SecondsUntil);
        if (rwCloser && rw.HasValue)
            return $"Held cast (raidwide in {rw.Value.SecondsUntil:F1}s)";
        if (tb.HasValue)
            return $"Held cast (tank buster in {tb.Value.SecondsUntil:F1}s)";
        return "Held cast (mechanic)";
    }
}
