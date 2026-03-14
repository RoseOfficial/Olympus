using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for SingleTargetHealingModule: Essential Dignity, Celestial Intersection,
/// Aspected Benefic, and Benefic II/Benefic.
/// Sub-modules do not check CanExecuteOgcd/CanExecuteGcd — those checks stay in the coordinator.
/// </summary>
public class SingleTargetHealingModuleTests
{
    private readonly SingleTargetHealingModule _module;

    public SingleTargetHealingModuleTests()
    {
        _module = new SingleTargetHealingModule();
    }

    #region Essential Dignity — TryOgcd

    [Fact]
    public void TryOgcd_EssentialDignityDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEssentialDignity = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000); // 30% HP
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryOgcd(context);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.EssentialDignity.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryOgcd_EssentialDignityReady_InjuredTarget_Executes()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEssentialDignity = true;
        config.Astrologian.EssentialDignityThreshold = 0.40f;

        // Member at 30% HP — below the 40% threshold
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.EssentialDignity.ActionId))
            .Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.EssentialDignity.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryOgcd(context);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.EssentialDignity.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryOgcd_EssentialDignity_TargetAboveThreshold_DoesNotFire()
    {
        // Member at 80% HP — above the 40% threshold
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEssentialDignity = true;
        config.Astrologian.EssentialDignityThreshold = 0.40f;

        var healthyMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 40000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { healthyMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.EssentialDignity.ActionId))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryOgcd(context);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.EssentialDignity.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Aspected Benefic — TryGcd

    [Fact]
    public void TryGcd_AspectedBeneficDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 30000, maxHp: 50000); // 60% HP
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryGcd(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.AspectedBenefic.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryGcd_SingleTargetHeal_Moving_DoesNotFireCastHeal()
    {
        // Benefic II has a cast time — blocked when moving (isMoving=true)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = false; // disable instant to isolate cast-time path
        config.Astrologian.EnableBeneficII = true;
        config.Astrologian.BeneficIIThreshold = 0.80f;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 30000, maxHp: 50000); // 60% HP — below threshold
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryGcd(context, isMoving: true);

        // isMoving=true blocks TrySingleTargetHeal; AspectedBenefic disabled → false
        Assert.False(result);
    }

    #endregion
}
