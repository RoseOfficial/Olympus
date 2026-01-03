using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Models;

using LuminaAction = Lumina.Excel.Sheets.Action;

namespace Olympus.Services;

/// <summary>
/// Tracks action attempts for debugging and GCD uptime analysis
/// </summary>
public sealed class ActionTracker
{
    private readonly IDataManager dataManager;
    private readonly int historySize;

    private readonly LinkedList<ActionAttempt> history = new();
    private readonly object historyLock = new();
    private IReadOnlyList<ActionAttempt>? cachedHistory;
    private int cachedVersion;
    private int historyVersion;

    // GCD tracking - XIVAnalysis style (event-based, not frame-based)
    private DateTime? combatStartTime;
    private float totalGcdTimeSeconds;
    private float lastCombatGcdUptime;
    private DateTime? lastSuccessfulCast;

    // Debug: current GCD state (updated each frame)
    public float DebugGcdRemaining { get; private set; }
    public bool DebugIsCasting { get; private set; }
    public bool DebugHasAnimLock { get; private set; }
    public bool DebugGcdReady { get; private set; }
    public bool DebugIsActive { get; private set; }

    // Debug: track last downtime event
    public string LastDowntimeReason { get; private set; } = "";
    public DateTime LastDowntimeTime { get; private set; }
    private bool wasOnGcdLastFrame = true;
    public int DowntimeEventCount { get; private set; }


    // Statistics
    private int totalAttempts;
    private int successfulCasts;
    private readonly Dictionary<ActionResult, int> failureReasons = new();
    private readonly Dictionary<uint, int> spellUsageCounts = new();

    public int HistorySize => historySize;

    public ActionTracker(
        IDataManager dataManager,
        Configuration configuration)
    {
        this.dataManager = dataManager;
        this.historySize = configuration.ActionHistorySize;
    }

    /// <summary>
    /// Log an action attempt with full details
    /// </summary>
    public void LogAttempt(
        uint actionId,
        string? targetName,
        uint? targetHp,
        ActionResult result,
        byte playerLevel,
        uint? statusCode = null)
    {
        var now = DateTime.Now;
        var timeSinceLastCast = lastSuccessfulCast.HasValue
            ? (float)(now - lastSuccessfulCast.Value).TotalSeconds
            : 0f;

        var spellName = GetActionName(actionId);
        var failureReason = statusCode.HasValue && statusCode.Value != 0
            ? ActionAttempt.StatusCodeDescription(statusCode.Value)
            : result switch
            {
                ActionResult.NoTarget => "No valid target found",
                ActionResult.Failed => "UseAction returned false",
                _ => null
            };

        var attempt = new ActionAttempt
        {
            Timestamp = now,
            SpellName = spellName,
            ActionId = actionId,
            TargetName = targetName,
            TargetHp = targetHp,
            Result = result,
            PlayerLevel = playerLevel,
            FailureReason = failureReason,
            TimeSinceLastCast = timeSinceLastCast,
            StatusCode = statusCode
        };

        // Update statistics
        totalAttempts++;
        if (result == ActionResult.Success)
        {
            successfulCasts++;
            lastSuccessfulCast = now;

            // Track spell usage
            spellUsageCounts.TryGetValue(actionId, out var spellCount);
            spellUsageCounts[actionId] = spellCount + 1;
        }
        else
        {
            failureReasons.TryGetValue(result, out var count);
            failureReasons[result] = count + 1;
        }

        // Add to ring buffer
        lock (historyLock)
        {
            history.AddFirst(attempt);
            while (history.Count > HistorySize)
            {
                history.RemoveLast();
            }
            historyVersion++;
        }
    }

    /// <summary>
    /// Get a cached copy of the action history.
    /// Only regenerates when history has changed, avoiding per-frame allocations.
    /// </summary>
    public IReadOnlyList<ActionAttempt> GetHistory()
    {
        lock (historyLock)
        {
            if (cachedHistory == null || cachedVersion != historyVersion)
            {
                cachedHistory = history.ToArray();
                cachedVersion = historyVersion;
            }
            return cachedHistory;
        }
    }

    /// <summary>
    /// Track GCD state each frame - call this every frame when you have a target
    /// </summary>
    public void TrackGcdState(bool gcdReady, float gcdRemaining = 0, bool isCasting = false, bool hasAnimLock = false, bool isActive = false)
    {
        // Store debug info
        DebugGcdRemaining = gcdRemaining;
        DebugIsCasting = isCasting;
        DebugHasAnimLock = hasAnimLock;
        DebugGcdReady = gcdReady;
        DebugIsActive = isActive;

        // Capture the moment downtime starts (transition from on-GCD to ready)
        if (gcdReady && wasOnGcdLastFrame)
        {
            DowntimeEventCount++;
            LastDowntimeTime = DateTime.Now;
            LastDowntimeReason = $"GCD:{gcdRemaining:F2}s Cast:{isCasting} Anim:{hasAnimLock} Active:{isActive}";
        }
        wasOnGcdLastFrame = !gcdReady;
    }

    /// <summary>
    /// Start tracking combat time. Call when player enters combat.
    /// </summary>
    public void StartCombat()
    {
        if (combatStartTime == null)
        {
            combatStartTime = DateTime.Now;
            totalGcdTimeSeconds = 0f;
            lastCombatGcdUptime = 0f;
        }
    }

    /// <summary>
    /// Stop tracking combat time. Call when player leaves combat.
    /// Caches the final uptime so it persists for review after combat.
    /// </summary>
    public void EndCombat()
    {
        if (combatStartTime != null)
        {
            lastCombatGcdUptime = CalculateGcdUptime();
        }
        combatStartTime = null;
    }

    /// <summary>
    /// Record a GCD cast with its duration (XIVAnalysis style).
    /// The duration should be the actual recast time from ActionManager.
    /// </summary>
    public void LogGcdCast(float gcdDuration)
    {
        if (combatStartTime != null)
        {
            totalGcdTimeSeconds += gcdDuration;
        }
    }

    /// <summary>
    /// Get current GCD uptime percentage using XIVAnalysis methodology.
    /// Returns cached value after combat ends, resets when new combat starts.
    /// </summary>
    public float GetGcdUptime()
    {
        if (combatStartTime == null)
            return lastCombatGcdUptime;

        return CalculateGcdUptime();
    }

    /// <summary>
    /// Calculate GCD uptime: (totalGcdTime / combatDuration) * 100
    /// </summary>
    private float CalculateGcdUptime()
    {
        if (combatStartTime == null)
            return 0f;

        var combatDuration = (DateTime.Now - combatStartTime.Value).TotalSeconds;
        if (combatDuration <= 0)
            return 0f;

        // Cap at 100% - slight overlaps from queue window can cause >100%
        var uptime = (float)(totalGcdTimeSeconds / combatDuration) * 100f;
        return Math.Min(uptime, 100f);
    }

    /// <summary>
    /// Get average time between successful casts
    /// </summary>
    public float GetAverageTimeBetweenCasts()
    {
        lock (historyLock)
        {
            if (history.Count < 2)
                return 0f;

            // Single-pass calculation without intermediate list allocation
            float totalTime = 0f;
            int count = 0;
            DateTime? lastSuccessTime = null;

            foreach (var attempt in history)
            {
                if (attempt.Result != ActionResult.Success)
                    continue;

                if (lastSuccessTime.HasValue)
                {
                    totalTime += (float)(lastSuccessTime.Value - attempt.Timestamp).TotalSeconds;
                    count++;
                }
                lastSuccessTime = attempt.Timestamp;
            }

            return count > 0 ? totalTime / count : 0f;
        }
    }

    /// <summary>
    /// Get success rate percentage
    /// </summary>
    public float GetSuccessRate()
    {
        if (totalAttempts == 0)
            return 0f;

        return (float)successfulCasts / totalAttempts * 100f;
    }

    /// <summary>
    /// Get the most common failure reason
    /// </summary>
    public (ActionResult reason, int count)? GetMostCommonFailure()
    {
        if (failureReasons.Count == 0)
            return null;

        var most = failureReasons.MaxBy(kvp => kvp.Value);
        return (most.Key, most.Value);
    }

    /// <summary>
    /// Get statistics summary
    /// </summary>
    public (int total, int success, float successRate, float gcdUptime, float avgCastGap) GetStatistics()
    {
        return (
            totalAttempts,
            successfulCasts,
            GetSuccessRate(),
            GetGcdUptime(),
            GetAverageTimeBetweenCasts()
        );
    }

    /// <summary>
    /// Clear all tracking data
    /// </summary>
    public void Clear()
    {
        lock (historyLock)
        {
            history.Clear();
            cachedHistory = null;
            historyVersion++;
        }

        totalAttempts = 0;
        successfulCasts = 0;
        failureReasons.Clear();
        spellUsageCounts.Clear();
        lastSuccessfulCast = null;

        // Reset XIVAnalysis-style GCD tracking
        combatStartTime = null;
        totalGcdTimeSeconds = 0f;
        lastCombatGcdUptime = 0f;
        DowntimeEventCount = 0;
    }

    /// <summary>
    /// Get spell usage counts with resolved names, sorted by count descending
    /// </summary>
    public List<(string name, uint actionId, int count)> GetSpellUsageCounts()
    {
        var result = new List<(string name, uint actionId, int count)>();

        foreach (var (actionId, count) in spellUsageCounts)
        {
            var name = GetActionName(actionId);
            result.Add((name, actionId, count));
        }

        result.Sort((a, b) => b.count.CompareTo(a.count));
        return result;
    }

    /// <summary>
    /// Resolve action ID to display name
    /// </summary>
    private string GetActionName(uint actionId)
    {
        var actionSheet = dataManager.GetExcelSheet<LuminaAction>();
        if (actionSheet == null)
            return $"Action {actionId}";

        var row = actionSheet.GetRowOrDefault(actionId);
        if (!row.HasValue)
            return $"Action {actionId}";

        var name = row.Value.Name.ToString();
        return string.IsNullOrEmpty(name) ? $"Action {actionId}" : name;
    }
}
