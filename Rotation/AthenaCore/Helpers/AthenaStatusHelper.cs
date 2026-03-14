using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.AthenaCore.Helpers;

/// <summary>
/// Helper class for checking Scholar-specific status effects.
/// </summary>
public sealed class AthenaStatusHelper : BaseStatusHelper
{
    #region Buff Checks

    /// <summary>
    /// Checks if the player has Recitation active (next Aetherflow heal is free + guaranteed crit).
    /// </summary>
    public bool HasRecitation(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.RecitationStatusId);
    }

    /// <summary>
    /// Checks if the player has Emergency Tactics active (next shield becomes direct heal).
    /// </summary>
    public bool HasEmergencyTactics(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.EmergencyTacticsStatusId);
    }

    /// <summary>
    /// Checks if the player has Dissipation active (fairy dismissed, +20% GCD heal potency).
    /// </summary>
    public bool HasDissipation(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.DissipationStatusId);
    }

    /// <summary>
    /// Checks if the player has Seraphism active (level 100 fairy transformation).
    /// </summary>
    public bool HasSeraphism(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.SeraphismStatusId);
    }

    /// <summary>
    /// Checks if the player has Impact Imminent (from Chain Stratagem, enables Baneful Impaction).
    /// </summary>
    public bool HasImpactImminent(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.ImpactImminentStatusId);
    }

    /// <summary>
    /// Checks if the player has Protraction active.
    /// </summary>
    public bool HasProtraction(IPlayerCharacter player)
    {
        return HasStatus(player, SCHActions.ProtractionStatusId);
    }

    #endregion

    #region Target Shield Checks

    /// <summary>
    /// Checks if the target has Galvanize shield (from Adloquium/Succor).
    /// </summary>
    public bool HasGalvanize(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, SCHActions.GalvanizeStatusId);
    }

    /// <summary>
    /// Checks if the target has Catalyze shield (critical Adloquium bonus).
    /// </summary>
    public bool HasCatalyze(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, SCHActions.CatalyzeStatusId);
    }

    /// <summary>
    /// Checks if the target has Excogitation buff (will heal when HP drops below 50%).
    /// </summary>
    public bool HasExcogitation(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, SCHActions.ExcogitationStatusId);
    }

    /// <summary>
    /// Checks if the target has Fey Union tether active.
    /// </summary>
    public bool HasFeyUnion(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, SCHActions.FeyUnionStatusId);
    }

    /// <summary>
    /// Checks if Fey Union is currently active (player has the channeling status).
    /// The player gets status ID 1224 while channeling Fey Union.
    /// </summary>
    public bool HasFeyUnionActive(IPlayerCharacter player)
    {
        // Player gets Fey Union channeling status while active
        const ushort FeyUnionChannelStatusId = 1224;
        return HasStatus(player, FeyUnionChannelStatusId);
    }

    /// <summary>
    /// Gets the remaining duration of Galvanize on a target.
    /// </summary>
    public float GetGalvanizeDuration(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return 0f;

        return GetStatusRemaining(battleChara, SCHActions.GalvanizeStatusId);
    }

    /// <summary>
    /// Gets the remaining duration of Excogitation on a target.
    /// </summary>
    public float GetExcogitationDuration(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return 0f;

        return GetStatusRemaining(battleChara, SCHActions.ExcogitationStatusId);
    }

    #endregion

    #region Enemy Debuff Checks

    /// <summary>
    /// Checks if the target has Chain Stratagem debuff (+10% crit rate).
    /// </summary>
    public bool HasChainStratagem(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, SCHActions.ChainStratagemStatusId);
    }

    /// <summary>
    /// Checks if the target has our DoT (Bio/Bio II/Biolysis).
    /// </summary>
    public bool HasOurDot(IPlayerCharacter player, IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        // Check for any of the Bio DoT status IDs applied by us
        foreach (var status in battleChara.StatusList)
        {
            if (status.SourceId != player.EntityId)
                continue;

            if (status.StatusId is 179 or 189 or 1895) // Bio, Bio II, Biolysis
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the remaining duration of our DoT on the target.
    /// </summary>
    public float GetDotDuration(IPlayerCharacter player, IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return 0f;

        foreach (var status in battleChara.StatusList)
        {
            if (status.SourceId != player.EntityId)
                continue;

            if (status.StatusId is 179 or 189 or 1895) // Bio, Bio II, Biolysis
                return status.RemainingTime;
        }

        return 0f;
    }

    #endregion

    // Core status methods (HasStatus, GetStatusRemaining) inherited from BaseStatusHelper
    // Note: GetStatusDuration calls are replaced with GetStatusRemaining
}
