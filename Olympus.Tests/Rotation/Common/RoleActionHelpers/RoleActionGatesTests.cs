using Moq;
using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.RoleActionHelpers;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Rotation.Common.RoleActionHelpers;

public class RoleActionGatesTests
{
    private static (Mock<IRotationContext> ctx, Mock<IActionService> actionService) BuildContext(
        byte playerLevel,
        uint actionId,
        bool actionReady = true)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(a => a.IsActionReady(actionId)).Returns(actionReady);

        var ctx = new Mock<IRotationContext>();
        ctx.SetupGet(c => c.Player).Returns(player.Object);
        ctx.SetupGet(c => c.ActionService).Returns(actionService.Object);

        return (ctx, actionService);
    }

    // --- SwiftcastReady ---

    [Fact]
    public void SwiftcastReady_False_When_Level_Too_Low()
    {
        var (ctx, _) = BuildContext(playerLevel: (byte)(RoleActions.Swiftcast.MinLevel - 1), RoleActions.Swiftcast.ActionId);
        Assert.False(RoleActionGates.SwiftcastReady(ctx.Object));
    }

    [Fact]
    public void SwiftcastReady_False_When_OnCooldown()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, RoleActions.Swiftcast.ActionId, actionReady: false);
        Assert.False(RoleActionGates.SwiftcastReady(ctx.Object));
    }

    [Fact]
    public void SwiftcastReady_True_When_All_Gates_Pass()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, RoleActions.Swiftcast.ActionId, actionReady: true);
        Assert.True(RoleActionGates.SwiftcastReady(ctx.Object));
    }

    // --- TrueNorthReady ---

    [Fact]
    public void TrueNorthReady_False_When_Level_Too_Low()
    {
        var (ctx, _) = BuildContext(playerLevel: (byte)(RoleActions.TrueNorth.MinLevel - 1), RoleActions.TrueNorth.ActionId);
        Assert.False(RoleActionGates.TrueNorthReady(ctx.Object));
    }

    [Fact]
    public void TrueNorthReady_False_When_OnCooldown()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, RoleActions.TrueNorth.ActionId, actionReady: false);
        Assert.False(RoleActionGates.TrueNorthReady(ctx.Object));
    }

    [Fact]
    public void TrueNorthReady_True_When_All_Gates_Pass()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, RoleActions.TrueNorth.ActionId, actionReady: true);
        Assert.True(RoleActionGates.TrueNorthReady(ctx.Object));
    }
}
