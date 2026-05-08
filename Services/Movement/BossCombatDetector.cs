using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Services.Movement.Probes;

namespace Olympus.Services.Movement;

public class BossCombatDetector : IBossCombatDetector
{
    private readonly IObjectTable? objectTable;
    private readonly IBNpcRankProbe rankProbe;
    private readonly Func<HashSet<byte>> configAccessor;

    public bool IsBossEngaged { get; private set; }

    public BossCombatDetector(IObjectTable objectTable, IClientState clientState, IBNpcRankProbe rankProbe, Func<HashSet<byte>> configAccessor)
    {
        this.objectTable = objectTable;
        this.rankProbe = rankProbe;
        this.configAccessor = configAccessor;
    }

    public void Update()
    {
        IsBossEngaged = false;
        var bossRanks = configAccessor();
        foreach (var (dataId, inCombat, playerIsTarget) in EnumerateCombatTargets())
        {
            if (!inCombat || !playerIsTarget) continue;
            var rank = rankProbe.GetRank(dataId);
            if (IsBossClass(rank, bossRanks))
            {
                IsBossEngaged = true;
                return;
            }
        }
    }

    /// <summary>Test seam -- yields (dataId, isInCombat, playerIsOnEnmityListOrTarget) tuples.</summary>
    protected virtual IEnumerable<(uint dataId, bool inCombat, bool playerIsTarget)> EnumerateCombatTargets()
    {
        if (objectTable == null) yield break;

        var player = objectTable.LocalPlayer;
        if (player == null) yield break;

        var playerId = player.GameObjectId;

        foreach (var obj in objectTable)
        {
            if (obj is not IBattleNpc npc) continue;
            var inCombat = (npc.StatusFlags & StatusFlags.InCombat) != 0;
            if (!inCombat) continue;
            var playerIsTarget = npc.TargetObjectId == playerId || (player.TargetObjectId == npc.GameObjectId);
            yield return (npc.BaseId, inCombat, playerIsTarget);
        }
    }

    public static bool IsBossClass(byte rank, IReadOnlySet<byte> bossRanks) => bossRanks.Contains(rank);
}
