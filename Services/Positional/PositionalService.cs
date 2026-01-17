using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Positional;

/// <summary>
/// Service for determining player position relative to target.
/// Uses FFXIV's coordinate system to calculate rear/flank/front positions.
/// </summary>
/// <remarks>
/// FFXIV Positional Geometry:
/// - Front: 315-45 degrees (90 degree cone)
/// - Right Flank: 45-135 degrees (90 degree cone)
/// - Rear: 135-225 degrees (90 degree cone)
/// - Left Flank: 225-315 degrees (90 degree cone)
///
/// The target's rotation gives their facing direction.
/// We calculate the angle from target to player, then determine which cone the player is in.
/// </remarks>
public sealed class PositionalService : IPositionalService
{
    // Degree thresholds for positional cones (each cone is 90 degrees)
    private const float FrontStartDegrees = 315f;  // Front spans 315-45 (wrapping through 0)
    private const float FrontEndDegrees = 45f;
    private const float FlankRightEndDegrees = 135f;
    private const float RearEndDegrees = 225f;
    private const float FlankLeftEndDegrees = 315f;

    /// <inheritdoc />
    public PositionalType GetPositional(IBattleChara player, IBattleChara target)
    {
        var relativeAngle = CalculateRelativeAngle(player, target);

        // Rear: 135-225 degrees
        if (relativeAngle >= 135f && relativeAngle < 225f)
            return PositionalType.Rear;

        // Flank: 45-135 or 225-315 degrees
        if ((relativeAngle >= 45f && relativeAngle < 135f) ||
            (relativeAngle >= 225f && relativeAngle < 315f))
            return PositionalType.Flank;

        // Front: 0-45 or 315-360 degrees
        return PositionalType.Front;
    }

    /// <inheritdoc />
    public bool IsAtRear(IBattleChara player, IBattleChara target)
    {
        var relativeAngle = CalculateRelativeAngle(player, target);
        return relativeAngle >= 135f && relativeAngle < 225f;
    }

    /// <inheritdoc />
    public bool IsAtFlank(IBattleChara player, IBattleChara target)
    {
        var relativeAngle = CalculateRelativeAngle(player, target);
        return (relativeAngle >= 45f && relativeAngle < 135f) ||
               (relativeAngle >= 225f && relativeAngle < 315f);
    }

    /// <inheritdoc />
    public bool IsAtFront(IBattleChara player, IBattleChara target)
    {
        var relativeAngle = CalculateRelativeAngle(player, target);
        return relativeAngle < 45f || relativeAngle >= 315f;
    }

    /// <inheritdoc />
    public bool HasPositionalImmunity(IBattleChara target)
    {
        // Enemies with circular hitbox indicators don't have positional requirements
        // This is typically indicated by the enemy's model type or specific status flags
        // For now, we check if the target is casting (many positional-immune enemies are)
        // or if it's a specific type of enemy

        // BattleNpc check - some NPCs like striking dummies or circular enemies have no rear
        if (target is IBattleNpc npc)
        {
            // Check if the NPC has the "no positional" flag
            // This is a simplification - in practice, you'd check the enemy's model data
            // Most ring-type enemies and training dummies have positional immunity

            // Training dummies (SubKind 2) typically have no positionals
            // Actual implementation would need more sophisticated checks
            return npc.SubKind == 2; // Training dummy subkind
        }

        return false;
    }

    /// <summary>
    /// Calculates the relative angle from target to player in degrees (0-360).
    /// 0/360 = directly in front of target
    /// 90 = right flank
    /// 180 = directly behind (rear)
    /// 270 = left flank
    /// </summary>
    private static float CalculateRelativeAngle(IBattleChara player, IBattleChara target)
    {
        // Get positions
        var playerPos = player.Position;
        var targetPos = target.Position;

        // Calculate direction vector from target to player
        var dx = playerPos.X - targetPos.X;
        var dz = playerPos.Z - targetPos.Z;

        // Calculate angle from target to player in radians
        // atan2 returns angle in radians: -PI to +PI
        var angleToPlayer = (float)Math.Atan2(dx, dz);

        // Get target's facing direction (rotation is in radians)
        var targetFacing = target.Rotation;

        // Calculate relative angle (how far around the target the player is from the front)
        // This gives us the angle from the target's perspective
        var relativeAngle = angleToPlayer - targetFacing;

        // Normalize to 0-360 degrees
        var degrees = relativeAngle * (180f / (float)Math.PI);
        degrees = (degrees + 360f) % 360f;

        return degrees;
    }
}
