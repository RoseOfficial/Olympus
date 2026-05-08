using System;
using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeCrossTests
{
    [Theory]
    [InlineData(0, 0, 0, true)]        // center
    [InlineData(5, 0, 0, true)]        // along +X arm
    [InlineData(-5, 0, 0, true)]       // along -X arm
    [InlineData(0, 5, 0, true)]        // along +Y arm
    [InlineData(0, -5, 0, true)]       // along -Y arm
    [InlineData(2, 2, 0, false)]       // diagonal, outside both arms
    [InlineData(11, 0, 0, false)]      // beyond +X arm
    [InlineData(5, 1, 0, true)]        // +X arm, edge of width
    [InlineData(5, 1.01f, 0, false)]   // +X arm, just outside width
    public void Contains_PointMembership(float px, float py, float rot, bool expected)
    {
        var shape = new AOEShapeCross(length: 10f, halfWidth: 1f);
        Assert.Equal(expected, shape.Contains(new Vector2(0, 0), rot, new Vector2(px, py)));
    }
}
