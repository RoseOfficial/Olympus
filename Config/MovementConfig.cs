using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;

namespace Olympus.Config;

/// <summary>
/// Configuration for the Movement subsystem: trash AoE avoidance and auto-interact.
/// Both features default off and require deliberate opt-in.
/// </summary>
public sealed class MovementConfig
{
    // Trash AoE avoidance
    public bool EnableTrashAoEAvoidance { get; set; } = false;
    public int ReactionDelayMinMs { get; set; } = 250;
    public int ReactionDelayMaxMs { get; set; } = 700;
    public float ArrivalToleranceMinYalms { get; set; } = 0.3f;
    public float ArrivalToleranceMaxYalms { get; set; } = 0.8f;
    public int InterCastPauseMinMs { get; set; } = 200;
    public int InterCastPauseMaxMs { get; set; } = 400;
    public float DirectionalNoiseDegrees { get; set; } = 3.0f;
    public float WalkVsSprintThresholdSeconds { get; set; } = 1.5f;
    public float MaxThreatRangeYalms { get; set; } = 30.0f;
    public int RaycastBudgetPerFrame { get; set; } = 32;
    public HashSet<byte> BossRanks { get; set; } = new() { 4, 6 };
    public HashSet<uint> AvoidanceBossOverrides { get; set; } = new();

    // Auto-interact
    public bool EnableAutoInteract { get; set; } = false;
    public HashSet<ObjectKind> InteractAllowedKinds { get; set; } = new() { ObjectKind.Treasure };
    public float InteractRangeYalms { get; set; } = 3.5f;
    public float InteractCooldownSeconds { get; set; } = 1.5f;
    public bool InteractInCombat { get; set; } = true;
}
