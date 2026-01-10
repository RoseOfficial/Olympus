using System;
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
    // FFXIV has 74 player attributes (indices 0-73)
    private const int MaxAttributeIndex = 74;

    /// <summary>
    /// Generic helper for safely getting game instance pointers.
    /// Uses nint to work around C# pointer-generic limitation.
    /// </summary>
    private static unsafe nint SafeGetInstance(
        Func<nint> getInstance,
        string typeName,
        IErrorMetricsService? errorMetrics)
    {
        try
        {
            var instance = getInstance();
            if (instance == 0)
                errorMetrics?.RecordError("SafeGameAccess", $"{typeName}.Instance() returned null");
            return instance;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", $"{typeName}.Instance() threw exception");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the ActionManager instance.
    /// </summary>
    public static unsafe ActionManager* GetActionManager(IErrorMetricsService? errorMetrics = null)
        => (ActionManager*)SafeGetInstance(() => (nint)ActionManager.Instance(), "ActionManager", errorMetrics);

    /// <summary>
    /// Safely gets the PlayerState instance.
    /// </summary>
    public static unsafe PlayerState* GetPlayerState(IErrorMetricsService? errorMetrics = null)
        => (PlayerState*)SafeGetInstance(() => (nint)PlayerState.Instance(), "PlayerState", errorMetrics);

    /// <summary>
    /// Safely gets the JobGaugeManager instance.
    /// </summary>
    public static unsafe JobGaugeManager* GetJobGaugeManager(IErrorMetricsService? errorMetrics = null)
        => (JobGaugeManager*)SafeGetInstance(() => (nint)JobGaugeManager.Instance(), "JobGaugeManager", errorMetrics);

    /// <summary>
    /// Safely gets the InventoryManager instance.
    /// </summary>
    public static unsafe InventoryManager* GetInventoryManager(IErrorMetricsService? errorMetrics = null)
        => (InventoryManager*)SafeGetInstance(() => (nint)InventoryManager.Instance(), "InventoryManager", errorMetrics);

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
    /// Safely gets the Paladin Oath Gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Oath Gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetPldOathGauge(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Paladin.OathGauge;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read PLD Oath Gauge");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the current combo action ID.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Current combo action ID, or 0 if no combo active.</returns>
    public static unsafe uint GetComboAction(IErrorMetricsService? errorMetrics = null)
    {
        var actionManager = GetActionManager(errorMetrics);
        if (actionManager == null)
            return 0;

        try
        {
            return actionManager->Combo.Action;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read combo action");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the combo timer remaining.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Combo time remaining in seconds, or 0 if no combo active.</returns>
    public static unsafe float GetComboTimer(IErrorMetricsService? errorMetrics = null)
    {
        var actionManager = GetActionManager(errorMetrics);
        if (actionManager == null)
            return 0f;

        try
        {
            return actionManager->Combo.Timer;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read combo timer");
            return 0f;
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
        // Bounds check to prevent array access violations
        if (attributeIndex < 0 || attributeIndex >= MaxAttributeIndex)
        {
            errorMetrics?.RecordError("SafeGameAccess", $"Invalid attribute index {attributeIndex} (valid: 0-{MaxAttributeIndex - 1})");
            return 0;
        }

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
