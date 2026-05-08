using System;
using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeRectTests
{
    [Theory]
    [InlineData(0, 0, 0, 5, 0, true)]    // center of rect
    [InlineData(0, 0, 0, 5, 2, true)]    // edge of width
    [InlineData(0, 0, 0, 5, 2.01f, false)]
    [InlineData(0, 0, 0, 10, 0, true)]   // far end
    [InlineData(0, 0, 0, 10.01f, 0, false)]
    [InlineData(0, 0, 0, -1, 0, false)]  // behind origin
    public void Contains_FacingPositiveX(float ox, float oy, float rot, float px, float py, bool expected)
    {
        var shape = new AOEShapeRect(length: 10f, halfWidth: 2f);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), rot, new Vector2(px, py)));
    }
}
