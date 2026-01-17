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
    /// Safely gets the Warrior Beast Gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Beast Gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetWarBeastGauge(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Warrior.BeastGauge;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read WAR Beast Gauge");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Dark Knight Blood Gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Blood Gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetDrkBloodGauge(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->DarkKnight.Blood;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRK Blood Gauge");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Dark Knight Darkside timer in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Darkside timer in seconds, or 0 if unavailable.</returns>
    public static unsafe float GetDrkDarksideTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->DarkKnight.DarksideTimer / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRK Darkside timer");
            return 0f;
        }
    }

    /// <summary>
    /// Safely gets the Gunbreaker Cartridge count.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Cartridge count (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetGnbCartridges(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Gunbreaker.Ammo;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read GNB Cartridges");
            return 0;
        }
    }

    #region Monk Gauge

    /// <summary>
    /// Safely gets the Monk Chakra count.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Chakra count (0-5), or 0 if unavailable.</returns>
    public static unsafe int GetMnkChakra(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Monk.Chakra;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MNK Chakra");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Monk Beast Chakra array (3 elements for Masterful Blitz).
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Array of 3 Beast Chakra types (0=None, 1=Opo-opo, 2=Raptor, 3=Coeurl).</returns>
    public static unsafe byte[] GetMnkBeastChakra(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return new byte[3];

        try
        {
            var beastChakra = new byte[3];
            var gauge = jobGauge->Monk;
            beastChakra[0] = (byte)gauge.BeastChakra[0];
            beastChakra[1] = (byte)gauge.BeastChakra[1];
            beastChakra[2] = (byte)gauge.BeastChakra[2];
            return beastChakra;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MNK Beast Chakra");
            return new byte[3];
        }
    }

    /// <summary>
    /// Safely gets the Monk Nadi flags (Lunar and Solar).
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Nadi flags (bit 0 = Lunar, bit 1 = Solar).</returns>
    public static unsafe byte GetMnkNadi(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return (byte)jobGauge->Monk.Nadi;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MNK Nadi");
            return 0;
        }
    }

    /// <summary>
    /// Checks if the Monk has Lunar Nadi active.
    /// </summary>
    public static unsafe bool HasMnkLunarNadi(IErrorMetricsService? errorMetrics = null)
    {
        var nadi = GetMnkNadi(errorMetrics);
        return (nadi & 0x02) != 0; // Lunar is bit 1
    }

    /// <summary>
    /// Checks if the Monk has Solar Nadi active.
    /// </summary>
    public static unsafe bool HasMnkSolarNadi(IErrorMetricsService? errorMetrics = null)
    {
        var nadi = GetMnkNadi(errorMetrics);
        return (nadi & 0x01) != 0; // Solar is bit 0
    }

    /// <summary>
    /// Gets the count of Beast Chakra currently accumulated.
    /// </summary>
    public static unsafe int GetMnkBeastChakraCount(IErrorMetricsService? errorMetrics = null)
    {
        var beastChakra = GetMnkBeastChakra(errorMetrics);
        var count = 0;
        if (beastChakra[0] != 0) count++;
        if (beastChakra[1] != 0) count++;
        if (beastChakra[2] != 0) count++;
        return count;
    }

    #endregion

    #region Dragoon Gauge

    /// <summary>
    /// Safely gets the Dragoon Firstmind's Focus count.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Firstmind's Focus count (0-2), or 0 if unavailable.</returns>
    public static unsafe int GetDrgFirstmindsFocus(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Dragoon.FirstmindsFocusCount;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRG Firstmind's Focus");
            return 0;
        }
    }

    /// <summary>
    /// Safely checks if Life of the Dragon is active.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>True if Life of the Dragon is active, false otherwise.</returns>
    public static unsafe bool IsDrgLifeOfDragonActive(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return false;

        try
        {
            return jobGauge->Dragoon.LotdTimer > 0;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRG Life of the Dragon state");
            return false;
        }
    }

    /// <summary>
    /// Safely gets the Life of the Dragon timer in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Life of the Dragon timer in seconds, or 0 if unavailable.</returns>
    public static unsafe float GetDrgLifeOfDragonTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->Dragoon.LotdTimer / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRG Life of the Dragon timer");
            return 0f;
        }
    }

    /// <summary>
    /// Safely gets the Dragon Eye count (for Life of the Dragon activation).
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Eye count (0-2), or 0 if unavailable.</returns>
    public static unsafe int GetDrgEyeCount(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Dragoon.EyeCount;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read DRG Eye count");
            return 0;
        }
    }

    #endregion

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
