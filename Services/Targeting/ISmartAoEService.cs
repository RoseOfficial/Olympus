using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Models;

namespace Olympus.Services.Targeting;

/// <summary>
/// Interface for smart AoE service — computes optimal facing angles for directional abilities.
/// </summary>
public interface ISmartAoEService
{
    /// <summary>
    /// Returns true if the given action is a directional (cone/line) AoE.
    /// </summary>
    bool IsDirectionalAoE(uint actionId);

    /// <summary>
    /// Finds the optimal target to face for a directional AoE ability.
    /// </summary>
    AoEResult FindBestAoETarget(uint actionId, float maxRange, IPlayerCharacter player, bool recordPrediction = true);
}
