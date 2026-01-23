namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Olympus.Config;

/// <summary>
/// Core implementation of Training Mode - captures rotation decisions and provides explanations.
/// </summary>
public sealed class TrainingService : ITrainingService
{
    private readonly TrainingConfig config;
    private readonly IObjectTable objectTable;
    private readonly IPluginLog? log;

    private readonly List<ActionExplanation> explanations = new();
    private readonly object explanationsLock = new();

    private bool wasInCombat;

    public TrainingService(
        TrainingConfig config,
        IObjectTable objectTable,
        IPluginLog? log = null)
    {
        this.config = config;
        this.objectTable = objectTable;
        this.log = log;
    }

    public bool IsTrainingEnabled
    {
        get => config.EnableTraining;
        set => config.EnableTraining = value;
    }

    public bool IsInCombat { get; private set; }

    public IReadOnlyList<ActionExplanation> RecentExplanations
    {
        get
        {
            lock (this.explanationsLock)
            {
                return this.explanations.ToList();
            }
        }
    }

    public ActionExplanation? CurrentExplanation
    {
        get
        {
            lock (this.explanationsLock)
            {
                return this.explanations.FirstOrDefault();
            }
        }
    }

    public void RecordDecision(ActionExplanation explanation)
    {
        if (!this.config.EnableTraining)
            return;

        // Filter by priority
        if (explanation.Priority < this.config.MinimumPriorityToShow)
            return;

        lock (this.explanationsLock)
        {
            // Add to front (most recent first)
            this.explanations.Insert(0, explanation);

            // Trim to max size
            while (this.explanations.Count > this.config.MaxExplanationsToShow)
            {
                this.explanations.RemoveAt(this.explanations.Count - 1);
            }
        }

        // Track concept exposure
        if (!string.IsNullOrEmpty(explanation.ConceptId))
        {
            if (this.config.ConceptExposureCount.TryGetValue(explanation.ConceptId, out var count))
            {
                this.config.ConceptExposureCount[explanation.ConceptId] = count + 1;
            }
            else
            {
                this.config.ConceptExposureCount[explanation.ConceptId] = 1;
            }
        }

        this.log?.Debug("Training: Recorded {ActionName} - {Reason}", explanation.ActionName, explanation.ShortReason);
    }

    public LearningProgress GetProgress()
    {
        var allConcepts = GetAllHealerConcepts();
        var learned = this.config.LearnedConcepts.Count(c => allConcepts.Contains(c));

        // Find concepts with high exposure but not learned (>10 exposures)
        var needingAttention = this.config.ConceptExposureCount
            .Where(kvp => kvp.Value >= 10 && !this.config.LearnedConcepts.Contains(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToArray();

        // Find recently demonstrated concepts (from current explanations)
        string[] recentlyDemonstrated;
        lock (this.explanationsLock)
        {
            recentlyDemonstrated = this.explanations
                .Where(e => !string.IsNullOrEmpty(e.ConceptId))
                .Select(e => e.ConceptId!)
                .Distinct()
                .Take(5)
                .ToArray();
        }

        return new LearningProgress
        {
            TotalConcepts = allConcepts.Length,
            LearnedConcepts = learned,
            ConceptsNeedingAttention = needingAttention,
            RecentlyDemonstratedConcepts = recentlyDemonstrated,
        };
    }

    /// <summary>
    /// Gets all healer concepts across WHM, SCH, AST, and SGE.
    /// </summary>
    private static string[] GetAllHealerConcepts()
    {
        return WhmConcepts.AllConcepts
            .Concat(SchConcepts.AllConcepts)
            .Concat(AstConcepts.AllConcepts)
            .Concat(SgeConcepts.AllConcepts)
            .ToArray();
    }

    /// <summary>
    /// Gets concepts for a specific job based on concept ID prefix.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch", "ast", "sge").</param>
    public static string[] GetConceptsForJob(string jobPrefix)
    {
        return jobPrefix.ToLowerInvariant() switch
        {
            "whm" => WhmConcepts.AllConcepts,
            "sch" => SchConcepts.AllConcepts,
            "ast" => AstConcepts.AllConcepts,
            "sge" => SgeConcepts.AllConcepts,
            _ => Array.Empty<string>(),
        };
    }

    public void MarkConceptLearned(string conceptId)
    {
        this.config.LearnedConcepts.Add(conceptId);
        this.log?.Information("Training: Marked concept as learned: {Concept}", conceptId);
    }

    public void UnmarkConceptLearned(string conceptId)
    {
        this.config.LearnedConcepts.Remove(conceptId);
        this.log?.Information("Training: Unmarked concept: {Concept}", conceptId);
    }

    public void ClearExplanations()
    {
        lock (this.explanationsLock)
        {
            this.explanations.Clear();
        }

        this.log?.Debug("Training: Cleared all explanations");
    }

    public void Update()
    {
        // Update combat state
        var player = this.objectTable.LocalPlayer;
        this.IsInCombat = player?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) ?? false;

        // Clear explanations when combat ends
        if (this.wasInCombat && !this.IsInCombat)
        {
            // Keep explanations for a bit after combat for review
            // They'll naturally be cleared when new combat starts and fills the queue
        }

        // Clear when entering new combat
        if (!this.wasInCombat && this.IsInCombat)
        {
            this.ClearExplanations();
        }

        this.wasInCombat = this.IsInCombat;
    }
}
