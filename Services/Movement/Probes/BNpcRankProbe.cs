using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Olympus.Services.Movement.Probes;

public sealed class BNpcRankProbe : IBNpcRankProbe
{
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, byte> cache = new();

    public BNpcRankProbe(IDataManager dataManager)
    {
        this.dataManager = dataManager;
    }

    public byte GetRank(uint dataId)
    {
        if (cache.TryGetValue(dataId, out var cached))
            return cached;

        var sheet = dataManager.GetExcelSheet<BNpcBase>();
        var row = sheet.GetRowOrDefault(dataId);
        var rank = row?.Rank ?? (byte)0;
        cache[dataId] = rank;
        return rank;
    }
}
