using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;

namespace Olympus.Services.Healing;

/// <summary>
/// Interface for healing spell selection, enabling testability.
/// </summary>
public interface IHealingSpellSelector
{
    /// <summary>
    /// Gets the last spell selection decision for debugging.
    /// </summary>
    SpellSelectionDebug? LastSelection { get; }

    /// <summary>
    /// Selects the best single-target heal for a given target.
    /// Uses tiered priority: oGCDs > Lily heals > Regen > GCD heals
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="target">The target to heal.</param>
    /// <param name="isWeaveWindow">Whether we're in a valid oGCD weave window.</param>
    /// <param name="hasFreecure">Whether the player has the Freecure proc (prioritizes Cure II).</param>
    /// <param name="hasRegen">Whether the target already has Regen active.</param>
    /// <param name="regenRemaining">Remaining duration of Regen on target (0 if not present).</param>
    /// <param name="isInMpConservationMode">Whether MP is low and should conserve.</param>
    /// <returns>The best action and its heal amount, or null if no valid heal found.</returns>
    (ActionDefinition? action, int healAmount) SelectBestSingleHeal(
        IPlayerCharacter player,
        IBattleChara target,
        bool isWeaveWindow,
        bool hasFreecure = false,
        bool hasRegen = false,
        float regenRemaining = 0f,
        bool isInMpConservationMode = false);

    /// <summary>
    /// Selects the best AoE heal for the current situation.
    /// Considers: oGCD heals, Lily heals, and GCD heals.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="averageMissingHp">Average missing HP across injured party members.</param>
    /// <param name="injuredCount">Number of party members needing healing.</param>
    /// <param name="anyHaveRegen">Whether any injured members already have a regen effect.</param>
    /// <param name="isWeaveWindow">Whether we're in a valid oGCD weave window.</param>
    /// <param name="cureIIITargetCount">Number of valid Cure III targets.</param>
    /// <param name="cureIIITarget">Best target for Cure III (if available).</param>
    /// <param name="isInMpConservationMode">Whether MP is low and should conserve.</param>
    /// <param name="partyDamageRate">Party-wide damage rate (DPS) for damage-aware lily selection.</param>
    /// <returns>The best action, its heal amount, and Cure III target (if selected).</returns>
    (ActionDefinition? action, int healAmount, IBattleChara? cureIIITarget) SelectBestAoEHeal(
        IPlayerCharacter player,
        int averageMissingHp,
        int injuredCount,
        bool anyHaveRegen,
        bool isWeaveWindow,
        int cureIIITargetCount = 0,
        IBattleChara? cureIIITarget = null,
        bool isInMpConservationMode = false,
        float partyDamageRate = 0f);
}
