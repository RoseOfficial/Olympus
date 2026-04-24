using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ThemisCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Paladin
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
///
/// Complex multi-factor gates (party coordination, tank cooldown service decisions,
/// HP thresholds, enmity position checks, etc.) are NOT encoded here — they live
/// in the module's TryPushXxx methods and short-circuit before pushing.
/// AbilityBehavior only encodes the simple declarative gates (Toggle, ProcBuff).
/// </summary>
public static class ThemisAbilities
{
    // --- Basic ST combo ---

    public static readonly AbilityBehavior FastBlade = new()
    {
        Action = PLDActions.FastBlade,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior RiotBlade = new()
    {
        Action = PLDActions.RiotBlade,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior RoyalAuthority = new()
    {
        Action = PLDActions.RoyalAuthority,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior RageOfHalone = new()
    {
        Action = PLDActions.RageOfHalone,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- AoE combo ---

    public static readonly AbilityBehavior TotalEclipse = new()
    {
        Action = PLDActions.TotalEclipse,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior Prominence = new()
    {
        Action = PLDActions.Prominence,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    // --- Atonement chain (Sword Oath) ---

    public static readonly AbilityBehavior Atonement = new()
    {
        Action = PLDActions.Atonement,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior Supplication = new()
    {
        Action = PLDActions.Supplication,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior Sepulchre = new()
    {
        Action = PLDActions.Sepulchre,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- DoT / Blade of Honor ---

    public static readonly AbilityBehavior GoringBlade = new()
    {
        Action = PLDActions.GoringBlade,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior BladeOfHonor = new()
    {
        Action = PLDActions.BladeOfHonor,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- Magic Phase (Requiescat) ---

    public static readonly AbilityBehavior HolySpirit = new()
    {
        Action = PLDActions.HolySpirit,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior HolyCircle = new()
    {
        Action = PLDActions.HolyCircle,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior Confiteor = new()
    {
        Action = PLDActions.Confiteor,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior BladeOfFaith = new()
    {
        Action = PLDActions.BladeOfFaith,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior BladeOfTruth = new()
    {
        Action = PLDActions.BladeOfTruth,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior BladeOfValor = new()
    {
        Action = PLDActions.BladeOfValor,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- oGCD damage ---

    public static readonly AbilityBehavior CircleOfScorn = new()
    {
        Action = PLDActions.CircleOfScorn,
        Toggle = cfg => cfg.Tank.EnableCircleOfScorn,
    };

    public static readonly AbilityBehavior Expiacion = new()
    {
        Action = PLDActions.Expiacion,
        Toggle = cfg => cfg.Tank.EnableSpiritsWithin,
    };

    public static readonly AbilityBehavior SpiritsWithin = new()
    {
        Action = PLDActions.SpiritsWithin,
        Toggle = cfg => cfg.Tank.EnableSpiritsWithin,
    };

    public static readonly AbilityBehavior Intervene = new()
    {
        Action = PLDActions.Intervene,
        Toggle = cfg => cfg.Tank.EnableIntervene,
    };

    // --- Ranged filler ---

    public static readonly AbilityBehavior ShieldLob = new()
    {
        Action = PLDActions.ShieldLob,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- Buffs ---

    public static readonly AbilityBehavior FightOrFlight = new()
    {
        Action = PLDActions.FightOrFlight,
        Toggle = cfg => cfg.Tank.EnableFightOrFlight,
    };

    public static readonly AbilityBehavior Requiescat = new()
    {
        Action = PLDActions.Requiescat,
        Toggle = cfg => cfg.Tank.EnableRequiescat,
    };

    public static readonly AbilityBehavior IronWill = new()
    {
        Action = PLDActions.IronWill,
    };

    // --- Defensives ---

    public static readonly AbilityBehavior Sheltron = new()
    {
        Action = PLDActions.Sheltron,
        Toggle = cfg => cfg.Tank.EnableSheltron,
        LevelReplacements = new[] { ((byte)82, PLDActions.HolySheltron) },
    };

    public static readonly AbilityBehavior Sentinel = new()
    {
        Action = PLDActions.Sentinel,
        Toggle = cfg => cfg.Tank.EnableSentinel,
        LevelReplacements = new[] { ((byte)92, PLDActions.Guardian) },
    };

    public static readonly AbilityBehavior Bulwark = new()
    {
        Action = PLDActions.Bulwark,
        Toggle = cfg => cfg.Tank.EnableBulwark,
    };

    public static readonly AbilityBehavior HallowedGround = new()
    {
        Action = PLDActions.HallowedGround,
        Toggle = cfg => cfg.Tank.EnableHallowedGround,
    };

    public static readonly AbilityBehavior DivineVeil = new()
    {
        Action = PLDActions.DivineVeil,
        Toggle = cfg => cfg.Tank.EnableDivineVeil,
    };

    public static readonly AbilityBehavior Cover = new()
    {
        Action = PLDActions.Cover,
        Toggle = cfg => cfg.Tank.EnableCover,
    };

    public static readonly AbilityBehavior Clemency = new()
    {
        Action = PLDActions.Clemency,
        Toggle = cfg => cfg.Tank.EnableClemency,
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
