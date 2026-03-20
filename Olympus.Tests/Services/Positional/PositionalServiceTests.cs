using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Services.Positional;

namespace Olympus.Tests.Services.Positional;

/// <summary>
/// Tests for PositionalService geometry calculations.
/// Verifies rear/flank/front detection relative to a target's facing direction.
///
/// FFXIV Positional Geometry (degrees from target's perspective):
/// - Front: 315-45 (90 degree cone, wrapping through 0)
/// - Right Flank: 45-135 (90 degree cone)
/// - Rear: 135-225 (90 degree cone)
/// - Left Flank: 225-315 (90 degree cone)
/// </summary>
public class PositionalServiceTests
{
    private readonly PositionalService _service = new();

    /// <summary>
    /// Creates a mock IBattleChara at a given position with a given facing rotation.
    /// </summary>
    private static Mock<IBattleChara> CreateActor(Vector3 position, float rotation = 0f)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.Position).Returns(position);
        mock.Setup(x => x.Rotation).Returns(rotation);
        return mock;
    }

    /// <summary>
    /// Creates a mock IBattleNpc for positional immunity tests.
    /// </summary>
    private static Mock<IBattleNpc> CreateNpc(byte subKind = 0)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        mock.Setup(x => x.Rotation).Returns(0f);
        mock.Setup(x => x.SubKind).Returns(subKind);
        return mock;
    }

    #region Player Directly Behind Target (180 degrees - Rear)

    [Fact]
    public void GetPositional_PlayerDirectlyBehindTarget_ReturnsRear()
    {
        // Target at origin facing north (+Z), player behind at -Z
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var player = CreateActor(new Vector3(0, 0, -5));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Rear, result);
    }

    [Fact]
    public void IsAtRear_PlayerDirectlyBehindTarget_ReturnsTrue()
    {
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var player = CreateActor(new Vector3(0, 0, -5));

        Assert.True(_service.IsAtRear(player.Object, target.Object));
        Assert.False(_service.IsAtFlank(player.Object, target.Object));
        Assert.False(_service.IsAtFront(player.Object, target.Object));
    }

    #endregion

    #region Rear/Flank Boundaries (135 and 225 degrees)

    [Fact]
    public void GetPositional_PlayerAtRearFlankBoundary135_ReturnsRear()
    {
        // At exactly 135 degrees (start of rear cone)
        // Target at origin facing +Z (rotation = 0)
        // 135 degrees = rear starts
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 135f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Rear, result);
    }

    [Fact]
    public void GetPositional_PlayerAtRearFlankBoundary225_IsNotRear()
    {
        // At exactly 225 degrees (end of rear cone, start of left flank)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 225f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        // 225 is the start of left flank, not rear
        Assert.Equal(PositionalType.Flank, result);
    }

    #endregion

    #region Flank Positions (90 and 270 degrees)

    [Fact]
    public void GetPositional_PlayerAtRightFlank90_ReturnsFlank()
    {
        // 90 degrees = right side
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 90f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Flank, result);
    }

    [Fact]
    public void IsAtFlank_PlayerAtRightFlank90_ReturnsTrue()
    {
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 90f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        Assert.True(_service.IsAtFlank(player.Object, target.Object));
        Assert.False(_service.IsAtRear(player.Object, target.Object));
        Assert.False(_service.IsAtFront(player.Object, target.Object));
    }

    [Fact]
    public void GetPositional_PlayerAtLeftFlank270_ReturnsFlank()
    {
        // 270 degrees = left side
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 270f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Flank, result);
    }

    #endregion

    #region Flank/Front Boundaries (45 and 315 degrees)

    [Fact]
    public void GetPositional_PlayerAtFlankFrontBoundary45_ReturnsFlank()
    {
        // At exactly 45 degrees (start of right flank)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 45f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Flank, result);
    }

    [Fact]
    public void GetPositional_PlayerAtFlankFrontBoundary315_ReturnsFront()
    {
        // At exactly 315 degrees (start of front cone, wrapping)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 315f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Front, result);
    }

    #endregion

    #region Front Position (0 degrees)

    [Fact]
    public void GetPositional_PlayerDirectlyInFront_ReturnsFront()
    {
        // Player directly in front of target (0 degrees)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var player = CreateActor(new Vector3(0, 0, 5)); // +Z is in front when target faces +Z

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Front, result);
    }

    [Fact]
    public void IsAtFront_PlayerDirectlyInFront_ReturnsTrue()
    {
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var player = CreateActor(new Vector3(0, 0, 5));

        Assert.True(_service.IsAtFront(player.Object, target.Object));
        Assert.False(_service.IsAtRear(player.Object, target.Object));
        Assert.False(_service.IsAtFlank(player.Object, target.Object));
    }

    #endregion

    #region Wraparound Case (near 0/360 degrees)

    [Fact]
    public void GetPositional_PlayerAt350Degrees_ReturnsFront()
    {
        // 350 degrees is in the front cone (315-360 range)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 350f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Front, result);
    }

    [Fact]
    public void GetPositional_PlayerAt10Degrees_ReturnsFront()
    {
        // 10 degrees is in the front cone (0-45 range)
        var target = CreateActor(Vector3.Zero, rotation: 0f);
        var angle = 10f * (MathF.PI / 180f);
        var player = CreateActor(new Vector3(MathF.Sin(angle) * 5f, 0, MathF.Cos(angle) * 5f));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Front, result);
    }

    #endregion

    #region Rotated Target (non-zero facing)

    [Fact]
    public void GetPositional_TargetFacingEast_PlayerBehind_ReturnsRear()
    {
        // Target at origin facing east (rotation = PI/2), player to the west
        var targetRotation = MathF.PI / 2f; // Facing east
        var target = CreateActor(Vector3.Zero, rotation: targetRotation);
        // Player to the west = directly behind a target facing east
        var player = CreateActor(new Vector3(-5, 0, 0));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Rear, result);
    }

    [Fact]
    public void GetPositional_TargetFacingSouth_PlayerInFront_ReturnsFront()
    {
        // Target facing south (rotation = PI), player to the south
        var targetRotation = MathF.PI;
        var target = CreateActor(Vector3.Zero, rotation: targetRotation);
        // Player directly in front when target faces south = -Z direction
        var player = CreateActor(new Vector3(0, 0, -5));

        var result = _service.GetPositional(player.Object, target.Object);

        Assert.Equal(PositionalType.Front, result);
    }

    #endregion

    #region Omnidirectional Targets (positional immunity)

    [Fact]
    public void HasPositionalImmunity_TrainingDummy_ReturnsTrue()
    {
        // Training dummies (SubKind 2) have positional immunity
        var npc = CreateNpc(subKind: 2);

        Assert.True(_service.HasPositionalImmunity(npc.Object));
    }

    [Fact]
    public void HasPositionalImmunity_NormalEnemy_ReturnsFalse()
    {
        // Normal enemies (SubKind != 2) have positional requirements
        var npc = CreateNpc(subKind: 0);

        Assert.False(_service.HasPositionalImmunity(npc.Object));
    }

    [Fact]
    public void HasPositionalImmunity_NonBattleNpc_ReturnsFalse()
    {
        // Non-BattleNpc targets (using IBattleChara) always return false
        var actor = CreateActor(Vector3.Zero);

        Assert.False(_service.HasPositionalImmunity(actor.Object));
    }

    #endregion
}
