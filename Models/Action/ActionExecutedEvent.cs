using System;

namespace Olympus.Models.Action;

/// <summary>
/// Fired by ActionService after a successful UseAction call.
/// Consumed by the action feed overlay to visualize what Olympus just pressed.
/// </summary>
public readonly record struct ActionExecutedEvent(
    uint ActionId,
    string ActionName,
    bool IsGcd,
    DateTime TimestampUtc);
