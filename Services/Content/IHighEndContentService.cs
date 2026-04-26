namespace Olympus.Services.Content;

/// <summary>
/// Classifies the current zone as high-end duty content
/// (savage, extreme, ultimate, criterion, chaotic alliance) or not.
/// </summary>
public interface IHighEndContentService
{
    /// <summary>True if the current TerritoryType is a high-end duty.</summary>
    bool IsHighEndZone { get; }

    /// <summary>
    /// Called when the territory changes. Re-evaluates <see cref="IsHighEndZone"/>.
    /// </summary>
    void OnTerritoryChanged(ushort territoryType);
}
