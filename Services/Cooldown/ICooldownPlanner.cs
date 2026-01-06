namespace Olympus.Services.Cooldown;

/// <summary>
/// Cooldown priority levels for resource management.
/// </summary>
public enum CooldownPriority
{
    /// <summary>Hold cooldown, not needed.</summary>
    Hold = 0,

    /// <summary>Low priority, can use if nothing else needed.</summary>
    Low = 1,

    /// <summary>Medium priority, use when appropriate.</summary>
    Medium = 2,

    /// <summary>High priority, should use soon.</summary>
    High = 3,

    /// <summary>Emergency, use immediately.</summary>
    Emergency = 4
}

/// <summary>
/// Abstraction for cooldown planning across all healer jobs.
/// Provides a unified interface for making defensive and resource cooldown decisions.
/// </summary>
public interface ICooldownPlanner
{
    /// <summary>
    /// Updates the planner with current party state.
    /// Should be called once per frame before making cooldown decisions.
    /// </summary>
    /// <param name="avgPartyHpPercent">Average party HP percentage.</param>
    /// <param name="lowestHpPercent">Lowest party member HP percentage.</param>
    /// <param name="injuredCount">Number of injured party members.</param>
    /// <param name="criticalCount">Number of critically low party members.</param>
    void Update(float avgPartyHpPercent, float lowestHpPercent, int injuredCount, int criticalCount);

    /// <summary>
    /// Determines if a major defensive cooldown should be used.
    /// Major defensives are party-wide mitigation (e.g., Temperance, Neutral Sect).
    /// </summary>
    bool ShouldUseMajorDefensive();

    /// <summary>
    /// Determines if a minor defensive cooldown should be used.
    /// Minor defensives are single-target mitigation (e.g., Divine Benison, Exaltation).
    /// </summary>
    bool ShouldUseMinorDefensive();

    /// <summary>
    /// Determines if resources should be conserved.
    /// Active when MP is low or major healing may be needed soon.
    /// </summary>
    bool ShouldConserveResources();

    /// <summary>
    /// Checks if the party is in emergency mode.
    /// Emergency mode triggers when multiple party members are critically low.
    /// </summary>
    bool IsInEmergencyMode();

    /// <summary>
    /// Gets the priority for using a specific cooldown type.
    /// </summary>
    /// <param name="cooldownType">The type of cooldown to evaluate.</param>
    /// <returns>The priority level for using this cooldown.</returns>
    CooldownPriority GetCooldownPriority(string cooldownType);

    /// <summary>
    /// Checks if damage is expected to spike soon based on trend analysis.
    /// </summary>
    bool IsDamageSpikeExpected();

    /// <summary>
    /// Gets the urgency level for party healing (0.0 = no urgency, 1.0 = critical).
    /// </summary>
    float GetHealingUrgency();
}
