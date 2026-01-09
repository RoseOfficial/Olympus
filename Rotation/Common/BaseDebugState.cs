namespace Olympus.Rotation.Common;

/// <summary>
/// Base debug state containing fields common to all healer rotations.
/// Job-specific debug states should extend this class.
/// </summary>
public abstract class BaseDebugState
{
    // General planning
    public string PlanningState { get; set; } = "Idle";
    public string PlannedAction { get; set; } = "None";

    // Party info
    public float PlayerHpPercent { get; set; }
    public int PartyListCount { get; set; }
    public int PartyValidCount { get; set; }

    // DPS
    public string DpsState { get; set; } = "Idle";
    public string DotState { get; set; } = "Idle";
    public string TargetInfo { get; set; } = "None";
    public string AoEDpsState { get; set; } = "Idle";
    public int AoEDpsEnemyCount { get; set; }

    // Healing
    public int LastHealAmount { get; set; }
    public string LastHealStats { get; set; } = "";
    public string SingleHealState { get; set; } = "Idle";
    public string AoEHealState { get; set; } = "Idle";
    public int AoEInjuredCount { get; set; }
    public uint AoESelectedSpell { get; set; }
    public string AoEStatus { get; set; } = "Idle";

    // Resurrection
    public string RaiseState { get; set; } = "Idle";
    public string RaiseTarget { get; set; } = "None";

    // Esuna
    public string EsunaState { get; set; } = "Idle";
    public string EsunaTarget { get; set; } = "None";

    // Buffs
    public string LucidState { get; set; } = "Idle";
    public string SurecastState { get; set; } = "Idle";

    // Defensive
    public string DefensiveState { get; set; } = "Idle";
}
