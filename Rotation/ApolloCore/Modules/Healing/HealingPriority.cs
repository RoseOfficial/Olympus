namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Internal priority ordering for healing handlers.
/// Lower values execute first.
/// </summary>
/// <remarks>
/// Priority order rationale:
/// 1. Emergency (Benediction) - Absolute priority for life-saving
/// 2. AoE oGCD (Assize) - Free healing while dealing damage
/// 3. Esuna - Cleanse lethal debuffs before they kill
/// 4. oGCD single-target (Tetragrammaton) - Free heals before GCDs
/// 5. Preemptive - Prepare for incoming damage
/// 6. Regen - HoT maintenance before GCD heals to prevent overhealing
/// 7. AoE GCD - Multi-target healing
/// 8. Single GCD - Single-target GCD heals (most expensive resource)
/// 9. Lily cap prevention - Use Lilies before they cap (lowest priority)
/// </remarks>
public enum HealingPriority
{
    /// <summary>Emergency full heal (Benediction).</summary>
    Benediction = 10,

    /// <summary>AoE oGCD when party needs healing (Assize).</summary>
    AssizeHealing = 15,

    /// <summary>Lethal debuff cleanse (Esuna).</summary>
    Esuna = 20,

    /// <summary>oGCD single-target heal (Tetragrammaton) - execute before GCD heals.</summary>
    Tetragrammaton = 25,

    /// <summary>Spike damage prediction healing.</summary>
    PreemptiveHeal = 30,

    /// <summary>HoT maintenance (Regen) - apply before GCD heals to prevent overhealing.</summary>
    Regen = 35,

    /// <summary>Multi-target healing (Medica, Cure III, etc.).</summary>
    AoEHeal = 40,

    /// <summary>Single-target GCD heals.</summary>
    SingleHeal = 50,

    /// <summary>Blood Lily building (prefer Lily heals when at 2 Blood Lilies).</summary>
    BloodLilyBuilding = 60,

    /// <summary>Lily cap prevention (use Lilies before they cap out).</summary>
    LilyCapPrevention = 80
}
