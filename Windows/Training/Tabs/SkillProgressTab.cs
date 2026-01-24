namespace Olympus.Windows.Training.Tabs;

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Skill Progress tab: displays skill level detection results and adaptive explanation controls.
/// </summary>
public static class SkillProgressTab
{
    // Colors for UI elements
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);

    // Skill level colors
    private static readonly Vector4 BeginnerColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 IntermediateColor = new(0.9f, 0.7f, 0.3f, 1.0f);
    private static readonly Vector4 AdvancedColor = new(0.3f, 0.9f, 0.3f, 1.0f);

    // All supported job prefixes
    private static readonly string[] AllJobPrefixes =
    {
        // Healers
        "whm", "sch", "ast", "sge",
        // Tanks
        "pld", "war", "drk", "gnb",
        // Melee DPS
        "drg", "nin", "sam", "mnk", "rpr", "vpr",
        // Ranged Physical DPS
        "mch", "brd", "dnc",
        // Casters
        "blm", "smn", "rdm", "pct",
    };

    private static readonly string[] JobDisplayNames =
    {
        // Healers
        "White Mage", "Scholar", "Astrologian", "Sage",
        // Tanks
        "Paladin", "Warrior", "Dark Knight", "Gunbreaker",
        // Melee DPS
        "Dragoon", "Ninja", "Samurai", "Monk", "Reaper", "Viper",
        // Ranged Physical DPS
        "Machinist", "Bard", "Dancer",
        // Casters
        "Black Mage", "Summoner", "Red Mage", "Pictomancer",
    };

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Adaptive explanations toggle
        DrawAdaptiveSettings(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Per-job skill level display
        DrawJobSkillLevels(trainingService, config);
    }

    private static void DrawAdaptiveSettings(TrainingConfig config)
    {
        ImGui.Text("Adaptive Explanations");
        ImGui.Separator();

        var enableAdaptive = config.EnableAdaptiveExplanations;
        if (ImGui.Checkbox("Enable Adaptive Verbosity", ref enableAdaptive))
        {
            config.EnableAdaptiveExplanations = enableAdaptive;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "When enabled, explanation verbosity automatically adjusts based on your skill level:\n" +
                "- Beginners see detailed explanations for all decisions\n" +
                "- Intermediate players see normal detail, with extra detail for unfamiliar concepts\n" +
                "- Advanced players see minimal detail, except for new or critical decisions");
        }

        if (config.EnableAdaptiveExplanations)
        {
            ImGui.Spacing();

            // Override dropdown
            ImGui.Text("Skill Level Override:");
            ImGui.SameLine();

            var overrideOptions = new[] { "Auto-detect", "Beginner", "Intermediate", "Advanced" };
            var currentOverride = config.SkillLevelOverride.HasValue
                ? (int)config.SkillLevelOverride.Value + 1
                : 0;

            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("##SkillOverride", ref currentOverride, overrideOptions, overrideOptions.Length))
            {
                config.SkillLevelOverride = currentOverride switch
                {
                    1 => SkillLevelOverride.Beginner,
                    2 => SkillLevelOverride.Intermediate,
                    3 => SkillLevelOverride.Advanced,
                    _ => null,
                };
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    "Override the detected skill level:\n" +
                    "- Auto-detect: Uses quiz and lesson progress to determine level\n" +
                    "- Beginner/Intermediate/Advanced: Force a specific level");
            }
        }
    }

    private static void DrawJobSkillLevels(ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text("Skill Levels by Job");
        ImGui.Separator();

        if (!config.EnableAdaptiveExplanations)
        {
            ImGui.TextColored(NeutralColor, "Enable adaptive explanations above to see skill levels.");
            return;
        }

        // Find jobs with any progress
        var jobsWithProgress = AllJobPrefixes
            .Select((prefix, index) => (Prefix: prefix, Name: JobDisplayNames[index], Result: trainingService.GetSkillLevel(prefix)))
            .Where(j => j.Result.PassedQuizzes > 0 || j.Result.CompletedLessonsCount > 0)
            .ToArray();

        if (jobsWithProgress.Length == 0)
        {
            ImGui.TextColored(NeutralColor, "No progress yet. Complete lessons and quizzes to see your skill levels.");
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, "Go to the Lessons tab to get started!");
            return;
        }

        // Display each job with progress
        foreach (var job in jobsWithProgress)
        {
            DrawJobSkillLevel(job.Prefix, job.Name, job.Result);
            ImGui.Spacing();
        }

        // Show hint for other jobs
        var jobsWithoutProgress = AllJobPrefixes.Length - jobsWithProgress.Length;
        if (jobsWithoutProgress > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, $"{jobsWithoutProgress} other jobs with no progress yet.");
        }
    }

    private static void DrawJobSkillLevel(string jobPrefix, string jobName, SkillLevelResult result)
    {
        // Job header with level badge
        var levelColor = result.Level switch
        {
            SkillLevel.Beginner => BeginnerColor,
            SkillLevel.Intermediate => IntermediateColor,
            SkillLevel.Advanced => AdvancedColor,
            _ => NeutralColor,
        };

        if (ImGui.TreeNode($"{jobName} ({jobPrefix.ToUpperInvariant()})##{jobPrefix}"))
        {
            // Skill level and score
            ImGui.TextColored(levelColor, $"Level: {result.Level}");
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, $"(Score: {result.CompositeScore:F0}/100)");

            // Score breakdown
            ImGui.Spacing();
            ImGui.Text("Score Breakdown:");

            // Quiz pass rate (40%)
            DrawScoreComponent("Quiz Pass Rate", result.QuizPassRate, 40,
                $"{result.PassedQuizzes}/{result.TotalQuizzes} quizzes passed");

            // Quiz quality (25%)
            DrawScoreComponent("Quiz Quality", result.QuizQuality, 25,
                "Average score on passed quizzes");

            // Lessons completed (25%)
            DrawScoreComponent("Lessons Completed", result.LessonsCompleted, 25,
                $"{result.CompletedLessonsCount}/{result.TotalLessons} lessons done");

            // Concepts learned (10%)
            DrawScoreComponent("Concepts Learned", result.ConceptsLearned, 10,
                "Marked as learned");

            // Engagement penalty warning
            if (result.EngagementPenaltyApplied)
            {
                ImGui.Spacing();
                ImGui.TextColored(WarningColor, "Note: -25% penalty applied (lessons completed without quizzes)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Take quizzes to validate your understanding and remove this penalty.");
                }
            }

            ImGui.TreePop();
        }
        else
        {
            // Compact display when collapsed
            ImGui.SameLine();
            ImGui.TextColored(levelColor, $"[{result.Level}]");
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, $"Score: {result.CompositeScore:F0}");
        }
    }

    private static void DrawScoreComponent(string label, float value, int weight, string tooltip)
    {
        ImGui.Text($"  {label}:");
        ImGui.SameLine(180);

        // Progress bar
        var barColor = value switch
        {
            >= 75 => GoodColor,
            >= 40 => WarningColor,
            _ => NeutralColor,
        };

        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.ProgressBar(value / 100f, new Vector2(100, 0), $"{value:F0}%");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextColored(NeutralColor, $"({weight}% weight)");

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
