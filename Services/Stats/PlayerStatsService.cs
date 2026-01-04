using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Services.Calculation;
using LuminaItem = Lumina.Excel.Sheets.Item;

namespace Olympus.Services.Stats;

/// <summary>
/// Reads player combat stats from game memory.
/// Uses FFXIVClientStructs to access PlayerState.
/// </summary>
public sealed class PlayerStatsService : IPlayerStatsService
{
    private readonly IPluginLog log;
    private readonly IDataManager dataManager;
    private readonly IErrorMetricsService? errorMetrics;

    // BaseParam IDs from the game's BaseParam Excel sheet
    // These are used as indices into the Attributes array
    private const int AttributeMind = 5;
    private const int AttributePhysicalDamage = 12;  // Computed physical weapon damage
    private const int AttributeMagicDamage = 13;     // Computed magic weapon damage
    private const int AttributeDetermination = 44;

    public PlayerStatsService(IPluginLog log, IDataManager dataManager, IErrorMetricsService? errorMetrics = null)
    {
        this.log = log;
        this.dataManager = dataManager;
        this.errorMetrics = errorMetrics;
    }

    /// <summary>
    /// Gets the player's current Mind stat.
    /// </summary>
    public int GetMind()
    {
        var mind = SafeGameAccess.GetPlayerAttribute(AttributeMind, errorMetrics);
        if (mind == 0)
        {
            errorMetrics?.RecordError("PlayerStatsService", "Mind stat returned 0");
        }
        return mind;
    }

    /// <summary>
    /// Gets the player's current Determination stat.
    /// </summary>
    public int GetDetermination()
    {
        var det = SafeGameAccess.GetPlayerAttribute(AttributeDetermination, errorMetrics);
        if (det == 0)
        {
            errorMetrics?.RecordError("PlayerStatsService", "Determination stat returned 0");
        }
        return det;
    }

    /// <summary>
    /// Gets the player's weapon magic damage.
    /// Reads from PlayerState.Attributes[13] (Magic Damage) which is the synced value.
    /// Falls back to level-based estimation if not available.
    /// </summary>
    public int GetWeaponDamage(int level)
    {
        var magicDamage = SafeGameAccess.GetPlayerAttribute(AttributeMagicDamage, errorMetrics);
        if (magicDamage > 0)
            return magicDamage;

        // Fall back to estimation
        errorMetrics?.RecordError("PlayerStatsService", "Magic damage unavailable, using estimation");
        return EstimateWeaponDamage(level);
    }

    /// <summary>
    /// Weapon damage estimation based on level.
    /// Values tuned slightly lower to account for level sync scaling.
    /// </summary>
    private static int EstimateWeaponDamage(int level)
    {
        return level switch
        {
            >= 100 => 141,  // ~i710
            >= 90 => 126,   // ~i630 synced
            >= 80 => 114,   // ~i530 synced
            >= 70 => 97,    // ~i400 synced
            >= 60 => 62,    // ~i270 synced
            >= 50 => 41,    // ~i130 synced
            >= 40 => 26,
            >= 30 => 17,
            >= 20 => 11,
            >= 10 => 5,
            _ => 3
        };
    }

    /// <summary>
    /// Gets all relevant stats for healing calculations.
    /// </summary>
    /// <param name="level">Player's current level (for weapon damage estimation).</param>
    public (int Mind, int Determination, int WeaponDamage) GetHealingStats(int level)
    {
        return (GetMind(), GetDetermination(), GetWeaponDamage(level));
    }

    /// <summary>
    /// Debug: Get raw attribute values to verify reading works.
    /// Shows computed magic damage from PlayerState if available.
    /// </summary>
    public unsafe string GetDebugInfo(int level)
    {
        var playerState = SafeGameAccess.GetPlayerState(errorMetrics);
        if (playerState == null)
            return "PlayerState is null";

        try
        {
            var mind = GetMind();
            var det = GetDetermination();
            var computedMagDmg = SafeGameAccess.GetPlayerAttribute(AttributeMagicDamage, errorMetrics);
            var estimatedWd = EstimateWeaponDamage(level);

            // Show computed magic damage from PlayerState (this should be synced!)
            var wdInfo = computedMagDmg > 0
                ? $"WD:{computedMagDmg}"
                : $"WD:{estimatedWd}";

            // Show calibration factor
            var factor = HealingCalculator.GetCorrectionFactor();
            return $"MND:{mind} DET:{det} {wdInfo} Cal:{factor:F2} (Lv{level})";
        }
        catch (Exception ex)
        {
            errorMetrics?.RecordError("PlayerStatsService.GetDebugInfo", ex.Message);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the actual weapon damage from equipped item (unsync'd).
    /// Used for debug display only.
    /// </summary>
    private unsafe int GetActualItemWeaponDamage()
    {
        var inventoryManager = SafeGameAccess.GetInventoryManager(errorMetrics);
        if (inventoryManager == null)
            return 0;

        try
        {
            var equippedItems = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
            if (equippedItems == null || equippedItems->Size == 0)
                return 0;

            var mainHandSlot = equippedItems->GetInventorySlot(0);
            if (mainHandSlot == null || mainHandSlot->ItemId == 0)
                return 0;

            var itemSheet = dataManager.GetExcelSheet<LuminaItem>();
            var item = itemSheet?.GetRowOrDefault(mainHandSlot->ItemId);
            if (item == null)
                return 0;

            var magicDamage = item.Value.DamageMag;
            if (magicDamage > 0)
                return magicDamage;

            return item.Value.DamagePhys;
        }
        catch
        {
            errorMetrics?.RecordError("PlayerStatsService", "Failed to read actual item weapon damage");
            return 0;
        }
    }
}
