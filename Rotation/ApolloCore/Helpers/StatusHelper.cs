using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Services;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper class for checking status effects on characters.
/// </summary>
public sealed class StatusHelper
{
    // Status IDs
    public static class StatusIds
    {
        // DoT statuses
        public const uint Aero = 143;
        public const uint AeroII = 144;
        public const uint Dia = 1871;

        // Medica regen statuses
        public const uint MedicaII = 150;
        public const uint MedicaIII = 3879;

        // Raise/Resurrection
        public const uint Raise = 148;

        // Buffs
        public const uint Swiftcast = 167;
        public const uint ThinAir = 1217;
        public const uint Freecure = 155;
        public const uint SacredSight = 3879;
        public const uint Surecast = 160;

        // Defensive cooldowns
        public const uint DivineBenison = 1218;
        public const uint Aquaveil = 2708;
        public const uint Temperance = 1872;
        public const uint DivineGrace = 3881;
        public const uint PlenaryIndulgence = 1219;

        // HoT
        public const uint Regen = 158;
    }

    /// <summary>
    /// Checks if a character has a specific status effect.
    /// </summary>
    public static bool HasStatus(IBattleChara chara, uint statusId)
    {
        // Null check for testing and defensive coding
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a character has a specific status effect and returns remaining time.
    /// </summary>
    public static bool HasStatus(IBattleChara chara, uint statusId, out float remainingTime)
    {
        remainingTime = 0f;

        // Null check for testing and defensive coding
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
            {
                remainingTime = status.RemainingTime;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the stack count for a specific status effect.
    /// </summary>
    public static int GetStatusStacks(IBattleChara chara, uint statusId)
    {
        // Null check for testing and defensive coding
        if (chara.StatusList == null)
            return 0;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }
        return 0;
    }

    /// <summary>
    /// Checks if a character has Regen active and returns remaining duration.
    /// </summary>
    public static bool HasRegenActive(IBattleChara chara, out float remainingDuration)
    {
        return HasStatus(chara, StatusIds.Regen, out remainingDuration);
    }

    /// <summary>
    /// Checks if a character has Medica II or III regen active.
    /// </summary>
    public static bool HasMedicaRegen(IBattleChara chara)
    {
        // Null check for testing and defensive coding
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == StatusIds.MedicaII || status.StatusId == StatusIds.MedicaIII)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if player has Swiftcast buff active.
    /// </summary>
    public static bool HasSwiftcast(IPlayerCharacter player) =>
        HasStatus(player, StatusIds.Swiftcast);

    /// <summary>
    /// Checks if player has Thin Air buff active.
    /// </summary>
    public static bool HasThinAir(IPlayerCharacter player) =>
        HasStatus(player, StatusIds.ThinAir);

    /// <summary>
    /// Checks if player has Freecure proc active.
    /// </summary>
    public static bool HasFreecure(IPlayerCharacter player) =>
        HasStatus(player, StatusIds.Freecure);

    /// <summary>
    /// Checks if player has Divine Grace status (enables Divine Caress).
    /// </summary>
    public static bool HasDivineGrace(IPlayerCharacter player) =>
        HasStatus(player, StatusIds.DivineGrace);

    /// <summary>
    /// Gets Sacred Sight stack count (from Presence of Mind at Lvl 92+).
    /// </summary>
    public static int GetSacredSightStacks(IPlayerCharacter player) =>
        GetStatusStacks(player, StatusIds.SacredSight);

    /// <summary>
    /// Gets the appropriate DoT status ID for the player's level.
    /// </summary>
    public static uint GetDotStatusId(byte playerLevel) =>
        playerLevel >= 72 ? StatusIds.Dia :
        playerLevel >= 46 ? StatusIds.AeroII :
        StatusIds.Aero;

    /// <summary>
    /// Gets the current Blood Lily count from the WHM job gauge (0-3).
    /// </summary>
    public static int GetBloodLilyCount() => SafeGameAccess.GetWhmBloodLilyCount();

    /// <summary>
    /// Gets the current Lily count from the WHM job gauge (0-3).
    /// </summary>
    public static int GetLilyCount() => SafeGameAccess.GetWhmLilyCount();
}
