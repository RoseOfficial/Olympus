using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Moq;
using Olympus.Config;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;
using Olympus.Tests.Services.Movement.Mocks;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class InteractDispatchServiceTests
{
    private static (TestableInteractDispatchService svc, Mock<IObjectInteractor> interactor, Mock<IMovementClock> clock, MovementConfig cfg) Build(Action<MovementConfig>? configure = null)
    {
        var interactor = new Mock<IObjectInteractor>();
        var clock = new Mock<IMovementClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UnixEpoch.AddSeconds(1000));
        var cfg = new MovementConfig { EnableAutoInteract = true };
        configure?.Invoke(cfg);
        return (new TestableInteractDispatchService(interactor.Object, clock.Object, cfg), interactor, clock, cfg);
    }

    [Fact]
    public void Update_FeatureDisabled_DoesNotInteract()
    {
        var (svc, interactor, _, cfg) = Build(c => c.EnableAutoInteract = false);
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        svc.Update();
        interactor.Verify(i => i.Interact(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void Update_TreasureWithinRange_Interacts()
    {
        var (svc, interactor, _, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Once);
    }

    [Fact]
    public void Update_TreasureOutOfRange_DoesNotInteract()
    {
        var (svc, interactor, _, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 10f, true));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Never);
    }

    [Fact]
    public void Update_KindNotInAllowlist_DoesNotInteract()
    {
        var (svc, interactor, _, _) = Build();
        svc.Nearby.Add((100, ObjectKind.EventNpc, 2f, true));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Never);
    }

    [Fact]
    public void Update_AllowlistExpanded_InteractsEventObj()
    {
        var (svc, interactor, _, cfg) = Build(c => c.InteractAllowedKinds.Add(ObjectKind.EventObj));
        svc.Nearby.Add((100, ObjectKind.EventObj, 2f, true));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Once);
    }

    [Fact]
    public void Update_NotTargetable_DoesNotInteract()
    {
        var (svc, interactor, _, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, false));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Never);
    }

    [Fact]
    public void Update_PlayerCasting_DoesNotInteract()
    {
        var (svc, interactor, _, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        svc.PlayerCasting = true;
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Never);
    }

    [Fact]
    public void Update_InCombatAndInCombatDisabled_DoesNotInteract()
    {
        var (svc, interactor, _, _) = Build(c => c.InteractInCombat = false);
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        svc.PlayerInCombat = true;
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Never);
    }

    [Fact]
    public void Update_PerObjectCooldown_DedupesRepeatInteracts()
    {
        var (svc, interactor, clock, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        svc.Update();
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Once);
    }

    [Fact]
    public void Update_AfterCooldownExpires_InteractsAgain()
    {
        var (svc, interactor, clock, _) = Build();
        svc.Nearby.Add((100, ObjectKind.Treasure, 2f, true));
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UnixEpoch.AddSeconds(1000));
        svc.Update();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UnixEpoch.AddSeconds(1010));
        svc.Update();
        interactor.Verify(i => i.Interact(100), Times.Exactly(2));
    }
}
