using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Party;

/// <summary>
/// Party health metrics snapshot.
/// </summary>
public record PartyHealthMetrics(
    float AverageHpPercent,
    float LowestHpPercent,
    int InjuredCount,
    int CriticalCount
);

/// <summary>
/// Abstraction for party analysis across all healer jobs.
/// Provides a unified interface for finding heal targets and assessing party health.
/// </summary>
public interface IPartyAnalyzer
{
    /// <summary>
    /// Finds the party member most in need of healing based on damage intake and HP.
    /// </summary>
    /// <param name="healAmount">Expected heal amount for overheal prevention.</param>
    /// <returns>The most endangered party member, or null if none need healing.</returns>
    IBattleChara? FindMostEndangeredMember(int healAmount = 0);

    /// <summary>
    /// Finds the party member with the lowest HP percentage.
    /// </summary>
    /// <param name="healAmount">Expected heal amount for overheal prevention.</param>
    /// <returns>The lowest HP party member, or null if none need healing.</returns>
    IBattleChara? FindLowestHpMember(int healAmount = 0);

    /// <summary>
    /// Finds the tank in the party.
    /// </summary>
    /// <returns>The tank, or null if not found.</returns>
    IBattleChara? FindTank();

    /// <summary>
    /// Finds a dead party member needing resurrection.
    /// </summary>
    /// <returns>A dead party member without raise status, or null if none found.</returns>
    IBattleChara? FindDeadMemberNeedingRaise();

    /// <summary>
    /// Gets overall party health metrics.
    /// </summary>
    PartyHealthMetrics GetHealthMetrics();

    /// <summary>
    /// Counts party members who would benefit from an AoE heal.
    /// </summary>
    /// <param name="healAmount">Expected heal amount per target.</param>
    /// <returns>Number of party members who need healing.</returns>
    int CountMembersNeedingAoEHeal(int healAmount);

    /// <summary>
    /// Finds the best position for a ground-targeted AoE heal.
    /// </summary>
    /// <param name="healRadius">Radius of the AoE heal.</param>
    /// <param name="healAmount">Expected heal amount per target.</param>
    /// <returns>Best target for centering the AoE, or null if not worth using.</returns>
    IBattleChara? FindBestAoEHealTarget(float healRadius, int healAmount);
}
