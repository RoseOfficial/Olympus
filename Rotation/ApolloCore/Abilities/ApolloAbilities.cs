using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ApolloCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for White Mage abilities pushed through
/// the scheduler.
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

    public static readonly AbilityBehavior AssizeBuff = new() { Action = WHMActions.Assize };

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

    // --- Buff oGCDs ---
    public static readonly AbilityBehavior ThinAir = new() { Action = WHMActions.ThinAir };
    public static readonly AbilityBehavior PresenceOfMind = new() { Action = WHMActions.PresenceOfMind };
    public static readonly AbilityBehavior Asylum = new() { Action = WHMActions.Asylum };
    public static readonly AbilityBehavior LucidDreaming = new() { Action = RoleActions.LucidDreaming };
    public static readonly AbilityBehavior Surecast = new() { Action = RoleActions.Surecast };
    public static readonly AbilityBehavior AetherialShift = new() { Action = WHMActions.AetherialShift };

    // --- Defensive oGCDs ---
    public static readonly AbilityBehavior Temperance = new() { Action = WHMActions.Temperance };
    public static readonly AbilityBehavior DivineCaress = new() { Action = WHMActions.DivineCaress };
    public static readonly AbilityBehavior PlenaryIndulgence = new() { Action = WHMActions.PlenaryIndulgence };
    public static readonly AbilityBehavior DivineBenison = new() { Action = WHMActions.DivineBenison };
    public static readonly AbilityBehavior Aquaveil = new() { Action = WHMActions.Aquaveil };
    public static readonly AbilityBehavior LiturgyOfTheBell = new() { Action = WHMActions.LiturgyOfTheBell };

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

    // Single-target damage, AoE, and DoT actions are level-resolved at push time
    // (Stone/Glare, Holy/HolyIII, Aero/AeroII/Dia). Modules construct an inline
    // AbilityBehavior { Action = action } when pushing.
}
