using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.EchidnaCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Viper rotation fires.
/// </summary>
public static class EchidnaAbilities
{
    // --- ST dual wield combo ---
    public static readonly AbilityBehavior SteelFangs = new() { Action = VPRActions.SteelFangs };
    public static readonly AbilityBehavior ReavingFangs = new() { Action = VPRActions.ReavingFangs };
    public static readonly AbilityBehavior HuntersSting = new() { Action = VPRActions.HuntersSting };
    public static readonly AbilityBehavior SwiftskinsString = new() { Action = VPRActions.SwiftskinsString };
    public static readonly AbilityBehavior FlankstingStrike = new() { Action = VPRActions.FlankstingStrike };
    public static readonly AbilityBehavior FlanksbaneFang = new() { Action = VPRActions.FlanksbaneFang };
    public static readonly AbilityBehavior HindstingStrike = new() { Action = VPRActions.HindstingStrike };
    public static readonly AbilityBehavior HindsbaneFang = new() { Action = VPRActions.HindsbaneFang };

    // --- AoE dual wield combo ---
    public static readonly AbilityBehavior SteelMaw = new() { Action = VPRActions.SteelMaw, Toggle = cfg => cfg.Viper.EnableAoERotation };
    public static readonly AbilityBehavior ReavingMaw = new() { Action = VPRActions.ReavingMaw, Toggle = cfg => cfg.Viper.EnableAoERotation };
    public static readonly AbilityBehavior HuntersBite = new() { Action = VPRActions.HuntersBite, Toggle = cfg => cfg.Viper.EnableAoERotation };
    public static readonly AbilityBehavior SwiftskinsBite = new() { Action = VPRActions.SwiftskinsBite, Toggle = cfg => cfg.Viper.EnableAoERotation };
    public static readonly AbilityBehavior JaggedMaw = new() { Action = VPRActions.JaggedMaw, Toggle = cfg => cfg.Viper.EnableAoERotation };
    public static readonly AbilityBehavior BloodiedMaw = new() { Action = VPRActions.BloodiedMaw, Toggle = cfg => cfg.Viper.EnableAoERotation };

    // --- Twinblade (Vicewinder/Vicepit chains) ---
    public static readonly AbilityBehavior Vicewinder = new() { Action = VPRActions.Vicewinder, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };
    public static readonly AbilityBehavior HuntersCoil = new() { Action = VPRActions.HuntersCoil, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };
    public static readonly AbilityBehavior SwiftskinsCoil = new() { Action = VPRActions.SwiftskinsCoil, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };
    public static readonly AbilityBehavior Vicepit = new() { Action = VPRActions.Vicepit, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };
    public static readonly AbilityBehavior HuntersDen = new() { Action = VPRActions.HuntersDen, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };
    public static readonly AbilityBehavior SwiftskinsDen = new() { Action = VPRActions.SwiftskinsDen, Toggle = cfg => cfg.Viper.EnableTwinbladeCombo };

    // --- Twinfang/Twinblood oGCD procs ---
    public static readonly AbilityBehavior Twinfang = new() { Action = VPRActions.Twinfang };
    public static readonly AbilityBehavior Twinblood = new() { Action = VPRActions.Twinblood };
    public static readonly AbilityBehavior TwinfangBite = new() { Action = VPRActions.TwinfangBite };
    public static readonly AbilityBehavior TwinbloodBite = new() { Action = VPRActions.TwinbloodBite };

    // --- Uncoiled Fury chain ---
    public static readonly AbilityBehavior UncoiledFury = new() { Action = VPRActions.UncoiledFury, Toggle = cfg => cfg.Viper.EnableUncoiledFury };
    public static readonly AbilityBehavior UncoiledTwinfang = new() { Action = VPRActions.UncoiledTwinfang };
    public static readonly AbilityBehavior UncoiledTwinblood = new() { Action = VPRActions.UncoiledTwinblood };

    // --- Reawaken / Generations / Ouroboros ---
    public static readonly AbilityBehavior Reawaken = new() { Action = VPRActions.Reawaken, Toggle = cfg => cfg.Viper.EnableReawaken };
    public static readonly AbilityBehavior FirstGeneration = new() { Action = VPRActions.FirstGeneration, Toggle = cfg => cfg.Viper.EnableGenerationAbilities };
    public static readonly AbilityBehavior SecondGeneration = new() { Action = VPRActions.SecondGeneration, Toggle = cfg => cfg.Viper.EnableGenerationAbilities };
    public static readonly AbilityBehavior ThirdGeneration = new() { Action = VPRActions.ThirdGeneration, Toggle = cfg => cfg.Viper.EnableGenerationAbilities };
    public static readonly AbilityBehavior FourthGeneration = new() { Action = VPRActions.FourthGeneration, Toggle = cfg => cfg.Viper.EnableGenerationAbilities };
    public static readonly AbilityBehavior FirstLegacy = new() { Action = VPRActions.FirstLegacy };
    public static readonly AbilityBehavior SecondLegacy = new() { Action = VPRActions.SecondLegacy };
    public static readonly AbilityBehavior ThirdLegacy = new() { Action = VPRActions.ThirdLegacy };
    public static readonly AbilityBehavior FourthLegacy = new() { Action = VPRActions.FourthLegacy };
    public static readonly AbilityBehavior Ouroboros = new() { Action = VPRActions.Ouroboros, Toggle = cfg => cfg.Viper.EnableOuroboros };

    // --- Ranged filler ---
    public static readonly AbilityBehavior WrithingSnap = new() { Action = VPRActions.WrithingSnap };

    // --- Buff ---
    public static readonly AbilityBehavior SerpentsIre = new() { Action = VPRActions.SerpentsIre, Toggle = cfg => cfg.Viper.EnableSerpentsIre };

    // --- Role ---
    public static readonly AbilityBehavior SecondWind = new() { Action = RoleActions.SecondWind, Toggle = cfg => cfg.MeleeShared.EnableSecondWind };
    public static readonly AbilityBehavior Bloodbath = new() { Action = RoleActions.Bloodbath, Toggle = cfg => cfg.Viper.EnableBloodbath };
    public static readonly AbilityBehavior Feint = new() { Action = RoleActions.Feint, Toggle = cfg => cfg.Viper.EnableFeint };
    public static readonly AbilityBehavior TrueNorth = new() { Action = RoleActions.TrueNorth, Toggle = cfg => cfg.Viper.EnableTrueNorth };
}
