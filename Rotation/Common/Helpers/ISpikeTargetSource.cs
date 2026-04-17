using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Minimal party-helper surface consumed by shared healer detection helpers
/// (currently <see cref="PreemptiveSpikeDetectionHelper"/>). Implemented by
/// <see cref="HealerPartyHelper"/> and inherited by each healer's party
/// helper interface so shared code works against Moq proxies too.
/// </summary>
public interface ISpikeTargetSource
{
    IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false);

    float GetHpPercent(IBattleChara target);

    IBattleChara? FindMostEndangeredPartyMember(
        IPlayerCharacter player,
        IDamageIntakeService damageIntakeService,
        int healAmount = 0,
        IDamageTrendService? damageTrendService = null,
        IShieldTrackingService? shieldTrackingService = null,
        float rangeSquared = 900f);
}
