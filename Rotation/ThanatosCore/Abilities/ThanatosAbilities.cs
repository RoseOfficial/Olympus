using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ThanatosCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Reaper rotation fires.
/// </summary>
public static class ThanatosAbilities
{
    // --- Basic combo (ST) ---
    public static readonly AbilityBehavior Slice = new() { Action = RPRActions.Slice };
    public static readonly AbilityBehavior WaxingSlice = new() { Action = RPRActions.WaxingSlice };
    public static readonly AbilityBehavior InfernalSlice = new() { Action = RPRActions.InfernalSlice };

    // --- Basic combo (AoE) ---
    public static readonly AbilityBehavior SpinningScythe = new() { Action = RPRActions.SpinningScythe, Toggle = cfg => cfg.Reaper.EnableAoERotation };
    public static readonly AbilityBehavior NightmareScythe = new() { Action = RPRActions.NightmareScythe, Toggle = cfg => cfg.Reaper.EnableAoERotation };

    // --- Death's Design ---
    public static readonly AbilityBehavior ShadowOfDeath = new() { Action = RPRActions.ShadowOfDeath };
    public static readonly AbilityBehavior WhorlOfDeath = new() { Action = RPRActions.WhorlOfDeath, Toggle = cfg => cfg.Reaper.EnableAoERotation };

    // --- Soul Reaver finishers ---
    public static readonly AbilityBehavior Gibbet = new() { Action = RPRActions.Gibbet, Toggle = cfg => cfg.Reaper.EnableSoulReaver };
    public static readonly AbilityBehavior Gallows = new() { Action = RPRActions.Gallows, Toggle = cfg => cfg.Reaper.EnableSoulReaver };
    public static readonly AbilityBehavior Guillotine = new() { Action = RPRActions.Guillotine, Toggle = cfg => cfg.Reaper.EnableSoulReaver };

    // --- Enshroud GCDs ---
    public static readonly AbilityBehavior VoidReaping = new() { Action = RPRActions.VoidReaping, Toggle = cfg => cfg.Reaper.EnableEnshroud };
    public static readonly AbilityBehavior CrossReaping = new() { Action = RPRActions.CrossReaping, Toggle = cfg => cfg.Reaper.EnableEnshroud };
    public static readonly AbilityBehavior GrimReaping = new() { Action = RPRActions.GrimReaping, Toggle = cfg => cfg.Reaper.EnableEnshroud };

    // --- Communio / Perfectio ---
    public static readonly AbilityBehavior Communio = new() { Action = RPRActions.Communio, Toggle = cfg => cfg.Reaper.EnableCommunio };
    public static readonly AbilityBehavior Perfectio = new() { Action = RPRActions.Perfectio, Toggle = cfg => cfg.Reaper.EnablePerfectio };

    // --- Soul spenders ---
    public static readonly AbilityBehavior BloodStalk = new() { Action = RPRActions.BloodStalk };
    public static readonly AbilityBehavior GrimSwathe = new() { Action = RPRActions.GrimSwathe };
    public static readonly AbilityBehavior Gluttony = new() { Action = RPRActions.Gluttony, Toggle = cfg => cfg.Reaper.EnableGluttony };
    public static readonly AbilityBehavior UnveiledGibbet = new() { Action = RPRActions.UnveiledGibbet };
    public static readonly AbilityBehavior UnveiledGallows = new() { Action = RPRActions.UnveiledGallows };

    // --- Lemure ---
    public static readonly AbilityBehavior LemuresSlice = new() { Action = RPRActions.LemuresSlice, Toggle = cfg => cfg.Reaper.EnableLemureAbilities };
    public static readonly AbilityBehavior LemuresScythe = new() { Action = RPRActions.LemuresScythe, Toggle = cfg => cfg.Reaper.EnableLemureAbilities };
    public static readonly AbilityBehavior Sacrificium = new() { Action = RPRActions.Sacrificium, Toggle = cfg => cfg.Reaper.EnableEnshroud };

    // --- Buffs / harvest ---
    public static readonly AbilityBehavior ArcaneCircle = new() { Action = RPRActions.ArcaneCircle, Toggle = cfg => cfg.Reaper.EnableArcaneCircle };
    public static readonly AbilityBehavior Enshroud = new() { Action = RPRActions.Enshroud, Toggle = cfg => cfg.Reaper.EnableEnshroud };
    public static readonly AbilityBehavior PlentifulHarvest = new() { Action = RPRActions.PlentifulHarvest, Toggle = cfg => cfg.Reaper.EnablePlentifulHarvest };
    public static readonly AbilityBehavior HarvestMoon = new() { Action = RPRActions.HarvestMoon, Toggle = cfg => cfg.Reaper.EnableHarvestMoon };

    // --- Soul builder ---
    public static readonly AbilityBehavior SoulSlice = new() { Action = RPRActions.SoulSlice };
    public static readonly AbilityBehavior SoulScythe = new() { Action = RPRActions.SoulScythe };

    // --- Role ---
    public static readonly AbilityBehavior Feint = new() { Action = RoleActions.Feint, Toggle = cfg => cfg.Reaper.EnableFeint };
}
