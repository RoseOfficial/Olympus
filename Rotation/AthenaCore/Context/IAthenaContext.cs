using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Scholar;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Context;

/// <summary>
/// Interface for Athena context (for testability).
/// Extends the healer rotation context with SCH-specific properties.
/// </summary>
public interface IAthenaContext : IHealerRotationContext
{
    // SCH-specific services
    IAetherflowTrackingService AetherflowService { get; }
    IFairyGaugeService FairyGaugeService { get; }
    IFairyStateManager FairyStateManager { get; }

    // SCH Job Gauge
    int AetherflowStacks { get; }
    int FairyGauge { get; }

    // SCH-specific status checks
    bool HasRecitation { get; }
    bool HasEmergencyTactics { get; }
    bool HasDissipation { get; }
    bool HasSeraphism { get; }
    bool HasImpactImminent { get; }

    // Debug state
    AthenaDebugState Debug { get; }

    // Smart healing services
    ICoHealerDetectionService? CoHealerDetectionService { get; }
    IBossMechanicDetector? BossMechanicDetector { get; }
    IShieldTrackingService? ShieldTrackingService { get; }

    // Helpers
    AthenaStatusHelper StatusHelper { get; }
    AthenaPartyHelper PartyHelper { get; }

    // Training mode
    ITrainingService? TrainingService { get; }

    // Healing coordination
    HealingCoordinationState HealingCoordination { get; }

    // Logging helpers
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);
    void LogAetherflowDecision(string spellName, int stacksRemaining, string reason);
    void LogFairyDecision(string abilityName, FairyState fairyState, string reason);
    void LogShieldDecision(string targetName, string spellName, int shieldAmount, string reason);
}
