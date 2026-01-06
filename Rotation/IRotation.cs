using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Rotation.ApolloCore.Context;

namespace Olympus.Rotation;

/// <summary>
/// Base interface for all rotation implementations.
/// Each job module (Apollo/WHM, Athena/SCH, etc.) implements this interface.
/// </summary>
public interface IRotation
{
    /// <summary>
    /// Display name for this rotation (e.g., "Apollo" for WHM).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Job IDs supported by this rotation.
    /// For example, Apollo supports both WhiteMage (24) and Conjurer (6).
    /// </summary>
    uint[] SupportedJobIds { get; }

    /// <summary>
    /// Main execution loop - called every frame when the rotation is active.
    /// </summary>
    /// <param name="player">The local player character.</param>
    void Execute(IPlayerCharacter player);

    /// <summary>
    /// Debug state for this rotation, used by the debug window.
    /// </summary>
    DebugState DebugState { get; }
}
