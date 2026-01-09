using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Olympus.Services.Scholar;

/// <summary>
/// Tracks Scholar's Aetherflow resource.
/// Aetherflow provides 3 stacks that power oGCD heals and Energy Drain.
/// </summary>
public sealed class AetherflowTrackingService : IAetherflowTrackingService
{
    /// <summary>
    /// Maximum Aetherflow stacks.
    /// </summary>
    public const int MaxStacks = 3;

    /// <summary>
    /// Fairy Gauge generated per Aetherflow ability used.
    /// </summary>
    public const int FairyGaugePerStack = 10;

    /// <summary>
    /// Gets the current number of Aetherflow stacks.
    /// </summary>
    public int CurrentStacks => GetAetherflowStacks();

    /// <summary>
    /// Returns true if we have any Aetherflow stacks available.
    /// </summary>
    public bool HasStacks => CurrentStacks > 0;

    /// <summary>
    /// Returns true if Aetherflow is at max stacks.
    /// </summary>
    public bool IsAtMax => CurrentStacks >= MaxStacks;

    /// <summary>
    /// Returns true if we should use Aetherflow ability to avoid wasting the cooldown.
    /// Typically when at 0 stacks.
    /// </summary>
    public bool ShouldRefreshAetherflow => CurrentStacks == 0;

    /// <summary>
    /// Returns true if we should dump stacks before Aetherflow comes off cooldown.
    /// </summary>
    /// <param name="aetherflowCooldownRemaining">Remaining cooldown on Aetherflow in seconds.</param>
    /// <param name="reserveCount">Minimum stacks to reserve for emergencies.</param>
    public bool ShouldDumpStacks(float aetherflowCooldownRemaining, int reserveCount = 0)
    {
        var stacks = CurrentStacks;
        if (stacks <= reserveCount)
            return false;

        // If Aetherflow is coming off cooldown soon and we have stacks, dump them
        // Allow ~5 seconds to use remaining stacks
        return aetherflowCooldownRemaining < 5f && stacks > 0;
    }

    /// <summary>
    /// Returns true if we have enough stacks for the specified cost.
    /// </summary>
    /// <param name="cost">Number of stacks required (typically 1).</param>
    public bool CanAfford(int cost = 1) => CurrentStacks >= cost;

    /// <summary>
    /// Returns true if we should reserve stacks for emergency healing.
    /// </summary>
    /// <param name="reserveCount">Minimum stacks to reserve.</param>
    /// <param name="partyHealthCritical">Whether party health is in critical state.</param>
    public bool ShouldReserveForHealing(int reserveCount, bool partyHealthCritical)
    {
        // If party health is critical, don't reserve - use stacks for healing
        if (partyHealthCritical)
            return false;

        return CurrentStacks <= reserveCount;
    }

    /// <summary>
    /// Called when an Aetherflow ability is used.
    /// Note: This is informational only - the game manages actual stack consumption.
    /// </summary>
    public void ConsumeStack()
    {
        // No-op: Game manages stack consumption automatically.
        // This method exists for semantic clarity in calling code.
    }

    /// <summary>
    /// Gets the remaining cooldown on Aetherflow ability.
    /// </summary>
    public unsafe float GetCooldownRemaining()
    {
        try
        {
            var actionManager = ActionManager.Instance();
            if (actionManager == null)
                return 60f; // Assume worst case

            // Aetherflow action ID
            const uint aetherflowActionId = 166;
            var recastGroup = actionManager->GetRecastGroup(1, aetherflowActionId);
            var recastInfo = actionManager->GetRecastGroupDetail(recastGroup);

            if (recastInfo == null)
                return 0f;

            return recastInfo->Total - recastInfo->Elapsed;
        }
        catch
        {
            return 60f;
        }
    }

    /// <summary>
    /// Gets the Aetherflow stack count from the game's job gauge.
    /// </summary>
    private static unsafe int GetAetherflowStacks()
    {
        try
        {
            var jobGauge = JobGaugeManager.Instance();
            if (jobGauge == null)
                return 0;

            // Scholar job gauge stores Aetherflow in the first byte
            // The gauge structure varies by job, but SCH uses bytes for Aetherflow and Fairy Gauge
            var gaugeData = jobGauge->CurrentGauge;

            // For SCH: byte 0 = Aetherflow stacks, byte 1 = Fairy Gauge
            // Access the raw gauge data
            var rawGauge = (byte*)&gaugeData;
            return rawGauge[0];
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Interface for Aetherflow tracking service.
/// </summary>
public interface IAetherflowTrackingService
{
    /// <summary>
    /// Gets the current number of Aetherflow stacks.
    /// </summary>
    int CurrentStacks { get; }

    /// <summary>
    /// Returns true if we have any Aetherflow stacks available.
    /// </summary>
    bool HasStacks { get; }

    /// <summary>
    /// Returns true if Aetherflow is at max stacks.
    /// </summary>
    bool IsAtMax { get; }

    /// <summary>
    /// Returns true if we should use Aetherflow ability to refill stacks.
    /// </summary>
    bool ShouldRefreshAetherflow { get; }

    /// <summary>
    /// Returns true if we should dump stacks before Aetherflow comes off cooldown.
    /// </summary>
    bool ShouldDumpStacks(float aetherflowCooldownRemaining, int reserveCount = 0);

    /// <summary>
    /// Returns true if we have enough stacks for the specified cost.
    /// </summary>
    bool CanAfford(int cost = 1);

    /// <summary>
    /// Returns true if we should reserve stacks for emergency healing.
    /// </summary>
    bool ShouldReserveForHealing(int reserveCount, bool partyHealthCritical);

    /// <summary>
    /// Called when an Aetherflow ability is used.
    /// </summary>
    void ConsumeStack();

    /// <summary>
    /// Gets the remaining cooldown on Aetherflow ability.
    /// </summary>
    float GetCooldownRemaining();
}
