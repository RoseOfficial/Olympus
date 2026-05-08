using System.Collections.Generic;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Probes;

namespace Olympus.Tests.Services.Movement.Mocks;

public sealed class TestableBossCombatDetector : BossCombatDetector
{
    public List<(uint dataId, bool inCombat, bool playerIsTarget)> CombatTargets { get; set; } = new();

    public TestableBossCombatDetector(IBNpcRankProbe rankProbe)
        : base(objectTable: null!, clientState: null!, rankProbe: rankProbe, configAccessor: () => new HashSet<byte> { 4, 6 })
    { }

    protected override IEnumerable<(uint dataId, bool inCombat, bool playerIsTarget)> EnumerateCombatTargets()
        => CombatTargets;
}
