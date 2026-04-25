using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AstraeaCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for Astrologian abilities pushed through
/// the scheduler. EarthlyStarPlacement (ground-targeted), LadyOfCrowns (priority 60),
/// and HoroscopePreparation (priority 65) remain on legacy TryExecute because the
/// scheduler dispatches before legacy fallback — keeping them legacy preserves the
/// original priority ordering relative to EarthlyStarPlacement which can't be migrated.
/// </summary>
public static class AstraeaAbilities
{
    public static readonly AbilityBehavior Ascend = new()
    {
        Action = RoleActions.Ascend,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    public static readonly AbilityBehavior Swiftcast = new()
    {
        Action = RoleActions.Swiftcast,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    public static readonly AbilityBehavior Lightspeed = new() { Action = ASTActions.Lightspeed };

    public static readonly AbilityBehavior Esuna = new()
    {
        Action = RoleActions.Esuna,
        Toggle = cfg => cfg.RoleActions.EnableEsuna,
    };

    public static readonly AbilityBehavior CelestialIntersection = new() { Action = ASTActions.CelestialIntersection };
    public static readonly AbilityBehavior AspectedBenefic = new() { Action = ASTActions.AspectedBenefic };
    public static readonly AbilityBehavior EssentialDignity = new() { Action = ASTActions.EssentialDignity };
    public static readonly AbilityBehavior CelestialOpposition = new() { Action = ASTActions.CelestialOpposition };
    public static readonly AbilityBehavior Exaltation = new() { Action = ASTActions.Exaltation };
    public static readonly AbilityBehavior HoroscopeEnd = new() { Action = ASTActions.HoroscopeEnd };
    public static readonly AbilityBehavior Microcosmos = new() { Action = ASTActions.Microcosmos };
    public static readonly AbilityBehavior StellarDetonation = new() { Action = ASTActions.StellarDetonation };
    public static readonly AbilityBehavior Synastry = new() { Action = ASTActions.Synastry };
    public static readonly AbilityBehavior EarthlyStar = new() { Action = ASTActions.EarthlyStar };
    public static readonly AbilityBehavior LadyOfCrowns = new() { Action = ASTActions.LadyOfCrowns };
    public static readonly AbilityBehavior Horoscope = new() { Action = ASTActions.Horoscope };
    public static readonly AbilityBehavior Macrocosmos = new() { Action = ASTActions.Macrocosmos };
    public static readonly AbilityBehavior Helios = new() { Action = ASTActions.Helios };
    public static readonly AbilityBehavior AspectedHelios = new() { Action = ASTActions.AspectedHelios };
    public static readonly AbilityBehavior HeliosConjunction = new() { Action = ASTActions.HeliosConjunction };
    public static readonly AbilityBehavior Benefic = new() { Action = ASTActions.Benefic };
    public static readonly AbilityBehavior BeneficII = new() { Action = ASTActions.BeneficII };
}
