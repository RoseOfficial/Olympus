using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Tests for DamageModule.CollectCandidates gauge-phase branching.
///
/// Covers the real decision gates (config toggles, level, MP, gauge state,
/// proc state) for each distinct branch in the BLM rotation:
///   - Early-exit guards (not in combat, no target)
///   - Flare Star (level 100, 6 Astral Soul stacks, EnableFlareStar toggle)
///   - Fire phase: Fire IV main spam, Despair finisher
///   - Ice phase: Blizzard IV for Umbral Hearts, Paradox
///   - Polyglot cap avoidance: Xenoglossy at-cap push
///   - Expiring proc: Thunderhead near-timeout pushes HighThunder at priority 2
///   - Start rotation: Fire III transition when no element active
///
/// Each positive assertion verifies both the correct ability behavior instance
/// AND the expected priority. Each blocked assertion uses DoesNotContain so
/// a later-priority fallback candidate does not mask the missing primary one.
/// </summary>
public class DamageModuleGaugePhaseTests
{
    private readonly DamageModule _module = new();

    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static Mock<IBattleNpc> CreateEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        // Both properties used by TryPushIcePhase Thunder HP check
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWith(IBattleNpc enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy);
        return targeting;
    }

    // ─── Early-exit guards ───────────────────────────────────────────────────

    [Fact]
    public void NotInCombat_BothQueuesEmpty()
    {
        // CollectCandidates returns immediately on !InCombat — nothing is pushed.
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void NoTarget_BothQueuesEmpty()
    {
        // Default targeting service returns null from FindEnemy. Addle is
        // pushed after the target check, so no oGCDs are pushed either.
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(inCombat: true);
        // targetingService defaults to MockBuilders.CreateMockTargetingService()
        // which returns null from FindEnemy.

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    // ─── Flare Star ──────────────────────────────────────────────────────────

    [Fact]
    public void FlareStar_PushedAtPriority1_When6Stacks_Level100()
    {
        // Gate: EnableFlareStar=true (default), level=100 (MinLevel=100),
        //       AstralSoulStacks=6, IsActionReady=true, no timeline (MechanicCastGate bypassed).
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            astralSoulStacks: 6);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.FlareStar && c.Priority == 1);
    }

    [Fact]
    public void FlareStar_NotPushed_WhenAstralSoulStacksLessThan6()
    {
        // Gate: 5 < 6 stacks → TryPushFlareStar returns before pushing.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            astralSoulStacks: 5);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == HecateAbilities.FlareStar);
    }

    [Fact]
    public void FlareStar_NotPushed_WhenLevelBelow100()
    {
        // Gate: Lv.99 < FlareStar.MinLevel(100) → returns before push.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 99,
            inCombat: true,
            astralSoulStacks: 6);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == HecateAbilities.FlareStar);
    }

    [Fact]
    public void FlareStar_NotPushed_WhenToggleDisabled()
    {
        // Gate: EnableFlareStar=false → TryPushFlareStar returns without pushing.
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableFlareStar = false;

        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HecateTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            astralSoulStacks: 6);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == HecateAbilities.FlareStar);
    }

    // ─── Fire phase ──────────────────────────────────────────────────────────

    [Fact]
    public void Fire4_PushedAtPriority6_InFirePhase_WithMp()
    {
        // Setup: Astral Fire 3, timer healthy (no emergency transition), plenty of MP.
        // AstralSoulStacks=0 < FireIVsBeforeDespair(4) → first Despair check skipped.
        // CurrentMp=10000 >= fire4MinMp*2(1600) → second Despair check skipped.
        // Falls through to Fire IV branch (Lv.60 MinLevel, 10000 >= 800 MinMp). Priority 6.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inAstralFire: true,
            astralFireStacks: 3,
            elementTimer: 15f,
            currentMp: 10000,
            maxMp: 10000,
            astralSoulStacks: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Fire4 && c.Priority == 6);
    }

    [Fact]
    public void Despair_PushedAtPriority5_WhenMpBelowFinisherThreshold()
    {
        // Setup: currentMp=800 satisfies second Despair check:
        //   EnableDespair=true, Lv.100>=72, 800>=DespairMpCost(800), 800 < fire4MinMp*2(1600).
        // AstralSoulStacks=0 < FireIVsBeforeDespair(4) → first Despair check skipped.
        // HasFirestarter=false → goes straight to Despair push.
        // At Lv.100 the Enhanced Astral Fire trait makes Despair instant (castTime=0f) so
        // MechanicCastGate is bypassed. Priority 5.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inAstralFire: true,
            astralFireStacks: 3,
            elementTimer: 15f,
            currentMp: 800,
            maxMp: 10000,
            astralSoulStacks: 0,
            hasFirestarter: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Despair && c.Priority == 5);
    }

    // ─── Ice phase ───────────────────────────────────────────────────────────

    [Fact]
    public void BlizzardIV_PushedAtPriority6_InIcePhase_UI3_NoHearts()
    {
        // UmbralIceStacks=3, UmbralHearts=0 < 3, Lv.100 >= Blizzard4.MinLevel(58).
        // "Generate Umbral Hearts with Blizzard IV" branch fires. Priority 6.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: true,
            umbralIceStacks: 3,
            umbralHearts: 0,
            elementTimer: 15f);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Blizzard4 && c.Priority == 6);
    }

    [Fact]
    public void BlizzardIV_NotPushed_WhenUmbralHeartsAlreadyFull()
    {
        // UmbralHearts=3 → Blizzard IV branch condition (< 3) is false; falls through.
        // MaintainThunder=false and EnableParadox=false skip both subsequent branches.
        // mpPercent=0.5 < 0.99 prevents fire-transition push. Nothing is queued for GCD.
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.MaintainThunder = false;
        config.BlackMage.EnableParadox = false;

        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HecateTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: true,
            umbralIceStacks: 3,
            umbralHearts: 3,
            elementTimer: 15f,
            mpPercent: 0.5f);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == HecateAbilities.Blizzard4);
    }

    [Fact]
    public void Paradox_PushedAtPriority7_InIcePhase_UI3_HeartsFullThunderOff()
    {
        // Hearts=3 → Blizzard IV skipped. MaintainThunder=false → Thunder skipped.
        // EnableParadox=true, HasParadox=true, Lv.100 >= Paradox.MinLevel(90)
        // → Paradox push at priority 7. mpPercent=0.5 prevents premature fire transition.
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.MaintainThunder = false;

        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HecateTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inUmbralIce: true,
            umbralIceStacks: 3,
            umbralHearts: 3,
            elementTimer: 15f,
            hasParadox: true,
            mpPercent: 0.5f);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Paradox && c.Priority == 7);
    }

    // ─── Polyglot cap avoidance ──────────────────────────────────────────────

    [Fact]
    public void Xenoglossy_PushedAtPriority3_WhenPolyglotAtCap_SingleTarget()
    {
        // Lv.80, maxPolyglot=2 (level<98). PolyglotStacks=2 == maxPolyglot → cap branch.
        // EnableFoul=false so SelectPolyglotAction returns Xenoglossy. Priority 3.
        // TryPushPolyglot returns after pushing, so phase rotation is not reached.
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableFoul = false;

        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = HecateTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 80,
            inCombat: true,
            polyglotStacks: 2);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Xenoglossy && c.Priority == 3);
    }

    [Fact]
    public void Xenoglossy_NotPushed_WhenPolyglotStacksZero()
    {
        // PolyglotStacks=0 → TryPushPolyglot returns immediately before any push.
        // No Xenoglossy appears regardless of phase. Start-rotation pushes Fire3 instead.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            polyglotStacks: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == HecateAbilities.Xenoglossy);
    }

    // ─── Thunderhead expiring proc ────────────────────────────────────────────

    [Fact]
    public void ThunderheadExpiring_PushesHighThunderAtPriority2_WhenProcNearTimeout()
    {
        // HasThunderhead=true, ThunderheadRemaining=3f < 5f → expiring-proc branch fires.
        // At Lv.100 GetThunderST returns HighThunder (MinLevel=92).
        // MapThunderAbility(HighThunder) → HecateAbilities.HighThunder. Priority 2.
        // Note: TryPushExpiringProcs does NOT return, so phase rotation also runs.
        // We assert only that HighThunder at priority 2 is present.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasThunderhead: true,
            thunderheadRemaining: 3f,
            thunderDoTRemaining: 20f);  // DoT healthy; expiry of proc alone triggers

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.HighThunder && c.Priority == 2);
    }

    [Fact]
    public void Thunder_NotPushedAtPriority2_WhenNoProcAndDoTHealthy()
    {
        // HasThunderhead=false → condition (HasThunderhead && ...) is false.
        // No Thunder push at priority 2. Start-rotation pushes Fire3 at priority 6.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasThunderhead: false,
            thunderheadRemaining: 0f,
            thunderDoTRemaining: 20f);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.HighThunder && c.Priority == 2);
    }

    // ─── Thunder DoT refresh via Thunderhead (DoT about to expire) ────────────

    [Fact]
    public void ThunderheadRefresh_PushesHighThunderAtPriority2_WhenDoTBelowThreshold()
    {
        // HasThunderhead=true, ThunderDoTRemaining=3f < ThunderRefreshThreshold(6f default).
        // Expiring-proc branch fires via the DoT-remaining condition even though
        // the proc itself has plenty of time left (thunderheadRemaining=15f).
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasThunderhead: true,
            thunderheadRemaining: 15f,
            thunderDoTRemaining: 3f);   // below default ThunderRefreshThreshold=6f

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.HighThunder && c.Priority == 2);
    }

    // ─── Start rotation ──────────────────────────────────────────────────────

    [Fact]
    public void StartRotation_PushesFireIII_WhenNoElementActive()
    {
        // No Astral Fire or Umbral Ice → TryPushStartRotation.
        // UseFireIIITransition=true (default), Lv.100 → GetFireTransition(100)=Fire3
        // → HecateAbilities.Fire3 at priority 6.
        // Other pushes suppressed: polyglotStacks=0 (no Xenoglossy), astralSoulStacks=0
        // (no FlareStar), hasFirestarter=false, hasThunderhead=false (no expiring procs).
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWith(enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = HecateTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            inAstralFire: false,
            inUmbralIce: false,
            polyglotStacks: 0,
            astralSoulStacks: 0,
            hasFirestarter: false,
            hasThunderhead: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Fire3 && c.Priority == 6);
    }
}
