using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.AsclepiusCore.Helpers;

/// <summary>
/// Helper class for checking Sage-specific status effects on characters.
/// </summary>
public sealed class AsclepiusStatusHelper
{
    // Role action status IDs (shared with all healers)
    public static class RoleStatusIds
    {
        public const uint Swiftcast = 167;
        public const uint Surecast = 160;
        public const uint LucidDreaming = 1204;
        public const uint Raise = 148;
    }

    /// <summary>
    /// Checks if a character has a specific status effect.
    /// </summary>
    public static bool HasStatus(IBattleChara chara, uint statusId)
    {
        if (chara?.StatusList == null)
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

        if (chara?.StatusList == null)
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
    /// Checks if a character has a status effect from a specific source.
    /// </summary>
    public static bool HasStatusFromSource(IBattleChara target, uint statusId, uint sourceId)
    {
        if (target?.StatusList == null)
            return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == sourceId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the stack count for a specific status effect.
    /// </summary>
    public static int GetStatusStacks(IBattleChara chara, uint statusId)
    {
        if (chara?.StatusList == null)
            return 0;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }
        return 0;
    }

    #region Role Actions

    /// <summary>
    /// Checks if player has Swiftcast buff active.
    /// </summary>
    public static bool HasSwiftcast(IPlayerCharacter player) =>
        HasStatus(player, RoleStatusIds.Swiftcast);

    /// <summary>
    /// Checks if player has Lucid Dreaming buff active.
    /// </summary>
    public static bool HasLucidDreaming(IPlayerCharacter player) =>
        HasStatus(player, RoleStatusIds.LucidDreaming);

    /// <summary>
    /// Checks if player has Surecast buff active.
    /// </summary>
    public static bool HasSurecast(IPlayerCharacter player) =>
        HasStatus(player, RoleStatusIds.Surecast);

    #endregion

    #region Eukrasia System

    /// <summary>
    /// Checks if Eukrasia buff is active on the player.
    /// </summary>
    public static bool HasEukrasia(IPlayerCharacter player) =>
        HasStatus(player, SGEActions.EukrasiaStatusId);

    /// <summary>
    /// Checks if Zoe buff is active on the player (+50% next GCD heal).
    /// </summary>
    public static bool HasZoe(IPlayerCharacter player) =>
        HasStatus(player, SGEActions.ZoeStatusId);

    /// <summary>
    /// Gets the remaining duration of Zoe buff.
    /// </summary>
    public static float GetZoeRemaining(IPlayerCharacter player)
    {
        if (HasStatus(player, SGEActions.ZoeStatusId, out float remaining))
            return remaining;
        return 0f;
    }

    #endregion

    #region Kardia System

    /// <summary>
    /// Checks if the player has Kardia placed (player-side buff).
    /// </summary>
    public static bool HasKardia(IPlayerCharacter player) =>
        HasStatus(player, SGEActions.KardiaStatusId);

    /// <summary>
    /// Checks if a target has Kardion (receiver buff from this Sage).
    /// </summary>
    public static bool HasKardionFrom(IBattleChara target, uint sageId) =>
        HasStatusFromSource(target, SGEActions.KardionStatusId, sageId);

    /// <summary>
    /// Checks if Soteria buff is active on the player.
    /// </summary>
    public static bool HasSoteria(IPlayerCharacter player) =>
        HasStatus(player, SGEActions.SoteriaStatusId);

    /// <summary>
    /// Gets the number of Soteria stacks remaining.
    /// </summary>
    public static int GetSoteriaStacks(IPlayerCharacter player) =>
        GetStatusStacks(player, SGEActions.SoteriaStatusId);

    /// <summary>
    /// Checks if Philosophia buff is active on the player (party-wide Kardia).
    /// </summary>
    public static bool HasPhilosophia(IPlayerCharacter player) =>
        HasStatus(player, SGEActions.PhilosophiaStatusId);

    #endregion

    #region Shields

    /// <summary>
    /// Checks if target has Eukrasian Diagnosis shield.
    /// </summary>
    public static bool HasEukrasianDiagnosisShield(IBattleChara target) =>
        HasStatus(target, SGEActions.EukrasianDiagnosisStatusId);

    /// <summary>
    /// Checks if target has Eukrasian Prognosis shield.
    /// </summary>
    public static bool HasEukrasianPrognosisShield(IBattleChara target) =>
        HasStatus(target, SGEActions.EukrasianPrognosisStatusId);

    /// <summary>
    /// Checks if target has any Eukrasian shield (Diagnosis or Prognosis).
    /// </summary>
    public static bool HasAnyEukrasianShield(IBattleChara target) =>
        HasEukrasianDiagnosisShield(target) || HasEukrasianPrognosisShield(target);

    /// <summary>
    /// Checks if target has Haima buff active.
    /// </summary>
    public static bool HasHaima(IBattleChara target) =>
        HasStatus(target, SGEActions.HaimaStatusId);

    /// <summary>
    /// Gets remaining Haima stacks on target.
    /// </summary>
    public static int GetHaimaStacks(IBattleChara target) =>
        GetStatusStacks(target, SGEActions.HaimaStatusId);

    /// <summary>
    /// Checks if target has Panhaima buff active.
    /// </summary>
    public static bool HasPanhaima(IBattleChara target) =>
        HasStatus(target, SGEActions.PanhaimaStatusId);

    /// <summary>
    /// Gets remaining Panhaima stacks on target.
    /// </summary>
    public static int GetPanhaimaStacks(IBattleChara target) =>
        GetStatusStacks(target, SGEActions.PanhaimaStatusId);

    #endregion

    #region HoTs and Buffs

    /// <summary>
    /// Checks if target has Physis II HoT active.
    /// </summary>
    public static bool HasPhysisII(IBattleChara target) =>
        HasStatus(target, SGEActions.PhysisIIStatusId);

    /// <summary>
    /// Checks if target has Kerachole mitigation/HoT active.
    /// </summary>
    public static bool HasKerachole(IBattleChara target) =>
        HasStatus(target, SGEActions.KeracholeStatusId);

    /// <summary>
    /// Checks if target has Taurochole mitigation active.
    /// Note: Taurochole and Kerachole share the same mitigation buff.
    /// </summary>
    public static bool HasTaurochole(IBattleChara target) =>
        HasStatus(target, SGEActions.KeracholeStatusId);

    /// <summary>
    /// Checks if target has Holos mitigation active.
    /// </summary>
    public static bool HasHolos(IBattleChara target) =>
        HasStatus(target, SGEActions.HolosStatusId);

    /// <summary>
    /// Checks if target has Krasis buff active (increased healing received).
    /// </summary>
    public static bool HasKrasis(IBattleChara target) =>
        HasStatus(target, SGEActions.KrasisStatusId);

    #endregion

    #region DoT

    /// <summary>
    /// Checks if target has Eukrasian Dosis DoT and returns remaining duration.
    /// </summary>
    public static bool HasEukrasianDosis(IBattleChara target, out float remainingTime) =>
        HasStatus(target, SGEActions.EukrasianDosisStatusId, out remainingTime);

    /// <summary>
    /// Checks if target has any version of Eukrasian Dosis DoT.
    /// Note: All Dosis upgrades apply the same DoT status.
    /// </summary>
    public static bool HasEukrasianDosisDoT(IBattleChara target) =>
        HasStatus(target, SGEActions.EukrasianDosisStatusId);

    #endregion

    #region Utility

    /// <summary>
    /// Gets the appropriate Dosis action for the player's level.
    /// </summary>
    public static uint GetDosisForLevel(byte playerLevel) =>
        SGEActions.GetDamageGcdForLevel(playerLevel).ActionId;

    /// <summary>
    /// Gets the appropriate Eukrasian Dosis action for the player's level.
    /// </summary>
    public static uint GetEukrasianDosisForLevel(byte playerLevel) =>
        SGEActions.GetDotForLevel(playerLevel).ActionId;

    /// <summary>
    /// Gets the appropriate Phlegma action for the player's level.
    /// Returns 0 if below level 26.
    /// </summary>
    public static uint GetPhlegmaForLevel(byte playerLevel) =>
        SGEActions.GetPhlegmaForLevel(playerLevel)?.ActionId ?? 0;

    /// <summary>
    /// Gets the appropriate Toxikon action for the player's level.
    /// Returns 0 if below level 66.
    /// </summary>
    public static uint GetToxikonForLevel(byte playerLevel) =>
        SGEActions.GetToxikonForLevel(playerLevel)?.ActionId ?? 0;

    /// <summary>
    /// Gets the appropriate Dyskrasia action for the player's level.
    /// Returns 0 if below level 46.
    /// </summary>
    public static uint GetDyskrasiaForLevel(byte playerLevel) =>
        SGEActions.GetAoEDamageGcdForLevel(playerLevel)?.ActionId ?? 0;

    #endregion
}
