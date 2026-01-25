namespace Olympus.Services.Training;

/// <summary>
/// Optional context data for role-specific decision tracking.
/// All properties are nullable to allow only relevant data to be set.
/// </summary>
public sealed record DecisionContext
{
    // Healing context
    /// <summary>Target's HP percentage (0-100) when heal was decided.</summary>
    public float? TargetHpPercent { get; init; }

    /// <summary>Estimated heal amount.</summary>
    public int? HealAmount { get; init; }

    // Tank context
    /// <summary>Tank's own HP percentage when mitigation was decided.</summary>
    public float? SelfHpPercent { get; init; }

    // Melee DPS context
    /// <summary>Whether the positional requirement was met.</summary>
    public bool? HitPositional { get; init; }

    /// <summary>Position requirement (e.g., "Rear", "Flank").</summary>
    public string? Position { get; init; }

    /// <summary>Current combo step (1, 2, 3, etc.).</summary>
    public int? ComboStep { get; init; }

    // Ranged DPS context
    /// <summary>Name of the proc being used.</summary>
    public string? ProcName { get; init; }

    /// <summary>Current song name (BRD).</summary>
    public string? CurrentSong { get; init; }

    /// <summary>Remaining song duration in seconds.</summary>
    public float? SongRemaining { get; init; }

    /// <summary>Remaining DoT duration in seconds.</summary>
    public float? DotRemaining { get; init; }

    // Caster context
    /// <summary>Current element/phase name (e.g., "Astral Fire").</summary>
    public string? CurrentPhase { get; init; }

    /// <summary>Target element/phase name (e.g., "Umbral Ice").</summary>
    public string? NextPhase { get; init; }

    /// <summary>Summon or pet name (SMN).</summary>
    public string? SummonName { get; init; }

    // Shared context
    /// <summary>Primary gauge value.</summary>
    public int? GaugeValue { get; init; }

    /// <summary>Resource name (e.g., "Beast Gauge", "Ninki").</summary>
    public string? ResourceName { get; init; }

    /// <summary>Resource value.</summary>
    public int? ResourceValue { get; init; }

    /// <summary>Number of enemies in AoE range.</summary>
    public int? EnemyCount { get; init; }
}
