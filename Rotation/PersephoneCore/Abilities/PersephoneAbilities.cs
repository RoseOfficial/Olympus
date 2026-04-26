using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.PersephoneCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Summoner rotation fires.
/// Demi-summon GCDs use ReplacementBaseId = Ruin3 because UseAction rejects the replacement
/// IDs (Astral Impulse / Fountain of Fire / Umbral Impulse) directly — the game upgrades
/// Ruin III server-side based on the active demi-summon. See CLAUDE.md ActionManager Quirks.
/// </summary>
public static class PersephoneAbilities
{
    // --- Filler / Ruin ---
    public static readonly AbilityBehavior Ruin = new() { Action = SMNActions.Ruin, Toggle = cfg => cfg.Summoner.EnableRuin };
    public static readonly AbilityBehavior Ruin2 = new() { Action = SMNActions.Ruin2, Toggle = cfg => cfg.Summoner.EnableRuin };
    public static readonly AbilityBehavior Ruin3 = new() { Action = SMNActions.Ruin3, Toggle = cfg => cfg.Summoner.EnableRuin };
    public static readonly AbilityBehavior Ruin4 = new() { Action = SMNActions.Ruin4, Toggle = cfg => cfg.Summoner.EnableRuinIV };

    // --- AoE filler ---
    public static readonly AbilityBehavior Outburst = new() { Action = SMNActions.Outburst, Toggle = cfg => cfg.Summoner.EnableAoERotation };
    public static readonly AbilityBehavior TriDisaster = new() { Action = SMNActions.TriDisaster, Toggle = cfg => cfg.Summoner.EnableAoERotation };

    // --- Demi-summon GCDs (replacement chain via Ruin3 base) ---
    public static readonly AbilityBehavior AstralImpulse = new()
    {
        Action = SMNActions.AstralImpulse,
        Toggle = cfg => cfg.Summoner.EnableAstralAbilities,
        ReplacementBaseId = SMNActions.Ruin3.ActionId,
    };
    public static readonly AbilityBehavior AstralFlare = new()
    {
        Action = SMNActions.AstralFlare,
        Toggle = cfg => cfg.Summoner.EnableAstralAbilities,
        ReplacementBaseId = SMNActions.TriDisaster.ActionId,
    };
    public static readonly AbilityBehavior FountainOfFire = new()
    {
        Action = SMNActions.FountainOfFire,
        Toggle = cfg => cfg.Summoner.EnableFountainAbilities,
        ReplacementBaseId = SMNActions.Ruin3.ActionId,
    };
    public static readonly AbilityBehavior BrandOfPurgatory = new()
    {
        Action = SMNActions.BrandOfPurgatory,
        Toggle = cfg => cfg.Summoner.EnableFountainAbilities,
        ReplacementBaseId = SMNActions.TriDisaster.ActionId,
    };
    public static readonly AbilityBehavior UmbralImpulse = new()
    {
        Action = SMNActions.UmbralImpulse,
        Toggle = cfg => cfg.Summoner.EnableAstralAbilities,
        ReplacementBaseId = SMNActions.Ruin3.ActionId,
    };
    public static readonly AbilityBehavior UmbralFlare = new()
    {
        Action = SMNActions.UmbralFlare,
        Toggle = cfg => cfg.Summoner.EnableAstralAbilities,
        ReplacementBaseId = SMNActions.TriDisaster.ActionId,
    };

    // --- Primal attunement GCDs ---
    public static readonly AbilityBehavior RubyRite = new() { Action = SMNActions.RubyRite, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior RubyCatastrophe = new() { Action = SMNActions.RubyCatastrophe, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior TopazRite = new() { Action = SMNActions.TopazRite, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior TopazCatastrophe = new() { Action = SMNActions.TopazCatastrophe, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior EmeraldRite = new() { Action = SMNActions.EmeraldRite, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior EmeraldCatastrophe = new() { Action = SMNActions.EmeraldCatastrophe, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };

    // --- Primal favor GCDs ---
    public static readonly AbilityBehavior CrimsonCyclone = new() { Action = SMNActions.CrimsonCyclone, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };
    public static readonly AbilityBehavior Slipstream = new() { Action = SMNActions.Slipstream, Toggle = cfg => cfg.Summoner.EnablePrimalAbilities };

    // --- Carbuncle / Primal summons ---
    public static readonly AbilityBehavior SummonCarbuncle = new() { Action = SMNActions.SummonCarbuncle };
    public static readonly AbilityBehavior SummonIfrit = new() { Action = SMNActions.SummonIfrit, Toggle = cfg => cfg.Summoner.EnableIfrit };
    public static readonly AbilityBehavior SummonTitan = new() { Action = SMNActions.SummonTitan, Toggle = cfg => cfg.Summoner.EnableTitan };
    public static readonly AbilityBehavior SummonGaruda = new() { Action = SMNActions.SummonGaruda, Toggle = cfg => cfg.Summoner.EnableGaruda };

    // --- oGCD damage ---
    public static readonly AbilityBehavior EnergyDrain = new() { Action = SMNActions.EnergyDrain, Toggle = cfg => cfg.Summoner.EnableEnergyDrain };
    public static readonly AbilityBehavior EnergySiphon = new() { Action = SMNActions.EnergySiphon, Toggle = cfg => cfg.Summoner.EnableEnergyDrain };
    public static readonly AbilityBehavior Necrotize = new() { Action = SMNActions.Necrotize, Toggle = cfg => cfg.Summoner.EnableFester };
    public static readonly AbilityBehavior Fester = new() { Action = SMNActions.Fester, Toggle = cfg => cfg.Summoner.EnableFester };
    public static readonly AbilityBehavior Painflare = new() { Action = SMNActions.Painflare, Toggle = cfg => cfg.Summoner.EnableFester };
    public static readonly AbilityBehavior MountainBuster = new() { Action = SMNActions.MountainBuster, Toggle = cfg => cfg.Summoner.EnableMountainBuster };

    // --- Buffs / Enkindle / Astral Flow ---
    public static readonly AbilityBehavior SearingLight = new() { Action = SMNActions.SearingLight, Toggle = cfg => cfg.Summoner.EnableSearingLight };
    public static readonly AbilityBehavior SearingFlash = new() { Action = SMNActions.SearingFlash, Toggle = cfg => cfg.Summoner.EnableSearingFlash };
    public static readonly AbilityBehavior EnkindleBahamut = new() { Action = SMNActions.EnkindleBahamut, Toggle = cfg => cfg.Summoner.EnableEnkindle };
    public static readonly AbilityBehavior EnkindlePhoenix = new() { Action = SMNActions.EnkindlePhoenix, Toggle = cfg => cfg.Summoner.EnableEnkindle };
    public static readonly AbilityBehavior EnkindleSolarBahamut = new() { Action = SMNActions.EnkindleSolarBahamut, Toggle = cfg => cfg.Summoner.EnableEnkindle };
    public static readonly AbilityBehavior Deathflare = new() { Action = SMNActions.Deathflare, Toggle = cfg => cfg.Summoner.EnableAstralFlow };
    public static readonly AbilityBehavior Rekindle = new() { Action = SMNActions.Rekindle, Toggle = cfg => cfg.Summoner.EnableAstralFlow };
    public static readonly AbilityBehavior Sunflare = new() { Action = SMNActions.Sunflare, Toggle = cfg => cfg.Summoner.EnableAstralFlow };

    // --- Role ---
    public static readonly AbilityBehavior Swiftcast = new() { Action = RoleActions.Swiftcast };
    public static readonly AbilityBehavior LucidDreaming = new() { Action = RoleActions.LucidDreaming, Toggle = cfg => cfg.Summoner.EnableLucidDreaming };
    public static readonly AbilityBehavior Addle = new() { Action = RoleActions.Addle, Toggle = cfg => cfg.Summoner.EnableAddle };
}
