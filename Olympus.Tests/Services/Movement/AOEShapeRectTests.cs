using System;
using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeRectTests
{
    // FFXIV convention: rotation=0 means facing South (+Y in the XZ 2D plane).
    // forward = (sin r, cos r): at r=0 → (0,1) = South.
    // Rect facing South (rotation 0), length 10, halfWidth 2.
    [Theory]
    [InlineData(0, 0, 0, 0, 5, true)]      // center of rect (due south)
    [InlineData(0, 0, 0, 2, 5, true)]      // edge of width
    [InlineData(0, 0, 0, 2.01f, 5, false)] // just past edge of width
    [InlineData(0, 0, 0, 0, 10, true)]     // far end
    [InlineData(0, 0, 0, 0, 10.01f, false)] // beyond far end
    [InlineData(0, 0, 0, 0, -1, false)]    // behind origin (north)
    [InlineData(0, 0, 0, 5, 0, false)]     // due east: orthogonal, not in rect facing south
    public void Contains_FacingSouth_Rotation0(float ox, float oy, float rot, float px, float py, bool expected)
    {
        var shape = new AOEShapeRect(length: 10f, halfWidth: 2f);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), rot, new Vector2(px, py)));
    }

    [Fact]
    public void Contains_FacingEast_RotationHalfPi()
    {
        // rotation pi/2 → forward = (1, 0) = East
        var shape = new AOEShapeRect(length: 10f, halfWidth: 2f);
        Assert.True(shape.Contains(Vector2.Zero, MathF.PI / 2f, new Vector2(5, 0)));   // center of east-facing rect
        Assert.False(shape.Contains(Vector2.Zero, MathF.PI / 2f, new Vector2(0, 5)));  // south: orthogonal, outside
    }

    [Fact]
    public void ContainsExpanded_ExtendsAllBoundaries()
    {
        // Player at (0, 10.3): just beyond length 10; base returns false, expanded by 0.5 returns true.
        var shape = new AOEShapeRect(length: 10f, halfWidth: 2f);
        var beyond = new Vector2(0, 10.3f);
        Assert.False(shape.Contains(Vector2.Zero, 0f, beyond));
        Assert.True(shape.ContainsExpanded(Vector2.Zero, 0f, beyond, 0.5f));

        // Player at (2.3, 5): just past halfWidth 2; expanded by 0.5 returns true.
        var side = new Vector2(2.3f, 5f);
        Assert.False(shape.Contains(Vector2.Zero, 0f, side));
        Assert.True(shape.ContainsExpanded(Vector2.Zero, 0f, side, 0.5f));
    }
}
