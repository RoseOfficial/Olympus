namespace Olympus.Services.Tank;

/// <summary>
/// Service for planning tank defensive cooldown usage.
/// </summary>
public interface ITankCooldownService
{
    /// <summary>
    /// Returns true if a defensive cooldown should be used based on current HP and incoming damage.
    /// </summary>
    /// <param name="hpPercent">Current HP percentage (0-1).</param>
    /// <param name="incomingDamageRate">Estimated damage per second.</param>
    /// <param name="hasActiveMitigation">Whether any mitigation buff is currently active.</param>
    bool ShouldUseMitigation(float hpPercent, float incomingDamageRate, bool hasActiveMitigation);

    /// <summary>
    /// Returns true if a major cooldown (Sentinel/Nebula/Shadow Wall/etc.) should be used.
    /// Major cooldowns are typically saved for tank busters or heavy damage phases.
    /// </summary>
    bool ShouldUseMajorCooldown(float hpPercent, float incomingDamageRate);

    /// <summary>
    /// Returns true if the short cooldown mitigation (Sheltron/Raw Intuition/etc.) should be used.
    /// These are typically used more freely.
    /// </summary>
    /// <param name="hpPercent">Current HP percentage.</param>
    /// <param name="gaugeValue">Current gauge value for resource-based mitigations.</param>
    /// <param name="minGauge">Minimum gauge required to use the ability.</param>
    bool ShouldUseShortCooldown(float hpPercent, int gaugeValue, int minGauge);

    /// <summary>
    /// Updates the service state. Called each frame.
    /// </summary>
    void Update(float hpPercent, float incomingDamageRate);
}
