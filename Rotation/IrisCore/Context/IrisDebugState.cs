namespace Olympus.Rotation.IrisCore.Context;

/// <summary>
/// Debug state for Iris (Pictomancer) rotation.
/// Tracks all relevant gauge, buff, and ability state for debugging.
/// </summary>
public sealed class IrisDebugState
{
    // Planning state
    public string PlanningState { get; set; } = "";
    public string PlannedAction { get; set; } = "";

    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Palette Gauge
    public int PaletteGauge { get; set; }
    public bool CanUseSubtractive { get; set; }

    // Paint stacks
    public int WhitePaint { get; set; }
    public bool HasBlackPaint { get; set; }

    // Canvas state
    public string CreatureMotif { get; set; } = "None";
    public bool HasCreatureCanvas { get; set; }
    public bool HasWeaponCanvas { get; set; }
    public bool HasLandscapeCanvas { get; set; }

    // Portrait state
    public bool MogReady { get; set; }
    public bool MadeenReady { get; set; }

    // Hammer combo
    public bool IsInHammerCombo { get; set; }
    public int HammerComboStep { get; set; }
    public string HammerComboStepName { get; set; } = "None";

    // Base combo
    public int BaseComboStep { get; set; }
    public bool IsInSubtractiveCombo { get; set; }

    // Buff state
    public bool HasStarryMuse { get; set; }
    public float StarryMuseRemaining { get; set; }
    public bool HasHyperphantasia { get; set; }
    public int HyperphantasiaStacks { get; set; }
    public bool HasInspiration { get; set; }
    public bool HasSubtractiveSpectrum { get; set; }
    public bool HasRainbowBright { get; set; }
    public bool HasStarstruck { get; set; }
    public bool HasSwiftcast { get; set; }
    public bool HasHammerTime { get; set; }
    public int HammerTimeStacks { get; set; }

    // Cooldown state
    public bool StarryMuseReady { get; set; }
    public bool LivingMuseReady { get; set; }
    public int LivingMuseCharges { get; set; }
    public bool StrikingMuseReady { get; set; }
    public bool SubtractivePaletteReady { get; set; }
    public bool TemperaCoatReady { get; set; }
    public bool TemperaGrassaReady { get; set; }
    public bool SmudgeReady { get; set; }

    // Resource state
    public int CurrentMp { get; set; }
    public int MaxMp { get; set; }

    // Combat info
    public string CurrentTarget { get; set; } = "None";
    public int NearbyEnemies { get; set; }

    // Phase tracking
    public string Phase { get; set; } = "Waiting";
}
