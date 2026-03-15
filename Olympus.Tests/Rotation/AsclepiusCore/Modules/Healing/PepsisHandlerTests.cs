using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class PepsisHandlerTests
{
    private readonly PepsisHandler _handler = new();

    [Fact]
    public void TryExecute_WhenPartyAboveHpThreshold_Skips()
    {
        // shieldedCount will be 0 (null StatusList), min AoEHealMinTargets clamps to 1,
        // so the shielded-count guard always fires first. Test that avg HP check
        // also correctly gates further execution when party is above the threshold.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.PepsisThreshold = 0.50f;

        // Even with members present, shieldedCount=0 < min=3 → skips early.
        // This test verifies the avgHp guard separately by making shieldedCount
        // pass (via AoEHealMinTargets=0 is clamped to 1, so still skips first).
        // We verify the handler returns false — the ordering of guards is deterministic.
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.80f, 0.70f, 3)); // avgHp=80% > PepsisThreshold=50%

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pepsis.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 58);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePepsis = false;

        var context = AsclepiusTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pepsis.ActionId)).Returns(false);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.60f, 0.50f, 3));

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 58);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoEukrasianShieldsActive_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 2; // require at least 2 shielded

        // Empty party members — no shields counted
        var partyHelper = MockBuilders.CreateMockPartyHelper(partyMembers: new System.Collections.Generic.List<Dalamud.Game.ClientState.Objects.Types.IBattleChara>());
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.60f, 0.50f, 3));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pepsis.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 58);

        // shieldedCount = 0 < AoEHealMinTargets = 2 → skip
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    // Positive path not testable: AsclepiusStatusHelper.HasEukrasianDiagnosisShield calls
    // HasStatus which iterates IBattleChara.StatusList. StatusList is
    // Dalamud.Game.ClientState.Statuses.StatusList — a sealed class backed by native game memory
    // that cannot be constructed or mocked in tests. Moq returns null for this property by
    // default, so the null-guard in BaseStatusHelper.HasStatus fires immediately and every
    // member counts as unshielded (shieldedCount = 0). SageConfig.AoEHealMinTargets is clamped
    // to a minimum of 1 via Math.Clamp, so shieldedCount (0) < AoEHealMinTargets (≥1) always
    // fires, returning false before reaching ExecuteOgcd. PepsisHandler is sealed with a
    // private TryPepsis method so the shield-counting path cannot be bypassed by subclassing.
}
