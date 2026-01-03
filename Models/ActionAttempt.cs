using System;

namespace Olympus.Models;

/// <summary>
/// Result of an action attempt
/// </summary>
public enum ActionResult
{
    Success,
    NoTarget,
    ActionNotReady,
    Failed,
    NotInCombat,
    OutOfRange,
    NotEnoughMp,
    OnCooldown,
    NotLearned,
    InvalidTarget,
    Unknown
}

/// <summary>
/// Represents a single action attempt for tracking and debugging
/// </summary>
public sealed class ActionAttempt
{
    public DateTime Timestamp { get; init; }
    public string SpellName { get; init; } = string.Empty;
    public uint ActionId { get; init; }
    public string? TargetName { get; init; }
    public uint? TargetHp { get; init; }
    public ActionResult Result { get; init; }
    public byte PlayerLevel { get; init; }
    public string? FailureReason { get; init; }
    public float TimeSinceLastCast { get; init; }
    public uint? StatusCode { get; init; }

    /// <summary>
    /// Maps ActionManager status codes to ActionResult.
    /// Status code 0 = ready, anything else = not ready.
    /// </summary>
    public static ActionResult StatusCodeToResult(uint statusCode) => statusCode switch
    {
        0 => ActionResult.Success,
        _ => ActionResult.ActionNotReady
    };

    /// <summary>
    /// Gets description - just shows raw status code since mappings vary by game version
    /// </summary>
    public static string StatusCodeDescription(uint statusCode) => statusCode switch
    {
        0 => "Ready",
        _ => $"Status {statusCode}"
    };
}
