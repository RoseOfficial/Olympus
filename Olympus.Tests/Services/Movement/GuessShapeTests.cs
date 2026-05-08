using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class GuessShapeTests
{
    private const float HitboxRadius = 0.5f;

    [Fact]
    public void CastType2_PointBlankCircle()
    {
        var shape = GuessShape.From(castType: 2, effectRange: 8f, xAxisModifier: 0f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeCircle>(shape);
        Assert.Equal(8f, ((AOEShapeCircle)shape!).Radius);
    }

    [Fact]
    public void CastType3_ConeWithFanOmen()
    {
        var shape = GuessShape.From(castType: 3, effectRange: 15f, xAxisModifier: 0f, omenPath: "vfx/omen/eff/general/gl_fan090.avfx", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeCone>(shape);
        var cone = (AOEShapeCone)shape!;
        Assert.Equal(15f + HitboxRadius, cone.Radius, precision: 3);
        Assert.Equal(System.MathF.PI / 4f, cone.HalfAngleRadians, precision: 3);
    }

    [Fact]
    public void CastType3_FallbackOmenAngle()
    {
        var shape = GuessShape.From(castType: 3, effectRange: 15f, xAxisModifier: 0f, omenPath: "no_match", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeCone>(shape);
        var cone = (AOEShapeCone)shape!;
        Assert.Equal(System.MathF.PI / 6f, cone.HalfAngleRadians, precision: 3);
    }

    [Fact]
    public void CastType4_LineRect()
    {
        var shape = GuessShape.From(castType: 4, effectRange: 20f, xAxisModifier: 4f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeRect>(shape);
        var rect = (AOEShapeRect)shape!;
        Assert.Equal(20f + HitboxRadius, rect.Length, precision: 3);
        Assert.Equal(2f, rect.HalfWidth, precision: 3);
    }

    [Fact]
    public void CastType5_CircleWithHitbox()
    {
        var shape = GuessShape.From(castType: 5, effectRange: 10f, xAxisModifier: 0f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.Equal(10f + HitboxRadius, ((AOEShapeCircle)shape!).Radius, precision: 3);
    }

    [Fact]
    public void CastType8_ChargeIgnored()
    {
        var shape = GuessShape.From(castType: 8, effectRange: 25f, xAxisModifier: 4f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.Null(shape);
    }

    [Fact]
    public void CastType10_DonutWithInnerFromOmen()
    {
        var shape = GuessShape.From(castType: 10, effectRange: 20f, xAxisModifier: 0f, omenPath: "vfx/omen/eff/general/gl_dnt05.avfx", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeDonut>(shape);
        var donut = (AOEShapeDonut)shape!;
        Assert.Equal(5f, donut.InnerRadius, precision: 1);
        Assert.Equal(20f, donut.OuterRadius, precision: 1);
    }

    [Fact]
    public void CastType11_Cross()
    {
        var shape = GuessShape.From(castType: 11, effectRange: 15f, xAxisModifier: 4f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeCross>(shape);
    }

    [Fact]
    public void CastType12_LineNoHitbox()
    {
        var shape = GuessShape.From(castType: 12, effectRange: 20f, xAxisModifier: 4f, omenPath: "", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeRect>(shape);
        Assert.Equal(20f, ((AOEShapeRect)shape!).Length, precision: 3);
    }

    [Fact]
    public void CastType13_ConeNoHitbox()
    {
        var shape = GuessShape.From(castType: 13, effectRange: 15f, xAxisModifier: 0f, omenPath: "vfx/omen/eff/general/gl_fan060.avfx", casterHitboxRadius: HitboxRadius);
        Assert.IsType<AOEShapeCone>(shape);
        Assert.Equal(15f, ((AOEShapeCone)shape!).Radius, precision: 3);
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)6)]
    [InlineData((byte)7)]
    [InlineData((byte)99)]
    public void UnknownCastType_ReturnsNull(byte castType)
    {
        var shape = GuessShape.From(castType, 10f, 4f, "", HitboxRadius);
        Assert.Null(shape);
    }
}
