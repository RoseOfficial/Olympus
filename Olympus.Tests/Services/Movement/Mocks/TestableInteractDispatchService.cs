using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Olympus.Config;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;

namespace Olympus.Tests.Services.Movement.Mocks;

public sealed class TestableInteractDispatchService : InteractDispatchService
{
    public List<(ulong gameObjectId, ObjectKind kind, float distance, bool targetable)> Nearby { get; } = new();
    public bool PlayerInCombat { get; set; }
    public bool PlayerCasting { get; set; }
    public bool MenuOpen { get; set; }

    public TestableInteractDispatchService(IObjectInteractor interactor, IMovementClock clock, MovementConfig cfg)
        : base(objectTable: null!, clientState: null!, interactor: interactor, clock: clock, configAccessor: () => cfg, log: null!) { }

    protected override IEnumerable<(ulong gameObjectId, ObjectKind kind, float distance, bool targetable)> EnumerateNearbyObjects() => Nearby;
    protected override bool IsPlayerInCombat() => PlayerInCombat;
    protected override bool IsPlayerCasting() => PlayerCasting;
    protected override bool IsMenuFocused() => MenuOpen;
}
