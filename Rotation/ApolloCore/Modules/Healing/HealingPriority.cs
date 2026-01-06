namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Internal priority ordering for healing handlers.
/// Lower values execute first.
/// </summary>
public enum HealingPriority
{
    /// <summary>Emergency full heal (Benediction).</summary>
    Benediction = 10,

    /// <summary>AoE oGCD when party needs healing (Assize).</summary>
    AssizeHealing = 15,

    /// <summary>Lethal debuff cleanse (Esuna).</summary>
    Esuna = 20,

    /// <summary>Spike damage prediction healing.</summary>
    PreemptiveHeal = 30,

    /// <summary>Multi-target healing (Medica, Cure III, etc.).</summary>
    AoEHeal = 40,

    /// <summary>Single-target GCD heals.</summary>
    SingleHeal = 50,

    /// <summary>HoT maintenance (Regen).</summary>
    Regen = 60,

    /// <summary>oGCD single-target heal (Tetragrammaton).</summary>
    Tetragrammaton = 70,

    /// <summary>Lily cap prevention (use Lilies before they cap out).</summary>
    LilyCapPrevention = 80
}
