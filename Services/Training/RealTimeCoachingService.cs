namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;

/// <summary>
/// Service for generating real-time coaching hints during combat.
/// Monitors concept applications and generates contextual tips for struggling concepts.
/// </summary>
public sealed class RealTimeCoachingService
{
    private readonly TrainingConfig config;
    private readonly ITrainingService trainingService;
    private readonly IPluginLog? log;

    private readonly List<CoachingHint> activeHints = new();
    private readonly object hintsLock = new();

    private readonly Dictionary<string, DateTime> lastHintTimeByJob = new();
    private readonly Dictionary<string, DateTime> lastHintTimeByConcept = new();

    // Throttling constants
    private const float DefaultHintCooldownSeconds = 10f;
    private const float ConceptCooldownSeconds = 60f; // Don't repeat same concept within 60s
    private const float StrugglingThreshold = 0.60f; // 60% success rate

    private string currentJobPrefix = string.Empty;

    public RealTimeCoachingService(
        TrainingConfig config,
        ITrainingService trainingService,
        IPluginLog? log = null)
    {
        this.config = config;
        this.trainingService = trainingService;
        this.log = log;
    }

    /// <summary>
    /// Whether coaching hints are enabled.
    /// </summary>
    public bool IsEnabled => config.EnableCoachingHints && trainingService.IsTrainingEnabled;

    /// <summary>
    /// The current active hints (thread-safe copy).
    /// </summary>
    public IReadOnlyList<CoachingHint> ActiveHints
    {
        get
        {
            lock (this.hintsLock)
            {
                return this.activeHints.Where(h => !h.IsDismissed && !h.IsExpired).ToList();
            }
        }
    }

    /// <summary>
    /// The most recent hint (for overlay display).
    /// </summary>
    public CoachingHint? CurrentHint
    {
        get
        {
            lock (this.hintsLock)
            {
                return this.activeHints
                    .Where(h => !h.IsDismissed && !h.IsExpired)
                    .OrderByDescending(h => h.Priority)
                    .ThenByDescending(h => h.CreatedAt)
                    .FirstOrDefault();
            }
        }
    }

    /// <summary>
    /// Sets the current job for hint generation context.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch").</param>
    public void SetCurrentJob(string jobPrefix)
    {
        if (!string.IsNullOrEmpty(jobPrefix))
        {
            this.currentJobPrefix = jobPrefix.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Called when a concept is exercised (opportunity or application).
    /// Generates a hint if the concept is struggling and throttling allows.
    /// </summary>
    /// <param name="conceptId">The concept that was exercised.</param>
    /// <param name="wasSuccessful">Whether the application was successful.</param>
    public void OnConceptExercised(string conceptId, bool wasSuccessful)
    {
        if (!IsEnabled || string.IsNullOrEmpty(conceptId))
            return;

        // Only generate hints for failures or struggling concepts
        if (wasSuccessful)
            return;

        // Check if this concept is struggling
        var mastery = trainingService.GetConceptMastery(currentJobPrefix);
        if (!mastery.StrugglingConcepts.Contains(conceptId))
            return;

        // Get success rate for this concept
        var successRate = GetConceptSuccessRate(conceptId);

        // Check throttling
        if (!CanShowHint(conceptId))
            return;

        // Generate and add hint
        var hint = CreateHint(conceptId, successRate);
        if (hint != null)
        {
            AddHint(hint);
        }
    }

    /// <summary>
    /// Proactively checks for concepts that need attention and generates hints.
    /// Called periodically during combat.
    /// </summary>
    public void CheckForHintOpportunities()
    {
        if (!IsEnabled || string.IsNullOrEmpty(currentJobPrefix))
            return;

        // Only check if we're not on cooldown
        if (!CanShowHintForJob())
            return;

        // Get struggling concepts for current job
        var strugglingConcepts = trainingService.GetStrugglingConcepts(currentJobPrefix);
        if (strugglingConcepts.Length == 0)
            return;

        // Find the worst struggling concept that we haven't hinted recently
        foreach (var conceptId in strugglingConcepts)
        {
            if (!CanShowHintForConcept(conceptId))
                continue;

            var successRate = GetConceptSuccessRate(conceptId);
            var hint = CreateHint(conceptId, successRate);
            if (hint != null)
            {
                AddHint(hint);
                break; // Only one hint at a time
            }
        }
    }

    /// <summary>
    /// Dismisses a hint by ID.
    /// </summary>
    /// <param name="hintId">The hint ID to dismiss.</param>
    public void DismissHint(Guid hintId)
    {
        lock (this.hintsLock)
        {
            var hint = this.activeHints.FirstOrDefault(h => h.Id == hintId);
            if (hint != null)
            {
                hint.IsDismissed = true;
            }
        }
    }

    /// <summary>
    /// Dismisses all active hints.
    /// </summary>
    public void DismissAllHints()
    {
        lock (this.hintsLock)
        {
            foreach (var hint in this.activeHints)
            {
                hint.IsDismissed = true;
            }
        }
    }

    /// <summary>
    /// Clears all hints (called when combat ends).
    /// </summary>
    public void ClearHints()
    {
        lock (this.hintsLock)
        {
            this.activeHints.Clear();
        }
    }

    /// <summary>
    /// Updates hint state (removes expired/dismissed hints).
    /// Called each frame.
    /// </summary>
    public void Update()
    {
        if (!IsEnabled)
            return;

        lock (this.hintsLock)
        {
            // Remove expired and dismissed hints
            this.activeHints.RemoveAll(h => h.IsDismissed || h.IsExpired);

            // Limit to max hints
            while (this.activeHints.Count > 3)
            {
                // Remove oldest low-priority hint
                var toRemove = this.activeHints
                    .OrderBy(h => h.Priority)
                    .ThenBy(h => h.CreatedAt)
                    .First();
                this.activeHints.Remove(toRemove);
            }
        }
    }

    private CoachingHint? CreateHint(string conceptId, float successRate)
    {
        var tipText = ConceptTips.GetTipForConcept(conceptId, successRate);
        if (string.IsNullOrEmpty(tipText))
            return null;

        var conceptName = FormatConceptName(conceptId);
        var recommendedAction = ConceptTips.GetRecommendedAction(conceptId);

        // Determine priority based on success rate
        var priority = successRate switch
        {
            < 0.30f => HintPriority.Critical,
            < 0.45f => HintPriority.High,
            < 0.60f => HintPriority.Normal,
            _ => HintPriority.Low,
        };

        // Check if personality allows this hint to be shown
        var personality = config.CoachingPersonality;
        if (!PersonalityTextGenerator.ShouldShowHint(personality, priority))
            return null;

        // Apply personality to text
        var personalizedTip = PersonalityTextGenerator.GetHintMessage(personality, conceptName, tipText);
        var personalizedAction = !string.IsNullOrEmpty(recommendedAction)
            ? PersonalityTextGenerator.GetRecommendedActionMessage(personality, recommendedAction)
            : null;

        // Silent personality returns empty strings, skip hint
        if (string.IsNullOrEmpty(personalizedTip))
            return null;

        return new CoachingHint
        {
            ConceptId = conceptId,
            ConceptName = conceptName,
            TipText = personalizedTip,
            RecommendedAction = personalizedAction,
            Priority = priority,
            ConceptSuccessRate = successRate,
            DisplayDurationSeconds = config.HintDisplayDurationSeconds,
        };
    }

    private void AddHint(CoachingHint hint)
    {
        lock (this.hintsLock)
        {
            this.activeHints.Add(hint);

            // Update throttle timestamps
            this.lastHintTimeByJob[currentJobPrefix] = DateTime.Now;
            this.lastHintTimeByConcept[hint.ConceptId] = DateTime.Now;
        }

        this.log?.Debug("Coaching: Generated hint for {Concept} ({SuccessRate:P0})",
            hint.ConceptId, hint.ConceptSuccessRate);
    }

    private bool CanShowHint(string conceptId)
    {
        return CanShowHintForJob() && CanShowHintForConcept(conceptId);
    }

    private bool CanShowHintForJob()
    {
        if (string.IsNullOrEmpty(currentJobPrefix))
            return false;

        if (!this.lastHintTimeByJob.TryGetValue(currentJobPrefix, out var lastTime))
            return true;

        var elapsed = (DateTime.Now - lastTime).TotalSeconds;
        return elapsed >= config.HintCooldownSeconds;
    }

    private bool CanShowHintForConcept(string conceptId)
    {
        if (!this.lastHintTimeByConcept.TryGetValue(conceptId, out var lastTime))
            return true;

        var elapsed = (DateTime.Now - lastTime).TotalSeconds;
        return elapsed >= ConceptCooldownSeconds;
    }

    private float GetConceptSuccessRate(string conceptId)
    {
        // Access config directly since we have it
        if (config.ConceptMastery.TryGetValue(conceptId, out var data))
        {
            return data.SuccessRate;
        }

        return 0f;
    }

    private static string FormatConceptName(string conceptId)
    {
        var parts = conceptId.Split('.');
        if (parts.Length > 1)
        {
            var name = parts[^1].Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }

        return conceptId;
    }
}
