using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Olympus.Services.Sage;

/// <summary>
/// Tracks Sage's Addersting resource.
/// Addersting is generated when Eukrasian Diagnosis shield is fully consumed by damage.
/// Stacks are consumed by Toxikon (instant cast AoE damage).
/// Starts at 3 stacks when entering an instance.
/// </summary>
public sealed class AdderstingTrackingService : IAdderstingTrackingService
{
    /// <summary>
    /// Maximum Addersting stacks.
    /// </summary>
    public const int MaxStacks = 3;

    /// <summary>
    /// Gets the current number of Addersting stacks.
    /// </summary>
    public int CurrentStacks => GetAdderstingStacks();

    /// <summary>
    /// Returns true if we have any Addersting stacks available.
    /// </summary>
    public bool HasStacks => CurrentStacks > 0;

    /// <summary>
    /// Returns true if Addersting is at max stacks.
    /// </summary>
    public bool IsAtMax => CurrentStacks >= MaxStacks;

    /// <summary>
    /// Returns true if we have enough stacks for the specified cost.
    /// </summary>
    /// <param name="cost">Number of stacks required (typically 1).</param>
    public bool CanAfford(int cost = 1) => CurrentStacks >= cost;

    /// <summary>
    /// Returns true if we should use Toxikon for movement.
    /// Toxikon is instant cast, useful when moving.
    /// </summary>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    public bool ShouldUseToxikon(bool isMoving)
    {
        if (!HasStacks)
            return false;

        // Use Toxikon when moving to maintain DPS
        return isMoving;
    }

    /// <summary>
    /// Returns true if we should try to generate Addersting during downtime.
    /// Apply E.Diagnosis to tank to generate stacks when shields will break.
    /// </summary>
    /// <param name="inCombat">Whether currently in combat.</param>
    /// <param name="hasBossTarget">Whether there's an active boss target.</param>
    public bool ShouldGenerateDuringDowntime(bool inCombat, bool hasBossTarget)
    {
        // Only try to generate if not at max
        if (IsAtMax)
            return false;

        // During downtime (no boss target but in combat), can apply shields for future stacks
        return inCombat && !hasBossTarget;
    }

    /// <summary>
    /// Gets the Addersting stack count from the game's job gauge.
    /// Sage gauge: byte 0 = Addersgall, byte 1 = Addersting
    /// </summary>
    private static unsafe int GetAdderstingStacks()
    {
        try
        {
            var jobGauge = JobGaugeManager.Instance();
            if (jobGauge == null)
                return 0;

            var gaugeData = jobGauge->CurrentGauge;
            var rawGauge = (byte*)&gaugeData;

            // Sage gauge layout:
            // byte 0: Addersgall stacks (0-3)
            // byte 1: Addersting stacks (0-3)
            return rawGauge[1];
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Interface for Addersting tracking service.
/// </summary>
public interface IAdderstingTrackingService
{
    /// <summary>
    /// Gets the current number of Addersting stacks.
    /// </summary>
    int CurrentStacks { get; }

    /// <summary>
    /// Returns true if we have any Addersting stacks available.
    /// </summary>
    bool HasStacks { get; }

    /// <summary>
    /// Returns true if Addersting is at max stacks.
    /// </summary>
    bool IsAtMax { get; }

    /// <summary>
    /// Returns true if we have enough stacks for the specified cost.
    /// </summary>
    bool CanAfford(int cost = 1);

    /// <summary>
    /// Returns true if we should use Toxikon for movement.
    /// </summary>
    bool ShouldUseToxikon(bool isMoving);

    /// <summary>
    /// Returns true if we should try to generate Addersting during downtime.
    /// </summary>
    bool ShouldGenerateDuringDowntime(bool inCombat, bool hasBossTarget);
}
