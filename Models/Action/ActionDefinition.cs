using Olympus.Services.Calculation;

namespace Olympus.Models.Action;

/// <summary>
/// Static definition of an action's properties.
/// Loaded from game data or hardcoded for known actions.
/// </summary>
public sealed class ActionDefinition
{
    /// <summary>The game's action ID.</summary>
    public required uint ActionId { get; init; }

    /// <summary>Display name of the action.</summary>
    public required string Name { get; init; }

    /// <summary>Minimum level required to use this action.</summary>
    public required byte MinLevel { get; init; }

    /// <summary>Whether this is a GCD or oGCD.</summary>
    public required ActionCategory Category { get; init; }

    /// <summary>How this action selects targets.</summary>
    public required ActionTargetType TargetType { get; init; }

    /// <summary>What effects this action has (can be combined).</summary>
    public ActionEffectType EffectTypes { get; init; } = ActionEffectType.None;

    /// <summary>Cast time in seconds (0 for instant).</summary>
    public float CastTime { get; init; }

    /// <summary>Recast time in seconds (GCD actions typically 2.5s, oGCDs vary).</summary>
    public float RecastTime { get; init; } = 2.5f;

    /// <summary>Range in yalms (0 for self-targeted).</summary>
    public float Range { get; init; }

    /// <summary>Radius for AoE effects in yalms.</summary>
    public float Radius { get; init; }

    /// <summary>MP cost.</summary>
    public int MpCost { get; init; }

    /// <summary>Healing potency for prediction calculations.</summary>
    public int HealPotency { get; init; }

    /// <summary>Damage potency for prediction calculations.</summary>
    public int DamagePotency { get; init; }

    /// <summary>Shield potency for prediction calculations.</summary>
    public int ShieldPotency { get; init; }

    /// <summary>Status ID applied by this action (e.g., Regen from Medica II).</summary>
    public uint? AppliedStatusId { get; init; }

    /// <summary>Duration of applied status in seconds.</summary>
    public float AppliedStatusDuration { get; init; }

    /// <summary>Range squared - pre-computed for distance checks (avoids repeated multiplication).</summary>
    public float RangeSquared => Range * Range;

    /// <summary>Radius squared - pre-computed for AoE distance checks.</summary>
    public float RadiusSquared => Radius * Radius;

    /// <summary>Whether this action can be cast while moving.</summary>
    public bool IsInstantCast => CastTime <= 0;

    /// <summary>Whether this action is a GCD.</summary>
    public bool IsGCD => Category == ActionCategory.GCD;

    /// <summary>Whether this action is an oGCD.</summary>
    public bool IsOGCD => Category == ActionCategory.oGCD;

    /// <summary>Whether this action heals.</summary>
    public bool IsHeal => EffectTypes.HasFlag(ActionEffectType.Heal);

    /// <summary>Whether this action deals damage.</summary>
    public bool IsDamage => EffectTypes.HasFlag(ActionEffectType.Damage);

    /// <summary>Whether this action is an AoE.</summary>
    public bool IsAoE => TargetType is ActionTargetType.PartyAoE or ActionTargetType.GroundAoE;

    /// <summary>
    /// Estimates the heal amount using the actual FFXIV healing formula.
    /// </summary>
    /// <param name="mind">Player's Mind stat.</param>
    /// <param name="determination">Player's Determination stat.</param>
    /// <param name="weaponDamage">Player's weapon magic damage.</param>
    /// <param name="level">Player's current level.</param>
    /// <returns>Estimated heal amount (with correction factor applied).</returns>
    public int EstimateHealAmount(int mind, int determination, int weaponDamage, int level)
    {
        return HealingCalculator.CalculateHeal(HealPotency, mind, determination, weaponDamage, level);
    }

    /// <summary>
    /// Gets the raw heal amount without correction factor - used for calibration.
    /// </summary>
    public int EstimateHealAmountRaw(int mind, int determination, int weaponDamage, int level)
    {
        return HealingCalculator.CalculateHealRaw(HealPotency, mind, determination, weaponDamage, level);
    }

    public override string ToString() => $"{Name} ({ActionId})";
}
