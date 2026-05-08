using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeCircleTests
{
    [Theory]
    [InlineData(5f, 0f, 0f, 0f, 0f, true)]   // origin = test point
    [InlineData(5f, 0f, 0f, 4f, 0f, true)]   // inside
    [InlineData(5f, 0f, 0f, 5f, 0f, true)]   // exact boundary
    [InlineData(5f, 0f, 0f, 5.01f, 0f, false)] // just outside
    [InlineData(5f, 0f, 0f, 0f, 5f, true)]   // boundary on Y axis
    [InlineData(5f, 10f, 10f, 13f, 14f, true)] // inside, offset origin
    [InlineData(5f, 10f, 10f, 16f, 14f, false)] // outside, offset origin
    public void Contains_PointMembership(float radius, float ox, float oy, float px, float py, bool expected)
    {
        var shape = new AOEShapeCircle(radius);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), 0f, new Vector2(px, py)));
    }
}
