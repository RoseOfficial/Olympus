namespace Olympus.Services.Movement.Probes;

/// <summary>
/// Wraps <c>BNpcBase.Rank</c> Lumina lookup for testability.
/// </summary>
public interface IBNpcRankProbe
{
    /// <summary>Returns the BNpcBase rank for the given DataId, or 0 if not found.</summary>
    byte GetRank(uint dataId);
}
