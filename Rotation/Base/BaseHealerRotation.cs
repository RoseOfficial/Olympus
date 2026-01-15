using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common;
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
public abstract class BaseHealerRotation<TContext, TModule> : BaseRotation<TContext, TModule>, IDisposable
    where TContext : IHealerRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Healer-Specific Services

    protected readonly HealingSpellSelector HealingSpellSelector;
    protected readonly ICooldownPlanner CooldownPlanner;
    protected readonly CoHealerDetectionService CoHealerDetectionService;
    protected readonly BossMechanicDetector BossMechanicDetector;
    protected readonly ShieldTrackingService ShieldTrackingService;

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

        // Initialize smart healing services (these are healer-specific and per-rotation)
        CoHealerDetectionService = new CoHealerDetectionService(
            combatEventService, partyList, objectTable, configuration.Healing);
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
        ShieldTrackingService.Update();
        CoHealerDetectionService.Update(player.EntityId);
        BossMechanicDetector.Update();
    }

    /// <summary>
    /// Updates damage trend service with party entity IDs.
    /// </summary>
    protected virtual void UpdateDamageTrend(IPlayerCharacter player, IEnumerable<uint> partyEntityIds)
    {
        (DamageTrendService as DamageTrendService)?.Update(1f / 60f, new List<uint>(partyEntityIds));
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
    /// Collects party entity IDs for damage trend tracking.
    /// </summary>
    protected abstract IEnumerable<uint> GetPartyEntityIds(IPlayerCharacter player);

    /// <summary>
    /// Gets party health metrics for cooldown planning.
    /// </summary>
    protected abstract (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IPlayerCharacter player);

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the healer rotation.
    /// </summary>
    public virtual void Dispose()
    {
        CoHealerDetectionService.Dispose();
        BossMechanicDetector.Dispose();
    }

    #endregion

    #region Override Base Methods

    /// <summary>
    /// Override to add healer-specific service updates.
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Update combat event service with combat state (for various tracking)
        (CombatEventService as CombatEventService)?.UpdateCombatState(inCombat);

        // Update healer services
        UpdateHealerServices(player, inCombat);

        // Update damage trends and cooldown planner when in combat
        if (inCombat)
        {
            var partyEntityIds = GetPartyEntityIds(player);
            UpdateDamageTrend(player, partyEntityIds);

            var (avgHpPercent, lowestHpPercent, injuredCount) = GetPartyHealthMetrics(player);
            UpdateCooldownPlanner(avgHpPercent, lowestHpPercent, injuredCount);
        }
    }

    #endregion
}
