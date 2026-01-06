using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.ApolloCore.Context;

/// <summary>
/// Interface for Apollo context to enable unit testing.
/// Provides access to player state, services, and helper utilities.
/// </summary>
public interface IApolloContext
{
    // Player state
    IPlayerCharacter Player { get; }
    bool InCombat { get; }
    bool IsMoving { get; }
    bool CanExecuteGcd { get; }
    bool CanExecuteOgcd { get; }

    // Services with interfaces
    IActionService ActionService { get; }
    ICombatEventService CombatEventService { get; }
    IDamageIntakeService DamageIntakeService { get; }
    IDamageTrendService DamageTrendService { get; }
    IFrameScopedCache FrameCache { get; }
    Configuration Configuration { get; }
    IDebuffDetectionService DebuffDetectionService { get; }
    IHpPredictionService HpPredictionService { get; }
    IMpForecastService MpForecastService { get; }
    IPlayerStatsService PlayerStatsService { get; }
    ITargetingService TargetingService { get; }

    // Services without interfaces (concrete types for now)
    ActionTracker ActionTracker { get; }
    IHealingSpellSelector HealingSpellSelector { get; }

    // Dalamud services
    IObjectTable ObjectTable { get; }
    IPartyList PartyList { get; }

    // Helpers
    StatusHelper StatusHelper { get; }
    IPartyHelper PartyHelper { get; }

    // Debug state
    DebugState Debug { get; }

    // Cached status checks (computed once per frame)
    bool HasThinAir { get; }
    bool HasFreecure { get; }
    bool HasSwiftcast { get; }
    int LilyCount { get; }
    int BloodLilyCount { get; }
    int SacredSightStacks { get; }

    /// <summary>
    /// Cached party health metrics (avgHpPercent, lowestHpPercent, injuredCount).
    /// </summary>
    (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics { get; }
}
