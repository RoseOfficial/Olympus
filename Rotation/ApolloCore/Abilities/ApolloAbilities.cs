using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ApolloCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for White Mage abilities pushed through
/// the scheduler. Buff/defensive abilities remain on the legacy TryExecute path because
/// they include ground-targeted abilities (Asylum, Liturgy of the Bell) that the
/// scheduler cannot dispatch.
/// </summary>
public static class ApolloAbilities
{
    // --- Resurrection ---
    public static readonly AbilityBehavior Raise = new()
    {
        Action = RoleActions.Raise,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    public static readonly AbilityBehavior Swiftcast = new()
    {
        Action = RoleActions.Swiftcast,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    // --- Cleanse ---
    public static readonly AbilityBehavior Esuna = new()
    {
        Action = RoleActions.Esuna,
        Toggle = cfg => cfg.RoleActions.EnableEsuna,
    };

    // --- Healing oGCDs ---
    public static readonly AbilityBehavior Benediction = new()
    {
        Action = WHMActions.Benediction,
        Toggle = cfg => cfg.EnableHealing && cfg.Healing.EnableBenediction,
    };

    public static readonly AbilityBehavior Tetragrammaton = new()
    {
        Action = WHMActions.Tetragrammaton,
        Toggle = cfg => cfg.EnableHealing && cfg.Healing.EnableTetragrammaton,
    };

    public static readonly AbilityBehavior AssizeHeal = new()
    {
        Action = WHMActions.Assize,
        Toggle = cfg => cfg.EnableHealing && cfg.Healing.EnableAssizeHealing,
    };

    // --- Healing GCDs ---
    public static readonly AbilityBehavior Cure = new() { Action = WHMActions.Cure };
    public static readonly AbilityBehavior CureII = new() { Action = WHMActions.CureII };
    public static readonly AbilityBehavior CureIII = new() { Action = WHMActions.CureIII };
    public static readonly AbilityBehavior Medica = new() { Action = WHMActions.Medica };
    public static readonly AbilityBehavior MedicaII = new() { Action = WHMActions.MedicaII };
    public static readonly AbilityBehavior MedicaIII = new() { Action = WHMActions.MedicaIII };
    public static readonly AbilityBehavior AfflatusSolace = new()
    {
        Action = WHMActions.AfflatusSolace,
        Toggle = cfg => cfg.Healing.EnableAfflatusSolace,
    };
    public static readonly AbilityBehavior AfflatusRapture = new()
    {
        Action = WHMActions.AfflatusRapture,
        Toggle = cfg => cfg.Healing.EnableAfflatusRapture,
    };
    public static readonly AbilityBehavior Regen = new()
    {
        Action = WHMActions.Regen,
        Toggle = cfg => cfg.EnableHealing && cfg.Healing.EnableRegen,
    };

    // --- Damage GCDs ---
    public static readonly AbilityBehavior AfflatusMisery = new()
    {
        Action = WHMActions.AfflatusMisery,
        Toggle = cfg => cfg.EnableDamage && cfg.Damage.EnableAfflatusMisery,
    };

    public static readonly AbilityBehavior GlareIV = new()
    {
        Action = WHMActions.GlareIV,
        Toggle = cfg => cfg.EnableDamage && cfg.Damage.EnableGlareIV,
    };

    // Single-target damage and AoE damage are picked by level via GetDamageGcdForLevel /
    // GetAoEDamageGcdForLevel; the module pushes them with the resolved ActionDefinition
    // wrapped in an inline AbilityBehavior, so no static declarations needed for those.

    // DoTs: same — Aero/AeroII/Dia is selected by level, action chosen at push time.
}
