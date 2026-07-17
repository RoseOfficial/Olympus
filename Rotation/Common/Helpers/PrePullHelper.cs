namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared constants for pre-pull hardcast timing.
/// </summary>
public static class PrePullHelper
{
    /// <summary>
    /// Slidecast buffer in seconds. A GCD resolves this many seconds before the cast bar
    /// reaches zero. The pre-pull branch fires when countdown &lt;= castTime + SlidecastBuffer.
    /// </summary>
    public const float SlidecastBuffer = 0.5f;
}
