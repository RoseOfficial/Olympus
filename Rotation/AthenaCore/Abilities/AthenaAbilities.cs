using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AthenaCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for Scholar abilities pushed through the scheduler.
/// </summary>
public static class AthenaAbilities
{
    public static readonly AbilityBehavior Resurrection = new()
    {
        Action = RoleActions.Resurrection,
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

    public static readonly AbilityBehavior Recitation = new() { Action = SCHActions.Recitation };
    public static readonly AbilityBehavior Excogitation = new() { Action = SCHActions.Excogitation };
    public static readonly AbilityBehavior Lustrate = new() { Action = SCHActions.Lustrate };
    public static readonly AbilityBehavior Indomitability = new() { Action = SCHActions.Indomitability };
    public static readonly AbilityBehavior SacredSoil = new() { Action = SCHActions.SacredSoil };
    public static readonly AbilityBehavior Protraction = new() { Action = SCHActions.Protraction };
    public static readonly AbilityBehavior EmergencyTactics = new() { Action = SCHActions.EmergencyTactics };
    public static readonly AbilityBehavior Succor = new() { Action = SCHActions.Succor };
    public static readonly AbilityBehavior Concitation = new() { Action = SCHActions.Concitation };
    public static readonly AbilityBehavior Adloquium = new() { Action = SCHActions.Adloquium };
    public static readonly AbilityBehavior Manifestation = new() { Action = SCHActions.Manifestation };
    public static readonly AbilityBehavior Physick = new() { Action = SCHActions.Physick };

    // --- Buffs ---
    public static readonly AbilityBehavior LucidDreaming = new() { Action = RoleActions.LucidDreaming };
    public static readonly AbilityBehavior Dissipation = new() { Action = SCHActions.Dissipation };

    // --- Defensive ---
    public static readonly AbilityBehavior Expedient = new() { Action = SCHActions.Expedient };
    public static readonly AbilityBehavior DeploymentTactics = new() { Action = SCHActions.DeploymentTactics };
}
