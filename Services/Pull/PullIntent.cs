namespace Olympus.Services.Pull;

/// <summary>
/// Player pull-intent state. Drives <c>PrePullModule</c> dispatch gating.
/// </summary>
public enum PullIntent
{
    /// <summary>Idle. No pull is happening or imminent.</summary>
    None = 0,

    /// <summary>Player intent detected, combat has not yet started.</summary>
    Imminent = 1,

    /// <summary>Combat started within the last 2 seconds (opener phase).</summary>
    Active = 2,
}
