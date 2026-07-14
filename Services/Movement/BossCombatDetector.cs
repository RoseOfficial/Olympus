using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Movement.Probes;

namespace Olympus.Services.Movement;

public class BossCombatDetector : IBossCombatDetector
{
    private readonly IObjectTable? objectTable;
    private readonly IBNpcRankProbe rankProbe;
    private readonly Func<MovementConfig> configAccessor;

    public bool IsBossEngaged { get; private set; }

    public BossCombatDetector(IObjectTable objectTable, IClientState clientState, IBNpcRankProbe rankProbe, Func<MovementConfig> configAccessor)
    {
        this.objectTable = objectTable;
        this.rankProbe = rankProbe;
        this.configAccessor = configAccessor;
    }

    public void Update()
    {
        IsBossEngaged = false;
        var cfg = configAccessor();
        var bossRanks = cfg.BossRanks;
        var overrides = cfg.AvoidanceBossOverrides;
        foreach (var (dataId, inCombat, playerIsTarget) in EnumerateCombatTargets())
        {
            if (!inCombat || !playerIsTarget) continue;
            // DataId-based explicit override: treat as boss regardless of rank (suppresses avoidance).
            if (overrides.Contains(dataId))
            {
                IsBossEngaged = true;
                return;
            }
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
