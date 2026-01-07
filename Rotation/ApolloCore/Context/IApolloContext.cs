using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.ApolloCore.Context;

/// <summary>
/// Interface for Apollo (White Mage) context.
/// Extends the healer rotation context with WHM-specific properties.
/// </summary>
public interface IApolloContext : IHealerRotationContext
{
    // WHM-specific helpers
    StatusHelper StatusHelper { get; }
    IPartyHelper PartyHelper { get; }

    // Debug state
    DebugState Debug { get; }

    // Healing coordination
    /// <summary>
    /// Frame-scoped coordination state to prevent multiple handlers from targeting the same entity.
    /// </summary>
    HealingCoordinationState HealingCoordination { get; }

    // Smart healing services
    /// <summary>
    /// Service for tracking co-healer presence and activity.
    /// Null if co-healer awareness is disabled or not available.
    /// </summary>
    ICoHealerDetectionService? CoHealerDetectionService { get; }

    /// <summary>
    /// Detector for boss mechanic patterns (raidwides, tank busters).
    /// Null if mechanic awareness is disabled or not available.
    /// </summary>
    IBossMechanicDetector? BossMechanicDetector { get; }

    /// <summary>
    /// Service for tracking shields and mitigation buffs on party members.
    /// </summary>
    IShieldTrackingService? ShieldTrackingService { get; }

    // WHM-specific status checks
    /// <summary>
    /// Whether the player has Thin Air active (free MP on next spell).
    /// </summary>
    bool HasThinAir { get; }

    /// <summary>
    /// Whether the player has Freecure proc (free Cure II).
    /// </summary>
    bool HasFreecure { get; }

    // WHM Job Gauge
    /// <summary>
    /// Current Lily count (0-3).
    /// </summary>
    int LilyCount { get; }

    /// <summary>
    /// Current Blood Lily count (0-3).
    /// </summary>
    int BloodLilyCount { get; }

    /// <summary>
    /// Current Sacred Sight stacks for Glare IV (0-3).
    /// </summary>
    int SacredSightStacks { get; }

    // Logging helpers (implemented in ApolloContext)
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);
    void LogOgcdDecision(string targetName, float hpPercent, string spellName, string reason);
    void LogDefensiveDecision(string targetName, float hpPercent, string spellName, float damageRate, string reason);
}
