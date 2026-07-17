namespace Olympus.Services.Targeting;

/// <summary>
/// Reads FFXIV's attack and stop sign markers from the game's MarkingController.
/// Test seam: production reads native memory; tests inject a controlled mock.
/// Slot indices mirror RSR's HeadMarker enum:
///   Attack1-5 at 0-4, Stop1-2 at 8-9, Attack6-8 at 14-16.
/// </summary>
public interface IMarkerProbe
{
    /// <summary>
    /// Returns eight object IDs in Attack1..Attack8 slot order.
    /// A value of 0 means the slot is unset.
    /// </summary>
    ulong[] GetAttackMarkTargets();

    /// <summary>
    /// Returns two object IDs for Stop1 and Stop2.
    /// A value of 0 means the slot is unset.
    /// </summary>
    ulong[] GetStopMarkTargets();
}
