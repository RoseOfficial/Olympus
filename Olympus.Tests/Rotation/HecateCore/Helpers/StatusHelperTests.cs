using Moq;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Rotation.HecateCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HecateCore.Helpers;

public class StatusHelperTests
{
    private readonly HecateStatusHelper _helper = new();

    [Fact]
    public void HasFirestarter_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFirestarter(mock.Object));
    }

    [Fact]
    public void GetFirestarterRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetFirestarterRemaining(mock.Object));
    }

    [Fact]
    public void HasThunderhead_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasThunderhead(mock.Object));
    }

    [Fact]
    public void GetThunderheadRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetThunderheadRemaining(mock.Object));
    }

    [Fact]
    public void HasTriplecast_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTriplecast(mock.Object));
    }

    [Fact]
    public void GetTriplecastStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetTriplecastStacks(mock.Object));
    }

    [Fact]
    public void HasLeyLines_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasLeyLines(mock.Object));
    }

    [Fact]
    public void GetLeyLinesRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetLeyLinesRemaining(mock.Object));
    }

    [Fact]
    public void HasThunderDoT_NullStatusList_ReturnsFalse()
    {
        var target = new Mock<IBattleChara>();
        target.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasThunderDoT(target.Object, 0u));
    }

    [Fact]
    public void GetThunderDoTRemaining_NullStatusList_ReturnsZero()
    {
        var target = new Mock<IBattleChara>();
        target.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetThunderDoTRemaining(target.Object, 0u));
    }
}
