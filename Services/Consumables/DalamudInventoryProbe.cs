using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;

namespace Olympus.Services.Consumables;

/// <summary>
/// Production implementation of <see cref="IInventoryProbe"/>.
///
/// The probe API takes a single uint item ID. The HQ encoding convention used by
/// <c>ConsumableService</c> is "HQ ID = NQ ID + 1_000_000". This probe detects that
/// encoding, splits it into the underlying NQ ID + an HQ boolean, and calls the
/// native <c>InventoryManager.GetInventoryItemCount(uint, bool)</c> with the
/// correct arguments. (The native API takes a separate HQ flag; the +1_000_000
/// convention is for <c>UseAction(ActionType.Item, ...)</c> dispatch only.)
///
/// Returns 0 if the inventory pointer is null or the count is negative.
/// </summary>
public sealed class DalamudInventoryProbe : IInventoryProbe
{
    private readonly IErrorMetricsService? _errorMetrics;

    public DalamudInventoryProbe(IErrorMetricsService? errorMetrics = null)
    {
        _errorMetrics = errorMetrics;
    }

    public unsafe uint GetItemCount(uint itemId)
    {
        var mgr = SafeGameAccess.GetInventoryManager(_errorMetrics);
        if (mgr == null) return 0u;

        var isHq = itemId >= ConsumableIds.HqOffset;
        var resolvedId = isHq ? itemId - ConsumableIds.HqOffset : itemId;

        var count = mgr->GetInventoryItemCount(resolvedId, isHq);
        return count < 0 ? 0u : (uint)count;
    }
}
