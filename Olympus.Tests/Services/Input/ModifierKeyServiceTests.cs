using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Moq;
using Olympus;
using Olympus.Config;
using Olympus.Services.Input;
using Xunit;

namespace Olympus.Tests.Services.Input;

public class ModifierKeyServiceTests
{
    private static (ModifierKeyService service, Configuration config, Mock<IKeyState> keyStateMock) Build(
        bool enabled = true,
        ModifierKey burstKey = ModifierKey.Shift,
        ModifierKey conservativeKey = ModifierKey.Control)
    {
        var keyStateMock = new Mock<IKeyState>(MockBehavior.Loose);
        keyStateMock.Setup(k => k[It.IsAny<VirtualKey>()]).Returns(false);

        var config = new Configuration
        {
            Input =
            {
                EnableModifierOverrides = enabled,
                BurstOverrideKey = burstKey,
                ConservativeOverrideKey = conservativeKey,
            }
        };

        return (new ModifierKeyService(keyStateMock.Object, config), config, keyStateMock);
    }

    [Fact]
    public void Update_WhenFeatureDisabled_BothOverridesAreFalse()
    {
        var (service, _, keyState) = Build(enabled: false);
        keyState.Setup(k => k[VirtualKey.SHIFT]).Returns(true);
        keyState.Setup(k => k[VirtualKey.CONTROL]).Returns(true);

        service.Update();

        Assert.False(service.IsBurstOverride);
        Assert.False(service.IsConservativeOverride);
    }

    [Fact]
    public void Update_WhenBurstKeyHeld_BurstOverrideIsTrue()
    {
        var (service, _, keyState) = Build(burstKey: ModifierKey.Shift);
        keyState.Setup(k => k[VirtualKey.SHIFT]).Returns(true);

        service.Update();

        Assert.True(service.IsBurstOverride);
        Assert.False(service.IsConservativeOverride);
    }

    [Fact]
    public void Update_WhenConservativeKeyHeld_ConservativeOverrideIsTrue()
    {
        var (service, _, keyState) = Build(conservativeKey: ModifierKey.Control);
        keyState.Setup(k => k[VirtualKey.CONTROL]).Returns(true);

        service.Update();

        Assert.False(service.IsBurstOverride);
        Assert.True(service.IsConservativeOverride);
    }

    [Fact]
    public void Update_WhenBothKeysHeld_BothOverridesCancel()
    {
        var (service, _, keyState) = Build(burstKey: ModifierKey.Shift, conservativeKey: ModifierKey.Control);
        keyState.Setup(k => k[VirtualKey.SHIFT]).Returns(true);
        keyState.Setup(k => k[VirtualKey.CONTROL]).Returns(true);

        service.Update();

        Assert.False(service.IsBurstOverride);
        Assert.False(service.IsConservativeOverride);
    }

    [Fact]
    public void Update_WhenBurstKeyIsNone_BurstOverrideIsAlwaysFalse()
    {
        var (service, _, keyState) = Build(burstKey: ModifierKey.None);
        keyState.Setup(k => k[It.IsAny<VirtualKey>()]).Returns(true);

        service.Update();

        Assert.False(service.IsBurstOverride);
    }

    [Fact]
    public void Update_AltKeyMappingWorks()
    {
        var (service, _, keyState) = Build(burstKey: ModifierKey.Alt);
        keyState.Setup(k => k[VirtualKey.MENU]).Returns(true);

        service.Update();

        Assert.True(service.IsBurstOverride);
    }

    [Fact]
    public void Update_KeyReleased_OverrideClears()
    {
        var (service, _, keyState) = Build(burstKey: ModifierKey.Shift);
        var shiftHeld = true;
        keyState.Setup(k => k[VirtualKey.SHIFT]).Returns(() => shiftHeld);

        service.Update();
        Assert.True(service.IsBurstOverride);

        shiftHeld = false;
        service.Update();

        Assert.False(service.IsBurstOverride);
    }

    [Fact]
    public void Update_FeatureDisabledMidSession_OverridesClear()
    {
        var (service, config, keyState) = Build(enabled: true);
        keyState.Setup(k => k[VirtualKey.SHIFT]).Returns(true);

        service.Update();
        Assert.True(service.IsBurstOverride);

        config.Input.EnableModifierOverrides = false;
        service.Update();

        Assert.False(service.IsBurstOverride);
    }
}
