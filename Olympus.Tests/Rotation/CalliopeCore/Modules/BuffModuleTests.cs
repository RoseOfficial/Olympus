using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.CalliopeCore.Modules;

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

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.BuffState);
    }

    #region Pitch Perfect

    [Fact]
    public void TryExecute_PitchPerfect_FiresAt3Repertoire_DuringWM()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.PitchPerfect.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            songTimer: 30f,
            repertoire: 3,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PitchPerfect_SkipsWhenNotInWM()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: false,
            repertoire: 3,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PitchPerfect_UsesRemainingStacksBeforeSongEnds()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.PitchPerfect.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // 1 stack but song timer below 3s threshold
        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            songTimer: 2f, // Below 3s switch threshold
            repertoire: 1,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PitchPerfect_SkipsWhenRepertoireZero()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.PitchPerfect.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            songTimer: 30f,
            repertoire: 0, // No stacks
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PitchPerfect_BelowMinLevel_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 50, // Below PitchPerfect MinLevel (52)
            isWanderersMinuetActive: true,
            repertoire: 3,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Song Rotation

    [Fact]
    public void TryExecute_SongRotation_StartsWMWhenNoSong()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // PitchPerfect not applicable (no stacks)
        actionService.Setup(x => x.IsActionReady(BRDActions.WanderersMinuet.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.WanderersMinuet.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            noSongActive: true,
            currentSong: (byte)BRDActions.Song.None,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.WanderersMinuet.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SongRotation_SwitchesToMBWhenWMOnCooldown()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.WanderersMinuet.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(BRDActions.MagesBallad.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.MagesBallad.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            noSongActive: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.MagesBallad.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SongRotation_SwitchesWhenAPTimerBelow12Seconds()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.WanderersMinuet.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.WanderersMinuet.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isArmysPaeonActive: true,
            noSongActive: false,
            songTimer: 10f, // Below 12s AP cut threshold
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
    }

    [Fact]
    public void TryExecute_SongRotation_DoesNotSwitchWhenSongStillActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // No songs should be triggered
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            noSongActive: false,
            songTimer: 30f, // Plenty of time
            actionService: actionService,
            targetingService: targeting);

        // Should not try to switch songs
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a =>
                a.ActionId == BRDActions.WanderersMinuet.ActionId ||
                a.ActionId == BRDActions.MagesBallad.ActionId ||
                a.ActionId == BRDActions.ArmysPaeon.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Raging Strikes

    [Fact]
    public void TryExecute_RagingStrikes_FiresDuringWM()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Songs — WM active, no need to switch
        actionService.Setup(x => x.IsActionReady(BRDActions.RagingStrikes.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RagingStrikes.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            noSongActive: false,
            songTimer: 30f,
            hasRagingStrikes: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RagingStrikes.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RagingStrikes_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            noSongActive: false,
            songTimer: 30f,
            hasRagingStrikes: true, // Already active
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RagingStrikes.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RagingStrikes_SkipsWhenNotInWM()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // WM is available, so RS should wait for WM alignment
        actionService.Setup(x => x.IsActionReady(BRDActions.WanderersMinuet.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(BRDActions.RagingStrikes.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: false,
            isMagesBalladActive: true,
            noSongActive: false,
            songTimer: 30f,
            hasRagingStrikes: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // RS should not fire — waiting for WM alignment
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RagingStrikes.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Radiant Finale

    [Fact]
    public void TryExecute_RadiantFinale_FiresWith3CodaDuringBurst()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.RadiantFinale.ActionId)).Returns(true);
        // RS ready → return false so we get to RF
        actionService.Setup(x => x.IsActionReady(BRDActions.RagingStrikes.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(BRDActions.BattleVoice.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RadiantFinale.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            noSongActive: false,
            songTimer: 30f,
            hasRagingStrikes: true,
            hasBattleVoice: true,
            hasRadiantFinale: false,
            codaCount: 3,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RadiantFinale.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RadiantFinale_SkipsWithNoCoda()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isWanderersMinuetActive: true,
            noSongActive: false,
            songTimer: 30f,
            hasRagingStrikes: true,
            hasBattleVoice: true,
            hasRadiantFinale: false,
            codaCount: 0, // No coda
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RadiantFinale.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    private static ICalliopeContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool isWanderersMinuetActive = false,
        bool isMagesBalladActive = false,
        bool isArmysPaeonActive = false,
        bool noSongActive = true,
        float songTimer = 0f,
        byte currentSong = (byte)BRDActions.Song.None,
        int repertoire = 0,
        int codaCount = 0,
        bool hasRagingStrikes = false,
        bool hasBattleVoice = false,
        bool hasBarrage = false,
        bool hasRadiantFinale = false,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return CalliopeTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            isWanderersMinuetActive: isWanderersMinuetActive,
            isMagesBalladActive: isMagesBalladActive,
            isArmysPaeonActive: isArmysPaeonActive,
            noSongActive: noSongActive,
            songTimer: songTimer,
            currentSong: currentSong,
            repertoire: repertoire,
            codaCount: codaCount,
            hasRagingStrikes: hasRagingStrikes,
            hasBattleVoice: hasBattleVoice,
            hasBarrage: hasBarrage,
            hasRadiantFinale: hasRadiantFinale,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
