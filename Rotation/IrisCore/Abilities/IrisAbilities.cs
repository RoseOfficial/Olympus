using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.IrisCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Pictomancer rotation fires.
/// </summary>
public static class IrisAbilities
{
    // --- Base combo (single-target) ---
    public static readonly AbilityBehavior FireInRed = new() { Action = PCTActions.FireInRed };
    public static readonly AbilityBehavior AeroInGreen = new() { Action = PCTActions.AeroInGreen };
    public static readonly AbilityBehavior WaterInBlue = new() { Action = PCTActions.WaterInBlue };

    // --- Subtractive combo (single-target) ---
    public static readonly AbilityBehavior BlizzardInCyan = new() { Action = PCTActions.BlizzardInCyan, Toggle = cfg => cfg.Pictomancer.EnableSubtractiveCombo };
    public static readonly AbilityBehavior StoneInYellow = new() { Action = PCTActions.StoneInYellow, Toggle = cfg => cfg.Pictomancer.EnableSubtractiveCombo };
    public static readonly AbilityBehavior ThunderInMagenta = new() { Action = PCTActions.ThunderInMagenta, Toggle = cfg => cfg.Pictomancer.EnableSubtractiveCombo };

    // --- AoE base combo ---
    public static readonly AbilityBehavior Fire2InRed = new() { Action = PCTActions.Fire2InRed, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };
    public static readonly AbilityBehavior Aero2InGreen = new() { Action = PCTActions.Aero2InGreen, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };
    public static readonly AbilityBehavior Water2InBlue = new() { Action = PCTActions.Water2InBlue, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };

    // --- AoE subtractive ---
    public static readonly AbilityBehavior Blizzard2InCyan = new() { Action = PCTActions.Blizzard2InCyan, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };
    public static readonly AbilityBehavior Stone2InYellow = new() { Action = PCTActions.Stone2InYellow, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };
    public static readonly AbilityBehavior Thunder2InMagenta = new() { Action = PCTActions.Thunder2InMagenta, Toggle = cfg => cfg.Pictomancer.EnableAoERotation };

    // --- Paint spenders ---
    public static readonly AbilityBehavior HolyInWhite = new() { Action = PCTActions.HolyInWhite, Toggle = cfg => cfg.Pictomancer.EnableHolyInWhite };
    public static readonly AbilityBehavior CometInBlack = new() { Action = PCTActions.CometInBlack, Toggle = cfg => cfg.Pictomancer.EnableCometInBlack };

    // --- Motifs ---
    public static readonly AbilityBehavior PomMotif = new() { Action = PCTActions.PomMotif, Toggle = cfg => cfg.Pictomancer.EnableCreatureMotif };
    public static readonly AbilityBehavior WingMotif = new() { Action = PCTActions.WingMotif, Toggle = cfg => cfg.Pictomancer.EnableCreatureMotif };
    public static readonly AbilityBehavior ClawMotif = new() { Action = PCTActions.ClawMotif, Toggle = cfg => cfg.Pictomancer.EnableCreatureMotif };
    public static readonly AbilityBehavior MawMotif = new() { Action = PCTActions.MawMotif, Toggle = cfg => cfg.Pictomancer.EnableCreatureMotif };
    public static readonly AbilityBehavior HammerMotif = new() { Action = PCTActions.HammerMotif, Toggle = cfg => cfg.Pictomancer.EnableWeaponMotif };
    public static readonly AbilityBehavior StarrySkyMotif = new() { Action = PCTActions.StarrySkyMotif, Toggle = cfg => cfg.Pictomancer.EnableLandscapeMotif };

    // --- Living Muses ---
    public static readonly AbilityBehavior PomMuse = new() { Action = PCTActions.PomMuse, Toggle = cfg => cfg.Pictomancer.EnableLivingMuse };
    public static readonly AbilityBehavior WingedMuse = new() { Action = PCTActions.WingedMuse, Toggle = cfg => cfg.Pictomancer.EnableLivingMuse };
    public static readonly AbilityBehavior ClawedMuse = new() { Action = PCTActions.ClawedMuse, Toggle = cfg => cfg.Pictomancer.EnableLivingMuse };
    public static readonly AbilityBehavior FangedMuse = new() { Action = PCTActions.FangedMuse, Toggle = cfg => cfg.Pictomancer.EnableLivingMuse };

    // --- Steel/Scenic Muses & Starry Muse ---
    public static readonly AbilityBehavior StrikingMuse = new() { Action = PCTActions.StrikingMuse, Toggle = cfg => cfg.Pictomancer.EnableSteelMuse };
    public static readonly AbilityBehavior StarryMuse = new() { Action = PCTActions.StarryMuse, Toggle = cfg => cfg.Pictomancer.EnableStarryMuse };

    // --- Hammer combo ---
    public static readonly AbilityBehavior HammerStamp = new() { Action = PCTActions.HammerStamp };
    public static readonly AbilityBehavior HammerBrush = new() { Action = PCTActions.HammerBrush };
    public static readonly AbilityBehavior PolishingHammer = new() { Action = PCTActions.PolishingHammer };

    // --- Portraits ---
    public static readonly AbilityBehavior MogOfTheAges = new() { Action = PCTActions.MogOfTheAges, Toggle = cfg => cfg.Pictomancer.EnablePortraits };
    public static readonly AbilityBehavior RetributionOfTheMadeen = new() { Action = PCTActions.RetributionOfTheMadeen, Toggle = cfg => cfg.Pictomancer.EnablePortraits };

    // --- Rainbow Drip / Star Prism ---
    public static readonly AbilityBehavior RainbowDrip = new() { Action = PCTActions.RainbowDrip, Toggle = cfg => cfg.Pictomancer.EnableRainbowDrip };
    public static readonly AbilityBehavior StarPrism = new() { Action = PCTActions.StarPrism, Toggle = cfg => cfg.Pictomancer.EnableStarPrism };

    // --- Other oGCDs ---
    public static readonly AbilityBehavior SubtractivePalette = new() { Action = PCTActions.SubtractivePalette, Toggle = cfg => cfg.Pictomancer.EnableSubtractivePalette };
    public static readonly AbilityBehavior TemperaCoat = new() { Action = PCTActions.TemperaCoat, Toggle = cfg => cfg.Pictomancer.EnableTemperaCoat };
    public static readonly AbilityBehavior TemperaGrassa = new() { Action = PCTActions.TemperaGrassa, Toggle = cfg => cfg.Pictomancer.EnableTemperaGrassa };
    public static readonly AbilityBehavior Smudge = new() { Action = PCTActions.Smudge, Toggle = cfg => cfg.Pictomancer.EnableSmudge };

    // --- Role ---
    public static readonly AbilityBehavior Swiftcast = new() { Action = RoleActions.Swiftcast };
    public static readonly AbilityBehavior LucidDreaming = new() { Action = RoleActions.LucidDreaming, Toggle = cfg => cfg.CasterShared.EnableLucidDreaming };
    public static readonly AbilityBehavior Addle = new() { Action = RoleActions.Addle, Toggle = cfg => cfg.Pictomancer.EnableAddle };
}
