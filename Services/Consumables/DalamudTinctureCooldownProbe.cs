using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;

namespace Olympus.Services.Consumables;

/// <summary>
/// Production implementation of <see cref="ITinctureCooldownProbe"/>.
///
/// Tinctures share a 270s recast group. We probe via any tincture's item ID using
/// <c>ActionManager.GetRecastTime / GetRecastTimeElapsed(ActionType.Item, id)</c>;
/// the result reflects the shared cooldown regardless of which tincture was last used.
/// Pattern adapted from RotationSolverReborn's <c>BaseItem.ItemCooldown</c>.
/// </summary>
public sealed class DalamudTinctureCooldownProbe : ITinctureCooldownProbe
{
    private readonly IErrorMetricsService? _errorMetrics;

    public DalamudTinctureCooldownProbe(IErrorMetricsService? errorMetrics = null)
    {
        _errorMetrics = errorMetrics;
    }

    public unsafe float GetTinctureCooldownRemaining()
    {
        var am = SafeGameAccess.GetActionManager(_errorMetrics);
        if (am == null) return 0f;

        var total = am->GetRecastTime(ActionType.Item, ConsumableIds.TinctureOfStrength_NQ);
        var elapsed = am->GetRecastTimeElapsed(ActionType.Item, ConsumableIds.TinctureOfStrength_NQ);
        var remaining = total - elapsed;
        return remaining > 0f ? remaining : 0f;
    }
}
