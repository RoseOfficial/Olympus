namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// A pre-pull candidate. Implementations dispatch directly (item use, scheduler push,
/// etc.) and report whether they fired.
/// </summary>
public interface IPrePullCandidate
{
    /// <summary>
    /// Attempts to dispatch this candidate. Returns true if an action fired,
    /// in which case the calling rotation should treat this frame as having
    /// spent its oGCD slot (no further oGCDs this frame).
    /// </summary>
    bool TryDispatch(uint jobId, IRotationContext context);
}
