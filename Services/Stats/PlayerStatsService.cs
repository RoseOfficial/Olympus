using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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

    // BaseParam IDs from the game's BaseParam Excel sheet
    // These are used as indices into the Attributes array
    private const int AttributeMind = 5;
    private const int AttributePhysicalDamage = 12;  // Computed physical weapon damage
    private const int AttributeMagicDamage = 13;     // Computed magic weapon damage
    private const int AttributeDetermination = 44;

    public PlayerStatsService(IPluginLog log, IDataManager dataManager)
    {
        this.log = log;
        this.dataManager = dataManager;
    }

    /// <summary>
    /// Gets the player's current Mind stat.
    /// </summary>
    public unsafe int GetMind()
    {
        try
        {
            var playerState = PlayerState.Instance();
            if (playerState == null)
                return 0;

            // Attributes array is indexed by BaseParam ID
            return playerState->Attributes[AttributeMind];
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to read Mind stat");
            return 0;
        }
    }

    /// <summary>
    /// Gets the player's current Determination stat.
    /// </summary>
    public unsafe int GetDetermination()
    {
        try
        {
            var playerState = PlayerState.Instance();
            if (playerState == null)
                return 0;

            return playerState->Attributes[AttributeDetermination];
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to read Determination stat");
            return 0;
        }
    }

    /// <summary>
    /// Gets the player's weapon magic damage.
    /// Reads from PlayerState.Attributes[13] (Magic Damage) which is the synced value.
    /// Falls back to level-based estimation if not available.
    /// </summary>
    public unsafe int GetWeaponDamage(int level)
    {
        try
        {
            var playerState = PlayerState.Instance();
            if (playerState != null)
            {
                var magicDamage = playerState->Attributes[AttributeMagicDamage];
                if (magicDamage > 0)
                    return magicDamage;
            }
        }
        catch
        {
            // Fall through to estimation
        }

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
        try
        {
            var playerState = PlayerState.Instance();
            if (playerState == null)
                return "PlayerState is null";

            var mind = playerState->Attributes[AttributeMind];
            var det = playerState->Attributes[AttributeDetermination];
            var computedMagDmg = playerState->Attributes[AttributeMagicDamage];
            var computedPhysDmg = playerState->Attributes[AttributePhysicalDamage];
            var estimatedWd = EstimateWeaponDamage(level);
            var actualItemWd = GetActualItemWeaponDamage();

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
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the actual weapon damage from equipped item (unsync'd).
    /// Used for debug display only.
    /// </summary>
    private unsafe int GetActualItemWeaponDamage()
    {
        try
        {
            var inventoryManager = InventoryManager.Instance();
            if (inventoryManager == null)
                return 0;

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
            return 0;
        }
    }
}
