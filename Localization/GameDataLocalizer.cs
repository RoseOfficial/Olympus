namespace Olympus.Localization;

using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

public record ActionTooltipData(
    string Name,
    uint ActionId,
    bool IsGcd,
    float CastTime,
    float RecastTime,
    int Range,
    int EffectRange,
    uint IconId
);

/// <summary>
/// Provides localized FFXIV ability names directly from game data.
/// Uses Lumina Excel sheets to get ability names in the current client language.
/// </summary>
public sealed class GameDataLocalizer
{
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, string> actionNameCache = new();
    private readonly Dictionary<uint, string> statusNameCache = new();
    private readonly Dictionary<uint, ActionTooltipData> actionTooltipCache = new();

    /// <summary>
    /// Singleton instance for easy access.
    /// </summary>
    public static GameDataLocalizer? Instance { get; private set; }

    /// <summary>
    /// Creates a new GameDataLocalizer.
    /// </summary>
    public GameDataLocalizer(IDataManager dataManager)
    {
        this.dataManager = dataManager;
        Instance = this;
    }

    /// <summary>
    /// Gets the localized name for an action by its ID.
    /// Returns the action name in the game's current client language.
    /// </summary>
    /// <param name="actionId">The action ID from ActionIds constants.</param>
    /// <returns>The localized action name, or empty string if not found.</returns>
    public string GetActionName(uint actionId)
    {
        // Check cache first
        if (this.actionNameCache.TryGetValue(actionId, out var cachedName))
            return cachedName;

        // Load from game data
        var actionSheet = this.dataManager.GetExcelSheet<Action>();
        if (actionSheet == null)
            return string.Empty;

        var action = actionSheet.GetRowOrDefault(actionId);
        if (action == null)
            return string.Empty;

        var name = action.Value.Name.ToString();
        this.actionNameCache[actionId] = name;
        return name;
    }

    /// <summary>
    /// Gets the localized name for a status effect by its ID.
    /// </summary>
    /// <param name="statusId">The status effect ID.</param>
    /// <returns>The localized status name, or empty string if not found.</returns>
    public string GetStatusName(uint statusId)
    {
        // Check cache first
        if (this.statusNameCache.TryGetValue(statusId, out var cachedName))
            return cachedName;

        // Load from game data
        var statusSheet = this.dataManager.GetExcelSheet<Status>();
        if (statusSheet == null)
            return string.Empty;

        var status = statusSheet.GetRowOrDefault(statusId);
        if (status == null)
            return string.Empty;

        var name = status.Value.Name.ToString();
        this.statusNameCache[statusId] = name;
        return name;
    }

    /// <summary>
    /// Gets tooltip data for an action (name, type, cast/recast, range, icon).
    /// Queries the Lumina Action sheet and caches by action ID.
    /// Returns null if the action ID is not found.
    /// </summary>
    public ActionTooltipData? GetActionTooltipData(uint actionId)
    {
        if (actionTooltipCache.TryGetValue(actionId, out var cached))
            return cached;

        var actionSheet = dataManager.GetExcelSheet<Action>();
        if (actionSheet == null)
            return null;

        var row = actionSheet.GetRowOrDefault(actionId);
        if (row == null)
            return null;

        var action = row.Value;
        var data = new ActionTooltipData(
            Name: action.Name.ToString(),
            ActionId: actionId,
            IsGcd: action.ActionCategory.RowId is 2 or 3,
            CastTime: action.Cast100ms / 1000f,
            RecastTime: action.Recast100ms / 1000f,
            Range: (int)action.Range,
            EffectRange: (int)action.EffectRange,
            IconId: (uint)action.Icon
        );
        actionTooltipCache[actionId] = data;
        return data;
    }

    /// <summary>
    /// Gets the localized name for a job/class by its ID.
    /// </summary>
    /// <param name="jobId">The class/job ID.</param>
    /// <returns>The localized job name, or empty string if not found.</returns>
    public string GetJobName(uint jobId)
    {
        var jobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (jobSheet == null)
            return string.Empty;

        var job = jobSheet.GetRowOrDefault(jobId);
        if (job == null)
            return string.Empty;

        // Use Name for full name (e.g., "White Mage"), Abbreviation for short form (e.g., "WHM")
        return job.Value.Name.ToString();
    }

    /// <summary>
    /// Gets the abbreviated job name (e.g., "WHM", "SCH").
    /// </summary>
    /// <param name="jobId">The class/job ID.</param>
    /// <returns>The abbreviated job name, or empty string if not found.</returns>
    public string GetJobAbbreviation(uint jobId)
    {
        var jobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (jobSheet == null)
            return string.Empty;

        var job = jobSheet.GetRowOrDefault(jobId);
        if (job == null)
            return string.Empty;

        return job.Value.Abbreviation.ToString();
    }

    /// <summary>
    /// Clears the name caches. Call this on language change if needed.
    /// </summary>
    public void ClearCache()
    {
        this.actionNameCache.Clear();
        this.statusNameCache.Clear();
        this.actionTooltipCache.Clear();
    }
}

/// <summary>
/// Static helper for convenient access to game data localization.
/// </summary>
public static class GameLoc
{
    /// <summary>
    /// Gets the localized action name.
    /// </summary>
    public static string Action(uint actionId)
    {
        return GameDataLocalizer.Instance?.GetActionName(actionId) ?? string.Empty;
    }

    /// <summary>
    /// Gets the localized status name.
    /// </summary>
    public static string Status(uint statusId)
    {
        return GameDataLocalizer.Instance?.GetStatusName(statusId) ?? string.Empty;
    }

    /// <summary>
    /// Gets the localized job name.
    /// </summary>
    public static string Job(uint jobId)
    {
        return GameDataLocalizer.Instance?.GetJobName(jobId) ?? string.Empty;
    }
}
