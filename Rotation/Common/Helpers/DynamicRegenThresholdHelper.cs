using Olympus.Config;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared helper for raising regen-style HoT apply thresholds when the
/// party is taking sustained damage. Used by WHM Regen, AST Aspected
/// Benefic, and any future healer handler that wants "apply the HoT
/// earlier than usual because big damage is landing".
/// </summary>
public static class DynamicRegenThresholdHelper
{
    /// <summary>
    /// Returns the effective HP threshold for applying a regen-style HoT.
    /// When <see cref="HealingConfig.EnableDynamicRegenThreshold"/> is on
    /// and the party is taking at least
    /// <see cref="HealingConfig.RegenHighDamageDpsThreshold"/> DPS, the
    /// threshold is raised to
    /// <see cref="HealingConfig.RegenHighDamageThreshold"/> so the HoT is
    /// ticking before the next hit lands. Otherwise the base threshold
    /// (caller-supplied per-action default) is returned unchanged.
    /// </summary>
    public static float GetEffectiveThreshold(
        HealingConfig healing,
        IDamageIntakeService damageIntakeService,
        float baseThreshold)
    {
        if (!healing.EnableDynamicRegenThreshold)
            return baseThreshold;

        var partyDamageRate = damageIntakeService.GetPartyDamageRate(3f);
        if (partyDamageRate < healing.RegenHighDamageDpsThreshold)
            return baseThreshold;

        // Only raise — never lower — the caller's base threshold.
        return baseThreshold > healing.RegenHighDamageThreshold
            ? baseThreshold
            : healing.RegenHighDamageThreshold;
    }
}
