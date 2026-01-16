using System;
using System.IO;
using System.Reflection;
using Dalamud.Plugin.Services;
using Olympus.Services;
using Olympus.Timeline.Models;
using Olympus.Timeline.Parser;

namespace Olympus.Timeline;

/// <summary>
/// Runtime service for fight timeline tracking and mechanic prediction.
/// Maintains timeline state, syncs to game events, and provides predictions to rotation modules.
/// </summary>
public sealed class TimelineService : ITimelineService, IDisposable
{
    private readonly IPluginLog log;
    private readonly ICombatEventService combatEventService;
    private readonly CactbotTimelineParser parser;

    private FightTimeline? loadedTimeline;
    private TimelineState? state;

    // Prediction cache to avoid allocations in Update() hot path
    private MechanicPrediction? cachedNextRaidwide;
    private MechanicPrediction? cachedNextTankBuster;
    private float lastPredictionUpdateTime;
    private const float PredictionCacheRefreshInterval = 0.25f; // Refresh predictions every 250ms

    // Simulation state
    private bool isSimulating;
    private float simulationStartTime;
    private DateTime simulationStartRealTime;

    public TimelineService(IPluginLog log, ICombatEventService combatEventService)
    {
        this.log = log;
        this.combatEventService = combatEventService;
        this.parser = new CactbotTimelineParser();
    }

    #region ITimelineService Properties

    public bool IsActive => state != null && loadedTimeline != null && (combatEventService.IsInCombat || isSimulating);

    public bool IsSimulating => isSimulating;

    public float CurrentTime => state?.CurrentTime ?? 0f;

    public string CurrentPhase => state?.CurrentPhase ?? string.Empty;

    public string FightName => loadedTimeline?.Name ?? string.Empty;

    public float Confidence => state?.Confidence ?? 0f;

    public MechanicPrediction? NextRaidwide => IsActive ? cachedNextRaidwide : null;

    public MechanicPrediction? NextTankBuster => IsActive ? cachedNextTankBuster : null;

    #endregion

    #region ITimelineService Methods

    public void Update()
    {
        if (loadedTimeline == null || state == null)
            return;

        float currentTime;

        if (isSimulating)
        {
            // Simulation mode - use real elapsed time with perfect confidence
            var elapsed = (float)(DateTime.UtcNow - simulationStartRealTime).TotalSeconds;
            currentTime = simulationStartTime + elapsed;
            state.ForceSync(currentTime); // Keep 100% confidence in simulation
        }
        else
        {
            // Normal mode - use combat state
            var inCombat = combatEventService.IsInCombat;

            if (!inCombat)
            {
                // Not in combat - reset state if we were tracking
                if (state.HasSynced)
                {
                    state.Reset();
                    ClearPredictionCache();
                }
                return;
            }

            // Update timeline position from combat duration
            currentTime = combatEventService.GetCombatDurationSeconds();
            state.UpdateTime(currentTime);
        }

        // Refresh prediction cache periodically (not every frame)
        if (currentTime - lastPredictionUpdateTime >= PredictionCacheRefreshInterval)
        {
            RefreshPredictionCache();
            lastPredictionUpdateTime = currentTime;
        }
    }

    public bool IsMechanicImminent(TimelineEntryType type, float withinSeconds)
    {
        var prediction = GetNextMechanic(type);
        return prediction.HasValue && prediction.Value.SecondsUntil <= withinSeconds;
    }

    public MechanicPrediction? GetNextMechanic(TimelineEntryType type)
    {
        if (!IsActive || state == null || loadedTimeline == null)
            return null;

        var currentTime = state.CurrentTime;
        var confidence = state.Confidence;

        // Binary search for first entry at or after current time
        var startIndex = loadedTimeline.FindFirstEntryAtOrAfter(currentTime);

        for (var i = startIndex; i < loadedTimeline.Entries.Length; i++)
        {
            var entry = loadedTimeline.Entries[i];

            if (entry.EntryType == type && !entry.IsHidden)
            {
                var secondsUntil = entry.Timestamp - currentTime;
                return new MechanicPrediction(
                    secondsUntil,
                    entry.EntryType,
                    entry.Name,
                    confidence,
                    entry.Duration);
            }
        }

        return null;
    }

    public void LoadForZone(uint zoneId)
    {
        var zoneInfo = TimelineZoneMapping.GetZoneInfo(zoneId);
        if (zoneInfo == null)
        {
            Clear();
            return;
        }

        var info = zoneInfo.Value;
        log.Info("TimelineService: Loading timeline for {0} (zone {1})", info.Name, zoneId);

        try
        {
            var content = LoadEmbeddedResource(info.ResourceName);
            if (string.IsNullOrEmpty(content))
            {
                log.Warning("TimelineService: Timeline resource not found: {0}", info.ResourceName);
                Clear();
                return;
            }

            var timeline = parser.Parse(content, zoneId, info.ContentId, info.Name);
            if (timeline == null)
            {
                log.Warning("TimelineService: Failed to parse timeline for {0}", info.Name);
                Clear();
                return;
            }

            loadedTimeline = timeline;
            state = new TimelineState(timeline);
            ClearPredictionCache();

            log.Info("TimelineService: Loaded timeline with {0} entries", timeline.Entries.Length);
        }
        catch (Exception ex)
        {
            log.Error(ex, "TimelineService: Error loading timeline for zone {0}", zoneId);
            Clear();
        }
    }

    public void Clear()
    {
        loadedTimeline = null;
        state = null;
        ClearPredictionCache();
    }

    public void OnAbilityUsed(uint sourceId, uint actionId)
    {
        if (state == null || (!combatEventService.IsInCombat && !isSimulating))
            return;

        var combatTime = isSimulating
            ? simulationStartTime + (float)(DateTime.UtcNow - simulationStartRealTime).TotalSeconds
            : combatEventService.GetCombatDurationSeconds();

        if (state.TrySync(actionId, combatTime))
        {
            log.Debug("TimelineService: Synced to action {0:X} at {1:F1}s", actionId, state.CurrentTime);

            // Immediately refresh predictions after sync
            RefreshPredictionCache();
            lastPredictionUpdateTime = combatTime;
        }
    }

    #endregion

    #region Simulation Methods

    public void StartSimulation()
    {
        log.Info("TimelineService: Starting simulation mode");

        // Create a test timeline programmatically
        loadedTimeline = CreateTestTimeline();
        state = new TimelineState(loadedTimeline);

        isSimulating = true;
        simulationStartTime = 0f;
        simulationStartRealTime = DateTime.UtcNow;

        // Force initial sync so confidence starts at 100%
        // Simulation has perfect timing, no drift possible
        state.ForceSync(0f);

        ClearPredictionCache();
        RefreshPredictionCache();

        log.Info("TimelineService: Simulation started with {0} entries", loadedTimeline.Entries.Length);
    }

    public void StopSimulation()
    {
        log.Info("TimelineService: Stopping simulation mode");

        isSimulating = false;
        loadedTimeline = null;
        state = null;
        ClearPredictionCache();
    }

    public void SimulateSyncPoint(uint actionId)
    {
        if (!isSimulating || state == null)
            return;

        var currentTime = simulationStartTime + (float)(DateTime.UtcNow - simulationStartRealTime).TotalSeconds;
        if (state.TrySync(actionId, currentTime))
        {
            log.Debug("TimelineService: Simulation synced to action {0:X} at {1:F1}s", actionId, state.CurrentTime);
            RefreshPredictionCache();
        }
    }

    public void AdvanceSimulationTime(float seconds)
    {
        if (!isSimulating || state == null)
            return;

        // Offset the start time backwards to effectively advance the current time
        simulationStartTime += seconds;

        var newTime = simulationStartTime + (float)(DateTime.UtcNow - simulationStartRealTime).TotalSeconds;

        // Force sync to maintain 100% confidence in simulation
        state.ForceSync(newTime);
        RefreshPredictionCache();

        log.Debug("TimelineService: Advanced simulation to {0:F1}s", newTime);
    }

    public MechanicPrediction[] GetUpcomingMechanics(float windowSeconds)
    {
        if (!IsActive || state == null || loadedTimeline == null)
            return Array.Empty<MechanicPrediction>();

        var currentTime = state.CurrentTime;
        var confidence = state.Confidence;
        var endTime = currentTime + windowSeconds;

        var startIndex = loadedTimeline.FindFirstEntryAtOrAfter(currentTime);
        var results = new System.Collections.Generic.List<MechanicPrediction>();

        for (var i = startIndex; i < loadedTimeline.Entries.Length; i++)
        {
            var entry = loadedTimeline.Entries[i];

            if (entry.Timestamp > endTime)
                break;

            if (entry.IsHidden)
                continue;

            // Only include combat-relevant mechanics
            if (entry.EntryType is TimelineEntryType.Raidwide or TimelineEntryType.TankBuster
                or TimelineEntryType.Stack or TimelineEntryType.Spread
                or TimelineEntryType.Adds or TimelineEntryType.Enrage
                or TimelineEntryType.Ability)
            {
                var secondsUntil = entry.Timestamp - currentTime;
                results.Add(new MechanicPrediction(
                    secondsUntil,
                    entry.EntryType,
                    entry.Name,
                    confidence,
                    entry.Duration));
            }
        }

        return results.ToArray();
    }

    private FightTimeline CreateTestTimeline()
    {
        // Create a realistic test timeline that demonstrates the system
        var entries = new TimelineEntry[]
        {
            // Phase 1: Opening
            new(0.0f, "Combat Start", TimelineEntryType.Phase, null, 0f, -1f, "Phase1", false),
            new(5.0f, "Auto-Attack", TimelineEntryType.Ability, null, 0f, -1f, null, false),
            new(10.0f, "Raidwide Alpha", TimelineEntryType.Raidwide, null, 3.0f, -1f, null, false),
            new(18.0f, "Tank Buster", TimelineEntryType.TankBuster, null, 0f, -1f, null, false),
            new(25.0f, "Auto-Attack", TimelineEntryType.Ability, null, 0f, -1f, null, false),
            new(30.0f, "Stack Marker", TimelineEntryType.Stack, null, 5.0f, -1f, null, false),

            // Phase 2: Adds
            new(40.0f, "Phase 2", TimelineEntryType.Phase, null, 0f, -1f, "Phase2", false),
            new(42.0f, "Adds Spawn", TimelineEntryType.Adds, null, 20.0f, -1f, null, false),
            new(50.0f, "Raidwide Beta", TimelineEntryType.Raidwide, null, 3.0f, -1f, null, false),
            new(60.0f, "Spread Markers", TimelineEntryType.Spread, null, 4.0f, -1f, null, false),

            // Phase 3: Burn
            new(70.0f, "Phase 3", TimelineEntryType.Phase, null, 0f, -1f, "Phase3", false),
            new(75.0f, "Double Tank Buster", TimelineEntryType.TankBuster, null, 0f, -1f, null, false),
            new(85.0f, "Raidwide Gamma", TimelineEntryType.Raidwide, null, 3.0f, -1f, null, false),
            new(95.0f, "Stack Marker", TimelineEntryType.Stack, null, 5.0f, -1f, null, false),
            new(105.0f, "Raidwide Delta", TimelineEntryType.Raidwide, null, 3.0f, -1f, null, false),

            // Enrage
            new(120.0f, "Enrage", TimelineEntryType.Enrage, null, 0f, -1f, "Enrage", false),
        };

        return new FightTimeline(
            zoneId: 0,
            contentId: "test",
            name: "Test Fight (Simulation)",
            entries: entries);
    }

    #endregion

    #region Private Methods

    private void RefreshPredictionCache()
    {
        cachedNextRaidwide = GetNextMechanicInternal(TimelineEntryType.Raidwide);
        cachedNextTankBuster = GetNextMechanicInternal(TimelineEntryType.TankBuster);
    }

    private MechanicPrediction? GetNextMechanicInternal(TimelineEntryType type)
    {
        if (state == null || loadedTimeline == null)
            return null;

        var currentTime = state.CurrentTime;
        var confidence = state.Confidence;

        var startIndex = loadedTimeline.FindFirstEntryAtOrAfter(currentTime);

        for (var i = startIndex; i < loadedTimeline.Entries.Length; i++)
        {
            var entry = loadedTimeline.Entries[i];

            if (entry.EntryType == type && !entry.IsHidden)
            {
                var secondsUntil = entry.Timestamp - currentTime;
                return new MechanicPrediction(
                    secondsUntil,
                    entry.EntryType,
                    entry.Name,
                    confidence,
                    entry.Duration);
            }
        }

        return null;
    }

    private void ClearPredictionCache()
    {
        cachedNextRaidwide = null;
        cachedNextTankBuster = null;
        lastPredictionUpdateTime = 0f;
    }

    private static string? LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #endregion

    public void Dispose()
    {
        Clear();
        log.Info("TimelineService: Disposed");
    }
}
