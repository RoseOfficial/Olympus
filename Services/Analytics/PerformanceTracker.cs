using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;

namespace Olympus.Services.Analytics;

/// <summary>
/// Real-time performance tracker that collects combat metrics.
/// Integrates with ActionTracker and CombatEventService for data.
/// </summary>
public sealed class PerformanceTracker : IPerformanceTracker, IDisposable
{
    private readonly AnalyticsConfig config;
    private readonly ActionTracker actionTracker;
    private readonly CombatEventService combatEventService;
    private readonly IObjectTable objectTable;
    private readonly IPartyList partyList;
    private readonly IPluginLog log;
    private readonly IDataManager dataManager;

    // Session history (most recent first)
    private readonly LinkedList<FightSession> sessionHistory = new();
    private readonly object historyLock = new();

    // Current combat tracking state
    private bool isInCombat;
    private DateTime? combatStartTime;
    private uint currentJobId;
    private string currentZoneName = "Unknown";

    // Real-time metrics accumulators
    private long totalDamageDealt;
    private int deathCount;
    private int nearDeathCount;

    // HP tracking for near-death detection
    private readonly Dictionary<uint, float> lastHpPercent = new();

    // Cooldown tracking
    private readonly Dictionary<uint, CooldownTrackingState> cooldownStates = new();

    public bool IsTracking => isInCombat && config.EnableTracking;
    public float CombatDuration => combatEventService.GetCombatDurationSeconds();

    public event Action<FightSession>? OnSessionCompleted;
    public event Action<uint, float>? OnNearDeath;
    public event Action<uint>? OnDeath;

    public PerformanceTracker(
        AnalyticsConfig config,
        ActionTracker actionTracker,
        CombatEventService combatEventService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPluginLog log,
        IDataManager dataManager)
    {
        this.config = config;
        this.actionTracker = actionTracker;
        this.combatEventService = combatEventService;
        this.objectTable = objectTable;
        this.partyList = partyList;
        this.log = log;
        this.dataManager = dataManager;

        // Subscribe to combat events
        combatEventService.OnDamageReceived += OnDamageReceived;
        combatEventService.OnLocalPlayerHealLanded += OnHealLanded;
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
        }

        combatStartTime = null;
    }

    private void UpdateCombatTracking()
    {
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

    private void OnDamageReceived(uint entityId, int damageAmount)
    {
        // For DPS tracking - only count damage from local player
        // CombatEventService fires this for all damage received, so we skip it here
        // DPS tracking would need a separate hook or event for damage dealt
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

        // Build metrics snapshot
        var metrics = new CombatMetricsSnapshot
        {
            CombatDuration = duration,
            GcdUptime = actionTracker.GetGcdUptime(),
            PersonalDps = 0, // Would need damage dealt tracking
            TotalDamage = totalDamageDealt,
            TotalHealing = overhealStats.TotalHealing,
            OverhealPercent = overhealStats.OverhealPercent,
            Deaths = deathCount,
            NearDeaths = nearDeathCount,
            Cooldowns = BuildCooldownUsage(duration),
            Timestamp = endTime
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

        return new CombatMetricsSnapshot
        {
            CombatDuration = duration,
            GcdUptime = actionTracker.GetGcdUptime(),
            PersonalDps = 0,
            TotalDamage = totalDamageDealt,
            TotalHealing = overhealStats.TotalHealing,
            OverhealPercent = overhealStats.OverhealPercent,
            Deaths = deathCount,
            NearDeaths = nearDeathCount,
            Cooldowns = BuildCooldownUsage(duration),
            Timestamp = DateTime.Now
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

    private string GetActionName(uint actionId)
    {
        var actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (actionSheet == null)
            return $"Action {actionId}";

        var row = actionSheet.GetRowOrDefault(actionId);
        if (!row.HasValue)
            return $"Action {actionId}";

        var name = row.Value.Name.ToString();
        return string.IsNullOrEmpty(name) ? $"Action {actionId}" : name;
    }

    public void Dispose()
    {
        combatEventService.OnDamageReceived -= OnDamageReceived;
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
    }
}
