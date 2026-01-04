using System;
using System.Collections.Generic;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Services.Action;

namespace Olympus.Services.Debug;

/// <summary>
/// Complete debug state snapshot captured once per frame.
/// </summary>
public sealed class DebugSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public DebugStatistics Statistics { get; init; } = new();
    public DebugGcdState GcdState { get; init; } = new();
    public DebugRotationState Rotation { get; init; } = new();
    public DebugHealingState Healing { get; init; } = new();
    public DebugActionState Actions { get; init; } = new();
}

/// <summary>
/// Performance statistics for the Performance tab.
/// </summary>
public sealed class DebugStatistics
{
    // Action tracking
    public int TotalAttempts { get; init; }
    public int SuccessCount { get; init; }
    public float SuccessRate { get; init; }

    // GCD uptime
    public float GcdUptime { get; init; }
    public float AverageCastGap { get; init; }

    // Most common failure
    public string? TopFailureReason { get; init; }
    public int TopFailureCount { get; init; }

    // Downtime tracking
    public int DowntimeEventCount { get; init; }
    public DateTime LastDowntimeTime { get; init; }
    public string LastDowntimeReason { get; init; } = "";
}

/// <summary>
/// Current GCD and action system state.
/// </summary>
public sealed class DebugGcdState
{
    public GcdState State { get; init; }
    public float GcdRemaining { get; init; }
    public float AnimationLockRemaining { get; init; }
    public bool IsCasting { get; init; }
    public bool CanExecuteGcd { get; init; }
    public bool CanExecuteOgcd { get; init; }
    public int WeaveSlots { get; init; }
    public string LastActionName { get; init; } = "None";

    // Legacy debug flags from ActionTracker
    public bool DebugGcdReady { get; init; }
    public bool DebugIsActive { get; init; }
}

/// <summary>
/// Rotation planning state for the Overview and Why Stuck tabs.
/// </summary>
public sealed class DebugRotationState
{
    // Core state
    public string PlanningState { get; init; } = "Idle";
    public string PlannedAction { get; init; } = "None";
    public string DpsState { get; init; } = "Idle";
    public string TargetInfo { get; init; } = "None";

    // Resurrection
    public string RaiseState { get; init; } = "Idle";
    public string RaiseTarget { get; init; } = "None";

    // Esuna
    public string EsunaState { get; init; } = "Idle";
    public string EsunaTarget { get; init; } = "None";

    // oGCD States
    public string ThinAirState { get; init; } = "Idle";
    public string AsylumState { get; init; } = "Idle";
    public string AsylumTarget { get; init; } = "None";
    public string DefensiveState { get; init; } = "Idle";
    public string TemperanceState { get; init; } = "Idle";
    public string SurecastState { get; init; } = "Idle";

    // DPS Details
    public string AoEDpsState { get; init; } = "Idle";
    public int AoEDpsEnemyCount { get; init; }
    public string MiseryState { get; init; } = "Idle";

    // Resources
    public int LilyCount { get; init; }
    public int BloodLilyCount { get; init; }
    public string LilyStrategy { get; init; } = "Balanced";
    public int SacredSightStacks { get; init; }
}

/// <summary>
/// Healing-related debug state.
/// </summary>
public sealed class DebugHealingState
{
    // AoE healing
    public string AoEStatus { get; init; } = "Idle";
    public int AoEInjuredCount { get; init; }
    public uint AoESelectedSpell { get; init; }
    public float PlayerHpPercent { get; init; }

    // Party info
    public int PartyListCount { get; init; }
    public int PartyValidCount { get; init; }
    public int BattleNpcCount { get; init; }
    public string NpcInfo { get; init; } = "";

    // Pending heals (HP prediction)
    public List<DebugPendingHeal> PendingHeals { get; init; } = new();
    public int TotalPendingHealAmount { get; init; }

    // Last heal calculation (for debugging formula)
    public int LastHealAmount { get; init; }
    public string LastHealStats { get; init; } = "";

    // Recent heals
    public List<DebugRecentHeal> RecentHeals { get; init; } = new();
    public int TotalRecentHealAmount { get; init; }

    // Shadow HP tracking
    public List<DebugShadowHpEntry> ShadowHpEntries { get; init; } = new();
}

/// <summary>
/// A pending heal from HP prediction.
/// </summary>
public sealed class DebugPendingHeal
{
    public uint TargetId { get; init; }
    public string TargetName { get; init; } = "";
    public int Amount { get; init; }
}

/// <summary>
/// A recent heal event.
/// </summary>
public sealed class DebugRecentHeal
{
    public DateTime Timestamp { get; init; }
    public uint ActionId { get; init; }
    public string ActionName { get; init; } = "";
    public string TargetName { get; init; } = "";
    public int Amount { get; init; }
    public float SecondsAgo => (float)(DateTime.Now - Timestamp).TotalSeconds;
}

/// <summary>
/// Shadow HP tracking entry.
/// </summary>
public sealed class DebugShadowHpEntry
{
    public uint EntityId { get; init; }
    public string EntityName { get; init; } = "";
    public uint GameHp { get; init; }
    public uint ShadowHp { get; init; }
    public uint MaxHp { get; init; }
    public int Delta => (int)ShadowHp - (int)GameHp;
    public float GameHpPercent => MaxHp > 0 ? GameHp * 100f / MaxHp : 0;
    public float ShadowHpPercent => MaxHp > 0 ? ShadowHp * 100f / MaxHp : 0;
}

/// <summary>
/// Action-related debug state.
/// </summary>
public sealed class DebugActionState
{
    // History
    public IReadOnlyList<ActionAttempt> History { get; init; } = Array.Empty<ActionAttempt>();

    // Spell usage counts
    public List<DebugSpellUsage> SpellUsage { get; init; } = new();
}

/// <summary>
/// Spell usage count entry.
/// </summary>
public sealed class DebugSpellUsage
{
    public string Name { get; init; } = "";
    public uint ActionId { get; init; }
    public int Count { get; init; }
}
