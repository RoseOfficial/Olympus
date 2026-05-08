namespace Olympus.Services.Movement.Humanization;

/// <summary>
/// Computes a deterministic-but-randomized arrival tolerance in yalms for a given cast seed.
/// </summary>
public static class ArrivalTolerance
{
    public static float Compute(ulong seed, float minYalms, float maxYalms)
    {
        if (maxYalms <= minYalms)
            return minYalms;
        var mixed = (seed * 1442695040888963407ul) ^ (seed >> 17);
        var unit = (mixed & 0xFFFFFFu) / (float)0xFFFFFFu;
        return minYalms + unit * (maxYalms - minYalms);
    }
}
