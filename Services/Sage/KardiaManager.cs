using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Services.Sage;

/// <summary>
/// Manages Sage's Kardia system.
/// Kardia places a buff on a target that heals them when the Sage deals damage.
/// Can be swapped between targets with a 5 second cooldown.
/// </summary>
public sealed class KardiaManager : IKardiaManager
{
    private readonly IPartyList _partyList;
    private DateTime _lastSwapTime = DateTime.MinValue;
    private ulong _lastKnownKardiaTarget;

    /// <summary>
    /// Cooldown between Kardia target swaps.
    /// </summary>
    public const float SwapCooldown = 5f;

    /// <summary>
    /// Kardia heal potency per damage action.
    /// </summary>
    public const int KardiaHealPotency = 170;

    /// <summary>
    /// Soteria healing boost percentage per stack.
    /// </summary>
    public const float SoteriaBoostPerStack = 0.70f;

    public KardiaManager(IPartyList partyList)
    {
        _partyList = partyList;
    }

    /// <summary>
    /// Gets the object ID of the current Kardia target.
    /// Returns 0 if no Kardia is placed.
    /// </summary>
    public ulong CurrentKardiaTarget => _lastKnownKardiaTarget;

    /// <summary>
    /// Returns true if Kardia is currently placed on a target.
    /// </summary>
    public bool HasKardia => _lastKnownKardiaTarget != 0;

    /// <summary>
    /// Returns true if Kardia swap is off cooldown.
    /// </summary>
    public bool CanSwapKardia => (DateTime.Now - _lastSwapTime).TotalSeconds >= SwapCooldown;

    /// <summary>
    /// Gets the time remaining until Kardia can be swapped.
    /// </summary>
    public float SwapCooldownRemaining
    {
        get
        {
            var elapsed = (float)(DateTime.Now - _lastSwapTime).TotalSeconds;
            return Math.Max(0f, SwapCooldown - elapsed);
        }
    }

    /// <summary>
    /// Updates the known Kardia target from status effects.
    /// Call this each frame to keep track of current Kardia placement.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public void UpdateKardiaTarget(IPlayerCharacter player)
    {
        if (player == null)
        {
            _lastKnownKardiaTarget = 0;
            return;
        }

        // Check if player has Kardia status (indicates Kardia is placed)
        bool hasKardiaStatus = false;
        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.KardiaStatusId)
            {
                hasKardiaStatus = true;
                break;
            }
        }

        if (!hasKardiaStatus)
        {
            _lastKnownKardiaTarget = 0;
            return;
        }

        // Search party members for Kardion status
        foreach (var member in _partyList)
        {
            if (member?.GameObject == null)
                continue;

            if (member.GameObject is not IBattleChara battleChara)
                continue;

            foreach (var status in battleChara.StatusList)
            {
                // Kardion status with source being our player
                if (status.StatusId == SGEActions.KardionStatusId && status.SourceId == (uint)player.GameObjectId)
                {
                    _lastKnownKardiaTarget = battleChara.GameObjectId;
                    return;
                }
            }
        }

        // If we have Kardia status but couldn't find the target, might be on a trust NPC
        // Keep the last known target in this case
    }

    /// <summary>
    /// Records that a Kardia swap was performed.
    /// </summary>
    /// <param name="newTargetId">The object ID of the new Kardia target.</param>
    public void RecordSwap(ulong newTargetId)
    {
        _lastSwapTime = DateTime.Now;
        _lastKnownKardiaTarget = newTargetId;
    }

    /// <summary>
    /// Returns true if we should swap Kardia to a new target.
    /// </summary>
    /// <param name="currentTargetHpPercent">HP percentage of current Kardia target.</param>
    /// <param name="newTargetHpPercent">HP percentage of potential new target.</param>
    /// <param name="swapThreshold">HP threshold below which to consider swapping.</param>
    public bool ShouldSwapKardia(float currentTargetHpPercent, float newTargetHpPercent, float swapThreshold)
    {
        if (!CanSwapKardia)
            return false;

        // Only swap if:
        // 1. Current target is healthy (above threshold)
        // 2. New target is low (below threshold)
        // 3. New target is significantly lower than current
        if (currentTargetHpPercent > swapThreshold && newTargetHpPercent < swapThreshold)
        {
            // Require a meaningful HP difference to avoid constant swapping
            return currentTargetHpPercent - newTargetHpPercent > 0.15f;
        }

        return false;
    }

    /// <summary>
    /// Checks if a party member has Soteria active (from this Sage).
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public bool HasSoteriaActive(IPlayerCharacter player)
    {
        if (player == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.SoteriaStatusId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of Soteria stacks remaining.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public int GetSoteriaStacks(IPlayerCharacter player)
    {
        if (player == null)
            return 0;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.SoteriaStatusId)
                return status.Param;
        }

        return 0;
    }

    /// <summary>
    /// Checks if Philosophia is active (party-wide Kardia effect).
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public bool HasPhilosophiaActive(IPlayerCharacter player)
    {
        if (player == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.PhilosophiaStatusId)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Interface for Kardia management service.
/// </summary>
public interface IKardiaManager
{
    /// <summary>
    /// Gets the object ID of the current Kardia target.
    /// </summary>
    ulong CurrentKardiaTarget { get; }

    /// <summary>
    /// Returns true if Kardia is currently placed on a target.
    /// </summary>
    bool HasKardia { get; }

    /// <summary>
    /// Returns true if Kardia swap is off cooldown.
    /// </summary>
    bool CanSwapKardia { get; }

    /// <summary>
    /// Gets the time remaining until Kardia can be swapped.
    /// </summary>
    float SwapCooldownRemaining { get; }

    /// <summary>
    /// Updates the known Kardia target from status effects.
    /// </summary>
    void UpdateKardiaTarget(IPlayerCharacter player);

    /// <summary>
    /// Records that a Kardia swap was performed.
    /// </summary>
    void RecordSwap(ulong newTargetId);

    /// <summary>
    /// Returns true if we should swap Kardia to a new target.
    /// </summary>
    bool ShouldSwapKardia(float currentTargetHpPercent, float newTargetHpPercent, float swapThreshold);

    /// <summary>
    /// Checks if Soteria is active.
    /// </summary>
    bool HasSoteriaActive(IPlayerCharacter player);

    /// <summary>
    /// Gets the number of Soteria stacks remaining.
    /// </summary>
    int GetSoteriaStacks(IPlayerCharacter player);

    /// <summary>
    /// Checks if Philosophia is active.
    /// </summary>
    bool HasPhilosophiaActive(IPlayerCharacter player);
}
