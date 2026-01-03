namespace Olympus.Data;

/// <summary>
/// FFXIV combat timing constants. These are server-side values that
/// define the fundamental rhythm of combat.
/// </summary>
public static class FFXIVTimings
{
    // GCD Base Timings
    /// <summary>Base GCD duration without speed buffs (2.5 seconds).</summary>
    public const float GcdBase = 2.5f;

    /// <summary>Minimum GCD with maximum spell/skill speed.</summary>
    public const float GcdMinimum = 1.5f;

    /// <summary>Base animation lock for most actions (~0.7 seconds).</summary>
    public const float AnimationLockBase = 0.7f;

    /// <summary>Animation lock for true instant abilities (~0.1 seconds).</summary>
    public const float AnimationLockInstant = 0.1f;

    /// <summary>Window before GCD ends where actions can be queued (~0.5 seconds).</summary>
    public const float QueueWindow = 0.5f;

    // Server Communication
    /// <summary>Server tick rate (~30Hz, 33ms per tick).</summary>
    public const float ServerTickRate = 0.033f;

    /// <summary>Estimated server latency for action confirmation.</summary>
    public const float LatencyEstimate = 0.075f;

    /// <summary>Typical delay from cast completion to effect application.</summary>
    public const float ActionEffectDelay = 0.6f;

    // oGCD Weaving Windows
    /// <summary>Time available for 1 oGCD after an instant GCD.</summary>
    public const float SingleWeaveWindow = 0.7f;

    /// <summary>GCD must be at least this long for safe double weave.</summary>
    public const float DoubleWeaveThreshold = 2.1f;

    // Healing Specific
    /// <summary>Time from cast complete to HP change being visible.</summary>
    public const float HealEffectDelay = 0.5f;

    /// <summary>Server-side DoT/HoT tick interval.</summary>
    public const float RegenTickRate = 3.0f;

    /// <summary>
    /// Protection window after casting a heal before allowing another heal decision.
    /// This accounts for server latency and effect application delay.
    /// </summary>
    public const float HealProtectionWindow = 1.0f;
}
