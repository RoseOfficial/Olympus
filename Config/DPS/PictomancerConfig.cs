using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Pictomancer (Iris) configuration options.
/// Controls canvas system, Muse abilities, and burst windows.
/// </summary>
public sealed class PictomancerConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use single-target rotation.
    /// </summary>
    public bool EnableSingleTargetRotation { get; set; } = true;

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Fire in Red/Aero in Green/Water in Blue (combo spells).
    /// </summary>
    public bool EnableSubtractiveCombo { get; set; } = true;

    /// <summary>
    /// Whether to use Holy in White.
    /// </summary>
    public bool EnableHolyInWhite { get; set; } = true;

    /// <summary>
    /// Whether to use Comet in Black.
    /// </summary>
    public bool EnableCometInBlack { get; set; } = true;

    /// <summary>
    /// Whether to use Rainbow Drip.
    /// </summary>
    public bool EnableRainbowDrip { get; set; } = true;

    /// <summary>
    /// Whether to use Star Prism.
    /// </summary>
    public bool EnableStarPrism { get; set; } = true;

    #endregion

    #region Canvas Toggles

    /// <summary>
    /// Whether to use Creature Motif abilities.
    /// </summary>
    public bool EnableCreatureMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Weapon Motif abilities.
    /// </summary>
    public bool EnableWeaponMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Landscape Motif abilities.
    /// </summary>
    public bool EnableLandscapeMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Pom Motif (Creature).
    /// </summary>
    public bool EnablePomMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Wing Motif (Creature).
    /// </summary>
    public bool EnableWingMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Claw Motif (Creature).
    /// </summary>
    public bool EnableClawMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Maw Motif (Creature).
    /// </summary>
    public bool EnableMawMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Hammer Motif (Weapon).
    /// </summary>
    public bool EnableHammerMotif { get; set; } = true;

    /// <summary>
    /// Whether to use Starry Sky Motif (Landscape).
    /// </summary>
    public bool EnableStarrySkyMotif { get; set; } = true;

    #endregion

    #region Muse Toggles

    /// <summary>
    /// Whether to use Living Muse.
    /// </summary>
    public bool EnableLivingMuse { get; set; } = true;

    /// <summary>
    /// Whether to use Steel Muse.
    /// </summary>
    public bool EnableSteelMuse { get; set; } = true;

    /// <summary>
    /// Whether to use Scenic Muse.
    /// </summary>
    public bool EnableScenicMuse { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Starry Muse (party buff).
    /// </summary>
    public bool EnableStarryMuse { get; set; } = true;

    /// <summary>
    /// Whether to use Subtractive Palette.
    /// </summary>
    public bool EnableSubtractivePalette { get; set; } = true;

    #endregion

    #region Palette Settings

    /// <summary>
    /// Minimum Palette gauge to use Holy in White.
    /// </summary>
    private int _holyMinPalette = 50;
    public int HolyMinPalette
    {
        get => _holyMinPalette;
        set => _holyMinPalette = Math.Clamp(value, 25, 100);
    }

    /// <summary>
    /// Save Palette for Comet in Black (requires Subtractive).
    /// </summary>
    public bool SavePaletteForComet { get; set; } = true;

    #endregion

    #region Canvas Settings

    /// <summary>
    /// Preferred creature motif order.
    /// </summary>
    public CreatureOrder CreatureMotifOrder { get; set; } = CreatureOrder.PomWingClawMaw;

    /// <summary>
    /// Pre-paint motifs out of combat.
    /// </summary>
    public bool PrepaintMotifs { get; set; } = true;

    /// <summary>
    /// Which motifs to pre-paint.
    /// </summary>
    public PrepaintOption PrepaintOption { get; set; } = PrepaintOption.All;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Pool Hammer Time and paint resources for raid buff burst windows.
    /// When enabled, saves Hammer Stamp combos within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    /// <summary>
    /// Align Starry Muse with party burst windows.
    /// </summary>
    public bool AlignStarryMuseWithParty { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Starry Muse waiting for party buffs.
    /// </summary>
    private float _starryMuseHoldTime = 3.0f;
    public float StarryMuseHoldTime
    {
        get => _starryMuseHoldTime;
        set => _starryMuseHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Use Hammer combo during burst windows.
    /// </summary>
    public bool UseHammerDuringBurst { get; set; } = true;

    #endregion

    #region AoE Settings

    /// <summary>
    /// Minimum enemies for AoE rotation.
    /// </summary>
    private int _aoEMinTargets = 3;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region MP Management

    /// <summary>
    /// Whether to use Lucid Dreaming.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold for Lucid Dreaming.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Utility Settings

    /// <summary>
    /// Whether to use Tempera Coat for self-mitigation.
    /// </summary>
    public bool EnableTemperaCoat { get; set; } = true;

    /// <summary>
    /// Whether to use Tempera Grassa for party mitigation.
    /// </summary>
    public bool EnableTemperaGrassa { get; set; } = true;

    #endregion
}

/// <summary>
/// Creature motif order preference.
/// </summary>
public enum CreatureOrder
{
    /// <summary>
    /// Pom → Wing → Claw → Maw (standard).
    /// </summary>
    PomWingClawMaw,

    /// <summary>
    /// Maw → Claw → Wing → Pom (reverse).
    /// </summary>
    MawClawWingPom
}

/// <summary>
/// Pre-paint motif options.
/// </summary>
public enum PrepaintOption
{
    /// <summary>
    /// Pre-paint all available motifs.
    /// </summary>
    All,

    /// <summary>
    /// Pre-paint Creature motifs only.
    /// </summary>
    CreatureOnly,

    /// <summary>
    /// Pre-paint Weapon motifs only.
    /// </summary>
    WeaponOnly,

    /// <summary>
    /// Pre-paint Landscape motifs only.
    /// </summary>
    LandscapeOnly,

    /// <summary>
    /// Do not pre-paint motifs.
    /// </summary>
    None
}
