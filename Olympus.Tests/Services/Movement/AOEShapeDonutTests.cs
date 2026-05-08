using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeDonutTests
{
    [Theory]
    [InlineData(2f, 5f, 0, 0, 0, false)]      // center, inside inner radius (safe)
    [InlineData(2f, 5f, 0, 0, 2, true)]       // exact inner edge (in)
    [InlineData(2f, 5f, 0, 0, 1.99f, false)]  // just inside inner (safe)
    [InlineData(2f, 5f, 0, 0, 4, true)]       // mid donut
    [InlineData(2f, 5f, 0, 0, 5, true)]       // outer edge
    [InlineData(2f, 5f, 0, 0, 5.01f, false)]  // outside
    public void Contains_PointMembership(float inner, float outer, float ox, float oy, float py, bool expected)
    {
        var shape = new AOEShapeDonut(inner, outer);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), 0f, new Vector2(0, py)));
    }
}
