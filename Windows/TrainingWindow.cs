namespace Olympus.Windows;

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.Training;
using Olympus.Windows.Training.Tabs;

/// <summary>
/// Training Mode window - real-time coaching and decision explanations.
/// </summary>
public sealed class TrainingWindow : Window
{
    private readonly ITrainingService trainingService;
    private readonly Configuration configuration;
    private readonly DecisionValidationService? decisionValidationService;
    private readonly SpacedRepetitionService? spacedRepetitionService;

    // State for programmatic tab navigation (v3.29.0)
    private bool navigateToLessonsTab;
    private string? pendingLessonId;

    public TrainingWindow(
        ITrainingService trainingService,
        Configuration configuration,
        DecisionValidationService? decisionValidationService = null,
        SpacedRepetitionService? spacedRepetitionService = null)
        : base("Olympus Training", ImGuiWindowFlags.NoSavedSettings)
    {
        this.trainingService = trainingService;
        this.configuration = configuration;
        this.decisionValidationService = decisionValidationService;
        this.spacedRepetitionService = spacedRepetitionService;

        Size = new Vector2(450, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        this.configuration.Training.TrainingWindowVisible = true;
    }

    public override void OnClose()
    {
        this.configuration.Training.TrainingWindowVisible = false;
    }

    public override void Draw()
    {
        // Check for pending lesson navigation from SkillProgressTab (v3.29.0)
        var pendingNav = SkillProgressTab.GetPendingLessonNavigation();
        if (pendingNav != null)
        {
            this.navigateToLessonsTab = true;
            this.pendingLessonId = pendingNav;
        }

        // Header with controls
        DrawHeader();

        ImGui.Separator();

        // Tab bar
        if (ImGui.BeginTabBar("TrainingTabs"))
        {
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.LiveCoachingTab, "Live Coaching")))
            {
                ImGui.Spacing();
                LiveCoachingTab.Draw(this.trainingService, this.configuration.Training, this.decisionValidationService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.RecommendedTab, "Recommended")))
            {
                ImGui.Spacing();
                RecommendationsTab.Draw(this.trainingService, this.configuration.Training);
                ImGui.EndTabItem();
            }

            // Lessons tab with programmatic navigation support (v3.29.0)
            var lessonsFlags = this.navigateToLessonsTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.LessonsTab, "Lessons"), lessonsFlags))
            {
                // Pass pending lesson ID to LessonsTab for selection
                if (this.pendingLessonId != null)
                {
                    LessonsTab.NavigateToLesson(this.pendingLessonId);
                    this.pendingLessonId = null;
                }

                this.navigateToLessonsTab = false;

                ImGui.Spacing();
                LessonsTab.Draw(this.trainingService, this.configuration.Training);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.QuizzesTab, "Quizzes")))
            {
                ImGui.Spacing();
                QuizzesTab.Draw(this.trainingService, this.configuration.Training);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.ProgressTab, "Progress")))
            {
                ImGui.Spacing();
                DrawProgressTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Training.SkillLevelTab, "Skill Level")))
            {
                ImGui.Spacing();
                SkillProgressTab.Draw(this.trainingService, this.configuration.Training, this.spacedRepetitionService);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawHeader()
    {
        // Training toggle
        var enabled = this.trainingService.IsTrainingEnabled;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.EnableTrainingMode, "Enable Training Mode"), ref enabled))
        {
            this.trainingService.IsTrainingEnabled = enabled;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.EnableTrainingModeTooltip, "When enabled, captures and explains rotation decisions in real-time."));
        }

        ImGui.SameLine();

        // Settings dropdown
        DrawSettingsDropdown();

        ImGui.SameLine();

        // Clear button
        if (ImGui.Button(Loc.T(LocalizedStrings.Training.Clear, "Clear")))
        {
            this.trainingService.ClearExplanations();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.ClearTooltip, "Clear all captured explanations."));
        }
    }

    private void DrawSettingsDropdown()
    {
        if (ImGui.BeginCombo("##TrainingSettings", Loc.T(LocalizedStrings.Training.Settings, "Settings"), ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text(Loc.T(LocalizedStrings.Training.DisplayOptions, "Display Options"));
            ImGui.Separator();

            var showAlts = this.configuration.Training.ShowAlternatives;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.ShowAlternatives, "Show Alternatives"), ref showAlts))
            {
                this.configuration.Training.ShowAlternatives = showAlts;
            }

            var showTips = this.configuration.Training.ShowTips;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.ShowTips, "Show Tips"), ref showTips))
            {
                this.configuration.Training.ShowTips = showTips;
            }

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.Verbosity, "Verbosity"));
            ImGui.Separator();

            var verbosity = (int)this.configuration.Training.Verbosity;
            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.VerbosityMinimal, "Minimal"), ref verbosity, (int)ExplanationVerbosity.Minimal))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Minimal;
            }

            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.VerbosityNormal, "Normal"), ref verbosity, (int)ExplanationVerbosity.Normal))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Normal;
            }

            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.VerbosityDetailed, "Detailed"), ref verbosity, (int)ExplanationVerbosity.Detailed))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Detailed;
            }

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.PriorityFilter, "Priority Filter"));
            ImGui.Separator();

            var minPriority = (int)this.configuration.Training.MinimumPriorityToShow;
            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.PriorityAll, "All"), ref minPriority, (int)ExplanationPriority.Low))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Low;
            }

            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.PriorityNormalPlus, "Normal+"), ref minPriority, (int)ExplanationPriority.Normal))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Normal;
            }

            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.PriorityHighPlus, "High+"), ref minPriority, (int)ExplanationPriority.High))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.High;
            }

            if (ImGui.RadioButton(Loc.T(LocalizedStrings.Training.PriorityCriticalOnly, "Critical Only"), ref minPriority, (int)ExplanationPriority.Critical))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Critical;
            }

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.Sections, "Sections"));
            ImGui.Separator();
            DrawSectionToggle("CurrentAction", Loc.T(LocalizedStrings.Training.SectionCurrentAction, "Current Action"));
            DrawSectionToggle("DecisionFactors", Loc.T(LocalizedStrings.Training.SectionDecisionFactors, "Decision Factors"));
            DrawSectionToggle("Alternatives", Loc.T(LocalizedStrings.Training.SectionAlternatives, "Alternatives"));
            DrawSectionToggle("Tips", Loc.T(LocalizedStrings.Training.SectionTips, "Tips"));
            DrawSectionToggle("RecentHistory", Loc.T(LocalizedStrings.Training.SectionRecentHistory, "Recent History"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.CoachingHintsHeader, "Coaching Hints (v3.49)"));
            ImGui.Separator();

            var enableHints = this.configuration.Training.EnableCoachingHints;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.ShowCoachingHints, "Show Coaching Hints"), ref enableHints))
            {
                this.configuration.Training.EnableCoachingHints = enableHints;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.ShowCoachingHintsTooltip, "Show real-time hints during combat for struggling concepts."));
            }

            if (enableHints)
            {
                var hintCooldown = this.configuration.Training.HintCooldownSeconds;
                ImGui.SetNextItemWidth(80);
                if (ImGui.SliderFloat(Loc.T(LocalizedStrings.Training.HintCooldown, "Hint Cooldown"), ref hintCooldown, 5f, 60f, "%.0fs"))
                {
                    this.configuration.Training.HintCooldownSeconds = hintCooldown;
                }

                var hintDuration = this.configuration.Training.HintDisplayDurationSeconds;
                ImGui.SetNextItemWidth(80);
                if (ImGui.SliderFloat(Loc.T(LocalizedStrings.Training.HintDuration, "Hint Duration"), ref hintDuration, 3f, 30f, "%.0fs"))
                {
                    this.configuration.Training.HintDisplayDurationSeconds = hintDuration;
                }
            }

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.CoachingPersonalityHeader, "Coaching Personality (v3.51)"));
            ImGui.Separator();

            var personalityOptions = new[]
            {
                Loc.T(LocalizedStrings.Training.Encouraging, "Encouraging"),
                Loc.T(LocalizedStrings.Training.Analytical, "Analytical"),
                Loc.T(LocalizedStrings.Training.Strict, "Strict"),
                Loc.T(LocalizedStrings.Training.Silent, "Silent"),
            };
            var currentPersonality = (int)this.configuration.Training.CoachingPersonality;
            ImGui.SetNextItemWidth(120);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Training.Personality, "Personality"), ref currentPersonality, personalityOptions, personalityOptions.Length))
            {
                this.configuration.Training.CoachingPersonality = (CoachingPersonality)currentPersonality;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.PersonalityTooltip,
                    "Encouraging: Positive, supportive feedback\n" +
                    "Analytical: Data-focused, objective feedback\n" +
                    "Strict: Direct, high-standards feedback\n" +
                    "Silent: Minimal feedback, critical only"));
            }

            ImGui.EndCombo();
        }
    }

    private void DrawSectionToggle(string key, string label)
    {
        if (!this.configuration.Training.SectionVisibility.TryGetValue(key, out var visible))
            visible = true;

        if (ImGui.Checkbox(label, ref visible))
        {
            this.configuration.Training.SectionVisibility[key] = visible;
        }
    }

    private void DrawProgressTab()
    {
        var progress = this.trainingService.GetProgress();

        // Overall progress
        ImGui.Text(Loc.T(LocalizedStrings.Training.LearningProgress, "Learning Progress"));
        ImGui.Separator();

        var progressFraction = progress.TotalConcepts > 0 ? (float)progress.LearnedConcepts / progress.TotalConcepts : 0f;
        ImGui.ProgressBar(progressFraction, new Vector2(-1, 0), Loc.TFormat(LocalizedStrings.Training.ConceptsFormat, "{0}/{1} concepts", progress.LearnedConcepts.ToString(), progress.TotalConcepts.ToString()));

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Loc.TFormat(LocalizedStrings.Training.ProgressFormat, "Progress: {0}%", $"{progress.ProgressPercent:F0}"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Recently demonstrated concepts
        if (progress.RecentlyDemonstratedConcepts.Length > 0)
        {
            ImGui.Text(Loc.T(LocalizedStrings.Training.RecentlySeen, "Recently Seen:"));
            foreach (var concept in progress.RecentlyDemonstratedConcepts)
            {
                var isLearned = this.configuration.Training.LearnedConcepts.Contains(concept);
                var displayName = FormatConceptName(concept);

                if (isLearned)
                {
                    ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), $"  {Loc.T(LocalizedStrings.Training.Learned, "[Learned]")} {displayName}");
                }
                else
                {
                    ImGui.Text($"  {displayName}");
                    ImGui.SameLine();
                    if (ImGui.SmallButton($"{Loc.T(LocalizedStrings.Training.MarkLearned, "Mark Learned")}##{concept}"))
                    {
                        this.trainingService.MarkConceptLearned(concept);
                    }
                }
            }
            ImGui.Spacing();
        }

        // Concepts needing attention
        if (progress.ConceptsNeedingAttention.Length > 0)
        {
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.3f, 1.0f), Loc.T(LocalizedStrings.Training.NeedsReview, "Needs Review (seen 10+ times):"));
            foreach (var concept in progress.ConceptsNeedingAttention)
            {
                var displayName = FormatConceptName(concept);
                ImGui.Text($"  {displayName}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"{Loc.T(LocalizedStrings.Training.MarkLearned, "Mark Learned")}##{concept}"))
                {
                    this.trainingService.MarkConceptLearned(concept);
                }
            }
        }
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
