namespace Olympus.Windows.Training;

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Floating overlay window for real-time coaching hints.
/// Positioned near the party list for easy visibility during combat.
/// </summary>
public sealed class HintOverlay : Window
{
    private readonly RealTimeCoachingService coachingService;
    private readonly TrainingConfig config;

    // Colors for hint display
    private static readonly Vector4 CriticalColor = new(0.95f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 HighColor = new(0.95f, 0.65f, 0.2f, 1.0f);
    private static readonly Vector4 NormalColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 LowColor = new(0.6f, 0.6f, 0.6f, 1.0f);
    private static readonly Vector4 TipColor = new(0.9f, 0.9f, 0.9f, 1.0f);
    private static readonly Vector4 ActionColor = new(0.4f, 0.9f, 0.4f, 1.0f);
    private static readonly Vector4 DimColor = new(0.5f, 0.5f, 0.5f, 0.8f);
    private static readonly Vector4 BackgroundColor = new(0.1f, 0.1f, 0.15f, 0.9f);

    public HintOverlay(RealTimeCoachingService coachingService, TrainingConfig config)
        : base(
            "##OlympusHintOverlay",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoMove)
    {
        this.coachingService = coachingService;
        this.config = config;

        // Start visible if enabled
        this.IsOpen = config.HintOverlayVisible;

        // Position based on config
        this.Position = new Vector2(config.HintOverlayX, config.HintOverlayY);
        this.PositionCondition = ImGuiCond.FirstUseEver;

        // Size constraints
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 50),
            MaximumSize = new Vector2(350, 150),
        };
    }

    public override void PreDraw()
    {
        // Update visibility from config
        if (this.IsOpen != (this.config.HintOverlayVisible && this.config.EnableCoachingHints))
        {
            this.IsOpen = this.config.HintOverlayVisible && this.config.EnableCoachingHints;
        }
    }

    public override void Draw()
    {
        // Only draw if coaching is enabled and there's a hint to show
        var hint = coachingService.CurrentHint;
        if (hint == null)
        {
            // Show minimal placeholder when no hints
            ImGui.PushStyleColor(ImGuiCol.Text, DimColor);
            ImGui.Text("No coaching tips right now");
            ImGui.PopStyleColor();
            return;
        }

        DrawHint(hint);
    }

    private void DrawHint(CoachingHint hint)
    {
        var priorityColor = GetPriorityColor(hint.Priority);

        // Progress bar showing time remaining
        if (hint.AutoDismiss)
        {
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, priorityColor with { W = 0.6f });
            ImGui.ProgressBar(hint.RemainingFraction, new Vector2(-1, 3), string.Empty);
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        // Concept name header
        ImGui.PushStyleColor(ImGuiCol.Text, priorityColor);
        var priorityIcon = hint.Priority switch
        {
            HintPriority.Critical => "[!]",
            HintPriority.High => "[*]",
            _ => "",
        };
        if (!string.IsNullOrEmpty(priorityIcon))
        {
            ImGui.Text(priorityIcon);
            ImGui.SameLine();
        }
        ImGui.Text(hint.ConceptName);
        ImGui.PopStyleColor();

        // Success rate indicator
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, DimColor);
        ImGui.Text($"({hint.ConceptSuccessRate:P0})");
        ImGui.PopStyleColor();

        ImGui.Spacing();

        // Tip text
        ImGui.PushStyleColor(ImGuiCol.Text, TipColor);
        ImGui.TextWrapped(hint.TipText);
        ImGui.PopStyleColor();

        // Recommended action (if any)
        if (!string.IsNullOrEmpty(hint.RecommendedAction))
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, ActionColor);
            ImGui.Text($"> {hint.RecommendedAction}");
            ImGui.PopStyleColor();
        }

        ImGui.Spacing();

        // Dismiss button
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.4f, 0.7f));
        if (ImGui.SmallButton("Dismiss"))
        {
            coachingService.DismissHint(hint.Id);
        }
        ImGui.PopStyleColor(2);

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, DimColor);
        ImGui.Text("ESC to close all");
        ImGui.PopStyleColor();
    }

    private static Vector4 GetPriorityColor(HintPriority priority) => priority switch
    {
        HintPriority.Critical => CriticalColor,
        HintPriority.High => HighColor,
        HintPriority.Normal => NormalColor,
        HintPriority.Low => LowColor,
        _ => NormalColor,
    };

    /// <summary>
    /// Called each frame to handle keyboard shortcuts.
    /// </summary>
    public void HandleInput()
    {
        // ESC dismisses all hints
        if (ImGui.IsKeyPressed(ImGuiKey.Escape) && coachingService.ActiveHints.Count > 0)
        {
            coachingService.DismissAllHints();
        }
    }

    public override void OnClose()
    {
        this.config.HintOverlayVisible = false;
    }

    public override void OnOpen()
    {
        this.config.HintOverlayVisible = true;
    }
}
