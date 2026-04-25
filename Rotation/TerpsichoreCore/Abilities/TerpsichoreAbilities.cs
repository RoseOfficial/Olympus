using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.TerpsichoreCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Dancer rotation fires.
/// </summary>
public static class TerpsichoreAbilities
{
    // --- Combo / fillers ---
    public static readonly AbilityBehavior Cascade = new() { Action = DNCActions.Cascade };
    public static readonly AbilityBehavior Fountain = new() { Action = DNCActions.Fountain };
    public static readonly AbilityBehavior ReverseCascade = new() { Action = DNCActions.ReverseCascade };
    public static readonly AbilityBehavior Fountainfall = new() { Action = DNCActions.Fountainfall };

    // --- AoE combo ---
    public static readonly AbilityBehavior Windmill = new() { Action = DNCActions.Windmill, Toggle = cfg => cfg.Dancer.EnableAoERotation };
    public static readonly AbilityBehavior Bladeshower = new() { Action = DNCActions.Bladeshower, Toggle = cfg => cfg.Dancer.EnableAoERotation };
    public static readonly AbilityBehavior RisingWindmill = new() { Action = DNCActions.RisingWindmill, Toggle = cfg => cfg.Dancer.EnableAoERotation };
    public static readonly AbilityBehavior Bloodshower = new() { Action = DNCActions.Bloodshower, Toggle = cfg => cfg.Dancer.EnableAoERotation };

    // --- Dance steps & finishes ---
    public static readonly AbilityBehavior StandardStep = new() { Action = DNCActions.StandardStep, Toggle = cfg => cfg.Dancer.EnableStandardStep };
    public static readonly AbilityBehavior TechnicalStep = new() { Action = DNCActions.TechnicalStep, Toggle = cfg => cfg.Dancer.EnableTechnicalStep };
    public static readonly AbilityBehavior Emboite = new() { Action = DNCActions.Emboite };
    public static readonly AbilityBehavior Entrechat = new() { Action = DNCActions.Entrechat };
    public static readonly AbilityBehavior Jete = new() { Action = DNCActions.Jete };
    public static readonly AbilityBehavior Pirouette = new() { Action = DNCActions.Pirouette };
    public static readonly AbilityBehavior StandardFinish = new() { Action = DNCActions.StandardFinish };
    public static readonly AbilityBehavior TechnicalFinish = new() { Action = DNCActions.TechnicalFinish };

    // --- Burst follow-ups ---
    public static readonly AbilityBehavior Tillana = new() { Action = DNCActions.Tillana, Toggle = cfg => cfg.Dancer.EnableTillana };
    public static readonly AbilityBehavior SaberDance = new() { Action = DNCActions.SaberDance, Toggle = cfg => cfg.Dancer.EnableSaberDance };
    public static readonly AbilityBehavior DanceOfTheDawn = new() { Action = DNCActions.DanceOfTheDawn, Toggle = cfg => cfg.Dancer.EnableSaberDance };
    public static readonly AbilityBehavior StarfallDance = new() { Action = DNCActions.StarfallDance, Toggle = cfg => cfg.Dancer.EnableStarfallDance };
    public static readonly AbilityBehavior LastDance = new() { Action = DNCActions.LastDance, Toggle = cfg => cfg.Dancer.EnableLastDance };
    public static readonly AbilityBehavior FinishingMove = new() { Action = DNCActions.FinishingMove, Toggle = cfg => cfg.Dancer.EnableFinishingMove };

    // --- Fan Dance oGCDs ---
    public static readonly AbilityBehavior FanDance = new() { Action = DNCActions.FanDance, Toggle = cfg => cfg.Dancer.EnableFanDance };
    public static readonly AbilityBehavior FanDanceII = new() { Action = DNCActions.FanDanceII, Toggle = cfg => cfg.Dancer.EnableFanDance };
    public static readonly AbilityBehavior FanDanceIII = new() { Action = DNCActions.FanDanceIII, Toggle = cfg => cfg.Dancer.EnableFanDance };
    public static readonly AbilityBehavior FanDanceIV = new() { Action = DNCActions.FanDanceIV, Toggle = cfg => cfg.Dancer.EnableFanDanceIV };

    // --- Burst buffs ---
    public static readonly AbilityBehavior Devilment = new() { Action = DNCActions.Devilment, Toggle = cfg => cfg.Dancer.EnableDevilment };
    public static readonly AbilityBehavior Flourish = new() { Action = DNCActions.Flourish, Toggle = cfg => cfg.Dancer.EnableFlourish };
    public static readonly AbilityBehavior ClosedPosition = new() { Action = DNCActions.ClosedPosition };

    // --- Role ---
    public static readonly AbilityBehavior HeadGraze = new() { Action = RoleActions.HeadGraze, Toggle = cfg => cfg.RangedShared.EnableHeadGraze };
}
