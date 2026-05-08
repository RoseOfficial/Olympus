using System;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Decodes Lumina <c>Action</c> sheet data into an <see cref="AOEShape"/> based on cast type.
/// Covers cast types 2, 3, 4, 5, 10, 11, 12, 13. CastType 8 (charge) is intentionally returned
/// as null in v1.
/// </summary>
public static class GuessShape
{
    private const float DefaultConeHalfAngleDegrees = 30f;

    public static AOEShape? From(byte castType, float effectRange, float xAxisModifier, string omenPath, float casterHitboxRadius)
    {
        switch (castType)
        {
            case 2:
                return new AOEShapeCircle(effectRange);
            case 3:
                return new AOEShapeCone(effectRange + casterHitboxRadius, ParseConeHalfAngle(omenPath));
            case 4:
                return new AOEShapeRect(effectRange + casterHitboxRadius, xAxisModifier * 0.5f);
            case 5:
                return new AOEShapeCircle(effectRange + casterHitboxRadius);
            case 8:
                return null; // charge -- ignored in v1
            case 10:
                return new AOEShapeDonut(ParseDonutInner(omenPath), effectRange);
            case 11:
                return new AOEShapeCross(effectRange, xAxisModifier * 0.5f);
            case 12:
                return new AOEShapeRect(effectRange, xAxisModifier * 0.5f);
            case 13:
                return new AOEShapeCone(effectRange, ParseConeHalfAngle(omenPath));
            default:
                return null;
        }
    }

    /// <summary>
    /// Parses cone half-angle from omen path strings of the form ".../gl_fanXXX.avfx" where XXX
    /// is the full cone angle in degrees. Returns half-angle in radians. Falls back to 30 degrees
    /// (60 degrees / 2) if the pattern does not match.
    /// </summary>
    internal static float ParseConeHalfAngle(string omenPath)
    {
        if (string.IsNullOrEmpty(omenPath))
            return DefaultConeHalfAngleDegrees * MathF.PI / 180f;

        var idx = omenPath.IndexOf("fan", StringComparison.OrdinalIgnoreCase);
        if (idx < 0 || idx + 6 > omenPath.Length)
            return DefaultConeHalfAngleDegrees * MathF.PI / 180f;

        if (!int.TryParse(omenPath.AsSpan(idx + 3, 3), out var fullAngleDegrees))
            return DefaultConeHalfAngleDegrees * MathF.PI / 180f;

        return (fullAngleDegrees * 0.5f) * MathF.PI / 180f;
    }

    /// <summary>
    /// Parses donut inner radius from omen path strings of the form ".../gl_dntXX.avfx".
    /// Returns 0 if the pattern does not match.
    /// </summary>
    internal static float ParseDonutInner(string omenPath)
    {
        if (string.IsNullOrEmpty(omenPath))
            return 0f;

        var idx = omenPath.IndexOf("dnt", StringComparison.OrdinalIgnoreCase);
        if (idx < 0 || idx + 5 > omenPath.Length)
            return 0f;

        if (!int.TryParse(omenPath.AsSpan(idx + 3, 2), out var inner))
            return 0f;

        return inner;
    }
}
