using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Base;

/// <summary>
/// Base class for healer rotation implementations.
/// Provides shared healer services and update patterns.
/// </summary>
/// <typeparam name="TContext">The healer job-specific context type.</typeparam>
/// <typeparam name="TModule">The healer job-specific module interface type.</typeparam>
public abstract class BaseHealerRotation<TContext, TModule> : BaseRotation<TContext, TModule>
    where TContext : IHealerRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Healer-Specific Services

    protected readonly HealingSpellSelector HealingSpellSelector;
    protected readonly ICooldownPlanner CooldownPlanner;
    protected readonly CoHealerDetectionService CoHealerDetectionService;
    protected readonly BossMechanicDetector BossMechanicDetector;
    protected readonly ShieldTrackingService ShieldTrackingService;
    protected readonly IPartyCoordinationService? PartyCoordinationService;

    /// <summary>
    /// Timer for rate-limiting gauge state broadcasts to once per second.
    /// </summary>
    private readonly Stopwatch _gaugeBroadcastTimer = Stopwatch.StartNew();

    #endregion

    #region Constructor

    protected BaseHealerRotation(
        IPluginLog log,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetingService targetingService,
        IHpPredictionService hpPredictionService,
        ActionService actionService,
        IPlayerStatsService playerStatsService,
        IDebuffDetectionService debuffDetectionService,
        HealingSpellSelector healingSpellSelector,
        ICooldownPlanner cooldownPlanner,
        ShieldTrackingService shieldTrackingService,
        IPartyCoordinationService? partyCoordinationService = null,
        IErrorMetricsService? errorMetrics = null)
        : base(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            errorMetrics)
    {
        HealingSpellSelector = healingSpellSelector;
        CooldownPlanner = cooldownPlanner;
        ShieldTrackingService = shieldTrackingService;
        PartyCoordinationService = partyCoordinationService;

        // Initialize smart healing services (these are healer-specific and per-rotation)
        CoHealerDetectionService = new CoHealerDetectionService(
            combatEventService, partyList, objectTable, configuration.Healing, partyCoordinationService);
        BossMechanicDetector = new BossMechanicDetector(
            configuration.Healing, combatEventService, damageIntakeService, partyList, objectTable);
    }

    #endregion

    #region Healer-Specific Updates

    /// <summary>
    /// Updates shared healer services (shield tracking, co-healer detection, mechanic detection).
    /// </summary>
    protected virtual void UpdateHealerServices(IPlayerCharacter player, bool inCombat)
    {
        CoHealerDetectionService.Update(player.EntityId);
        BossMechanicDetector.Update();
    }

    /// <summary>
    /// Updates damage trend service with party entity IDs.
    /// </summary>
    protected virtual void UpdateDamageTrend(IPlayerCharacter player, IEnumerable<uint> partyEntityIds)
    {
        DamageTrendService.Update(1f / 60f, partyEntityIds);
    }

    /// <summary>
    /// Updates cooldown planner with party health state.
    /// </summary>
    protected virtual void UpdateCooldownPlanner(float avgHpPercent, float lowestHpPercent, int injuredCount)
    {
        var criticalCount = lowestHpPercent < 0.30f ? Math.Max(1, injuredCount / 2) : 0;
        CooldownPlanner.Update(avgHpPercent, lowestHpPercent, injuredCount, criticalCount);
    }

    /// <summary>
    /// The party helper for this healer rotation.
    /// Used by the default implementations of GetPartyEntityIds and GetPartyHealthMetrics.
    /// </summary>
    protected abstract HealerPartyHelper HealerParty { get; }

    /// <summary>
    /// Collects party entity IDs for damage trend tracking.
    /// </summary>
    protected virtual IEnumerable<uint> GetPartyEntityIds(IPlayerCharacter player)
    {
        foreach (var member in HealerParty.GetAllPartyMembers(player))
            yield return member.EntityId;
    }

    /// <summary>
    /// Gets party health metrics for cooldown planning.
    /// </summary>
    protected virtual (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IPlayerCharacter player)
    {
        return HealerParty.CalculatePartyHealthMetrics(player);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the healer rotation.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CoHealerDetectionService.Dispose();
            BossMechanicDetector.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Override Base Methods

    /// <summary>
    /// Override to add healer-specific service updates.
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Update combat event service with combat state (for various tracking)
        CombatEventService.UpdateCombatState(inCombat);

        // Update healer services
        UpdateHealerServices(player, inCombat);

        // Update damage trends and cooldown planner when in combat.
        // Compute party members once and reuse for both calls to avoid a second object-table scan.
        if (inCombat)
        {
            var partyMembers = HealerParty.GetAllPartyMembers(player).ToList();
            var partyEntityIds = partyMembers.Select(m => m.EntityId);
            UpdateDamageTrend(player, partyEntityIds);

            var (avgHpPercent, lowestHpPercent, injuredCount) = HealerPartyHelper.CalculatePartyHealthMetrics(partyMembers);
            UpdateCooldownPlanner(avgHpPercent, lowestHpPercent, injuredCount);
        }

        // Broadcast gauge state every 1s (not every frame) for multi-healer coordination
        if (PartyCoordinationService != null &&
            PartyCoordinationService.IsPartyCoordinationEnabled &&
            _gaugeBroadcastTimer.ElapsedMilliseconds >= 1000)
        {
            BroadcastHealerGaugeState(player);
            _gaugeBroadcastTimer.Restart();
        }
    }

    /// <summary>
    /// Override in derived classes to broadcast job-specific gauge state.
    /// Called once per second when party coordination is enabled.
    /// </summary>
    /// <param name="player">The local player character.</param>
    protected virtual void BroadcastHealerGaugeState(IPlayerCharacter player)
    {
        // Default implementation does nothing.
        // Override in derived healer classes to broadcast job-specific gauge state.
    }

    #endregion
}
