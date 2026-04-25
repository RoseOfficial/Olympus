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

    // --- Damage oGCDs ---
    public static readonly AbilityBehavior ChainStratagem = new() { Action = SCHActions.ChainStratagem };
    public static readonly AbilityBehavior BanefulImpaction = new() { Action = SCHActions.BanefulImpaction };
    public static readonly AbilityBehavior EnergyDrain = new() { Action = SCHActions.EnergyDrain };
    public static readonly AbilityBehavior Aetherflow = new() { Action = SCHActions.Aetherflow };

    // --- Damage GCDs ---
    public static readonly AbilityBehavior RuinII = new() { Action = SCHActions.RuinII };

    // --- Fairy ---
    public static readonly AbilityBehavior SummonEos = new() { Action = SCHActions.SummonEos };
    public static readonly AbilityBehavior Seraphism = new() { Action = SCHActions.Seraphism };
    public static readonly AbilityBehavior SummonSeraph = new() { Action = SCHActions.SummonSeraph };
    public static readonly AbilityBehavior Consolation = new() { Action = SCHActions.Consolation };
    public static readonly AbilityBehavior FeyUnion = new() { Action = SCHActions.FeyUnion };
    public static readonly AbilityBehavior FeyBlessing = new() { Action = SCHActions.FeyBlessing };
    public static readonly AbilityBehavior WhisperingDawn = new() { Action = SCHActions.WhisperingDawn };
    public static readonly AbilityBehavior FeyIllumination = new() { Action = SCHActions.FeyIllumination };
}
