using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Olympus.Services.Content;

/// <summary>
/// Classifies the current zone as high-end duty content
/// (savage, extreme, ultimate, criterion, chaotic alliance) or not,
/// using the Lumina TerritoryType and ContentFinderCondition sheets.
/// </summary>
public sealed class HighEndContentService : IHighEndContentService
{
    private readonly IDataManager _dataManager;
    private bool _isHighEnd;

    public HighEndContentService(IDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public bool IsHighEndZone => _isHighEnd;

    public void OnTerritoryChanged(ushort territoryType)
    {
        _isHighEnd = false;
        if (territoryType == 0) return;

        var row = _dataManager.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryType);
        if (!row.HasValue) return;

        if (row.Value.IsPvpZone) return;

        var cfc = row.Value.ContentFinderCondition.ValueNullable;
        if (cfc is null) return;

        _isHighEnd = cfc.Value.HighEndDuty;
    }
}
