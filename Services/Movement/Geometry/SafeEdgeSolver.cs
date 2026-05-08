using System;
using System.Collections.Generic;
using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Pure geometric solver for "where should I move to clear the active threats."
/// Samples candidate points around the player and scores them by the number of threats cleared
/// minus a large penalty if the straight-line path is blocked.
/// </summary>
public static class SafeEdgeSolver
{
    public const float BlockedPenalty = -1000f;

    /// <summary>
    /// Returns <paramref name="numSamples"/> points evenly distributed on a circle of
    /// <paramref name="radius"/> around <paramref name="player"/>.
    /// </summary>
    public static IEnumerable<Vector2> SampleCandidates(Vector2 player, int numSamples, float radius)
    {
        for (var i = 0; i < numSamples; i++)
        {
            var angle = (i / (float)numSamples) * MathF.PI * 2f;
            yield return player + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
    }

    /// <summary>
    /// Scores a candidate position. +1 per threat cleared, large negative if path is blocked.
    /// </summary>
    public static float Score(Vector2 candidate, IReadOnlyList<TrackedAOE> threats, Func<Vector2, bool> isBlocked)
    {
        if (isBlocked(candidate))
            return BlockedPenalty;

        var cleared = 0;
        for (var i = 0; i < threats.Count; i++)
        {
            var t = threats[i];
            if (!t.Shape.Contains(t.Origin, t.RotationRadians, candidate))
                cleared++;
        }
        return cleared;
    }
}
