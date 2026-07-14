using System.Collections.Generic;
using Olympus.Config;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Probes;

namespace Olympus.Tests.Services.Movement.Mocks;

public sealed class TestableBossCombatDetector : BossCombatDetector
{
    public List<(uint dataId, bool inCombat, bool playerIsTarget)> CombatTargets { get; set; } = new();

    public TestableBossCombatDetector(IBNpcRankProbe rankProbe, MovementConfig? cfg = null)
        : base(objectTable: null!, clientState: null!, rankProbe: rankProbe,
               configAccessor: () => cfg ?? new MovementConfig { BossRanks = new System.Collections.Generic.HashSet<byte> { 4, 6 } })
    { }

    protected override IEnumerable<(uint dataId, bool inCombat, bool playerIsTarget)> EnumerateCombatTargets()
        => CombatTargets;
}
