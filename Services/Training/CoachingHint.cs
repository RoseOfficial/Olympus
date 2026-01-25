namespace Olympus.Services.Training;

using System;

/// <summary>
/// Represents a real-time coaching hint shown during combat.
/// Hints are contextual tips for struggling concepts that appear as the player practices.
/// </summary>
public sealed class CoachingHint
{
    /// <summary>
    /// Unique identifier for this hint instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the hint was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// The concept ID this hint is for (e.g., "whm.lily_management").
    /// </summary>
    public string ConceptId { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable concept name for display.
    /// </summary>
    public string ConceptName { get; init; } = string.Empty;

    /// <summary>
    /// Short actionable tip text (1-2 sentences).
    /// </summary>
    public string TipText { get; init; } = string.Empty;

    /// <summary>
    /// Optional recommended action to take now.
    /// </summary>
    public string? RecommendedAction { get; init; }

    /// <summary>
    /// Priority of this hint (affects display order and styling).
    /// </summary>
    public HintPriority Priority { get; init; } = HintPriority.Normal;

    /// <summary>
    /// Whether this hint has been dismissed by the user.
    /// </summary>
    public bool IsDismissed { get; set; }

    /// <summary>
    /// The current success rate for this concept (0.0 to 1.0).
    /// </summary>
    public float ConceptSuccessRate { get; init; }

    /// <summary>
    /// Duration in seconds before the hint auto-dismisses (0 = never auto-dismiss).
    /// </summary>
    public float DisplayDurationSeconds { get; init; } = 8f;

    /// <summary>
    /// Whether this hint should auto-dismiss after DisplayDurationSeconds.
    /// </summary>
    public bool AutoDismiss => DisplayDurationSeconds > 0;

    /// <summary>
    /// Elapsed time since creation in seconds.
    /// </summary>
    public float ElapsedSeconds => (float)(DateTime.Now - CreatedAt).TotalSeconds;

    /// <summary>
    /// Whether this hint has expired (should be removed).
    /// </summary>
    public bool IsExpired => AutoDismiss && ElapsedSeconds >= DisplayDurationSeconds;

    /// <summary>
    /// Remaining display time as a fraction (1.0 = full, 0.0 = expired).
    /// </summary>
    public float RemainingFraction => AutoDismiss
        ? Math.Max(0f, 1f - (ElapsedSeconds / DisplayDurationSeconds))
        : 1f;
}

/// <summary>
/// Priority level for coaching hints (affects styling and display order).
/// </summary>
public enum HintPriority
{
    /// <summary>
    /// Low priority - general tips and reminders.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard coaching hints.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - important concepts the player is struggling with.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - urgent tips for concepts with very low success rates.
    /// </summary>
    Critical = 3,
}

/// <summary>
/// Static tip library for each concept.
/// Contains pre-written tips that can be shown when a concept is exercised.
/// </summary>
public static class ConceptTips
{
    /// <summary>
    /// Gets a tip for a concept based on the current context.
    /// </summary>
    /// <param name="conceptId">The concept ID to get a tip for.</param>
    /// <param name="successRate">Current success rate for context-aware tips.</param>
    /// <returns>A tip string, or null if no tip is available.</returns>
    public static string? GetTipForConcept(string conceptId, float successRate)
    {
        // Extract the base concept name (after the job prefix)
        var conceptName = conceptId.Contains('.')
            ? conceptId.Substring(conceptId.IndexOf('.') + 1)
            : conceptId;

        return conceptName.ToLowerInvariant() switch
        {
            // WHM Concepts
            "emergency_healing" => successRate < 0.5f
                ? "Save Benediction for true emergencies (<25% HP). Don't panic heal!"
                : "Good emergency heal timing! Keep watching for critical moments.",
            "lily_management" => "Blood Lily is ready! Use Afflatus Misery for big DPS.",
            "ogcd_weaving" => "Weave oGCDs between GCDs - never clip your GCD timer.",
            "benediction_usage" => "Benediction is your panic button. Optimal use is 20-25% HP.",
            "assize_usage" => "Assize deals damage AND heals. Use it on cooldown for DPS!",
            "tetragrammaton_usage" => "Tetra is free healing. Use it often on the tank.",
            "regen_maintenance" => "Keep Regen on the tank between pulls. Free healing!",
            "temperance_usage" => "Temperance before raidwides gives party mitigation + heal boost.",
            "glare_priority" => "ABC: Always Be Casting. Glare when nobody needs healing.",
            "dot_maintenance" => "Keep Dia up on the boss. 30s of free damage!",

            // SCH Concepts
            "aetherflow_management" => "Don't cap Aetherflow stacks! Use them or Energy Drain.",
            "lustrate_usage" => "Lustrate is instant. Perfect for emergency tank healing.",
            "sacred_soil_usage" => "Sacred Soil: 10% mitigation + regen. Great for party damage.",
            "excogitation_usage" => "Excog triggers automatically at 50% HP. Pre-cast before busters!",
            "chain_stratagem_timing" => "Chain Stratagem during party burst windows for max DPS.",

            // AST Concepts
            "card_management" => "Draw on cooldown! Don't let cards cap.",
            "earthly_star_placement" => "Place Earthly Star 10-15s before raidwides to cook.",
            "essential_dignity_usage" => "Essential Dignity heals more on low HP targets.",
            "divination_timing" => "Align Divination with party 2-minute burst windows.",

            // SGE Concepts
            "addersgall_management" => "Addersgall regenerates every 20s. Don't cap at 3!",
            "kardia_management" => "Keep Kardia on whoever is taking damage.",
            "kerachole_usage" => "Kerachole: party mitigation + regen. Use before raidwides.",
            "taurochole_usage" => "Taurochole is your best single-target heal. Use it often.",

            // Tank General
            "mitigation_stacking" => "Don't stack all mitigations at once. Rotate them!",
            "invuln_timing" => "Invulns are powerful but long CD. Save for big hits.",
            "tank_swap" => "Watch for swap mechanics. Provoke takes hate instantly.",

            // DPS General
            "positionals" => "Positionals matter! Flank and rear give bonus damage.",
            "burst_window" or "burst_alignment" => "Save cooldowns for party buff windows!",
            "aoe_rotation" => "Use AoE rotation at 3+ enemies for better damage.",

            // Default
            _ => null,
        };
    }

    /// <summary>
    /// Gets a recommended action for a concept based on the current state.
    /// </summary>
    /// <param name="conceptId">The concept ID.</param>
    /// <returns>An action string, or null if no action is recommended.</returns>
    public static string? GetRecommendedAction(string conceptId)
    {
        var conceptName = conceptId.Contains('.')
            ? conceptId.Substring(conceptId.IndexOf('.') + 1)
            : conceptId;

        return conceptName.ToLowerInvariant() switch
        {
            "lily_management" => "Use Afflatus Misery now!",
            "assize_usage" => "Use Assize on cooldown",
            "aetherflow_management" => "Spend stacks or Energy Drain",
            "earthly_star_placement" => "Pre-place Earthly Star",
            "addersgall_management" => "Use a heal to prevent capping",
            "card_management" => "Draw a card now",
            "burst_window" or "burst_alignment" => "Align buffs with party",
            _ => null,
        };
    }
}
