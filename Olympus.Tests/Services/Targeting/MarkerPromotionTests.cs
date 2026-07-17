using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Services.Targeting;
using Xunit;

namespace Olympus.Tests.Services.Targeting;

/// <summary>
/// Tests for IMarkerProbe integration in TargetingService.
/// LoS (BGCollision) throws in tests and the catch-block returns true (pass).
/// StatusList is null on mocks — invulnerability check skips safely.
/// </summary>
public sealed class MarkerPromotionTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a fully-mocked IBattleNpc that passes every GetValidEnemies filter.
    /// SubKind = 0 satisfies the "(byte)BattleNpcKind != Combatant && SubKind != 0" gate.
    /// StatusFlags.InCombat satisfies the combat-presence filter in FindLowestHpEnemy etc.
    /// </summary>
    private static Mock<IBattleNpc> MakeEnemy(ulong id, uint hp = 10_000, Vector3? pos = null)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(id);
        mock.Setup(x => x.EntityId).Returns((uint)id);
        mock.Setup(x => x.ObjectKind).Returns(ObjectKind.BattleNpc);
        mock.Setup(x => x.IsTargetable).Returns(true);
        mock.Setup(x => x.IsDead).Returns(false);
        mock.Setup(x => x.YalmDistanceX).Returns(5);       // well within any range
        mock.Setup(x => x.SubKind).Returns((byte)0);       // passes NPC-kind gate unconditionally
        mock.Setup(x => x.Position).Returns(pos ?? Vector3.Zero);
        mock.Setup(x => x.HitboxRadius).Returns(0.5f);
        mock.Setup(x => x.StatusFlags).Returns(StatusFlags.InCombat);
        mock.Setup(x => x.CurrentHp).Returns(hp);
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null);
        return mock;
    }

    private static IPlayerCharacter MakePlayer()
    {
        var mock = new Mock<IPlayerCharacter>();
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        mock.Setup(x => x.HitboxRadius).Returns(0.5f);
        mock.Setup(x => x.GameObjectId).Returns(999ul);
        return mock.Object;
    }

    private static TargetingService BuildService(
        Configuration config,
        Mock<IMarkerProbe> probeMock,
        IEnumerable<IBattleNpc> enemies,
        IGameObject? currentTarget = null,
        IGameObject? focusTarget = null)
    {
        var objectTableMock = new Mock<IObjectTable>();
        var enemyList = new List<IGameObject>();
        foreach (var e in enemies) enemyList.Add(e);
        objectTableMock
            .Setup(x => x.GetEnumerator())
            .Returns(() => enemyList.GetEnumerator());
        objectTableMock
            .Setup(x => x.SearchById(It.IsAny<ulong>()))
            .Returns((ulong id) => enemyList.Find(e => e.GameObjectId == id));

        var partyListMock = new Mock<IPartyList>();
        partyListMock.Setup(x => x.GetEnumerator()).Returns(new List<IPartyMember>().GetEnumerator());

        var targetManagerMock = new Mock<ITargetManager>();
        targetManagerMock.Setup(x => x.Target).Returns(currentTarget);
        targetManagerMock.Setup(x => x.FocusTarget).Returns(focusTarget);

        var gapCloserMock = new Mock<IGapCloserSafetyService>();

        // Force TTL=0 so the cache never suppresses a fresh rebuild, and disable
        // PauseWhenNoTarget so IsDamageTargetingPaused() never short-circuits.
        config.Targeting.TargetCacheTtlMs = 0;
        config.Targeting.PauseWhenNoTarget = false;

        return new TargetingService(
            objectTableMock.Object,
            partyListMock.Object,
            targetManagerMock.Object,
            config,
            gapCloserMock.Object,
            probeMock.Object);
    }

    private static Mock<IMarkerProbe> EmptyProbe()
    {
        var m = new Mock<IMarkerProbe>();
        m.Setup(x => x.GetAttackMarkTargets()).Returns(new ulong[8]);
        m.Setup(x => x.GetStopMarkTargets()).Returns(new ulong[2]);
        return m;
    }

    // ── attack mark promotion ─────────────────────────────────────────────────

    [Fact]
    public void AttackMark_WinsOverLowerHpUnmarked_UnderLowestHpStrategy()
    {
        // lowHpEnemy has the lowest HP but no mark; highHpEnemy has Attack1.
        // With UseAttackMarkers=true the Attack1 enemy must be returned.
        var lowHpEnemy = MakeEnemy(id: 1, hp: 500);
        var highHpEnemy = MakeEnemy(id: 2, hp: 9_000);

        var probe = EmptyProbe();
        // Attack1 slot (index 0) = highHpEnemy
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 2, 0, 0, 0, 0, 0, 0, 0 });

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.LowestHp;

        var svc = BuildService(config, probe, [lowHpEnemy.Object, highHpEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(2ul, result!.GameObjectId); // Attack1-marked enemy wins
    }

    [Fact]
    public void AttackMark1_BeatsAttackMark2_InPriority()
    {
        // Both enemies are attack-marked; Attack1 must win regardless of HP.
        var attack1Enemy = MakeEnemy(id: 10, hp: 8_000);
        var attack2Enemy = MakeEnemy(id: 20, hp: 500);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 10, 20, 0, 0, 0, 0, 0, 0 }); // Attack1=10, Attack2=20

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;

        var svc = BuildService(config, probe, [attack1Enemy.Object, attack2Enemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(10ul, result!.GameObjectId); // Attack1 wins over Attack2
    }

    [Fact]
    public void AttackMarkers_Disabled_FallsBackToNormalStrategy()
    {
        // UseAttackMarkers=false: marked high-HP enemy must NOT win; low-HP wins normally.
        var lowHpEnemy = MakeEnemy(id: 1, hp: 300);
        var highHpEnemy = MakeEnemy(id: 2, hp: 9_000);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 2, 0, 0, 0, 0, 0, 0, 0 }); // highHpEnemy is Attack1

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = false;
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.LowestHp;

        var svc = BuildService(config, probe, [lowHpEnemy.Object, highHpEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(1ul, result!.GameObjectId); // LowestHp wins when markers disabled
    }

    [Fact]
    public void CurrentTarget_IgnoresAttackMarkers()
    {
        // CurrentTarget strategy must return the player's selected target,
        // even when another enemy has an Attack1 mark.
        var attackMarkedEnemy = MakeEnemy(id: 5, hp: 5_000);
        var currentTargetEnemy = MakeEnemy(id: 6, hp: 9_000);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 5, 0, 0, 0, 0, 0, 0, 0 }); // enemy 5 is Attack1

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;

        // Pass currentTargetEnemy as the ITargetManager.Target
        var svc = BuildService(config, probe,
            [attackMarkedEnemy.Object, currentTargetEnemy.Object],
            currentTargetEnemy.Object);

        var player = MakePlayer();
        var result = svc.FindEnemy(EnemyTargetingStrategy.CurrentTarget, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(6ul, result!.GameObjectId); // current target, not the attack-marked one
    }

    [Fact]
    public void EmptyProbe_AttackReturnsAllZeros_NormalStrategyApplies()
    {
        // All marker slots are 0 → FindFirstAttackMarkedEnemy returns null → normal sort.
        var lowHpEnemy = MakeEnemy(id: 1, hp: 200);
        var highHpEnemy = MakeEnemy(id: 2, hp: 9_000);

        var probe = EmptyProbe(); // all zeros

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;

        var svc = BuildService(config, probe, [lowHpEnemy.Object, highHpEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(1ul, result!.GameObjectId); // LowestHp = enemy with 200 HP
    }

    // ── stop mark exclusion ───────────────────────────────────────────────────

    [Fact]
    public void StopMark_ExcludesEnemy_WhenFilterEnabled()
    {
        var normalEnemy = MakeEnemy(id: 3, hp: 10_000);
        var stopMarkedEnemy = MakeEnemy(id: 4, hp: 100); // lower HP, but stop-marked

        var probe = EmptyProbe();
        probe.Setup(x => x.GetStopMarkTargets()).Returns(new ulong[] { 4, 0 }); // id 4 is Stop1

        var config = new Configuration();
        config.Targeting.FilterStopMarkers = true;
        config.Targeting.UseAttackMarkers = false;

        var svc = BuildService(config, probe, [normalEnemy.Object, stopMarkedEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(3ul, result!.GameObjectId); // stop-marked enemy excluded even though lower HP
    }

    [Fact]
    public void StopMark_IncludesEnemy_WhenFilterDisabled()
    {
        var normalEnemy = MakeEnemy(id: 3, hp: 10_000);
        var stopMarkedEnemy = MakeEnemy(id: 4, hp: 100);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetStopMarkTargets()).Returns(new ulong[] { 4, 0 }); // id 4 is Stop1

        var config = new Configuration();
        config.Targeting.FilterStopMarkers = false;
        config.Targeting.UseAttackMarkers = false;

        var svc = BuildService(config, probe, [normalEnemy.Object, stopMarkedEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(4ul, result!.GameObjectId); // stop-marked enemy included → wins LowestHp
    }

    [Fact]
    public void NullProbe_AttackAndStop_NoPromotion_NoExclusion()
    {
        // When markerProbe is null (existing tests, no container registration),
        // the service falls through to the normal strategy without crashing.
        var lowHpEnemy = MakeEnemy(id: 1, hp: 100);
        var highHpEnemy = MakeEnemy(id: 2, hp: 9_000);

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;
        config.Targeting.FilterStopMarkers = true;
        config.Targeting.TargetCacheTtlMs = 0;
        config.Targeting.PauseWhenNoTarget = false;

        var objectTableMock = new Mock<IObjectTable>();
        var enemyList = new List<IGameObject> { lowHpEnemy.Object, highHpEnemy.Object };
        objectTableMock.Setup(x => x.GetEnumerator()).Returns(() => enemyList.GetEnumerator());
        objectTableMock.Setup(x => x.SearchById(It.IsAny<ulong>())).Returns((ulong id) => enemyList.Find(e => e.GameObjectId == id));

        var partyListMock = new Mock<IPartyList>();
        partyListMock.Setup(x => x.GetEnumerator()).Returns(new List<IPartyMember>().GetEnumerator());
        var targetManagerMock = new Mock<ITargetManager>();
        targetManagerMock.Setup(x => x.Target).Returns((IGameObject?)null);
        targetManagerMock.Setup(x => x.FocusTarget).Returns((IGameObject?)null);
        var gapCloserMock = new Mock<IGapCloserSafetyService>();

        // Construct without a probe — the optional param defaults to null
        var svc = new TargetingService(
            objectTableMock.Object,
            partyListMock.Object,
            targetManagerMock.Object,
            config,
            gapCloserMock.Object);   // no markerProbe

        var result = svc.FindEnemy(EnemyTargetingStrategy.LowestHp, 25f, MakePlayer());
        Assert.NotNull(result);
        Assert.Equal(1ul, result!.GameObjectId); // LowestHp wins, no crash
    }

    [Fact]
    public void FocusTarget_IgnoresAttackMarkers()
    {
        // FocusTarget strategy must return the player's focus target,
        // even when another enemy has an Attack1 mark.
        var attackMarkedEnemy = MakeEnemy(id: 7, hp: 5_000);
        var focusTargetEnemy = MakeEnemy(id: 8, hp: 9_000);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 7, 0, 0, 0, 0, 0, 0, 0 }); // enemy 7 is Attack1

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;

        // Pass focusTargetEnemy as the ITargetManager.FocusTarget
        var svc = BuildService(config, probe,
            [attackMarkedEnemy.Object, focusTargetEnemy.Object],
            focusTarget: focusTargetEnemy.Object);

        var player = MakePlayer();
        var result = svc.FindEnemy(EnemyTargetingStrategy.FocusTarget, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(8ul, result!.GameObjectId); // focus target, not the attack-marked one
    }

    [Fact]
    public void AttackMarkedEnemy_Promoted_UnderHighestHpStrategy()
    {
        // Under HighestHp the unmarked enemy (9000 HP) would win normally.
        // With UseAttackMarkers=true the Attack1-marked enemy (500 HP) must win instead.
        var unmarkedHighHpEnemy = MakeEnemy(id: 11, hp: 9_000);
        var markedLowHpEnemy = MakeEnemy(id: 12, hp: 500);

        var probe = EmptyProbe();
        probe.Setup(x => x.GetAttackMarkTargets())
             .Returns(new ulong[] { 12, 0, 0, 0, 0, 0, 0, 0 }); // Attack1 = markedLowHpEnemy

        var config = new Configuration();
        config.Targeting.UseAttackMarkers = true;
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.HighestHp;

        var svc = BuildService(config, probe, [unmarkedHighHpEnemy.Object, markedLowHpEnemy.Object]);
        var player = MakePlayer();

        var result = svc.FindEnemy(EnemyTargetingStrategy.HighestHp, 25f, player);

        Assert.NotNull(result);
        Assert.Equal(12ul, result!.GameObjectId); // Attack1-marked enemy wins over higher-HP unmarked
    }
}
