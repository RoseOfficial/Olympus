using System;
using System.Numerics;

namespace Olympus.Services.Movement.Humanization;

/// <summary>
/// Applies a low-frequency sinusoidal wobble to a direction vector. Output deflection
/// stays within [-peakDegrees, +peakDegrees]. Used to make movement look human, not
/// laser-straight from start to safe edge.
/// </summary>
public static class DirectionalNoise
{
    public static Vector2 Apply(Vector2 direction, DateTime now, ulong seed, float peakDegrees)
    {
        if (peakDegrees <= 0f)
            return direction;

        var seconds = now.Ticks / (double)TimeSpan.TicksPerSecond;
        var phaseOffset = (seed % 1000ul) / 1000.0 * (Math.PI * 2.0);
        var deflectDegrees = peakDegrees * (float)Math.Sin(seconds * Math.PI * 2.0 * 0.7 + phaseOffset);
        var deflectRadians = deflectDegrees * MathF.PI / 180f;
        var cos = MathF.Cos(deflectRadians);
        var sin = MathF.Sin(deflectRadians);
        return new Vector2(direction.X * cos - direction.Y * sin, direction.X * sin + direction.Y * cos);
    }
}
