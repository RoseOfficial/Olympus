using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services.Party;

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
