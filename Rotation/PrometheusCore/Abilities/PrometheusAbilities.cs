using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.PrometheusCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Machinist rotation fires.
/// Includes LevelReplacements for GaussRound→DoubleCheck and Ricochet→Checkmate at Lv.92.
/// </summary>
public static class PrometheusAbilities
{
    // --- Combo (1-2-3) ---
    public static readonly AbilityBehavior SplitShot = new()
    {
        Action = MCHActions.SplitShot,
        LevelReplacements = new[] { ((byte)54, MCHActions.HeatedSplitShot) },
    };

    public static readonly AbilityBehavior SlugShot = new()
    {
        Action = MCHActions.SlugShot,
        LevelReplacements = new[] { ((byte)60, MCHActions.HeatedSlugShot) },
    };

    public static readonly AbilityBehavior CleanShot = new()
    {
        Action = MCHActions.CleanShot,
        LevelReplacements = new[] { ((byte)64, MCHActions.HeatedCleanShot) },
    };

    // --- Tools ---
    public static readonly AbilityBehavior Drill = new() { Action = MCHActions.Drill, Toggle = cfg => cfg.Machinist.EnableDrill };
    public static readonly AbilityBehavior AirAnchor = new()
    {
        Action = MCHActions.HotShot,
        Toggle = cfg => cfg.Machinist.EnableAirAnchor,
        LevelReplacements = new[] { ((byte)76, MCHActions.AirAnchor) },
    };
    public static readonly AbilityBehavior ChainSaw = new() { Action = MCHActions.ChainSaw, Toggle = cfg => cfg.Machinist.EnableChainSaw };
    public static readonly AbilityBehavior Excavator = new() { Action = MCHActions.Excavator, Toggle = cfg => cfg.Machinist.EnableExcavator };
    public static readonly AbilityBehavior FullMetalField = new() { Action = MCHActions.FullMetalField, Toggle = cfg => cfg.Machinist.EnableFullMetalField };

    // --- Overheated GCDs ---
    public static readonly AbilityBehavior HeatBlast = new()
    {
        Action = MCHActions.HeatBlast,
        Toggle = cfg => cfg.Machinist.EnableHeatBlast,
        LevelReplacements = new[] { ((byte)100, MCHActions.BlazingShot) },
    };
    public static readonly AbilityBehavior AutoCrossbow = new() { Action = MCHActions.AutoCrossbow, Toggle = cfg => cfg.Machinist.EnableAutoCrossbow };

    // --- AoE GCDs ---
    public static readonly AbilityBehavior SpreadShot = new()
    {
        Action = MCHActions.SpreadShot,
        Toggle = cfg => cfg.Machinist.EnableAoERotation,
        LevelReplacements = new[] { ((byte)82, MCHActions.Scattergun) },
    };
    public static readonly AbilityBehavior Bioblaster = new() { Action = MCHActions.Bioblaster, Toggle = cfg => cfg.Machinist.EnableAoERotation };

    // --- oGCDs ---
    public static readonly AbilityBehavior GaussRound = new()
    {
        Action = MCHActions.GaussRound,
        Toggle = cfg => cfg.Machinist.EnableGaussRicochet,
        LevelReplacements = new[] { ((byte)92, MCHActions.DoubleCheck) },
    };
    public static readonly AbilityBehavior Ricochet = new()
    {
        Action = MCHActions.Ricochet,
        Toggle = cfg => cfg.Machinist.EnableGaussRicochet,
        LevelReplacements = new[] { ((byte)92, MCHActions.Checkmate) },
    };

    public static readonly AbilityBehavior Reassemble = new() { Action = MCHActions.Reassemble, Toggle = cfg => cfg.Machinist.EnableReassemble };
    public static readonly AbilityBehavior BarrelStabilizer = new() { Action = MCHActions.BarrelStabilizer, Toggle = cfg => cfg.Machinist.EnableBarrelStabilizer };
    public static readonly AbilityBehavior Wildfire = new() { Action = MCHActions.Wildfire, Toggle = cfg => cfg.Machinist.EnableWildfire };
    public static readonly AbilityBehavior Hypercharge = new() { Action = MCHActions.Hypercharge, Toggle = cfg => cfg.Machinist.EnableHypercharge };

    // --- Pet ---
    public static readonly AbilityBehavior AutomatonQueen = new()
    {
        Action = MCHActions.RookAutoturret,
        Toggle = cfg => cfg.Machinist.EnableAutomatonQueen,
        LevelReplacements = new[] { ((byte)80, MCHActions.AutomatonQueen) },
    };

    // --- Role ---
    public static readonly AbilityBehavior HeadGraze = new() { Action = RoleActions.HeadGraze, Toggle = cfg => cfg.RangedShared.EnableHeadGraze };
}
