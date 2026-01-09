using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Services.Scholar;

/// <summary>
/// Manages Scholar's fairy companion state.
/// Tracks whether Eos is active, Seraph is summoned, or Dissipation is active.
/// </summary>
public sealed class FairyStateManager : IFairyStateManager
{
    private readonly IObjectTable _objectTable;

    /// <summary>
    /// Seraph transformation duration in seconds.
    /// </summary>
    public const float SeraphDuration = 22f;

    /// <summary>
    /// Seraphism buff duration in seconds.
    /// </summary>
    public const float SeraphismDuration = 20f;

    /// <summary>
    /// Dissipation buff duration in seconds.
    /// </summary>
    public const float DissipationDuration = 30f;

    public FairyStateManager(IObjectTable objectTable)
    {
        _objectTable = objectTable;
    }

    /// <summary>
    /// Gets the current fairy state.
    /// </summary>
    public FairyState CurrentState
    {
        get
        {
            var player = _objectTable.LocalPlayer;
            if (player == null)
                return FairyState.None;

            // Check for Dissipation first (fairy is dismissed)
            if (HasDissipation(player))
                return FairyState.Dissipated;

            // Check for Seraphism (level 100 transformation)
            if (HasSeraphism(player))
                return FairyState.Seraphism;

            // Check if Seraph is summoned (pet ID check)
            if (IsSeraphActive())
                return FairyState.Seraph;

            // Check if Eos is summoned
            if (IsEosActive())
                return FairyState.Eos;

            return FairyState.None;
        }
    }

    /// <summary>
    /// Returns true if the fairy is available for commands.
    /// </summary>
    public bool IsFairyAvailable => CurrentState is FairyState.Eos or FairyState.Seraph or FairyState.Seraphism;

    /// <summary>
    /// Returns true if Seraph is currently active (either Summon Seraph or Seraphism).
    /// </summary>
    public bool IsSeraphOrSeraphismActive => CurrentState is FairyState.Seraph or FairyState.Seraphism;

    /// <summary>
    /// Returns true if Dissipation is active (fairy is dismissed for Aetherflow stacks).
    /// </summary>
    public bool IsDissipationActive => CurrentState == FairyState.Dissipated;

    /// <summary>
    /// Returns true if the fairy needs to be summoned.
    /// </summary>
    public bool NeedsSummon => CurrentState == FairyState.None;

    /// <summary>
    /// Returns true if we can use Seraph-related abilities.
    /// </summary>
    public bool CanUseSeraphAbilities => CurrentState is FairyState.Seraph or FairyState.Seraphism;

    /// <summary>
    /// Returns true if we can use normal fairy abilities (Eos).
    /// </summary>
    public bool CanUseEosAbilities => CurrentState == FairyState.Eos;

    /// <summary>
    /// Returns true if we should avoid using Dissipation.
    /// (e.g., during Seraph transformation)
    /// </summary>
    public bool ShouldAvoidDissipation => CurrentState is FairyState.Seraph or FairyState.Seraphism;

    /// <summary>
    /// Gets the remaining duration of the current special state.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <returns>Remaining duration in seconds, or 0 if not in special state.</returns>
    public float GetSpecialStateDuration(IPlayerCharacter player)
    {
        // Check Seraphism first
        var seraphismStatus = GetStatusDuration(player, SCHActions.SeraphismStatusId);
        if (seraphismStatus > 0)
            return seraphismStatus;

        // Check Dissipation
        var dissipationStatus = GetStatusDuration(player, SCHActions.DissipationStatusId);
        if (dissipationStatus > 0)
            return dissipationStatus;

        // Note: Seraph duration is tracked differently (via pet existence, not a status)
        return 0;
    }

    /// <summary>
    /// Checks if the player has the Dissipation buff.
    /// </summary>
    private static bool HasDissipation(IPlayerCharacter player)
    {
        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SCHActions.DissipationStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the player has the Seraphism buff.
    /// </summary>
    private static bool HasSeraphism(IPlayerCharacter player)
    {
        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SCHActions.SeraphismStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of a status effect.
    /// </summary>
    private static float GetStatusDuration(IPlayerCharacter player, ushort statusId)
    {
        foreach (var status in player.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0;
    }

    /// <summary>
    /// Checks if Eos is currently summoned.
    /// </summary>
    private bool IsEosActive()
    {
        // Look for Eos pet in object table
        // Pet names: "Eos" (old) or fairy appearance
        foreach (var obj in _objectTable)
        {
            if (obj is IBattleNpc npc && npc.OwnerId == _objectTable.LocalPlayer?.EntityId)
            {
                // Eos has specific pet IDs
                // BattleNpcSubKind.Pet for summoned pets
                if (IsFairyPet(npc))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if Seraph is currently summoned.
    /// </summary>
    private bool IsSeraphActive()
    {
        foreach (var obj in _objectTable)
        {
            if (obj is IBattleNpc npc && npc.OwnerId == _objectTable.LocalPlayer?.EntityId)
            {
                if (IsSeraphPet(npc))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a battle NPC is the Eos fairy.
    /// </summary>
    private static bool IsFairyPet(IBattleNpc npc)
    {
        // Eos/Selene pet check
        // Pet ModelCharaId or specific identifiers
        // Common approach: check if it's a summoner pet with fairy model
        var name = npc.Name?.TextValue ?? "";
        return name.Contains("Eos", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Selene", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a battle NPC is Seraph.
    /// </summary>
    private static bool IsSeraphPet(IBattleNpc npc)
    {
        var name = npc.Name?.TextValue ?? "";
        return name.Contains("Seraph", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Interface for fairy state management.
/// </summary>
public interface IFairyStateManager
{
    /// <summary>
    /// Gets the current fairy state.
    /// </summary>
    FairyState CurrentState { get; }

    /// <summary>
    /// Returns true if the fairy is available for commands.
    /// </summary>
    bool IsFairyAvailable { get; }

    /// <summary>
    /// Returns true if Seraph or Seraphism is active.
    /// </summary>
    bool IsSeraphOrSeraphismActive { get; }

    /// <summary>
    /// Returns true if Dissipation is active.
    /// </summary>
    bool IsDissipationActive { get; }

    /// <summary>
    /// Returns true if the fairy needs to be summoned.
    /// </summary>
    bool NeedsSummon { get; }

    /// <summary>
    /// Returns true if we can use Seraph-related abilities.
    /// </summary>
    bool CanUseSeraphAbilities { get; }

    /// <summary>
    /// Returns true if we can use normal fairy abilities.
    /// </summary>
    bool CanUseEosAbilities { get; }

    /// <summary>
    /// Returns true if we should avoid using Dissipation.
    /// </summary>
    bool ShouldAvoidDissipation { get; }

    /// <summary>
    /// Gets the remaining duration of the current special state.
    /// </summary>
    float GetSpecialStateDuration(IPlayerCharacter player);
}

/// <summary>
/// Represents the current state of the Scholar's fairy companion.
/// </summary>
public enum FairyState
{
    /// <summary>
    /// No fairy is summoned.
    /// </summary>
    None,

    /// <summary>
    /// Eos is summoned (normal fairy).
    /// </summary>
    Eos,

    /// <summary>
    /// Seraph is summoned (via Summon Seraph).
    /// </summary>
    Seraph,

    /// <summary>
    /// Seraphism is active (level 100 transformation).
    /// </summary>
    Seraphism,

    /// <summary>
    /// Fairy is dissipated (via Dissipation).
    /// </summary>
    Dissipated
}
