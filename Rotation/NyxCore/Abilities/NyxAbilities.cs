using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.NyxCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Dark Knight
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
/// </summary>
public static class NyxAbilities
{
    // --- Basic ST combo ---
    public static readonly AbilityBehavior HardSlash = new() { Action = DRKActions.HardSlash, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior SyphonStrike = new() { Action = DRKActions.SyphonStrike, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior Souleater = new() { Action = DRKActions.Souleater, Toggle = cfg => cfg.Tank.EnableDamage };

    // --- AoE combo ---
    public static readonly AbilityBehavior Unleash = new() { Action = DRKActions.Unleash, Toggle = cfg => cfg.Tank.EnableAoEDamage };
    public static readonly AbilityBehavior StalwartSoul = new() { Action = DRKActions.StalwartSoul, Toggle = cfg => cfg.Tank.EnableAoEDamage };

    // --- Blood Gauge spenders ---
    public static readonly AbilityBehavior Bloodspiller = new() { Action = DRKActions.Bloodspiller, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior Quietus = new() { Action = DRKActions.Quietus, Toggle = cfg => cfg.Tank.EnableAoEDamage };

    // --- Delirium combo (Lv.96+) ---
    public static readonly AbilityBehavior ScarletDelirium = new() { Action = DRKActions.ScarletDelirium, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior Comeuppance = new() { Action = DRKActions.Comeuppance, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior Torcleaver = new() { Action = DRKActions.Torcleaver, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior Impalement = new() { Action = DRKActions.Impalement, Toggle = cfg => cfg.Tank.EnableAoEDamage };

    // --- L100 AoE finisher ---
    public static readonly AbilityBehavior Disesteem = new() { Action = DRKActions.Disesteem, Toggle = cfg => cfg.Tank.EnableDamage };

    // --- MP spenders ---
    public static readonly AbilityBehavior EdgeOfShadow = new() { Action = DRKActions.EdgeOfShadow, Toggle = cfg => cfg.Tank.EnableDamage };
    public static readonly AbilityBehavior FloodOfShadow = new() { Action = DRKActions.FloodOfShadow, Toggle = cfg => cfg.Tank.EnableAoEDamage };

    // --- oGCD damage ---
    public static readonly AbilityBehavior Shadowbringer = new() { Action = DRKActions.Shadowbringer, Toggle = cfg => cfg.Tank.EnableShadowbringer };
    public static readonly AbilityBehavior SaltedEarth = new() { Action = DRKActions.SaltedEarth, Toggle = cfg => cfg.Tank.EnableSaltedEarth };
    public static readonly AbilityBehavior SaltAndDarkness = new() { Action = DRKActions.SaltAndDarkness, Toggle = cfg => cfg.Tank.EnableSaltedEarth };
    public static readonly AbilityBehavior CarveAndSpit = new() { Action = DRKActions.CarveAndSpit, Toggle = cfg => cfg.Tank.EnableCarveAndSpit };
    public static readonly AbilityBehavior AbyssalDrain = new() { Action = DRKActions.AbyssalDrain, Toggle = cfg => cfg.Tank.EnableAbyssalDrain };
    public static readonly AbilityBehavior Shadowstride = new() { Action = DRKActions.Shadowstride, Toggle = cfg => cfg.Tank.EnableShadowstride };

    // --- Ranged ---
    public static readonly AbilityBehavior Unmend = new() { Action = DRKActions.Unmend, Toggle = cfg => cfg.Tank.EnableDamage };

    // --- Buffs ---
    public static readonly AbilityBehavior BloodWeapon = new() { Action = DRKActions.BloodWeapon, Toggle = cfg => cfg.Tank.EnableBloodWeapon };
    public static readonly AbilityBehavior Delirium = new() { Action = DRKActions.Delirium, Toggle = cfg => cfg.Tank.EnableDelirium };
    public static readonly AbilityBehavior LivingShadow = new() { Action = DRKActions.LivingShadow, Toggle = cfg => cfg.Tank.EnableLivingShadow };
    public static readonly AbilityBehavior Grit = new() { Action = DRKActions.Grit };

    // --- Defensives ---
    public static readonly AbilityBehavior LivingDead = new() { Action = DRKActions.LivingDead, Toggle = cfg => cfg.Tank.EnableLivingDead };
    public static readonly AbilityBehavior ShadowWall = new()
    {
        Action = DRKActions.ShadowWall,
        Toggle = cfg => cfg.Tank.EnableShadowWall,
        LevelReplacements = new[] { ((byte)92, DRKActions.ShadowedVigil) },
    };
    public static readonly AbilityBehavior DarkMind = new() { Action = DRKActions.DarkMind, Toggle = cfg => cfg.Tank.EnableDarkMind };
    public static readonly AbilityBehavior Oblation = new() { Action = DRKActions.Oblation, Toggle = cfg => cfg.Tank.EnableOblation };
    public static readonly AbilityBehavior TheBlackestNight = new() { Action = DRKActions.TheBlackestNight, Toggle = cfg => cfg.Tank.EnableTheBlackestNight };
    public static readonly AbilityBehavior DarkMissionary = new() { Action = DRKActions.DarkMissionary, Toggle = cfg => cfg.Tank.EnableDarkMissionary };

    // --- Role actions ---
    public static readonly AbilityBehavior Rampart = new() { Action = RoleActions.Rampart };
    public static readonly AbilityBehavior Reprisal = new() { Action = RoleActions.Reprisal, Toggle = cfg => cfg.Tank.EnableReprisal };
    public static readonly AbilityBehavior ArmsLength = new() { Action = RoleActions.ArmsLength, Toggle = cfg => cfg.Tank.EnableArmsLength };
    public static readonly AbilityBehavior Interject = new() { Action = RoleActions.Interject, Toggle = cfg => cfg.Tank.EnableInterject };
    public static readonly AbilityBehavior LowBlow = new() { Action = RoleActions.LowBlow, Toggle = cfg => cfg.Tank.EnableLowBlow };
    public static readonly AbilityBehavior Provoke = new() { Action = RoleActions.Provoke };
    public static readonly AbilityBehavior Shirk = new() { Action = RoleActions.Shirk };
}
