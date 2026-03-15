namespace Olympus.Windows.Training.Tabs;

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.Training;

/// <summary>
/// Lessons tab: structured educational content for all jobs.
/// </summary>
public static class LessonsTab
{
    // Colors for UI elements
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 TipColor = new(0.6f, 0.4f, 0.9f, 1.0f);
    private static readonly Vector4 LockedColor = new(0.5f, 0.5f, 0.5f, 1.0f);
    private static readonly Vector4 AbilityColor = new(0.9f, 0.7f, 0.3f, 1.0f);

    // State
    private static string selectedJob = "whm";
    private static string? selectedLessonId;
    private static ITrainingService? cachedTrainingService;

    /// <summary>
    /// Navigates to a specific lesson by ID. Called from Recommendations tab.
    /// </summary>
    public static void NavigateToLesson(string lessonId)
    {
        var lesson = cachedTrainingService?.GetLesson(lessonId);
        if (lesson == null)
            return;

        selectedJob = lesson.JobPrefix;
        selectedLessonId = lessonId;
    }

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        cachedTrainingService = trainingService;

        // Job tabs - organized by role
        if (ImGui.BeginTabBar("LessonRoleTabs"))
        {
            DrawLessonRoleTab("Healer", [("whm", "WHM"), ("sch", "SCH"), ("ast", "AST"), ("sge", "SGE")]);
            DrawLessonRoleTab("Tank", [("pld", "PLD"), ("war", "WAR"), ("drk", "DRK"), ("gnb", "GNB")]);
            DrawLessonRoleTab("Melee", [("drg", "DRG"), ("nin", "NIN"), ("sam", "SAM"), ("mnk", "MNK"), ("rpr", "RPR"), ("vpr", "VPR")]);
            DrawLessonRoleTab("Ranged", [("mch", "MCH"), ("brd", "BRD"), ("dnc", "DNC")]);
            DrawLessonRoleTab("Caster", [("blm", "BLM"), ("smn", "SMN"), ("rdm", "RDM"), ("pct", "PCT")]);
            ImGui.EndTabBar();
        }

        ImGui.Spacing();

        // Get lessons for selected job
        var lessons = trainingService.GetLessonsForJob(selectedJob);
        if (lessons.Count == 0)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.NoLessonsAvailable, "No lessons available for this job."));
            return;
        }

        // Auto-select first lesson if none selected or wrong job
        if (selectedLessonId == null || !selectedLessonId.StartsWith(selectedJob))
        {
            selectedLessonId = lessons[0].LessonId;
        }

        // Learning Path guidance panel
        DrawLearningPathGuidance(trainingService, config, selectedJob);

        ImGui.Spacing();

        // Two-column layout: lesson list (left) and detail (right)
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var listWidth = Math.Min(200f, availableWidth * 0.4f);

        // Left panel: Lesson list
        if (ImGui.BeginChild("LessonList", new Vector2(listWidth, -1), true))
        {
            DrawLessonList(lessons, trainingService, config);
        }

        ImGui.EndChild();

        ImGui.SameLine();

        // Right panel: Lesson detail
        if (ImGui.BeginChild("LessonDetail", new Vector2(-1, -1), true))
        {
            DrawLessonDetail(trainingService, config);
        }

        ImGui.EndChild();
    }

    private static void DrawLessonRoleTab(string roleLabel, (string Prefix, string Label)[] jobs)
    {
        if (ImGui.BeginTabItem(roleLabel))
        {
            if (ImGui.BeginTabBar($"Lesson{roleLabel}Jobs"))
            {
                foreach (var (prefix, label) in jobs)
                {
                    if (ImGui.BeginTabItem(label))
                    {
                        selectedJob = prefix;
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }

            ImGui.EndTabItem();
        }
    }

    private static void DrawLessonList(System.Collections.Generic.IReadOnlyList<LessonDefinition> lessons, ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Training.LessonsHeader, "Lessons"));
        ImGui.Separator();
        ImGui.Spacing();

        // Calculate overall progress for this job
        var completedCount = lessons.Count(l => config.CompletedLessons.Contains(l.LessonId));
        var progressFraction = lessons.Count > 0 ? (float)completedCount / lessons.Count : 0f;
        ImGui.ProgressBar(progressFraction, new Vector2(-1, 0), $"{completedCount}/{lessons.Count}");
        ImGui.Spacing();

        foreach (var lesson in lessons)
        {
            var isComplete = config.CompletedLessons.Contains(lesson.LessonId);
            var isLocked = !AreLessonPrerequisitesMet(lesson, config);
            var isSelected = selectedLessonId == lesson.LessonId;

            // Build display string
            string statusIcon;
            Vector4 textColor;
            if (isLocked)
            {
                statusIcon = "[L]";
                textColor = LockedColor;
            }
            else if (isComplete)
            {
                statusIcon = "[x]";
                textColor = GoodColor;
            }
            else
            {
                statusIcon = "[ ]";
                textColor = NeutralColor;
            }

            var displayText = $"{statusIcon} {lesson.LessonNumber}. {lesson.Title}";

            // Highlight selected
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, InfoColor);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            }

            if (ImGui.Selectable(displayText, isSelected))
            {
                selectedLessonId = lesson.LessonId;
            }

            ImGui.PopStyleColor();

            // Tooltip for locked lessons
            if (isLocked && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(Loc.T(LocalizedStrings.Training.PrerequisitesNotMet, "Prerequisites not met:"));
                foreach (var prereq in lesson.Prerequisites)
                {
                    var prereqLesson = trainingService.GetLesson(prereq);
                    if (prereqLesson != null && !config.CompletedLessons.Contains(prereq))
                    {
                        ImGui.BulletText(Loc.TFormat(LocalizedStrings.Training.CompleteFirstFormat, "Complete \"{0}\" first", prereqLesson.Title));
                    }
                }

                ImGui.EndTooltip();
            }
        }
    }

    private static void DrawLessonDetail(ITrainingService trainingService, TrainingConfig config)
    {
        var lesson = trainingService.GetLesson(selectedLessonId ?? string.Empty);
        if (lesson == null)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.SelectLessonToView, "Select a lesson to view details."));
            return;
        }

        var isComplete = config.CompletedLessons.Contains(lesson.LessonId);
        var isLocked = !AreLessonPrerequisitesMet(lesson, config);

        // Title with completion status
        ImGui.TextColored(InfoColor, Loc.TFormat(LocalizedStrings.Training.LessonTitleFormat, "Lesson {0}: {1}", lesson.LessonNumber.ToString(), lesson.Title));
        ImGui.SameLine();
        if (isComplete)
        {
            ImGui.TextColored(GoodColor, Loc.T(LocalizedStrings.Training.Completed, "(Completed)"));
        }
        else if (isLocked)
        {
            ImGui.TextColored(LockedColor, Loc.T(LocalizedStrings.Training.Locked, "(Locked)"));
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Concept progress
        var learnedConcepts = lesson.ConceptsCovered.Count(c => config.LearnedConcepts.Contains(c));
        var totalConcepts = lesson.ConceptsCovered.Length;
        ImGui.Text(Loc.TFormat(LocalizedStrings.Training.ConceptProgressFormat, "Concept Progress: {0}/{1}", learnedConcepts.ToString(), totalConcepts.ToString()));
        ImGui.ProgressBar(totalConcepts > 0 ? (float)learnedConcepts / totalConcepts : 0f, new Vector2(-1, 0));
        ImGui.Spacing();

        // Locked message
        if (isLocked)
        {
            ImGui.TextColored(WarningColor, Loc.T(LocalizedStrings.Training.CompletePrerequisites, "Complete the prerequisite lessons to unlock this content."));
            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.Prerequisites, "Prerequisites:"));
            foreach (var prereq in lesson.Prerequisites)
            {
                var prereqLesson = trainingService.GetLesson(prereq);
                if (prereqLesson != null)
                {
                    var prereqComplete = config.CompletedLessons.Contains(prereq);
                    var color = prereqComplete ? GoodColor : WarningColor;
                    var status = prereqComplete ? "[x]" : "[ ]";
                    ImGui.TextColored(color, $"  {status} {prereqLesson.Title}");
                }
            }

            return;
        }

        // Scrollable content area
        if (ImGui.BeginChild("LessonContent", new Vector2(-1, -40), false))
        {
            // Description
            ImGui.TextWrapped(lesson.Description);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Key Points
            if (lesson.KeyPoints.Length > 0)
            {
                ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.KeyPoints, "KEY POINTS:"));
                ImGui.Spacing();
                foreach (var point in lesson.KeyPoints)
                {
                    ImGui.BulletText(point);
                }

                ImGui.Spacing();
            }

            // Related Abilities
            if (lesson.RelatedAbilities.Length > 0)
            {
                ImGui.TextColored(AbilityColor, Loc.T(LocalizedStrings.Training.Abilities, "ABILITIES:"));
                ImGui.SameLine();
                ImGui.TextWrapped(string.Join(", ", lesson.RelatedAbilities));
                ImGui.Spacing();
            }

            // Tips
            if (lesson.Tips.Length > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(TipColor, Loc.T(LocalizedStrings.Training.Tips, "TIPS:"));
                ImGui.Spacing();
                foreach (var tip in lesson.Tips)
                {
                    ImGui.TextColored(TipColor, $"  - {tip}");
                }

                ImGui.Spacing();
            }

            // Concepts covered (expandable)
            if (lesson.ConceptsCovered.Length > 0)
            {
                ImGui.Spacing();
                if (ImGui.CollapsingHeader(Loc.T(LocalizedStrings.Training.ConceptsCovered, "Concepts Covered")))
                {
                    foreach (var concept in lesson.ConceptsCovered)
                    {
                        var conceptLearned = config.LearnedConcepts.Contains(concept);
                        var conceptName = FormatConceptName(concept);
                        var color = conceptLearned ? GoodColor : NeutralColor;
                        var status = conceptLearned ? "[x]" : "[ ]";
                        ImGui.TextColored(color, $"  {status} {conceptName}");

                        // Allow marking concept as learned
                        if (!conceptLearned)
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Learn, "Learn") + $"##{concept}"))
                            {
                                trainingService.MarkConceptLearned(concept);
                            }
                        }
                    }
                }
            }
        }

        ImGui.EndChild();

        // Bottom buttons
        ImGui.Separator();
        ImGui.Spacing();

        if (isComplete)
        {
            if (ImGui.Button(Loc.T(LocalizedStrings.Training.MarkIncomplete, "Mark Incomplete")))
            {
                config.CompletedLessons.Remove(lesson.LessonId);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.MarkIncompleteTooltip, "Remove completion status from this lesson."));
            }
        }
        else
        {
            if (ImGui.Button(Loc.T(LocalizedStrings.Training.MarkComplete, "Mark Complete")))
            {
                config.CompletedLessons.Add(lesson.LessonId);

                // Also mark all concepts as learned when completing a lesson
                foreach (var concept in lesson.ConceptsCovered)
                {
                    if (!config.LearnedConcepts.Contains(concept))
                    {
                        trainingService.MarkConceptLearned(concept);
                    }
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.MarkCompleteTooltip, "Mark this lesson as completed. This will also mark all concepts as learned."));
            }
        }

        ImGui.SameLine();

        // Learn all concepts button
        var unlearnedConcepts = lesson.ConceptsCovered.Where(c => !config.LearnedConcepts.Contains(c)).ToArray();
        if (unlearnedConcepts.Length > 0)
        {
            if (ImGui.Button(Loc.TFormat(LocalizedStrings.Training.LearnAllConceptsFormat, "Learn All Concepts ({0})", unlearnedConcepts.Length.ToString())))
            {
                foreach (var concept in unlearnedConcepts)
                {
                    trainingService.MarkConceptLearned(concept);
                }
            }
        }
    }

    private static void DrawLearningPathGuidance(ITrainingService trainingService, TrainingConfig config, string jobPrefix)
    {
        var recommendation = trainingService.GetNextRecommendedLesson(jobPrefix);

        // Header panel
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.2f, 1.0f));
        if (ImGui.BeginChild("LearningPathPanel", new Vector2(-1, 95), true))
        {
            // Header row with title and skill badge
            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.LearningPath, "Learning Path"));
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 90);
            DrawSkillBadge(recommendation.SkillLevel);

            ImGui.Spacing();

            // Progress bar
            var progressFraction = recommendation.TotalLessons > 0
                ? (float)recommendation.CompletedLessons / recommendation.TotalLessons
                : 0f;
            ImGui.ProgressBar(progressFraction, new Vector2(-1, 0), Loc.TFormat(LocalizedStrings.Training.CompletedProgressFormat, "{0}/{1} completed", recommendation.CompletedLessons.ToString(), recommendation.TotalLessons.ToString()));

            ImGui.Spacing();

            // Recommendation
            if (recommendation.RecommendedLessonId != null)
            {
                var lesson = trainingService.GetLesson(recommendation.RecommendedLessonId);
                if (lesson != null)
                {
                    // Reason type indicator
                    var reasonColor = recommendation.ReasonType switch
                    {
                        LearningPathReason.AddressStrugglingConcept => WarningColor,
                        LearningPathReason.StartFromBeginning => GoodColor,
                        _ => NeutralColor,
                    };

                    ImGui.TextColored(GoodColor, Loc.T(LocalizedStrings.Training.RecommendedNext, "Recommended Next:"));
                    ImGui.SameLine();
                    ImGui.Text($"{lesson.LessonNumber}. {lesson.Title}");

                    ImGui.TextColored(reasonColor, $"  {recommendation.Reason}");

                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - 100);
                    if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.StartThisLesson, "Start This Lesson")))
                    {
                        selectedLessonId = recommendation.RecommendedLessonId;
                    }
                }
            }
            else if (recommendation.ReasonType == LearningPathReason.AllComplete)
            {
                ImGui.TextColored(GoodColor, Loc.T(LocalizedStrings.Training.AllLessonsComplete, "All lessons completed!"));
                ImGui.TextColored(NeutralColor, recommendation.Reason);
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static void DrawSkillBadge(SkillLevel skillLevel)
    {
        var (label, color) = skillLevel switch
        {
            SkillLevel.Beginner => (Loc.T(LocalizedStrings.Training.SkillBeginner, "Beginner"), new Vector4(0.6f, 0.6f, 0.6f, 1.0f)),
            SkillLevel.Intermediate => (Loc.T(LocalizedStrings.Training.SkillIntermediate, "Intermediate"), InfoColor),
            SkillLevel.Advanced => (Loc.T(LocalizedStrings.Training.SkillAdvanced, "Advanced"), GoodColor),
            _ => (Loc.T(LocalizedStrings.Training.SkillUnknown, "Unknown"), NeutralColor),
        };

        ImGui.PushStyleColor(ImGuiCol.Button, color with { W = 0.3f });
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color with { W = 0.4f });
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color with { W = 0.5f });
        ImGui.PushStyleColor(ImGuiCol.Text, color);

        ImGui.SmallButton(label);

        ImGui.PopStyleColor(4);
    }

    private static bool AreLessonPrerequisitesMet(LessonDefinition lesson, TrainingConfig config)
    {
        if (lesson.Prerequisites.Length == 0)
            return true;

        return lesson.Prerequisites.All(prereq => config.CompletedLessons.Contains(prereq));
    }

    private static string FormatConceptName(string conceptId)
    {
        // Convert "whm.emergency_healing" to "Emergency Healing"
        var parts = conceptId.Split('.');
        if (parts.Length > 1)
        {
            var name = parts[^1].Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }

        return conceptId;
    }
}
