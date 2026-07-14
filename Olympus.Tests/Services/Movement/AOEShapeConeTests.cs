using System;
using System.Numerics;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class AOEShapeConeTests
{
    // FFXIV convention: rotation=0 means facing South (+Y in the XZ 2D plane).
    // forward = (sin r, cos r): at r=0 → (0,1) = South. Matches BossMod's WDir(Sin,Cos).
    // Cone facing South (rotation 0). HalfAngle 45°, radius 10.
    [Theory]
    [InlineData(0, 0, 0, 0, 5, true)]   // straight ahead (south), in range
    [InlineData(0, 0, 0, 0, 11, false)] // straight ahead, out of range
    [InlineData(0, 0, 0, 4, 5, true)]   // slightly off-axis, within half-angle
    [InlineData(0, 0, 0, 6, 5, false)]  // off-axis, beyond half-angle
    [InlineData(0, 0, 0, 0, -5, false)] // behind caster (north)
    [InlineData(0, 0, 0, 5, 0, false)]  // due east: perpendicular to forward, outside 45° cone
    [InlineData(0, 0, 0, 0, 0, true)]   // exact origin (boundary)
    public void Contains_FacingSouth_Rotation0(float ox, float oy, float rot, float px, float py, bool expected)
    {
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        Assert.Equal(expected, shape.Contains(new Vector2(ox, oy), rot, new Vector2(px, py)));
    }

    [Fact]
    public void Contains_FacingEast_RotationHalfPi()
    {
        // rotation pi/2 → forward = (sin(pi/2), cos(pi/2)) = (1, 0) = East
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        Assert.True(shape.Contains(new Vector2(0, 0), MathF.PI / 2f, new Vector2(5, 0)));   // due east: inside
        Assert.False(shape.Contains(new Vector2(0, 0), MathF.PI / 2f, new Vector2(0, 5)));  // due south: outside
    }

    [Fact]
    public void Contains_CasterFacingNorth_PlayerDueNorth_IsInside()
    {
        // rotation=pi → forward = (sin(pi), cos(pi)) ≈ (0, -1) = North
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        Assert.True(shape.Contains(new Vector2(0, 0), MathF.PI, new Vector2(0, -5)));   // due north: inside
        Assert.False(shape.Contains(new Vector2(0, 0), MathF.PI, new Vector2(0, 5)));   // due south: behind caster
    }

    [Fact]
    public void ContainsExpanded_ExtendsRadiusBoundary()
    {
        // Player at exactly radius from origin is on the boundary.
        // With margin 0.5 the expanded shape has radius 10.5, so a player at 10.2 is inside expanded but not base.
        var shape = new AOEShapeCone(radius: 10f, halfAngleRadians: MathF.PI / 4f);
        var player = new Vector2(0, 10.2f); // due south, just outside radius 10
        Assert.False(shape.Contains(Vector2.Zero, 0f, player));
        Assert.True(shape.ContainsExpanded(Vector2.Zero, 0f, player, 0.5f));
    }
}
