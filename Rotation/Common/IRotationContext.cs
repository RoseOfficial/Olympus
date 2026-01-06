using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Common;

/// <summary>
/// Base interface for rotation contexts across all jobs.
/// Contains common player state, services, and helper utilities.
/// Job-specific contexts extend this interface with their own properties.
/// </summary>
public interface IRotationContext
{
    #region Player State

    /// <summary>
    /// The local player character.
    /// </summary>
    IPlayerCharacter Player { get; }

    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    bool InCombat { get; }

    /// <summary>
    /// Whether the player is currently moving.
    /// </summary>
    bool IsMoving { get; }

    /// <summary>
    /// Whether the player can currently execute a GCD action.
    /// </summary>
    bool CanExecuteGcd { get; }

    /// <summary>
    /// Whether the player can currently execute an oGCD action.
    /// </summary>
    bool CanExecuteOgcd { get; }

    #endregion

    #region Core Services

    /// <summary>
    /// Service for executing actions.
    /// </summary>
    IActionService ActionService { get; }

    /// <summary>
    /// Tracks action execution history.
    /// </summary>
    ActionTracker ActionTracker { get; }

    /// <summary>
    /// Service for tracking combat events (damage, healing).
    /// </summary>
    ICombatEventService CombatEventService { get; }

    /// <summary>
    /// Service for tracking damage intake rates.
    /// </summary>
    IDamageIntakeService DamageIntakeService { get; }

    /// <summary>
    /// Service for damage trend analysis (spike detection).
    /// </summary>
    IDamageTrendService DamageTrendService { get; }

    /// <summary>
    /// Frame-scoped cache for reducing redundant calculations.
    /// </summary>
    IFrameScopedCache FrameCache { get; }

    /// <summary>
    /// Plugin configuration.
    /// </summary>
    Configuration Configuration { get; }

    /// <summary>
    /// Service for detecting cleansable debuffs.
    /// </summary>
    IDebuffDetectionService DebuffDetectionService { get; }

    /// <summary>
    /// Service for predicting HP after pending heals.
    /// </summary>
    IHpPredictionService HpPredictionService { get; }

    /// <summary>
    /// Service for MP forecasting and conservation mode.
    /// </summary>
    IMpForecastService MpForecastService { get; }

    /// <summary>
    /// Service for reading player stats (Mind, Determination, etc.).
    /// </summary>
    IPlayerStatsService PlayerStatsService { get; }

    /// <summary>
    /// Service for enemy targeting.
    /// </summary>
    ITargetingService TargetingService { get; }

    #endregion

    #region Dalamud Services

    /// <summary>
    /// Game object table for finding entities.
    /// </summary>
    IObjectTable ObjectTable { get; }

    /// <summary>
    /// Party list for party member information.
    /// </summary>
    IPartyList PartyList { get; }

    /// <summary>
    /// Optional plugin logger.
    /// </summary>
    IPluginLog? Log { get; }

    #endregion

    #region Party Health

    /// <summary>
    /// Cached party health metrics (avgHpPercent, lowestHpPercent, injuredCount).
    /// Computed once per frame.
    /// </summary>
    (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics { get; }

    #endregion

    #region Role Actions

    /// <summary>
    /// Whether the player has Swiftcast active.
    /// </summary>
    bool HasSwiftcast { get; }

    #endregion
}

/// <summary>
/// Extended rotation context for healer jobs.
/// Adds healing-specific services and helpers.
/// </summary>
public interface IHealerRotationContext : IRotationContext
{
    /// <summary>
    /// Service for selecting optimal healing spells.
    /// </summary>
    IHealingSpellSelector HealingSpellSelector { get; }

    /// <summary>
    /// Service for analyzing party health and finding heal targets.
    /// </summary>
    IPartyAnalyzer? PartyAnalyzer { get; }

    /// <summary>
    /// Service for cooldown planning decisions (defensive cooldowns, resource management).
    /// </summary>
    ICooldownPlanner CooldownPlanner { get; }
}
