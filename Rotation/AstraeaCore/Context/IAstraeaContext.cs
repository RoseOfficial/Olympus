using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services.Astrologian;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Context;

/// <summary>
/// Interface for Astraea context (for testability).
/// Extends the healer rotation context with AST-specific properties.
/// </summary>
public interface IAstraeaContext : IHealerRotationContext
{
    // AST-specific services
    ICardTrackingService CardService { get; }
    IEarthlyStarService EarthlyStarService { get; }

    // AST Card State (Dawntrail: 4 cards drawn at once)
    Data.ASTActions.CardType CurrentCard { get; }
    Data.ASTActions.CardType MinorArcana { get; }
    bool HasCard { get; }
    bool HasBalance { get; }
    bool HasSpear { get; }
    bool HasMinorArcana { get; }
    int SealCount { get; }
    int UniqueSealCount { get; }
    int BalanceCount { get; }
    int SpearCount { get; }
    int TotalCardsInHand { get; }
    bool CanUseAstrodyne { get; }

    // Earthly Star State
    bool IsStarPlaced { get; }
    bool IsStarMature { get; }
    float StarTimeRemaining { get; }

    // AST-specific status checks
    bool HasLightspeed { get; }
    bool HasNeutralSect { get; }
    bool HasDivining { get; }
    bool HasHoroscope { get; }
    bool HasHoroscopeHelios { get; }
    bool HasMacrocosmos { get; }
    bool HasSynastry { get; }

    // Debug state
    AstraeaDebugState Debug { get; }

    // Smart healing services
    ICoHealerDetectionService? CoHealerDetectionService { get; }
    IBossMechanicDetector? BossMechanicDetector { get; }
    IShieldTrackingService? ShieldTrackingService { get; }

    // Helpers
    AstraeaStatusHelper StatusHelper { get; }
    AstraeaPartyHelper PartyHelper { get; }

    // Training mode
    ITrainingService? TrainingService { get; }

    // Healing coordination
    HealingCoordinationState HealingCoordination { get; }

    // Logging helpers
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);
    void LogCardDecision(string cardName, string targetName, string reason);
    void LogEarthlyStarDecision(string action, string reason);
    void LogBuffDecision(string buffName, string targetName, string reason);
}
