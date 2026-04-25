using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AresCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Warrior
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
/// </summary>
public static class AresAbilities
{
    // --- Basic ST combo ---

    public static readonly AbilityBehavior HeavySwing = new()
    {
        Action = WARActions.HeavySwing,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior Maim = new()
    {
        Action = WARActions.Maim,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior StormsPath = new()
    {
        Action = WARActions.StormsPath,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior StormsEye = new()
    {
        Action = WARActions.StormsEye,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- AoE combo ---

    public static readonly AbilityBehavior Overpower = new()
    {
        Action = WARActions.Overpower,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior MythrilTempest = new()
    {
        Action = WARActions.MythrilTempest,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    // --- Gauge spenders ---

    public static readonly AbilityBehavior FellCleave = new()
    {
        Action = WARActions.FellCleave,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior InnerBeast = new()
    {
        Action = WARActions.InnerBeast,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior Decimate = new()
    {
        Action = WARActions.Decimate,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior SteelCyclone = new()
    {
        Action = WARActions.SteelCyclone,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    // --- Nascent Chaos burst GCDs ---

    public static readonly AbilityBehavior InnerChaos = new()
    {
        Action = WARActions.InnerChaos,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior ChaoticCyclone = new()
    {
        Action = WARActions.ChaoticCyclone,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    // --- Primal chain ---

    public static readonly AbilityBehavior PrimalRend = new()
    {
        Action = WARActions.PrimalRend,
        Toggle = cfg => cfg.Tank.EnablePrimalRend,
    };

    public static readonly AbilityBehavior PrimalRuination = new()
    {
        Action = WARActions.PrimalRuination,
        Toggle = cfg => cfg.Tank.EnablePrimalRuination,
    };

    public static readonly AbilityBehavior PrimalWrath = new()
    {
        Action = WARActions.PrimalWrath,
        Toggle = cfg => cfg.Tank.EnablePrimalWrath,
    };

    // --- oGCD damage ---

    public static readonly AbilityBehavior Upheaval = new()
    {
        Action = WARActions.Upheaval,
        Toggle = cfg => cfg.Tank.EnableOrogeny,
    };

    public static readonly AbilityBehavior Orogeny = new()
    {
        Action = WARActions.Orogeny,
        Toggle = cfg => cfg.Tank.EnableOrogeny,
    };

    public static readonly AbilityBehavior Onslaught = new()
    {
        Action = WARActions.Onslaught,
        Toggle = cfg => cfg.Tank.EnableOnslaught,
    };

    public static readonly AbilityBehavior Tomahawk = new()
    {
        Action = WARActions.Tomahawk,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- Buffs ---

    public static readonly AbilityBehavior InnerRelease = new()
    {
        Action = WARActions.InnerRelease,
        Toggle = cfg => cfg.Tank.EnableInnerRelease,
    };

    public static readonly AbilityBehavior Infuriate = new()
    {
        Action = WARActions.Infuriate,
        Toggle = cfg => cfg.Tank.EnableInfuriate,
    };

    public static readonly AbilityBehavior Defiance = new()
    {
        Action = WARActions.Defiance,
    };

    // --- Defensives ---

    public static readonly AbilityBehavior Holmgang = new()
    {
        Action = WARActions.Holmgang,
        Toggle = cfg => cfg.Tank.EnableHolmgang,
    };

    public static readonly AbilityBehavior Vengeance = new()
    {
        Action = WARActions.Vengeance,
        Toggle = cfg => cfg.Tank.EnableVengeance,
    };

    public static readonly AbilityBehavior RawIntuition = new()
    {
        Action = WARActions.RawIntuition,
        Toggle = cfg => cfg.Tank.EnableBloodWhetting,
    };

    public static readonly AbilityBehavior ThrillOfBattle = new()
    {
        Action = WARActions.ThrillOfBattle,
        Toggle = cfg => cfg.Tank.EnableThrillOfBattle,
    };

    public static readonly AbilityBehavior Equilibrium = new()
    {
        Action = WARActions.Equilibrium,
        Toggle = cfg => cfg.Tank.EnableEquilibrium,
    };

    public static readonly AbilityBehavior ShakeItOff = new()
    {
        Action = WARActions.ShakeItOff,
        Toggle = cfg => cfg.Tank.EnableShakeItOff,
    };

    public static readonly AbilityBehavior NascentFlash = new()
    {
        Action = WARActions.NascentFlash,
        Toggle = cfg => cfg.Tank.EnableNascentFlash,
    };

    // --- Role actions ---

    public static readonly AbilityBehavior Rampart = new()
    {
        Action = RoleActions.Rampart,
    };

    public static readonly AbilityBehavior Reprisal = new()
    {
        Action = RoleActions.Reprisal,
        Toggle = cfg => cfg.Tank.EnableReprisal,
    };

    public static readonly AbilityBehavior ArmsLength = new()
    {
        Action = RoleActions.ArmsLength,
        Toggle = cfg => cfg.Tank.EnableArmsLength,
    };

    public static readonly AbilityBehavior Interject = new()
    {
        Action = RoleActions.Interject,
        Toggle = cfg => cfg.Tank.EnableInterject,
    };

    public static readonly AbilityBehavior LowBlow = new()
    {
        Action = RoleActions.LowBlow,
        Toggle = cfg => cfg.Tank.EnableLowBlow,
    };

    public static readonly AbilityBehavior Provoke = new()
    {
        Action = RoleActions.Provoke,
    };

    public static readonly AbilityBehavior Shirk = new()
    {
        Action = RoleActions.Shirk,
    };
}
