using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Olympus.Services;

/// <summary>
/// Centralized wrapper for unsafe pointer operations to game memory.
/// Provides null-safe access with optional error tracking.
/// </summary>
public static class SafeGameAccess
{
    /// <summary>
    /// Safely gets the ActionManager instance.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Pointer to ActionManager, or null if unavailable.</returns>
    public static unsafe ActionManager* GetActionManager(IErrorMetricsService? errorMetrics = null)
    {
        try
        {
            var instance = ActionManager.Instance();
            if (instance == null)
            {
                errorMetrics?.RecordError("SafeGameAccess", "ActionManager.Instance() returned null");
            }
            return instance;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "ActionManager.Instance() threw exception");
            return null;
        }
    }

    /// <summary>
    /// Safely gets the PlayerState instance.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Pointer to PlayerState, or null if unavailable.</returns>
    public static unsafe PlayerState* GetPlayerState(IErrorMetricsService? errorMetrics = null)
    {
        try
        {
            var instance = PlayerState.Instance();
            if (instance == null)
            {
                errorMetrics?.RecordError("SafeGameAccess", "PlayerState.Instance() returned null");
            }
            return instance;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "PlayerState.Instance() threw exception");
            return null;
        }
    }

    /// <summary>
    /// Safely gets the JobGaugeManager instance.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Pointer to JobGaugeManager, or null if unavailable.</returns>
    public static unsafe JobGaugeManager* GetJobGaugeManager(IErrorMetricsService? errorMetrics = null)
    {
        try
        {
            var instance = JobGaugeManager.Instance();
            if (instance == null)
            {
                errorMetrics?.RecordError("SafeGameAccess", "JobGaugeManager.Instance() returned null");
            }
            return instance;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "JobGaugeManager.Instance() threw exception");
            return null;
        }
    }

    /// <summary>
    /// Safely gets the InventoryManager instance.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Pointer to InventoryManager, or null if unavailable.</returns>
    public static unsafe InventoryManager* GetInventoryManager(IErrorMetricsService? errorMetrics = null)
    {
        try
        {
            var instance = InventoryManager.Instance();
            if (instance == null)
            {
                errorMetrics?.RecordError("SafeGameAccess", "InventoryManager.Instance() returned null");
            }
            return instance;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "InventoryManager.Instance() threw exception");
            return null;
        }
    }

    /// <summary>
    /// Safely gets the WHM Lily count from the job gauge.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Lily count (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetWhmLilyCount(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->WhiteMage.Lily;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read WHM Lily count");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the WHM Blood Lily count from the job gauge.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Blood Lily count (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetWhmBloodLilyCount(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->WhiteMage.BloodLily;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read WHM Blood Lily count");
            return 0;
        }
    }

    /// <summary>
    /// Safely reads a player attribute by index.
    /// </summary>
    /// <param name="attributeIndex">The attribute index (e.g., 5 for Mind, 44 for Determination).</param>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>The attribute value, or 0 if unavailable.</returns>
    public static unsafe int GetPlayerAttribute(int attributeIndex, IErrorMetricsService? errorMetrics = null)
    {
        var playerState = GetPlayerState(errorMetrics);
        if (playerState == null)
            return 0;

        try
        {
            return playerState->Attributes[attributeIndex];
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", $"Failed to read player attribute {attributeIndex}");
            return 0;
        }
    }
}
