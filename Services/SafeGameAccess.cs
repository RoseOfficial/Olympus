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

    #region Ninja Gauge

    /// <summary>
    /// Safely gets the Ninja Ninki gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Ninki gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetNinNinki(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Ninja.Ninki;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read NIN Ninki");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Ninja Kazematoi stacks.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Kazematoi stacks (0-5), or 0 if unavailable.</returns>
    public static unsafe int GetNinKazematoi(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Ninja.Kazematoi;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read NIN Kazematoi");
            return 0;
        }
    }

    #endregion

    #region Samurai Gauge

    /// <summary>
    /// Safely gets the Samurai Kenki gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Kenki gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetSamKenki(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Samurai.Kenki;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read SAM Kenki");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Samurai Sen flags as a byte.
    /// Bit flags: Setsu=1, Getsu=2, Ka=4.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Sen flags byte, or 0 if unavailable.</returns>
    public static unsafe byte GetSamSen(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return (byte)jobGauge->Samurai.SenFlags;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read SAM Sen");
            return 0;
        }
    }

    /// <summary>
    /// Checks if the Samurai has Setsu (Snow) Sen active.
    /// </summary>
    public static unsafe bool HasSamSetsu(IErrorMetricsService? errorMetrics = null)
    {
        var sen = GetSamSen(errorMetrics);
        return (sen & 0x01) != 0; // Setsu is bit 0
    }

    /// <summary>
    /// Checks if the Samurai has Getsu (Moon) Sen active.
    /// </summary>
    public static unsafe bool HasSamGetsu(IErrorMetricsService? errorMetrics = null)
    {
        var sen = GetSamSen(errorMetrics);
        return (sen & 0x02) != 0; // Getsu is bit 1
    }

    /// <summary>
    /// Checks if the Samurai has Ka (Flower) Sen active.
    /// </summary>
    public static unsafe bool HasSamKa(IErrorMetricsService? errorMetrics = null)
    {
        var sen = GetSamSen(errorMetrics);
        return (sen & 0x04) != 0; // Ka is bit 2
    }

    /// <summary>
    /// Gets the count of active Sen (0-3).
    /// </summary>
    public static unsafe int GetSamSenCount(IErrorMetricsService? errorMetrics = null)
    {
        var sen = GetSamSen(errorMetrics);
        var count = 0;
        if ((sen & 0x01) != 0) count++; // Setsu
        if ((sen & 0x02) != 0) count++; // Getsu
        if ((sen & 0x04) != 0) count++; // Ka
        return count;
    }

    /// <summary>
    /// Safely gets the Samurai Meditation stacks.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Meditation stacks (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetSamMeditation(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Samurai.MeditationStacks;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read SAM Meditation");
            return 0;
        }
    }

    #endregion

    #region Reaper Gauge

    /// <summary>
    /// Safely gets the Reaper Soul gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Soul gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetRprSoul(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Reaper.Soul;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read RPR Soul");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Reaper Shroud gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Shroud gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetRprShroud(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Reaper.Shroud;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read RPR Shroud");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Reaper Lemure Shroud stacks during Enshroud.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Lemure Shroud stacks (0-5), or 0 if unavailable.</returns>
    public static unsafe int GetRprLemureShroud(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Reaper.LemureShroud;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read RPR Lemure Shroud");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Reaper Void Shroud stacks during Enshroud.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Void Shroud stacks (0-5), or 0 if unavailable.</returns>
    public static unsafe int GetRprVoidShroud(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Reaper.VoidShroud;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read RPR Void Shroud");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Reaper Enshroud timer remaining in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Enshroud timer in seconds, or 0 if not enshrouded.</returns>
    public static unsafe float GetRprEnshroudTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->Reaper.EnshroudedTimeRemaining / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read RPR Enshroud timer");
            return 0f;
        }
    }

    /// <summary>
    /// Checks if the Reaper is currently in Enshroud state.
    /// </summary>
    public static unsafe bool IsRprEnshrouded(IErrorMetricsService? errorMetrics = null)
    {
        return GetRprLemureShroud(errorMetrics) > 0 || GetRprEnshroudTimer(errorMetrics) > 0;
    }

    #endregion

    #region Viper Gauge

    /// <summary>
    /// Safely gets the Viper Rattling Coil stacks.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Rattling Coil stacks (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetVprRattlingCoilStacks(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Viper.RattlingCoilStacks;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read VPR Rattling Coil");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Viper Anguine Tribute stacks.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Anguine Tribute stacks (0-5), or 0 if unavailable.</returns>
    public static unsafe int GetVprAnguineTribute(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Viper.AnguineTribute;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read VPR Anguine Tribute");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Viper Serpent Offering gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Serpent Offering gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetVprSerpentOffering(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Viper.SerpentOffering;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read VPR Serpent Offering");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Viper Dread Combo state.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>DreadCombo enum value, or 0 if unavailable.</returns>
    public static unsafe byte GetVprDreadCombo(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return (byte)jobGauge->Viper.DreadCombo;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read VPR Dread Combo");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Viper Reawakened timer remaining in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Reawakened timer in seconds, or 0 if not reawakened.</returns>
    public static unsafe float GetVprReawakenedTimer(IErrorMetricsService? errorMetrics = null)
    {
        // Reawakened timer is tracked via Anguine Tribute > 0
        // The timer itself isn't directly exposed, but Anguine Tribute presence indicates active state
        var anguineTribute = GetVprAnguineTribute(errorMetrics);
        return anguineTribute > 0 ? 10f : 0f; // Approximate - Reawaken window is about 10s
    }

    /// <summary>
    /// Checks if the Viper is currently in Reawakened state.
    /// </summary>
    public static unsafe bool IsVprReawakened(IErrorMetricsService? errorMetrics = null)
    {
        return GetVprAnguineTribute(errorMetrics) > 0;
    }

    #endregion

    #region Machinist Gauge

    /// <summary>
    /// Safely gets the Machinist Heat gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Heat gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetMchHeat(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Machinist.Heat;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MCH Heat");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Machinist Battery gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Battery gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetMchBattery(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Machinist.Battery;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MCH Battery");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Machinist Overheated timer remaining in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Overheated timer in seconds, or 0 if not overheated.</returns>
    public static unsafe float GetMchOverheatTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->Machinist.OverheatTimeRemaining / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MCH Overheat timer");
            return 0f;
        }
    }

    /// <summary>
    /// Checks if the Machinist is currently in Overheated state.
    /// </summary>
    public static unsafe bool IsMchOverheated(IErrorMetricsService? errorMetrics = null)
    {
        return GetMchOverheatTimer(errorMetrics) > 0;
    }

    /// <summary>
    /// Safely gets the Machinist Automaton Queen timer remaining in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Queen timer in seconds, or 0 if Queen not active.</returns>
    public static unsafe float GetMchQueenTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->Machinist.SummonTimeRemaining / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MCH Queen timer");
            return 0f;
        }
    }

    /// <summary>
    /// Checks if the Machinist has an Automaton Queen active.
    /// </summary>
    public static unsafe bool IsMchQueenActive(IErrorMetricsService? errorMetrics = null)
    {
        return GetMchQueenTimer(errorMetrics) > 0;
    }

    /// <summary>
    /// Safely gets the Battery value that was used to summon the last Queen.
    /// Used to calculate Queen damage output.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Last Queen Battery (50-100), or 0 if no Queen was summoned.</returns>
    public static unsafe int GetMchLastQueenBattery(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Machinist.LastSummonBatteryPower;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read MCH Last Queen Battery");
            return 0;
        }
    }

    #endregion

    #region Bard Gauge

    /// <summary>
    /// Safely gets the Bard Soul Voice gauge value.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Soul Voice gauge value (0-100), or 0 if unavailable.</returns>
    public static unsafe int GetBrdSoulVoice(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Bard.SoulVoice;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read BRD Soul Voice");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Bard song timer remaining in seconds.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Song timer in seconds, or 0 if no song active.</returns>
    public static unsafe float GetBrdSongTimer(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0f;

        try
        {
            // Timer is stored in milliseconds, convert to seconds
            return jobGauge->Bard.SongTimer / 1000f;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read BRD Song timer");
            return 0f;
        }
    }

    /// <summary>
    /// Safely gets the Bard Repertoire stacks (0-4).
    /// During Wanderer's Minuet: Pitch Perfect stacks
    /// During Army's Paeon: Speed stacks
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Repertoire stacks (0-4), or 0 if unavailable.</returns>
    public static unsafe int GetBrdRepertoire(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            return jobGauge->Bard.Repertoire;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read BRD Repertoire");
            return 0;
        }
    }

    /// <summary>
    /// Safely gets the Bard current song.
    /// SongFlags lower bits: 0=None, 1=MagesBallad, 2=ArmysPaeon, 3=WanderersMinuet.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Song enum value (0=None, 1=MB, 2=AP, 3=WM), or 0 if unavailable.</returns>
    public static unsafe byte GetBrdCurrentSong(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            // Song type is stored in lower 2 bits of SongFlags
            return (byte)((byte)jobGauge->Bard.SongFlags & 0x3);
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read BRD Current Song");
            return 0;
        }
    }

    /// <summary>
    /// Gets the count of Coda available for Radiant Finale (0-3).
    /// Coda flags in SongFlags: MagesBalladCoda=16, ArmysPaeonCoda=32, WanderersMinuetCoda=64.
    /// </summary>
    /// <param name="errorMetrics">Optional error metrics service for tracking failures.</param>
    /// <returns>Coda count (0-3), or 0 if unavailable.</returns>
    public static unsafe int GetBrdCodaCount(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return 0;

        try
        {
            var count = 0;
            var flags = (byte)jobGauge->Bard.SongFlags;
            if ((flags & 16) != 0) count++; // MagesBalladCoda
            if ((flags & 32) != 0) count++; // ArmysPaeonCoda
            if ((flags & 64) != 0) count++; // WanderersMinuetCoda
            return count;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to read BRD Coda count");
            return 0;
        }
    }

    /// <summary>
    /// Checks if the Bard has Mage's Ballad Coda (flag value 16).
    /// </summary>
    public static unsafe bool GetBrdHasMBCoda(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return false;

        try
        {
            return ((byte)jobGauge->Bard.SongFlags & 16) != 0;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to check BRD MB Coda");
            return false;
        }
    }

    /// <summary>
    /// Checks if the Bard has Army's Paeon Coda (flag value 32).
    /// </summary>
    public static unsafe bool GetBrdHasAPCoda(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return false;

        try
        {
            return ((byte)jobGauge->Bard.SongFlags & 32) != 0;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to check BRD AP Coda");
            return false;
        }
    }

    /// <summary>
    /// Checks if the Bard has Wanderer's Minuet Coda (flag value 64).
    /// </summary>
    public static unsafe bool GetBrdHasWMCoda(IErrorMetricsService? errorMetrics = null)
    {
        var jobGauge = GetJobGaugeManager(errorMetrics);
        if (jobGauge == null)
            return false;

        try
        {
            return ((byte)jobGauge->Bard.SongFlags & 64) != 0;
        }
        catch
        {
            errorMetrics?.RecordError("SafeGameAccess", "Failed to check BRD WM Coda");
            return false;
        }
    }

    /// <summary>
    /// Checks if a song is currently active.
    /// </summary>
    public static unsafe bool IsBrdSongActive(IErrorMetricsService? errorMetrics = null)
    {
        return GetBrdSongTimer(errorMetrics) > 0;
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
