using System;
using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeConeTests
{
    // Cone facing +X (rotation 0). HalfAngle 45°, radius 10.
    [Theory]
    [InlineData(0, 0, 0, 5, 0, true)]   // straight ahead, in range
    [InlineData(0, 0, 0, 11, 0, false)] // straight ahead, out of range
    [InlineData(0, 0, 0, 5, 4, true)]   // slightly off-axis, within half-angle
    [InlineData(0, 0, 0, 5, 6, false)]  // off-axis, beyond half-angle
    [InlineData(0, 0, 0, -5, 0, false)] // behind caster
    [InlineData(0, 0, 0, 0, 0, true)]   // exact origin (boundary)
    public void Contains_FacingPositiveX(float ox, float oy, float rot, float px, float py, bool expected)
    {
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), rot, new Vector2(px, py)));
    }

    [Fact]
    public void Contains_FacingPositiveY()
    {
        // rotation pi/2 = facing +Y (90° CCW from +X)
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        Assert.True(shape.Contains(new Vector2(0, 0), MathF.PI / 2f, new Vector2(0, 5)));
        Assert.False(shape.Contains(new Vector2(0, 0), MathF.PI / 2f, new Vector2(5, 0)));
    }
}
