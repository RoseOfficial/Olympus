namespace Olympus.Data;

/// <summary>
/// Game constants that don't change based on timing or server state.
/// Ranges, thresholds, and fixed values used throughout the rotation.
/// </summary>
public static class GameConstants
{
    // Healing thresholds
    /// <summary>HP percent threshold for applying Regen (don't waste on nearly full HP).</summary>
    public const float RegenHpThreshold = 0.90f;

    /// <summary>Seconds remaining before Regen should be refreshed.</summary>
    public const float RegenRefreshThreshold = 3f;

    // Cure III clustering
    /// <summary>Radius for detecting Cure III cluster targets.</summary>
    public const float CureIIIClusterRadius = 10f;

    // Job IDs
    /// <summary>Tank job IDs: PLD=19, WAR=21, DRK=32, GNB=37.</summary>
    public static readonly uint[] TankJobIds = [19, 21, 32, 37];
}
