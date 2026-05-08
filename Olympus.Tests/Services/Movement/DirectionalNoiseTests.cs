using System;
using System.Numerics;
using Olympus.Services.Movement.Humanization;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class DirectionalNoiseTests
{
    [Fact]
    public void Apply_ZeroPeak_ReturnsInputUnchanged()
    {
        var dir = Vector2.Normalize(new Vector2(1, 0));
        var result = DirectionalNoise.Apply(dir, DateTime.UtcNow, seed: 123, peakDegrees: 0f);
        Assert.Equal(dir.X, result.X, precision: 3);
        Assert.Equal(dir.Y, result.Y, precision: 3);
    }

    [Fact]
    public void Apply_PeakDegrees_KeepsDeflectionWithinPeak()
    {
        var dir = Vector2.Normalize(new Vector2(1, 0));
        var t = DateTime.UnixEpoch.AddSeconds(2.5);
        var result = DirectionalNoise.Apply(dir, t, seed: 42, peakDegrees: 5f);
        var dotProduct = Vector2.Dot(dir, Vector2.Normalize(result));
        var deflectionRadians = MathF.Acos(MathF.Min(1f, MathF.Max(-1f, dotProduct)));
        var deflectionDegrees = deflectionRadians * 180f / MathF.PI;
        Assert.True(deflectionDegrees <= 5.01f);
    }

    [Fact]
    public void Apply_DeterministicForSameSeedAndTime()
    {
        var dir = Vector2.Normalize(new Vector2(1, 1));
        var t = DateTime.UnixEpoch.AddSeconds(10);
        var a = DirectionalNoise.Apply(dir, t, seed: 7, peakDegrees: 5f);
        var b = DirectionalNoise.Apply(dir, t, seed: 7, peakDegrees: 5f);
        Assert.Equal(a, b);
    }
}
