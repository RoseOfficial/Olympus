namespace Olympus.Windows;

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Services.Training;
using Olympus.Windows.Training.Tabs;

/// <summary>
/// Training Mode window - real-time coaching and decision explanations.
/// </summary>
public sealed class TrainingWindow : Window
{
    private readonly ITrainingService trainingService;
    private readonly Configuration configuration;

    public TrainingWindow(ITrainingService trainingService, Configuration configuration)
        : base("Olympus Training", ImGuiWindowFlags.NoSavedSettings)
    {
        this.trainingService = trainingService;
        this.configuration = configuration;

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
        // Header with controls
        DrawHeader();

        ImGui.Separator();

        // Tab bar
        if (ImGui.BeginTabBar("TrainingTabs"))
        {
            if (ImGui.BeginTabItem("Live Coaching"))
            {
                ImGui.Spacing();
                LiveCoachingTab.Draw(this.trainingService, this.configuration.Training);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Progress"))
            {
                ImGui.Spacing();
                DrawProgressTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawHeader()
    {
        // Training toggle
        var enabled = this.trainingService.IsTrainingEnabled;
        if (ImGui.Checkbox("Enable Training Mode", ref enabled))
        {
            this.trainingService.IsTrainingEnabled = enabled;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("When enabled, captures and explains rotation decisions in real-time.");
        }

        ImGui.SameLine();

        // Settings dropdown
        DrawSettingsDropdown();

        ImGui.SameLine();

        // Clear button
        if (ImGui.Button("Clear"))
        {
            this.trainingService.ClearExplanations();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Clear all captured explanations.");
        }
    }

    private void DrawSettingsDropdown()
    {
        if (ImGui.BeginCombo("##TrainingSettings", "Settings", ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text("Display Options");
            ImGui.Separator();

            var showAlts = this.configuration.Training.ShowAlternatives;
            if (ImGui.Checkbox("Show Alternatives", ref showAlts))
            {
                this.configuration.Training.ShowAlternatives = showAlts;
            }

            var showTips = this.configuration.Training.ShowTips;
            if (ImGui.Checkbox("Show Tips", ref showTips))
            {
                this.configuration.Training.ShowTips = showTips;
            }

            ImGui.Spacing();
            ImGui.Text("Verbosity");
            ImGui.Separator();

            var verbosity = (int)this.configuration.Training.Verbosity;
            if (ImGui.RadioButton("Minimal", ref verbosity, (int)ExplanationVerbosity.Minimal))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Minimal;
            }

            if (ImGui.RadioButton("Normal", ref verbosity, (int)ExplanationVerbosity.Normal))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Normal;
            }

            if (ImGui.RadioButton("Detailed", ref verbosity, (int)ExplanationVerbosity.Detailed))
            {
                this.configuration.Training.Verbosity = ExplanationVerbosity.Detailed;
            }

            ImGui.Spacing();
            ImGui.Text("Priority Filter");
            ImGui.Separator();

            var minPriority = (int)this.configuration.Training.MinimumPriorityToShow;
            if (ImGui.RadioButton("All", ref minPriority, (int)ExplanationPriority.Low))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Low;
            }

            if (ImGui.RadioButton("Normal+", ref minPriority, (int)ExplanationPriority.Normal))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Normal;
            }

            if (ImGui.RadioButton("High+", ref minPriority, (int)ExplanationPriority.High))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.High;
            }

            if (ImGui.RadioButton("Critical Only", ref minPriority, (int)ExplanationPriority.Critical))
            {
                this.configuration.Training.MinimumPriorityToShow = ExplanationPriority.Critical;
            }

            ImGui.Spacing();
            ImGui.Text("Sections");
            ImGui.Separator();
            DrawSectionToggle("CurrentAction", "Current Action");
            DrawSectionToggle("DecisionFactors", "Decision Factors");
            DrawSectionToggle("Alternatives", "Alternatives");
            DrawSectionToggle("Tips", "Tips");
            DrawSectionToggle("RecentHistory", "Recent History");

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
        ImGui.Text("Learning Progress");
        ImGui.Separator();

        var progressFraction = progress.TotalConcepts > 0 ? (float)progress.LearnedConcepts / progress.TotalConcepts : 0f;
        ImGui.ProgressBar(progressFraction, new Vector2(-1, 0), $"{progress.LearnedConcepts}/{progress.TotalConcepts} concepts");

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"Progress: {progress.ProgressPercent:F0}%");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Recently demonstrated concepts
        if (progress.RecentlyDemonstratedConcepts.Length > 0)
        {
            ImGui.Text("Recently Seen:");
            foreach (var concept in progress.RecentlyDemonstratedConcepts)
            {
                var isLearned = this.configuration.Training.LearnedConcepts.Contains(concept);
                var displayName = FormatConceptName(concept);

                if (isLearned)
                {
                    ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), $"  [Learned] {displayName}");
                }
                else
                {
                    ImGui.Text($"  {displayName}");
                    ImGui.SameLine();
                    if (ImGui.SmallButton($"Mark Learned##{concept}"))
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
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.3f, 1.0f), "Needs Review (seen 10+ times):");
            foreach (var concept in progress.ConceptsNeedingAttention)
            {
                var displayName = FormatConceptName(concept);
                ImGui.Text($"  {displayName}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"Mark Learned##{concept}"))
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
