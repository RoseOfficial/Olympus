namespace Olympus.Services.Movement.Humanization;

/// <summary>
/// Computes a deterministic-but-randomized reaction delay in milliseconds for a given cast seed.
/// The delay sits within [minMs, maxMs] and is stable for the same seed.
/// </summary>
public static class ReactionDelay
{
    public static int Compute(ulong seed, int minMs, int maxMs)
    {
        if (maxMs <= minMs)
            return minMs;
        var mixed = (seed * 6364136223846793005ul) ^ (seed >> 13);
        var range = (uint)(maxMs - minMs + 1);
        var offset = (int)(mixed % range);
        return minMs + offset;
    }
}
