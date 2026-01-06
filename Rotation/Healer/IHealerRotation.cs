using Olympus.Services.Cooldown;
using Olympus.Services.Party;

namespace Olympus.Rotation.Healer;

/// <summary>
/// Base interface for all healer rotation modules.
/// Extends the base IRotation with healer-specific capabilities.
/// </summary>
public interface IHealerRotation : IRotation
{
    /// <summary>
    /// Gets the party analyzer for this rotation.
    /// </summary>
    IPartyAnalyzer PartyAnalyzer { get; }

    /// <summary>
    /// Gets the cooldown planner for this rotation.
    /// </summary>
    ICooldownPlanner CooldownPlanner { get; }

    /// <summary>
    /// Checks if this healer can currently cast Raise.
    /// </summary>
    bool CanRaise { get; }

    /// <summary>
    /// Checks if this healer has an instant cast available (Swiftcast, etc.).
    /// </summary>
    bool HasInstantCast { get; }

    /// <summary>
    /// Gets the healer's current MP.
    /// </summary>
    int CurrentMp { get; }

    /// <summary>
    /// Gets the MP cost for the healer's Raise spell.
    /// </summary>
    int RaiseMpCost { get; }

    /// <summary>
    /// Checks if the healer has their healing resource available.
    /// For WHM: Lilies, for SCH: Aetherflow, for AST: Cards, for SGE: Addersgall.
    /// </summary>
    bool HasHealingResource { get; }

    /// <summary>
    /// Gets the current healing resource count.
    /// </summary>
    int HealingResourceCount { get; }

    /// <summary>
    /// Gets the maximum healing resource count.
    /// </summary>
    int MaxHealingResourceCount { get; }
}
