namespace Olympus.Windows.Training.Tabs;

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Lessons tab: structured educational content for healers.
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

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Job tabs
        if (ImGui.BeginTabBar("LessonJobTabs"))
        {
            if (ImGui.BeginTabItem("WHM"))
            {
                selectedJob = "whm";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("SCH"))
            {
                selectedJob = "sch";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("AST"))
            {
                selectedJob = "ast";
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("SGE"))
            {
                selectedJob = "sge";
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Spacing();

        // Get lessons for selected job
        var lessons = LessonRegistry.GetLessonsForJob(selectedJob);
        if (lessons.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "No lessons available for this job.");
            return;
        }

        // Auto-select first lesson if none selected or wrong job
        if (selectedLessonId == null || !selectedLessonId.StartsWith(selectedJob))
        {
            selectedLessonId = lessons[0].LessonId;
        }

        // Two-column layout: lesson list (left) and detail (right)
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var listWidth = Math.Min(200f, availableWidth * 0.4f);

        // Left panel: Lesson list
        if (ImGui.BeginChild("LessonList", new Vector2(listWidth, -1), true))
        {
            DrawLessonList(lessons, config);
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

    private static void DrawLessonList(System.Collections.Generic.IReadOnlyList<LessonDefinition> lessons, TrainingConfig config)
    {
        ImGui.Text("Lessons");
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
                ImGui.Text("Prerequisites not met:");
                foreach (var prereq in lesson.Prerequisites)
                {
                    var prereqLesson = LessonRegistry.GetLesson(prereq);
                    if (prereqLesson != null && !config.CompletedLessons.Contains(prereq))
                    {
                        ImGui.BulletText($"Complete \"{prereqLesson.Title}\" first");
                    }
                }

                ImGui.EndTooltip();
            }
        }
    }

    private static void DrawLessonDetail(ITrainingService trainingService, TrainingConfig config)
    {
        var lesson = LessonRegistry.GetLesson(selectedLessonId ?? string.Empty);
        if (lesson == null)
        {
            ImGui.TextColored(NeutralColor, "Select a lesson to view details.");
            return;
        }

        var isComplete = config.CompletedLessons.Contains(lesson.LessonId);
        var isLocked = !AreLessonPrerequisitesMet(lesson, config);

        // Title with completion status
        ImGui.TextColored(InfoColor, $"Lesson {lesson.LessonNumber}: {lesson.Title}");
        ImGui.SameLine();
        if (isComplete)
        {
            ImGui.TextColored(GoodColor, "(Completed)");
        }
        else if (isLocked)
        {
            ImGui.TextColored(LockedColor, "(Locked)");
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Concept progress
        var learnedConcepts = lesson.ConceptsCovered.Count(c => config.LearnedConcepts.Contains(c));
        var totalConcepts = lesson.ConceptsCovered.Length;
        ImGui.Text($"Concept Progress: {learnedConcepts}/{totalConcepts}");
        ImGui.ProgressBar(totalConcepts > 0 ? (float)learnedConcepts / totalConcepts : 0f, new Vector2(-1, 0));
        ImGui.Spacing();

        // Locked message
        if (isLocked)
        {
            ImGui.TextColored(WarningColor, "Complete the prerequisite lessons to unlock this content.");
            ImGui.Spacing();
            ImGui.Text("Prerequisites:");
            foreach (var prereq in lesson.Prerequisites)
            {
                var prereqLesson = LessonRegistry.GetLesson(prereq);
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
                ImGui.TextColored(InfoColor, "KEY POINTS:");
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
                ImGui.TextColored(AbilityColor, "ABILITIES:");
                ImGui.SameLine();
                ImGui.TextWrapped(string.Join(", ", lesson.RelatedAbilities));
                ImGui.Spacing();
            }

            // Tips
            if (lesson.Tips.Length > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(TipColor, "TIPS:");
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
                if (ImGui.CollapsingHeader("Concepts Covered"))
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
                            if (ImGui.SmallButton($"Learn##{concept}"))
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
            if (ImGui.Button("Mark Incomplete"))
            {
                config.CompletedLessons.Remove(lesson.LessonId);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Remove completion status from this lesson.");
            }
        }
        else
        {
            if (ImGui.Button("Mark Complete"))
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
                ImGui.SetTooltip("Mark this lesson as completed. This will also mark all concepts as learned.");
            }
        }

        ImGui.SameLine();

        // Learn all concepts button
        var unlearnedConcepts = lesson.ConceptsCovered.Where(c => !config.LearnedConcepts.Contains(c)).ToArray();
        if (unlearnedConcepts.Length > 0)
        {
            if (ImGui.Button($"Learn All Concepts ({unlearnedConcepts.Length})"))
            {
                foreach (var concept in unlearnedConcepts)
                {
                    trainingService.MarkConceptLearned(concept);
                }
            }
        }
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
