namespace Olympus.Config;

/// <summary>
/// Configuration for the Draw Helper feature — world-space visual overlays.
/// </summary>
public sealed class DrawHelperConfig
{
    // Master toggle
    public bool DrawingEnabled { get; set; } = false;

    // Pictomancy backend
    public bool UsePictomancy { get; set; } = true;
    public float PictomancyMaxAlpha { get; set; } = 0.5f;
    public bool PictomancyClipNativeUI { get; set; } = true;

    // Enemy hitboxes
    public bool ShowEnemyHitboxes { get; set; } = false;
    public uint EnemyHitboxColor { get; set; } = 0x500000FFu; // semi-transparent red (ABGR)

    // Melee range indicator
    public bool ShowMeleeRange { get; set; } = false;
    public bool MeleeRangeFade { get; set; } = true;
    public uint MeleeRangeColor { get; set; } = 0xC000FF00u; // green
    public uint MeleeRangeOutOfRangeColor { get; set; } = 0xC000FFFFu; // yellow

    // Ranged range indicator (auto-detects 25y for all ranged/caster jobs)
    public bool ShowRangedRange { get; set; } = false;
    public uint RangedRangeColor { get; set; } = 0xC0FF8000u; // blue-ish
    public uint RangedRangeOutOfRangeColor { get; set; } = 0xC000FFFFu; // yellow

    // Positionals
    public bool ShowPositionals { get; set; } = false;
    public uint PositionalRearColor { get; set; } = 0x5000FF00u; // green
    public uint PositionalFlankColor { get; set; } = 0x50CFCF51u; // cyan
}
