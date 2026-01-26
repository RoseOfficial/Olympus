using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.EchidnaCore.Helpers;

/// <summary>
/// Helper class for checking Viper status effects.
/// </summary>
public sealed class EchidnaStatusHelper : BaseStatusHelper
{
    #region Primary Buffs

    /// <summary>
    /// Checks if Hunter's Instinct is active (+10% damage).
    /// </summary>
    public bool HasHuntersInstinct(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.HuntersInstinct);
    }

    /// <summary>
    /// Gets remaining duration of Hunter's Instinct.
    /// </summary>
    public float GetHuntersInstinctRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, VPRActions.StatusIds.HuntersInstinct);
    }

    /// <summary>
    /// Checks if Swiftscaled is active (-15% GCD).
    /// </summary>
    public bool HasSwiftscaled(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.Swiftscaled);
    }

    /// <summary>
    /// Gets remaining duration of Swiftscaled.
    /// </summary>
    public float GetSwiftscaledRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, VPRActions.StatusIds.Swiftscaled);
    }

    /// <summary>
    /// Checks if Reawakened state is active.
    /// </summary>
    public bool HasReawakened(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.Reawakened);
    }

    #endregion

    #region Combo Enhancement Buffs

    /// <summary>
    /// Checks if Honed Steel is active (enhances Steel Fangs).
    /// </summary>
    public bool HasHonedSteel(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.HonedSteel);
    }

    /// <summary>
    /// Checks if Honed Reavers is active (enhances Reaving Fangs).
    /// </summary>
    public bool HasHonedReavers(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.HonedReavers);
    }

    /// <summary>
    /// Checks if Ready to Reawaken proc is active (from Serpent's Ire).
    /// </summary>
    public bool HasReadyToReawaken(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.ReadyToReawaken);
    }

    #endregion

    #region Venom Buffs (Positional Tracking)

    /// <summary>
    /// Checks if Flankstung Venom is active.
    /// When active, use REAR for the bonus.
    /// </summary>
    public bool HasFlankstungVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.FlankstungVenom);
    }

    /// <summary>
    /// Checks if Hindstung Venom is active.
    /// When active, use FLANK for the bonus.
    /// </summary>
    public bool HasHindstungVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.HindstungVenom);
    }

    /// <summary>
    /// Checks if Flanksbane Venom is active.
    /// When active, use REAR for the bonus.
    /// </summary>
    public bool HasFlanksbaneVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.FlanksbaneVenom);
    }

    /// <summary>
    /// Checks if Hindsbane Venom is active.
    /// When active, use FLANK for the bonus.
    /// </summary>
    public bool HasHindsbaneVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.HindsbaneVenom);
    }

    /// <summary>
    /// Checks if Grimskin's Venom is active (AoE).
    /// </summary>
    public bool HasGrimskinsVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.GrimskinsVenom);
    }

    /// <summary>
    /// Checks if Grimhunter's Venom is active (AoE).
    /// </summary>
    public bool HasGrimhuntersVenom(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.GrimhuntersVenom);
    }

    /// <summary>
    /// Determines if any venom buff is active.
    /// </summary>
    public bool HasAnyVenom(IBattleChara player)
    {
        return HasFlankstungVenom(player) ||
               HasHindstungVenom(player) ||
               HasFlanksbaneVenom(player) ||
               HasHindsbaneVenom(player);
    }

    /// <summary>
    /// Determines if the next positional should be rear based on current venom.
    /// Flankstung and Flanksbane mean use rear next.
    /// </summary>
    public bool ShouldUseRear(IBattleChara player)
    {
        return HasFlankstungVenom(player) || HasFlanksbaneVenom(player);
    }

    /// <summary>
    /// Determines if the next positional should be flank based on current venom.
    /// Hindstung and Hindsbane mean use flank next.
    /// </summary>
    public bool ShouldUseFlank(IBattleChara player)
    {
        return HasHindstungVenom(player) || HasHindsbaneVenom(player);
    }

    #endregion

    #region oGCD Proc Detection

    /// <summary>
    /// Checks if Poised for Twinfang is active.
    /// </summary>
    public bool HasPoisedForTwinfang(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.PoisedForTwinfang);
    }

    /// <summary>
    /// Checks if Poised for Twinblood is active.
    /// </summary>
    public bool HasPoisedForTwinblood(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.PoisedForTwinblood);
    }

    #endregion

    #region Target Debuff Detection

    /// <summary>
    /// Checks if Noxious Gnash is active on the target.
    /// </summary>
    public bool HasNoxiousGnash(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == VPRActions.StatusIds.NoxiousGnash && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Noxious Gnash on target.
    /// </summary>
    public float GetNoxiousGnashRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == VPRActions.StatusIds.NoxiousGnash && status.SourceId == playerId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if True North is active.
    /// </summary>
    public bool HasTrueNorth(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.TrueNorth);
    }

    /// <summary>
    /// Checks if Bloodbath is active.
    /// </summary>
    public bool HasBloodbath(IBattleChara player)
    {
        return HasStatus(player, VPRActions.StatusIds.Bloodbath);
    }

    #endregion

    // Core status methods (HasStatus, GetStatusRemaining, GetStatusStacks) inherited from BaseStatusHelper
}
