using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Party;

namespace Olympus.Services.Analytics;

/// <summary>
/// Real-time performance tracker that collects combat metrics.
/// Integrates with ActionTracker and CombatEventService for data.
/// </summary>
public sealed class PerformanceTracker : IPerformanceTracker, IDisposable
{
    private readonly AnalyticsConfig config;
    private readonly IActionTracker actionTracker;
    private readonly CombatEventService combatEventService;
    private readonly IObjectTable objectTable;
    private readonly IPartyList partyList;
    private readonly IPluginLog log;
    private readonly IDataManager dataManager;
    private readonly IPartyCoordinationService? partyCoordinationService;

    private readonly string configDirectory;
    private string SessionsFilePath => System.IO.Path.Combine(configDirectory, "sessions.json");

    // Session history (most recent first)
    private readonly LinkedList<FightSession> sessionHistory = new();
    private readonly object historyLock = new();

    // Current combat tracking state
    private volatile bool isInCombat;
    private DateTime? combatStartTime;
    private uint currentJobId;
    private string currentZoneName = "Unknown";

    // Real-time metrics accumulators
    private long totalDamageDealt;
    private int deathCount;
    private int nearDeathCount;

    // HP tracking for near-death detection
    private readonly Dictionary<uint, float> lastHpPercent = new();
    private readonly Dictionary<uint, string> _actionNameCache = new();

    // Frame throttle for party HP checks
    private int _hpCheckFrameCounter = 0;
    private const int HpCheckFrameInterval = 10;

    // Cooldown tracking
    private readonly Dictionary<uint, CooldownTrackingState> cooldownStates = new();

    public bool IsTracking => isInCombat && config.EnableTracking;
    public float CombatDuration => combatEventService.GetCombatDurationSeconds();

    public event Action<FightSession>? OnSessionCompleted;
    public event Action<uint, float>? OnNearDeath;
    public event Action<uint>? OnDeath;

    public PerformanceTracker(
        AnalyticsConfig config,
        IActionTracker actionTracker,
        CombatEventService combatEventService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPluginLog log,
        IDataManager dataManager,
        IPartyCoordinationService? partyCoordinationService = null,
        string configDirectory = "")
    {
        this.config = config;
        this.actionTracker = actionTracker;
        this.combatEventService = combatEventService;
        this.objectTable = objectTable;
        this.partyList = partyList;
        this.log = log;
        this.dataManager = dataManager;
        this.partyCoordinationService = partyCoordinationService;
        this.configDirectory = configDirectory;

        // Subscribe to combat events
        combatEventService.OnLocalPlayerDamageDealt += OnLocalPlayerDamageDealt;
        combatEventService.OnLocalPlayerHealLanded += OnHealLanded;

        if (!string.IsNullOrEmpty(configDirectory))
            LoadPersistedSessions();
    }

    public void Update()
    {
        if (!config.EnableTracking)
            return;

        var wasInCombat = isInCombat;
        isInCombat = combatEventService.IsInCombat;

        // Combat state transitions
        if (isInCombat && !wasInCombat)
        {
            OnCombatStart();
        }
        else if (!isInCombat && wasInCombat)
        {
            OnCombatEnd();
        }

        // Update tracking while in combat
        if (isInCombat)
        {
            UpdateCombatTracking();
        }
    }

    private void OnCombatStart()
    {
        combatStartTime = DateTime.Now;

        // Get player info
        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer != null)
        {
            currentJobId = localPlayer.ClassJob.RowId;
        }

        // Reset accumulators
        totalDamageDealt = 0;
        deathCount = 0;
        nearDeathCount = 0;
        lastHpPercent.Clear();
        cooldownStates.Clear();

        // Initialize HP tracking for party members
        foreach (var member in partyList)
        {
            if (member.GameObject is Dalamud.Game.ClientState.Objects.Types.ICharacter character)
            {
                var hpPercent = character.MaxHp > 0 ? (float)character.CurrentHp / character.MaxHp : 1f;
                lastHpPercent[character.EntityId] = hpPercent;
            }
        }

        // Notify ActionTracker
        actionTracker.StartCombat();

        log.Debug("PerformanceTracker: Combat started for job {JobId}", currentJobId);
    }

    private void OnCombatEnd()
    {
        // Notify ActionTracker
        actionTracker.EndCombat();

        // Only record if above minimum duration
        var duration = CombatDuration;
        if (duration < config.MinCombatDuration)
        {
            log.Debug("PerformanceTracker: Combat too short ({Duration:F1}s < {Min:F1}s), not recording",
                duration, config.MinCombatDuration);
            combatStartTime = null;
            return;
        }

        // Create and store session
        var session = CreateSession();
        if (session != null)
        {
            lock (historyLock)
            {
                sessionHistory.AddFirst(session);

                // Trim to max history
                while (sessionHistory.Count > config.MaxSessionHistory)
                {
                    sessionHistory.RemoveLast();
                }
            }

            log.Information("PerformanceTracker: Session recorded - {Duration:F1}s, Score: {Score:F0}",
                session.Duration, session.Score?.Overall ?? 0);

            OnSessionCompleted?.Invoke(session);
            SaveSessionsToDisk();
        }

        combatStartTime = null;
    }

    private void UpdateCombatTracking()
    {
        _hpCheckFrameCounter++;
        if (_hpCheckFrameCounter < HpCheckFrameInterval)
            return;
        _hpCheckFrameCounter = 0;

        // Check for near-death events
        foreach (var member in partyList)
        {
            if (member.GameObject is not Dalamud.Game.ClientState.Objects.Types.ICharacter character)
                continue;

            var entityId = character.EntityId;
            var hpPercent = character.MaxHp > 0 ? (float)character.CurrentHp / character.MaxHp : 1f;

            // Check for death
            if (hpPercent <= 0 && lastHpPercent.TryGetValue(entityId, out var lastHp) && lastHp > 0)
            {
                deathCount++;
                OnDeath?.Invoke(entityId);
            }
            // Check for near-death (crossed threshold from above)
            else if (hpPercent <= config.NearDeathThreshold &&
                     lastHpPercent.TryGetValue(entityId, out lastHp) &&
                     lastHp > config.NearDeathThreshold)
            {
                nearDeathCount++;
                OnNearDeath?.Invoke(entityId, hpPercent);
            }

            lastHpPercent[entityId] = hpPercent;
        }
    }

    private void OnLocalPlayerDamageDealt(uint targetId, int damageAmount, uint actionId)
    {
        if (!IsTracking)
            return;

        System.Threading.Interlocked.Add(ref totalDamageDealt, damageAmount);
    }

    private void OnHealLanded(uint targetId)
    {
        // Healing is tracked via overheal statistics in CombatEventService
        // We'll pull those stats when creating the session
    }

    private FightSession? CreateSession()
    {
        if (combatStartTime == null)
            return null;

        var endTime = DateTime.Now;
        var duration = (float)(endTime - combatStartTime.Value).TotalSeconds;

        // Get overheal stats from CombatEventService
        var overhealStats = combatEventService.GetOverhealStatistics();

        // Build downtime breakdown if enabled
        DowntimeBreakdown? downtimeBreakdown = null;
        if (config.TrackDowntimeBreakdown)
        {
            downtimeBreakdown = actionTracker.GetDowntimeBreakdown();
        }

        // Build metrics snapshot
        var metrics = new CombatMetricsSnapshot
        {
            CombatDuration = duration,
            GcdUptime = actionTracker.GetGcdUptime(),
            PersonalDps = duration > 0 ? (float)System.Threading.Interlocked.Read(ref totalDamageDealt) / duration : 0f,
            TotalDamage = System.Threading.Interlocked.Read(ref totalDamageDealt),
            TotalHealing = overhealStats.TotalHealing,
            OverhealPercent = overhealStats.OverhealPercent,
            Deaths = deathCount,
            NearDeaths = nearDeathCount,
            Cooldowns = BuildCooldownUsage(duration),
            Timestamp = endTime,
            DowntimeAnalysis = downtimeBreakdown
        };

        // Calculate scores
        var score = CalculateScore(metrics);

        // Detect issues
        var issues = DetectIssues(metrics, score);

        return new FightSession
        {
            StartTime = combatStartTime.Value,
            EndTime = endTime,
            JobId = currentJobId,
            ZoneName = currentZoneName,
            FinalMetrics = metrics,
            Score = score,
            Issues = issues
        };
    }

    private List<CooldownUsage> BuildCooldownUsage(float duration)
    {
        var result = new List<CooldownUsage>();

        foreach (var (actionId, cooldownDuration) in config.TrackedCooldowns)
        {
            if (!cooldownStates.TryGetValue(actionId, out var state))
                state = new CooldownTrackingState();

            var optimalUses = (int)Math.Floor(duration / cooldownDuration) + 1;
            var avgDrift = state.DriftValues.Count > 0 ? state.DriftValues.Average() : 0f;

            result.Add(new CooldownUsage
            {
                ActionId = actionId,
                Name = GetActionName(actionId),
                CooldownDuration = cooldownDuration,
                TimesUsed = state.UseCount,
                OptimalUses = optimalUses,
                AverageDrift = avgDrift,
                DriftValues = state.DriftValues.ToList()
            });
        }

        return result;
    }

    private PerformanceScore CalculateScore(CombatMetricsSnapshot metrics)
    {
        // GCD Uptime Score: 95%+ = 100, linear down to 0 at 60%
        var gcdScore = Math.Clamp((metrics.GcdUptime - 60f) / 35f * 100f, 0f, 100f);

        // Cooldown Efficiency: average of all tracked cooldown efficiencies
        var cooldownScore = metrics.Cooldowns.Count > 0
            ? metrics.Cooldowns.Average(c => Math.Min(c.Efficiency, 100f))
            : 100f;

        // Healing Efficiency: 100% at 0 overheal, 0% at 50%+ overheal
        var healingScore = Math.Clamp(100f - (metrics.OverhealPercent * 2f), 0f, 100f);

        // Survival: 100% with 0 deaths, -20 per death, -5 per near-death
        var survivalScore = Math.Max(0f, 100f - (metrics.Deaths * 20f) - (metrics.NearDeaths * 5f));

        // Overall: weighted average
        // Weights depend on role, but for now use generic weights
        var overall = (gcdScore * 0.40f) + (cooldownScore * 0.30f) +
                     (healingScore * 0.15f) + (survivalScore * 0.15f);

        return new PerformanceScore
        {
            Overall = overall,
            GcdUptime = gcdScore,
            CooldownEfficiency = cooldownScore,
            HealingEfficiency = healingScore,
            Survival = survivalScore
        };
    }

    private List<PerformanceIssue> DetectIssues(CombatMetricsSnapshot metrics, PerformanceScore score)
    {
        var issues = new List<PerformanceIssue>();

        // GCD downtime
        if (metrics.GcdUptime < 90f)
        {
            var severity = metrics.GcdUptime < 80f ? IssueSeverity.Error : IssueSeverity.Warning;
            issues.Add(new PerformanceIssue
            {
                Type = IssueType.GcdDowntime,
                Severity = severity,
                Description = $"GCD uptime was {metrics.GcdUptime:F1}%",
                Suggestion = "Try to always be casting - use instant casts while moving"
            });
        }

        // Deaths
        if (metrics.Deaths > 0)
        {
            issues.Add(new PerformanceIssue
            {
                Type = IssueType.PartyDeath,
                Severity = IssueSeverity.Error,
                Description = $"{metrics.Deaths} party member(s) died",
                Suggestion = "Prioritize healing when party HP is critical"
            });
        }

        // Near-deaths
        if (metrics.NearDeaths > 2)
        {
            issues.Add(new PerformanceIssue
            {
                Type = IssueType.NearDeath,
                Severity = IssueSeverity.Warning,
                Description = $"{metrics.NearDeaths} near-death events occurred",
                Suggestion = "Consider more proactive healing before raidwides"
            });
        }

        // High overheal (for healers)
        if (metrics.OverhealPercent > 30f && metrics.TotalHealing > 0)
        {
            var severity = metrics.OverhealPercent > 50f ? IssueSeverity.Warning : IssueSeverity.Info;
            issues.Add(new PerformanceIssue
            {
                Type = IssueType.HighOverheal,
                Severity = severity,
                Description = $"Overheal was {metrics.OverhealPercent:F1}%",
                Suggestion = "Wait longer before healing, use smaller heals, or deal more damage"
            });
        }

        // Cooldown drift
        foreach (var cooldown in metrics.Cooldowns)
        {
            if (cooldown.AverageDrift > 5f)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = IssueType.CooldownDrift,
                    Severity = cooldown.AverageDrift > 10f ? IssueSeverity.Warning : IssueSeverity.Info,
                    Description = $"{cooldown.Name} drifted {cooldown.AverageDrift:F1}s on average",
                    Suggestion = $"Use {cooldown.Name} more promptly when available",
                    ActionId = cooldown.ActionId
                });
            }

            // Unused ability
            if (cooldown.TimesUsed == 0 && cooldown.OptimalUses > 0)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = IssueType.AbilityUnused,
                    Severity = IssueSeverity.Warning,
                    Description = $"{cooldown.Name} was never used",
                    Suggestion = $"Use {cooldown.Name} - it could have been used {cooldown.OptimalUses} time(s)",
                    ActionId = cooldown.ActionId
                });
            }
        }

        return issues.OrderByDescending(i => i.Severity).ToList();
    }

    public CombatMetricsSnapshot? GetCurrentSnapshot()
    {
        if (!IsTracking || combatStartTime == null)
            return null;

        var duration = CombatDuration;
        var overhealStats = combatEventService.GetOverhealStatistics();

        // Build downtime breakdown if enabled
        DowntimeBreakdown? downtimeBreakdown = null;
        if (config.TrackDowntimeBreakdown)
        {
            downtimeBreakdown = actionTracker.GetDowntimeBreakdown();
        }

        return new CombatMetricsSnapshot
        {
            CombatDuration = duration,
            GcdUptime = actionTracker.GetGcdUptime(),
            PersonalDps = duration > 0 ? (float)System.Threading.Interlocked.Read(ref totalDamageDealt) / duration : 0f,
            TotalDamage = System.Threading.Interlocked.Read(ref totalDamageDealt),
            TotalHealing = overhealStats.TotalHealing,
            OverhealPercent = overhealStats.OverhealPercent,
            Deaths = deathCount,
            NearDeaths = nearDeathCount,
            Cooldowns = BuildCooldownUsage(duration),
            Timestamp = DateTime.Now,
            DowntimeAnalysis = downtimeBreakdown
        };
    }

    public IReadOnlyList<FightSession> GetSessionHistory()
    {
        lock (historyLock)
        {
            return sessionHistory.ToList();
        }
    }

    public FightSession? GetLastSession()
    {
        lock (historyLock)
        {
            return sessionHistory.First?.Value;
        }
    }

    public PerformanceTrend? GetTrend()
    {
        lock (historyLock)
        {
            if (sessionHistory.Count < 3)
                return null;

            var sessions = sessionHistory.Take(10).ToList();

            var avgScore = sessions.Average(s => s.Score?.Overall ?? 0);
            var avgGcd = sessions.Average(s => s.FinalMetrics?.GcdUptime ?? 0);

            // Calculate trend: compare first half to second half
            var halfCount = sessions.Count / 2;
            var recentAvg = sessions.Take(halfCount).Average(s => s.Score?.Overall ?? 0);
            var olderAvg = sessions.Skip(halfCount).Average(s => s.Score?.Overall ?? 0);
            var trend = recentAvg - olderAvg;

            return new PerformanceTrend
            {
                AverageScore = avgScore,
                AverageGcdUptime = avgGcd,
                ScoreTrend = trend,
                SessionCount = sessions.Count
            };
        }
    }

    public void ClearHistory()
    {
        lock (historyLock)
        {
            sessionHistory.Clear();
        }
        log.Information("PerformanceTracker: History cleared");
    }

    public void RecordCooldownUse(uint actionId, string? context = null)
    {
        if (!IsTracking || !config.TrackedCooldowns.ContainsKey(actionId))
            return;

        var fightTime = CombatDuration;
        var phase = DeterminePhase(fightTime);

        if (!cooldownStates.TryGetValue(actionId, out var state))
        {
            state = new CooldownTrackingState();
            cooldownStates[actionId] = state;
        }

        // Calculate drift from optimal timing
        float drift = 0f;
        if (state.BecameAvailableAt.HasValue)
        {
            drift = (float)(DateTime.Now - state.BecameAvailableAt.Value).TotalSeconds;

            // If cooldown was available for a long time, record it as a missed window
            if (drift > 5f && config.TrackCooldownDetails)
            {
                state.MissedWindows.Add(new MissedOpportunityWindow
                {
                    StartFightTime = fightTime - drift,
                    EndFightTime = fightTime,
                    Reason = DetermineDelayReason()
                });
            }

            state.TotalAvailableTime += drift;
        }

        state.UseCount++;
        state.LastUseTime = DateTime.Now;
        state.DriftValues.Add(drift);
        state.BecameAvailableAt = null; // Reset availability tracking

        // Record detailed use if enabled
        if (config.TrackCooldownDetails)
        {
            state.UseRecords.Add(new CooldownUseRecord
            {
                ActionId = actionId,
                Timestamp = DateTime.Now,
                FightTimeSeconds = fightTime,
                DriftSeconds = drift,
                Phase = phase,
                WasAvailable = true,
                Context = context ?? DetermineUseContext(phase, fightTime)
            });
        }

        log.Debug("PerformanceTracker: Recorded {ActionId} use at {FightTime:F1}s, drift: {Drift:F1}s, phase: {Phase}",
            actionId, fightTime, drift, phase);
    }

    public void OnCooldownBecameReady(uint actionId)
    {
        if (!IsTracking || !config.TrackedCooldowns.ContainsKey(actionId))
            return;

        if (!cooldownStates.TryGetValue(actionId, out var state))
        {
            state = new CooldownTrackingState();
            cooldownStates[actionId] = state;
        }

        state.BecameAvailableAt = DateTime.Now;
    }

    public IReadOnlyList<CooldownAnalysis> GetCooldownAnalysis()
    {
        var lastSession = GetLastSession();
        if (lastSession?.FinalMetrics?.Cooldowns == null)
            return Array.Empty<CooldownAnalysis>();

        // Build enhanced analysis from the last session's cooldown data
        return BuildCooldownAnalysisList(lastSession.Duration);
    }

    private CooldownPhase DeterminePhase(float fightTimeSeconds)
    {
        // Opener: first 15 seconds
        if (fightTimeSeconds <= 15f)
            return CooldownPhase.Opener;

        // Use actual party coordination if available
        var burstState = partyCoordinationService?.GetBurstWindowState();
        if (burstState?.IsActive == true)
            return CooldownPhase.Burst;

        // Fall back to heuristic when coordination unavailable
        // Burst windows: roughly every 2 minutes (120s), lasting ~20s
        // Common raid buff windows: 0s (opener), 120s, 240s, 360s, etc.
        var cycleTime = fightTimeSeconds % 120f;
        if (cycleTime <= 20f)
            return CooldownPhase.Burst;

        return CooldownPhase.Sustained;
    }

    private string DetermineDelayReason()
    {
        // Check if we can determine why cooldown wasn't pressed
        // This could be expanded with more context from ActionTracker
        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer == null)
            return "Unknown";

        // Check if player was moving (simplified check)
        // A more sophisticated version would track movement state
        return "Unknown";
    }

    private string? DetermineUseContext(CooldownPhase phase, float fightTime)
    {
        return phase switch
        {
            CooldownPhase.Opener => "Opener",
            CooldownPhase.Burst => "Burst window",
            CooldownPhase.Recovery => "Recovery",
            _ => null
        };
    }

    private List<CooldownAnalysis> BuildCooldownAnalysisList(float duration)
    {
        var result = new List<CooldownAnalysis>();

        foreach (var (actionId, cooldownDuration) in config.TrackedCooldowns)
        {
            if (!cooldownStates.TryGetValue(actionId, out var state))
                state = new CooldownTrackingState();

            var optimalUses = (int)Math.Floor(duration / cooldownDuration) + 1;
            var avgDrift = state.DriftValues.Count > 0 ? state.DriftValues.Average() : 0f;
            var totalDrift = state.DriftValues.Sum();

            // Count uses by phase
            var openerUses = state.UseRecords.Count(r => r.Phase == CooldownPhase.Opener);
            var burstUses = state.UseRecords.Count(r => r.Phase == CooldownPhase.Burst);
            var sustainedUses = state.UseRecords.Count(r => r.Phase == CooldownPhase.Sustained);

            // Build missed opportunities from recorded windows
            var missedOpportunities = state.MissedWindows
                .Where(w => w.Duration >= 5f) // Only count significant missed windows
                .Select(w => new MissedCooldownOpportunity
                {
                    ActionId = actionId,
                    AbilityName = GetActionName(actionId),
                    FightTimeSeconds = w.StartFightTime,
                    AvailableForSeconds = w.Duration,
                    Reason = w.Reason
                })
                .ToList();

            // Determine primary issue
            var efficiency = optimalUses > 0 ? (float)state.UseCount / optimalUses * 100f : 100f;
            var primaryIssue = DeterminePrimaryIssue(efficiency, avgDrift, missedOpportunities.Count);

            // Generate tip based on issue
            var tip = GenerateTip(primaryIssue, avgDrift, missedOpportunities.Count);

            result.Add(new CooldownAnalysis
            {
                ActionId = actionId,
                Name = GetActionName(actionId),
                CooldownDuration = cooldownDuration,
                TimesUsed = state.UseCount,
                OptimalUses = optimalUses,
                AverageDrift = avgDrift,
                Uses = state.UseRecords.ToList(),
                MissedOpportunities = missedOpportunities,
                OpenerUses = openerUses,
                BurstUses = burstUses,
                SustainedUses = sustainedUses,
                TotalDriftSeconds = totalDrift,
                PrimaryIssue = primaryIssue,
                Tip = tip
            });
        }

        return result;
    }

    private static string DeterminePrimaryIssue(float efficiency, float avgDrift, int missedCount)
    {
        if (efficiency >= 90f && avgDrift < 3f)
            return "Good";

        if (efficiency < 50f)
            return "Missed";

        if (avgDrift > 5f)
            return "Drift";

        if (missedCount > 0)
            return "Gaps";

        return "Good";
    }

    private static string? GenerateTip(string primaryIssue, float avgDrift, int missedCount)
    {
        return primaryIssue switch
        {
            "Drift" => $"Average {avgDrift:F1}s delay - use immediately when available",
            "Missed" => "Use this ability more - track cooldown with UI or audio cue",
            "Gaps" => $"{missedCount} window(s) where ability sat unused",
            "Good" => null,
            _ => null
        };
    }

    private string GetActionName(uint actionId)
    {
        if (_actionNameCache.TryGetValue(actionId, out var cached)) return cached;

        var actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (actionSheet == null)
            return $"Action {actionId}";

        var row = actionSheet.GetRowOrDefault(actionId);
        if (!row.HasValue)
        {
            var fallback = $"Action {actionId}";
            _actionNameCache[actionId] = fallback;
            return fallback;
        }

        var name = row.Value.Name.ToString();
        name = string.IsNullOrEmpty(name) ? $"Action {actionId}" : name;
        _actionNameCache[actionId] = name;
        return name;
    }

    private void LoadPersistedSessions()
    {
        try
        {
            var loaded = SessionPersistence.Load(SessionsFilePath);
            if (loaded.Count == 0) return;
            lock (historyLock)
            {
                foreach (var session in loaded)
                    sessionHistory.AddLast(session);
                while (sessionHistory.Count > config.MaxSessionHistory)
                    sessionHistory.RemoveLast();
            }
            log.Information("PerformanceTracker: Loaded {Count} session(s) from disk", loaded.Count);
        }
        catch (Exception ex)
        {
            log.Warning(ex, "PerformanceTracker: Failed to load persisted sessions");
        }
    }

    private void SaveSessionsToDisk()
    {
        if (string.IsNullOrEmpty(configDirectory)) return;
        try
        {
            List<FightSession> snapshot;
            lock (historyLock) { snapshot = sessionHistory.ToList(); }
            SessionPersistence.Save(SessionsFilePath, snapshot);
        }
        catch (Exception ex)
        {
            log.Warning(ex, "PerformanceTracker: Failed to save sessions to disk");
        }
    }

    public void Dispose()
    {
        SaveSessionsToDisk();
        combatEventService.OnLocalPlayerDamageDealt -= OnLocalPlayerDamageDealt;
        combatEventService.OnLocalPlayerHealLanded -= OnHealLanded;
    }

    /// <summary>
    /// Internal state for tracking a single cooldown.
    /// </summary>
    private sealed class CooldownTrackingState
    {
        public int UseCount { get; set; }
        public DateTime? LastUseTime { get; set; }
        public List<float> DriftValues { get; } = new();

        // Enhanced tracking for v3.3.0
        public List<CooldownUseRecord> UseRecords { get; } = new();
        public DateTime? BecameAvailableAt { get; set; }
        public float TotalAvailableTime { get; set; }
        public List<MissedOpportunityWindow> MissedWindows { get; } = new();
    }

    /// <summary>
    /// Tracks a window where a cooldown was available but not used.
    /// </summary>
    private sealed class MissedOpportunityWindow
    {
        public float StartFightTime { get; set; }
        public float EndFightTime { get; set; }
        public float Duration => EndFightTime - StartFightTime;
        public string Reason { get; set; } = "Unknown";
    }
}

public static class SessionPersistence
{
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void Save(string filePath, IEnumerable<FightSession> sessions)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(sessions.ToList(), Options);
        System.IO.File.WriteAllText(filePath, json);
    }

    public static List<FightSession> Load(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return new();
        try
        {
            var json = System.IO.File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<List<FightSession>>(json, Options) ?? new();
        }
        catch (System.Text.Json.JsonException) { return new(); }
        catch (System.IO.IOException) { return new(); }
    }
}
