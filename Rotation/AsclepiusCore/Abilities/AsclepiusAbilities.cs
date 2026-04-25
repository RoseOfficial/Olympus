using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AsclepiusCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for Sage abilities pushed through the scheduler.
/// Eukrasia activation bypasses the scheduler (direct dispatch in
/// <c>ShieldHealingHandler</c>) because the original ExecuteOgcd-during-GCD-pass trick
/// is required to fire Eukrasia after a GCD cast, which the scheduler's queue gating cannot
/// reproduce. KardiaModule and DefensiveModule remain on legacy.
/// </summary>
public static class AsclepiusAbilities
{
    public static readonly AbilityBehavior Egeiro = new()
    {
        Action = RoleActions.Egeiro,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    public static readonly AbilityBehavior Swiftcast = new()
    {
        Action = RoleActions.Swiftcast,
        Toggle = cfg => cfg.Resurrection.EnableRaise,
    };

    public static readonly AbilityBehavior Esuna = new()
    {
        Action = RoleActions.Esuna,
        Toggle = cfg => cfg.RoleActions.EnableEsuna,
    };

    public static readonly AbilityBehavior LucidDreaming = new()
    {
        Action = RoleActions.LucidDreaming,
        Toggle = cfg => cfg.HealerShared.EnableLucidDreaming,
    };

    public static readonly AbilityBehavior Druochole = new() { Action = SGEActions.Druochole };
    public static readonly AbilityBehavior Taurochole = new() { Action = SGEActions.Taurochole };
    public static readonly AbilityBehavior Ixochole = new() { Action = SGEActions.Ixochole };
    public static readonly AbilityBehavior Kerachole = new() { Action = SGEActions.Kerachole };
    public static readonly AbilityBehavior PhysisII = new() { Action = SGEActions.PhysisII };
    public static readonly AbilityBehavior Holos = new() { Action = SGEActions.Holos };
    public static readonly AbilityBehavior Haima = new() { Action = SGEActions.Haima };
    public static readonly AbilityBehavior Panhaima = new() { Action = SGEActions.Panhaima };
    public static readonly AbilityBehavior Pepsis = new() { Action = SGEActions.Pepsis };
    public static readonly AbilityBehavior Rhizomata = new() { Action = SGEActions.Rhizomata };
    public static readonly AbilityBehavior Krasis = new() { Action = SGEActions.Krasis };
    public static readonly AbilityBehavior Zoe = new() { Action = SGEActions.Zoe };
    public static readonly AbilityBehavior Pneuma = new() { Action = SGEActions.Pneuma };
    public static readonly AbilityBehavior Prognosis = new() { Action = SGEActions.Prognosis };
    public static readonly AbilityBehavior Diagnosis = new() { Action = SGEActions.Diagnosis };
    public static readonly AbilityBehavior EukrasianPrognosis = new() { Action = SGEActions.EukrasianPrognosis };
    public static readonly AbilityBehavior EukrasianPrognosisII = new() { Action = SGEActions.EukrasianPrognosisII };
    public static readonly AbilityBehavior EukrasianDiagnosis = new() { Action = SGEActions.EukrasianDiagnosis };
}
