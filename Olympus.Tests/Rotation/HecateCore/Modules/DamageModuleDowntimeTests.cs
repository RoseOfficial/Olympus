using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.HecateCore;
using Xunit;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Tests for DamageModule.CollectCandidates during-downtime behavior:
/// when InCombat and no valid enemy exists, BLM pushes Umbral Soul to maintain
/// Umbral Ice stacks, Umbral Hearts, and Enochian (element timer).
///
/// Trigger: InCombat &amp;&amp; IsDamageTargetingPaused() &amp;&amp; InUmbralIce.
/// No timeline dependency — works in untimelined fights.
///
/// No new config gate: UmbralSoul during downtime is always-correct BLM play.
/// Olympus deliberately departs from BossMod's conditional
/// (Ice &lt; 3 || Hearts &lt; MaxHearts || CurMP &lt; MaxMP) and presses UmbralSoul
/// unconditionally while in Umbral Ice. This preserves Enochian through longer
/// downtime windows where even UI3 + max-hearts + full-MP players would lose it.
/// </summary>
public class DamageModuleDowntimeTests
{
    private readonly DamageModule _module = new();

    private static Mock<ITargetingService> CreatePausedTargeting()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(true);
        return targeting;
    }

    private static Mock<ITargetingService> CreateActiveTargeting(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        // IsDamageTargetingPaused defaults to false in MockBuilders.
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        return targeting;
    }

    // -----------------------------------------------------------------------
    // Positive: UmbralSoul pushed during downtime when in Umbral Ice
    // -----------------------------------------------------------------------

    [Fact]
    public void UmbralSoul_PushedAtPriority1_WhenInCombatNoEnemyInUmbralIce()
    {
        // InCombat=true + IsDamageTargetingPaused=true + InUmbralIce=true
        // → downtime branch fires → UmbralSoul at priority 1.
        var targeting = CreatePausedTargeting();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: true,
            umbralIceStacks: 3);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.UmbralSoul && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // Negative 1: Enemy present — normal rotation runs, UmbralSoul not pushed
    // Discriminating variable: IsDamageTargetingPaused=false (same InUmbralIce)
    // -----------------------------------------------------------------------

    [Fact]
    public void UmbralSoul_NotPushed_WhenEnemyPresent()
    {
        // Same InUmbralIce=true as positive but IsDamageTargetingPaused=false
        // and FindEnemy returns an enemy → normal ice-phase rotation runs.
        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        enemy.Setup(x => x.CurrentHp).Returns(10000u);
        enemy.Setup(x => x.MaxHp).Returns(10000u);
        var targeting = CreateActiveTargeting(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: true,
            umbralIceStacks: 3);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.UmbralSoul);
    }

    // -----------------------------------------------------------------------
    // Negative 2: Not in combat — module returns before reaching downtime branch
    // Discriminating variable: inCombat=false (same InUmbralIce, same pause)
    // -----------------------------------------------------------------------

    [Fact]
    public void UmbralSoul_NotPushed_WhenNotInCombat()
    {
        var targeting = CreatePausedTargeting();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: false,
            inUmbralIce: true,
            umbralIceStacks: 3);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.UmbralSoul);
    }

    // -----------------------------------------------------------------------
    // Negative 3: Not in Umbral Ice — downtime branch fires but gate prevents push
    // Discriminating variable: inUmbralIce=false (same InCombat, same pause)
    // -----------------------------------------------------------------------

    [Fact]
    public void UmbralSoul_NotPushed_WhenNotInUmbralIce()
    {
        var targeting = CreatePausedTargeting();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: false,
            inAstralFire: true,
            astralFireStacks: 3);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.UmbralSoul);
    }
}
