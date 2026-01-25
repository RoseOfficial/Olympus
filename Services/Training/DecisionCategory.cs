namespace Olympus.Services.Training;

/// <summary>
/// Standard category strings for training decision explanations.
/// Provides consistent category naming across all role helpers.
/// </summary>
public static class DecisionCategory
{
    // Universal categories
    public const string Damage = "Damage";
    public const string BurstWindow = "Burst Window";
    public const string ResourceManagement = "Resource Management";
    public const string Utility = "Utility";
    public const string RaidBuff = "Raid Buff";
    public const string Interrupt = "Interrupt";

    // Healer categories
    public const string Healing = "Healing";
    public const string Defensive = "Defensive";
    public const string Buff = "Buff";

    // Tank categories
    public const string Mitigation = "Mitigation";
    public const string Invulnerability = "Invulnerability";
    public const string PartyMitigation = "Party Mitigation";
    public const string Enmity = "Enmity";

    // DoT category
    public const string DotManagement = "DoT Management";

    // Movement category
    public const string MovementOptimization = "Movement Optimization";

    // Dynamic category generators

    /// <summary>Positional category with hit/miss status.</summary>
    public static string Positional(bool hit) => hit ? "Positional (Hit)" : "Positional (Missed)";

    /// <summary>Combo step category.</summary>
    public static string Combo(int step) => $"Combo Step {step}";

    /// <summary>Melee combo step category (for casters like RDM).</summary>
    public static string MeleeCombo(int step) => $"Melee Combo Step {step}";

    /// <summary>Proc usage category.</summary>
    public static string Proc(string name) => $"Proc ({name})";

    /// <summary>Song/dance category.</summary>
    public static string Song(string name) => $"Song ({name})";

    /// <summary>Phase transition category.</summary>
    public static string Phase(string current, string next) => $"Phase ({current} → {next})";

    /// <summary>Summon/pet category.</summary>
    public static string Summon(string name) => $"Summon ({name})";

    /// <summary>AoE category with target count.</summary>
    public static string AoE(int count) => $"AoE ({count} targets)";
}
