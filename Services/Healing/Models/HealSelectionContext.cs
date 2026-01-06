using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;

namespace Olympus.Services.Healing.Models;

/// <summary>
/// Context for single-target heal selection decisions.
/// Contains all factors that influence which heal to select.
/// </summary>
public sealed record HealSelectionContext
{
    /// <summary>The player character casting the heal.</summary>
    public required IPlayerCharacter Player { get; init; }

    /// <summary>The target to heal.</summary>
    public required IBattleChara Target { get; init; }

    /// <summary>Player's Mind stat.</summary>
    public required int Mind { get; init; }

    /// <summary>Player's Determination stat.</summary>
    public required int Det { get; init; }

    /// <summary>Player's weapon damage.</summary>
    public required int Wd { get; init; }

    /// <summary>HP the target is missing (after pending heals).</summary>
    public required int MissingHp { get; init; }

    /// <summary>Target's current HP percentage (after pending heals).</summary>
    public required float HpPercent { get; init; }

    /// <summary>Current lily count (0-3).</summary>
    public required int LilyCount { get; init; }

    /// <summary>Current blood lily count (0-3).</summary>
    public required int BloodLilyCount { get; init; }

    /// <summary>Whether we're in a valid oGCD weave window.</summary>
    public required bool IsWeaveWindow { get; init; }

    /// <summary>Whether the player has the Freecure proc.</summary>
    public required bool HasFreecure { get; init; }

    /// <summary>Whether the target already has Regen active.</summary>
    public required bool HasRegen { get; init; }

    /// <summary>Remaining duration of Regen on target (0 if not present).</summary>
    public required float RegenRemaining { get; init; }

    /// <summary>Whether MP is low and should conserve.</summary>
    public required bool IsInMpConservationMode { get; init; }

    /// <summary>The configured lily generation strategy.</summary>
    public required LilyGenerationStrategy LilyStrategy { get; init; }

    /// <summary>Current combat duration in seconds (for lily timing).</summary>
    public required float CombatDuration { get; init; }

    /// <summary>Configuration for healing behavior.</summary>
    public required HealingConfig Config { get; init; }

    /// <summary>Current damage rate (DPS) being taken by the target. Used for dynamic thresholds.</summary>
    public float DamageRate { get; init; }
}

/// <summary>
/// Context for AoE heal selection decisions.
/// Contains all factors that influence which AoE heal to select.
/// </summary>
public sealed record AoEHealSelectionContext
{
    /// <summary>The player character casting the heal.</summary>
    public required IPlayerCharacter Player { get; init; }

    /// <summary>Player's Mind stat.</summary>
    public required int Mind { get; init; }

    /// <summary>Player's Determination stat.</summary>
    public required int Det { get; init; }

    /// <summary>Player's weapon damage.</summary>
    public required int Wd { get; init; }

    /// <summary>Average missing HP across injured targets.</summary>
    public required int AverageMissingHp { get; init; }

    /// <summary>Number of injured party members (for self-centered AoE).</summary>
    public required int InjuredCount { get; init; }

    /// <summary>Whether any targets already have a Medica regen.</summary>
    public required bool AnyHaveRegen { get; init; }

    /// <summary>Whether we're in a valid oGCD weave window.</summary>
    public required bool IsWeaveWindow { get; init; }

    /// <summary>Number of targets within Cure III radius of best target.</summary>
    public required int CureIIITargetCount { get; init; }

    /// <summary>The best target for Cure III (party member with most injured allies nearby).</summary>
    public IBattleChara? CureIIITarget { get; init; }

    /// <summary>Whether MP is low and should conserve.</summary>
    public required bool IsInMpConservationMode { get; init; }

    /// <summary>Current lily count (0-3).</summary>
    public required int LilyCount { get; init; }

    /// <summary>Current blood lily count (0-3).</summary>
    public required int BloodLilyCount { get; init; }

    /// <summary>The configured lily generation strategy.</summary>
    public required LilyGenerationStrategy LilyStrategy { get; init; }

    /// <summary>Current combat duration in seconds (for lily timing).</summary>
    public required float CombatDuration { get; init; }

    /// <summary>Configuration for healing behavior.</summary>
    public required HealingConfig Config { get; init; }

    /// <summary>Party-wide damage rate (DPS). Used for damage-aware AoE lily selection.</summary>
    public float PartyDamageRate { get; init; }
}
