using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.EchidnaCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Serpent's Ire

    [Fact]
    public void TryExecute_SerpentsIre_FiresWhenRequirementsMet()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SerpentsIre.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SerpentsIre_SkipsWhenNotReady()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SerpentsIre.ActionId)).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_SerpentsIre_SkipsWithoutNoxiousGnash()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SerpentsIre.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasNoxiousGnash: false, // No debuff on target
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);

        Assert.Equal("Waiting for Noxious Gnash", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_SerpentsIre_SkipsWithoutHuntersInstinct()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SerpentsIre.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasNoxiousGnash: true,
            hasHuntersInstinct: false, // Missing Hunter's Instinct
            hasSwiftscaled: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);

        Assert.Equal("Waiting for buffs", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_SerpentsIre_SkipsWithoutSwiftscaled()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(VPRActions.SerpentsIre.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: false, // Missing Swiftscaled
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);

        Assert.Equal("Waiting for buffs", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_SerpentsIre_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 85, // Below SerpentsIre MinLevel (86)
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static IEchidnaContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasNoxiousGnash = false,
        bool hasHuntersInstinct = false,
        bool hasSwiftscaled = false,
        Mock<IActionService>? actionService = null)
    {
        return EchidnaTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            hasNoxiousGnash: hasNoxiousGnash,
            hasHuntersInstinct: hasHuntersInstinct,
            hasSwiftscaled: hasSwiftscaled,
            actionService: actionService);
    }

    #endregion
}
