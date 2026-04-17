using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Services.Healing;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared co-healer awareness heuristic used by all four healer rotations.
/// Skips a single-target heal when a co-healer's in-flight cast would already
/// cover most of the target's missing HP, avoiding double-healing waste.
/// </summary>
public static class CoHealerAwarenessHelper
{
    /// <summary>
    /// Returns true if a co-healer has a pending heal on the given target that
    /// covers at least <paramref name="coverageThreshold"/> of the missing HP —
    /// meaning the current healer should skip their own cast.
    /// </summary>
    /// <param name="enabled">Master toggle (<c>HealingConfig.EnableCoHealerAwareness</c>).</param>
    /// <param name="service">Shared co-healer detection service (may be null).</param>
    /// <param name="target">Heal target; must be non-null.</param>
    /// <param name="coverageThreshold">Fraction of missing HP the co-healer must cover (<c>HealingConfig.CoHealerPendingHealThreshold</c>).</param>
    public static bool CoHealerWillCover(
        bool enabled,
        ICoHealerDetectionService? service,
        IBattleChara target,
        float coverageThreshold)
    {
        if (!enabled) return false;
        if (service is null || !service.HasCoHealer) return false;
        if (target is null) return false;

        var pending = service.CoHealerPendingHeals;
        if (!pending.TryGetValue(target.EntityId, out var pendingHeal) || pendingHeal <= 0)
            return false;

        var missingHp = target.MaxHp - target.CurrentHp;
        if (missingHp <= 0) return true;

        var coverage = (float)pendingHeal / missingHp;
        return coverage >= coverageThreshold;
    }
}
