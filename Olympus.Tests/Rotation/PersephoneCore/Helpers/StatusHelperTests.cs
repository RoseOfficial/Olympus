using Moq;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Rotation.PersephoneCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.PersephoneCore.Helpers;

public class StatusHelperTests
{
    private readonly PersephoneStatusHelper _helper = new();

    [Fact]
    public void HasFurtherRuin_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFurtherRuin(mock.Object));
    }

    [Fact]
    public void GetFurtherRuinRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetFurtherRuinRemaining(mock.Object));
    }

    [Fact]
    public void HasSearingLight_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSearingLight(mock.Object));
    }

    [Fact]
    public void GetSearingLightRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetSearingLightRemaining(mock.Object));
    }

    [Fact]
    public void HasRadiantAegis_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRadiantAegis(mock.Object));
    }

    [Fact]
    public void HasIfritsFavor_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasIfritsFavor(mock.Object));
    }

    [Fact]
    public void HasTitansFavor_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTitansFavor(mock.Object));
    }

    [Fact]
    public void HasGarudasFavor_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasGarudasFavor(mock.Object));
    }

    [Fact]
    public void HasEverlastingFlight_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEverlastingFlight(mock.Object));
    }

    [Fact]
    public void HasSurecast_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSurecast(mock.Object));
    }
}
