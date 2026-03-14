using Olympus.Rotation.IrisCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.IrisCore.Helpers;

public class StatusHelperTests
{
    private readonly IrisStatusHelper _helper = new();

    [Fact]
    public void HasSubtractivePalette_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSubtractivePalette(mock.Object));
    }

    [Fact]
    public void GetSubtractivePaletteRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetSubtractivePaletteRemaining(mock.Object));
    }

    [Fact]
    public void HasMonochromeTones_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasMonochromeTones(mock.Object));
    }

    [Fact]
    public void HasStarryMuse_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasStarryMuse(mock.Object));
    }

    [Fact]
    public void GetStarryMuseRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetStarryMuseRemaining(mock.Object));
    }

    [Fact]
    public void HasStarstruck_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasStarstruck(mock.Object));
    }

    [Fact]
    public void HasHyperphantasia_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHyperphantasia(mock.Object));
    }

    [Fact]
    public void GetHyperphantasiaStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetHyperphantasiaStacks(mock.Object));
    }

    [Fact]
    public void HasRainbowBright_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRainbowBright(mock.Object));
    }

    [Fact]
    public void HasHammerTime_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHammerTime(mock.Object));
    }

    [Fact]
    public void GetHammerTimeStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetHammerTimeStacks(mock.Object));
    }

    [Fact]
    public void HasTemperaCoat_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTemperaCoat(mock.Object));
    }

    [Fact]
    public void HasInspiration_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasInspiration(mock.Object));
    }
}
