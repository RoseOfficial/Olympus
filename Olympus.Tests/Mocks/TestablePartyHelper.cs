using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services;
using Olympus.Services.Prediction;

namespace Olympus.Tests.Mocks;

/// <summary>
/// Testable subclass of PartyHelper that overrides GetAllPartyMembers
/// to return a pre-built party list without Dalamud runtime dependencies.
/// Requires PartyHelper to be non-sealed.
/// </summary>
public sealed class TestableApolloPartyHelper : PartyHelper
{
    private readonly List<IBattleChara> _members;

    public TestableApolloPartyHelper(IEnumerable<IBattleChara> members, Configuration? config = null)
        : base(
            new Mock<IObjectTable>().Object,
            new Mock<IPartyList>().Object,
            CreateHpPredictionService(),
            config ?? new Configuration())
    {
        _members = members.ToList();
    }

    public override IEnumerable<IBattleChara> GetAllPartyMembers(
        IPlayerCharacter player, bool includeDead = false)
    {
        // Return all members regardless of includeDead — production callers
        // apply their own dead-member guards (e.g., the IsDead check in
        // CountPartyMembersNeedingAoEHeal). This ensures tests exercise those guards.
        return _members;
    }

    private static HpPredictionService CreateHpPredictionService()
    {
        var combatEventsMock = new Mock<ICombatEventService>();

        // Set up the event backing fields so the constructor subscription does not throw.
        combatEventsMock.SetupAdd(x => x.OnLocalPlayerHealLanded += It.IsAny<Action<uint>>());
        combatEventsMock.SetupRemove(x => x.OnLocalPlayerHealLanded -= It.IsAny<Action<uint>>());

        // Pass through: shadow HP = current HP (no adjustments)
        combatEventsMock.Setup(x => x.GetShadowHp(It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns((uint _, uint hp) => hp);

        return new HpPredictionService(combatEventsMock.Object, new Configuration());
    }
}
